using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    // Add Profile and baking settings.
    /// <summary> A class containing info about the bounds defined by the probe volumes in various scenes. </summary>
    [System.Serializable]
    public class ProbeVolumeSceneData : ISerializationCallbackReceiver
    {
        static PropertyInfo s_SceneGUID = typeof(Scene).GetProperty("guid", System.Reflection.BindingFlags.NonPublic | BindingFlags.Instance);
        static internal string GetSceneGUID(Scene scene)
        {
            Debug.Assert(s_SceneGUID != null, "Reflection for scene GUID failed");
            return (string)s_SceneGUID.GetValue(scene);
        }

        [System.Serializable]
        struct SerializableBoundItem
        {
            [SerializeField] public string sceneGUID;
            [SerializeField] public Bounds bounds;
        }

        [System.Serializable]
        struct SerializableHasPVItem
        {
            [SerializeField] public string sceneGUID;
            [SerializeField] public bool hasProbeVolumes;
        }

        [System.Serializable]
        struct SerializablePVProfile
        {
            [SerializeField] public string sceneGUID;
            [SerializeField] public ProbeReferenceVolumeProfile profile;
        }

        [System.Serializable]
        struct SerializablePVBakeSettings
        {
            public string sceneGUID;
            public ProbeVolumeBakingProcessSettings settings;
        }

        [System.Serializable]
        internal class BakingSet
        {
            public string name;
            public List<string> sceneGUIDs = new List<string>();
            public ProbeVolumeBakingProcessSettings settings;
            public ProbeReferenceVolumeProfile profile;

            public List<string> lightingScenarios = new List<string>();

            internal string CreateScenario(string name)
            {
                if (lightingScenarios.Contains(name))
                {
                    string renamed;
                    int index = 1;
                    do
                        renamed = $"{name} ({index++})";
                    while (lightingScenarios.Contains(renamed));
                    name = renamed;
                }
                lightingScenarios.Add(name);
                return name;
            }

            internal bool RemoveScenario(string name)
            {
                return lightingScenarios.Remove(name);
            }
        }

        [SerializeField] List<SerializableBoundItem> serializedBounds;
        [SerializeField] List<SerializableHasPVItem> serializedHasVolumes;
        [SerializeField] List<SerializablePVProfile> serializedProfiles;
        [SerializeField] List<SerializablePVBakeSettings> serializedBakeSettings;

        [SerializeField] List<BakingSet> serializedBakingSets;

        internal Object parentAsset = null;
        internal string parentSceneDataPropertyName;
        /// <summary> A dictionary containing the Bounds defined by probe volumes for each scene (scene path is the key of the dictionary). </summary>
        public Dictionary<string, Bounds> sceneBounds;
        internal Dictionary<string, bool> hasProbeVolumes;
        internal Dictionary<string, ProbeReferenceVolumeProfile> sceneProfiles;
        internal Dictionary<string, ProbeVolumeBakingProcessSettings> sceneBakingSettings;
        internal List<BakingSet> bakingSets;

        [SerializeField] string m_LightingScenario = ProbeReferenceVolume.defaultLightingScenario;
        string m_OtherScenario = null;
        float m_ScenarioBlendingFactor = 0.0f;

        internal string lightingScenario => m_LightingScenario;
        internal string otherScenario => m_OtherScenario;
        internal float scenarioBlendingFactor => m_ScenarioBlendingFactor;

        internal void SetActiveScenario(string scenario)
        {
            if (m_LightingScenario == scenario && m_ScenarioBlendingFactor == 0.0f)
                return;

            m_LightingScenario = scenario;
            m_OtherScenario = null;
            m_ScenarioBlendingFactor = 0.0f;

            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                data.UpdateActiveScenario(m_LightingScenario, m_OtherScenario);

            if (ProbeReferenceVolume.instance.enableScenarioBlending)
            {
                // Trigger blending system to replace old cells with the one from the new active scenario.
                // Although we technically don't need blending for that, it is better than unloading all cells
                // because it will replace them progressively. There is no real performance cost to using blending
                // rather than regular load thanks to the bypassBlending branch in AddBlendingBricks.
                ProbeReferenceVolume.instance.ScenarioBlendingChanged(true);
            }
            else
                ProbeReferenceVolume.instance.UnloadAllCells();
        }

        internal void BlendLightingScenario(string otherScenario, float blendingFactor)
        {
            if (!ProbeReferenceVolume.instance.enableScenarioBlending)
            {
                if (!ProbeBrickBlendingPool.isSupported)
                    Debug.LogError("Blending between lighting scenarios is not supported by this render pipeline.");
                else
                    Debug.LogError("Blending between lighting scenarios is disabled in the render pipeline settings.");
                return;
            }

            blendingFactor = Mathf.Clamp01(blendingFactor);

            if (otherScenario == m_LightingScenario || string.IsNullOrEmpty(otherScenario))
                otherScenario = null;
            if (otherScenario == null)
                blendingFactor = 0.0f;
            if (otherScenario == m_OtherScenario && Mathf.Approximately(blendingFactor, m_ScenarioBlendingFactor))
                return;

            bool scenarioChanged = otherScenario != m_OtherScenario;
            m_OtherScenario = otherScenario;
            m_ScenarioBlendingFactor = blendingFactor;

            if (scenarioChanged)
            {
                foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                    data.UpdateActiveScenario(m_LightingScenario, m_OtherScenario);
            }

            ProbeReferenceVolume.instance.ScenarioBlendingChanged(scenarioChanged);
        }

        /// <summary>
        /// Constructor for ProbeVolumeSceneData.
        /// </summary>
        /// <param name="parentAsset">The asset holding this ProbeVolumeSceneData, it will be dirtied every time scene bounds or settings are changed.</param>
        /// <param name="parentSceneDataPropertyName">The name of the property holding the ProbeVolumeSceneData in the parentAsset.</param>
        public ProbeVolumeSceneData(Object parentAsset, string parentSceneDataPropertyName)
        {
            this.parentAsset = parentAsset;
            this.parentSceneDataPropertyName = parentSceneDataPropertyName;
            sceneBounds = new Dictionary<string, Bounds>();
            hasProbeVolumes = new Dictionary<string, bool>();
            sceneProfiles = new Dictionary<string, ProbeReferenceVolumeProfile>();
            sceneBakingSettings = new Dictionary<string, ProbeVolumeBakingProcessSettings>();
            bakingSets = new List<BakingSet>();

            serializedBounds = new List<SerializableBoundItem>();
            serializedHasVolumes = new List<SerializableHasPVItem>();
            serializedProfiles = new List<SerializablePVProfile>();
            serializedBakeSettings = new List<SerializablePVBakeSettings>();

            UpdateBakingSets();
        }

        /// <summary>Set a reference to the object holding this ProbeVolumeSceneData.</summary>
        /// <param name="parent">The object holding this ProbeVolumeSceneData, it will be dirtied every time scene bounds or settings are changed. </param>
        /// <param name="parentSceneDataPropertyName">The name of the property holding the ProbeVolumeSceneData in the parentAsset.</param>
        public void SetParentObject(Object parent, string parentSceneDataPropertyName)
        {
            parentAsset = parent;
            this.parentSceneDataPropertyName = parentSceneDataPropertyName;

            UpdateBakingSets();
        }

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // We haven't initialized the bounds, no need to do anything here.
            if (serializedBounds == null || serializedHasVolumes == null ||
                serializedProfiles == null || serializedBakeSettings == null) return;

            sceneBounds = new Dictionary<string, Bounds>();
            hasProbeVolumes = new Dictionary<string, bool>();
            sceneProfiles = new Dictionary<string, ProbeReferenceVolumeProfile>();
            sceneBakingSettings = new Dictionary<string, ProbeVolumeBakingProcessSettings>();
            bakingSets = new List<BakingSet>();

            foreach (var boundItem in serializedBounds)
            {
                sceneBounds.Add(boundItem.sceneGUID, boundItem.bounds);
            }

            foreach (var boundItem in serializedHasVolumes)
            {
                hasProbeVolumes.Add(boundItem.sceneGUID, boundItem.hasProbeVolumes);
            }

            foreach (var profileItem in serializedProfiles)
            {
                sceneProfiles.Add(profileItem.sceneGUID, profileItem.profile);
            }

            foreach (var settingsItem in serializedBakeSettings)
            {
                sceneBakingSettings.Add(settingsItem.sceneGUID, settingsItem.settings);
            }

            if (string.IsNullOrEmpty(m_LightingScenario))
                m_LightingScenario = ProbeReferenceVolume.defaultLightingScenario;

            foreach (var set in serializedBakingSets)
            {
                // Ensure baking set settings are up to date
                set.settings.Upgrade();

                bakingSets.Add(set);
            }

            if (m_OtherScenario == "")
                m_OtherScenario = null;
        }

        // This function must not be called during the serialization (because of asset creation)
        void UpdateBakingSets()
        {
            foreach (var set in serializedBakingSets)
            {
                // Small migration code to ensure that old sets have correct settings
                if (set.profile == null)
                    InitializeBakingSet(set, set.name);
                if (set.lightingScenarios.Count == 0)
                    InitializeScenarios(set);
            }

            // Initialize baking set in case it's empty:
            if (bakingSets.Count == 0)
            {
                var set = CreateNewBakingSet("Default");
                set.sceneGUIDs = serializedProfiles.Select(s => s.sceneGUID).ToList();
            }

            SyncBakingSetSettings();
        }

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        public void OnBeforeSerialize()
        {
            // We haven't initialized the bounds, no need to do anything here.
            if (sceneBounds == null || hasProbeVolumes == null || sceneBakingSettings == null || sceneProfiles == null ||
                serializedBounds == null || serializedHasVolumes == null || serializedBakeSettings == null || serializedProfiles == null
                || serializedBakingSets == null) return;

            serializedBounds.Clear();
            serializedHasVolumes.Clear();
            serializedProfiles.Clear();
            serializedBakeSettings.Clear();
            serializedBakingSets.Clear();

            foreach (var k in sceneBounds.Keys)
            {
                SerializableBoundItem item;
                item.sceneGUID = k;
                item.bounds = sceneBounds[k];
                serializedBounds.Add(item);
            }

            foreach (var k in hasProbeVolumes.Keys)
            {
                SerializableHasPVItem item;
                item.sceneGUID = k;
                item.hasProbeVolumes = hasProbeVolumes[k];
                serializedHasVolumes.Add(item);
            }

            foreach (var k in sceneBakingSettings.Keys)
            {
                SerializablePVBakeSettings item;
                item.sceneGUID = k;
                item.settings = sceneBakingSettings[k];
                serializedBakeSettings.Add(item);
            }

            foreach (var k in sceneProfiles.Keys)
            {
                SerializablePVProfile item;
                item.sceneGUID = k;
                item.profile = sceneProfiles[k];
                serializedProfiles.Add(item);
            }

            foreach (var set in bakingSets)
                serializedBakingSets.Add(set);
        }

        internal BakingSet CreateNewBakingSet(string name)
        {
            BakingSet set = new BakingSet();

            // Initialize new baking set settings
            InitializeBakingSet(set, name);
            bakingSets.Add(set);

            return set;
        }

        void InitializeBakingSet(BakingSet set, string name)
        {
            var newProfile = ScriptableObject.CreateInstance<ProbeReferenceVolumeProfile>();
#if UNITY_EDITOR
            ProjectWindowUtil.CreateAsset(newProfile, name + ".asset");
#endif

            set.name = name;
            set.profile = newProfile;
            set.settings = ProbeVolumeBakingProcessSettings.Default;

            InitializeScenarios(set);
        }

        void InitializeScenarios(BakingSet set)
        {
            set.lightingScenarios = new List<string>() { ProbeReferenceVolume.defaultLightingScenario };
        }

        internal void SyncBakingSetSettings()
        {
            // Sync all the scene settings in the set to avoid config mismatch.
            foreach (var set in bakingSets)
            {
                foreach (var guid in set.sceneGUIDs)
                {
                    sceneBakingSettings[guid] = set.settings;
                    sceneProfiles[guid] = set.profile;
                }
            }
        }

