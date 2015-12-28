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

    function timetableController($scope, $location, $cookies) {
        var vm = this;
        vm.title = 'timetableController';
        vm.props = {};
        vm.props.IsConnected = false; // Indicates whether the SignalR connection is established
        vm.props.ScheduleId = ""; // The schedule ID entered by the user
        vm.props.UserName = ""; // The user name entered by the user
        vm.props.SearchQuery = ""; // The query string used to search for courses
        vm.props.CourseCatalogId = ""; // The CourseCatalog ID (semester) which is used when creating a new schedule
        vm.props.DatesDialogContent = {
            datesList: []
        };
        vm.props.VisitedSchedules = [];
        var schedules = $cookies.get("schedules");
        if (schedules) {
            vm.props.VisitedSchedules = schedules.split(",");
        }


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

            $scope.beginJoinSchedule = function (scheduleID) {
                timetableProxy.server.beginJoinSchedule(scheduleID);

            }

            $scope.completeJoinSchedule = function (userName) {
                timetableProxy.server.completeJoinSchedule(userName);
                var schedules = $cookies.get("schedules");
                if (schedules) {
                    if ($.inArray(vm.props.ScheduleId, vm.props.VisitedSchedules, 0) == -1) {
                        schedules += "," + vm.props.ScheduleId;
                    }
                } else {
                    schedules = vm.props.ScheduleId;
                }
                $cookies.put("schedules", schedules);
                vm.props.VisitedSchedules = schedules.split(",");
                vm.props.ScheduleId = "";
            }

            $scope.createSchedule = function (userName, catalogId) {
                timetableProxy.server.createSchedule(userName, catalogId);
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

            // Open the SignalR connection
            $.connection.hub.start().done(function () {
                $scope.$apply(function () {
                    vm.props.IsConnected = true;

                    var urlParams = $location.search();
                    if (urlParams.ScheduleId) {
                        timetableProxy.server.beginJoinSchedule(urlParams.ScheduleId);
                        $('#joinDialog').modal('show');
                    }
                });
            });
        }

        activate();
    }
})();
