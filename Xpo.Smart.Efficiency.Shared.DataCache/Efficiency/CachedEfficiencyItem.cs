using System;
using Xpo.Smart.Core.DataCache;

namespace Xpo.Smart.Efficiency.Shared.DataCache.Efficiency
{
    internal class CachedEfficiencyItem : IEquatable<CachedEfficiencyItem>, IComparable<CachedEfficiencyItem>,
        IBucketCacheKey<string>
    {
        public long EfficiencyId { get; set; }

        public DateTime ClockInStartTime { get; set; }

        public DateTime ClockInEndTime { get; set; }

        public string HashKey { get; set; }

        public int EmployeeNumberId { get; set; }

        public DateTime OperationalDay { get; set; }

        public short TransactionTypeId { get; set; }

        public int EmployeeFullNameId { get; set; }

        public short EmployeeJobCodeId { get; set; }

        public int WorkCenterCodeId { get; set; }

        public int? SecondsClockedInInTransactionalWorkCenter { get; set; }

        public int? SecondsClockedIn { get; set; }

        public int SecondsEarned { get; set; }

        public int QuantityEarned { get; set; }

        public short RecordTypeCodeId { get; set; }

        public bool IsClockedIn { get; set; }

        public DateTime? LastTransactionDate { get; set; }

        public int SupervisorId { get; set; }

        public int ShiftCodeId { get; set; }

        public bool Equals(CachedEfficiencyItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualsCore(other);
        }

        public int CompareTo(CachedEfficiencyItem other)
        {
            if (other == null)
            {
                return -1;
            }

            // IMPORTANT: The key fields here must for a unique sortable key for the entity.
            //            Therefore if the transaction dates are equal we use the TransactionCode to break the tie.
            var result = DateTime.Compare(ClockInStartTime, other.ClockInStartTime);
            return result != 0
                ? result
                : string.Compare(HashKey ?? "", other.HashKey ?? "", StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return EqualsCore((CachedEfficiencyItem)obj);
        }

        public override int GetHashCode()
        {
            return HashKey?.GetHashCode() ?? 0;
        }

        private bool EqualsCore(CachedEfficiencyItem other)
        {
            return HashKey == other.HashKey;
        }

        public string Key
        {
            get => HashKey;
            set => HashKey = value;
        }
    }
}
