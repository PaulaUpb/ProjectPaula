using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectPaula.DAL;
using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using ProjectPaula.ViewModel;
using ProjectPaula.Util;

namespace ProjectPaula.Hubs
{
    public class TimetableHub : ObjectSynchronizationHub<IObjectSynchronizationHubClient>
    {
        private UserViewModel CallingClient => ScheduleManager.Instance.Clients[Context.ConnectionId];

        public TimetableHub()
        {
            PaulRepository.UpdateStarting += async () =>
            {
                foreach (var c in ScheduleManager.Instance.Clients)
                {
                    //can't call ExitSchedule because it depends one the calling client
                    await c.Value.DisconnectAsync();
                    SynchronizedObjects[c.Key]["SharedSchedule"] = null;
                    SynchronizedObjects[c.Key]["TailoredSchedule"] = null;
                    SynchronizedObjects[c.Key]["Search"] = null;
                    SynchronizedObjects[c.Key]["Export"] = null;
                    SynchronizedObjects[c.Key]["CourseList"] = null;

                }
            };
        }

        public override async Task OnConnected()
        {
            await base.OnConnected();
            await ConnectClientAsync();
        }

        public override async Task OnReconnected()
        {
            await base.OnReconnected();
            await ConnectClientAsync();
        }

        private async Task ConnectClientAsync()
        {
            await ScheduleManager.Instance.AddClientAsync(Context.ConnectionId);
            // Begin synchronization of public VM and User VM
            CallerSynchronizedObjects["Public"] = await ScheduleManager.Instance.GetPublicViewModelAsync();
            CallerSynchronizedObjects["User"] = CallingClient;
        }

        public override async Task OnDisconnected(bool stopCalled)
        {
            await ScheduleManager.Instance.RemoveClientAsync(Context.ConnectionId);
            await base.OnDisconnected(stopCalled);
        }

        public void BeginJoinSchedule(string scheduleId)
        {
            using (var errorReporter = new ErrorReporter(s => CallingClient.Errors.ScheduleJoinMessage = s))
            {
                // This loads the SharedScheduleVM and assigns it to the client
                CallingClient.BeginJoinSchedule(scheduleId, errorReporter);
                // Begin synchronization of shared schedule VM
                CallerSynchronizedObjects["SharedSchedule"] = CallingClient.SharedScheduleVM;
            }
        }

        public async Task CompleteJoinSchedule(string userName)
        {
            using (var errorReporter = new ErrorReporter(s => CallingClient.Errors.ScheduleJoinMessage = s))
            {
                // This adds the client to the list of users and creates
                // a tailored schedule VM and a search VM
                await CallingClient.CompleteJoinScheduleAsync(userName, errorReporter);

                // Begin synchronization of tailored schedule VM and search VM
                CallerSynchronizedObjects["Search"] = CallingClient.SearchVM;
                CallerSynchronizedObjects["TailoredSchedule"] = CallingClient.TailoredScheduleVM;
                CallerSynchronizedObjects["Export"] = CallingClient.ExportVM;
                CallerSynchronizedObjects["CourseList"] = CallingClient.CourseListVM;
            }
        }

        public async Task<string> CreateSchedule(string userName, string catalogId)
        {
            using (var errorReporter = new ErrorReporter(s => CallingClient.Errors.ScheduleCreationMessage = s))
            {
                if (string.IsNullOrWhiteSpace(userName))
                {
                    errorReporter.Throw("Es wurde ein ungültiger Name eingegeben");
                }

                // Create a new schedule and make the user join it
                await CallingClient.CreateAndJoinScheduleAsync(userName, catalogId, errorReporter);

                // Begin synchronization of VMs
                CallerSynchronizedObjects["SharedSchedule"] = CallingClient.SharedScheduleVM;
                CallerSynchronizedObjects["TailoredSchedule"] = CallingClient.TailoredScheduleVM;
                CallerSynchronizedObjects["Search"] = CallingClient.SearchVM;
                CallerSynchronizedObjects["Export"] = CallingClient.ExportVM;
                CallerSynchronizedObjects["CourseList"] = CallingClient.CourseListVM;

                // Return ID of the new schedule
                return CallingClient.SharedScheduleVM.Schedule.Id;
            }
        }

