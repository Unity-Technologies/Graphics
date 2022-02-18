using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A component that stores baked probe volume state and data references. Normally hidden from the user.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("")] // Hide.
    public class ProbeVolumePerSceneData : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Serializable]
        internal struct PerStateData
        {
            public int sceneHash;
            public TextAsset cellDataAsset; // Contains L0 L1 SH data
            public TextAsset cellOptionalDataAsset; // Contains L2 SH data
        }

        [Serializable]
        struct SerializablePerStateDataItem
        {
            public string state;
            public PerStateData data;
        }

        [SerializeField] internal ProbeVolumeAsset asset;
        [SerializeField] internal TextAsset cellSharedDataAsset; // Contains bricks and validity data
        [SerializeField] internal TextAsset cellSupportDataAsset; // Contains debug data
        [SerializeField] List<SerializablePerStateDataItem> serializedStates = new();

        internal Dictionary<string, PerStateData> states = new();

        bool assetLoaded = false;
        string currentState = null, transitionState = null;

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            states.Clear();
            foreach (var stateData in serializedStates)
                states.Add(stateData.state, stateData.data);
        }

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            serializedStates.Clear();
            foreach (var kvp in states)
            {
                serializedStates.Add(new SerializablePerStateDataItem()
                {
                    state = kvp.Key,
                    data = kvp.Value,
                });
            }
        }

#if UNITY_EDITOR
        void DeleteAsset(Object asset)
        {
            if (asset != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long instanceID))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.DeleteAsset(assetPath);
            }
        }
#endif

        internal void Clear()
        {
            QueueAssetRemoval();

#if UNITY_EDITOR
            try
            {
                AssetDatabase.StartAssetEditing();
                DeleteAsset(asset);
                DeleteAsset(cellSharedDataAsset);
                DeleteAsset(cellSupportDataAsset);
                foreach (var stateData in states.Values)
                {
                    DeleteAsset(stateData.cellDataAsset);
                    DeleteAsset(stateData.cellOptionalDataAsset);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                EditorUtility.SetDirty(this);
            }
#endif

            states.Clear();
        }

        internal void RemoveBakingState(string state)
        {
#if UNITY_EDITOR
            if (states.TryGetValue(state, out var stateData))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(stateData.cellDataAsset));
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(stateData.cellOptionalDataAsset));
                EditorUtility.SetDirty(this);
            }
#endif
            states.Remove(state);
        }

        internal void RenameBakingState(string state, string newState)
        {
            if (!states.TryGetValue(state, out var stateData))
                return;
            states.Remove(state);
            states.Add(newState, stateData);

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            var baseName = ProbeVolumeAsset.assetName + "-" + newState;
            void RenameAsset(Object asset, string extension)
            {
                var oldPath = AssetDatabase.GetAssetPath(asset);
                AssetDatabase.RenameAsset(oldPath, baseName + extension);
            }
            RenameAsset(stateData.cellDataAsset, ".CellData.bytes");
            RenameAsset(stateData.cellOptionalDataAsset, ".CellOptionalData.bytes");
#endif
        }

        internal bool ResolveCells() => ResolveSharedCellData() && ResolvePerStateCellData();

        bool ResolveSharedCellData() => asset != null && asset.ResolveSharedCellData(cellSharedDataAsset, cellSupportDataAsset);
        bool ResolvePerStateCellData()
        {
            int loadedCount = 0;
            string state0 = transitionState != null ? transitionState : currentState;
            string state1 = transitionState != null ? currentState : null;
            if (state0 != null && states.TryGetValue(state0, out var data0))
            {
                if (asset.ResolvePerStateCellData(data0.cellDataAsset, data0.cellOptionalDataAsset, 0))
                    loadedCount++;
            }
            if (state1 != null && states.TryGetValue(state1, out var data1))
            {
                if (asset.ResolvePerStateCellData(data1.cellDataAsset, data1.cellOptionalDataAsset, loadedCount))
                    loadedCount++;
            }
            for (var i = 0; i < asset.cells.Length; ++i)
                asset.cells[i].hasTwoStates = loadedCount == 2;
            return loadedCount != 0;
        }

        internal void QueueAssetLoading()
        {
            if (asset == null || !ResolvePerStateCellData())
                return;

            var refVol = ProbeReferenceVolume.instance;
            refVol.AddPendingAssetLoading(asset);
            assetLoaded = true;
#if UNITY_EDITOR
            if (refVol.sceneData != null)
                refVol.bakingProcessSettings = refVol.sceneData.GetBakeSettingsForScene(gameObject.scene);
#endif
        }

        internal void QueueAssetRemoval()
        {
            if (asset != null)
                ProbeReferenceVolume.instance.AddPendingAssetRemoval(asset);
            assetLoaded = false;
        }

        void OnEnable()
        {
            ProbeReferenceVolume.instance.RegisterPerSceneData(this);

            if (ProbeReferenceVolume.instance.sceneData != null)
                Initialize();
            // otherwise baking state will be initialized in ProbeReferenceVolume.Initialize when sceneData is loaded
        }

        void OnDisable()
        {
            QueueAssetRemoval();
            currentState = transitionState = null;
            ProbeReferenceVolume.instance.UnregisterPerSceneData(this);
        }

        internal void Initialize()
        {
            ResolveSharedCellData();

            QueueAssetRemoval();
            currentState = ProbeReferenceVolume.instance.sceneData.bakingState;
            transitionState = null;
            QueueAssetLoading();
        }

        internal void UpdateBakingState(string state, string previousState)
        {
            if (asset == null)
                return;

            // if we just change state, don't need to queue anything
            // Just load state cells from disk and wait for blending to stream updates to gpu
            // When blending factor is < 0.5, streaming will upload cell from transition state
            // After that, it will always upload cells from current state
            // So gradually, all loaded cells, will be either blended towards new state, or replaced by streaming

            currentState = state;
            transitionState = previousState;
            if (!assetLoaded)
                QueueAssetLoading();
            else if (!ResolvePerStateCellData())
                QueueAssetRemoval();
        }

#if UNITY_EDITOR
        internal void GetBlobFileNames(out string cellDataFilename, out string cellOptionalDataFilename, out string cellSharedDataFilename, out string cellSupportDataFilename)
        {
            var state = ProbeReferenceVolume.instance.bakingState;
            string basePath = Path.Combine(ProbeVolumeAsset.GetDirectory(gameObject.scene.path, gameObject.scene.name), ProbeVolumeAsset.assetName);

            string GetOrCreateFileName(Object o, string extension)
            {
                var res = AssetDatabase.GetAssetPath(o);
                if (string.IsNullOrEmpty(res)) res = basePath + extension;
                return res;
            }
            cellDataFilename = GetOrCreateFileName(states[state].cellDataAsset, "-" + state + ".CellData.bytes");
            cellOptionalDataFilename = GetOrCreateFileName(states[state].cellOptionalDataAsset, "-" + state + ".CellOptionalData.bytes");
            cellSharedDataFilename = GetOrCreateFileName(cellSharedDataAsset, ".CellSharedData.bytes");
            cellSupportDataFilename = GetOrCreateFileName(cellSupportDataAsset, ".CellSupportData.bytes");
        }

        /// <summary>
        /// Call this function during OnProcessScene to strip debug from project builds.
        /// </summary>
        public void StripSupportData()
        {
            cellSupportDataAsset = null;
        }
#endif
    }
}
