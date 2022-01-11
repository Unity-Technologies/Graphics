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
            [SerializeField] public string state;
            [SerializeField] public ProbeVolumeAsset asset;
        }

        internal Dictionary<string, ProbeVolumeAsset> assets = new();

        [SerializeField] List<SerializableAssetItem> serializedAssets;

        string m_CurrentState = ProbeReferenceVolume.defaultBakingState;

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        public void OnAfterDeserialize()
        {
            if (serializedAssets == null) return;

            assets = new Dictionary<string, ProbeVolumeAsset>();
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

        internal void StoreAssetForState(string state, ProbeVolumeAsset asset)
        {
            assets[state] = asset;
        }

        internal ProbeVolumeAsset GetAssetForState(string state) => assets.GetValueOrDefault(state, null);

        internal void Clear()
        {
            InvalidateAllAssets();

#if UNITY_EDITOR
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var asset in assets.Values)
                {
                    if (asset != null)
                        AssetDatabase.DeleteAsset(asset.GetSerializedFullPath());
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
#endif

            assets.Clear();
        }

        internal void RenameBakingState(string state, string newName)
        {
            if (!assets.TryGetValue(state, out var asset))
                return;
            assets.Remove(state);
            assets.Add(newName, asset);
            asset.Rename(gameObject.scene, newName);
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
            m_CurrentState = ProbeReferenceVolume.defaultBakingState;
        }

        public void SetBakingState(string state)
        {
            if (state == m_CurrentState)
                return;

            QueueAssetRemoval();
            m_CurrentState = state;
            QueueAssetLoading();
        }
    }
}
