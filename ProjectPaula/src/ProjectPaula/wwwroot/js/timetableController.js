(function () {
    'use strict';

    angular
        .module('timetableApp')
        .controller('timetableController', timetableController);

    //chatController.$inject = ['$location']; 

    function timetableController($scope) {
        /* jshint validthis:true */
        var vm = this;
        vm.title = 'timetableController';

        vm.range = function (n) {
            return new Array(n);
        }

        activate();

        function activate() {

            // Get chat hub proxy
            var timetableProxy = $.connection.timetableHub;

            // Initialize object syncing on the chat hub
            // Pass the Angular $scope so that changes to synced objects can
            // be wrapped inside a $scope.$apply-call which triggers binding updates
            $.connection.initializeObjectSynchronization(timetableProxy, $scope);

            // In the Angular ViewModel put a reference to the container for synced objects
            vm.syncedObjects = timetableProxy.synchronizedObjects;

            // Open the connection
            $.connection.hub.start().done(function () {

                setInterval(function () {

                    console.log(JSON.stringify($.connection.timetableHub.synchronizedObjects.Timetable, null, 4));

                }, 1000);

       
            });

        }
    }
})();
