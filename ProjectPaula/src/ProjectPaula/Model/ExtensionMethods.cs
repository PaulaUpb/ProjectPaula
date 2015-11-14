using Microsoft.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPaula.Model
{
    public static class ExtensionMethods
    {
        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector)
        {
            return source.MinBy(selector, Comparer<TKey>.Default);
        }

        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer)
        {
            using (IEnumerator<TSource> sourceIterator = source.GetEnumerator())
            {
                if (!sourceIterator.MoveNext())
                {
                    throw new InvalidOperationException("Sequence was empty");
                }
                TSource min = sourceIterator.Current;
                TKey minKey = selector(min);
                while (sourceIterator.MoveNext())
                {
                    TSource candidate = sourceIterator.Current;
                    TKey candidateProjected = selector(candidate);
                    if (comparer.Compare(candidateProjected, minKey) < 0)
                    {
                        min = candidate;
                        minKey = candidateProjected;
                    }
                }
                return min;
            }
        }

        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector)
        {
            return source.MaxBy(selector, Comparer<TKey>.Default);
        }

        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer)
        {
            using (IEnumerator<TSource> sourceIterator = source.GetEnumerator())
            {
                if (!sourceIterator.MoveNext())
                {
                    throw new InvalidOperationException("Sequence was empty");
                }
                TSource max = sourceIterator.Current;
                TKey maxKey = selector(max);
                while (sourceIterator.MoveNext())
                {
                    TSource candidate = sourceIterator.Current;
                    TKey candidateProjected = selector(candidate);
                    if (comparer.Compare(candidateProjected, maxKey) > 0)
                    {
                        max = candidate;
                        maxKey = candidateProjected;
                    }
                }
                return max;
            }
        }

        public static DateTime FloorHalfHour(this DateTime source) => new DateTime(source.Year, source.Month, source.Day, source.Hour, source.Minute >= 30 ? 30 : 0, 0);

        public static DateTime CeilHalfHour(this DateTime source)
            => new DateTime(source.Year, source.Month, source.Day, source.Minute > 30 ? source.Hour + 1 : source.Hour, (source.Minute > 0 && source.Minute < 30) ? 30 : 0, 0);

        public static DateTime AtDate(this DateTime source, int day, int month, int year)
            => new DateTime(year, month, day, source.Hour, source.Minute, source.Second);

        public static IEnumerable<T> LocalChanges<T>(this IEnumerable<T> set, DbContext db) where T : class
        {
            return set.Concat(db.ChangeTracker.Entries<T>().Where(t => t.State == EntityState.Added).Select(e => e.Entity)).Except(db.ChangeTracker.Entries<T>().Where(t => t.State == EntityState.Deleted).Select(e => e.Entity));
        }

        public static IEnumerable<Course> IncludeAll(this DbSet<Course> set)
        {
            return set.Include(d => d.ConnectedCoursesInternal).Include(d => d.Catalogue).Include(d => d.Tutorials).ThenInclude(t => t.Dates).Include(d => d.Dates);
        }

        public static int LengthInHalfHours(this Date date) => ((int)(date.To - date.From).TotalMinutes) / 30;
    }
}
