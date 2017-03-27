using System;
using System.Collections.Generic;

namespace ProjectPaula.Util
{
    public struct SemesterName
    {
        /// <summary>
        /// Examples:
        /// "Vorlesungsverzeichnis der Universität Paderborn SS 2017"
        /// "Vorlesungsverzeichnis der Universität Paderborn WS 2017/18"
        /// </summary>
        public string Title => "Vorlesungsverzeichnis der Universität Paderborn " + ShortTitle;

        /// <summary>
        /// Examples: "SS 2017", "WS 2017/18"
        /// </summary>
        public string ShortTitle { get; }

        public SemesterName(string shortTitle)
        {
            ShortTitle = shortTitle;
        }

        public override string ToString() => ShortTitle;

        public static SemesterName ForCurrentSemester() => ForDate(DateUtil.ConvertUtcToGermanTime(DateTimeOffset.UtcNow));

        public static SemesterName ForPreviousSemester()
        {
            var datePlusHalfYear = DateUtil.ConvertUtcToGermanTime(DateTimeOffset.UtcNow - TimeSpan.FromDays(182));
            return ForDate(datePlusHalfYear);
        }

        public static SemesterName ForNextSemester()
        {
            var datePlusHalfYear = DateUtil.ConvertUtcToGermanTime(DateTimeOffset.UtcNow + TimeSpan.FromDays(182));
            return ForDate(datePlusHalfYear);
        }

        public static SemesterName ForDate(DateTimeOffset date)
        {
            if (date.Month < 4)
            {
                // January 1 to March 31: winter term "WS AA/BB" in the BB-year
                return new SemesterName($"WS {date.Year - 1}/{(date.Year % 100).ToString().PadLeft(2, '0')}");
            }
            else if (date.Month > 9)
            {
                // October 1 to December 31: winter term "WS AA/BB" in the AA-year
                return new SemesterName($"WS {date.Year}/{((date.Year + 1) % 100).ToString().PadLeft(2, '0')}");
            }
            else
            {
                // April 1 to September 30: summer term
                return new SemesterName($"SS {date.Year}");
            }
        }

        /// <summary>
        /// Gets <see cref="SemesterName"/>s of the previous, the current and the next semester in the correct order.
        /// </summary>
        public static IReadOnlyList<SemesterName> ForRelevantThreeSemesters()
        {
            return new[]
            {
                ForPreviousSemester(),
                ForCurrentSemester(),
                ForNextSemester()
            };
        }
    }
}
