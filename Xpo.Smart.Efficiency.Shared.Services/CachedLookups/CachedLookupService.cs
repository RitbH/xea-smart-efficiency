using System.Collections.Generic;
using Xpo.Smart.Core.Models;

namespace Xpo.Smart.Efficiency.Shared.Services.CachedLookups
{
    public interface ICachedLookupService
    {
        Workcenter[] GetWorkcenters(string siteCode);

        TransactionType[] GetTransactionTypes(string siteCode);

        EmployeeShiftSupevisor[] GetEmployeeShiftSupevisors(string siteCode);
    }

    public class CachedLookupService : ICachedLookupService
    {
        private readonly IWorkcenterCachedLookupService _workcenterLookupService;
        private readonly ITransactionTypeCachedLookupService _transactionTypeLookupService;
        private readonly IShiftSupervisorCachedLookupService _shiftSupervisorLookupService;

        private readonly object _shiftSupervisorLocker = new object();
        private readonly object _transactionTypeLocker = new object();

        public CachedLookupService(
            IWorkcenterCachedLookupService workcenterLookupService,
            ITransactionTypeCachedLookupService transactionTypeLookupService,
            IShiftSupervisorCachedLookupService shiftSupervisorLookupService)
        {
            _workcenterLookupService = workcenterLookupService;
            _transactionTypeLookupService = transactionTypeLookupService;
            _shiftSupervisorLookupService = shiftSupervisorLookupService;
        }
        public EmployeeShiftSupevisor[] GetEmployeeShiftSupevisors(string siteCode)
        {

            IDictionary<string, EmployeeShiftSupevisor[]> data;
            //cached for 1 hour, lock the load
            lock (_shiftSupervisorLocker)
            {
                data = _shiftSupervisorLookupService.Load();
            }
            return data[siteCode];
        }

        public TransactionType[] GetTransactionTypes(string siteCode)
        {
            IDictionary<string, TransactionType[]> data;
            //cached for 1 hour, lock the load
            lock (_transactionTypeLocker)
            {
                data = _transactionTypeLookupService.Load();
            }
            return data[siteCode];
        }

        public Workcenter[] GetWorkcenters(string siteCode)
        {
            IDictionary<string, Workcenter[]> data;
            //cached for 1 hour, lock the load
            lock (_shiftSupervisorLocker)
            {
                data = _workcenterLookupService.Load();
            }
            return data[siteCode];
        }
    }
}
