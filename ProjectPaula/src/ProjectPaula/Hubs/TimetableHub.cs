﻿using System;
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
        private static Lazy<ScheduleViewModel> _globalScheduleVM = new Lazy<ScheduleViewModel>(() => CreateViewModel());

        public override async Task OnConnected()
        {
            await base.OnConnected();

            // Make the ScheduleViewModel available to the new client
            // using the key "Timetable"
            CallerSynchronizedObjects.Add("Timetable", _globalScheduleVM.Value);
        }

        private static ScheduleViewModel CreateViewModel()
        {
            var schedule = new Schedule();
            var sampleCourses = PaulRepository.GetLocalCourses("Grundlagen").Select(c => new SelectedCourse() { Course = c }).ToList();
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
