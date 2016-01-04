(function () {
    "use strict";

    angular
        .module("chatApp")
        .controller("chatController", ["$scope", chatController]);

    //chatController.$inject = ['$location']; 

    function chatController($scope) {
        /* jshint validthis:true */
        var vm = this;
        vm.title = "chatController";

        activate();

        function activate() {

            // Get chat hub proxy
            var chat = $.connection.chatHub;

            // Initialize object syncing on the chat hub
            // Pass the Angular $scope so that changes to synced objects can
            // be wrapped inside a $scope.$apply-call which triggers binding updates
            $.connection.initializeObjectSynchronization(chat, $scope);

            // In the Angular ViewModel put a reference to the container for synced objects
            vm.syncedObjects = chat.synchronizedObjects;

            // Open the connection
            $.connection.hub.start().done(function () {

                // Ask for username
                var name = prompt("Enter your name:", "");
                chat.server.register(name);

                setInterval(function () {

                    $("#discussion").html(JSON.stringify($.connection.chatHub.synchronizedObjects.Chat, null, 4));
                    $("#discussion").css("white-space", "pre-wrap");

                }, 1000);

                $("#chatLoadingIndicator").css("display", "none");
                $("#chatContainer").css("display", "");

                $("#sendmessage").click(function () {
                    // Call the Send method on the hub.
                    chat.server.send($("#message").val());
                });
            });

        }
    }
})();
