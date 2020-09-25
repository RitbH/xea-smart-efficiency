using System;
using System.Collections.Generic;
using System.Linq;
using Xpo.Smart.Efficiency.Shared.DataCache.Core;
using Xpo.Smart.Efficiency.Shared.Models;

namespace Xpo.Smart.Efficiency.Shared.DataCache.Efficiency
{
    internal class EmployeeEfficiencyCache : SiteEmployeeCache<string, CachedEfficiencyItem>, IEmployeeEfficiencyCacheRepository
    {
        private readonly CodeMaps _codeMaps;
        private readonly object _locker = new object();

        public EmployeeEfficiencyCache(CodeMaps codeMaps)
        {
            _codeMaps = codeMaps;
        }

        public void AddOrUpdate(IEnumerable<EfficiencyData> employeeEfficiencyData)
        {
            if (employeeEfficiencyData == null)
            {
                return;
            }

            foreach (var grouping in employeeEfficiencyData.GroupBy(t => new { t.SiteCode, t.SiteEmployeeCode }))
            {
                lock (_locker)
                {
                    AddOrUpdate(grouping.Key.SiteCode, grouping.Key.SiteEmployeeCode, grouping.Select(Map));
                }
            }
        }

        public void Prune(DateTime endDate)
        {
            var lowerValue = new CachedEfficiencyItem { ClockInStartTime = DateTime.MinValue, EfficiencyId = 0 };
            var upperValue = new CachedEfficiencyItem { ClockInStartTime = endDate, EfficiencyId = 0 };

            lock (_locker)
            {
                base.Prune(lowerValue, upperValue);
            }
        }

        public void DeleteByHashKeys(string siteCode, IEnumerable<string> hashKeys)
        {
            if (hashKeys == null)
            {
                return;
            }

            var haystack = new HashSet<string>(hashKeys);

            if (!haystack.Any())
            {
                return;
            }

            lock (_locker)
            {
                foreach (var siteEmployeeCode in GetSiteEmployeeCodes(siteCode))
                {
                    var deleteKeys = new List<string>();
                    var siteEmployee = GetEmployeeCache(siteCode, siteEmployeeCode);

                    foreach (var item in siteEmployee.GetAll())
                    {
                        if (haystack.Contains(item.HashKey))
                        {
                            deleteKeys.Add(item.HashKey);
                        }
                    }

                    if (deleteKeys.Any())
                    {
                        siteEmployee.Delete(deleteKeys);
                    }
                }
            }
        }

        public IEnumerable<EfficiencyData> GetAllEfficiencyData(string siteCode)
        {
            lock (_locker)
            {
                return base.GetAll(siteCode).Select(Map).ToArray();
            }
        }

        public IEnumerable<string> GetAllSiteCodes()
        {
            lock (_locker)
            {
                return base.GetSiteCodes().ToArray();
            }
        }

        private EfficiencyData Map(SiteEmployeeCacheItem item)
        {
            return new EfficiencyData
            {
                HashKey = item.Item.HashKey,
                SiteCode = item.SiteCode,
                SiteEmployeeCode = item.SiteEmployeeCode,
                EfficiencyId = item.Item.EfficiencyId,
                ClockInEndTime = item.Item.ClockInEndTime,
                ClockInStartTime = item.Item.ClockInStartTime,
                EmployeeNumber = _codeMaps.EmployeeNumberCodeMap.TryGetIntCode(item.Item.EmployeeNumberId),
                OperationalDay = item.Item.OperationalDay,
                TransactionTypeCode = _codeMaps.TransactionTypeCodeMap.TryGetCode(item.Item.TransactionTypeId),
                EmployeeFullName = _codeMaps.PersonNameCodeMap.TryGetIntCode(item.Item.EmployeeFullNameId),
                EmployeeJobCode = _codeMaps.JobCodeCodeMap.TryGetCode(item.Item.EmployeeJobCodeId),
                SecondsClockedInInTransactionalWorkCenter = item.Item.SecondsClockedInInTransactionalWorkCenter,
                SecondsClockedIn = item.Item.SecondsClockedIn,
                SecondsEarned = item.Item.SecondsEarned,
                QuantityEarned = item.Item.QuantityEarned,
                WorkCenterCode = _codeMaps.WorkCenterCodeMap.TryGetIntCode(item.Item.WorkCenterCodeId),
                RecordTypeCode = _codeMaps.RecordTypeCodeMap.TryGetCode(item.Item.RecordTypeCodeId),
                IsClockedIn = item.Item.IsClockedIn,
                LastTransactionDate = item.Item.LastTransactionDate,
                Supervisor = _codeMaps.PersonNameCodeMap.TryGetIntCode(item.Item.SupervisorId),
                ShiftCode = _codeMaps.ShiftCodeMap.TryGetIntCode(item.Item.ShiftCodeId)
            };
        }

        private CachedEfficiencyItem Map(EfficiencyData item)
        {
            var employeeNumberId = _codeMaps.EmployeeNumberCodeMap.GetOrAddIntCode(item.EmployeeNumber);
            var transactionTypeId = _codeMaps.TransactionTypeCodeMap.GetOrAddCode(item.TransactionTypeCode);
            var employeeFullNameId = _codeMaps.PersonNameCodeMap.GetOrAddIntCode(item.EmployeeFullName);
            var employeeJobCodeId = _codeMaps.JobCodeCodeMap.GetOrAddCode(item.EmployeeJobCode);
            var workCenterCodeId = _codeMaps.WorkCenterCodeMap.GetOrAddIntCode(item.WorkCenterCode);
            var recordTypeCodeId = _codeMaps.RecordTypeCodeMap.GetOrAddCode(item.RecordTypeCode);
            var supervisorId = _codeMaps.PersonNameCodeMap.GetOrAddIntCode(item.Supervisor);
            var shiftCodeId = _codeMaps.ShiftCodeMap.GetOrAddIntCode(item.ShiftCode);

            return new CachedEfficiencyItem
            {
                HashKey = item.HashKey,
                ClockInEndTime = item.ClockInEndTime,
                ClockInStartTime = item.ClockInStartTime,
                EfficiencyId = item.EfficiencyId,
                EmployeeNumberId = employeeNumberId,
                OperationalDay = item.OperationalDay,
                TransactionTypeId = transactionTypeId,
                EmployeeFullNameId = employeeFullNameId,
                EmployeeJobCodeId = employeeJobCodeId,
                SecondsClockedInInTransactionalWorkCenter = item.SecondsClockedInInTransactionalWorkCenter,
                SecondsClockedIn = item.SecondsClockedIn,
                SecondsEarned = item.SecondsEarned,
                QuantityEarned = item.QuantityEarned,
                WorkCenterCodeId = workCenterCodeId,
                RecordTypeCodeId = recordTypeCodeId,
                IsClockedIn = item.IsClockedIn,
                LastTransactionDate = item.LastTransactionDate,
                SupervisorId = supervisorId,
                ShiftCodeId = shiftCodeId
            };
        }
    }
}
