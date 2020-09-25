using System.Collections.Generic;
using Xpo.Smart.Efficiency.Shared.Models;

namespace Xpo.Smart.Efficiency.Shared.Data.Repository
{
    public interface IEmployeeEfficiencyRepository
    {
        IEnumerable<EfficiencyData> GetAllEfficiencyData(string siteCode);

        IEnumerable<string> GetAllSiteCodes();
    }
}
