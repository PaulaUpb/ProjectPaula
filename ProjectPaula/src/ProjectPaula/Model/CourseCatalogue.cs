using System.ComponentModel.DataAnnotations;

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

        public override int GetHashCode() => InternalID.GetHashCode();
    }
}