        public async Task ExitSchedule()
        {
            await CallingClient.DisconnectAsync();

            // Remove VMs from client
            CallerSynchronizedObjects["SharedSchedule"] = null;
            CallerSynchronizedObjects["TailoredSchedule"] = null;
            CallerSynchronizedObjects["Search"] = null;
            CallerSynchronizedObjects["Export"] = null;
            CallerSynchronizedObjects["CourseList"] = null;

        }

        public void ExportSchedule()
        {
            CallingClient.ExportVM?.ExportSchedule(CallingClient.User);
        }

        /// <summary>
        /// RPC-method for optaining metadata, such as the course catalog,
        /// from schedules specified by their IDs.
        /// </summary>
        /// <param name="scheduleIds">Schedule IDs</param>
        public object[] GetScheduleMetadata(string[] scheduleIds)
        {
            var metadata = PaulRepository.GetSchedules(scheduleIds)
                .Select(schedule => new
                {
                    Id = schedule.Id,
                    Title = schedule.Name,
                    Users = string.Join(", ", schedule.Users.Select(user => user.Name))
                });

            return metadata.ToArray();
        }

        /// <summary>
        /// RPC-method for updating the searchQuery
        /// </summary>
        /// <param name="searchQuery"></param>
        public void SearchCourses(string searchQuery)
        {
            using (var errorReporter = new ErrorReporter(s => CallingClient.Errors.CourseSearchMessage = s))
            {
                if (CallingClient.SearchVM != null)
                {
                    CallingClient.SearchVM.SearchQuery = searchQuery;
                    CallingClient.SearchVM.UpdateSearchResults(errorReporter);
                }
            }
        }
        
        /// <summary>
        /// RPC-method for navigating to the course category with the specified ID.
        /// Used for the catalog browsing feature.
        /// </summary>
        /// <param name="categoryId"></param>
        public void NavigateToCourseCategory(int categoryId)
        {
            var category = PaulRepository.CategoryFilter.FirstOrDefault(c => c.ID == categoryId);
            if (category == null)
                CallingClient.SearchVM.CategoryBrowser.NavigateToRoot();
            else
                CallingClient.SearchVM.CategoryBrowser.Navigate(category);
        }

        /// <summary>
        /// RPC-method for showing the alternatives of a tutorial.
        /// The user is removed from the current tutorials and all tutorial
        /// alternatives are added as pending ones again.
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns></returns>
        public void ShowAlternatives(string courseId)
        {
            var course = PaulRepository.Courses.FirstOrDefault(c => c.Id == courseId);
            if (course == null)
            {
                throw new ArgumentException("Course not found", nameof(courseId));
            }

            var selectedCourses = CallingClient.SharedScheduleVM.Schedule.SelectedCourses.Select(it => it.Course);
            var parentCourse = course.FindParent(selectedCourses) ?? course.FindParent(PaulRepository.Courses);

            AddTutorialsForCourse(parentCourse.Id);
        }

        /// <summary>
        /// RPC wrapper for <see cref="ScheduleViewModel.RemovePendingTutorials(ProjectPaula.Model.Course, ErrorReporter)"/>.
        /// </summary>
        /// <param name="pendingTutorial"></param>
        public async Task RemovePendingTutorials(string courseId)
        {
            using (var errorReporter = new ErrorReporter(s => CallingClient.Errors.ScheduleMessage = s))
            {
                var pendingTutorial = PaulRepository.Courses.FirstOrDefault(c => c.Id == courseId);
                await CallingClient.SharedScheduleVM.TimetableHubSemaphore.WaitAsync();
                CallingClient.TailoredScheduleVM.RemovePendingTutorials(pendingTutorial, errorReporter);
                CallingClient.SharedScheduleVM.TimetableHubSemaphore.Release();
                CallingClient.TailoredScheduleVM.UpdateFrom(CallingClient.SharedScheduleVM.Schedule);
            }
        }

