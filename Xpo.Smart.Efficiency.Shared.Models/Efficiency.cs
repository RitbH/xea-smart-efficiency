using JetBrains.Annotations;
using System;
using System.Linq;
using Xpo.Smart.Core.Models;

namespace Xpo.Smart.Efficiency.Shared.Models
{
    public class EfficiencyRecord : EfficiencyRecordType
    {
        public EfficiencyRecord([NotNull]EfficiencyShift shift, [NotNull]DateRange range, string transactionTypeCode) :base(shift, transactionTypeCode)
        {
            SiteCode = shift.SiteCode;
            SiteEmployeeCode = shift.SiteEmployeeCodes?.OrderBy(x => x).FirstOrDefault();
            SegmentCode = shift.SiteCode;            
            IntervalStartTime = range.Start;
            IntervalEndTime = range.End;
            ClockInStartTime = shift.StartTime;
            ClockInEndTime = shift.EndTime;
            ShiftCode = shift.ShiftCode;
            EmployeeNumber = shift.EmployeeNumber;
            Supervisor = shift.Supervisor;
            ShiftSupervisor = shift.ShiftSupervisor;
            EmployeeFullName = shift.EmployeeFullName;
            WorkCenterCode = shift.WorkedWorkCenterCode;
            QuantityEarned = 0;
            QuantityProcessed = 0;
            OperationalDay = shift.OperationalDate;
            IsTransactional = shift.IsTransactional;
            SalaryClassCode = shift.SalaryClassCode;
            IsClockedIn = shift.IsClockedIn;
            IsPartTimeEmployee = shift.IsPartTimeEmployee;
            EmployeeJobCode = shift.BusinessUnitType != BusinessUnitType.LTL || shift.EmployeeJobCode == JobType.Driver.Code ? shift.EmployeeJobCode : JobType.DockWorker.Code;
            IsEmployeeTransactional = shift.IsEmployeeTransactional;
            WorkcenterType = shift.WorkcenterType;
        }

        public string SiteCode { get; set; }

        public string SegmentCode { get; set; }

        public string SiteEmployeeCode { get; set; }

        public DateTime ClockInStartTime { get; set; }

        public DateTime ClockInEndTime { get; set; }

        public DateTime IntervalStartTime { get; set; }

        public DateTime IntervalEndTime { get; set; }

        public DateTime OperationalDay { get; set; }

        public decimal QuantityProcessed { get; set; }
        
        public decimal QuantityEarned { get; set; }
        
        public decimal DuplicateTransactionCount { get; set; }

        public string WorkCenterCode { get; set; }

        public string ShiftCode { get; set; }
        
        public string EmployeeFullName { get; set; }

        public string EmployeeNumber { get; set; }

        public string ShiftSupervisor { get; set; }

        public string Supervisor { get; set; }

        public string EmployeeJobCode { get; set; }

        public bool IsClockedIn { get; set; }

        public bool IsTransactional { get; set; }

        public string SalaryClassCode { get; set; }

        public bool IsPartTimeEmployee { get; set; }

        public bool IsEmployeeTransactional { get; set; }

        public WorkcenterType WorkcenterType { get; set; }

        /// <summary>
        /// Gets or sets the amount of time earned during this efficiency interval.
        /// Time earned is the amount of time you receive for doing N transactions given a standard rate.
        /// E.X: If I do 10 transactions that have an expected rate of 5 minutes, I have earned 50 minutes for doing them.
        /// </summary>
        public TimeSpan TimeEarned { get; set; }

        /// <summary>
        /// Gets or sets the amount of time spent in functional changes.
        /// E.X. In the case of SupplyChain, the amount of time going from PICKING to VERIFICATION.
        /// </summary>
        public TimeSpan FunctionalTransitionTime { get; set; }
        
        /// <summary>
        /// During this interval, the amount of time spent from the start of the interval to the first transaction.
        /// </summary>
        public TimeSpan IntervalStartToFirstTransaction { get; set; }

        /// <summary>
        /// During this interval, the amount of time spent from the last transaction to the end of the interval.
        /// </summary>
        public TimeSpan LastTransactionToIntervalEnd { get; set; }
        
        /// <summary>
        /// The amount of time spent in transitions for this interval, not including functional transitions.
        /// </summary>
        public TimeSpan TransitionTime { get; set; }

        /// <summary>
        /// Gets or sets the amount of time spent that was over the target rate in this interval.
        /// This is the amount of time that is spend between all transaction transitions over the target time, minus functional transitions.
        /// </summary>
        //public TimeSpan TimeOverTarget => TransitionTime - TimeEarned;
        public TimeSpan TimeOverTarget { get; set; }
        
        /// <summary>
        /// Gets or sets the max interval between transactions
        /// </summary>
        public TimeSpan MaxTransactionInterval { get; set; }
        
        /// <summary>
        /// Gets or sets the time to the first transaction for this shift, when this summary contains the first transaction for the shift.
        /// </summary>
        public TimeSpan ShiftTimeToFirstTransaction { get; set; }

        /// <summary>
        /// Gets or sets the time to the first transaction for this shift, when it's first timesheet within operational day with non-zero transactions.
        /// </summary>
        public TimeSpan StartShiftTime { get; set; }

        /// <summary>
        /// Gets or sets the time to the first transaction for this shift, when it's first timesheet within operational day and shiftCode with non-zero transactions.
        /// </summary>
        public TimeSpan StartShiftTimePerShift { get; set; }

        /// <summary>
        /// Gets or sets the time after the first transaction for this shift, when this summary contains the last transaction for the shift.
        /// </summary>
        public TimeSpan ShiftTimeAfterLastTransaction { get; set; }

        /// <summary>
        /// Gets or sets the time after the first transaction for this shift, when it's last timesheet within operational day.
        /// </summary>
        public TimeSpan EndShiftTime { get; set; }

        /// <summary>
        /// Gets or sets the time after the first transaction for this shift, when it's last timesheet within operational day and shiftCode.
        /// </summary>
        public TimeSpan EndShiftTimePerShift { get; set; }
        
        /// <summary>
        /// Gets or sets the time that's spent on load transactions where no qty is earned = duplicate scans for same order code
        /// </summary>
        public TimeSpan TimeLostToRework { get; set; }

        /// <summary>
        /// Gets the total max interval for this efficiency interval
        /// </summary>
        public TimeSpan MaxInterval
        {
            get
            {
                var intervals = new[]
                {
                    MaxTransactionInterval,
                    IntervalStartToFirstTransaction,
                    LastTransactionToIntervalEnd
                };

                return intervals.Max();
            }
        }

        public TimeSpan TimeClockedIn { get; set; }

        public TimeSpan TimeClockedInInTransactionalWorkCenter =>
            WorkcenterType.Equals(WorkcenterType.TRANSACTIONAL) && IsEmployeeTransactional
                ? TimeClockedIn
                : new TimeSpan();

        public TimeSpan TimeOnTask { get; set; }

        public DateTime? LastTransactionDate { get; set; }

        public bool IsNonProductiveClockIn { get; set; }
    }
}
