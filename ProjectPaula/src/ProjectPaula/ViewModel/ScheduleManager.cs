﻿using Microsoft.Data.Entity;
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

        /// <summary>
        /// Loads the schedule with the specified ID either from
        /// cache or from the database.
        /// </summary>
        /// <param name="scheduleId">Schedule ID</param>
        /// <returns>
        /// A ViewModel representing the schedule or null if no
        /// schedule with the specified ID exists
        /// </returns>
        public SharedScheduleViewModel GetOrLoadSchedule(string scheduleId)
        {
            SharedScheduleViewModel scheduleVM;

            if (_loadedSchedules.TryGetValue(scheduleId, out scheduleVM))
            {
                return scheduleVM;
            }
            else
            {
                // TODO: Fix that string-to-int conversion crap

                int id;

                if (!int.TryParse(scheduleId, out id))
                    throw new NotImplementedException("Currently schedule IDs are integers, so the specified schedule ID string must be convertible to int");

                var schedule = PaulRepository.GetSchedule(id);
                
                if (schedule == null)
                {
                    // Schedule with that ID does not exist
                    return null;
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

        /// <summary>
        /// Creates a new empty schedule in the database.
        /// </summary>
        /// <returns>A ViewModel that represents the new schedule</returns>
        public async Task<SharedScheduleViewModel> CreateScheduleAsync()
        {
            // Create a new schedule in DB
            var schedule = new Schedule();
            await PaulRepository.StoreInDatabaseAsync(schedule, GraphBehavior.IncludeDependents);

            var scheduleId = schedule.Id.ToString();

            var vm = new SharedScheduleViewModel(schedule, scheduleId);
            _loadedSchedules.Add(scheduleId, vm);

            return vm;
        }
    }
}