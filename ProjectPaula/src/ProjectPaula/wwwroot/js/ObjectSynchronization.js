/*
    Client side JS code for object synchronization
    (ProjectPaula.Model.ObjectSynchronization).
    This script handles the reconstruction of server-side
    C# objects.

    To be used in conjunction with a SignalR hub derived
    from ObjectSynchronizationHub<T>.
*/

(function () {

    function setPropertyAtPath(obj, value, path) {
        path = path.split(".");

        for (i = 0; i < path.length - 1; i++)
            obj = obj[path[i]];

        obj[path[i]] = value;
    }

    function getObjectAtPath(obj, path) {

        path = path.split(".");

        for (i = 0; i < path.length; i++)
            obj = obj[path[i]];

        return obj;
    }

    $.connection.initializeObjectSynchronization = function (hub, angularScope, isLoggingEnabled) {
        // Call this once to setup object synchronization for the specified hub.
        // Call it before opening the hub connection.
        // After this call $.connection.<yourHubName>.synchronizedObjects is available.
        //
        // hub: The hubProxy where object synchronization should be enabled
        // angularScope: The $scope of AngularJS. We use this to call $apply on object changes.
        // isLoggingEnabled: True to enable logging of addition and removal of objects as well as property and collection changes
        //
        // sample usage: $.connection.initializeObjectSynchronization($.connection.chatHub)

        // This is where the synchronized objects will be placed
        hub.synchronizedObjects = {};

        // If no AngularJS scope is supplied, create a dummy scope with dummy apply function
        if (!angularScope)
            angularScope = { $apply: function (f) { f(); } };

        var $scope = angularScope;
        var isLoggingEnabled = (isLoggingEnabled == undefined) ? false : isLoggingEnabled;

        // Add the functions required by the ObjectSynchronizationHub<T>.
        // The ObjectSynchronizationHub<T> uses these to push object changes
        // to the clients.

        hub.client.initializeObject = function (key, o) {
            // This is called when we receive a new object from the server.
            // key: The key that is used to identify the SynchronizedObject on client and server side
            // o: The synchronized object ("ViewModel") in its current state

            $scope.$apply(function () {
                hub.synchronizedObjects[key] = o;
            });

            // Raise added event
            if (hub.synchronizedObjects.addedEventHandlers.hasOwnProperty(key))
            {
                hub.synchronizedObjects.addedEventHandlers[key].forEach(function (fn) {
                    fn(o);
                });
            }

            if (isLoggingEnabled)
                console.log("ObjectSynchronization: Added object '" + key + "'");
        }

        hub.client.removeObject = function (key) {
            // This is called when the server disables synchronization of an object with this client.
            // key: The key that is used to identify the SynchronizedObject on client and server side

            var oldObject = hub.synchronizedObjects[key];

            $scope.$apply(function () {
                delete hub.synchronizedObjects[key];
            });

            // Raise removed event
            if (hub.synchronizedObjects.removedEventHandlers.hasOwnProperty(key)) {
                hub.synchronizedObjects.removedEventHandlers[key].forEach(function (fn) {
                    fn(oldObject);
                });
            }

            if (isLoggingEnabled)
                console.log("ObjectSynchronization: Removed object '" + key + "'");
        }

        hub.synchronizedObjects.addedEventHandlers = {};
        hub.synchronizedObjects.removedEventHandlers = {};

        hub.synchronizedObjects.added = function (key, fn) {
            // Add fn as a handler for the event that an object with the specified key arrives
            if (!hub.synchronizedObjects.addedEventHandlers.hasOwnProperty(key))
                hub.synchronizedObjects.addedEventHandlers[key] = [];

            hub.synchronizedObjects.addedEventHandlers[key].push(fn);
        }

        hub.synchronizedObjects.removed = function (key, fn) {
            // Add fn as a handler for the event that the object with the specified key is removed
            if (!hub.synchronizedObjects.removedEventHandlers.hasOwnProperty(key))
                hub.synchronizedObjects.removedEventHandlers[key] = [];

            hub.synchronizedObjects.removedEventHandlers[key].push(fn);
        }

        hub.client.propertyChanged = function (key, e) {
            // This is called to notify clients of changes to the synchronized object.
            // e is of type PropertyPathChangedEventArgs
            
            $scope.$apply(function () {
                var o = hub.synchronizedObjects[key];
                setPropertyAtPath(o, e.NewValue, e.PropertyPath);
            });

            if (isLoggingEnabled)
                console.log("ObjectSynchronization: Property changed in '" + key + "': " + e.PropertyPath + " = " + e.NewValue);
        }

        hub.client.collectionChanged = function (key, e) {
            // This is called to notify clients of collection changes to the synchronized object.
            // e is of type CollectionPathChangedEventArgs
            
            $scope.$apply(function () {
                var o = hub.synchronizedObjects[key];
                var array = getObjectAtPath(o, e.PropertyPath);

                switch (e.Action) {
                    case "Add":
                        if (e.StartingIndex === -1)
                            array.push.apply(array, e.Items);
                        else
                            array.splice.apply(array, [e.StartingIndex, 0].concat(e.Items));
                        break;

                    case "Remove":
                        if (e.StartingIndex === -1)
                            console.error("NotImplemented: CollectionChanged with Remove but StartingIndex = -1");
                        else
                            array.splice(e.StartingIndex, e.Items.length);
                        break;

                    case "Reset":
                        array.length = 0; // Clears the array
                        break;
                }

                if (isLoggingEnabled)
                    console.log("ObjectSynchronization: Collection changed in '" + key + "': " + e.PropertyPath + " -> " + e.Action + (e.Items == null ? "" : " " + e.Items.length + " item(s)"));
            });
        }
    }

})();