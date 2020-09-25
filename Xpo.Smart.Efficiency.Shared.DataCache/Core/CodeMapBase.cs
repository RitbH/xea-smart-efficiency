using System;

namespace Xpo.Smart.Efficiency.Shared.DataCache.Core
{
    public abstract class CodeMapBase
    {
        private readonly object _locker = new object();
        private readonly Map<string, int> _map = new Map<string, int>();
        private readonly Random _random = new Random(DateTime.Now.Millisecond);

        protected string TryGetCode(int id)
        {
            return _map.Reverse[id];
        }

        protected int GetOrAddCode(string value, int maxValue)
        {
            if (_map.Forward.TryGetValue(value, out var id))
            {
                return id;
            }

            lock (_locker)
            {
                if (_map.Forward.TryGetValue(value, out id))
                {
                    return id;
                }

                var counter = 0;
                id = _random.Next(maxValue);

                while (_map.Reverse.ContainsKey(id))
                {
                    id = _random.Next(maxValue);
                    counter++;

                    if (counter > 1000)
                    {
                        throw new InvalidOperationException("Could not generate an appropriate key within 1000 iterations");
                    }
                }

                _map.Add(value, id);
                return id;
            }
        }
    }
}
