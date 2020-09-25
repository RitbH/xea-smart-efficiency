using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Xpo.Smart.Core.Models;
using Xpo.Smart.Efficiency.Engine.Interfaces;
using Xpo.Smart.Efficiency.Shared.Extensions;
using Xpo.Smart.Efficiency.Shared.Models;
using Transaction = Xpo.Smart.Efficiency.Shared.Models.Transaction;

namespace Xpo.Smart.Efficiency.Engine
{
    public sealed class EfficiencyEngine
    {
        private readonly ITransactionProvider _transactionProvider;

        public EfficiencyEngine([NotNull] ITransactionProvider transactionProvider)
        {
            _transactionProvider = transactionProvider;
        }

        [NotNull]
        public IEnumerable<EfficiencyRecord> Compute([NotNull] IEnumerable<EfficiencyShift> shifts, LaborRate[] laborRates, DateTime start, DateTime end)
        {
            // TODO: This can be configurable
            var breakdownInterval = TimeSpan.FromHours(1);

            var results = new List<EfficiencyRecord>();

            foreach (var shift in shifts)
            {
                Transaction shiftFirstTransaction = null;
                Transaction shiftLastTransaction = null;
                var shiftEfficiencyResults = new List<EfficiencyRecord>();

                var shiftIntervals = DateRangeList.GenerateAscending(shift.StartTime, shift.EndTime, breakdownInterval, nearest: true);

                foreach (var shiftInterval in shiftIntervals)
                {
                    var intervalTransactions = _transactionProvider.FindTransactions(shift.SiteCode,
                        shift.SiteEmployeeCodes, shiftInterval.Start, shiftInterval.End.AddMilliseconds(-1)  //use EndOf.AddMilliseconds(-1) here so that we don't count same transaction for different intervals
                        ).ToArray();

                    if (intervalTransactions.Any())
                    {
                        if (shiftFirstTransaction == null)
                        {
                            shiftFirstTransaction = intervalTransactions.First();

                            // For the first transaction in a shift the transition time is not relevant, since
                            // we count it in the time to first transaction.
                            shiftFirstTransaction.TransitionTimeSeconds = 0;
                        }

                        shiftLastTransaction = intervalTransactions.Last();
                    }

                    var intervalEfficiencyResults = HandleInterval(shift, shiftInterval, intervalTransactions, laborRates);

                    shiftEfficiencyResults.AddRange(intervalEfficiencyResults);
                }

                shiftEfficiencyResults.ForEach(x => x.LastTransactionDate = shiftLastTransaction?.TransactionDate);

                //set time clocked-in for shifts with no transactions
                if (shiftEfficiencyResults.All(x => x.QuantityProcessed == 0))
                    foreach (var effitem in shiftEfficiencyResults)
                    {
                        effitem.TimeClockedIn = TimeSpan.FromSeconds(Convert.ToInt32((effitem.IntervalEndTime - effitem.IntervalStartTime).TotalSeconds));
                    }

                //set time to first for shifts with no transactions that count towards efficiency
                if (shiftEfficiencyResults.All(x => x.QuantityProcessed == 0 && x.TimeEarned.TotalSeconds == 0))
                    foreach (var effitem in shiftEfficiencyResults)
                    {
                        effitem.ShiftTimeToFirstTransaction = effitem.TimeClockedIn;
                        effitem.IsNonProductiveClockIn = shift.IsTransactional;
                    }

                if (shiftEfficiencyResults.Any(x => x.QuantityProcessed > 0))
                {
                    // Set the time to the first and last transactions for this shift
                    var firstTransactionEfficiencyRecords = shiftEfficiencyResults.Where(e =>
                        (e.TransactionTypeCode == null || e.TransactionTypeCode.IgnoreCaseEquals(shiftFirstTransaction?.TransactionTypeCode)) &&
                        e.IntervalStartTime <= shiftFirstTransaction?.TransactionDate);

                    if (firstTransactionEfficiencyRecords != null && shiftFirstTransaction != null && firstTransactionEfficiencyRecords.Any())
                    {
                        foreach (var item in firstTransactionEfficiencyRecords)
                        {
                            var timeClockedIn = TimeSpan.FromSeconds(Convert.ToInt32((item.IntervalEndTime - item.IntervalStartTime).TotalSeconds));
                            var shiftTimeToFirstTransaction = item.QuantityProcessed == 0 ? timeClockedIn : item.IntervalStartToFirstTransaction;
                            item.ShiftTimeToFirstTransaction = shift.IsTransactional ? shiftTimeToFirstTransaction : new TimeSpan();
                            item.TimeClockedIn += shiftTimeToFirstTransaction;
                            item.TransactionTypeCode = item.TransactionTypeCode ?? shiftFirstTransaction.TransactionTypeCode;
                        }
                    }

                    var lastTransactionEfficiencyRecords = shiftEfficiencyResults.Where(e =>
                        (e.TransactionTypeCode == null || e.TransactionTypeCode.IgnoreCaseEquals(shiftLastTransaction?.TransactionTypeCode)) &&
                        e.IntervalEndTime >= shiftLastTransaction?.TransactionDate);

                    if (lastTransactionEfficiencyRecords != null && shiftLastTransaction != null && lastTransactionEfficiencyRecords.Any())
                    {
                        foreach (var item in lastTransactionEfficiencyRecords)
                        {
                            var timeClockedIn = TimeSpan.FromSeconds(Convert.ToInt32((item.IntervalEndTime - item.IntervalStartTime).TotalSeconds));
                            var shiftTimeAfterLastTransaction = item.QuantityProcessed == 0 ? timeClockedIn : item.LastTransactionToIntervalEnd;
                            item.ShiftTimeAfterLastTransaction = shift.IsTransactional && !shift.IsClockedIn ? shiftTimeAfterLastTransaction : new TimeSpan();
                            item.TimeClockedIn += shiftTimeAfterLastTransaction;
                            item.TransactionTypeCode = item.TransactionTypeCode ?? shiftLastTransaction.TransactionTypeCode;
                        }
                    }
                }

                results.AddRange(shiftEfficiencyResults
                    .Where(x => x.TransactionTypeCode != null || x.TimeClockedIn.TotalSeconds != 0)); //filter out rows that don't have any seconds clocked in and ttype is null
            }

            SetStartEndShiftTime(results, start, end);

            return results;
        }

