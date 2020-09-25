using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Xpo.Smart.Core.Context;
using Xpo.Smart.Core.Models;
using Xpo.Smart.Core.Services;
using Xpo.Smart.Efficiency.Engine.Interfaces;
using Xpo.Smart.Efficiency.Engine.Models;
using Xpo.Smart.Efficiency.Shared.Models;

namespace Xpo.Smart.Efficiency.Engine
{
    public class LiveEfficiencyEngine : ILiveEfficiencyEngine
    {
        private readonly static ConcurrentDictionary<string, object> _locks = new ConcurrentDictionary<string, object>();

        private readonly EfficiencyEngine _efficiencyEngine;
        private readonly IEfficiencyShiftProvider _efficiencyShiftProvider;
        private readonly ILaborRateService _laborRateService;
        private readonly ISiteContextProvider _siteContextProvider;

        public LiveEfficiencyEngine(IEfficiencyShiftProvider efficiencyShiftProvider, ITransactionProvider transactionProvider,
            ILaborRateService laborRateService, ISiteContextProvider siteContextProvider)
        {
            _efficiencyShiftProvider = efficiencyShiftProvider;
            _efficiencyEngine = new EfficiencyEngine(transactionProvider);
            _laborRateService = laborRateService;
            _siteContextProvider = siteContextProvider;
        }

        public void ComputeForSiteCodesAndExecute(
            IDictionary<string, SiteEmployeeCode[]> siteEmployeeCodesBySiteCodes,
            IDictionary<string, SiteEmployeeCode[]> tnaEmployeeCodesBySiteCodes,
            DateTime minDate, DateTime maxDate, Action<IEnumerable<EfficiencyRecord>> execute)
        {
            ComputeAndExecute(siteEmployeeCodesBySiteCodes, tnaEmployeeCodesBySiteCodes, minDate, maxDate, execute);
        }

        public void ComputeForSiteEmployeeCodesAndExecute(
            IDictionary<string, SiteEmployeeCode[]> siteEmployeeCodesBySiteCodes,            
            DateTime minDate, DateTime maxDate, Action<IEnumerable<EfficiencyRecord>> execute)
        {
            ComputeAndExecute(siteEmployeeCodesBySiteCodes, new Dictionary<string,SiteEmployeeCode[]>(), minDate, maxDate, execute);
        }

        public void ComputeForTnaEmployeeCodesAndExecute(
            IDictionary<string, SiteEmployeeCode[]> tnaEmployeeCodesBySiteCodes,
            DateTime minDate, DateTime maxDate, Action<IEnumerable<EfficiencyRecord>> execute)
        {
            ComputeAndExecute(new Dictionary<string, SiteEmployeeCode[]>(), tnaEmployeeCodesBySiteCodes, minDate, maxDate, execute);
        }

        private void ComputeAndExecute(
            IDictionary<string, SiteEmployeeCode[]> siteEmployeeCodesBySiteCodes,
            IDictionary<string, SiteEmployeeCode[]> tnaEmployeeCodesBySiteCodes,
            DateTime minDate, DateTime maxDate, Action<IEnumerable<EfficiencyRecord>> execute)
        {

            var allSiteCodes = siteEmployeeCodesBySiteCodes.Keys
                .Union(tnaEmployeeCodesBySiteCodes.Keys)
                .Distinct()
                .ToArray();

            foreach (var siteCode in allSiteCodes)
            {
                var siteEmployeeCodes = (siteEmployeeCodesBySiteCodes.ContainsKey(siteCode)
                        ? siteEmployeeCodesBySiteCodes[siteCode]
                        : new SiteEmployeeCode[0])
                    .Select(r => r.EmployeeCode)
                    .ToArray();

                var tnaEmployeeCodes = (tnaEmployeeCodesBySiteCodes.ContainsKey(siteCode)
                        ? tnaEmployeeCodesBySiteCodes[siteCode]
                        : new SiteEmployeeCode[0])
                    .Select(r => r.EmployeeCode)
                    .ToArray();

                if (!siteEmployeeCodes.Any() && !tnaEmployeeCodes.Any())
                {
                    continue;
                }

                var laborRates = _laborRateService.GetTargetUphRatesBySiteCode(siteCode);
                var siteContext = _siteContextProvider.GetSiteContext(siteCode);

                ComputeAndExecute(siteCode, siteContext, laborRates, siteEmployeeCodes, tnaEmployeeCodes, minDate, maxDate, execute);
            }
        }

        private void ComputeAndExecute(
            string siteCode,
            ISiteContext siteContext,
            LaborRate[] laborRates,
            string[] siteEmployeeCodes,
            string[] tnaEmployeeCodes,
            DateTime minDate,
            DateTime maxDate,
            Action<IEnumerable<EfficiencyRecord>> execute)
        {
            lock (_locks.GetOrAdd(siteCode, new object()))
            {
                var siteTime = TrimDate(siteContext.CurrentSiteTime, TimeSpan.TicksPerMinute);
                var siteEmployeeShifts = siteEmployeeCodes?.Any() == true
                    ? _efficiencyShiftProvider.GetShiftsForSiteEmployeeCodes(siteCode, siteTime, siteEmployeeCodes, minDate, maxDate)
                    : new EfficiencyShift[0];

                if (siteEmployeeShifts.Any())
                {
                    var efficiencyRecords = _efficiencyEngine.Compute(siteEmployeeShifts, laborRates, minDate, maxDate);
                    execute?.Invoke(efficiencyRecords);
                }

                var tnaShifts = tnaEmployeeCodes?.Any() == true
                    ? _efficiencyShiftProvider.GetShiftsForTnaEmployeeCodes(siteCode, siteTime, tnaEmployeeCodes, minDate, maxDate)
                    : new EfficiencyShift[0];

                if (tnaShifts.Any())
                {
                    var efficiencyRecords = _efficiencyEngine.Compute(tnaShifts, laborRates, minDate, maxDate);
                    execute?.Invoke(efficiencyRecords);
                }
            }
        }

        private static DateTime TrimDate(DateTime date, long roundTicks)
        {
            return new DateTime(date.Ticks - date.Ticks % roundTicks, date.Kind);
        }
    }
}
