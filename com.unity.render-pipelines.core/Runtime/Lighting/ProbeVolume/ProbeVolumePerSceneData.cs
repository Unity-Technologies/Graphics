using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering
{
    // TMP to be moved to ProbeReferenceVolume when we define the concept, here it is just to make stuff compile
    enum ProbeVolumeState
    {
        Default = 0,
        Invalid = 999
    }

    [ExecuteAlways]
    [AddComponentMenu("")] // Hide.
    public class ProbeVolumePerSceneData : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Serializable]
        struct SerializableAssetItem
        {
            public ProbeVolumeState state;
            public ProbeVolumeAsset asset;
            public TextAsset cellDataAsset;
            public TextAsset cellSupportDataAsset;
        }

        [SerializeField] List<SerializableAssetItem> serializedAssets = new();

        Dictionary<ProbeVolumeState, ProbeVolumeAsset> assets = new();

        ProbeVolumeState m_CurrentState = ProbeVolumeState.Default;
        ProbeVolumeState m_PreviousState = ProbeVolumeState.Invalid;

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            assets.Clear();
            foreach (var assetItem in serializedAssets)
            {
                assetItem.asset.cellDataAsset = assetItem.cellDataAsset;
                assetItem.asset.cellSupportDataAsset = assetItem.cellSupportDataAsset;
                assets.Add(assetItem.state, assetItem.asset);
            }
        }

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            serializedAssets.Clear();
            foreach (var k in assets.Keys)
            {
                SerializableAssetItem item;
                item.state = k;
                item.asset = assets[k];
                item.cellDataAsset = item.asset.cellDataAsset;
                item.cellSupportDataAsset = item.asset.cellSupportDataAsset;
                serializedAssets.Add(item);
            }
        }

        internal void StoreAssetForState(ProbeVolumeState state, ProbeVolumeAsset asset)
        {
            assets[state] = asset;
        }

        internal void InvalidateAllAssets()
        {
            foreach (var asset in assets.Values)
            {
                if (asset != null)
                    ProbeReferenceVolume.instance.AddPendingAssetRemoval(asset);
            }

            assets.Clear();
        }

        internal ProbeVolumeAsset GetCurrentStateAsset()
        {
            if (assets.ContainsKey(m_CurrentState)) return assets[m_CurrentState];
            else return null;
        }

        internal void QueueAssetLoading()
        {
            var refVol = ProbeReferenceVolume.instance;
            if (assets.TryGetValue(m_CurrentState, out var asset) && asset != null)
            {
                asset.ResolveCells();
                refVol.AddPendingAssetLoading(asset);

#if UNITY_EDITOR
                if (refVol.sceneData != null)
                {
                    refVol.dilationValidtyThreshold = refVol.sceneData.GetBakeSettingsForScene(gameObject.scene).dilationSettings.dilationValidityThreshold;
                }
#endif

                m_PreviousState = m_CurrentState;
            }
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

        void Update()
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
        }

#if UNITY_EDITOR
        public void StripSupportData()
        {
            foreach (var asset in assets.Values)
                asset.cellSupportDataAsset = null;
        }
#endif
    }
}
