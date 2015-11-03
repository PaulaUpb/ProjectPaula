using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPaul.Model
{
    public class CourseCatalogue
    {
        public string Title { get; set; }
        [Key]
        public string InternalID { get; set; }
    }
}
