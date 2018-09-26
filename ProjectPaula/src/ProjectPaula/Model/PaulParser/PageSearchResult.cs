using System.Collections.Generic;
using System.Globalization;

namespace ProjectPaula.Model.PaulParser
{
    public class PageSearchResult
    {
        public int Number { get; set; }
        public bool HasCourses { get; set; }
        public List<string> LinksToNextPages { get; set; }

        public static PageSearchResult Empty = new PageSearchResult()
        {
            Number = 0,
            HasCourses = false,
            LinksToNextPages = new List<string>()
        };
    }
}
