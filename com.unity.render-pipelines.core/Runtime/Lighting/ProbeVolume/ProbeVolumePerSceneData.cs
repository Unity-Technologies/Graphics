using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
{
    // TMP to be moved to ProbeReferenceVolume when we define the concept, here it is just to make stuff compile
    enum ProbeVolumeState
    {
        Default = 0
    }

    [ExecuteAlways]
    [AddComponentMenu("")] // Hide.
    internal class ProbeVolumePerSceneData : MonoBehaviour, ISerializationCallbackReceiver
    {

        [System.Serializable]
        struct SerializableAssetItem
        {
            [SerializeField] public ProbeVolumeState state;
            [SerializeField] public ProbeVolumeAsset asset;
        }

        internal Dictionary<ProbeVolumeState, ProbeVolumeAsset> assets = new Dictionary<ProbeVolumeState, ProbeVolumeAsset>();

        [SerializeField] List<SerializableAssetItem> serializedAssets;

        ProbeVolumeState m_CurrentState = ProbeVolumeState.Default;
        ProbeVolumeState m_PreviousState = ProbeVolumeState.Default;

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        public void OnAfterDeserialize()
        {
            if (serializedAssets == null) return;

            assets = new Dictionary<ProbeVolumeState, ProbeVolumeAsset>();
            foreach (var assetItem in serializedAssets)
            {
                assets.Add(assetItem.state, assetItem.asset);
            }
        }

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        public void OnBeforeSerialize()
        {
            if (assets == null || serializedAssets == null) return;

            serializedAssets.Clear();
            foreach (var k in assets.Keys)
            {
                SerializableAssetItem item;
                item.state = k;
                item.asset = assets[k];
                serializedAssets.Add(item);
            }
        }

        internal void QueueAssetLoading()
        {
            if (assets.ContainsKey(m_CurrentState) && assets[m_CurrentState] != null)
                ProbeReferenceVolume.instance.AddPendingAssetLoading(assets[m_CurrentState]);
        }

        internal void QueueAssetRemoval()
        {
            if (assets.ContainsKey(m_CurrentState) && assets[m_CurrentState] != null)
                ProbeReferenceVolume.instance.AddPendingAssetRemoval(assets[m_CurrentState]);
        }

        void OnEnable()
        {
            QueueAssetLoading();
        }

        void OnDisable()
        {
            QueueAssetRemoval();
        }

        void OnDestroy()
        {
            QueueAssetRemoval();
        }

        void OnUpdate()
        {
            // Query state from ProbeReferenceVolume.instance.
            // This is temporary here until we implement a state system.
            m_CurrentState = ProbeVolumeState.Default;
            if (m_PreviousState != m_CurrentState)
            {
                if (assets.ContainsKey(m_PreviousState) && assets[m_PreviousState] != null)
                    ProbeReferenceVolume.instance.AddPendingAssetRemoval(assets[m_PreviousState]);

                QueueAssetLoading();
            }
            m_PreviousState = m_CurrentState;
        }
    }
}
