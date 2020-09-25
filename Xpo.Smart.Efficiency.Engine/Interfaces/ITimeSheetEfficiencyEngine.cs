using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using Xpo.Smart.Core.Models;
using Xpo.Smart.Core.Models.TimeSheet;
using Xpo.Smart.Efficiency.Shared.Models;

namespace Xpo.Smart.Efficiency.Engine.Interfaces
{
    public interface ITimeSheetEfficiencyEngine
    {
        [NotNull]
        IEnumerable<EfficiencyRecord> Compute([NotNull] IEnumerable<TimeSheet> modifiedTimeSheets, LaborRate[] laborRates, DateTime minDate, DateTime maxDate);
    }
}
