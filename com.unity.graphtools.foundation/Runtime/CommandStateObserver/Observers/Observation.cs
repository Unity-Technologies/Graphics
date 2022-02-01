using System;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// A class used to encompass observations by an <see cref="IStateObserver"/> on an <see cref="IStateComponent"/>.
    /// </summary>
    /// <remarks>
    /// Instances of this class fetch the update type from the observed component and optionally update
    /// the last observed version of the observer. They can be used in a using() context.
    /// </remarks>
    public class Observation : IDisposable
    {
        IStateComponent m_ObservedComponent;
        IStateObserver m_Observer;
        bool m_UpdateObserverVersion;

        /// <summary>
        /// The update type for the observation. This tells the observer how it should do its update.
        /// </summary>
        public UpdateType UpdateType { get; }

        /// <summary>
        /// The observer's last observed version of the component.
        /// </summary>
        public uint LastObservedVersion { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Observation" /> class.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <param name="observedComponent">The observed state</param>
        /// <param name="updateObserverVersion">True if we should update the observer's observed version
        /// at the end of the observation (default). False otherwise.</param>
        internal Observation(IStateObserver observer, IStateComponent observedComponent, bool updateObserverVersion = true)
        {
            Assert.IsTrue(observer.ObservedStateComponents.Contains(observedComponent),
                $"Observer {observer.GetType().FullName} does not specify that it observes {observedComponent}. Please add the state component to its {nameof(IStateObserver.ObservedStateComponents)}.");

            m_ObservedComponent = observedComponent;
            m_Observer = observer;
            m_UpdateObserverVersion = updateObserverVersion;

            var internalObserver = m_Observer as IInternalStateObserver;
            var lastObservedVersion = internalObserver?.GetLastObservedComponentVersion(observedComponent) ?? default;
            UpdateType = m_ObservedComponent.GetUpdateType(lastObservedVersion);
            LastObservedVersion = lastObservedVersion.Version;
        }

        ~Observation()
        {
            Dispose(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_UpdateObserverVersion)
                {
                    var newVersion = new StateComponentVersion
                    {
                        HashCode = m_ObservedComponent.GetHashCode(),
                        Version = m_ObservedComponent.CurrentVersion
                    };
                    (m_Observer as IInternalStateObserver)?.UpdateObservedVersion(m_ObservedComponent, newVersion);
                }
            }
        }
    }
}
