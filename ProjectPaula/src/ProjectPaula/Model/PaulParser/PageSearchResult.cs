using System.Collections.Generic;
using System.Globalization;

namespace ProjectPaula.Model.PaulParser
{
    public class PageSearchResult
    {
        public int Number { get; set; }
        public List<Course> Courses { get; set; }
        public List<string> LinksToNextPages { get; set; }

        public static PageSearchResult Empty = new PageSearchResult()
        {
            Number = 0,
            Courses = new List<Course>(),
            LinksToNextPages = new List<string>()
        };
    }
}
