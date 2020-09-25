using FastMember;
using System;
using System.Collections.Generic;
using System.Linq;
using Xpo.Smart.Core.DataCache;

namespace Xpo.Smart.Efficiency.Shared.DataCache.Core
{
    public sealed class BucketCache<TItemKey, TItem>
        where TItem : class, IEquatable<TItem>, IComparable<TItem>, IBucketCacheKey<TItemKey>, new()
    {
        private SortedSet<TItem> _sortedItems;

        private readonly Dictionary<TItemKey, TItem> _items = new Dictionary<TItemKey, TItem>();

        public IEnumerable<TItem> GetAll()
        {
            if (_sortedItems == null)
            {
                _sortedItems = new SortedSet<TItem>(_items.Values);
            }

            return _sortedItems;
        }

        public void Delete(IEnumerable<TItemKey> keys)
        {
            foreach (var key in keys)
            {
                if (!_items.ContainsKey(key))
                {
                    continue;
                }

                var item = _items[key];
                _items.Remove(key);
                _sortedItems.Remove(item);
            }
        }

        public void Prune(TItem lowerValue, TItem upperValue)
        {
            if (_sortedItems == null)
            {
                _sortedItems = new SortedSet<TItem>(_items.Values);
            }

            var itemsToRemove = new List<TItem>();

            foreach (var item in _sortedItems.GetViewBetween(lowerValue, upperValue))
            {
                itemsToRemove.Add(item);
            }

            foreach (var item in itemsToRemove)
            {
                _items.Remove(item.Key);
                _sortedItems.Remove(item);
            }
        }

        public IEnumerable<TItem> GetViewsBetween(TItem lowerValue, TItem upperValue, bool includePrevious = false)
        {
            if (_sortedItems == null)
            {
                _sortedItems = new SortedSet<TItem>(_items.Values);
            }

            if (includePrevious)
            {
                var minimumValue = _sortedItems.Min;

                if (lowerValue.CompareTo(minimumValue) > 0)
                {
                    var previousValues = _sortedItems.GetViewBetween(minimumValue, lowerValue).Reverse();

                    foreach (var previousValue in previousValues)
                    {
                        if (previousValue.CompareTo(lowerValue) == 0)
                        {
                            continue;
                        }

                        yield return previousValue;
                        break;
                    }
                }
            }

            foreach (var value in _sortedItems.GetViewBetween(lowerValue, upperValue))
            {
                yield return value;
            }
        }

        public void AddOrUpdate(IEnumerable<TItem> items)
        {
            var accessor = TypeAccessor.Create(typeof(TItem));
            var members = accessor.GetMembers().ToArray();

            foreach (var item in items)
            {
                if (_items.TryGetValue(item.Key, out var actual))
                {
                    // If we are updating just assign all the properties from the source object to the one we already have.
                    foreach (var member in members)
                    {
                        if (!member.CanWrite) continue;
                        accessor[actual, member.Name] = accessor[item, member.Name];
                    }
                }
                else
                {
                    actual = item;
                    _items.Add(item.Key, item);
                }

                // We are going to lazy load this on first use, so if its not loaded don't do anything.
                if (_sortedItems == null)
                {
                    continue;
                }

                if (!_sortedItems.Contains(actual))
                {
                    _sortedItems.Add(actual);
                }
            }
        }
    }
}
