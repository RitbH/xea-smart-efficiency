using System;

namespace Xpo.Smart.Efficiency.Shared.Models
{
    public struct DateRange
    {
        public DateRange(DateTime startDate, DateTime endDate)
        {
            Start = startDate;
            End = endDate;
        }

        public DateTime Start { get; }

        public DateTime End { get; }
    }
}
