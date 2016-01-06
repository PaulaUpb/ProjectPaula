using Microsoft.Data.Entity;
using Microsoft.Data.Entity.ChangeTracking;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

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
        public static DateTime FloorHalfHour(this DateTimeOffset source)
            => new DateTime(source.Year, source.Month, source.Day, source.Hour, source.Minute >= 30 ? 30 : 0, 0);

        /// <summary>
        /// Return a DateTime that is rounded up to the next half hour, f.e. 10:01 -> 10:30 or 10:31 -> 11:00.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static DateTime CeilHalfHour(this DateTimeOffset source)
            => new DateTime(source.Year, source.Month, source.Day, source.Minute > 30 ? source.Hour + 1 : source.Hour, (source.Minute > 0 && source.Minute <= 30) ? 30 : 0, 0);

        /// <summary>
        /// Return a DateTime at the specified date with the receiver's hour minute and second.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="day"></param>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public static DateTime AtDate(this DateTimeOffset source, int day, int month, int year)
            => new DateTime(year, month, day, source.Hour, source.Minute, source.Second);

        public static IEnumerable<T> LocalChanges<T>(this IEnumerable<T> set, DbContext db) where T : class
        {
            return set.Concat(
                    db.ChangeTracker.Entries<T>()
                        .Where(t => t.State == EntityState.Added)
                        .Select(e => e.Entity))
                .Except(
                    db.ChangeTracker.Entries<T>()
                        .Where(t => t.State == EntityState.Deleted)
                        .Select(e => e.Entity));
        }

        public static IEnumerable<Course> IncludeAll(this DbSet<Course> set)
        {
            return set
                .Include(d => d.Catalogue)
                .Include(d => d.ConnectedCoursesInternal)
                .Include(d => d.Tutorials)
                .ThenInclude(t => t.Dates)
                .Include(d => d.Dates);
        }

        public static IEnumerable<Schedule> IncludeAll(this DbSet<Schedule> set)
        {
            return set
                .Include(s => s.Users)
                .Include(s => s.SelectedCourses)
                .ThenInclude(s => s.Users)
                .ThenInclude(s => s.SelectedCourse)
                .ThenInclude(s => s.Users)
                .Include(s => s.CourseCatalogue);
        }

        public static void TrackObject<T>(this ChangeTracker tracker, T o)
        {
            tracker.TrackGraph(o, a => { if (a?.Entry?.Entity?.GetType() == typeof(T)) a.Entry.State = EntityState.Modified; });
        }

        /// <summary>
        /// Compute the length of this Date, divided by 30 minutes by integer division.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int LengthInHalfHours(this Date date) => ((int)(date.To - date.From).TotalMinutes) / 30;

        public static void LogToConsole(this DbContext context)
        {
            var loggerFactory = context.GetService<ILoggerFactory>();
            loggerFactory.AddConsole(LogLevel.Verbose);
        }

        public static string ToBase64String(this string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

        public static string FromBase64String(this string b64) => Encoding.UTF8.GetString(Convert.FromBase64String(b64));

        /// <summary>
        /// Find all tutorials belonging to a course, including connected courses.
        /// </summary>
        /// <param name="course"></param>
        /// <returns></returns>
        public static IEnumerable<Course> FindAllTutorials(this Course course) =>
            course.Tutorials
                .Concat(
                    course.ConnectedCourses
                        .Where(connectedCourse => !connectedCourse.IsTutorial)
                        .SelectMany(connectedCourse => connectedCourse.Tutorials)
                );

        /// <summary>
        /// Find the parent Course for this tutorial in a list of parent candidates.
        /// </summary>
        /// <param name="tutorial"></param>
        /// <param name="parentCandidates"></param>
        /// <returns>Default element, if not found in candidates</returns>
        public static Course FindParent(this Course tutorial, IEnumerable<Course> parentCandidates) =>
            parentCandidates.FirstOrDefault(candidate => candidate.FindAllTutorials().Contains(tutorial));

        /// <summary>
        /// Defines an order for the days of the week.
        /// </summary>
        /// <param name="dayOfWeek"></param>
        /// <returns></returns>
        public static int Position(this DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Friday:
                    return 4;
                case DayOfWeek.Monday:
                    return 0;
                case DayOfWeek.Saturday:
                    return 5;
                case DayOfWeek.Sunday:
                    return 6;
                case DayOfWeek.Thursday:
                    return 3;
                case DayOfWeek.Tuesday:
                    return 1;
                case DayOfWeek.Wednesday:
                    return 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null);
            }
        }

        public static IEnumerable<T> SymmetricDifference<T>(this IEnumerable<T> enumerable, params IEnumerable<T>[] otherEnumerables)
        {
            var list = enumerable.ToList();
            var others = otherEnumerables.Select(it => it.ToList()).ToList();

            if (!others.Any())
            {
                throw new ArgumentException("Cannot create symmetric difference of a single set.", nameof(others));
            }

            var union = new HashSet<T>();
            union.UnionWith(others.Concat(new[] { list }).SelectMany(it => it));

            var intersection = list.Intersect(others[0]);
            intersection = others.Skip(1).Aggregate(intersection, (current, other) => current.Intersect(other));

            union.ExceptWith(intersection);
            return union;
        }

        /// <summary>
        /// Return a new DateTime instance at the same day as this,
        /// but at midnight.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime AtMidnight(this DateTime date) => new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);        
        
        /// <summary>
        /// Return a new DateTimeOffset instance at the same day as this,
        /// but at midnight.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTimeOffset AtMidnight(this DateTimeOffset date) 
            => date.AddHours(-date.Hour).AddMinutes(-date.Minute).AddSeconds(-date.Second).AddMilliseconds(-date.Millisecond);
    }
}
