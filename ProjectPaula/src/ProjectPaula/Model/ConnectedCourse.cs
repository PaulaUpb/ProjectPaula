using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.Model
{
    public class ConnectedCourse
    {
        public string CourseId2 { get; set; }
        public override bool Equals(object obj)
        {
            return obj is ConnectedCourse && ((ConnectedCourse)obj).CourseId2 == CourseId2;
        }

        public override int GetHashCode()
        {
            return CourseId2.GetHashCode();
        }
    }


}
