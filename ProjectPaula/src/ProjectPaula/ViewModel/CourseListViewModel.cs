using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.ViewModel
{
    public class CourseListViewModel : BindableBase
    {
        //Reuse the SearchResultViewModel
        public ObservableCollectionEx<SearchResultViewModel> SelectedCourses { get; } 
            = new ObservableCollectionEx<SearchResultViewModel>();

        private User _user;
        private Schedule _schedule;


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
                .Where(s => s.Users.Any(u => u.User == _user) && !s.Course.IsConnectedCourse).Select(s => s.Course)
                .OrderBy(c => c.Name);
            SelectedCourses.AddRange(courses.Select(c => new SearchResultViewModel(c, true)));
        }

    }
}
