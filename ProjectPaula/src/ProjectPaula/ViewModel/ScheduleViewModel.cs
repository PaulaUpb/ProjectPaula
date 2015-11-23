using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProjectPaula.Model;
using System.Collections.ObjectModel;
using ProjectPaula.DAL;
using ProjectPaula.Model.ObjectSynchronization;

namespace ProjectPaula.ViewModel
{
    public class ScheduleViewModel : BindableBase
    {

        private static readonly List<DayOfWeek> DaysOfWeek = new List<DayOfWeek>() {DayOfWeek.Monday, DayOfWeek.Tuesday,
            DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday};

        public List<string> HalfHourTimes { get; private set; }
        public ObservableCollection<Weekday> Weekdays { get; private set; }

        private string _searchQuery;
        public string SearchQuery
        {
            get { return _searchQuery; }
            set
            {
                _searchQuery = value;
                UpdateSearchResults();
            }
        }

        public ObservableCollection<SearchCourseViewModel> SearchResults { get; } = new ObservableCollection<SearchCourseViewModel>();

        private void UpdateSearchResults()
        {
            if (SearchQuery == null || SearchQuery.Count() < 3)
            {
                return;
            }
            SearchResults.Clear();
            
            var results = PaulRepository.GetLocalCourses(SearchQuery);
            
            foreach (var result in results)
            {
                SearchResults.Add(new SearchCourseViewModel(result.Name, result.Id));
            }
        }

        public void UpdateFrom(Schedule schedule)
        {
            var coursesForDay = new Dictionary<DayOfWeek, List<ViewModelMultiCourse>>();
            foreach (var dayOfWeek in DaysOfWeek)
            {
                if (!coursesForDay.ContainsKey(dayOfWeek))
                {
                    coursesForDay[dayOfWeek] = new List<ViewModelMultiCourse>();
                }

                for (var halfHourTime = 0; halfHourTime < schedule.HalfHourCount;)
                {
                    var multiCourse = GetCoursesAt(schedule, dayOfWeek, halfHourTime);
                    if (multiCourse != null)
                    {

                        coursesForDay[dayOfWeek].Add(multiCourse);
                        halfHourTime += multiCourse.LengthInHalfHours.Value;
                    }
                    else
                    {
                        coursesForDay[dayOfWeek].Add(new ViewModelMultiCourse(null, empty: true));
                        halfHourTime++;
                    }

                }

            }

            var weekdays = new[]
            {
                new Weekday(DayOfWeek.Monday, "Monday", coursesForDay[DayOfWeek.Monday]),
                new Weekday(DayOfWeek.Tuesday, "Tuesday", coursesForDay[DayOfWeek.Tuesday]),
                new Weekday(DayOfWeek.Wednesday, "Wednesday", coursesForDay[DayOfWeek.Wednesday]),
                new Weekday(DayOfWeek.Thursday, "Thursday", coursesForDay[DayOfWeek.Thursday]),
                new Weekday(DayOfWeek.Friday, "Friday", coursesForDay[DayOfWeek.Friday]),
                new Weekday(DayOfWeek.Saturday, "Saturday", coursesForDay[DayOfWeek.Saturday]),
                new Weekday(DayOfWeek.Sunday, "Sunday", coursesForDay[DayOfWeek.Sunday]),
            };

            HalfHourTimes = schedule.HalfHourTimes.Select(it => it.ToString("HH:mm")).ToList();
            Weekdays = new ObservableCollection<Weekday>(weekdays);
        }

        public static ScheduleViewModel CreateFrom(Schedule schedule)
        {
            var vm = new ScheduleViewModel();
            vm.UpdateFrom(schedule);
            return vm;
        }

        private static ViewModelMultiCourse GetCoursesAt(Schedule schedule, DayOfWeek dayOfWeek, int halfHour)
        {
            var timeToFind = schedule.EarliestTime.AddMinutes(30 * halfHour);

            var courses = schedule.DatesByDay[dayOfWeek];
            var startingDate = courses.ToList().Find(date => date.From.Hour == timeToFind.Hour && date.From.Minute == timeToFind.Minute);
            if (startingDate == null)
            {
                return null;
            }

            // We've found a matching course, now find overlapping courses
            var datesInFoundDateInterval = new List<ViewModelCourse> { ConvertToViewModelCourse(startingDate) };
            for (var i = 1; i < startingDate.LengthInHalfHours(); i++)
            {
                var overlappingTimeToFind = timeToFind.AddMinutes(i * 30);
                var overlappingDate =
                    courses.ToList().Find(
                        course => course.From.Hour == overlappingTimeToFind.Hour && course.From.Minute == overlappingTimeToFind.Minute);
                if (overlappingDate != null)
                {
                    datesInFoundDateInterval.Add(ConvertToViewModelCourse(overlappingDate));
                }
            }

            return new ViewModelMultiCourse(datesInFoundDateInterval, false);
        }

        private static ViewModelCourse ConvertToViewModelCourse(Date date)
        {
            return new ViewModelCourse(date.Course.Name, date.From, date.To);
        }


        public class Weekday : BindableBase
        {
            public DayOfWeek DayOfWeek { get; }
            public string Description { get; }
            public ObservableCollection<ViewModelMultiCourse> MultiCourses { get; }

            public Weekday(DayOfWeek dayOfWeek, string description, List<ViewModelMultiCourse> multiCourses)
            {
                DayOfWeek = dayOfWeek;
                Description = description;
                MultiCourses = new ObservableCollection<ViewModelMultiCourse>(multiCourses);
            }
        }

        public class ViewModelMultiCourse
        {
            public List<ViewModelCourse> Courses { get; }

            public DateTime? Begin => Courses?.Select(c => c.Begin).Min();

            public DateTime? End => Courses?.Select(c => c.End).Max();

            public int? LengthInHalfHours => End != null && Begin != null ? ((int)(End.Value.AtDate(Begin.Value.Day, Begin.Value.Month, Begin.Value.Year) - Begin).Value.TotalMinutes) / 30 : (int?)null;
            public bool Empty { get; }

            public ViewModelMultiCourse(List<ViewModelCourse> courses, bool empty)
            {
                Courses = courses;
                Empty = empty;

                if (courses != null)
                {
                    foreach (var viewModelCourse in courses)
                    {
                        viewModelCourse.HalfHourOffset = ((int)(viewModelCourse.Begin - Begin).Value.TotalMinutes) / 30;
                    }
                }
            }
        }


        public class ViewModelCourse
        {
            public string Title { get; }

            public string Time { get; }

            public DateTime Begin { get; }

            public DateTime End { get; }
            public int HalfHourOffset { get; set; }

            public int LengthInHalfHours => ((int)(End - Begin).TotalMinutes) / 30;

            public ViewModelCourse(string title, DateTime begin, DateTime end)
            {
                Title = title;
                Begin = begin;
                End = end;
                Time = $"{begin.ToString("t")} - {end.ToString("t")}";
            }
        }

        public class SearchCourseViewModel
        {
            public string Name { get; }

            public string Id { get; }

            public SearchCourseViewModel(string name, string id)
            {
                Name = name;
                Id = id;
            }
        }


    }
}
