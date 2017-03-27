using System;
using System.Linq;

namespace ProjectPaula.Util
{
    /// <summary>
    /// Provides methods to convert UTC time to German time.
    /// </summary>
    public static class DateUtil
    {
        private static readonly TimeZoneInfo _germanTime = TimeZoneInfo.GetSystemTimeZones()
            .FirstOrDefault(t => t.Id == "W. Europe Standard Time" || t.Id == "Europe/Berlin");

        public static TimeZoneInfo GermanTime
        {
            get
            {
                if (_germanTime == null)
                    throw new InvalidOperationException("The time zone for Germany could not be determined");

                return _germanTime;
            }
        }

        public static DateTimeOffset ConvertUtcToGermanTime(DateTimeOffset utcTime)
        {
            return TimeZoneInfo.ConvertTime(utcTime, _germanTime);
        }
    }
}
