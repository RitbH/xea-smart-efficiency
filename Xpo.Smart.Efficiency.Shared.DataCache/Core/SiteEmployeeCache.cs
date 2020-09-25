using System;
using System.Collections.Generic;
using System.Linq;
using Xpo.Smart.Core.DataCache;

namespace Xpo.Smart.Efficiency.Shared.DataCache.Core
{
    public abstract class SiteEmployeeCache<TItemKey, TCacheItem>
        where TCacheItem : class, IEquatable<TCacheItem>, IComparable<TCacheItem>, IBucketCacheKey<TItemKey>, new()
    {
        private readonly Dictionary<string, Dictionary<string, BucketCache<TItemKey, TCacheItem>>> _siteEmployees =
            new Dictionary<string, Dictionary<string, BucketCache<TItemKey, TCacheItem>>>(StringComparer
                .OrdinalIgnoreCase);

        protected IEnumerable<SiteEmployeeCacheItem> GetAll(string siteCode)
        {
            if (!_siteEmployees.ContainsKey(siteCode))
            {
                yield break;
            }

            var siteEmployees = _siteEmployees[siteCode];

            foreach (var siteEmployeeCode in GetSiteEmployeeCodes(siteCode))
            {
                var siteEmployee = siteEmployees[siteEmployeeCode];
                foreach (var item in siteEmployee.GetAll())
                {
                    yield return new SiteEmployeeCacheItem { SiteCode = siteCode, SiteEmployeeCode = siteEmployeeCode, Item = item };
                }
            }
        }

        protected BucketCache<TItemKey, TCacheItem> GetEmployeeCache(string siteCode, string siteEmployeeCode)
        {
            return _siteEmployees[siteCode][siteEmployeeCode];
        }

        protected void Prune(TCacheItem lowerValue, TCacheItem upperValue)
        {
            foreach (var site in _siteEmployees)
            {
                foreach (var siteEmployee in site.Value)
                {
                    siteEmployee.Value.Prune(lowerValue, upperValue);
                }
            }
        }

        protected IEnumerable<string> GetSiteEmployeeCodes(string siteCode)
        {
            if (!_siteEmployees.ContainsKey(siteCode))
            {
                return new string[0];
            }

            var siteEmployeeDictionary = _siteEmployees[siteCode];
            return siteEmployeeDictionary.Keys.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        protected IEnumerable<SiteEmployeeCacheItem> GetViewsBetween(string siteCode, IEnumerable<string> siteEmployeeCodes = null,
            TCacheItem lowerValue = null, TCacheItem upperValue = null, bool includePreviousValue = false)
        {
            if (!_siteEmployees.ContainsKey(siteCode))
            {
                yield break;
            }

            var siteEmployees = _siteEmployees[siteCode];

            IEnumerable<KeyValuePair<string, BucketCache<TItemKey, TCacheItem>>> siteEmployeeItems;

            if (siteEmployeeCodes == null)
            {
                siteEmployeeItems = siteEmployees.Select(x => x);
            }
            else
            {
                var siteEmployeeItemList = new List<KeyValuePair<string, BucketCache<TItemKey, TCacheItem>>>();

                foreach (var siteEmployeeCode in siteEmployeeCodes.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!siteEmployees.ContainsKey(siteEmployeeCode))
                    {
                        continue;
                    }

                    var item = new KeyValuePair<string, BucketCache<TItemKey, TCacheItem>>(siteEmployeeCode,
                        siteEmployees[siteEmployeeCode]);

                    siteEmployeeItemList.Add(item);
                }

                siteEmployeeItems = siteEmployeeItemList;
            }

            foreach (var siteEmployee in siteEmployeeItems)
            {
                var items = siteEmployee.Value.GetViewsBetween(lowerValue, upperValue, includePreviousValue);

                foreach (var item in items)
                {
                    yield return new SiteEmployeeCacheItem
                    { SiteCode = siteCode, SiteEmployeeCode = siteEmployee.Key, Item = item };
                }
            }
        }

        protected IEnumerable<string> GetSiteCodes()
        {
            return _siteEmployees.Keys;
        }

        protected void AddOrUpdate(string siteCode, string siteEmployeeCode, IEnumerable<TCacheItem> items)
        {
            if (string.IsNullOrWhiteSpace(siteCode) || string.IsNullOrWhiteSpace(siteEmployeeCode))
            {
                return;
            }

            if (!_siteEmployees.TryGetValue(siteCode, out var employees))
            {
                employees = new Dictionary<string, BucketCache<TItemKey, TCacheItem>>(StringComparer.OrdinalIgnoreCase);
                _siteEmployees.Add(siteCode, employees);
            }

            if (!employees.TryGetValue(siteEmployeeCode, out var employeeItems))
            {
                employeeItems = new BucketCache<TItemKey, TCacheItem>();
                employees.Add(siteEmployeeCode, employeeItems);
            }

            employeeItems.AddOrUpdate(items);
        }

        protected class SiteEmployeeCacheItem
        {
            public string SiteCode { get; set; }

            public string SiteEmployeeCode { get; set; }

            public TCacheItem Item { get; set; }
        }
    }
}
