using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// A component that stores baked probe volume state and data references. Normally hidden from the user.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("")] // Hide.
    public class ProbeVolumePerSceneData : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Serializable]
        struct SerializableAssetItem
        {
            public ProbeVolumeAsset asset;
            public TextAsset cellDataAsset;
            public TextAsset cellOptionalDataAsset;
            public TextAsset cellSupportDataAsset;
            public string state;
        }
        [SerializeField] List<SerializableAssetItem> serializedAssets = new();
        

        internal Dictionary<string, ProbeVolumeAsset> assets = new();

        string m_CurrentState = ProbeReferenceVolume.defaultBakingState;

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            assets.Clear();
            foreach (var assetItem in serializedAssets)
            {
                assetItem.asset.cellDataAsset = assetItem.cellDataAsset;
                assetItem.asset.cellOptionalDataAsset = assetItem.cellOptionalDataAsset;
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
                item.cellOptionalDataAsset = item.asset.cellOptionalDataAsset;
                item.cellSupportDataAsset = item.asset.cellSupportDataAsset;
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
                EditorUtility.SetDirty(this);
            }
#endif

            assets.Clear();
        }

        internal void RemoveBakingState(string state)
        {
#if UNITY_EDITOR
            if (assets.TryGetValue(state, out var asset))
            {
                AssetDatabase.DeleteAsset(asset.GetSerializedFullPath());
                EditorUtility.SetDirty(this);
            }
#endif
            assets.Remove(state);
        }

        internal void RenameBakingState(string state, string newName)
        {
            if (!assets.TryGetValue(state, out var asset))
                return;
            assets.Remove(state);
            assets.Add(newName, asset);

#if UNITY_EDITOR
            asset.Rename(gameObject.scene, newName);
            EditorUtility.SetDirty(this);
#endif
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
            if (assets.TryGetValue(m_CurrentState, out var asset) && asset != null && asset.ResolveCells())
            {

                refVol.AddPendingAssetLoading(asset);
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

#if UNITY_EDITOR
        public void StripSupportData()
        {
            foreach (var asset in assets.Values)
                asset.cellSupportDataAsset = null;
        }
#endif
    }
}
