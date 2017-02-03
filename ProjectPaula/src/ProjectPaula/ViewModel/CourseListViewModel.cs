using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using System.Collections.ObjectModel;
using System.Linq;

namespace ProjectPaula.ViewModel
{
    public class CourseListViewModel : BindableBase
    {
        //Reuse the SearchResultViewModel
        public ObservableCollectionEx<SearchResultViewModel> SelectedCourses { get; } 
            = new ObservableCollectionEx<SearchResultViewModel>();

        private readonly User _user;
        private readonly Schedule _schedule;


        public CourseListViewModel(Schedule schedule, User user)
        {
            _user = user;
            _schedule = schedule;
            UpdateCourseList();
        }

        public void UpdateCourseList()
        {
            SelectedCourses.Clear();
            var courses = _schedule.SelectedCourses
                .Where(s => s.Users.Any(u => u.User == _user) && !s.Course.IsConnectedCourse)
                .Select(s => s.Course)
                .OrderBy(c => c.Name);
            SelectedCourses.AddRange(courses.Select(c => new SearchResultViewModel(c, true)));
        }
    }
}
