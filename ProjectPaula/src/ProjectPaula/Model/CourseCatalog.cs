using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectPaula.Model
{
    public class CourseCatalog
    {
        public string Title { get; set; }

        [Key]       
        public string InternalID { get; set; }

        public override bool Equals(object obj)
        {
            return obj is CourseCatalog && ((CourseCatalog)obj).InternalID == InternalID;
        }

        public override int GetHashCode() => InternalID.GetHashCode();
    }
}
