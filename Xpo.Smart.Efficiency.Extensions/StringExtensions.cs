using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpo.Smart.Efficiency.Shared.Extensions
{
    public static class StringExtensions
    {
        public static bool IgnoreCaseEquals(this string s1, string s2)
        {
            return s1 != null && s2 != null && string.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool ExistsInArray(this string searchTerm, ICollection<string> arrayToSearch)
        {
            return arrayToSearch.IsNotNullOrEmptyElements() && arrayToSearch.Any(x => x.IgnoreCaseEquals(searchTerm));
        }
    }
}
