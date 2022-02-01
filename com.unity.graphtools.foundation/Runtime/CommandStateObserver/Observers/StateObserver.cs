using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Base class for state observers.
    /// </summary>
    public abstract class StateObserver : IInternalStateObserver, IStateObserver
    {
        List<(IStateComponent, StateComponentVersion)> m_ObservedComponentVersions;
        List<IStateComponent> m_ModifiedStateComponents;

        /// <inheritdoc />
        public IEnumerable<IStateComponent> ObservedStateComponents => m_ObservedComponentVersions.Select(t => t.Item1);

        /// <inheritdoc />
        public IEnumerable<IStateComponent> ModifiedStateComponents => m_ModifiedStateComponents;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateObserver" /> class.
        /// </summary>
        /// <param name="observedStateComponents">The names of the observed state components.</param>
        protected StateObserver(params IStateComponent[] observedStateComponents)
            : this(observedStateComponents, Enumerable.Empty<IStateComponent>()) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="StateObserver" /> class.
        /// </summary>
        /// <param name="observedStateComponents">The names of the observed state components.</param>
        /// <param name="modifiedStateComponents">The names of the modified state components.</param>
        protected StateObserver(IEnumerable<IStateComponent> observedStateComponents, IEnumerable<IStateComponent> modifiedStateComponents)
        {
            m_ObservedComponentVersions = new List<(IStateComponent, StateComponentVersion)>(
                observedStateComponents.Distinct().Select<IStateComponent, (IStateComponent, StateComponentVersion)>(s => (s, default)));
            m_ModifiedStateComponents = modifiedStateComponents.Distinct().ToList();
        }

        /// <inheritdoc/>
        StateComponentVersion IInternalStateObserver.GetLastObservedComponentVersion(IStateComponent stateComponent)
        {
            var index = m_ObservedComponentVersions.FindIndex(v => v.Item1 == stateComponent);
            return index >= 0 ? m_ObservedComponentVersions[index].Item2 : default;
        }

        /// <inheritdoc />
        void IInternalStateObserver.UpdateObservedVersion(IStateComponent stateComponent, StateComponentVersion newVersion)
        {
            var index = m_ObservedComponentVersions.FindIndex(v => v.Item1 == stateComponent);
            if (index >= 0)
                m_ObservedComponentVersions[index] = (stateComponent, newVersion);
        }

        /// <inheritdoc />
        public abstract void Observe();
    }
}
