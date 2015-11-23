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
        private static Schedule schedule;

        static TimetableHub()
        {
            var schedules = PaulRepository.GetSchedules();
            if (!schedules.Any())
            {
                schedule = new Schedule();
                var sampleCourses = PaulRepository.GetLocalCourses("Grundlagen").Select(c => new SelectedCourse() { CourseId = c.Id }).ToList();
                schedule.AddCourse(sampleCourses[0]);
                schedule.AddCourse(sampleCourses[1]);
                schedule.AddCourse(sampleCourses[2]);
                schedule.AddCourse(sampleCourses[3]);
                schedule.AddCourse(sampleCourses[4]);
                schedule.AddCourse(sampleCourses[5]);
                schedule.AddCourse(sampleCourses[6]);
                PaulRepository.StoreInDatabase(schedule, Microsoft.Data.Entity.GraphBehavior.IncludeDependents);
            }
            else
            {
                schedule = schedules.First();
            }
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

        public async Task AddCourse(string courseId)
        {
            var course = PaulRepository.Courses.FirstOrDefault(c => c.Id == courseId);
            if (course == null)
            {
                throw new ArgumentException("Course not found, wrong course id!");
            }
            
            if (!schedule.SelectedCourses.Any(c => c.CourseId == courseId))
            {
                await PaulRepository.AddCourseToSchedule(schedule, courseId, schedule.User.Select(u => u.Id));
            }
            //schedule.AddCourse(course);
            scheduleViewModel.UpdateFrom(schedule);
        }

    }
}
