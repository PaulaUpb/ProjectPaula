﻿using Microsoft.Data.Entity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPaula.Model
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Get the element from the enumerable with the smallest item returned by the selector.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector)
        {
            return source.MinBy(selector, Comparer<TKey>.Default);
        }

        /// <summary>
        /// Get the element from the enumerable with the smallest item returned by the selector.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get the element from the enumerable with the greatest item returned by the selector.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector)
        {
            return source.MaxBy(selector, Comparer<TKey>.Default);
        }

        /// <summary>
        /// Get the element from the enumerable with the smallest item greatest by the selector.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Return a DateTime that is rounded down to the previous half hour, f.e. 10:01 -> 10:00 or 10:31 -> 10:30.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static DateTime FloorHalfHour(this DateTime source) => new DateTime(source.Year, source.Month, source.Day, source.Hour, source.Minute >= 30 ? 30 : 0, 0);

        /// <summary>
        /// Return a DateTime that is rounded up to the next half hour, f.e. 10:01 -> 10:30 or 10:31 -> 11:00.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static DateTime CeilHalfHour(this DateTime source)
            => new DateTime(source.Year, source.Month, source.Day, source.Minute > 30 ? source.Hour + 1 : source.Hour, (source.Minute > 0 && source.Minute < 30) ? 30 : 0, 0);

        /// <summary>
        /// Return a DateTime at the specified date with the receiver's hour minute and second.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="day"></param>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>
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

        public static IEnumerable<Schedule> IncludeAll(this DbSet<Schedule> set)
        {
            return set.Include(s => s.SelectedCourses).ThenInclude(s => s.Users).ThenInclude(s => s.SelectedCourse).Include(s => s.User);
        }

        /// <summary>
        /// Compute the length of this Date, divided by 30 minutes by integer division.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int LengthInHalfHours(this Date date) => ((int)(date.To - date.From).TotalMinutes) / 30;

        /// <summary>
        /// Adds a range of items to the collection. This locks the collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="items"></param>
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            lock (collection)
            {
                foreach (var item in items)
                {
                    collection.Add(item);
                }
            }
        }

        /// <summary>
        /// Concat all element from the list with the specified separator in between them.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string JoinToString(this IEnumerable<string> list , string separator)
        {
            var sb = new StringBuilder();
            foreach (var s in list)
            {
                sb.Append(s).Append(separator);
            }
            var separatorLength = separator.Length;
            sb.Remove(sb.Length - separatorLength, separatorLength);
            return sb.ToString();
        }
    }
}
