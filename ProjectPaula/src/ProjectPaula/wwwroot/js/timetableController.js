(function () {
    'use strict';

    angular
        .module('timetableApp')
        .controller('timetableController', timetableController)
        .directive('paulaEnter', function () {
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
        });



    function timetableController($scope, $location, $cookies, focus) {
        var vm = this;
        vm.title = 'timetableController';
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

        function activate() {
            // Get SignalR hub proxy
            var timetableProxy = $.connection.timetableHub;
            timetableProxy.logging = true;

            // Initialize object syncing on the chat hub.
            // Pass the Angular $scope so that changes to synced objects can
            // be wrapped inside a $scope.$apply-call which triggers binding updates.
            // Pass true to enable logging.
            $.connection.initializeObjectSynchronization(timetableProxy, $scope, true);

            timetableProxy.synchronizedObjects.added("Public", function (publicVM) {
                vm.props.CourseCatalogId = publicVM.AvailableSemesters[0].InternalID;
            });

            // In the Angular ViewModel put a reference to the container for synced objects
            vm.sync = timetableProxy.synchronizedObjects;

            // Define functions in Angular scope
            $scope.range = function (n) {
                return new Array(n);
            }

            $scope.beginJoinSchedule = function (scheduleId) {
                vm.props.ScheduleId = scheduleId;
                timetableProxy.server.beginJoinSchedule(scheduleId);
                focusElement('nameInput');
            }

            $scope.completeJoinSchedule = function (userName) {
                timetableProxy.server.completeJoinSchedule(userName);
                addSchedule(vm.props.ScheduleId);
            }

            $scope.createSchedule = function (userName, catalogId) {
                timetableProxy.server.createSchedule(userName, catalogId).done(function (scheduleId) {
                    addSchedule(scheduleId);
                });
            }

            $scope.removeSchedule = function (scheduleId) {
                // Only removes the schedule ID from the cookie
                removeSchedule(scheduleId);
            }

            $scope.exitSchedule = function () {
                timetableProxy.server.exitSchedule();
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
                $('#datesDialog').modal('show');
            }

            $scope.showAlternatives = function (courseId) {
                timetableProxy.server.showAlternatives(courseId);
            }

            $scope.addTutorialsForCourse = function (courseId) {
                timetableProxy.server.addTutorialsForCourse(courseId)
            }

            $scope.loadVisitedSchedules = function () {
                loadVisitedSchedules();
            }

            $scope.getTextToCopy = function () {
                return $("code:first").text();
            }

            $scope.focusInput = function (name) {
                focusElement(name);
            }

            // Open the SignalR connection
            $.connection.hub.start().done(function () {
                $scope.$apply(function () {
                    vm.props.IsConnected = true;

                    // Parse URL parameters to join existing schedule
                    var urlParams = $location.search();
                    if (urlParams.ScheduleId) {
                        timetableProxy.server.beginJoinSchedule(urlParams.ScheduleId);
                        vm.props.ScheduleId = urlParams.ScheduleId;
                        $('#joinDialog').modal('show');
                    }
                });
            });


            //Focuses element with given name
            function focusElement(name) {
                setTimeout(function () { focus(name); }, 700);
            }

            // Adds a schedule ID to the schedules cookie (if it does not yet exist)
            function addSchedule(scheduleId) {

                if (scheduleId == "")
                    return;

                // Check if schedule already in list of visited schedules
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

            // Removes a schedule ID from the schedules cookie (if it exists)
            function removeSchedule(scheduleId) {
                for (var i = 0; i < vm.props.VisitedSchedules.length; i++) {
                    if (vm.props.VisitedSchedules[i].Id === scheduleId) {
                        vm.props.VisitedSchedules.splice(i, 1);
                        saveVisitedSchedules();
                        break;
                    }
                }
            }

            function loadVisitedSchedules() {
                var cookieContent = $cookies.get("schedules");

                if (cookieContent) {
                    var scheduleIds = cookieContent.split(",");
                    timetableProxy.server.getScheduleMetadata(scheduleIds).done(function (meta) {
                        $scope.$apply(function () {
                            vm.props.VisitedSchedules = meta;
                        });
                    });
                }
                else {
                    vm.props.VisitedSchedules = [];
                }
            }

            function saveVisitedSchedules() {
                var cookieContent = vm.props.VisitedSchedules.map(function (meta) { return meta.Id }).join();
                $cookies.put("schedules", cookieContent, { 'expires': 'Fri, 31 Dec 9999 23:59:59 GMT' });
            }
        }

        activate();
    }
})();
