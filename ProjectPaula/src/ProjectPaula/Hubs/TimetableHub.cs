using System;
using System.Linq;
using System.Threading.Tasks;
using ProjectPaula.DAL;
using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using ProjectPaula.ViewModel;

namespace ProjectPaula.Hubs
{
    public class TimetableHub : ObjectSynchronizationHub<IObjectSynchronizationHubClient>
    {

        private static ScheduleViewModel scheduleViewModel;
        private static Schedule schedule = new Schedule();

        static TimetableHub()
        {
            var sampleCourses = PaulRepository.GetLocalCourses("Grundlagen").Select(c => new SelectedCourse() { CourseId = c.Id }).ToList();
            schedule.AddCourse(sampleCourses[0]);
            schedule.AddCourse(sampleCourses[1]);
            schedule.AddCourse(sampleCourses[2]);
            schedule.AddCourse(sampleCourses[3]);
            schedule.AddCourse(sampleCourses[4]);
            schedule.AddCourse(sampleCourses[5]);
            schedule.AddCourse(sampleCourses[6]);
            scheduleViewModel = ScheduleViewModel.CreateFrom(schedule);
        }

        public override async Task OnConnected()
        {
            await base.OnConnected();

            // Make the ScheduleViewModel available to the new client
            // using the key "Timetable"
            CallerSynchronizedObjects.Add("Timetable", scheduleViewModel);
        }

        public void SearchCourses(string course)
        {
            scheduleViewModel.SearchQuery = course;
        }

        public void AddCourse(string courseId)
        {
            var course = PaulRepository.Courses.FirstOrDefault(c => c.Id == courseId);
            if (course == null)
            {
                throw new ArgumentException("Course not found, wrong course id!");
            }
            
            // TODO Remove the following call
            // schedule.AddCourse(course);
            scheduleViewModel.UpdateFrom(schedule);
        }

    }
}
