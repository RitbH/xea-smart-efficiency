using Xpo.Smart.Core.Models;
using Xpo.Smart.Core.Models.TimeSheet;

namespace Xpo.Smart.Efficiency.Shared.Models
{
    public class EfficiencyTimeSheet : TimeSheet
    {
        public long TimeSheetId { get; set; }

        public WorkcenterType? WorkcenterType { get; set; }
    }
}
