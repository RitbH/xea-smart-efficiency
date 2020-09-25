namespace Xpo.Smart.Efficiency.Shared.DataCache.Core
{
    public sealed class CodeMap : CodeMapBase
    {
        public short GetOrAddCode(string value)
        {
            return (short)GetOrAddCode(value, short.MaxValue);
        }

        public string TryGetCode(short id)
        {
            return base.TryGetCode(id);
        }

        public int GetOrAddIntCode(string value)
        {
            return GetOrAddCode(value, int.MaxValue);
        }

        public string TryGetIntCode(int id)
        {
            return base.TryGetCode(id);
        }
    }
}
