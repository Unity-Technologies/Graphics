using System.Collections.Generic;
using UnityEditor;

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
    internal class ProbeVolumePerSceneData : MonoBehaviour, ISerializationCallbackReceiver
    {

        [System.Serializable]
        struct SerializableAssetItem
        {
            [SerializeField] public int state;
            [SerializeField] public ProbeVolumeAsset asset;
        }

        internal Dictionary<int, ProbeVolumeAsset> assets = new Dictionary<int, ProbeVolumeAsset>();

        [SerializeField] List<SerializableAssetItem> serializedAssets;

        [SerializeField] int m_CurrentState = -1; // probably not needed to serialize it

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        public void OnAfterDeserialize()
        {
            if (serializedAssets == null) return;

            assets = new Dictionary<int, ProbeVolumeAsset>();
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

#if UNITY_EDITOR
        internal ProbeVolumeAsset CreateAssetForCurrentState()
        {
            var asset = ProbeVolumeAsset.CreateAsset(gameObject.scene, m_CurrentState);

            assets[m_CurrentState] = asset;
            QueueAssetLoading();
            return asset;
        }

        internal void DeleteAssetForState(int state)
        {
            assets.Remove(state);
        }
#endif

        internal ProbeVolumeAsset GetAssetForState(int state) => assets.GetValueOrDefault(state, null);

        internal void Clear()
        {
            AssetDatabase.StartAssetEditing();
            foreach (var asset in assets)
            {
                if (asset.Value != null)
                {
                    ProbeReferenceVolume.instance.AddPendingAssetRemoval(asset.Value);
                    AssetDatabase.DeleteAsset(ProbeVolumeAsset.GetPath(gameObject.scene, asset.Key, false));
                }
            }
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();

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

        public void SetBakingState(int state)
        {
            if (state == m_CurrentState)
                return;

            if (assets.ContainsKey(m_CurrentState) && assets[m_CurrentState] != null)
                ProbeReferenceVolume.instance.AddPendingAssetRemoval(assets[m_CurrentState]);

            m_CurrentState = state;
            QueueAssetLoading();
        }
    }
}
