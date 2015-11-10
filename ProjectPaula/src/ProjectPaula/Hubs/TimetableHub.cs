using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using ProjectPaula.ViewModel;

namespace ProjectPaula.Hubs
{
    public class TimetableHub : ObjectSynchronizationHub<IObjectSynchronizationClient>
    {
        public override Task OnConnected()
        {
            var synchronizedObject = SynchronizedObjects["Timetable"] ??
                                     SynchronizedObjects.Add("Timetable", TimetableViewModel.CreateFrom(new Timetable()));


            synchronizedObject.AddConnection(Context.ConnectionId);

            var timetableVM = (synchronizedObject.Object as TimetableViewModel);

            return base.OnConnected();
        }
    }
}
