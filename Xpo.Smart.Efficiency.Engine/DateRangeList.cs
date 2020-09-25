using System;
using System.Collections;
using System.Collections.Generic;
using Xpo.Smart.Core.Extensions;
using Xpo.Smart.Efficiency.Shared.Models;

namespace Xpo.Smart.Efficiency.Engine
{
    public class DateRangeList : IEnumerable<DateRange>
    {
        private readonly TimeSpan _step;
        private readonly bool _nearest;
        private readonly DateTime _startDate;
        private readonly DateTime _endDate;
        private readonly bool _descending;

        public DateRangeList(DateTime startDate, DateTime endDate, TimeSpan step) :
            this(startDate, endDate, step, true)
        {
        }

        public DateRangeList(DateTime startDate, DateTime endDate, TimeSpan step, bool descending) :
            this(startDate, endDate, step, false, descending)
        {
        }

        public DateRangeList(DateTime startDate, DateTime endDate, TimeSpan step, bool nearest, bool descending)
        {
            _step = step;
            _startDate = startDate;
            _endDate = endDate;
            _descending = descending;
            _nearest = nearest;
        }

        /// <summary>
        /// Generates a series of date ranges between a start date and end date in ascending order, using the specified step.
        /// </summary>
        /// <param name="startDate">The start date. If none is specified DateTime.MinValue is used.</param>
        /// <param name="endDate">The end date. If none is specified DateTime.MaxValue is used.</param>
        /// <param name="step">The step for each range in the sequence. If none is specified the step is the time between startDate and endDate (i.e one item will be generated in the sequence between the start and end).</param>
        /// <param name="nearest">When true, the first item in the series will end on the nearest (rounded) step rather than just adding step to the sequence</param>
        /// <returns></returns>
        public static DateRangeList GenerateAscending(DateTime? startDate = null, DateTime? endDate = null,
            TimeSpan? step = null, bool nearest = false)
        {
            var start = startDate.GetValueOrDefault(DateTime.MinValue);
            var end = endDate.GetValueOrDefault(DateTime.MaxValue);
            var interval = step.GetValueOrDefault(end - start);
            return new DateRangeList(start, end, interval, nearest, false);
        }

        public IEnumerator<DateRange> GetEnumerator()
        {
            if (_step <= TimeSpan.Zero)
            {
                throw new InvalidOperationException($"Step of {_step} is not valid");
            }

            var range = _endDate - _startDate;

            if (_descending)
            {
                for (var accumulator = TimeSpan.Zero; accumulator < range;)
                {
                    var step = _step;

                    if (_nearest && accumulator == TimeSpan.Zero)
                    {
                        var delta = _endDate - _endDate.Floor(_step);
                        if (delta != TimeSpan.Zero) step = delta;
                    }

                    yield return new DateRange(Max(_endDate - accumulator - step, _startDate), _endDate - accumulator);

                    accumulator += step;
                }
            }
            else
            {
                for (var accumulator = TimeSpan.Zero; accumulator < range;)
                {
                    var step = _step;

                    if (_nearest && accumulator == TimeSpan.Zero)
                    {
                        var delta = _startDate.Ceiling(_step) - _startDate;
                        if (delta != TimeSpan.Zero) step = delta;
                    }

                    yield return new DateRange(_startDate + accumulator, Min(_startDate + accumulator + step, _endDate));

                    accumulator += step;
                }
            }
        }

        private DateTime Min(DateTime d1, DateTime d2) => d1 > d2 ? d2 : d1;

        private DateTime Max(DateTime d1, DateTime d2) => d1 > d2 ? d1 : d2;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
