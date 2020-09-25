using System.Collections.Generic;
using System.Linq;

namespace Xpo.Smart.Efficiency.Shared.Extensions
{
    public static class CollectionExtensions
    {
        public static bool IsNotNullOrEmptyElements(this ICollection<string> collection)
        {
            if (collection != null)
                return (uint)collection.Count(x => !string.IsNullOrWhiteSpace(x)) > 0U;
            return false;
        }
    }
}
