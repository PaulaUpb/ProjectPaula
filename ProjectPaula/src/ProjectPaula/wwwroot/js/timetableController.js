﻿(function () {
    "use strict";

    function timetableController($scope, $location, $cookies, focus) {
        var vm = this;
        vm.title = "timetableController";
        vm.props = {};
        vm.props.IsConnected = false; // Indicates whether the SignalR connection is established
        vm.props.IsBusy = false; // Indicates whether any RPC call is in progress (buttons are disabled then)
        vm.props.IsRenamingSchedule = false; // Indicates whether the name is currently edited or not
        vm.props.ScheduleCreated = false; // Indicates whether a schedule has been created or not
        vm.props.NewScheduleName = ""; // The property used to rename the schedule
        vm.props.ScheduleId = ""; // The schedule ID entered by the user
        vm.props.UserName = ""; // The user name entered by the user
        vm.props.SearchQuery = ""; // The query string used to search for courses
        vm.props.SearchQueryChangeTimeout = null; // The currently active timeout initiated through a change to the search query
        vm.props.CourseCatalogId = ""; // The CourseCatalog ID (semester) which is used when creating a new schedule
        vm.props.DatesDialogContent = null; // Contains the CourseOverlapDetailViewModel for the selected course
        vm.props.VisitedSchedules = []; // The IDs of the schedules the user has already joined (read from cookie)
        vm.props.SelectedCourse = null; // The course that has been clicked (data context of #courseDialog)

        vm.funcs = {};
        vm.funcs.ComputeHalfHourTimes = function () {
            var tailoredSchedule = $.connection.timetableHub.synchronizedObjects.TailoredSchedule;
            if (tailoredSchedule === undefined) {
                return undefined;
            }

            var earliestHalfHour = tailoredSchedule.EarliestHalfHour;
            var latestHalfHour = tailoredSchedule.LatestHalfHour;

            var todayAtMidnight = new Date();
            todayAtMidnight.setHours(0, 0, 0, 0);

            var halfHourTimes = [];
            for (var halfHour = earliestHalfHour; halfHour < latestHalfHour; halfHour++) {
                var time = new Date(todayAtMidnight.getTime() + 30 * 60 * 1000 * halfHour);
                var minutes = time.getMinutes() > 9 ? time.getMinutes() : "0" + time.getMinutes();
                halfHourTimes.push(time.getHours() + ":" + minutes);
            }

            return halfHourTimes;
        }

        function activate() {

            // Get SignalR hub proxy
            var timetableProxy = $.connection.timetableHub;
            timetableProxy.logging = true;

            // Initialize object syncing on the timetable hub.
            // Pass the Angular $scope so that changes to synced objects can
            // be wrapped inside a $scope.$apply-call which triggers binding updates.
            // Pass true to enable logging.
            $.connection.initializeObjectSynchronization(timetableProxy, $scope, true);

            timetableProxy.synchronizedObjects.added("Public", function (publicVm) {
                vm.props.CourseCatalogId = publicVm.AvailableSemesters[0].InternalID;
            });

            timetableProxy.synchronizedObjects.added("User", function (userVm) {
                $scope.$apply(function () { vm.props.IsConnected = true; });
            });

            // In the Angular ViewModel put a reference to the container for synced objects
            vm.sync = timetableProxy.synchronizedObjects;

            History.Adapter.bind(window, "statechange", function () { // Note: We are using statechange instead of popstate
                var state = History.getState(); // Note: We are using History.getState() instead of event.state

                var urlParser = document.createElement("a");
                urlParser.href = state.url;
                var pathName = urlParser.pathname;
                if ((pathName === "" || pathName === "/") && urlParser.search === "" && vm.sync.User && (vm.sync.User.State === "JoinedSchedule" || vm.sync.User.State === "JoiningSchedule")) {
                    // We've reached the end of the history stack,
                    // the home page. Since no controller is registered
                    // to handle empty URL parameters,
                    // we need to handle this event here and load the desired URL
                    // manually by exiting the schedule
                    timetableProxy.server.exitSchedule();
                } else if (state.data.scheduleId) {
                    if (!vm.props.ScheduleCreated) {
                        beginJoinScheduleAndShowDialog(state.data.scheduleId);
                    } else {
                        vm.props.ScheduleCreated = false;
                    }

                }
            });

            // Define functions in Angular scope
            $scope.range = function (n) {
                return new Array(n);
            }

            function resetBusyFlag() {
                $scope.$apply(function () { vm.props.IsBusy = false; });
            }

            function focusElement(name) {
                setTimeout(function () { focus(name); }, 700);
            }

            function beginJoinScheduleAndShowDialog(scheduleId) {
                vm.props.IsBusy = true;
                timetableProxy.server.beginJoinSchedule(scheduleId).always(resetBusyFlag).fail(function () { History.back(); });
                vm.props.ScheduleId = scheduleId;
                $("#joinDialog").modal("show");
            }

            function saveVisitedSchedules() {
                var cookieContent = vm.props.VisitedSchedules.map(function (meta) { return meta.Id }).join();
                $cookies.put("schedules", cookieContent, {
                    'expires': "Fri, 31 Dec 9999 23:59:59 GMT"
                });
            }

            function addSchedule(scheduleId) {

                if (scheduleId === "") {
                    return;
                }

                // Check if schedule already in list of visited schedules
                for (var i = 0; i < vm.props.VisitedSchedules.length; i++) {
                    if (vm.props.VisitedSchedules[i].Id === scheduleId) {
                        return; // Do not add a second time
                    }
                }

                // Otherwise, add schedule and save to cookie
                vm.props.IsBusy = true;
                timetableProxy.server.getScheduleMetadata([scheduleId]).always(function (meta) {
                    resetBusyFlag();
                    $scope.$apply(function () {
                        vm.props.VisitedSchedules.push(meta[0]);
                        saveVisitedSchedules();
                    });
                });
            }

            function removeSchedule(scheduleId) {
                for (var i = 0; i < vm.props.VisitedSchedules.length; i++) {
                    if (vm.props.VisitedSchedules[i].Id === scheduleId) {
                        vm.props.VisitedSchedules.splice(i, 1);
                        saveVisitedSchedules();
                        break;
                    }
                }
            }

            // Focuses element with given name
            // Adds a schedule ID to the schedules cookie (if it does not yet exist)
            // Removes a schedule ID from the schedules cookie (if it exists)
            function loadVisitedSchedules() {
                var cookieContent = $cookies.get("schedules");

                if (cookieContent) {
                    vm.props.IsBusy = true;
                    var scheduleIds = cookieContent.split(",");
                    timetableProxy.server.getScheduleMetadata(scheduleIds).always(function (meta) {
                        resetBusyFlag();
                        $scope.$apply(function () {
                            vm.props.VisitedSchedules = meta;
                            saveVisitedSchedules();
                        });
                    });
                }
                else {
                    vm.props.VisitedSchedules = [];
                }
            }

            $scope.beginJoinSchedule = function (scheduleId) {
                vm.props.ScheduleId = scheduleId;
                History.pushState({ 'scheduleId': scheduleId, 'randomData': window.Math.random() }, "PAULa", "?ScheduleId=" + scheduleId);
                focusElement("nameInput");
            }

            $scope.completeJoinSchedule = function (userName) {
                vm.props.IsBusy = true;
                timetableProxy.server.completeJoinSchedule(userName).always(resetBusyFlag);
                addSchedule(vm.props.ScheduleId);
            }

            $scope.createSchedule = function (userName, catalogId) {
                vm.props.IsBusy = true;
                timetableProxy.server.createSchedule(userName, catalogId).always(function (scheduleId) {
                    resetBusyFlag();
                    vm.props.ScheduleCreated = true;
                    History.pushState({ 'scheduleId': scheduleId, 'randomData': window.Math.random() }, "PAULa", "?ScheduleId=" + scheduleId);
                    addSchedule(scheduleId);
                });
            }

            $scope.removeSchedule = function (scheduleId) {
                // Only removes the schedule ID from the cookie
                removeSchedule(scheduleId);
            }

            $scope.exitSchedule = function () {
                History.back();
            }

            $scope.beginRenameSchedule = function () {
                focusElement('scheduleNameInput');
                vm.props.NewScheduleName = vm.sync.SharedSchedule.Name;
                vm.props.IsRenamingSchedule = true;
            }

            $scope.completeRenameSchedule = function () {
                vm.props.IsBusy = true;
                timetableProxy.server.changeScheduleName(vm.props.NewScheduleName).always(function () {
                    resetBusyFlag();
                    $scope.$apply(function () {
                        vm.props.IsRenamingSchedule = false;
                        loadVisitedSchedules();
                    });
                });
            }

            $scope.addCourse = function (courseId) {
                vm.props.IsBusy = true;
                timetableProxy.server.addCourse(courseId).always(resetBusyFlag);
            }

            $scope.removeCourse = function (courseId) {
                vm.props.IsBusy = true;
                timetableProxy.server.removeCourse(courseId).always(resetBusyFlag);
            }

            $scope.removePendingTutorials = function (courseId) {
                vm.props.IsBusy = true;
                $scope.closeCoursePopover();
                timetableProxy.server.removePendingTutorials(courseId).always(resetBusyFlag);
            }

            $scope.addUserToCourse = function (courseId) {
                vm.props.IsBusy = true;
                $scope.closeCoursePopover();
                // Yes, this is the right call
                timetableProxy.server.addCourse(courseId).always(resetBusyFlag);
            }

            $scope.removeUserFromCourse = function (courseId) {
                vm.props.IsBusy = true;
                $scope.closeCoursePopover();
                timetableProxy.server.removeUserFromCourse(courseId, true).always(resetBusyFlag);
            }

            $scope.searchCourses = function (query) {
                vm.props.IsBusy = true;
                timetableProxy.server.searchCourses(query).always(resetBusyFlag);
            }

            $scope.searchQueryChanged = function (query) {
                // search query changed, so cancel the current timeout...
                if (vm.props.SearchQueryChangeTimeout !== null) {
                    window.clearTimeout(vm.props.SearchQueryChangeTimeout);
                    vm.props.SearchQueryChangeTimeout = null;
                }

                if (query.length >= 3) {
                    // ...and start a new timeout
                    vm.props.SearchQueryChangeTimeout = window.setTimeout(function () {
                        timetableProxy.server.searchCourses(query).always(resetBusyFlag);
                        vm.props.SearchQueryChangeTimeout = null;
                    }, 1000);
                } else {
                    // if search query is too short, we immediately clear the search results
                    // and show the catalog browsing UI again
                    vm.sync.Search.SearchResults = [];
                }
            }

            $scope.navigateToCourseCategory = function (categoryId) {
                vm.props.IsBusy = true;
                timetableProxy.server.navigateToCourseCategory(categoryId).always(resetBusyFlag);
            }

            $scope.exportSchedule = function () {
                timetableProxy.server.exportSchedule();
            }

            $scope.showDatesDialog = function (course) {

                vm.props.IsBusy = true;
                $scope.closeCoursePopover();

                timetableProxy.server.getCourseOverlapDetail(course.Id).always(function (overlapVM) {
                    resetBusyFlag();
                    $scope.$apply(function () {
                        vm.props.DatesDialogContent = overlapVM;
                        course.IsPopoverOpen = false;
                        $("#datesDialog").modal("show");
                    });
                });
            }

            $scope.showCoursePopover = function (course, event) {

                // Prevents top-most ng-click call to closeCoursePopover
                if (event) {
                    event.stopPropagation();
                }

                // If we clicked the same course that is already open,
                // close popover and remove highlightings
                if (course === vm.props.SelectedCourse) {
                    $scope.closeCoursePopover();
                    return;
                }

                // Close the currently opened popover
                if (vm.props.SelectedCourse !== null)
                    vm.props.SelectedCourse.IsPopoverOpen = false;

                vm.props.SelectedCourse = course;

                // Open popover of clicked course
                course.IsPopoverOpen = true;

                // Highlight all related courses, fade out non-related courses
                vm.sync.TailoredSchedule.Weekdays.forEach(function (weekday) {
                    weekday.CourseViewModelsByHour.forEach(function (hour) {
                        hour.forEach(function (c) {
                            c.IsHighlighted = (c.MainCourseId === course.MainCourseId);
                        });
                    });
                });
            }

            $scope.closeCoursePopover = function () {

                if (vm.props.SelectedCourse != null) {
                    vm.props.SelectedCourse.IsPopoverOpen = false;
                    vm.props.SelectedCourse = null;

                    vm.sync.TailoredSchedule.Weekdays.forEach(function (weekday) {
                        weekday.CourseViewModelsByHour.forEach(function (hour) {
                            hour.forEach(function (c) {
                                delete c.IsHighlighted;
                            });
                        });
                    });
                }
            }

            $scope.showAlternatives = function (courseId) {
                vm.props.IsBusy = true;
                $scope.closeCoursePopover();
                timetableProxy.server.showAlternatives(courseId).always(resetBusyFlag);
            }

            $scope.addTutorialsForCourse = function (courseId) {
                vm.props.IsBusy = true;
                $scope.closeCoursePopover();
                timetableProxy.server.addTutorialsForCourse(courseId).always(resetBusyFlag);
            }

            $scope.focusInput = function (name) {
                focusElement(name);
            }

            $scope.onCopyEvent = function (dialog, event) {
                var span = "";
                if (dialog === "JoinDialog") {
                    span = "joinDialogCopySpan";
                } else if (dialog === "ShareDialog") {
                    span = "shareDialogCopySpan";
                } else if (dialog === "ExportDialog") {
                    span = "exportDialogCopySpan"
                }

                if (span !== "") {
                    $("#" + span).text("Kopieren " + event + "!");
                    setTimeout(function () { $("#" + span).text("In Zwischenablage kopieren"); }, 2000);
                }

            }

            // Open the SignalR connection
            var browser = new UAParser().getBrowser();

            var onConnected = function () {
                $scope.$apply(function () {
                    //make sure visisted Schedules are loaded
                    loadVisitedSchedules();

                    // Parse URL parameters to join existing schedule
                    var urlParams = $location.search();
                    if (urlParams.ScheduleId) {
                        //Check if the base url was loaded before
                        var loaded = window.sessionStorage.getItem("baseLoaded");
                        if (!loaded) {
                            //Load base url and push state
                            History.replaceState({ 'randomData': window.Math.random() }, "PAULa", "/");
                            History.pushState({ 'scheduleId': urlParams.ScheduleId, 'randomData': window.Math.random() }, "PAULa", "?ScheduleId=" + urlParams.ScheduleId);
                            window.sessionStorage.setItem("baseLoaded", "loaded");
                        } else {
                            //Open join dialog
                            beginJoinScheduleAndShowDialog(urlParams.ScheduleId);
                        }

                    } else {
                        window.sessionStorage.setItem("baseLoaded", "loaded");
                    }
                });
            };

            if (browser.name.indexOf("IE") != -1 || browser.name.indexOf("Edge") != -1) {
                // For IE and Edge we use longPolling instead of foreverFrame
                // to hopefully increase performance
                $.connection.hub.start({ transport: 'longPolling' }).always(onConnected);
            } else {
                $.connection.hub.start().always(onConnected);
            }

            // Register callback for when client is disconnected from server
            $.connection.hub.disconnected(function () {
                $scope.$apply(function () {
                    // Mimic the actions of the server's ExitSchedule()
                    // (we can't call ExitSchedule() because we just disconnected)
                    vm.sync.User.State = "Default";
                    vm.sync.User.SharedScheduleVM = null;
                    vm.sync.User.TailoredScheduleVM = null;
                    vm.sync.User.SearchVM = null;
                    vm.sync.User.ExportVM = null;
                    vm.sync.User.Name = null;
                    vm.sync.User.Errors.StartPageMessage = "Die Verbindung wurde unterbrochen. Bitte aktualisiere die Seite und versuche es noch einmal.";
                });
            });

        }

        activate();
    }

    angular
        .module("timetableApp")
        .controller("timetableController", ["$scope", "$location", "$cookies", "focus", timetableController])
        .directive("paulaEnter", function () {
            // A custom Angular directive for enter keypresses in textboxes (http://stackoverflow.com/a/17472118)
            return function (scope, element, attrs) {
                element.bind("keydown keypress", function (event) {
                    if (event.which === 13) {
                        scope.$apply(function () {
                            scope.$eval(attrs.paulaEnter);
                        });

                        event.preventDefault();
                    }
                });
            };
        })
        .directive('paulaCourseList', function () {
            return {
                templateUrl: 'course.html',
                restrict: 'E',
                scope: {
                    courseList: '=',
                    addCourse: '&',
                    removeCourse: '&'
                },
                link: function (scope, elm, attrs) {
                    // don't ask, see http://stackoverflow.com/a/29023391
                    scope.addCourseTemplate = function (courseId) {
                        scope.addCourse()(courseId);
                    };
                    scope.removeCourseTemplate = function (courseId) {
                        scope.removeCourse()(courseId);
                    };
                }
            };
        })
})();
