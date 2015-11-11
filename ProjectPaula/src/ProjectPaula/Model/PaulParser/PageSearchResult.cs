using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.Model.PaulParser
{
    public class PageSearchResult
    {
        public int Number { get; set; }
        public List<Course> Courses { get; set; }
        public List<string> LinksToNextPages { get; set; }
    }
}
