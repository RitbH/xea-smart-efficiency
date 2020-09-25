using System;

namespace Xpo.Smart.Efficiency.EventProcessor.Events
{
    public interface IInitEfficiencyRunner
    {
        bool IsInitialLoad(Action action);
    }
}
