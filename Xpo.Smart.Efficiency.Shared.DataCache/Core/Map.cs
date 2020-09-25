using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Xpo.Smart.Efficiency.Shared.DataCache.Core
{
    internal class Map<T1, T2>
    {
        private readonly object _locker = new object();
        private ImmutableDictionary<T1, T2> _forward;
        private ImmutableDictionary<T2, T1> _reverse;
        private Indexer<T1, T2> _forwardIndexer;
        private Indexer<T2, T1> _reverseIndexer;

        public Map(IEqualityComparer<T1> comparer1 = null, IEqualityComparer<T2> comparer2 = null)
        {
            _forward = comparer1 == null ? ImmutableDictionary.Create<T1, T2>() : ImmutableDictionary.Create<T1, T2>(comparer1);
            _reverse = comparer2 == null ? ImmutableDictionary.Create<T2, T1>() : ImmutableDictionary.Create<T2, T1>(comparer2);
            _forwardIndexer = new Indexer<T1, T2>(_forward);
            _reverseIndexer = new Indexer<T2, T1>(_reverse);
        }

        public class Indexer<T3, T4>
        {
            private readonly ImmutableDictionary<T3, T4> _dictionary;

            public Indexer(ImmutableDictionary<T3, T4> dictionary)
            {
                _dictionary = dictionary;
            }
            public T4 this[T3 index]
            {
                get { return _dictionary[GetIndex(index)]; }
            }
            public bool TryGetValue(T3 index, out T4 value)
            {
                return _dictionary.TryGetValue(GetIndex(index), out value);
            }
            public bool ContainsKey(T3 index)
            {
                return _dictionary.ContainsKey(GetIndex(index));
            }
        }

        public void Add(T1 t1, T2 t2)
        {
            t1 = GetIndex(t1);
            t2 = GetIndex(t2);

            if (Forward.ContainsKey(t1))
            {
                return;
            }

            lock (_locker)
            {
                if (Forward.ContainsKey(t1))
                {
                    return;
                }

                _forward = _forward.Add(t1, t2);
                _reverse = _reverse.Add(GetIndex(t2), t1);

                var forward = new Indexer<T1, T2>(_forward);
                var reverse = new Indexer<T2, T1>(_reverse);
                Interlocked.Exchange(ref _forwardIndexer, forward);
                Interlocked.Exchange(ref _reverseIndexer, reverse);
            }
        }

        public Indexer<T1, T2> Forward => _forwardIndexer;
        public Indexer<T2, T1> Reverse => _reverseIndexer;

        private static T GetIndex<T>(T index)
        {
            if (default(T) == null && EqualityComparer<T>.Default.Equals(index, default(T)))
            {
                object defaultValue = null;

                if (IsString(index)) defaultValue = "";
                // ReSharper disable once AssignNullToNotNullAttribute
                else if (IsNullable(index)) defaultValue = Activator.CreateInstance(Nullable.GetUnderlyingType(typeof(T)));

                return (T)defaultValue;
            }

            return index;
        }

        private static bool IsString<T>(T t) { return typeof(T) == typeof(string); }
        private static bool IsNullable<T>(T t) { return false; }
        private static bool IsNullable<T>(T? t) where T : struct { return true; }
    }
}
