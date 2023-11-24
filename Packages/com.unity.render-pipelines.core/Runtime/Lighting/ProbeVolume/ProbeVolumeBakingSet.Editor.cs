#if UNITY_EDITOR

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering
{
    // Everything here is only needed in editor for baking or managing baking sets.
    public partial class ProbeVolumeBakingSet
    {
        [Serializable]
        internal class SceneBakeData
        {
            public bool hasProbeVolume = false;
            public bool bakeScene = true;
            public Bounds bounds = new();
        }

        [SerializeField]
        SerializedDictionary<string, SceneBakeData> m_SceneBakeData = new();
        static Dictionary<string, ProbeVolumeBakingSet> sceneToBakingSet = new Dictionary<string, ProbeVolumeBakingSet>();

        /// <summary>
        /// Tries to add a scene to the baking set.
        /// </summary>
        /// <param name ="guid">The GUID of the scene to add.</param>
        /// <returns>Whether the scene was successfull added to the baking set.</returns>
        public bool TryAddScene(string guid)
        {
            var sceneSet = GetBakingSetForScene(guid);
            if (sceneSet != null)
                return false;
            AddScene(guid);
            return true;
        }

        internal void AddScene(string guid, SceneBakeData bakeData = null)
        {
            m_SceneGUIDs.Add(guid);
            m_SceneBakeData.Add(guid, bakeData != null ? bakeData : new SceneBakeData());
            sceneToBakingSet[guid] = this;

            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Removes a scene from the baking set.
        /// </summary>
        /// <param name ="guid">The GUID of the scene to remove.</param>
        public void RemoveScene(string guid)
        {
            m_SceneGUIDs.Remove(guid);
            m_SceneBakeData.Remove(guid);
            sceneToBakingSet.Remove(guid);

            EditorUtility.SetDirty(this);
        }

        internal void SetScene(string guid, int index, SceneBakeData bakeData = null)
        {
            var previousSceneGUID = m_SceneGUIDs[index];
            m_SceneGUIDs[index] = guid;
            sceneToBakingSet.Remove(previousSceneGUID);
            sceneToBakingSet[guid] = this;
            m_SceneBakeData.Add(guid, bakeData != null ? bakeData : new SceneBakeData());

            EditorUtility.SetDirty(this);
        }

        internal void MoveSceneToBakingSet(string guid, int index)
        {
            var oldBakingSet = GetBakingSetForScene(guid);
            var oldBakeData = oldBakingSet.GetSceneBakeData(guid);

            if (oldBakingSet.singleSceneMode)
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(oldBakingSet));
            else
                oldBakingSet.RemoveScene(guid);

            if (index == -1)
                AddScene(guid, oldBakeData);
            else
                SetScene(guid, index, oldBakeData);
        }

        /// <summary>
        /// Changes the baking status of a scene. Objects in scenes disabled for baking will still contribute to
        /// lighting for other scenes.
        /// </summary>
        /// <param name ="guid">The GUID of the scene to remove.</param>
        /// <param name ="enableForBaking">Wheter or not this scene should be included when baking lighting.</param>
        public void SetSceneBaking(string guid, bool enableForBaking)
        {
            if (m_SceneBakeData.TryGetValue(guid, out var sceneData))
                sceneData.bakeScene = enableForBaking;

            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Changes the baking status of all scenes. Objects in scenes disabled for baking will still contribute to
        /// lighting for other scenes.
        /// </summary>
        /// <param name="enableForBaking">Wheter or not scenes should be included when baking lighting.</param>
        public void SetAllSceneBaking(bool enableForBaking)
        {
            foreach (var kvp in m_SceneBakeData)
                kvp.Value.bakeScene = enableForBaking;

            EditorUtility.SetDirty(this);
        }


        /// <summary>
        /// Tries to add a lighting scenario to the baking set.
        /// </summary>
        /// <param name ="name">The name of the scenario to add.</param>
        /// <returns>Whether the scenario was successfully created.</returns>
        /// 
        public bool TryAddScenario(string name)
        {
            if (m_LightingScenarios.Contains(name))
                return false;
            m_LightingScenarios.Add(name);
            EditorUtility.SetDirty(this);

            return true;
        }

        internal string CreateScenario(string name)
        {
            int index = 1;
            string renamed = name;
            while (!TryAddScenario(renamed))
                renamed = $"{name} ({index++})";

            return renamed;
        }

        internal bool RemoveScenario(string name)
        {
            if (scenarios.TryGetValue(name, out var scenarioData))
            {
                AssetDatabase.DeleteAsset(scenarioData.cellDataAsset.GetAssetPath());
                AssetDatabase.DeleteAsset(scenarioData.cellOptionalDataAsset.GetAssetPath());
                EditorUtility.SetDirty(this);
            }

            foreach (var cellData in cellDataMap.Values)
            {
                if (cellData.scenarios.TryGetValue(name, out var cellScenarioData))
                {
                    cellData.CleanupPerScenarioData(cellScenarioData);
                    cellData.scenarios.Remove(name);
                }
            }

            scenarios.Remove(name);

            EditorUtility.SetDirty(this);
            return m_LightingScenarios.Remove(name);
        }

        internal ProbeVolumeBakingSet Clone()
        {
            var newSet = Instantiate(this);
            newSet.m_SceneGUIDs.Clear();
            newSet.m_SceneBakeData.Clear();
            return newSet;
        }

        /// <summary>
        /// Determines if the Probe Reference Volume Profile is equivalent to another one.
        /// </summary>
        /// <param name ="otherProfile">The profile to compare with.</param>
        /// <returns>Whether the Probe Reference Volume Profile is equivalent to another one.</returns>
        public bool IsEquivalent(ProbeVolumeBakingSet otherProfile)
        {
            return minDistanceBetweenProbes == otherProfile.minDistanceBetweenProbes &&
                cellSizeInMeters == otherProfile.cellSizeInMeters &&
                simplificationLevels == otherProfile.simplificationLevels &&
                renderersLayerMask == otherProfile.renderersLayerMask;
        }

        internal void Clear()
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                if (cellBricksDataAsset != null)
                {
                    DeleteAsset(cellBricksDataAsset.GetAssetPath());
                    DeleteAsset(cellSharedDataAsset.GetAssetPath());
                    DeleteAsset(cellSupportDataAsset.GetAssetPath());
                    cellBricksDataAsset = null;
                    cellSharedDataAsset = null;
                    cellSupportDataAsset = null;
                }
                foreach (var scenarioData in scenarios.Values)
                {
                    if (scenarioData.IsValid())
                    {
                        DeleteAsset(scenarioData.cellDataAsset.GetAssetPath());
                        DeleteAsset(scenarioData.cellOptionalDataAsset.GetAssetPath());
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                EditorUtility.SetDirty(this);
            }

            cellDescs.Clear();
            scenarios.Clear();

            // All cells should have been released through unloading the scenes first.
            Debug.Assert(cellDataMap.Count == 0);

            perSceneCellLists.Clear();
            foreach (var sceneGUID in sceneGUIDs)
                perSceneCellLists.Add(sceneGUID, new List<int>());
        }


        internal string RenameScenario(string scenario, string newName)
        {
            if (!m_LightingScenarios.Contains(scenario))
                return newName;

            m_LightingScenarios.Remove(scenario);
            newName = CreateScenario(newName);

            // If the scenario was not baked at least once, this does not exist.
            if (scenarios.TryGetValue(scenario, out var data))
            {
                scenarios.Remove(scenario);
                scenarios.Add(newName, data);

                foreach (var cellData in cellDataMap.Values)
                {
                    if (cellData.scenarios.TryGetValue(scenario, out var cellScenarioData))
                    {
                        cellData.scenarios.Add(newName, cellScenarioData);
                        cellData.scenarios.Remove(scenario);
                    }
                }

                GetCellDataFileNames(name, newName, out string cellDataFileName, out string cellOptionalDataFileName);
                data.cellDataAsset.RenameAsset(cellDataFileName);
                data.cellOptionalDataAsset.RenameAsset(cellOptionalDataFileName);
            }

            return newName;
        }

        internal static void SyncBakingSets()
        {
            sceneToBakingSet = new Dictionary<string, ProbeVolumeBakingSet>();

            var setGUIDs = AssetDatabase.FindAssets("t:" + typeof(ProbeVolumeBakingSet).Name);

            foreach (var setGUID in setGUIDs)
            {
                var set = AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(AssetDatabase.GUIDToAssetPath(setGUID));
                if (set != null)
                {
                    // We need to call Migrate here because of Version.RemoveProbeVolumeSceneData step.
                    // This step needs the obsolete ProbeVolumeSceneData to be initialized first which can happen out of order. Here we now it's ok.
                    set.Migrate();

                    foreach (var guid in set.sceneGUIDs)
                        sceneToBakingSet[guid] = set;
                }
            }
        }

        internal static ProbeVolumeBakingSet GetBakingSetForScene(string sceneGUID) => sceneToBakingSet.GetValueOrDefault(sceneGUID, null);
        internal static ProbeVolumeBakingSet GetBakingSetForScene(Scene scene) => GetBakingSetForScene(scene.GetGUID());

        internal void SetDefaults()
        {
            settings.SetDefaults();
            m_LightingScenarios = new List<string> { ProbeReferenceVolume.defaultLightingScenario };

            // We have to initialize that to not trigger a warning on new baking sets
            chunkSizeInBricks = ProbeBrickPool.GetChunkSizeInBrickCount();

        }

        string GetOrCreateFileName(ProbeVolumeStreamableAsset asset, string filePath)
        {
            string res = string.Empty;
            if (asset != null && asset.IsValid())
                res = asset.GetAssetPath();
            if (string.IsNullOrEmpty(res))
                res = filePath;
            return res;
        }

        internal void EnsureScenarioAssetNameConsistencyForUndo()
        {
            foreach (var scenario in scenarios)
            {
                var scenarioName = scenario.Key;
                var scenarioData = scenario.Value;

                GetCellDataFileNames(name, scenarioName, out string cellDataFileName, out string cellOptionalDataFileName);

                if (!scenarioData.cellDataAsset.GetAssetPath().Contains(cellDataFileName))
                {
                    scenarioData.cellDataAsset.RenameAsset(cellDataFileName);
                    scenarioData.cellOptionalDataAsset.RenameAsset(cellOptionalDataFileName);
                }
            }
        }

        internal void GetCellDataFileNames(string basePath, string scenario, out string cellDataFileName, out string cellOptionalDataFileName)
        {
            cellDataFileName = $"{basePath}-{scenario}.CellData.bytes";
            cellOptionalDataFileName = $"{basePath}-{scenario}.CellOptionalData.bytes";
        }

        internal void GetBlobFileNames(string scenario, out string cellDataFilename, out string cellBricksDataFilename, out string cellOptionalDataFilename, out string cellSharedDataFilename, out string cellSupportDataFilename)
        {
            string baseDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));

            string basePath = Path.Combine(baseDir, name);

            GetCellDataFileNames(basePath, scenario, out string dataFile, out string optionalDataFile);

            cellDataFilename = GetOrCreateFileName(scenarios[scenario].cellDataAsset, dataFile);
            cellOptionalDataFilename = GetOrCreateFileName(scenarios[scenario].cellOptionalDataAsset, optionalDataFile);
            cellBricksDataFilename = GetOrCreateFileName(cellBricksDataAsset, basePath + ".CellBricksData.bytes");
            cellSharedDataFilename = GetOrCreateFileName(cellSharedDataAsset, basePath + ".CellSharedData.bytes");
            cellSupportDataFilename = GetOrCreateFileName(cellSupportDataAsset, basePath + ".CellSupportData.bytes");
        }

        // Returns the file size in bytes
        long GetFileSize(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;

        internal long GetDiskSizeOfSharedData()
        {
            if (cellSharedDataAsset == null || !cellSharedDataAsset.IsValid())
                return 0;

            return GetFileSize(cellBricksDataAsset.GetAssetPath()) + GetFileSize(cellSharedDataAsset.GetAssetPath()) + GetFileSize(cellSupportDataAsset.GetAssetPath());
        }

        internal long GetDiskSizeOfScenarioData(string scenario)
        {
            if (scenario == null || !scenarios.TryGetValue(scenario, out var data) || !data.IsValid())
                return 0;

            return GetFileSize(data.cellDataAsset.GetAssetPath()) + GetFileSize(data.cellOptionalDataAsset.GetAssetPath());
        }

        internal void SanitizeScenes()
        {
            // Remove entries in the list pointing to deleted scenes
            for (int i = m_SceneGUIDs.Count - 1; i >= 0; i--)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(m_SceneGUIDs[i]);
                if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(path) == null)
                    RemoveScene(m_SceneGUIDs[i]);
            }
        }

        void DeleteAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

            AssetDatabase.DeleteAsset(assetPath);
        }

        internal bool HasBeenBaked()
        {
            return cellSharedDataAsset.IsValid();
        }

        internal static string GetDirectory(string scenePath, string sceneName)
        {
            string sceneDir = Path.GetDirectoryName(scenePath);
            string assetPath = Path.Combine(sceneDir, sceneName);
            if (!AssetDatabase.IsValidFolder(assetPath))
                AssetDatabase.CreateFolder(sceneDir, sceneName);

            return assetPath;
        }


        internal static void OnSceneSaving(Scene scene, string path = null)
        {
            // If we are called from the scene callback, we want to update all global volumes that are potentially affected
            bool onSceneSave = path != null;

            string sceneGUID = ProbeReferenceVolume.GetSceneGUID(scene);
            var bakingSet = GetBakingSetForScene(sceneGUID);

            if (bakingSet != null)
            {
                bakingSet.UpdateSceneBounds(scene, sceneGUID, onSceneSave);
                bakingSet.EnsurePerSceneData(scene, sceneGUID);
            }
        }

        static internal int MaxSubdivLevelInProbeVolume(Vector3 volumeSize, int maxSubdiv)
        {
            float maxSizedDim = Mathf.Max(volumeSize.x, Mathf.Max(volumeSize.y, volumeSize.z));
            float maxSideInBricks = maxSizedDim / ProbeReferenceVolume.instance.MinDistanceBetweenProbes();
            int absoluteMaxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;
            int subdivLevel = Mathf.FloorToInt(Mathf.Log(maxSideInBricks, 3)) - 1;
            return Mathf.Max(subdivLevel, absoluteMaxSubdiv - maxSubdiv);
        }

        static void InflateBound(ref Bounds bounds, ProbeVolume pv)
        {
            Bounds originalBounds = bounds;
            // Round the probe volume bounds to cell size
            float cellSize = ProbeReferenceVolume.instance.MaxBrickSize();

            // Expand the probe volume bounds to snap on the cell size grid
            bounds.Encapsulate(new Vector3(cellSize * Mathf.Floor(bounds.min.x / cellSize),
                cellSize * Mathf.Floor(bounds.min.y / cellSize),
                cellSize * Mathf.Floor(bounds.min.z / cellSize)));
            bounds.Encapsulate(new Vector3(cellSize * Mathf.Ceil(bounds.max.x / cellSize),
                cellSize * Mathf.Ceil(bounds.max.y / cellSize),
                cellSize * Mathf.Ceil(bounds.max.z / cellSize)));

            // calculate how much padding we need to remove according to the brick generation in ProbePlacement.cs:
            var cellSizeVector = new Vector3(cellSize, cellSize, cellSize);
            var minPadding = (bounds.min - originalBounds.min);
            var maxPadding = (bounds.max - originalBounds.max);
            minPadding = cellSizeVector - new Vector3(Mathf.Abs(minPadding.x), Mathf.Abs(minPadding.y), Mathf.Abs(minPadding.z));
            maxPadding = cellSizeVector - new Vector3(Mathf.Abs(maxPadding.x), Mathf.Abs(maxPadding.y), Mathf.Abs(maxPadding.z));

            // Find the size of the brick we can put for every axis given the padding size
            int maxSubdiv = (pv.overridesSubdivLevels ? pv.highestSubdivLevelOverride : 0);
            float rightPaddingSubdivLevel = ProbeReferenceVolume.instance.BrickSize(MaxSubdivLevelInProbeVolume(new Vector3(maxPadding.x, originalBounds.size.y, originalBounds.size.z), maxSubdiv));
            float leftPaddingSubdivLevel = ProbeReferenceVolume.instance.BrickSize(MaxSubdivLevelInProbeVolume(new Vector3(minPadding.x, originalBounds.size.y, originalBounds.size.z), maxSubdiv));
            float topPaddingSubdivLevel = ProbeReferenceVolume.instance.BrickSize(MaxSubdivLevelInProbeVolume(new Vector3(originalBounds.size.x, maxPadding.y, originalBounds.size.z), maxSubdiv));
            float bottomPaddingSubdivLevel = ProbeReferenceVolume.instance.BrickSize(MaxSubdivLevelInProbeVolume(new Vector3(originalBounds.size.x, minPadding.y, originalBounds.size.z), maxSubdiv));
            float forwardPaddingSubdivLevel = ProbeReferenceVolume.instance.BrickSize(MaxSubdivLevelInProbeVolume(new Vector3(originalBounds.size.x, originalBounds.size.y, maxPadding.z), maxSubdiv));
            float backPaddingSubdivLevel = ProbeReferenceVolume.instance.BrickSize(MaxSubdivLevelInProbeVolume(new Vector3(originalBounds.size.x, originalBounds.size.y, minPadding.z), maxSubdiv));
            // Remove the extra padding caused by cell rounding
            bounds.min = bounds.min + new Vector3(
                leftPaddingSubdivLevel * Mathf.Floor(Mathf.Abs(bounds.min.x - originalBounds.min.x) / (float)leftPaddingSubdivLevel),
                bottomPaddingSubdivLevel * Mathf.Floor(Mathf.Abs(bounds.min.y - originalBounds.min.y) / (float)bottomPaddingSubdivLevel),
                backPaddingSubdivLevel * Mathf.Floor(Mathf.Abs(bounds.min.z - originalBounds.min.z) / (float)backPaddingSubdivLevel)
            );
            bounds.max = bounds.max - new Vector3(
                rightPaddingSubdivLevel * Mathf.Floor(Mathf.Abs(bounds.max.x - originalBounds.max.x) / (float)rightPaddingSubdivLevel),
                topPaddingSubdivLevel * Mathf.Floor(Mathf.Abs(bounds.max.y - originalBounds.max.y) / (float)topPaddingSubdivLevel),
                forwardPaddingSubdivLevel * Mathf.Floor(Mathf.Abs(bounds.max.z - originalBounds.max.z) / (float)forwardPaddingSubdivLevel)
            );
        }

        internal void UpdateSceneBounds(Scene scene, string sceneGUID, bool onSceneSave)
        {
            var volumes = Object.FindObjectsByType<ProbeVolume>(FindObjectsSortMode.InstanceID);
            float prevBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
            int prevMaxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision();

            if (onSceneSave)
                ProbeReferenceVolume.instance.SetMinBrickAndMaxSubdiv(minBrickSize, maxSubdivision);
            else
                ProbeReferenceVolume.instance.SetMinBrickAndMaxSubdiv(ProbeVolumeBakingSet.GetMinBrickSize(minDistanceBetweenProbes), ProbeVolumeBakingSet.GetMaxSubdivision(simplificationLevels));

            bool boundFound = false;
            Bounds newBound = new Bounds();
            foreach (var volume in volumes)
            {
                bool forceUpdate = onSceneSave && volume.mode == ProbeVolume.Mode.Global;
                if (!forceUpdate && volume.gameObject.scene != scene)
                    continue;

                if (volume.mode != ProbeVolume.Mode.Local)
                    volume.UpdateGlobalVolume(volume.mode == ProbeVolume.Mode.Global ? GIContributors.ContributorFilter.All : GIContributors.ContributorFilter.Scene);

                var transform = volume.gameObject.transform;
                var obb = new ProbeReferenceVolume.Volume(Matrix4x4.TRS(transform.position, transform.rotation, volume.GetExtents()), 0, 0);
                Bounds localBounds = obb.CalculateAABB();

                InflateBound(ref localBounds, volume);

                if (!boundFound)
                {
                    newBound = localBounds;
                    boundFound = true;
                }
                else
                {
                    newBound.Encapsulate(localBounds);
                }
            }

            bool bakeDataExist = m_SceneBakeData.TryGetValue(sceneGUID, out var bakeData);
            Debug.Assert(bakeDataExist, "Scene should have been added to the baking set with default bake data instance.");
            bakeData.hasProbeVolume = boundFound;
            if (boundFound)
                bakeData.bounds = newBound;

            ProbeReferenceVolume.instance.SetMinBrickAndMaxSubdiv(prevBrickSize, prevMaxSubdiv);
            EditorUtility.SetDirty(this);
        }

        // It is important this is called after UpdateSceneBounds is called otherwise SceneHasProbeVolumes might be out of date
        internal void EnsurePerSceneData(Scene scene, string sceneGUID)
        {
            bool bakeDataExist = m_SceneBakeData.TryGetValue(sceneGUID, out var bakeData);
            Debug.Assert(bakeDataExist, "Scene should have been added to the baking set with default bake data instance.");

            if (bakeData.hasProbeVolume)
            {
                if (!ProbeReferenceVolume.instance.TryGetPerSceneData(sceneGUID, out var data))
                {
                    GameObject go = new GameObject("ProbeVolumePerSceneData");
                    go.hideFlags |= HideFlags.HideInHierarchy;
                    var perSceneData = go.AddComponent<ProbeVolumePerSceneData>();
                    perSceneData.sceneGUID = sceneGUID;
                    SceneManager.MoveGameObjectToScene(go, scene);
                }
                else
                {
                    data.sceneGUID = sceneGUID; // Upgrade for older scenes.
                }
            }
        }

        internal SceneBakeData GetSceneBakeData(string sceneGUID)
        {
            if (m_SceneBakeData.TryGetValue(sceneGUID, out var bakeData))
                return bakeData;
            return null;
        }

        internal static bool SceneHasProbeVolumes(string sceneGUID)
        {
            var bakingSet = GetBakingSetForScene(sceneGUID);
            return bakingSet.GetSceneBakeData(sceneGUID).hasProbeVolume;
        }
		
		internal bool DialogNoProbeVolumeInSetShown()
        {
            return dialogNoProbeVolumeInSetShown;
        }

        internal void SetDialogNoProbeVolumeInSetShown(bool value)
        {
            dialogNoProbeVolumeInSetShown = value;
        }
    }
}

#endif