        /// <summary>
        /// RPC-method for adding a course to the schedule and for
        /// adding the calling user to a course that someone else
        /// has already added.
        /// </summary>
        /// <param name="courseId">Course ID</param>
        /// <returns></returns>
        public async Task AddCourse(string courseId)
        {
            using (var errorReporter = new ErrorReporter(s => CallingClient.Errors.ScheduleMessage = s))
            {
                var course = PaulRepository.Courses.FirstOrDefault(c => c.Id == courseId);

                if (course == null)
                {
                    errorReporter.Throw(
                        new ArgumentException("Course not found", nameof(courseId)),
                        UserErrorsViewModel.GenericErrorMessage);
                }


                var schedule = CallingClient.SharedScheduleVM.Schedule;

                await CallingClient.SharedScheduleVM.TimetableHubSemaphore.WaitAsync();
                try
                {
                    var selectedCourse = schedule.SelectedCourses.FirstOrDefault(c => c.CourseId == courseId);

                    if (course.IsTutorial)
                    {
                        // The user has decided to select a pending tutorial or one
                        // that was already selected by another user, so remove all pending
                        // tutorials of this course
                        CallingClient.TailoredScheduleVM.RemovePendingTutorials(course, errorReporter);

                        // Remove user from other possibly selected tutorial
                        var parentCourse = schedule.SelectedCourses.First(sel => sel.Course.AllTutorials.Contains(course));
                        var otherSelectedTutorial = parentCourse.Course.AllTutorials
                                                    .FirstOrDefault(tut => schedule.SelectedCourses.Any(sel => Equals(sel.Course, tut) && sel.Users.Select(it => it.User).Contains(CallingClient.User)));
                        if (otherSelectedTutorial != null)
                        {
                            await RemoveUserFromCourse(otherSelectedTutorial.Id, acquireSemaphore: false);
                        }


                        //If the user hasn't selected the parent course of the tutorial, it will be added here
                        if (!parentCourse.Users.Any(u => u.User.Name == CallingClient.Name))
                        {
                            var connectedCourses = parentCourse.Course.ConnectedCourses.Concat(new[] { parentCourse.Course });
                            foreach (var c in connectedCourses)
                            {
                                await PaulRepository.AddUserToSelectedCourseAsync(schedule.SelectedCourses.First(s => s.CourseId == c.Id), CallingClient.User);
                            }
                        }

                    }

                    if (selectedCourse == null)
                    {
                        var connectedCourses = course.ConnectedCourses.Concat(new[] { course })
                            .Select(it => PaulRepository.CreateSelectedCourse(schedule, CallingClient.User, it))
                            .ToList();

                        await PaulRepository.AddCourseToScheduleAsync(schedule, connectedCourses);
                        AddTutorialsForCourse(courseId);
                    }
                    else if (selectedCourse.Users.All(u => u.User != CallingClient.User))
                    {
                        // The course has already been added to the schedule by someone else.
                        // Add the calling user to the selected course (if not yet done).

                        await PaulRepository.AddUserToSelectedCourseAsync(selectedCourse, CallingClient.User);
                        var connectedCourseIds = selectedCourse.Course.ConnectedCourses.Select(it => it.Id).ToList();

                        var selectedConnectedCourses = schedule.SelectedCourses
                            .Where(selCo => connectedCourseIds.Contains(selCo.CourseId));

                        foreach (var connectedCourse in selectedConnectedCourses)
                        {
                            await PaulRepository.AddUserToSelectedCourseAsync(connectedCourse, CallingClient.User);
                        }
                    }

                    UpdateAddedStateInSearchResultsAndCourseList(course, isAdded: true);

                }
                finally
                {
                    CallingClient.SharedScheduleVM.TimetableHubSemaphore.Release();
                }
                UpdateTailoredViewModels();
            }
        }

        /// <summary>
        /// RPC-method for adding all tutorials of a 
        /// course as pending. This automatically updates
        /// the associated user's viewmodel.
        /// </summary>
        /// <param name="courseId"></param>
        public void AddTutorialsForCourse(string courseId)
        {
            var course = PaulRepository.Courses.Find(c => c.Id == courseId);
            var tutorials = course.AllTutorials;
            CallingClient.TailoredScheduleVM.AddPendingTutorials(tutorials);
            UpdateTailoredViewModels();
        }

