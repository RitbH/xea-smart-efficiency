using System;
using System.Collections.Generic;
using Xpo.Smart.Efficiency.Shared.Models;

namespace Xpo.Smart.Efficiency.Shared.Data.Repository
{
    public interface ITransactionRepository
    {
        IEnumerable<string> GetTransactionSiteEmployeeCodes(string siteCode);

        IEnumerable<Transaction> GetTransactionsByDateRange(string siteCode, IEnumerable<string> siteEmployeeCodes = null,
            DateTime? start = null, DateTime? end = null);

        IEnumerable<string> GetSiteCodes();
    }
}
