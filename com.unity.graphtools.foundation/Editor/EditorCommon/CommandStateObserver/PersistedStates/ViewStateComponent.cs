using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for implementations of <see cref="IViewStateComponent"/>.
    /// </summary>
    [Serializable]
    public abstract class ViewStateComponent<TUpdater> : StateComponent<TUpdater>, IViewStateComponent
        where TUpdater : class, IStateComponentUpdater, new()
    {
        [SerializeField]
        Hash128 m_ViewGuid;

        /// <inheritdoc/>
        public Hash128 ViewGuid
        {
            get => m_ViewGuid;
            set => m_ViewGuid = value;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                PersistedState.StoreStateComponent(this, ComponentName, ViewGuid, default);
            }
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other)
        {
            base.Move(other);

            if (other is ViewStateComponent<TUpdater> viewStateComponent)
            {
                m_ViewGuid = viewStateComponent.ViewGuid;
            }
        }

#if !UNITY_2022_1_OR_NEWER
        [SerializeField]
        string m_SerializedViewGuid;

        /// <inheritdoc />
        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_SerializedViewGuid = m_ViewGuid.ToString();
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_ViewGuid = Hash128.Parse(m_SerializedViewGuid);
        }
#endif
    }
}
