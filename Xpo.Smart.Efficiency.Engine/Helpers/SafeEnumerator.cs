using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;

namespace Xpo.Smart.Efficiency.Engine.Helpers
{
    /// <summary>
    /// Safe enumerator that will not throw once the enumeration has ended. Instead it will return default(T).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SafeEnumerator<T> : IEnumerator<T>
    {
        private bool _completed = false;
        private readonly IEnumerator<T> _inner;
        private T _currentValue;

        public SafeEnumerator([NotNull] IEnumerator<T> inner)
        {
            _inner = inner;
        }
            
        public bool MoveNext()
        {
            if (_completed)
            {
                return false;
            }

            var next = _inner.MoveNext();
            _completed = !next;
            Previous = _currentValue;
            _currentValue = Current;
            return next;
        }

        public void Reset()
        {
            _inner.Reset();
            _completed = false;
        }

        public T Previous { get; private set; }

        public T Current => _completed ? default(T) : _inner.Current;

        object IEnumerator.Current => Current;

        public void Dispose() => _inner.Dispose();
    }
}
