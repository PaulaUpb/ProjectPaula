using System;
using System.Linq;
using System.Threading.Tasks;
using ProjectPaula.DAL;
using ProjectPaula.Model.ObjectSynchronization;
using ProjectPaula.ViewModel;

namespace ProjectPaula.Hubs
{
    public class TimetableHub : ObjectSynchronizationHub<IObjectSynchronizationHubClient>
    {
        private UserViewModel CallingClient => ScheduleManager.Instance.Clients[Context.ConnectionId];

        public override async Task OnConnected()
        {
            await base.OnConnected();
            ScheduleManager.Instance.AddClient(Context.ConnectionId);

            // Begin synchronization of User VM
            CallerSynchronizedObjects["User"] = CallingClient;
        }

        public override async Task OnDisconnected(bool stopCalled)
        {
            await ScheduleManager.Instance.RemoveClientAsync(Context.ConnectionId);
            await base.OnDisconnected(stopCalled);
        }

        public void BeginJoinSchedule(string scheduleID)
        {
            // This loads the SharedScheduleVM and assigns it to the client
            CallingClient.BeginJoinSchedule(scheduleID);

            // Begin synchronization of shared schedule VM
            CallerSynchronizedObjects["SharedSchedule"] = CallingClient.SharedScheduleVM;
        }

        public async Task CompleteJoinSchedule(string userName)
        {
            // This adds the client to the list of users and creates
            // a tailored schedule VM and a search VM
            await CallingClient.CompleteJoinScheduleAsync(userName);

            // Begin synchronization of tailored schedule VM and search VM
            CallerSynchronizedObjects["TailoredSchedule"] = CallingClient.TailoredScheduleVM;
            CallerSynchronizedObjects["Search"] = CallingClient.SearchVM;
        }

        public async Task CreateSchedule(string userName)
        {
            // Create a new schedule and make the user join it
            await CallingClient.CreateAndJoinScheduleAsync(userName);

            // Begin synchronization of VMs
            CallerSynchronizedObjects["SharedSchedule"] = CallingClient.SharedScheduleVM;
            CallerSynchronizedObjects["TailoredSchedule"] = CallingClient.TailoredScheduleVM;
            CallerSynchronizedObjects["Search"] = CallingClient.SearchVM;
        }

        /// <summary>
        /// RPC-method for updating the searchQuery
        /// </summary>
        /// <param name="searchQuery"></param>
        public void SearchCourses(string searchQuery)
        {
            if (CallingClient.SearchVM != null)
            {
                CallingClient.SearchVM.SearchQuery = searchQuery;
            }
        }

        /// <summary>
        /// RPC-method for adding a course to the schedule
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns></returns>
        public async Task AddCourse(string courseId)
        {
            if (PaulRepository.Courses.All(c => c.Id != courseId))
            {
                throw new ArgumentException("Course not found", nameof(courseId));
            }

            var schedule = CallingClient.SharedScheduleVM.Schedule;

            if (schedule.SelectedCourses.All(c => c.CourseId != courseId))
            {
                await PaulRepository.AddCourseToSchedule(schedule, courseId, schedule.User.Select(u => u.Id));
            }
            else
            {
                throw new ArgumentException("Course is already added!");
            }

            // TODO: Temporary solution: Update all the tailored schedule VMs.
            // In the future we should find an easier way to update schedules
            // on all clients at once.
            foreach (var scheduleVm in CallingClient.SharedScheduleVM.Users.Select(o => o.TailoredScheduleVM))
            {
                scheduleVm.UpdateFrom(schedule);
            }
        }

        /// <summary>
        /// RPC-method for removing a course from the schedule
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns></returns>
        public async Task RemoveCourse(string courseId)
        {
            if (PaulRepository.Courses.All(c => c.Id != courseId))
            {
                throw new ArgumentException("Course not found", nameof(courseId));
            }

            var schedule = CallingClient.SharedScheduleVM.Schedule;

            if (schedule.SelectedCourses.Any(c => c.CourseId == courseId))
            {
                await PaulRepository.RemoveCourseFromSchedule(schedule, courseId, schedule.User.Select(u => u.Id));
            }
            else
            {
                throw new ArgumentException("Course not found in the schedule!");
            }

            // TODO: Temporary solution: Update all the tailored schedule VMs.
            // In the future we should find an easier way to update schedules
            // on all clients at once.
            foreach (var scheduleVm in CallingClient.SharedScheduleVM.Users.Select(o => o.TailoredScheduleVM))
            {
                scheduleVm.UpdateFrom(schedule);
            }
        }

    }
}
