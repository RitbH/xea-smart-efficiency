using System;
using Xpo.Smart.Core.DataCache;

namespace Xpo.Smart.Efficiency.Shared.DataCache.Transactions
{
    /// <summary>
    /// This class is a compact version of a transaction.
    /// All the lookup fields have a generated ID which are resolved at request time to the code to keep the memory footprint low.
    /// </summary>
    internal class CachedTransaction : IEquatable<CachedTransaction>, IComparable<CachedTransaction>,
        IBucketCacheKey<long>
    {
        public long TransactionId { get; set; }

        public int SecondsEarned { get; set; }

        public short TransactionTypeId { get; set; }

        public short SegmentId { get; set; }

        public float? Quantity { get; set; }

        public float? QuantityEarned { get; set; }

        public float? QuantityProcessed { get; set; }

        public DateTime OperationalDate { get; set; }

        public DateTime TransactionDate { get; set; }

        public bool Equals(CachedTransaction other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualsCore(other);
        }

        public int CompareTo(CachedTransaction other)
        {
            if (other == null)
            {
                return -1;
            }

            // IMPORTANT: The key fields here must for a unique sortable key for the entity.
            //            Therefore if the transaction dates are equal we use the TransactionCode to break the tie.
            var result = DateTime.Compare(TransactionDate, other.TransactionDate);
            return result != 0
                ? result
                : TransactionId.CompareTo(other.TransactionId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return EqualsCore((CachedTransaction)obj);
        }

        public override int GetHashCode()
        {
            return TransactionId.GetHashCode();
        }

        private bool EqualsCore(CachedTransaction other)
        {
            return TransactionId == other.TransactionId;
        }

        public long Key
        {
            get => TransactionId;
            set => TransactionId = value;
        }
    }
}
