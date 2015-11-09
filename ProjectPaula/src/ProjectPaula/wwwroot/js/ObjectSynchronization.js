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
        path = path.split('.');

        for (i = 0; i < path.length - 1; i++)
            obj = obj[path[i]];

        obj[path[i]] = value;
    }

    function getObjectAtPath(obj, path) {

        path = path.split('.');

        for (i = 0; i < path.length; i++)
            obj = obj[path[i]];

        return obj;
    }

    $.connection.initializeObjectSynchronization = function (hub) {
        // Call this once to setup object synchronization for the specified hub.
        // sample usage: $.connection.initializeObjectSynchronization($.connection.chatHub)

        // This is where the synchronized objects will be placed
        hub.synchronizedObjects = {};

        // Add the functions required by the ObjectSynchronizationHub<T>.
        // The ObjectSynchronizationHub<T> uses these to push object changes
        // to the clients.

        hub.client.initializeObject = function (key, o) {
            // This is called when we receive a new object from the server.
            // key: The key that is used to identify the SynchronizedObject on client and server side
            // o: The synchronized object ("ViewModel") in its current state
            hub.synchronizedObjects[key] = o;
        }

        hub.client.propertyChanged = function (key, e) {
            // This is called to notify clients of changes to the synchronized object.
            // e is of type PropertyPathChangedEventArgs
            var o = hub.synchronizedObjects[key];
            setPropertyAtPath(o, e.NewValue, e.PropertyPath);
        }

        hub.client.collectionChanged = function (key, e) {
            // This is called to notify clients of collection changes to the synchronized object.
            // e is of type CollectionPathChangedEventArgs
            console.log(e.PropertyPath + ": Collection changed");

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
                        console.log("NotImplemented: CollectionChanged with Remove but StartingIndex = -1");
                    else
                        array.splice(e.StartingIndex, e.Items.length);
                    break;

                case "Reset":
                    array.length = 0; // Clears the array
                    break;
            }
        }
    }

})();