using System;
using System.Collections.Generic;
using Xpo.Smart.Efficiency.Engine.Interfaces;
using Xpo.Smart.Efficiency.Shared.DataCache.Core;
using Xpo.Smart.Efficiency.Shared.DataCache.Transactions;

namespace Xpo.Smart.Efficiency.Engine.Providers.Transactions
{
    public class TransactionCacheTransactionProvider : ITransactionProvider
    {
        private readonly ICacheRepositoryFactory<ITransactionCacheRepository> _transactionCacheFactory;

        public TransactionCacheTransactionProvider(
            ICacheRepositoryFactory<ITransactionCacheRepository> transactionCacheFactory)
        {
            _transactionCacheFactory = transactionCacheFactory;
        }

        public IEnumerable<Shared.Models.Transaction> FindTransactions(string siteCode, IEnumerable<string> siteEmployeeCodes, DateTime startTime, DateTime endTime)
        {
            var cache = _transactionCacheFactory.GetCurrent();
            return cache.GetTransactionsByDateRange(siteCode, siteEmployeeCodes, startTime, endTime);
        }
    }
}
