using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.Serialization;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for implementations of <see cref="IPersistedStateComponent"/>.
    /// </summary>
    [Serializable]
    public abstract class PersistedStateComponent<TUpdater> : StateComponent<TUpdater>, IPersistedStateComponent
        where TUpdater : class, IStateComponentUpdater, new()
    {
        [SerializeField]
        Hash128 m_ViewGuid;

        [FormerlySerializedAs("m_AssetKey")]
        [SerializeField]
        string m_GraphKey;

        /// <inheritdoc/>
        public Hash128 ViewGuid
        {
            get => m_ViewGuid;
            set => m_ViewGuid = value;
        }

        /// <inheritdoc />
        public string GraphKey
        {
            get => m_GraphKey;
            set => m_GraphKey = value;
        }

        /// <inheritdoc />
        public override void OnRemovedFromState(IState state)
        {
            base.OnRemovedFromState(state);
            PersistedState.StoreStateComponent(this, ComponentName, ViewGuid, GraphKey);
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other)
        {
            base.Move(other);

            if (other is PersistedStateComponent<TUpdater> persistedStateComponent)
            {
                m_ViewGuid = persistedStateComponent.ViewGuid;
                m_GraphKey = persistedStateComponent.GraphKey;
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