#if UNITY_EDITOR
        static internal int MaxSubdivLevelInProbeVolume(Vector3 volumeSize, int maxSubdiv)
        {
            float maxSizedDim = Mathf.Max(volumeSize.x, Mathf.Max(volumeSize.y, volumeSize.z));
            float maxSideInBricks = maxSizedDim / ProbeReferenceVolume.instance.MinDistanceBetweenProbes();
            int absoluteMaxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;
            int subdivLevel = Mathf.FloorToInt(Mathf.Log(maxSideInBricks, 3)) - 1;
            return Mathf.Max(subdivLevel, absoluteMaxSubdiv - maxSubdiv);
        }

        private void InflateBound(ref Bounds bounds, ProbeVolume pv)
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

        // Should be called after EnsureSceneIsInBakingSet otherwise GetProfileForScene might be out of date
        internal void UpdateSceneBounds(Scene scene)
        {
            var volumes = Object.FindObjectsOfType<ProbeVolume>();
            float prevBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
            int prevMaxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision();

            {
                var profile = GetProfileForScene(scene);
                if (profile == null)
                {
                    if (volumes.Length > 0)
                        Debug.LogWarning("A probe volume is present in the scene but a profile has not been set. Please configure a profile for your scene in the Probe Volume Baking settings.");
                    return;
                }
                ProbeReferenceVolume.instance.SetMinBrickAndMaxSubdiv(profile.minBrickSize, profile.maxSubdivision);
            }

            var sceneGUID = GetSceneGUID(scene);
            bool boundFound = false;
            Bounds newBound = new Bounds();
            foreach (var volume in volumes)
            {
                if (volume.gameObject.scene != scene)
                    continue;

                if (volume.globalVolume)
                    volume.UpdateGlobalVolume();

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

            hasProbeVolumes[sceneGUID] = boundFound;
            if (boundFound)
                sceneBounds[sceneGUID] = newBound;

            ProbeReferenceVolume.instance.SetMinBrickAndMaxSubdiv(prevBrickSize, prevMaxSubdiv);

            if (parentAsset != null)
                EditorUtility.SetDirty(parentAsset);
        }

        // It is important this is called after UpdateSceneBounds is called otherwise SceneHasProbeVolumes might be out of date
        internal void EnsurePerSceneData(Scene scene)
        {
            var sceneGUID = GetSceneGUID(scene);
            if (SceneHasProbeVolumes(sceneGUID))
            {
                bool foundPerSceneData = false;
                foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                {
                    if (GetSceneGUID(data.gameObject.scene) == sceneGUID)
                    {
                        foundPerSceneData = true;
                        break;
                    }
                }

                if (!foundPerSceneData)
                {
                    GameObject go = new GameObject("ProbeVolumePerSceneData");
                    go.hideFlags |= HideFlags.HideInHierarchy;
                    go.AddComponent<ProbeVolumePerSceneData>();
                    SceneManager.MoveGameObjectToScene(go, scene);
                }
            }
        }

        internal void EnsureSceneIsInBakingSet(Scene scene)
        {
            var sceneGUID = GetSceneGUID(scene);

            foreach (var set in bakingSets)
                if (set.sceneGUIDs.Contains(sceneGUID))
                    return;

            // The scene is not in a baking set, we need to add it
            if (bakingSets.Count == 0)
                return; // Technically shouldn't be possible since it's blocked in the UI

            bakingSets[0].sceneGUIDs.Add(sceneGUID);
            SyncBakingSetSettings();
        }

        internal string GetFirstProbeVolumeSceneGUID(ProbeVolumeSceneData.BakingSet set)
        {
            foreach (var guid in set.sceneGUIDs)
            {
                if (sceneBakingSettings.ContainsKey(guid) && sceneProfiles.ContainsKey(guid))
                    return guid;
            }
            return null;
        }

        internal void OnSceneSaved(Scene scene)
        {
            EnsureSceneIsInBakingSet(scene);
            UpdateSceneBounds(scene);
            EnsurePerSceneData(scene);
        }

        internal void SetProfileForScene(Scene scene, ProbeReferenceVolumeProfile profile)
        {
            if (sceneProfiles == null) sceneProfiles = new Dictionary<string, ProbeReferenceVolumeProfile>();

            var sceneGUID = GetSceneGUID(scene);
            sceneProfiles[sceneGUID] = profile;
        }

        internal void SetProfileForScene(string sceneGUID, ProbeReferenceVolumeProfile profile)
        {
            if (sceneProfiles == null) sceneProfiles = new Dictionary<string, ProbeReferenceVolumeProfile>();
            sceneProfiles[sceneGUID] = profile;
        }

        internal void SetBakeSettingsForScene(Scene scene, ProbeDilationSettings dilationSettings, VirtualOffsetSettings virtualOffsetSettings)
        {
            if (sceneBakingSettings == null) sceneBakingSettings = new Dictionary<string, ProbeVolumeBakingProcessSettings>();

            var sceneGUID = GetSceneGUID(scene);
            sceneBakingSettings[sceneGUID] = new ProbeVolumeBakingProcessSettings(dilationSettings, virtualOffsetSettings);
        }

        internal ProbeReferenceVolumeProfile GetProfileForScene(Scene scene)
        {
            var sceneGUID = GetSceneGUID(scene);
            if (sceneProfiles != null && sceneProfiles.ContainsKey(sceneGUID))
                return sceneProfiles[sceneGUID];

            return null;
        }

        internal bool BakeSettingsDefinedForScene(Scene scene)
        {
            var sceneGUID = GetSceneGUID(scene);
            return sceneBakingSettings.ContainsKey(sceneGUID);
        }

        internal ProbeVolumeBakingProcessSettings GetBakeSettingsForScene(Scene scene)
        {
            var sceneGUID = GetSceneGUID(scene);
            if (sceneBakingSettings != null && sceneBakingSettings.ContainsKey(sceneGUID))
                return sceneBakingSettings[sceneGUID];

            return ProbeVolumeBakingProcessSettings.Default;
        }

        // This is sub-optimal, but because is called once when kicking off a bake
        internal BakingSet GetBakingSetForScene(Scene scene)
        {
            var sceneGUID = GetSceneGUID(scene);
            foreach (var set in bakingSets)
            {
                foreach (var guidInSet in set.sceneGUIDs)
                {
                    if (guidInSet == sceneGUID)
                        return set;
                }
            }

            return null;
        }

        internal bool SceneHasProbeVolumes(string sceneGUID) => hasProbeVolumes != null && hasProbeVolumes.TryGetValue(sceneGUID, out var hasPV) && hasPV;
        internal bool SceneHasProbeVolumes(Scene scene) => SceneHasProbeVolumes(GetSceneGUID(scene));
#endif
    }
}
