﻿using System;
using System.Collections.Generic;
using System.Linq;
using ProjectPaula.Model;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using ProjectPaula.Model.ObjectSynchronization;

namespace ProjectPaula.ViewModel
{
    public class ScheduleViewModel : BindableBase
    {
        private static readonly List<DayOfWeek> DaysOfWeek = new List<DayOfWeek>()
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        };

        private ObservableCollection<Weekday> _weekdays;

        public ObservableCollection<string> HalfHourTimes { get; } = new ObservableCollection<string>();

        public ObservableCollection<Weekday> Weekdays
        {
            get { return _weekdays; }
            private set { Set(ref _weekdays, value); }
        }

        public void UpdateFrom(Schedule schedule)
        {
            var coursesForDay = new Dictionary<DayOfWeek, List<MultiCourseViewModel>>();
            var selectedCoursesByCourse = schedule.SelectedCourses.ToDictionary(selectedCourse => selectedCourse.Course);
            foreach (var dayOfWeek in DaysOfWeek)
            {
                if (!coursesForDay.ContainsKey(dayOfWeek))
                {
                    coursesForDay[dayOfWeek] = new List<MultiCourseViewModel>();
                }

                for (var halfHourTime = 0; halfHourTime < schedule.HalfHourCount;)
                {
                    var multiCourse = GetCoursesAt(schedule, selectedCoursesByCourse, dayOfWeek, halfHourTime);
                    if (multiCourse != null)
                    {

                        coursesForDay[dayOfWeek].Add(multiCourse);
                        halfHourTime += multiCourse.LengthInHalfHours.Value;
                    }
                    else
                    {
                        coursesForDay[dayOfWeek].Add(new MultiCourseViewModel());
                        halfHourTime++;
                    }

                }

            }

            var weekdays = new List<Weekday>
            {
                new Weekday(DayOfWeek.Monday, "Monday", coursesForDay[DayOfWeek.Monday]),
                new Weekday(DayOfWeek.Tuesday, "Tuesday", coursesForDay[DayOfWeek.Tuesday]),
                new Weekday(DayOfWeek.Wednesday, "Wednesday", coursesForDay[DayOfWeek.Wednesday]),
                new Weekday(DayOfWeek.Thursday, "Thursday", coursesForDay[DayOfWeek.Thursday]),
                new Weekday(DayOfWeek.Friday, "Friday", coursesForDay[DayOfWeek.Friday]),
            };
            if (coursesForDay[DayOfWeek.Saturday].Any(x => !x.Empty) || coursesForDay[DayOfWeek.Sunday].Any(x => !x.Empty))
            {
                weekdays.Add(new Weekday(DayOfWeek.Saturday, "Saturday", coursesForDay[DayOfWeek.Saturday]));
            }
            if (coursesForDay[DayOfWeek.Sunday].Any(x => !x.Empty))
            {
                weekdays.Add(new Weekday(DayOfWeek.Sunday, "Sunday", coursesForDay[DayOfWeek.Sunday]));
            }

            HalfHourTimes.Clear();
            HalfHourTimes.AddRange(schedule.HalfHourTimes.Select(it => it.ToString("HH:mm")).ToList());
            Weekdays = new ObservableCollection<Weekday>(weekdays);
        }

        public static ScheduleViewModel CreateFrom(Schedule schedule)
        {
            var vm = new ScheduleViewModel();
            vm.UpdateFrom(schedule);
            return vm;
        }

        private static MultiCourseViewModel GetCoursesAt(Schedule schedule, IDictionary<Course, SelectedCourse> selectedCoursesByCourse, DayOfWeek dayOfWeek, int halfHour)
        {
            var timeToFind = schedule.EarliestTime.AddMinutes(30 * halfHour);

            var courses = schedule.DatesByDay[dayOfWeek];
            var courseList = courses.ToList();
            var startingDate = courseList.Find(date => date.From.Hour == timeToFind.Hour && date.From.Minute == timeToFind.Minute);
            if (startingDate == null)
            {
                return null;
            }

            // We've found a matching course, now find overlapping courses
            var datesInFoundDateInterval = new List<CourseViewModel> { ConvertToViewModelCourse(startingDate, selectedCoursesByCourse[startingDate.Course]) };
            // Ensure we're not adding our originally found course as an overlapping course
            courseList.Remove(startingDate);
            for (var i = 0; i < startingDate.LengthInHalfHours(); i++)
            {
                var overlappingTimeToFind = timeToFind.AddMinutes(i * 30);
                var overlappingDate =
                    courseList.Find(
                        course => course.From.Hour == overlappingTimeToFind.Hour && course.From.Minute == overlappingTimeToFind.Minute);
                if (overlappingDate != null)
                {
                    datesInFoundDateInterval.Add(ConvertToViewModelCourse(overlappingDate, selectedCoursesByCourse[overlappingDate.Course]));
                }
            }

            return new MultiCourseViewModel(datesInFoundDateInterval);
        }

        private static CourseViewModel ConvertToViewModelCourse(Date date, SelectedCourse course)
        {
            if (date.Course.Id != course.Course.Id)
            {
                throw new ArgumentException("SelectedCourse doesn't match course in the date");
            }
            return new CourseViewModel(date.Course.Id, date.Course.Name, date.From, date.To, course.Users.Select(x => x.User.Name).ToList());
        }

        public class Weekday : BindableBase
        {
            public DayOfWeek DayOfWeek { get; }
            public string Description { get; }
            public ObservableCollection<MultiCourseViewModel> MultiCourses { get; }

            public Weekday(DayOfWeek dayOfWeek, string description, List<MultiCourseViewModel> multiCourses)
            {
                DayOfWeek = dayOfWeek;
                Description = description;
                MultiCourses = new ObservableCollection<MultiCourseViewModel>(multiCourses);
            }
        }

        public class MultiCourseViewModel
        {
            public List<CourseViewModel> Courses { get; }

            public DateTime? Begin => Courses?.Select(c => c.Begin).Min();

            public DateTime? End => Courses?.Select(c => c.End).Max();

            public int? LengthInHalfHours => End != null && Begin != null ? ((int)(End.Value.AtDate(Begin.Value.Day, Begin.Value.Month, Begin.Value.Year) - Begin).Value.TotalMinutes) / 30 : (int?)null;

            public bool Empty => Courses == null || !Courses.Any();

            public MultiCourseViewModel(IEnumerable<CourseViewModel> courses = null)
            {
                Courses = courses?.ToList();

                if (courses != null)
                {
                    foreach (var viewModelCourse in courses)
                    {
                        viewModelCourse.HalfHourOffset = ((int)(viewModelCourse.Begin - Begin).Value.TotalMinutes) / 30;
                    }
                }
            }
        }

        public class CourseViewModel
        {
            public string Title { get; }

            public string Time { get; }

            public DateTime Begin { get; }

            public DateTime End { get; }
            public string Users { get; }
            public int HalfHourOffset { get; set; }

            public int LengthInHalfHours => ((int)(End - Begin).TotalMinutes) / 30;

            public string Id { get; }

            public CourseViewModel(string id, string title, DateTime begin, DateTime end, IEnumerable<string> users)
            {
                Title = title;
                Begin = begin;
                End = end;
                Users = users.JoinToString(separator: ", ");
                Time = $"{begin.ToString("t")} - {end.ToString("t")}";
                Id = id;
            }
        }
    }
}
