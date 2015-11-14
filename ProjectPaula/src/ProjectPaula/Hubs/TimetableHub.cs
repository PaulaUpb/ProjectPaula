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

            var timetableVM = (synchronizedObject.Object as TimetableViewModel);
        }

        private async Task<TimetableViewModel> CreateViewModel()
        {
            var timetable = new Timetable();
            var sampleCourses = PaulRepository.GetLocalCourses("Grundlagen");
            timetable.AddCourse(sampleCourses[0]);
            timetable.AddCourse(sampleCourses[1]);
            timetable.AddCourse(sampleCourses[2]);
            timetable.AddCourse(sampleCourses[3]);
            timetable.AddCourse(sampleCourses[4]);
            timetable.AddCourse(sampleCourses[5]);
            timetable.AddCourse(sampleCourses[6]);
            var timetableViewModel = TimetableViewModel.CreateFrom(timetable);
            return timetableViewModel;
        }
    }
}
