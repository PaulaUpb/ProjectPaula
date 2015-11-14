using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProjectPaula.DAL;
using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using ProjectPaula.ViewModel;

namespace ProjectPaula.Hubs
{
    public class TimetableHub : ObjectSynchronizationHub<IObjectSynchronizationClient>
    {
        public override async Task OnConnected()
        {

            var synchronizedObject = SynchronizedObjects["Timetable"] ??
                                     SynchronizedObjects.Add("Timetable", await CreateViewModel());


            synchronizedObject.AddConnection(Context.ConnectionId);

            var timetableVM = (synchronizedObject.Object as ScheduleViewModel);
        }

        private async Task<ScheduleViewModel> CreateViewModel()
        {
            var schedule = new Schedule();
            var sampleCourses = PaulRepository.GetLocalCourses("Grundlagen");
            schedule.AddCourse(sampleCourses[0]);
            schedule.AddCourse(sampleCourses[1]);
            schedule.AddCourse(sampleCourses[2]);
            schedule.AddCourse(sampleCourses[3]);
            schedule.AddCourse(sampleCourses[4]);
            schedule.AddCourse(sampleCourses[5]);
            schedule.AddCourse(sampleCourses[6]);
            var timetableViewModel = ScheduleViewModel.CreateFrom(schedule);
            return timetableViewModel;
        }
    }
}
