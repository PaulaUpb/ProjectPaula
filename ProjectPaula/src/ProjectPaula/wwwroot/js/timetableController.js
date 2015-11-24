(function () {
    'use strict';

    angular
        .module('timetableApp')
        .controller('timetableController', timetableController);

    function timetableController($scope) {
        /* jshint validthis:true */
        var vm = this;
        vm.title = 'timetableController';
        vm.props = {};
        vm.props.IsConnected = false; // Indicates whether the SignalR connection is established
        vm.props.ScheduleId = ""; // The schedule ID entered by the user
        vm.props.UserName = ""; // The user name entered by the user

        vm.range = function (n) {
            return new Array(n);
        }

        $scope.test = function () {
            alert('abc');
        };

        $scope.beginJoinSchedule = function (scheduleID) {
            var hub = $.connection.timetableHub;
            hub.server.beginJoinSchedule(scheduleID);
        }

        $scope.completeJoinSchedule = function (userName) {
            var hub = $.connection.timetableHub;
            hub.server.completeJoinSchedule(userName);
        }


        $scope.createSchedule = function (userName) {
            var hub = $.connection.timetableHub;
            hub.server.createSchedule(userName);
        }

        function activate() {

            // Get chat hub proxy
            var timetableProxy = $.connection.timetableHub;

            // Initialize object syncing on the chat hub.
            // Pass the Angular $scope so that changes to synced objects can
            // be wrapped inside a $scope.$apply-call which triggers binding updates.
            // Pass false to disable logging.
            $.connection.initializeObjectSynchronization(timetableProxy, $scope, false);

            // In the Angular ViewModel put a reference to the container for synced objects
            vm.syncedObjects = timetableProxy.synchronizedObjects;

            // Open the connection
            $.connection.hub.start().done(function () {

                $scope.$apply(function () {
                    vm.props.IsConnected = true;
                });

                $("#searchCourseModal-input").on("keypress", function (event) {
                    if (event.which === 13 && !event.shiftKey) {
                        timetableProxy.server.searchCourses($("#searchCourseModal-input").val());
                    }
                });

                $scope.addCourse = function(courseId) {
                    timetableProxy.server.addCourse(courseId);
                }

                $scope.removeCourse = function(courseId) {
                    timetableProxy.server.removeCourse(courseId);
                }

            });

        }

        activate();
    }
})();
