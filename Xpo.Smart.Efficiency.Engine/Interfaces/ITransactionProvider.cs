using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using Xpo.Smart.Efficiency.Shared.Models;

namespace Xpo.Smart.Efficiency.Engine.Interfaces
{
    [Obsolete("to move out of Engine assembly")]
    public interface ITransactionProvider
    {
        [NotNull]
        IEnumerable<Transaction> FindTransactions([NotNull] string siteCode, [NotNull] IEnumerable<string> siteEmployeeCodes, DateTime startTime, DateTime endTime);
    }
}
