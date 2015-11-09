using System;
using System.Diagnostics;

namespace ProjectPaula.Model.ObjectSynchronization.ChangeTracking
{
    interface IObjectObserver : IDisposable
    {
        object TrackedObject { get; }
        void Initialize(ObjectTracker tracker);
    }
}
