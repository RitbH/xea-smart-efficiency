using System.Collections.Generic;
using System.Linq;
using Xpo.Smart.Core.Caching;
using Xpo.Smart.Core.Context;
using Xpo.Smart.Core.Services;
using Xpo.Smart.Efficiency.Shared.Extensions;

namespace Xpo.Smart.Efficiency.Shared.Services.CachedLookups
{
    public interface IWorkcenterCachedLookupService : ISmartService
    {
        [Cache(3600)]
        IDictionary<string, Core.Models.Workcenter[]> Load();
    }

    public class WorkcenterCachedLookupService : IWorkcenterCachedLookupService
    {
        private readonly ISiteContextFactory _siteContextFactory;
        private readonly IActiveSiteCodesLookupService _activeSiteCodesLookupService;

        public WorkcenterCachedLookupService(IActiveSiteCodesLookupService activeSiteCodesLookupService, ISiteContextFactory siteContextFactory)
        {
            _siteContextFactory = siteContextFactory;
            _activeSiteCodesLookupService = activeSiteCodesLookupService;
        }

        public IDictionary<string, Core.Models.Workcenter[]> Load()
        {
            var result = new Dictionary<string, Core.Models.Workcenter[]>();

            //cached for an hour
            var siteCodes = _activeSiteCodesLookupService.Load();
            if (!siteCodes.Any())
                return result;

            var workcenterService = _siteContextFactory.Create(siteCodes[0]).GetWorkcenterService();

            foreach (var siteCode in siteCodes)
            {
                var workCenters = workcenterService.ListIncludeInvalids(siteCode);
                result.Add(siteCode, workCenters);
            }

            return result;
        }
    }
}
