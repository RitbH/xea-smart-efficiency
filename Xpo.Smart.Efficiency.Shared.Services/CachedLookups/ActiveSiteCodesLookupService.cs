using Xpo.Smart.Core.Caching;
using Xpo.Smart.Core.Services;

namespace Xpo.Smart.Efficiency.Shared.Services.CachedLookups
{
    [Cache(3600)]
    public interface IActiveSiteCodesLookupService : ISmartService
    {
        string[] Load();
    }

    public class ActiveSiteCodesLookupService : IActiveSiteCodesLookupService
    {
        public string[] Load()
        {
            return new string[0];
        }
    }
}
