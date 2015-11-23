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
        public override async Task OnConnected()
        {
            await base.OnConnected();
            ScheduleManager.Instance.AddClient(Context.ConnectionId);
        }

        public override async Task OnDisconnected(bool stopCalled)
        {
            ScheduleManager.Instance.RemoveClient(Context.ConnectionId);
            await base.OnDisconnected(stopCalled);
        }

        public void BeginJoinSchedule(string scheduleID)
        {
            var client = ScheduleManager.Instance.Clients[Context.ConnectionId];

            // This loads the SharedScheduleVM and assigns it to the client
            client.BeginJoinSchedule(scheduleID);

            // Begin sync of shared schedule VM
            CallerSynchronizedObjects["SharedSchedule"] = client.SharedScheduleVM;
        }

        public void CompleteJoinSchedule(string userName)
        {
            var client = ScheduleManager.Instance.Clients[Context.ConnectionId];

            // This adds the client to the list of users and creates
            // a tailored schedule VM and a search VM
            client.CompleteJoinSchedule(userName);

            // Begin sync of user VM, tailored schedule VM and search VM
            CallerSynchronizedObjects["User"] = client;
            CallerSynchronizedObjects["TailoredSchedule"] = client.TailoredScheduleVM;
            CallerSynchronizedObjects["Search"] = client.SearchVM;
        }

        public void SearchCourses(string searchQuery)
        {
            var client = ScheduleManager.Instance.Clients[Context.ConnectionId];

            if (client.SearchVM != null)
                client.SearchVM.SearchQuery = searchQuery;
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
            //scheduleViewModel.UpdateFrom(schedule);
        }

    }
}
