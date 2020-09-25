using System;
using System.Collections.Generic;
using System.Linq;
using Xpo.Smart.Core.DataCache;
using Xpo.Smart.Efficiency.Engine.Interfaces;
using Xpo.Smart.Efficiency.Engine.Models;
using Xpo.Smart.Efficiency.Shared.DataCache.Core;
using Xpo.Smart.Efficiency.Shared.DataCache.Efficiency;
using Xpo.Smart.Efficiency.Shared.DataCache.EfficiencyTimesheet;
using Xpo.Smart.Efficiency.Shared.DataCache.Transactions;

namespace Xpo.Smart.Efficiency.EventProcessor.Helpers
{
    public static class LiveEfficiencyCacheHelper
    {
        public static void ComputeAndUpdateCacheForAllSites(
            ICacheRepositoryFactory<ITransactionCacheRepository> transactionCacheRepositoryFactory,
            IDataCacheProvider<EfficiencyTimesheetCache> timesheetCacheProvider,
            IEmployeeEfficiencyCacheRepository liveEfficiencyCache,
            ILiveEfficiencyEngine liveEfficiencyEngine, 
            DateTime start, 
            DateTime end)
        {
            List<SiteEmployeeCode> siteEmployeeCodes = new List<SiteEmployeeCode>();
            var transactionCache = transactionCacheRepositoryFactory.GetCurrent();
            var transactionSiteCodes = transactionCache.GetSiteCodes();
            foreach (var siteCode in transactionSiteCodes)
            {
                siteEmployeeCodes.AddRange(transactionCache.GetTransactionSiteEmployeeCodes(siteCode)
                    .Select(x => new SiteEmployeeCode { SiteCode = siteCode, EmployeeCode = x }));
            }

            List<SiteEmployeeCode> tnaEmployeeCodes = new List<SiteEmployeeCode>();
            var timeSheetCache = timesheetCacheProvider.GetCurrent();
            var timeSheetSiteCodes = timeSheetCache.GetSiteCodes();
            foreach (var siteCode in timeSheetSiteCodes)
            {
                tnaEmployeeCodes.AddRange(timeSheetCache.GetAll(siteCode)
                    .GroupBy(r=> r.TnaEmployeeCode)
                    .Select(x => new SiteEmployeeCode { SiteCode = siteCode, EmployeeCode = x.Key })
                    .ToArray());
            }

            liveEfficiencyEngine.ComputeForSiteCodesAndExecute(
                siteEmployeeCodes.GroupBy(x => x.SiteCode).ToDictionary(r=>r.Key, r=> r.ToArray()),
                tnaEmployeeCodes.GroupBy(x => x.SiteCode).ToDictionary(r=>r.Key, r=> r.ToArray()),
                start, end, (calculatedEfficiency) => EfficiencyCacheHelper.UpdateLiveEfficiencyCache(calculatedEfficiency, liveEfficiencyCache, start, end,true));
        }
    }
}
