using System;

namespace ProjectPaula.Model.Public
{
    public class PublicDate
    {
        /// <summary>
        /// When this date starts. Will be ISO 8601 formatted in the JSON.
        /// </summary>
        public DateTimeOffset From;

        /// <summary>
        /// When this date ends. Will be ISO 8601 formatted in the JSON.
        /// </summary>
        public DateTimeOffset To;

        public string Room;

        public string Instructor;

        public PublicDate(Date date)
        {
            From = date.From;
            To = date.To;
            Room = date.Room;
            Instructor = date.Instructor;
        }

        public PublicDate(ExamDate date)
        {
            From = date.From;
            To = date.To;
            Room = "";
            Instructor = date.Instructor;
        }
    }
}
