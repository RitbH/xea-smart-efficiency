using System;
using System.Collections.Generic;
using Xpo.Smart.Efficiency.Shared.Models;

namespace Xpo.Smart.Efficiency.Engine.Interfaces
{
    public interface IEfficiencyShiftProvider
    {
        IEnumerable<EfficiencyShift> GetShiftsForSiteEmployeeCodes(string siteCode, DateTime siteNow, IEnumerable<string> siteEmployeeCodes, DateTime startTime, DateTime endTime);

        IEnumerable<EfficiencyShift> GetShiftsForTnaEmployeeCodes(string siteCode, DateTime siteNow, IEnumerable<string> tnaEmployeeCodes, DateTime startTime, DateTime endTime);
    }
}
