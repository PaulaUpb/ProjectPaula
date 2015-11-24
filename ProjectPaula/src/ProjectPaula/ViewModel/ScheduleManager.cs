using Microsoft.Data.Entity;
using ProjectPaula.DAL;
using ProjectPaula.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.ViewModel
{
    /// <summary>
    /// Handles loading and unloading of schedules, adding and removing of
    /// users to/from schedules and creation and deletion of appropiate ViewModels.
    /// </summary>
    public class ScheduleManager
    {
        private static readonly Lazy<ScheduleManager> _scheduleManager =
            new Lazy<ScheduleManager>(() => new ScheduleManager());

        /// <summary>
        /// Gets the singleton <see cref="ScheduleManager"/> instance.
        /// </summary>
        public static ScheduleManager Instance => _scheduleManager.Value;

        private readonly Dictionary<string, SharedScheduleViewModel> _loadedSchedules = new Dictionary<string, SharedScheduleViewModel>();
        private readonly Dictionary<string, UserViewModel> _connectedClients = new Dictionary<string, UserViewModel>();

        public IReadOnlyDictionary<string, UserViewModel> Clients => _connectedClients;

        public UserViewModel AddClient(string connectionId)
        {
            var client = new UserViewModel(this, connectionId);
            _connectedClients.Add(connectionId, client);
            return client;
        }

        /// <summary>
        /// Makes the user leave the schedule he/she is working on (if applicable)
        /// and removes the client.
        /// </summary>
        /// <param name="connectionId">Connection ID</param>
        /// <returns>Task</returns>
        public async Task<bool> RemoveClientAsync(string connectionId)
        {
            UserViewModel user;

            if (_connectedClients.TryGetValue(connectionId, out user))
            {
                // Disconnect from schedule and clean-up
                await user.DisconnectAsync();
            }

            return _connectedClients.Remove(connectionId);
        }

        public UserViewModel GetClient(string connectionId)
            => _connectedClients[connectionId];

        public SharedScheduleViewModel GetOrLoadSchedule(string scheduleId)
        {
            SharedScheduleViewModel scheduleVM;

            if (_loadedSchedules.TryGetValue(scheduleId, out scheduleVM))
            {
                return scheduleVM;
            }
            else
            {
                // For testing purposes, create a mock VM utilizing the first schedule
                // stored in the database. That means that - for now - everyone is still
                // working on the same schedule.
                // TODO: Load appropiate schedule from database
                var schedules = PaulRepository.GetSchedules();
                Schedule schedule;

                if (!schedules.Any())
                {
                    // TODO: This does not work as intended because no User is assigned

                    schedule = new Schedule();
                    var sampleCourses = PaulRepository.GetLocalCourses("Grundlagen").Select(c => new SelectedCourse() { CourseId = c.Id }).ToList();
                    schedule.AddCourse(sampleCourses[0]);
                    schedule.AddCourse(sampleCourses[1]);
                    schedule.AddCourse(sampleCourses[2]);
                    schedule.AddCourse(sampleCourses[3]);
                    schedule.AddCourse(sampleCourses[4]);
                    schedule.AddCourse(sampleCourses[5]);
                    schedule.AddCourse(sampleCourses[6]);
                    PaulRepository.StoreInDatabase(schedule, GraphBehavior.IncludeDependents);
                }
                else
                {
                    schedule = schedules.First();
                }
                
                var vm = new SharedScheduleViewModel(schedule, scheduleId);
                _loadedSchedules.Add(scheduleId, vm);
                return vm;
            }
        }

        public async Task SaveScheduleAsync(SharedScheduleViewModel scheduleVM)
        {
            await PaulRepository.StoreInDatabaseAsync(scheduleVM.Schedule, GraphBehavior.IncludeDependents);

            if (!scheduleVM.Users.Any())
            {
                // Schedule no longer in use -> unload it
                _loadedSchedules.Remove(scheduleVM.Id);
            }
        }
    }
}
