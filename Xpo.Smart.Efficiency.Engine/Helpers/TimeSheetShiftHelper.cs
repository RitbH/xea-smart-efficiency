using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Xpo.Smart.Core.Extensions;
using Xpo.Smart.Core.Models;
using Xpo.Smart.Efficiency.Shared.Extensions;
using Xpo.Smart.Efficiency.Shared.Models;
using Employee = Xpo.Smart.Core.Models.Employee;
using Transaction = Xpo.Smart.Efficiency.Shared.Models.Transaction;

namespace Xpo.Smart.Efficiency.Engine.Helpers
{
    internal static class TimeSheetShiftHelpers
    {
        /// <summary>
        /// Produces a list of shifts that are 1:1 with a time sheet
        /// </summary>
        /// <param name="timeSheets"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [NotNull]
        internal static IEnumerable<EfficiencyShift> MapTimeSheetShifts([NotNull] this IEnumerable<EfficiencyTimeSheet> timeSheets, string siteCode, DateTime siteNow,
            [NotNull] Employee[] employees, [CanBeNull] EmployeeShiftSupevisor[] employeeShiftSupevisors, BusinessUnitType businessUnitType,
            IEnumerable<string> filteredSiteEmployeeCodes = null, EfficiencyTransactionType[] transactionTypes =null)
        {
            foreach (var timeSheet in timeSheets.Where(x => x.PunchInTime < siteNow))
            {
                var employee = employees.FirstOrDefault(x => x.TnaEmployeeCode.IgnoreCaseEquals(timeSheet.TnaEmployeeCode));
                
                
                var employeeShiftSupevisor = employeeShiftSupevisors?.FirstOrDefault(x => x.Employee.TnaEmployeeCode.IgnoreCaseEquals(timeSheet.TnaEmployeeCode));
                
                var siteEmployeeCodes = employee?.SiteEmployees?.Where(x => x.SiteCode.IgnoreCaseEquals(siteCode))?.Select(x => x.SiteEmployeeCode)
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? new string[0];
                if (siteEmployeeCodes.IsNotNullOrEmptyElements()
                    && (filteredSiteEmployeeCodes == null || siteEmployeeCodes.Any(x => x.ExistsInArray(filteredSiteEmployeeCodes.ToArray()))))
                {
                    yield return new EfficiencyShift(employee, timeSheet, siteNow, siteEmployeeCodes, employeeShiftSupevisor, transactionTypes, businessUnitType);                    
                }
            }
        }

        /// <summary>
        /// Given a set of transactions and a set of time sheets we will generate a set of shifts
        /// for all transactions that fall outside of the time sheets that are known.
        /// </summary>
        /// <param name="shiftFactory"></param>
        /// <param name="timeSheets"></param>
        /// <param name="transactions"></param>
        /// <param name="startPadding"></param>
        /// <param name="endPadding"></param>
        /// <returns></returns>
        [NotNull]
        internal static IEnumerable<EfficiencyShift> GetOrphanShifts([NotNull] Func<EfficiencyShift> shiftFactory,
            [NotNull] IEnumerable<EfficiencyTimeSheet> timeSheets, [NotNull] IEnumerable<Transaction> transactions,
            TimeSpan? startPadding = null, TimeSpan? endPadding = null)
        {
            var timeSheetEnumerator = new SafeEnumerator<EfficiencyTimeSheet>(timeSheets.GetEnumerator());
            timeSheetEnumerator.MoveNext();

            // Track the current time sheet globally so we can
            // detect when it changes.
            EfficiencyTimeSheet currentTimeSheet = null;
            var shift = (EfficiencyShift)null;
            var lastTransaction = (Transaction)null;

            var startPad = startPadding.GetValueOrDefault(TimeSpan.FromMinutes(30));
            var endPad = endPadding.GetValueOrDefault(TimeSpan.FromMinutes(30));

            foreach (var transaction in transactions)
            {
                if (transaction.TransactionDate < lastTransaction?.TransactionDate)
                {
                    throw new ArgumentException(
                        $"Transactions must be ordered by {nameof(Transaction.TransactionDate)}",
                        nameof(transactions));
                }

                var advanceResult = AdvanceTimeSheet(timeSheetEnumerator, transaction);
                var previousTimeSheet = advanceResult.PreviousTimeSheet;
                var nextTimeSheet = advanceResult.TimeSheet;

                // If the time sheet changed it means we moved past its end date and are either:
                //   1) Within a new time sheet
                //   2) In a new gap
                // Either way we want to close off the old shift if we had one.
                // If it didn't change but we are now past the start time of the current
                // time sheet also close off the shift.
                var changed = nextTimeSheet != currentTimeSheet;
                var overlaps = nextTimeSheet?.PunchInTime <= transaction.TransactionDate;
                var closeShift = changed || transaction.TransactionDate - lastTransaction?.TransactionDate > TimeSpan.FromHours(8) || overlaps;
                currentTimeSheet = nextTimeSheet;

                if (shift != null && closeShift)
                {
                    shift.EndTime = lastTransaction.TransactionDate.Ceiling(endPad);

                    if (shift.EndTime > currentTimeSheet?.PunchInTime)
                        shift.EndTime = currentTimeSheet.PunchInTime;

                    yield return shift;
                    shift = null;
                }

                var punchInTime = currentTimeSheet?.PunchInTime ?? DateTime.MaxValue;

                if (shift == null && transaction.TransactionDate < punchInTime)
                {
                    shift = shiftFactory();
                    shift.StartTime = transaction.TransactionDate.Floor(startPad);

                    if (shift.StartTime < previousTimeSheet?.PunchOutTime)
                        shift.StartTime = previousTimeSheet.PunchOutTime.Value;

                    shift.OperationalDate = transaction.OperationalDate.Date;
                }

                lastTransaction = transaction;
            }

            if (shift == null)
            {
                yield break;
            }

            shift.EndTime = lastTransaction.TransactionDate.Ceiling(endPad);

            if (shift.EndTime > currentTimeSheet?.PunchInTime)
                shift.EndTime = currentTimeSheet.PunchInTime;

            yield return shift;
        }

        /// <summary>
        /// Advances the time sheet enumerator so that the current time sheet's punchOutTime > transactionDate.
        /// If no more time sheets exist that meet that criteria null is returned.
        /// </summary>
        /// <param name="enumerator"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        internal static (EfficiencyTimeSheet TimeSheet, EfficiencyTimeSheet PreviousTimeSheet) AdvanceTimeSheet([NotNull] SafeEnumerator<EfficiencyTimeSheet> enumerator, Transaction transaction)
        {
            var transactionDate = transaction.TransactionDate;

            if (enumerator.Current?.PunchOutTime == null || enumerator.Current?.PunchOutTime >= transactionDate)
            {
                return (enumerator.Current, enumerator.Previous);
            }

            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;

                if (current == null)
                {
                    continue;
                }

                if (current.PunchInTime > current.PunchOutTime)
                {
                    throw new InvalidOperationException(
                        $"Invalid TimeSheet. {nameof(EfficiencyTimeSheet.PunchInTime)} > {nameof(EfficiencyTimeSheet.PunchOutTime)}");
                }

                if (enumerator.Previous?.PunchInTime > current?.PunchInTime)
                {
                    throw new ArgumentException("TimeSheets must be ordered by PunchInTime",
                        nameof(enumerator));
                }

                // We found the next time sheet. Break out.
                if (current.PunchOutTime == null || current.PunchOutTime >= transactionDate)
                {
                    break;
                }
            }

            return (enumerator.Current, enumerator.Previous);
        }
    }
}
