using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPaula.Model
{
    public class CourseCatalogue
    {
        public string Title { get; set; }
        [Key]
        public string InternalID { get; set; }


        public override bool Equals(object obj)
        {
            return obj is CourseCatalogue && ((CourseCatalogue)obj).InternalID == InternalID;
        }
        public override int GetHashCode()
        {
            return InternalID.GetHashCode();
        }
    }


}
