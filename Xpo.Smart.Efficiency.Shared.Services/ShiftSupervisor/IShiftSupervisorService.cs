using Xpo.Smart.Core.Caching;
using Xpo.Smart.Core.Services;

namespace Xpo.Smart.Efficiency.Shared.Services.ShiftSupervisor
{
    public interface IShiftSupervisorService : ISmartService
    {
        [Cache(1800)]
        Core.Models.Employee[] GetSiteShiftSupervisors(string siteCode);

        [Cache(1800)]
        Core.Models.EmployeeShiftSupevisor[] List(string siteCode);
    }
}
