using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace ProjectPaula.Model
{
    public class CourseCatalog
    {
        public string Title { get; set; }

        [NotMapped]
        public string ShortTitle
        {
            get
            {
                var shorted = Regex.Match(Title, @"((WS|SS)\s+[0-9]+(\/[0-9]+)?)");
                if (shorted.Success)
                {
                    return shorted.Groups[1].Value;
                }

                return Title;
            }
        }

        [Key]
        public string InternalID { get; set; }

        public override bool Equals(object obj)
        {
            return obj is CourseCatalog && ((CourseCatalog)obj).InternalID == InternalID;
        }

        public override int GetHashCode() => InternalID.GetHashCode();
    }
}
