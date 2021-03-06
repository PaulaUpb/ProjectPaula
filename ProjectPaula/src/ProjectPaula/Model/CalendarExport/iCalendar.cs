﻿using ProjectPaula.DAL;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectPaula.Model.CalendarExport
{
    public class iCalendar
    {
        /// <summary>
        /// Creates a calendar in the iCal format
        /// </summary>
        /// <param name="dates">A list of dates given by tuples. The parameters in the tuples: StartTime,EndTime,Location,Description,Organizer</param>
        /// <param name="attendees">List of attendees</param>
        /// <returns>Calendar as a string</returns>
        public static string CreateCalendar(IEnumerable<CalendarEvent> dates)
        {
            using (var db = new DatabaseContext(PaulRepository.Filename, PaulRepository.BasePath))
            {
                var str = new StringBuilder();

                str.AppendLine("BEGIN:VCALENDAR");
                str.AppendLine("X-PUBLISHED-TTL:PT6H");
                str.AppendLine("VERSION:2.0");

                try
                {
                    foreach (var d in dates)
                    {
                        str.AppendLine("BEGIN:VEVENT");

                        // handle the case for whole day events
                        if ((d.EndTime - d.StartTime).Hours >= 23)
                        {
                            str.AppendLine($"DTSTART;VALUE=DATE:{d.StartTime:yyyyMMdd}");
                            str.AppendLine($"DTEND;VALUE=DATE:{d.StartTime.AddDays(1).AddMinutes(1):yyyyMMdd}");
                        }
                        else
                        {
                            str.AppendLine($"DTSTART:{d.StartTime.ToUniversalTime():yyyyMMddTHHmmssZ}");
                            str.AppendLine($"DTEND:{d.EndTime.ToUniversalTime():yyyyMMddTHHmmssZ}");
                        }

                        str.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");

                        str.AppendLine($"LOCATION:{d.Location}");
                        str.AppendLine($"UID:{d.Uid}");
                        str.AppendLine($"SUMMARY:{d.Name}");
                        str.AppendLine($"DESCRIPTION:{d.Organizer}");
                        str.AppendLine("END:VEVENT");
                    }

                }
                catch (Exception e)
                {
                    db.Logs.Add(new Log { Message = $"Exception at time conversion: {e.Message}", Date = DateTime.Now });
                    db.SaveChanges();
                }

                str.AppendLine("END:VCALENDAR");
                return str.ToString();
            }
        }


    }

    public struct CalendarEvent
    {
        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public string Location { get; set; }

        public string Name { get; set; }

        public string Organizer { get; set; }

        /// <summary>
        /// See http://www.kanzaki.com/docs/ical/uid.html.
        /// </summary>
        public string Uid { get; set; }
    }
}