        /// <summary>
        /// RPC-method for removing a course from the schedule
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns></returns>
        public async Task RemoveCourse(string courseId)
        {
            using (var errorReporter = new ErrorReporter(s => CallingClient.Errors.ScheduleMessage = s))
            {
                var course = PaulRepository.Courses.FirstOrDefault(c => c.Id == courseId);

                if (course == null)
                {
                    errorReporter.Throw(
                        new ArgumentException("Course not found", nameof(courseId)),
                        UserErrorsViewModel.GenericErrorMessage);
                }

                var schedule = CallingClient.SharedScheduleVM.Schedule;

                await CallingClient.SharedScheduleVM.TimetableHubSemaphore.WaitAsync();
                try
                {
                    var selectedCourse = schedule.SelectedCourses.FirstOrDefault(c => c.CourseId == courseId);
                    if (selectedCourse != null)
                    {

                        //Find selected Tutorials
                        var selectedTutorials = schedule.SelectedCourses
                            .Where(sel => selectedCourse.Course.Tutorials.Any(it => it.Id == sel.CourseId))
                            .Select(s => s.Course)
                            .ToList();

                        var courses = selectedCourse.Course.ConnectedCourses
                            .Concat(selectedTutorials)
                            .Concat(new[] { selectedCourse.Course });

                        foreach (var course1 in courses)
                        {
                            try
                            {

                                if (course1.Tutorials.Any())
                                {
                                    // Remove all pending tutorials from all TailoredSchedules
                                    foreach (var user in CallingClient.SharedScheduleVM.Users)
                                    {
                                        user.TailoredScheduleVM.RemovePendingTutorials(course1.Tutorials.FirstOrDefault(), errorReporter);
                                    }
                                }

                                await PaulRepository.RemoveCourseFromScheduleAsync(schedule, course1.Id);
                                UpdateAddedStateInSearchResultsAndCourseList(course1, isAdded: false);

                            }
                            catch (NullReferenceException e)
                            {
                                // This is just for purposes of compatibility
                                // with development versions. Can be safely removed
                                // after product launch
                                PaulRepository.AddLog(e.Message, FatilityLevel.Normal, typeof(TimetableHub).Name);
                            }
                        }

                        UpdateTailoredViewModels();
                    }
                    else if (course.IsTutorial)
                    {
                        // The user has decided to remove a tutorial before joining one
                        CallingClient.TailoredScheduleVM.RemovePendingTutorials(course, errorReporter);
                        UpdateTailoredViewModels();
                    }
                    else
                    {
                        errorReporter.Throw(
                            new ArgumentException("Course not found in the schedule!"),
                            UserErrorsViewModel.GenericErrorMessage);
                    }
                }
                finally
                {
                    CallingClient.SharedScheduleVM.TimetableHubSemaphore.Release();
                }
            }
        }

