using System;
using System.Collections.Generic;
using System.Linq;
using Xpo.Common.Cqrs;
using Xpo.Smart.Core.DataCache;
using Xpo.Smart.Efficiency.Engine.Interfaces;
using Xpo.Smart.Efficiency.Engine.Models;
using Xpo.Smart.Efficiency.EventProcessor.Helpers;
using Xpo.Smart.Efficiency.Shared.DataCache.Core;
using Xpo.Smart.Efficiency.Shared.DataCache.Efficiency;
using Xpo.Smart.Efficiency.Shared.DataCache.EfficiencyTimesheet;
using Xpo.Smart.Efficiency.Shared.DataCache.Transactions;
using Xpo.Smart.Efficiency.Shared.Models;
using Timesheet = Xpo.Smart.Core.Models.Canonical.Tna.Timesheet;

namespace Xpo.Smart.Efficiency.EventProcessor.Events
{
    public class TimesheetEventHandler : IEventHandler<TimesheetIngestionEvent>
    {
        private readonly IInitEfficiencyRunner _initEfficiencyRunner;
        private readonly ICacheRepositoryFactory<ITransactionCacheRepository> _transactionCacheRepositoryFactory;
        private readonly ICacheRepositoryFactory<IEmployeeEfficiencyCacheRepository> _efficiencyCacheRepositoryFactory;
        private readonly IDataCacheProvider<EfficiencyTimesheetCache> _timesheetCacheProvider;
        private readonly ILiveEfficiencyEngine _liveEfficiencyEngine;

        protected TimesheetEventHandler(IInitEfficiencyRunner initEfficiencyRunner, ICacheRepositoryFactory<ITransactionCacheRepository> transactionCacheRepositoryFactory,
            ICacheRepositoryFactory<IEmployeeEfficiencyCacheRepository> efficiencyCacheRepositoryFactory, IDataCacheProvider<EfficiencyTimesheetCache> timesheetCacheProvider,
            ILiveEfficiencyEngine liveEfficiencyEngine)
        {
            _initEfficiencyRunner = initEfficiencyRunner;
            _transactionCacheRepositoryFactory = transactionCacheRepositoryFactory;
            _efficiencyCacheRepositoryFactory = efficiencyCacheRepositoryFactory;
            _timesheetCacheProvider = timesheetCacheProvider;
            _liveEfficiencyEngine = liveEfficiencyEngine;
        }

        public void Handle(TimesheetIngestionEvent @event)
        {
            UpdateTimeshetsCache(@event);

            UpdateEfficiencyRecordsCache(@event);
        }

        private void UpdateTimeshetsCache(TimesheetIngestionEvent @event)
        {
            var eventTimesheets = new List<Timesheet>() { @event.Value };
            var groupedTimesheets = eventTimesheets.GroupBy(r => r.SiteCode);
            var timesheetCache = _timesheetCacheProvider.GetCurrent();

            foreach (var group in groupedTimesheets)
            {
                var timesheets = BuildTimesheets(group).ToArray();

                timesheetCache.Prune(group.Key, timesheets.Select(r => r.TnaEmployeeCode));
                timesheetCache.AddOrUpdate(timesheets);
            }
        }

        private IEnumerable<EfficiencyTimeSheet> BuildTimesheets(IGrouping<string, Timesheet> group)
        {
            foreach (var ingestedTimesheet in group)
            {
                yield return BuildTimesheets(ingestedTimesheet);
            }
        }

        private EfficiencyTimeSheet BuildTimesheets(Timesheet timesheet)
        {
            return new EfficiencyTimeSheet
            {
                TnaEmployeeCode = timesheet.EmployeeNumber,
                WorkedWorkCenterCode = timesheet.WorkCenterCode,
                SiteCode = timesheet.SiteCode,
                ShiftCode = timesheet.ShiftCode,
                OperationalDate = timesheet.OperationalDate,
                PunchInTime = timesheet.PunchInDateTime,
                PunchOutTime = timesheet.PunchOutDateTime
                //TimeSheetId = timesheet.TimesheetId need to add tsid
            };
        }

        private void UpdateEfficiencyRecordsCache(TimesheetIngestionEvent @event)
        {
            var eventTimesheets = new List<Timesheet>() { @event.Value };

            var end = DateTime.Today.AddDays(1);
            var start = end.AddDays(-2);

            var liveEfficiencyCache = _efficiencyCacheRepositoryFactory.GetCurrent();

            if (!_initEfficiencyRunner.IsInitialLoad(() => LiveEfficiencyCacheHelper.ComputeAndUpdateCacheForAllSites(
                _transactionCacheRepositoryFactory,
                _timesheetCacheProvider,
                liveEfficiencyCache,
                _liveEfficiencyEngine,
                start, end)))
            {
                var tnaEmployeeCodesBySiteCodes = eventTimesheets
                    .Where(r => r.PunchInDateTime < end
                                && (r.PunchOutDateTime ?? start) >= start
                                && !string.IsNullOrWhiteSpace(r.SiteCode)
                                && !string.IsNullOrWhiteSpace(r.EmployeeNumber))
                    .GroupBy(r => new { r.SiteCode, r.EmployeeNumber })
                    .Select(r => new SiteEmployeeCode { SiteCode = r.Key.SiteCode, EmployeeCode = r.Key.EmployeeNumber })
                    .GroupBy(r => r.SiteCode)
                    .ToDictionary(r => r.Key, r => r.ToArray());

                if (!tnaEmployeeCodesBySiteCodes.Any())
                {
                    //historical ingestion events will be ignored
                    return;
                }

                _liveEfficiencyEngine.ComputeForTnaEmployeeCodesAndExecute(tnaEmployeeCodesBySiteCodes, start, end,
                    (calculatedEfficiency) => EfficiencyCacheHelper.UpdateLiveEfficiencyCache(calculatedEfficiency, liveEfficiencyCache, start, end, false));
            }
        }
    }
}
