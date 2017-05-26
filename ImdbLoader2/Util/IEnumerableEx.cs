using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImdbLoader2.Util
{
    public static class IEnumerableEx
    {
        public static IEnumerable<List<T>> ToBatch<T>(this IEnumerable<T> source, int size)
        {
            var list = new List<T>();
            var count = 0;

            foreach (var item in source)
            {
                count++;
                list.Add(item);

                if (count == size)
                {
                    yield return list;
                    count = 0;
                    list.Clear();
                }
            }

            if (list.Any())
            {
                yield return list;
            }
        }
    }
}
