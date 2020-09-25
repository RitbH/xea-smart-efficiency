using System;
using Xpo.Smart.Core.Extensions;
using Xpo.Smart.Efficiency.Shared.Extensions.Enums;

namespace Xpo.Smart.Efficiency.Shared.Models
{
    public class EfficiencySaveModel
    {
        public EfficiencySaveModel()
        {
        }

        public EfficiencySaveModel(EfficiencyRecord efficiency)
        {
            ClockInStartTime = efficiency.ClockInStartTime;
            ClockInEndTime = efficiency.ClockInEndTime;
            IntervalEndTime = efficiency.IntervalEndTime;
            IntervalStartTime = efficiency.IntervalStartTime;
            MaxIntervalSeconds = Convert.ToInt32(efficiency.MaxTransactionInterval.TotalSeconds);
            OperationalDay = efficiency.OperationalDay;
            SecondsAfterLastTransaction = Convert.ToInt32(efficiency.ShiftTimeAfterLastTransaction.TotalSeconds);
            SecondsClockedIn = Convert.ToInt32(efficiency.TimeClockedIn.TotalSeconds);
            SecondsEarned = Convert.ToInt32(efficiency.TimeEarned.TotalSeconds);
            SiteCode = efficiency.SiteCode;
            SiteEmployeeCode = efficiency.SiteEmployeeCode;
            TransactionTypeCode = efficiency.TransactionTypeCode;
            ShiftCode = efficiency.ShiftCode;
            SegmentCode = efficiency.SegmentCode;
            WorkCenterCode = efficiency.WorkCenterCode;
            SecondsToFirstTransaction = Convert.ToInt32(efficiency.ShiftTimeToFirstTransaction.TotalSeconds);
            StartShiftSeconds = Convert.ToInt32(efficiency.StartShiftTime.TotalSeconds);
            StartShiftSecondsPerShift = Convert.ToInt32(efficiency.StartShiftTimePerShift.TotalSeconds);
            EndShiftSeconds = Convert.ToInt32(efficiency.EndShiftTime.TotalSeconds);
            EndShiftSecondsPerShift = Convert.ToInt32(efficiency.EndShiftTimePerShift.TotalSeconds);
            SecondsOnTask = Convert.ToInt32(efficiency.TimeOnTask.TotalSeconds);
            SecondsInFunctionalTransitions = Convert.ToInt32(efficiency.FunctionalTransitionTime.TotalSeconds);
            SecondsOverTarget = Convert.ToInt32(efficiency.TimeOverTarget.TotalSeconds);
            UpdatedDateTime = DateTime.UtcNow;
            EmployeeFullName = efficiency.EmployeeFullName;
            Supervisor = efficiency.Supervisor;
            ShiftSupervisor = efficiency.ShiftSupervisor;
            EmployeeJobCode = efficiency.EmployeeJobCode;
            EmployeeNumber = efficiency.EmployeeNumber;
            IsTransactional = efficiency.IsTransactional;
            IsClockedIn = efficiency.IsClockedIn;
            SalaryClassCode = efficiency.SalaryClassCode;
            QuantityProcessed = Convert.ToInt32(efficiency.QuantityProcessed);
            QuantityEarned = Convert.ToInt32(efficiency.QuantityEarned);
            DuplicateTransactionCount = Convert.ToInt32(efficiency.DuplicateTransactionCount);
            SecondsLostToRework = Convert.ToInt32(efficiency.TimeLostToRework.TotalSeconds);
            IsPartTimeEmployee = efficiency.IsPartTimeEmployee;
            LastTransactionDate = efficiency.LastTransactionDate;
            RecordTypeCode = Enum.GetName(typeof(RecordType), efficiency.RecordType);
            SecondsClockedInInTransactionalWorkCenter = Convert.ToInt32(efficiency.TimeClockedInInTransactionalWorkCenter.TotalSeconds);
            IsNonProductiveClockIn = efficiency.IsNonProductiveClockIn;
        }

        public string SiteCode { get; set; }

        public string SegmentCode { get; set; }

        public string SiteEmployeeCode { get; set; }

        public string TransactionTypeCode { get; set; }
        
        public DateTime ClockInStartTime { get; set; }

        public DateTime ClockInEndTime { get; set; }

        public DateTime IntervalStartTime { get; set; }

        public DateTime IntervalEndTime { get; set; }

        public DateTime OperationalDay { get; set; }
        
        public int SecondsToFirstTransaction { get; set; }

        public int SecondsAfterLastTransaction { get; set; }

        public int SecondsInFunctionalTransitions { get; set; }
        
        public int SecondsOverTarget { get; set; }

        public int SecondsOnTask { get; set; }

        public int SecondsEarned { get; set; }

        public int SecondsClockedIn { get; set; }

        public string WorkCenterCode { get; set; }

        public string ShiftCode { get; set; }
        
        public string EmployeeFullName { get; set; }

        public string EmployeeNumber { get; set; }

        public string ShiftSupervisor { get; set; }

        public string Supervisor { get; set; }

        public string EmployeeJobCode { get; set; }

        public int MaxIntervalSeconds { get; set; }

        public bool IsClockedIn { get; set; }

        public bool IsTransactional { get; set; }

        public string SalaryClassCode { get; set; }

        public DateTime UpdatedDateTime { get; set; }
        
        public string HashKey => HashUtils.Hash(SiteCode, SegmentCode, SiteEmployeeCode, TransactionTypeCode, ClockInStartTime, ClockInEndTime, IntervalStartTime, IntervalEndTime,
            WorkCenterCode, ShiftCode, QuantityProcessed, QuantityEarned, SecondsClockedIn, SecondsEarned, MaxIntervalSeconds, OperationalDay, IsTransactional, SecondsToFirstTransaction,
            SecondsAfterLastTransaction, SecondsLostToRework, DuplicateTransactionCount, StartShiftSeconds, StartShiftSecondsPerShift, EndShiftSeconds, EndShiftSecondsPerShift,
            RecordTypeCode, IsClockedIn, IsNonProductiveClockIn);

        public int SecondsLostToRework { get; set; }

        public int DuplicateTransactionCount { get; set; }

        public bool IsPartTimeEmployee { get; set; }

        public int StartShiftSeconds { get; set; }

        public int StartShiftSecondsPerShift { get; set; }

        public int EndShiftSeconds { get; set; }

        public int EndShiftSecondsPerShift { get; set; }

        public int QuantityProcessed { get; set; }
        
        public int QuantityEarned { get; set; }

        public DateTime? LastTransactionDate { get; set; }
      
        public string RecordTypeCode { get; set; }

        public int SecondsClockedInInTransactionalWorkCenter { get; set; }

        public bool IsNonProductiveClockIn { get; set; }
    }
}
