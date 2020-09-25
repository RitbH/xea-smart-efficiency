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

namespace Xpo.Smart.Efficiency.EventProcessor.Events
{
    public class TransactionEventHandler : IEventHandler<TransactionIngestionEvent>
    {
        private readonly IInitEfficiencyRunner _initEfficiencyRunner;
        private readonly ICacheRepositoryFactory<ITransactionCacheRepository> _transactionCacheRepositoryFactory;
        private readonly ICacheRepositoryFactory<IEmployeeEfficiencyCacheRepository> _efficiencyCacheRepositoryFactory;
        private readonly IDataCacheProvider<EfficiencyTimesheetCache> _timesheetCacheProvider;
        private readonly ILiveEfficiencyEngine _liveEfficiencyEngine;

        protected TransactionEventHandler(IInitEfficiencyRunner initEfficiencyRunner, ICacheRepositoryFactory<ITransactionCacheRepository> transactionCacheRepositoryFactory,
            ICacheRepositoryFactory<IEmployeeEfficiencyCacheRepository> efficiencyCacheRepositoryFactory, IDataCacheProvider<EfficiencyTimesheetCache> timesheetCacheProvider,
            ILiveEfficiencyEngine liveEfficiencyEngine)
        {
            _initEfficiencyRunner = initEfficiencyRunner;
            _transactionCacheRepositoryFactory = transactionCacheRepositoryFactory;
            _efficiencyCacheRepositoryFactory = efficiencyCacheRepositoryFactory;
            _timesheetCacheProvider = timesheetCacheProvider;
            _liveEfficiencyEngine = liveEfficiencyEngine;
        }

        public void Handle(TransactionIngestionEvent @event)
        {
            var transactionsToProcess = GetTransactions(@event);

            UpdateTransactionCache(transactionsToProcess);

            UpdateEfficiencyCache(transactionsToProcess);
        }

        private void UpdateTransactionCache(IEnumerable<Shared.Models.Transaction> transactions)
        {
            var transactionCache = _transactionCacheRepositoryFactory.GetCurrent();
            transactionCache.AddOrUpdate(transactions);
        }

        private IEnumerable<Shared.Models.Transaction> GetTransactions(TransactionIngestionEvent @event)
        {
            var eventTransaction = @event.Value;
            return new List<Shared.Models.Transaction>(){ new Shared.Models.Transaction
            {
                SiteCode = eventTransaction.SiteCode,
                SiteEmployeeCode = eventTransaction.EmployeeCode,
                TransactionDate = eventTransaction.TransactionDateTime,
                //TransactionId = eventTransaction.TransactionCode, // transactionId needs to be added to event
                SecondsEarned = eventTransaction.SecondsEarned,
                Quantity = eventTransaction.Quantity,
                QuantityEarned = eventTransaction.QuantityEarned,
                QuantityProcessed = eventTransaction.QuantityProcessed,
                TransactionTypeCode = eventTransaction.TransactionTypeCode,
                OperationalDate = eventTransaction.OperationalDate.GetValueOrDefault(eventTransaction.TransactionDateTime),
                SegmentCode = eventTransaction.SegmentCode
            }};
        }

        private void UpdateEfficiencyCache(IEnumerable<Shared.Models.Transaction> transactions)
        {
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
                var siteEmployeeCodesBySiteCodes = transactions
                    .Where(r => !string.IsNullOrWhiteSpace(r.SiteCode)
                               && !string.IsNullOrWhiteSpace(r.SiteEmployeeCode))
                    .GroupBy(r => new { r.SiteCode, r.SiteEmployeeCode })
                    .Select(r => new SiteEmployeeCode { SiteCode = r.Key.SiteCode, EmployeeCode = r.Key.SiteEmployeeCode })
                    .GroupBy(r => r.SiteCode)
                    .ToDictionary(r => r.Key, r => r.ToArray());

                if (!siteEmployeeCodesBySiteCodes.Any())
                {
                    //historical ingestion events will be ignored
                    return;
                }

                _liveEfficiencyEngine.ComputeForSiteEmployeeCodesAndExecute(siteEmployeeCodesBySiteCodes,
                    start, end, (calculatedEfficiency) => EfficiencyCacheHelper.UpdateLiveEfficiencyCache(calculatedEfficiency, liveEfficiencyCache, start, end, false));
            }
        }
    }
}
