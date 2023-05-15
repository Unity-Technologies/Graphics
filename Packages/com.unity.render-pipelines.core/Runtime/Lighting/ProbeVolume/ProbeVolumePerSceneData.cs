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
        string activeScenario = null, otherScenario = null;

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

        internal bool ResolveSharedCellData() => asset != null && asset.ResolveSharedCellData(cellSharedDataAsset, cellSupportDataAsset);
        bool ResolvePerScenarioCellData()
        {
            int loadedCount = 0, targetLoaded = otherScenario == null ? 1 : 2;
            if (activeScenario != null && scenarios.TryGetValue(activeScenario, out var data0))
            {
                if (asset.ResolvePerScenarioCellData(data0.cellDataAsset, data0.cellOptionalDataAsset, 0))
                    loadedCount++;
            }
            if (otherScenario != null && scenarios.TryGetValue(otherScenario, out var data1))
            {
                if (asset.ResolvePerScenarioCellData(data1.cellDataAsset, data1.cellOptionalDataAsset, loadedCount))
                    loadedCount++;
            }
            for (var i = 0; i < asset.cells.Length; ++i)
                asset.cells[i].hasTwoScenarios = loadedCount == 2;
            return loadedCount == targetLoaded;
        }

        internal void QueueAssetLoading()
        {
            if (asset == null || asset.IsInvalid() || !ResolvePerScenarioCellData())
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
            activeScenario = otherScenario = null;
            ProbeReferenceVolume.instance.UnregisterPerSceneData(this);
        }

        internal void Initialize()
        {
            ResolveSharedCellData();

            QueueAssetRemoval();
            activeScenario = ProbeReferenceVolume.instance.sceneData.lightingScenario;
            otherScenario = ProbeReferenceVolume.instance.sceneData.otherScenario;
            QueueAssetLoading();
        }

        internal void UpdateActiveScenario(string activeScenario, string otherScenario)
        {
            if (asset == null)
                return;

            // if we just change scenario, don't need to queue anything
            // Just load cells from disk and wait for blending to stream updates to gpu

            this.activeScenario = activeScenario;
            this.otherScenario = otherScenario;
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
