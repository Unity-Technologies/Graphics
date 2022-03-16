using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for models backing a <see cref="RootView"/>.
    /// </summary>
    public abstract class RootViewModel : IModel
    {
        protected SerializableGUID m_Guid;

        /// <inheritdoc />
        public SerializableGUID Guid
        {
            get
            {
                if (!m_Guid.Valid)
                    AssignNewGuid();
                return m_Guid;
            }
            set => m_Guid = value;
        }

        /// <inheritdoc />
        public void AssignNewGuid()
        {
            m_Guid = SerializableGUID.Generate();
        }

        /// <summary>
        /// Adds the <see cref="StateComponent{TUpdater}"/>s of this object to the <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The state to which to add the components.</param>
        public abstract void AddToState(IState state);

        /// <summary>
        /// Removes the <see cref="StateComponent{TUpdater}"/>s of this object from the <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The state from which to remove the components.</param>
        public abstract void RemoveFromState(IState state);
    }
}
