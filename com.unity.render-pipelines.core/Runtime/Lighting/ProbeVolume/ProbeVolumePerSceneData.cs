using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering
{
    [ExecuteAlways]
    [AddComponentMenu("")] // Hide.
    internal class ProbeVolumePerSceneData : MonoBehaviour, ISerializationCallbackReceiver
    {
        [System.Serializable]
        struct SerializableAssetItem
        {
            [SerializeField] public ProbeVolumeBakingState state;
            [SerializeField] public ProbeVolumeAsset asset;
        }

        internal Dictionary<ProbeVolumeBakingState, ProbeVolumeAsset> assets = new Dictionary<ProbeVolumeBakingState, ProbeVolumeAsset>();

        [SerializeField] List<SerializableAssetItem> serializedAssets;

        ProbeVolumeBakingState m_CurrentState = (ProbeVolumeBakingState)ProbeReferenceVolume.numBakingStates;

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        public void OnAfterDeserialize()
        {
            if (serializedAssets == null) return;

            assets = new Dictionary<ProbeVolumeBakingState, ProbeVolumeAsset>();
            foreach (var assetItem in serializedAssets)
                assets.Add(assetItem.state, assetItem.asset);
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

        internal void StoreAssetForState(ProbeVolumeBakingState state, ProbeVolumeAsset asset)
        {
            assets[state] = asset;
        }

        internal ProbeVolumeAsset GetAssetForState(ProbeVolumeBakingState state) => assets.GetValueOrDefault(state, null);

        internal void Clear()
        {
            InvalidateAllAssets();

#if UNITY_EDITOR
            AssetDatabase.StartAssetEditing();
            foreach (var asset in assets)
            {
                if (asset.Value != null)
                    AssetDatabase.DeleteAsset(ProbeVolumeAsset.GetPath(gameObject.scene, asset.Key, false));
            }
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
#endif

            assets.Clear();
        }

        internal void InvalidateAllAssets()
        {
            foreach (var asset in assets.Values)
            {
                if (asset != null)
                    ProbeReferenceVolume.instance.AddPendingAssetRemoval(asset);
            }
        }

        internal ProbeVolumeAsset GetCurrentStateAsset()
        {
            if (assets.ContainsKey(m_CurrentState)) return assets[m_CurrentState];
            else return null;
        }

        internal void QueueAssetLoading()
        {
            var refVol = ProbeReferenceVolume.instance;
            if (assets.ContainsKey(m_CurrentState) && assets[m_CurrentState] != null)
            {
                refVol.AddPendingAssetLoading(assets[m_CurrentState]);
#if UNITY_EDITOR
                if (refVol.sceneData != null)
                {
                    refVol.dilationValidtyThreshold = refVol.sceneData.GetBakeSettingsForScene(gameObject.scene).dilationSettings.dilationValidityThreshold;
                }
#endif
            }
        }

        internal void QueueAssetRemoval()
        {
            if (assets.TryGetValue(m_CurrentState, out var asset) && asset != null)
                ProbeReferenceVolume.instance.AddPendingAssetRemoval(asset);
        }

        void OnEnable()
        {
            ProbeReferenceVolume.instance.RegisterPerSceneData(this);

            // If a user doesn't save the scene after baking, we still have to look for file assets on disk to avoid lost baked data
            var scene = gameObject.scene;
            for (int i = 0; i < ProbeReferenceVolume.numBakingStates; i++)
            {
                var state = (ProbeVolumeBakingState)i;
                if (assets.ContainsKey(state))
                    continue;
                var asset = AssetDatabase.LoadAssetAtPath<ProbeVolumeAsset>(ProbeVolumeAsset.GetPath(scene, state, false));
                if (asset != null)
                    assets.Add(state, asset);
            }

            if (ProbeReferenceVolume.instance.sceneData != null)
                SetBakingState(ProbeReferenceVolume.instance.bakingState);
            // otherwise baking state will be initialized in ProbeReferenceVolume.Initialize when sceneData is loaded
        }

        void OnDisable()
        {
            OnDestroy();
            ProbeReferenceVolume.instance.UnregisterPerSceneData(this);
        }

        void OnDestroy()
        {
            QueueAssetRemoval();
            m_CurrentState = (ProbeVolumeBakingState)ProbeReferenceVolume.numBakingStates;
        }

        public void SetBakingState(ProbeVolumeBakingState state)
        {
            if (state == m_CurrentState)
                return;

            QueueAssetRemoval();
            m_CurrentState = state;
            QueueAssetLoading();
        }
    }
}
