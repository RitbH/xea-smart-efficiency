using System;
using System.Collections.Generic;
using Xpo.Smart.Efficiency.Shared.Data.Repository;
using Xpo.Smart.Efficiency.Shared.Models;

namespace Xpo.Smart.Efficiency.Shared.DataCache.Efficiency
{
    public interface IEmployeeEfficiencyCacheRepository : IEmployeeEfficiencyRepository
    {
        void AddOrUpdate(IEnumerable<EfficiencyData> employeeEfficiencyData);

        void Prune(DateTime endDate);

        void DeleteByHashKeys(string siteCode, IEnumerable<string> hashKeys);
    }
}