        private static void SetStartEndShiftTime(List<EfficiencyRecord> efficiency, DateTime start, DateTime end)
        {
            var groupedEfficiency = efficiency.Where(x => x.OperationalDay >= start.Date && x.OperationalDay <= end.Date && x.IsTransactional)
                .GroupBy(x => new { Employee = x.EmployeeNumber ?? x.SiteEmployeeCode, x.OperationalDay });

            foreach (var employeeDayEfficiency in groupedEfficiency)
            {
                var clockInsEfficiency = employeeDayEfficiency.GroupBy(x => x.ClockInStartTime).OrderBy(x => x.Key);

                var startShiftEfficiency = clockInsEfficiency.TakeUntil(x => x.Sum(y => y.QuantityProcessed) > 0).SelectMany(x => x);
                foreach (var item in startShiftEfficiency)
                {
                    item.StartShiftTime = item.ShiftTimeToFirstTransaction;
                }

                var endShiftEfficiency = clockInsEfficiency.LastOrDefault();
                foreach (var item in endShiftEfficiency)
                {
                    item.EndShiftTime = item.ShiftTimeAfterLastTransaction;
                }

                var shiftsEfficiency = employeeDayEfficiency.GroupBy(x => x.ShiftCode);

                foreach (var shiftEfficiency in shiftsEfficiency)
                {
                    var shiftClockInsEfficiency = shiftEfficiency.GroupBy(x => x.ClockInStartTime).OrderBy(x => x.Key);

                    var shiftStartShiftEfficiency = shiftClockInsEfficiency.TakeUntil(x => x.Sum(y => y.QuantityProcessed) > 0).SelectMany(x => x);
                    foreach (var item in shiftStartShiftEfficiency)
                    {
                        item.StartShiftTimePerShift = item.ShiftTimeToFirstTransaction;
                    }

                    var shiftEndShiftEfficiency = shiftClockInsEfficiency.LastOrDefault();
                    foreach (var item in shiftEndShiftEfficiency)
                    {
                        item.EndShiftTimePerShift = item.ShiftTimeAfterLastTransaction;
                    }
                }
            }
        }

