using System.Collections.Generic;

namespace ProjectPaula.Model.PaulParser
{
    public class PageSearchResult
    {
        public int Number { get; set; }
        public List<Course> Courses { get; set; }
        public List<string> LinksToNextPages { get; set; }
    }
}
