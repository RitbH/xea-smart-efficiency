using System;
using Xpo.Smart.Efficiency.Shared.Extensions.Enums;

namespace Xpo.Smart.Efficiency.Shared.Models
{
    public class EfficiencyData
    {
        public long EfficiencyId { get; set; }

        public string SiteCode { get; set; }

        public string EmployeeNumber { get; set; }

        public string SiteEmployeeCode { get; set; }

        public DateTime ClockInStartTime { get; set; }

        public DateTime ClockInEndTime { get; set; }

        public string HashKey { get; set; }

        public string EfficiencyEmployeeNumber => string.IsNullOrWhiteSpace(EmployeeNumber) ? SiteEmployeeCode : EmployeeNumber;

        public DateTime OperationalDay { get; set; }

        public string TransactionTypeCode { get; set; }

        public string EmployeeFullName { get; set; }

        public string EmployeeJobCode { get; set; }

        public int? SecondsClockedInInTransactionalWorkCenter { get; set; }

        public int? SecondsClockedIn { get; set; }

        public int SecondsEarned { get; set; }

        public int QuantityEarned { get; set; }

        public string WorkCenterCode { get; set; }

        public string RecordTypeCode { get; set; }

        public RecordType RecordType => Enum.TryParse(RecordTypeCode, true, out RecordType type) ? type : RecordType.NotSet;

        public bool IsClockedIn { get; set; }

        public DateTime? LastTransactionDate { get; set; }

        public string Supervisor { get; set; }

        public string ShiftCode { get; set; }
    }
}
