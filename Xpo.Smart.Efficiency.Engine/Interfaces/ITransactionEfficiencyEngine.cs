using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using Xpo.Smart.Core.Models;
using Xpo.Smart.Efficiency.Shared.Models;
using Transaction = Xpo.Smart.Efficiency.Shared.Models.Transaction;

namespace Xpo.Smart.Efficiency.Engine.Interfaces
{
    public interface ITransactionEfficiencyEngine
    {
        [NotNull]
        IEnumerable<EfficiencyRecord> Compute([NotNull] IEnumerable<Transaction> modifiedTransactions, LaborRate[] laborRates, DateTime minDate, DateTime maxDate);
    }
}
