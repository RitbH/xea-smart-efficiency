using System;
using System.Collections.Generic;

namespace Xpo.Smart.Efficiency.Shared.DataCache.Core
{
    public interface ICacheRepositoryFactory<out TRepository>
    {
        TRepository GetCurrent();

        TRepository GetActive();

        IDisposable CreateScope(DateTime startDate, DateTime endDate, IEnumerable<string> siteCodes = null);
    }
}
