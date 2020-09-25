using System.Collections.Generic;
using Xpo.Smart.Core.Caching;
using Xpo.Smart.Core.Services;
using Xpo.Smart.Efficiency.Shared.Services.ShiftSupervisor;

namespace Xpo.Smart.Efficiency.Shared.Services.CachedLookups
{
    public interface IShiftSupervisorCachedLookupService : ISmartService
    {
        [Cache(3600)]
        IDictionary<string, Core.Models.EmployeeShiftSupevisor[]> Load();
    }

    public class ShiftSupervisorCachedLookupService : IShiftSupervisorCachedLookupService
    {
        private readonly IShiftSupervisorService _shiftSupervisorService;
        private readonly IActiveSiteCodesLookupService _activeSiteCodesLookupService;

        public ShiftSupervisorCachedLookupService(IActiveSiteCodesLookupService activeSiteCodesLookupService, IShiftSupervisorService shiftSupervisorService)
        {
            _shiftSupervisorService = shiftSupervisorService;
            _activeSiteCodesLookupService = activeSiteCodesLookupService;
        }
        public IDictionary<string, Core.Models.EmployeeShiftSupevisor[]> Load()
        {
            //cached for an hour
            var siteCodes = _activeSiteCodesLookupService.Load();

            var result = new Dictionary<string, Core.Models.EmployeeShiftSupevisor[]>();
            foreach (var siteCode in siteCodes)
            {
                var shiftSupevisors = _shiftSupervisorService.List(siteCode);
                result.Add(siteCode, shiftSupevisors);
            }

            return result;
        }
    }
}
