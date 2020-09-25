using System;

namespace Xpo.Smart.Efficiency.EventProcessor.Events
{
    public class InitEfficiencyRunner : IInitEfficiencyRunner
    {
        private bool _isLoaded = false;
        private readonly object _locker = new object();
        public bool IsInitialLoad(Action action)
        {
            if (_isLoaded)
                return false;
            lock (_locker)
            {
                if (_isLoaded)
                    return false;

                action.Invoke();

                _isLoaded = true;
                return true;
            }
        }
    }
}
