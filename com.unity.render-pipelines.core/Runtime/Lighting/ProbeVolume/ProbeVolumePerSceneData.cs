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
        internal struct PerScenarioData
        {
            public int sceneHash;
            public TextAsset cellDataAsset; // Contains L0 L1 SH data
            public TextAsset cellOptionalDataAsset; // Contains L2 SH data
        }

        [Serializable]
        struct SerializablePerScenarioDataItem
        {
            public string scenario;
            public PerScenarioData data;
        }

        [SerializeField] internal ProbeVolumeAsset asset;
        [SerializeField] internal TextAsset cellSharedDataAsset; // Contains bricks and validity data
        [SerializeField] internal TextAsset cellSupportDataAsset; // Contains debug data
        [SerializeField] List<SerializablePerScenarioDataItem> serializedScenarios = new();

        internal Dictionary<string, PerScenarioData> scenarios = new();

        bool assetLoaded = false;
        string currentScenario = null, transitionScenario = null;

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            scenarios.Clear();
            foreach (var scenarioData in serializedScenarios)
                scenarios.Add(scenarioData.scenario, scenarioData.data);
        }

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            serializedScenarios.Clear();
            foreach (var kvp in scenarios)
            {
                serializedScenarios.Add(new SerializablePerScenarioDataItem()
                {
                    scenario = kvp.Key,
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
                foreach (var scenarioData in scenarios.Values)
                {
                    DeleteAsset(scenarioData.cellDataAsset);
                    DeleteAsset(scenarioData.cellOptionalDataAsset);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                EditorUtility.SetDirty(this);
            }
#endif

            scenarios.Clear();
        }

        internal void RemoveScenario(string scenario)
        {
#if UNITY_EDITOR
            if (scenarios.TryGetValue(scenario, out var scenarioData))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(scenarioData.cellDataAsset));
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(scenarioData.cellOptionalDataAsset));
                EditorUtility.SetDirty(this);
            }
#endif
            scenarios.Remove(scenario);
        }

        internal void RenameScenario(string scenario, string newName)
        {
            if (!scenarios.TryGetValue(scenario, out var data))
                return;
            scenarios.Remove(scenario);
            scenarios.Add(newName, data);

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            var baseName = ProbeVolumeAsset.assetName + "-" + newName;
            void RenameAsset(Object asset, string extension)
            {
                var oldPath = AssetDatabase.GetAssetPath(asset);
                AssetDatabase.RenameAsset(oldPath, baseName + extension);
            }
            RenameAsset(data.cellDataAsset, ".CellData.bytes");
            RenameAsset(data.cellOptionalDataAsset, ".CellOptionalData.bytes");
#endif
        }

        internal bool ResolveCells() => ResolveSharedCellData() && ResolvePerScenarioCellData();

        bool ResolveSharedCellData() => asset != null && asset.ResolveSharedCellData(cellSharedDataAsset, cellSupportDataAsset);
        bool ResolvePerScenarioCellData()
        {
            int loadedCount = 0;
            string state0 = transitionScenario != null ? transitionScenario : currentScenario;
            string state1 = transitionScenario != null ? currentScenario : null;
            if (state0 != null && scenarios.TryGetValue(state0, out var data0))
            {
                if (asset.ResolvePerScenarioCellData(data0.cellDataAsset, data0.cellOptionalDataAsset, 0))
                    loadedCount++;
            }
            if (state1 != null && scenarios.TryGetValue(state1, out var data1))
            {
                if (asset.ResolvePerScenarioCellData(data1.cellDataAsset, data1.cellOptionalDataAsset, loadedCount))
                    loadedCount++;
            }
            for (var i = 0; i < asset.cells.Length; ++i)
                asset.cells[i].hasTwoScenarios = loadedCount == 2;
            return loadedCount != 0;
        }

        internal void QueueAssetLoading()
        {
            if (asset == null || !ResolvePerScenarioCellData())
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
            currentScenario = transitionScenario = null;
            ProbeReferenceVolume.instance.UnregisterPerSceneData(this);
        }

        internal void Initialize()
        {
            ResolveSharedCellData();

            QueueAssetRemoval();
            currentScenario = ProbeReferenceVolume.instance.sceneData.lightingScenario;
            transitionScenario = null;
            QueueAssetLoading();
        }

        internal void UpdateActiveScenario(string state, string previousState)
        {
            if (asset == null)
                return;

            // if we just change state, don't need to queue anything
            // Just load state cells from disk and wait for blending to stream updates to gpu
            // When blending factor is < 0.5, streaming will upload cell from transition state
            // After that, it will always upload cells from current state
            // So gradually, all loaded cells, will be either blended towards new state, or replaced by streaming

            currentScenario = state;
            transitionScenario = previousState;
            if (!assetLoaded)
                QueueAssetLoading();
            else if (!ResolvePerScenarioCellData())
                QueueAssetRemoval();
        }

#if UNITY_EDITOR
        internal string GetAssetPathSafe(Object asset)
        {
            if (asset != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long instanceID))
                return AssetDatabase.GUIDToAssetPath(guid);
            return "";
        }

        internal void GetBlobFileNames(out string cellDataFilename, out string cellOptionalDataFilename, out string cellSharedDataFilename, out string cellSupportDataFilename)
        {
            var scenario = ProbeReferenceVolume.instance.lightingScenario;
            string basePath = Path.Combine(ProbeVolumeAsset.GetDirectory(gameObject.scene.path, gameObject.scene.name), ProbeVolumeAsset.assetName);

            string GetOrCreateFileName(Object o, string extension)
            {
                var res = AssetDatabase.GetAssetPath(o);
                if (string.IsNullOrEmpty(res)) res = basePath + extension;
                return res;
            }
            cellDataFilename = GetOrCreateFileName(scenarios[scenario].cellDataAsset, "-" + scenario + ".CellData.bytes");
            cellOptionalDataFilename = GetOrCreateFileName(scenarios[scenario].cellOptionalDataAsset, "-" + scenario + ".CellOptionalData.bytes");
            cellSharedDataFilename = GetOrCreateFileName(cellSharedDataAsset, ".CellSharedData.bytes");
            cellSupportDataFilename = GetOrCreateFileName(cellSupportDataAsset, ".CellSupportData.bytes");
        }

        // Returns the file size in bytes
        long GetFileSize(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;

        internal long GetDiskSizeOfSharedData()
        {
            return GetFileSize(GetAssetPathSafe(cellSharedDataAsset)) + GetFileSize(GetAssetPathSafe(cellSupportDataAsset));
        }

        internal long GetDiskSizeOfScenarioData(string scenario)
        {
            if (scenario == null || !scenarios.TryGetValue(scenario, out var data))
                return 0;
            return GetFileSize(GetAssetPathSafe(data.cellDataAsset)) + GetFileSize(GetAssetPathSafe(data.cellOptionalDataAsset));
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
