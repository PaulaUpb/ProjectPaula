/*
    AngularJS module for the chat sample.
*/

(function () {
    'use strict';

    angular.module('chatApp', []);
    angular.module('timetableApp', [],
        function ($locationProvider) {
            $locationProvider.html5Mode(true);
        });
})();