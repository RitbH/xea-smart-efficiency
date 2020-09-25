using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using Xpo.Smart.Efficiency.Shared.Models;

namespace Xpo.Smart.Efficiency.Engine.Interfaces
{
    public interface IDateRangeShiftProvider
    {
        /// <summary>
        /// Provides a set of shifts for a site that overlap a given date range.
        /// </summary>
        /// <param name="siteCode"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [NotNull]
        IEnumerable<EfficiencyShift> GetShifts([NotNull] string siteCode, DateTime startTime, DateTime endTime, bool exactTimeWindow = false,
            IEnumerable<string> siteEmployeeCodes = null);
    }
}
