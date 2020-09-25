namespace Xpo.Smart.Efficiency.Shared.Services.ShiftSupervisor
{
    public class ShiftSupervisorService : IShiftSupervisorService
    {
        public ShiftSupervisorService()
        {
        }

        public Core.Models.Employee[] GetSiteShiftSupervisors(string siteCode)
        {
            return new Core.Models.Employee[0];
        }

        public Core.Models.EmployeeShiftSupevisor[] List(string siteCode)
        {
            return new Core.Models.EmployeeShiftSupevisor[0];
        }
    }
}
