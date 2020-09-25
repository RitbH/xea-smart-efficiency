using System;
using System.Collections.Generic;
using System.Linq;
using Xpo.Smart.Core.Models;
using Xpo.Smart.Core.Models.TimeSheet;
using Xpo.Smart.Efficiency.Engine.Interfaces;
using Xpo.Smart.Efficiency.Shared.Models;
using Transaction = Xpo.Smart.Efficiency.Shared.Models.Transaction;

namespace Xpo.Smart.Efficiency.Engine
{
    public class CompositeEfficiencyEngine : ITransactionEfficiencyEngine, ITimeSheetEfficiencyEngine
    {
        private readonly EfficiencyEngine _efficiencyEngine;
        private readonly IDateRangeShiftProvider _dateRangeTimeSheetShiftProvider;
        
        public CompositeEfficiencyEngine(IDateRangeShiftProvider dateRangeTimeSheetShiftProvider, ITransactionProvider transactionProvider)
        {
            _efficiencyEngine = new EfficiencyEngine(transactionProvider);
            _dateRangeTimeSheetShiftProvider = dateRangeTimeSheetShiftProvider;            
        }

        public IEnumerable<EfficiencyRecord> Compute(IEnumerable<Transaction> modifiedTransactions, LaborRate[] laborRates, DateTime minDate, DateTime maxDate)
        {
            return modifiedTransactions.GroupBy(x => x.SiteCode)
                .SelectMany(x => _efficiencyEngine.Compute(_dateRangeTimeSheetShiftProvider.GetShifts(x.Key, minDate, maxDate), laborRates, minDate, maxDate));
        }

        public IEnumerable<EfficiencyRecord> Compute(IEnumerable<TimeSheet> modifiedTimeSheets, LaborRate[] laborRates, DateTime minDate, DateTime maxDate)
        {
            return modifiedTimeSheets.GroupBy(x => x.SiteCode)
                .SelectMany(x => _efficiencyEngine.Compute(_dateRangeTimeSheetShiftProvider.GetShifts(x.Key, minDate, maxDate), laborRates, minDate, maxDate));
        }

        public IEnumerable<EfficiencyRecord> Compute(string siteCode, DateTime minDate, DateTime maxDate, LaborRate[] laborRates, string[] filteredSiteEmployeeCodes = null)
        {
            return _efficiencyEngine.Compute(_dateRangeTimeSheetShiftProvider.GetShifts(siteCode, minDate, maxDate, false, filteredSiteEmployeeCodes), laborRates, minDate, maxDate);
        }
    }
}
