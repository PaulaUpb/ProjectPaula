namespace ProjectPaula.Model
{
    public class ConnectedCourse
    {
        public string CourseId { get; set; }
        public string CourseId2 { get; set; }

        protected bool Equals(ConnectedCourse other)
        {
            return string.Equals(CourseId, other.CourseId) && string.Equals(CourseId2, other.CourseId2);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ConnectedCourse) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (CourseId.GetHashCode() * 397) ^ CourseId2.GetHashCode();
            }
        }
    }


}
