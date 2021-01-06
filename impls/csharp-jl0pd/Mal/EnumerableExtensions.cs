using System.Collections.Generic;

namespace Mal
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<KeyValuePair<T, T>> ToKeyValuePairs<T>(this IEnumerable<T> seq)
        {
            T? prev = default;
            int i = 0;
            foreach (T el in seq)
            {
                if (i % 2 == 1)
                {
                    yield return new KeyValuePair<T, T>(prev!, el);
                }
                prev = el;
                i++;
            }
        }

        public static IEnumerable<T> Flatten<T>(this IEnumerable<KeyValuePair<T, T>> seq)
        {
            foreach (var kvp in seq)
            {
                yield return kvp.Key;
                yield return kvp.Value;
            }
        }
    }
}