        /// <summary>
        /// Removes the calling user from the specified selected course.
        /// If after the removal no other user has selected the course,
        /// the course is removed from the schedule.
        /// </summary>
        /// <remarks>
        /// If the user has not selected the course with the specified ID,
        /// nothing happens.
        /// </remarks>
        /// <param name="courseId">Course ID</param>
        /// <returns></returns>
        public async Task RemoveUserFromCourse(string courseId, bool acquireSemaphore = true)
        {
            using (var errorReporter = new ErrorReporter(s => CallingClient.Errors.ScheduleMessage = s))
            {
                if (PaulRepository.Courses.All(c => c.Id != courseId))
                {
                    errorReporter.Throw(
                        new ArgumentException("Course not found", nameof(courseId)),
                        UserErrorsViewModel.GenericErrorMessage);
                }

                var schedule = CallingClient.SharedScheduleVM.Schedule;

                var selectedCourse = schedule.SelectedCourses
                    .FirstOrDefault(c => c.CourseId == courseId);

                if (selectedCourse == null)
                {
                    errorReporter.Throw(
                        new ArgumentException("Course not found in the schedule!"),
                        UserErrorsViewModel.GenericErrorMessage);
                }

                var selectedConnectedCourses = schedule.SelectedCourses
                    .Where(sel => selectedCourse.Course.ConnectedCourses.Any(it => it.Id == sel.CourseId))
                    .ToList();

                if (acquireSemaphore)
                {
                    await CallingClient.SharedScheduleVM.TimetableHubSemaphore.WaitAsync();
                }
                try
                {
                    var selectedCourseUser = selectedCourse.Users.FirstOrDefault(o => o.User == CallingClient.User);

                    //Find selected Tutorials
                    var selectedTutorials = schedule.SelectedCourses
                            .Where(sel => selectedCourse.Course.Tutorials
                                .Concat(selectedConnectedCourses.SelectMany(s => s.Course.Tutorials))
                                .Any(it => it.Id == sel.CourseId))
                            .ToList();

                    if (selectedCourseUser != null)
                    {
                        // Remove user from selected courses
                        foreach (var sel in selectedConnectedCourses.Concat(selectedTutorials).Concat(new[] { selectedCourse }))
                        {
                            await PaulRepository.RemoveUserFromSelectedCourseAsync(sel, selectedCourseUser);
                        }


                    }

                    if (!selectedCourse.Users.Any())
                    {

                        var firstTutorials = selectedCourse.Course.Tutorials.Take(1)
                            .Concat(selectedConnectedCourses.SelectMany(s => s.Course.Tutorials.Take(1)));

                        // Remove all Pending Tutorials from all TailoredSchedules
                        foreach (var user in CallingClient.SharedScheduleVM.Users)
                        {
                            foreach (var t in firstTutorials)
                            {
                                user.TailoredScheduleVM.RemovePendingTutorials(t, errorReporter);
                            }
                        }

                        // The course is no longer selected by anyone
                        // -> Remove the whole course from schedule
                        foreach (var sel in selectedConnectedCourses.Concat(selectedTutorials).Concat(new[] { selectedCourse }))
                        {
                            await PaulRepository.RemoveCourseFromScheduleAsync(schedule, sel.CourseId);
                        }

                    }

                    UpdateAddedStateInSearchResultsAndCourseList(selectedCourse.Course, isAdded: false);
                    UpdateTailoredViewModels();
                }
                finally
                {
                    if (acquireSemaphore)
                    {
                        CallingClient.SharedScheduleVM.TimetableHubSemaphore.Release();
                    }
                }
            }
        }

        /// <summary>
        /// Renames the schedule corresponding to the calling client.
        /// </summary>
        /// <param name="name">The new schedule name</param>
        public async Task ChangeScheduleName(string name)
        {
            using (var errorReporter = new ErrorReporter(s => CallingClient.Errors.ScheduleMessage = s))
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    errorReporter.Throw("Dieser Stundenplan-Name ist ungültig");
                }

                await CallingClient.SharedScheduleVM.ChangeScheduleName(name);
            }
        }

        public CourseOverlapDetailViewModel GetCourseOverlapDetail(string courseId)
        {
            using (var errorReporter = new ErrorReporter(s => CallingClient.Errors.ScheduleMessage = s))
            {
                if (PaulRepository.Courses.Any(c => c.Id == courseId))
                {
                    return new CourseOverlapDetailViewModel(CallingClient.TailoredScheduleVM, courseId);
                }
                else
                {
                    errorReporter.Throw(
                        new ArgumentException("Course not found"),
                        UserErrorsViewModel.GenericErrorMessage);
                    return null;
                }
            }
        }

        /// <summary>
        /// Updates the tailored VMs of all users that have joined
        /// the same schedule as the calling user in order to
        /// reflect changes made to the model objects.
        /// </summary>
        private void UpdateTailoredViewModels()
        {
            foreach (var user in CallingClient.SharedScheduleVM.Users)
            {
                user.TailoredScheduleVM.UpdateFrom(CallingClient.SharedScheduleVM.Schedule);
            }
        }

        private void UpdateAddedStateInSearchResultsAndCourseList(Course course, bool isAdded)
        {
            // Find the course in the search results of all users of the schedule
            var users = CallingClient.SharedScheduleVM.Users;
            var searchResults = users.SelectMany(u => u.SearchVM.SearchResults.Where(r => r.MainCourse.Id == course.Id));

            // Update added state in these search results
            foreach (var result in searchResults)
            {
                result.MainCourse.IsAdded = isAdded;
            }

            //Also update the course list 
            foreach (var u in users) u.CourseListVM.UpdateCourseList();
        }
    }
}
