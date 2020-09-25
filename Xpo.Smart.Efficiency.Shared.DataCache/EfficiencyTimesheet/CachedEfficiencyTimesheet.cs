using System;
using Xpo.Smart.Core.DataCache;
using Xpo.Smart.Efficiency.Shared.Models;

namespace Xpo.Smart.Efficiency.Shared.DataCache.EfficiencyTimesheet
{
    public class CachedEfficiencyTimesheet: EfficiencyTimeSheet, IEquatable<CachedEfficiencyTimesheet>, IComparable<CachedEfficiencyTimesheet>, IBucketCacheKey<long>
    {
        public long Key => TimeSheetId ;

        public int CompareTo(CachedEfficiencyTimesheet other)
        {
            if (other == null)
                return -1;

            var result = PunchInTime.CompareTo(other.PunchInTime);

            if (result == 0)
            {
                result = Key.CompareTo(other.Key);
            }
            
            return result;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return EqualsCore((CachedEfficiencyTimesheet)obj);
        }

        public bool Equals(CachedEfficiencyTimesheet other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualsCore(other);
        }

        private bool EqualsCore(CachedEfficiencyTimesheet other)
        {
            return Key == other.Key;
        }
    }
}
