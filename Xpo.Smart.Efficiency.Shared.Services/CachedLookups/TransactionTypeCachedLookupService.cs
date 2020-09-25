using System.Collections.Generic;
using System.Linq;
using Xpo.Smart.Core.Caching;
using Xpo.Smart.Core.Services;

namespace Xpo.Smart.Efficiency.Shared.Services.CachedLookups
{
    public interface ITransactionTypeCachedLookupService : ISmartService
    {
        [Cache(3600)]
        IDictionary<string, Core.Models.TransactionType[]> Load();
    }

    public class TransactionTypeCachedLookupService : ITransactionTypeCachedLookupService
    {
        private readonly ITransactionTypeService _transactionTypeService;
        private readonly IActiveSiteCodesLookupService _activeSiteCodesLookupService;

        public TransactionTypeCachedLookupService(IActiveSiteCodesLookupService activeSiteCodesLookupService, ITransactionTypeService transactionTypeService)
        {
            _transactionTypeService = transactionTypeService;
            _activeSiteCodesLookupService = activeSiteCodesLookupService;
        }

        public IDictionary<string, Core.Models.TransactionType[]> Load()
        {
            //cached for an hour
            var siteCodes = _activeSiteCodesLookupService.Load();

            var result = new Dictionary<string, Core.Models.TransactionType[]>();
            foreach (var siteCode in siteCodes)
            {
                var transactionTypes = _transactionTypeService.List(siteCode, null);
                result.Add(siteCode, transactionTypes.ToArray());
            }

            return result;
        }
    }
}
