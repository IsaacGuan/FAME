using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FameBase
{
    public static class SubsetExtensions
    {
        public static IEnumerable<IEnumerable<T>> PowerSets<T>(this IList<T> set)
        {
            var totalSets = BigInteger.Pow(2, set.Count);
            for (BigInteger i = 0; i < totalSets; i++)
            {
                yield return set.SubSet(i);
            }
        }

        public static IEnumerable<T> SubSet<T>(this IList<T> set, BigInteger n)
        {
            for (int i = 0; i < set.Count && n > 0; i++)
            {
                if ((n & 1) == 1)
                {
                    yield return set[i];
                }
                n = n >> 1;
            }
        }

        public static IEnumerable<IEnumerable<T>> SubSets<T>(this IEnumerable<T> elements, int k)
        {
            return k == 0 ? new[] { new T[0] } :
              elements.SelectMany((e, i) =>
                elements.Skip(i + 1).SubSets(k - 1).Select(c => (new[] { e }).Concat(c)));
        }
    }
}
