using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectPaula.Model
{
    public class PrioritySearch<T>
    {
        private List<Func<T, string>> _properties;

        public PrioritySearch(IEnumerable<Func<T, string>> properties)
        {
            _properties = properties.ToList();
        }

        public List<T> Search(IEnumerable<T> list, string searchTerm)
        {
            return list
                .Select(l =>
                {
                    double score = 0;
                    for (int i = 0; i < _properties.Count; i++)
                    {
                        var val = _properties[i](l)?.ToLower();
                        if (val != null && val.Contains(searchTerm.ToLower()))
                        {
                            var distance = (double)LevenshteinDistance(val.ToLower(), searchTerm.ToLower());
                            var ratio = 1 - (distance / Math.Max(val.Length, searchTerm.Length));
                            score += (_properties.Count - i) + ratio * 2 * ((_properties.Count - i));
                        }
                    }
                    return new SearchResult<T> { Object = l, Score = score };
                })
                .Where(o => o.Score != 0)
                .OrderByDescending(o => o.Score)
                .Select(o => o.Object)
                .ToList();
        }

        public int LevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
            {
                if (string.IsNullOrEmpty(target)) return 0;
                return target.Length;
            }
            if (string.IsNullOrEmpty(target)) return source.Length;

            if (source.Length > target.Length)
            {
                var temp = target;
                target = source;
                source = temp;
            }

            var m = target.Length;
            var n = source.Length;
            var distance = new int[2, m + 1];
            // Initialize the distance 'matrix'
            for (var j = 1; j <= m; j++) distance[0, j] = j;

            var currentRow = 0;
            for (var i = 1; i <= n; ++i)
            {
                currentRow = i & 1;
                distance[currentRow, 0] = i;
                var previousRow = currentRow ^ 1;
                for (var j = 1; j <= m; j++)
                {
                    var cost = (target[j - 1] == source[i - 1] ? 0 : 1);
                    distance[currentRow, j] = Math.Min(Math.Min(
                                distance[previousRow, j] + 1,
                                distance[currentRow, j - 1] + 1),
                                distance[previousRow, j - 1] + cost);
                }
            }
            return distance[currentRow, m];
        }
    }

    struct SearchResult<T>
    {
        public T Object { get; set; }
        public double Score { get; set; }
    }

}
