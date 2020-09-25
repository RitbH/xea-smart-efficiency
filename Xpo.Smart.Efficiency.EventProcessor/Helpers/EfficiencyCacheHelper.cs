using System;
using System.Collections.Generic;
using System.Linq;
using Xpo.Smart.Efficiency.Shared.DataCache.Efficiency;
using Xpo.Smart.Efficiency.Shared.Extensions;
using Xpo.Smart.Efficiency.Shared.Models;

namespace Xpo.Smart.Efficiency.EventProcessor.Helpers
{
    public static class EfficiencyCacheHelper
    {
        public static void UpdateLiveEfficiencyCache(
            IEnumerable<EfficiencyRecord> calculatedEfficiencyData,
            IEmployeeEfficiencyCacheRepository efficiencyCacheRepository,
            DateTime start, DateTime end, bool processAllEmployees)
        {
            var hashKeysToDeleteDictionary = new Dictionary<string, string[]>();
            var efficiencyDataToSave = new List<EfficiencySaveModel>();

            foreach (var item in calculatedEfficiencyData.GroupBy(x => x.SiteCode))
            {
                var siteCode = item.Key;
                var engineCalculatedEfficiencyData = item.ToArray();
                var calculatedSiteEfficiencyData = MapEfficiencySaveData(engineCalculatedEfficiencyData);
                var efficiencySaveDropData = GetEfficiencySaveDropData(efficiencyCacheRepository, calculatedSiteEfficiencyData, siteCode, start, end, processAllEmployees);
                hashKeysToDeleteDictionary.Add(siteCode, efficiencySaveDropData.Item1);
                efficiencyDataToSave.AddRange(efficiencySaveDropData.Item2);
            }

            var allKeysToDelete = hashKeysToDeleteDictionary.SelectMany(r => r.Value).ToArray();

            if (efficiencyDataToSave.Any() || allKeysToDelete.Any())
            {
                UpdateCache(efficiencyCacheRepository, efficiencyDataToSave, hashKeysToDeleteDictionary);
            }
        }

        public static (string[], EfficiencySaveModel[]) GetEfficiencySaveDropData(IEmployeeEfficiencyCacheRepository efficiencyCacheRepository,
            IEnumerable<EfficiencySaveModel> calculatedEfficiencyData, string siteCode, DateTime start, DateTime end, bool processAllEmployees)
        {
            calculatedEfficiencyData = calculatedEfficiencyData.Where(x => x.ClockInStartTime < end && x.ClockInEndTime > start); //get calculated data that overlaps the processed interval

            var paddingHours = 12; //use padding to get records that are within 12 hours of calculated data min and max
            List<EfficiencyData> cachedEfficiencyData = new List<EfficiencyData>();
            var groupedCachedEfficiencyData = efficiencyCacheRepository.GetAllEfficiencyData(siteCode)
                .Where(x => x.ClockInStartTime < end && x.ClockInEndTime > start //get cached data that overlaps the processed interval
                    && x.OperationalDay >= start.Date && x.OperationalDay <= end.Date)
                .GroupBy(x => x.EfficiencyEmployeeNumber);

            foreach (var siteEmployeeCachedEfficiencyData in groupedCachedEfficiencyData)
            {
                var siteEmployeeCalculatedEfficiencyData = calculatedEfficiencyData
                    .Where(x => (string.IsNullOrWhiteSpace(x.EmployeeNumber) ? x.SiteEmployeeCode : x.EmployeeNumber).IgnoreCaseEquals(siteEmployeeCachedEfficiencyData.Key));
                if (!siteEmployeeCalculatedEfficiencyData.Any())
                {
                    if (processAllEmployees)
                        cachedEfficiencyData.AddRange(siteEmployeeCachedEfficiencyData
                            .Where(x => x.ClockInStartTime >= start.AddHours(-1 * paddingHours) && x.ClockInEndTime <= end.AddHours(paddingHours)));
                    continue;
                }

                var min = siteEmployeeCalculatedEfficiencyData.Min(x => x.ClockInStartTime).AddHours(-1 * paddingHours);
                var max = siteEmployeeCalculatedEfficiencyData.Max(x => x.ClockInEndTime).AddHours(paddingHours);

                //get cached data for calcualted data interval +-12 hours back and forward to compare against cached data
                cachedEfficiencyData.AddRange(siteEmployeeCachedEfficiencyData.Where(x => x.ClockInStartTime >= min && x.ClockInEndTime <= max));
            }

            var calculatedHashKeys = new HashSet<string>(calculatedEfficiencyData.Select(x => x.HashKey).Distinct());
            var cachedHashKeys = new HashSet<string>(cachedEfficiencyData.Select(x => x.HashKey).Distinct().ToArray());

            var hashKeysToDelete = cachedEfficiencyData.Where(x => !calculatedHashKeys.Contains(x.HashKey)).Select(x => x.HashKey).Distinct().ToArray();
            var efficiencyDataToSave = calculatedEfficiencyData.Where(x => !cachedHashKeys.Contains(x.HashKey)).ToArray();

            return (hashKeysToDelete, efficiencyDataToSave);
        }

        private static void UpdateCache(IEmployeeEfficiencyCacheRepository efficiencyCacheRepository, IEnumerable<EfficiencySaveModel> efficiencyDataToSave,
            Dictionary<string, string[]> hashKeysToDeleteDictionary)
        {
            //add eff data to cache
            efficiencyCacheRepository.AddOrUpdate(efficiencyDataToSave
                .Select(x => new EfficiencyData
                {
                    HashKey = x.HashKey,
                    SiteCode = x.SiteCode,
                    SiteEmployeeCode = x.SiteEmployeeCode,
                    ClockInStartTime = x.ClockInStartTime,
                    ClockInEndTime = x.ClockInEndTime,
                    EmployeeNumber = x.EmployeeNumber,
                    OperationalDay = x.OperationalDay,
                    TransactionTypeCode = x.TransactionTypeCode,
                    EmployeeFullName = x.EmployeeFullName,
                    EmployeeJobCode = x.EmployeeJobCode,
                    SecondsClockedInInTransactionalWorkCenter = x.SecondsClockedInInTransactionalWorkCenter,
                    SecondsClockedIn = x.SecondsClockedIn,
                    SecondsEarned = x.SecondsEarned,
                    QuantityEarned = x.QuantityEarned,
                    WorkCenterCode = x.WorkCenterCode,
                    RecordTypeCode = x.RecordTypeCode,
                    IsClockedIn = x.IsClockedIn,
                    LastTransactionDate = x.LastTransactionDate
                }));

            //drop eff data from cache
            foreach (var item in hashKeysToDeleteDictionary)
            {
                efficiencyCacheRepository.DeleteByHashKeys(item.Key, item.Value);
            }
        }

        private static EfficiencySaveModel[] MapEfficiencySaveData(EfficiencyRecord[] calculatedEfficiencyData)
        {
            return calculatedEfficiencyData.Select(x => new EfficiencySaveModel(x)).ToArray();
        }
    }
}
