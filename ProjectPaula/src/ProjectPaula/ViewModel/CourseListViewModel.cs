using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using System.Collections.ObjectModel;
using System.Linq;

namespace ProjectPaula.ViewModel
{
    public class CourseListViewModel : BindableBase
    {
        private readonly User _user;
        private readonly Schedule _schedule;
        
        // Reuse the SearchResultViewModel
        public ObservableCollectionEx<SearchResultViewModel> SelectedCourses { get; } = new ObservableCollectionEx<SearchResultViewModel>();

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
                .Select(s => new SearchResultViewModel(s.Course, true));

            SelectedCourses.AddRange(courses);
        }
    }
}
