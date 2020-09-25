using System;
using System.Collections.Generic;
using Xpo.Smart.Efficiency.Shared.Data.Repository;
using Xpo.Smart.Efficiency.Shared.Models;

namespace Xpo.Smart.Efficiency.Shared.DataCache.Transactions
{
    public interface ITransactionCacheRepository : ITransactionRepository
    {
        void AddOrUpdate(IEnumerable<Transaction> transactions);

        void Prune(DateTime date);
    }
}
