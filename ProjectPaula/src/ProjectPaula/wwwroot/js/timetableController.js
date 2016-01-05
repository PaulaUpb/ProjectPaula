(function () {
    "use strict";

    function timetableController($scope, $location, $cookies, focus) {
        var vm = this;
        vm.title = "timetableController";
        vm.props = {};
        vm.props.IsConnected = false; // Indicates whether the SignalR connection is established
        vm.props.ScheduleId = ""; // The schedule ID entered by the user
        vm.props.UserName = ""; // The user name entered by the user
        vm.props.SearchQuery = ""; // The query string used to search for courses
        vm.props.CourseCatalogId = ""; // The CourseCatalog ID (semester) which is used when creating a new schedule
        vm.props.DatesDialogContent = { // Contains the dates for the dialog of the currently selected course
            datesList: []
        };
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

            // In the Angular ViewModel put a reference to the container for synced objects
            vm.sync = timetableProxy.synchronizedObjects;

            History.Adapter.bind(window, "statechange", function () { // Note: We are using statechange instead of popstate
                var state = History.getState(); // Note: We are using History.getState() instead of event.state

                var urlParser = document.createElement("a");
                urlParser.href = state.url;
                var pathName = urlParser.pathname;
                if (pathName === "" || pathName === "/") {
                    // We've reached the end of the history stack,
                    // the home page. Since no controller is registered
                    // to handle empty URL parameters,
                    // we need to handle this event here and load the desired URL
                    // manually
                    window.location = state.url;
                }
            });

            // Define functions in Angular scope
            $scope.range = function (n) {
                return new Array(n);
            }

            function focusElement(name) {
                setTimeout(function () { focus(name); }, 700);
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
                } // Check if schedule already in list of visited schedules

                for (var i = 0; i < vm.props.VisitedSchedules.length; i++) {
                    if (vm.props.VisitedSchedules[i].Id === scheduleId) {
                        return; // Do not add a second time
                    }
                }

                // Otherwise, add schedule and save to cookie
                timetableProxy.server.getScheduleMetadata([scheduleId]).done(function (meta) {
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
                    var scheduleIds = cookieContent.split(",");
                    timetableProxy.server.getScheduleMetadata(scheduleIds).done(function (meta) {
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
                History.pushState({ 'scheduleId': scheduleId }, scheduleId, "?ScheduleId=" + scheduleId);
                timetableProxy.server.beginJoinSchedule(scheduleId);
                focusElement("nameInput");
            }

            $scope.completeJoinSchedule = function (userName) {
                timetableProxy.server.completeJoinSchedule(userName);
                addSchedule(vm.props.ScheduleId);
            }

            $scope.createSchedule = function (userName, catalogId) {
                timetableProxy.server.createSchedule(userName, catalogId).done(function (scheduleId) {
                    History.pushState({ 'scheduleId': scheduleId }, scheduleId, "?ScheduleId=" + scheduleId);
                    addSchedule(scheduleId);
                });
            }

            $scope.removeSchedule = function (scheduleId) {
                // Only removes the schedule ID from the cookie
                removeSchedule(scheduleId);
            }

            $scope.exitSchedule = function () {
                timetableProxy.server.exitSchedule();
                History.back();
            }

            $scope.addCourse = function (courseId) {
                timetableProxy.server.addCourse(courseId);
            }

            $scope.removeCourse = function (courseId) {
                timetableProxy.server.removeCourse(courseId);
            }

            $scope.addUserToCourse = function (courseId) {
                // Yes, this is the right call
                timetableProxy.server.addCourse(courseId);
            }

            $scope.removeUserFromCourse = function (courseId) {
                timetableProxy.server.removeUserFromCourse(courseId);
            }

            $scope.searchCourses = function (query) {
                timetableProxy.server.searchCourses(query);
            }

            $scope.exportSchedule = function () {
                timetableProxy.server.exportSchedule();
            }

            $scope.showDatesDialog = function (course) {
                vm.props.DatesDialogContent.datesList = course.AllDates;
                $("#datesDialog").modal("show");
            }

            $scope.initializeCoursePopover = function (e, course) {
                // e is $event in ng-click
                vm.props.SelectedCourse = course;

                // TODO: Close all other popovers
            }

            $scope.showAlternatives = function (courseId) {
                timetableProxy.server.showAlternatives(courseId);
            }

            $scope.addTutorialsForCourse = function (courseId) {
                timetableProxy.server.addTutorialsForCourse(courseId)
            }

            $scope.focusInput = function (name) {
                focusElement(name);
            }

            // Open the SignalR connection
            $.connection.hub.start().done(function () {
                $scope.$apply(function () {
                    vm.props.IsConnected = true;

                    //make sure visisted Schedules are loaded
                    loadVisitedSchedules();


                    // Parse URL parameters to join existing schedule
                    var urlParams = $location.search();
                    if (urlParams.ScheduleId) {
                        History.pushState({ 'scheduleId': urlParams.ScheduleId }, urlParams.ScheduleId, "?ScheduleId=" + urlParams.ScheduleId);
                        timetableProxy.server.beginJoinSchedule(urlParams.ScheduleId);
                        vm.props.ScheduleId = urlParams.ScheduleId;
                        $("#joinDialog").modal("show");
                    }
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
}) ();
