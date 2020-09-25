using System;
using System.Collections.Generic;
using System.Linq;
using Xpo.Smart.Core.Services;
using Xpo.Smart.Efficiency.Shared.DataCache.Core;
using Xpo.Smart.Efficiency.Shared.Extensions;
using Xpo.Smart.Efficiency.Shared.Models;

namespace Xpo.Smart.Efficiency.Shared.DataCache.Transactions
{
    internal class TransactionCache : SiteEmployeeCache<long, CachedTransaction>, ITransactionCacheRepository
    {
        private readonly object _locker = new object();
        private readonly ISiteService _siteService;
        private readonly CodeMaps _codeMaps;

        public TransactionCache(CodeMaps codeMaps, ISiteService siteService)
        {
            _codeMaps = codeMaps;
            _siteService = siteService;
        }

        public void AddOrUpdate(IEnumerable<Transaction> transactions)
        {
            foreach (var grouping in transactions.GroupBy(t => new {t.SiteCode, t.SiteEmployeeCode}))
            {
                lock (_locker)
                {
                    AddOrUpdate(grouping.Key.SiteCode, grouping.Key.SiteEmployeeCode, grouping.Select(Map));
                }
            }
        }

        public IEnumerable<string> GetSiteCodes()
        {
            lock (_locker)
            {
                var activeSiteCodes = _siteService.SearchSites(null, null).Where(x => x.IsActive).Select(x => x.Code).ToArray();
                return base.GetSiteCodes().Where(x => x.ExistsInArray(activeSiteCodes)).ToArray();
            }
        }

        public IEnumerable<Transaction> GetTransactionsByDateRange(string siteCode, IEnumerable<string> siteEmployeeCodes = null, DateTime? start = null, DateTime? end = null)
        {
            var lowerValue = new CachedTransaction
                { TransactionDate = start.GetValueOrDefault(DateTime.MinValue), TransactionId = long.MinValue };

            var upperValue = new CachedTransaction
                { TransactionDate = end.GetValueOrDefault(DateTime.MaxValue), TransactionId = long.MaxValue };

            lock (_locker)
            {
                var views = GetViewsBetween(siteCode, siteEmployeeCodes, lowerValue, upperValue, true);

                // If we only have one employee we filtered on there's no need to sort by anything, the data is already sorted
                if (siteEmployeeCodes.Count() > 1)
                    views = views.OrderBy(e => e.Item.TransactionDate).ThenBy(e => e.Item.TransactionId);

                return views.SelectWithPrevious(Map).Where(t => t.TransactionDate >= lowerValue.TransactionDate)
                    .ToArray();
            }
        }

        public IEnumerable<string> GetTransactionSiteEmployeeCodes(string siteCode)
        {
            lock (_locker)
            {
                return GetSiteEmployeeCodes(siteCode).ToArray();
            }
        }

        public void Prune(DateTime endDate)
        {
            var lowerValue = new CachedTransaction {TransactionDate = DateTime.MinValue, TransactionId = 0};
            var upperValue = new CachedTransaction {TransactionDate = endDate, TransactionId = 0};

            lock (_locker)
            {
                base.Prune(lowerValue, upperValue);
            }
        }

        private Transaction Map(SiteEmployeeCacheItem previous, SiteEmployeeCacheItem current)
        {
            return new Transaction
            {
                TransactionId = current.Item.TransactionId,
                SiteCode = current.SiteCode,
                SiteEmployeeCode = current.SiteEmployeeCode,
                OperationalDate = current.Item.OperationalDate,
                TransactionDate = current.Item.TransactionDate,
                Quantity = (decimal?) current.Item.Quantity,
                QuantityEarned = (decimal?) current.Item.QuantityEarned,
                QuantityProcessed = (decimal?) current.Item.QuantityProcessed,
                SecondsEarned = current.Item.SecondsEarned,
                SegmentCode = _codeMaps.SegmentTypeCodeMap.TryGetCode(current.Item.SegmentId),
                TransactionTypeCode = _codeMaps.TransactionTypeCodeMap.TryGetCode(current.Item.TransactionTypeId),
                TransitionTimeSeconds = previous == null
                    ? 0
                    : (int) (current.Item.TransactionDate - previous.Item.TransactionDate).TotalSeconds,
                PreviousTransactionTypeCode = previous == null
                    ? null
                    : _codeMaps.TransactionTypeCodeMap.TryGetCode(previous.Item.TransactionTypeId),
                PreviousTransactionId = previous?.Item.TransactionId
            };
        }

        private CachedTransaction Map(Transaction transaction)
        {
            var segmentId = _codeMaps.SegmentTypeCodeMap.GetOrAddCode(transaction.SegmentCode);
            var transactionTypeId = _codeMaps.TransactionTypeCodeMap.GetOrAddCode(transaction.TransactionTypeCode);

            return new CachedTransaction
            {
                TransactionId = transaction.TransactionId,
                SecondsEarned = transaction.SecondsEarned,
                Quantity = transaction.Quantity == null ? (float?) null : (float) transaction.Quantity,
                QuantityEarned = transaction.QuantityEarned == null ? (float?) null : (float) transaction.QuantityEarned,
                QuantityProcessed = transaction.QuantityProcessed == null ? (float?) null : (float) transaction.QuantityProcessed,
                TransactionDate = transaction.TransactionDate,
                TransactionTypeId = transactionTypeId,
                OperationalDate = transaction.OperationalDate,
                SegmentId = segmentId
            };
        }
    }
}
