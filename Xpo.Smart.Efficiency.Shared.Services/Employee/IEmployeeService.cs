using Xpo.Smart.Core.Caching;
using Xpo.Smart.Core.Services;

namespace Xpo.Smart.Efficiency.Shared.Services.Employee
{
    public interface IEmployeeService : ISmartService
    {
        [Cache(1800)]
        Core.Models.Employee[] GetEmployees(string siteCode);
    }
}
