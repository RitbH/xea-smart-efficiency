using System;
using System.Collections.Generic;
using Xpo.Smart.Efficiency.Engine.Models;
using Xpo.Smart.Efficiency.Shared.Models;

namespace Xpo.Smart.Efficiency.Engine.Interfaces
{
    public interface ILiveEfficiencyEngine
    {                
        void ComputeForSiteCodesAndExecute(
            IDictionary<string, SiteEmployeeCode[]> siteEmployeeCodesBySiteCodes,
            IDictionary<string, SiteEmployeeCode[]> tnaEmployeeCodesBySiteCodes,
            DateTime minDate, DateTime maxDate, Action<IEnumerable<EfficiencyRecord>> execute);
        
        void ComputeForSiteEmployeeCodesAndExecute(
            IDictionary<string, SiteEmployeeCode[]> siteEmployeeCodesBySiteCodes,            
            DateTime minDate, DateTime maxDate, Action<IEnumerable<EfficiencyRecord>> execute);

        void ComputeForTnaEmployeeCodesAndExecute(            
            IDictionary<string, SiteEmployeeCode[]> tnaEmployeeCodesBySiteCodes,
            DateTime minDate, DateTime maxDate, Action<IEnumerable<EfficiencyRecord>> execute);
    }
}