        [NotNull]
        internal static IEnumerable<EfficiencyRecord> HandleInterval([NotNull] EfficiencyShift shift, [NotNull] DateRange shiftInterval, [NotNull] ICollection<Transaction> intervalTransactions,
            LaborRate[] laborRates)
        {
            if (!intervalTransactions.Any())
                return new[] { new EfficiencyRecord(shift, shiftInterval,null) };

            var firstTransaction = intervalTransactions.First();
            var lastTransaction = intervalTransactions.Last();

            var transactionGroups = intervalTransactions.GroupBy(t => new
            {
                SegmentCode = (t.SegmentCode ?? t.SiteCode)?.ToUpperInvariant(),
                TransactionTypeCode = t.TransactionTypeCode?.ToUpperInvariant()
            });

            var groupingResults = new List<EfficiencyRecord>();

            foreach (var grouping in transactionGroups)
            {
                var efficiency = new EfficiencyRecord(shift, shiftInterval, grouping.Key.TransactionTypeCode)
                {
                    SegmentCode = grouping.Key.SegmentCode,
                    TransactionTypeCode = grouping.Key.TransactionTypeCode
                };

                var transitionSeconds = 0;
                var lostToReworkSeconds = 0;
                var functionalTransitionSeconds = 0;
                var secondsEarned = 0;
                var maxTransactionInterval = 0;
                var secondsOverTarget = 0;

                foreach (var item in grouping)
                {
                    var targetSecondsPerTransaction = Convert.ToDecimal(
                        laborRates.FirstOrDefault(x => x.TransactionTypeCode.IgnoreCaseEquals(grouping.Key.TransactionTypeCode))?.GetItemDuration().TotalSeconds ?? 0);

                    secondsEarned += item.SecondsEarned;

                    if (item.IsFunctionTransition)
                    {
                        functionalTransitionSeconds += item.TransitionTimeSeconds <= item.SecondsEarned ? 0 : (item.TransitionTimeSeconds - item.SecondsEarned);
                        var transactionTransitionTime = item.TransitionTimeSeconds <= item.SecondsEarned ? item.TransitionTimeSeconds : item.SecondsEarned;
                        transitionSeconds += transactionTransitionTime;
                        lostToReworkSeconds += Convert.ToInt32(IsReworkTransaction(item) ? item.QuantityProcessed * targetSecondsPerTransaction : 0);
                    }
                    else
                    {
                        transitionSeconds += item.TransitionTimeSeconds;
                        lostToReworkSeconds += Convert.ToInt32(IsReworkTransaction(item) ? item.QuantityProcessed * targetSecondsPerTransaction : 0);
                        secondsOverTarget += item.TransitionTimeSeconds < item.SecondsEarned ? 0 : item.TransitionTimeSeconds - item.SecondsEarned;
                    }

                    if (item.TransitionTimeSeconds > maxTransactionInterval)
                    {
                        maxTransactionInterval = item.TransitionTimeSeconds;
                    }
                }

                if (shift.IsTransactional)
                {
                    efficiency.TimeEarned = TimeSpan.FromSeconds(secondsEarned);
                    efficiency.TransitionTime = TimeSpan.FromSeconds(transitionSeconds);
                    efficiency.TimeOverTarget = TimeSpan.FromSeconds(secondsOverTarget);
                    efficiency.FunctionalTransitionTime = TimeSpan.FromSeconds(functionalTransitionSeconds);
                    efficiency.TimeOnTask = TimeSpan.FromSeconds(transitionSeconds - secondsOverTarget);
                }

                // Allocate to the appropriate transaction types
                if (grouping.Key.TransactionTypeCode.IgnoreCaseEquals(firstTransaction.TransactionTypeCode))
                {
                    efficiency.IntervalStartToFirstTransaction = firstTransaction.TransactionDate - shiftInterval.Start;
                }

                if (grouping.Key.TransactionTypeCode.IgnoreCaseEquals(lastTransaction.TransactionTypeCode))
                {
                    efficiency.LastTransactionToIntervalEnd = shiftInterval.End - lastTransaction.TransactionDate;
                }

                efficiency.MaxTransactionInterval = TimeSpan.FromSeconds(maxTransactionInterval);
                efficiency.TimeClockedIn = TimeSpan.FromSeconds(transitionSeconds + functionalTransitionSeconds);
                efficiency.QuantityProcessed = grouping.Sum(y => y.QuantityProcessed ?? 0);
                efficiency.QuantityEarned = grouping.Sum(y => y.QuantityEarned ?? 0);
                efficiency.DuplicateTransactionCount = grouping.Where(x => IsReworkTransaction(x)).Sum(y => y.DuplicateQuantity ?? 0);
                efficiency.TimeLostToRework += TimeSpan.FromSeconds(lostToReworkSeconds);
                efficiency.OperationalDay = shift.OperationalDate.Date;

                groupingResults.Add(efficiency);
            }

            return groupingResults;
        }

        private static bool IsReworkTransaction(Transaction transaction)
        {
            return transaction.TransactionTypeCode.IgnoreCaseEquals(Constants.TransactionTypes.LOAD) && transaction.QuantityEarned == 0;
        }
    }
}
