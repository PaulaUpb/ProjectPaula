using ProjectPaula.DAL;
using ProjectPaula.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        private readonly SemaphoreSlim _syncSemaphore = new SemaphoreSlim(1);
        private readonly Dictionary<string, SharedScheduleViewModel> _loadedSchedules = new Dictionary<string, SharedScheduleViewModel>();
        private readonly Dictionary<string, UserViewModel> _connectedClients = new Dictionary<string, UserViewModel>();
        private Lazy<Task<SharedPublicViewModel>> _publicVM = new Lazy<Task<SharedPublicViewModel>>(SharedPublicViewModel.CreateAsync);

        public IReadOnlyDictionary<string, UserViewModel> Clients => _connectedClients;

        public async Task<UserViewModel> AddClientAsync(string connectionId)
        {
            await _syncSemaphore.WaitAsync();

            try
            {
                UserViewModel existingUser;

                if (_connectedClients.TryGetValue(connectionId, out existingUser))
                {
                    // Client already added -> return existing UserVM
                    return existingUser;
                }
                else
                {
                    // Create new UserVM
                    var client = new UserViewModel(this, connectionId);
                    _connectedClients.Add(connectionId, client);
                    (await GetPublicViewModelAsync()).ClientCount++;
                    return client;
                }
            }
            finally
            {
                _syncSemaphore.Release();
            }
        }

        /// <summary>
        /// Makes the user leave the schedule he/she is working on (if applicable)
        /// and removes the client.
        /// </summary>
        /// <param name="connectionId">Connection ID</param>
        /// <returns>Task</returns>
        public async Task<bool> RemoveClientAsync(string connectionId)
        {
            await _syncSemaphore.WaitAsync();

            try
            {
                UserViewModel user;

                if (_connectedClients.TryGetValue(connectionId, out user))
                {
                    // Disconnect from schedule and clean-up
                    await user.DisconnectAsync();
                    (await GetPublicViewModelAsync()).ClientCount--;
                }

                return _connectedClients.Remove(connectionId);
            }
            finally
            {
                _syncSemaphore.Release();
            }
        }

        public UserViewModel GetClient(string connectionId)
            => _connectedClients[connectionId];

        public async Task<SharedPublicViewModel> GetPublicViewModelAsync()
            => await _publicVM.Value;

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
            SharedScheduleViewModel scheduleVm;

            if (_loadedSchedules.TryGetValue(scheduleId, out scheduleVm))
            {
                return scheduleVm;
            }
            else
            {
                // TODO: Fix that string-to-int conversion crap
                var schedule = PaulRepository.GetSchedule(scheduleId);

                if (schedule == null)
                {
                    // Schedule with that ID does not exist
                    return null;
                }

                var vm = new SharedScheduleViewModel(schedule);
                _loadedSchedules.Add(scheduleId, vm);
                return vm;
            }
        }

        public async Task SaveScheduleAsync(SharedScheduleViewModel scheduleVM)
        {
            await Task.FromResult(0);

            //TODO: Figure out a way to update entities in database (including Schedules)
            //await PaulRepository.StoreScheduleInDatabase(scheduleVM.Schedule);

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
        public async Task<SharedScheduleViewModel> CreateScheduleAsync(string catalogId, ErrorReporter errorReporter)
        {
            var catalogs = await PaulRepository.GetCourseCataloguesAsync();
            var selectedCatalog = catalogs.FirstOrDefault(o => o.InternalID == catalogId);

            if (selectedCatalog == null)
            {
                errorReporter.Throw(
                    new ArgumentException($"A CourseCatalog with the specified ID '{catalogId}' does not exist"),
                    UserErrorsViewModel.GenericErrorMessage);
            }

            // Create a new schedule in DB
            var schedule = await PaulRepository.CreateNewScheduleAsync(selectedCatalog);

            var vm = new SharedScheduleViewModel(schedule);
            _loadedSchedules.Add(schedule.Id, vm);

            return vm;
        }
    }
}
