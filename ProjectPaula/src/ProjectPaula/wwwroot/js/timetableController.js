﻿(function () {
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

    function timetableController($scope) {
        var vm = this;
        vm.title = 'timetableController';
        vm.props = {};
        vm.props.IsConnected = false; // Indicates whether the SignalR connection is established
        vm.props.ScheduleId = ""; // The schedule ID entered by the user
        vm.props.UserName = ""; // The user name entered by the user
        vm.props.SearchQuery = ""; // The query string used to search for courses
        
        function activate() {

            // Get SignalR hub proxy
            var timetableProxy = $.connection.timetableHub;
            timetableProxy.logging = true;

            // Initialize object syncing on the chat hub.
            // Pass the Angular $scope so that changes to synced objects can
            // be wrapped inside a $scope.$apply-call which triggers binding updates.
            // Pass false to disable logging.
            $.connection.initializeObjectSynchronization(timetableProxy, $scope, false);

            // In the Angular ViewModel put a reference to the container for synced objects
            vm.syncedObjects = timetableProxy.synchronizedObjects;

            // Define functions in Angular scope
            $scope.range = function (n) {
                return new Array(n);
            }

            $scope.beginJoinSchedule = function (scheduleID) {
                timetableProxy.server.beginJoinSchedule(scheduleID);
            }

            $scope.completeJoinSchedule = function (userName) {
                timetableProxy.server.completeJoinSchedule(userName);
            }

            $scope.createSchedule = function (userName) {
                timetableProxy.server.createSchedule(userName);
            }

            $scope.addCourse = function (courseId) {
                timetableProxy.server.addCourse(courseId);
            }

            $scope.removeCourse = function (courseId) {
                timetableProxy.server.removeCourse(courseId);
            }

            $scope.searchCourses = function (query) {
                timetableProxy.server.searchCourses(query);
            }

            // Open the SignalR connection
            $.connection.hub.start().done(function () {
                $scope.$apply(function () {
                    vm.props.IsConnected = true;
                });
            });
        }

        activate();
    }
})();
