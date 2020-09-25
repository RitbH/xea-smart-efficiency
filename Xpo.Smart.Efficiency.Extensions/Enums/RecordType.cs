using System.ComponentModel;

namespace Xpo.Smart.Efficiency.Shared.Extensions.Enums
{
    public enum RecordType
    {
        [Description("Not Set")]
        NotSet = 0,
        [Description("Non-Transactional")]
        NonTransactional = 1,
        [Description("Monitored")]
        Monitored = 2,
        [Description("Transactional")]
        Transactional = 3
    }
}
