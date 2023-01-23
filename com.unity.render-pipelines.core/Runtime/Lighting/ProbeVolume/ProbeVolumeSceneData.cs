using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    internal static class SceneExtensions
    {
        static PropertyInfo s_SceneGUID = typeof(Scene).GetProperty("guid", BindingFlags.NonPublic | BindingFlags.Instance);
        public static string GetGUID(this Scene scene)
        {
            Debug.Assert(s_SceneGUID != null, "Reflection for scene GUID failed");
            return (string)s_SceneGUID.GetValue(scene);
        }
    }

    // Add Profile and baking settings.
    /// <summary> A class containing info about the bounds defined by the probe volumes in various scenes. </summary>
    [System.Serializable]
    public class ProbeVolumeSceneData : ISerializationCallbackReceiver
    {
        static internal string GetSceneGUID(Scene scene) => scene.GetGUID();

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
        internal class BakingSet
        {
            public ProbeVolumeBakingSet profile;
            public ProbeVolumeBakingProcessSettings settings;

            public List<string> sceneGUIDs = new List<string>();
            public List<string> lightingScenarios = new List<string>();
        }

#if UNITY_EDITOR
        [SerializeField, FormerlySerializedAs("serializedBakingSets")] List<BakingSet> m_ObsoleteSerializedBakingSets;
#endif

        [SerializeField] List<SerializableBoundItem> serializedBounds;
        [SerializeField] List<SerializableHasPVItem> serializedHasVolumes;
        [SerializeField] List<ProbeVolumeBakingSet> bakingSets;

        internal Object parentAsset = null;
        internal string parentSceneDataPropertyName;
        /// <summary> A dictionary containing the Bounds defined by probe volumes for each scene (scene path is the key of the dictionary). </summary>
        public Dictionary<string, Bounds> sceneBounds;
        internal Dictionary<string, bool> hasProbeVolumes;
        internal Dictionary<string, ProbeVolumeBakingSet> sceneToBakingSet;

        [SerializeField] string m_LightingScenario = ProbeReferenceVolume.defaultLightingScenario;
        string m_OtherScenario = null;
        float m_ScenarioBlendingFactor = 0.0f;

        internal string lightingScenario => ProbeReferenceVolume.instance.supportLightingScenarios ? m_LightingScenario : ProbeReferenceVolume.defaultLightingScenario;
        internal string otherScenario => m_OtherScenario;
        internal float scenarioBlendingFactor => m_ScenarioBlendingFactor;

        internal void SetActiveScenario(string scenario, bool verbose = true)
        {
            if (!ProbeReferenceVolume.instance.supportLightingScenarios)
            {
                Debug.LogError("Lighting scenarios are not supported by this render pipeline.");
                return;
            }

            if (m_LightingScenario == scenario && m_ScenarioBlendingFactor == 0.0f)
                return;

            m_LightingScenario = scenario;
            m_OtherScenario = null;
            m_ScenarioBlendingFactor = 0.0f;

            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                data.UpdateActiveScenario(m_LightingScenario, m_OtherScenario, verbose);

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
                    data.UpdateActiveScenario(m_LightingScenario, m_OtherScenario, true);
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
            serializedBounds = new List<SerializableBoundItem>();
            serializedHasVolumes = new List<SerializableHasPVItem>();

            SetParentObject(parentAsset, parentSceneDataPropertyName);
        }

        /// <summary>Set a reference to the object holding this ProbeVolumeSceneData.</summary>
        /// <param name="parent">The object holding this ProbeVolumeSceneData, it will be dirtied every time scene bounds or settings are changed. </param>
        /// <param name="parentSceneDataPropertyName">The name of the property holding the ProbeVolumeSceneData in the parentAsset.</param>
        public void SetParentObject(Object parent, string parentSceneDataPropertyName)
        {
            parentAsset = parent;
            this.parentSceneDataPropertyName = parentSceneDataPropertyName;

            if (bakingSets == null) bakingSets = new();
            if (sceneBounds == null) sceneBounds = new();
            if (hasProbeVolumes == null) hasProbeVolumes = new();
            if (sceneToBakingSet == null) sceneToBakingSet = new();

            if (string.IsNullOrEmpty(m_LightingScenario))
                m_LightingScenario = ProbeReferenceVolume.defaultLightingScenario;

            SyncBakingSets();
            MigrateBakingSets();
        }

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // We haven't initialized the bounds, no need to do anything here.
            if (serializedBounds == null || serializedHasVolumes == null || bakingSets == null) return;

            sceneBounds = new();
            hasProbeVolumes = new();
            sceneToBakingSet = new();

            foreach (var boundItem in serializedBounds)
            {
                sceneBounds.Add(boundItem.sceneGUID, boundItem.bounds);
            }

            foreach (var boundItem in serializedHasVolumes)
            {
                hasProbeVolumes.Add(boundItem.sceneGUID, boundItem.hasProbeVolumes);
            }

            foreach (var set in bakingSets)
                AddBakingSet(set);

            if (string.IsNullOrEmpty(m_LightingScenario))
                m_LightingScenario = ProbeReferenceVolume.defaultLightingScenario;

            if (m_OtherScenario == "")
                m_OtherScenario = null;

#if UNITY_EDITOR
            // Migration code
            EditorApplication.delayCall += MigrateBakingSets;
#endif
        }

        void MigrateBakingSets()
        {
#if UNITY_EDITOR
            if (m_ObsoleteSerializedBakingSets == null)
                return;

            foreach (var set in m_ObsoleteSerializedBakingSets)
            {
                if (set.profile == null)
                    continue;
                set.profile.Migrate(set);
                AddBakingSet(set.profile);
                EditorUtility.SetDirty(set.profile);
            }
            if (parentAsset != null && m_ObsoleteSerializedBakingSets.Count != 0)
            {
                m_ObsoleteSerializedBakingSets = null;
                EditorUtility.SetDirty(parentAsset);
            }
#endif
        }

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        public void OnBeforeSerialize()
        {
            // We haven't initialized the bounds, no need to do anything here.
            if (sceneBounds == null || hasProbeVolumes == null) return;

            serializedBounds = new();
            serializedHasVolumes = new();

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
        }

        internal void SyncBakingSets()
        {
            #if UNITY_EDITOR
            bakingSets.Clear();
            sceneToBakingSet.Clear();

            var setGUIDs = AssetDatabase.FindAssets("t:" + typeof(ProbeVolumeBakingSet).Name);

            // Sync all the scene settings in the set to avoid config mismatch.
            foreach (var setGUID in setGUIDs)
            {
                var set = AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(AssetDatabase.GUIDToAssetPath(setGUID));
                AddBakingSet(set);
            }

            if (parentAsset != null)
                EditorUtility.SetDirty(parentAsset);
            #endif
        }

        internal void AddBakingSet(ProbeVolumeBakingSet set)
        {
            if (set == null)
                return;
            if (!bakingSets.Contains(set))
                bakingSets.Add(set);
            foreach (var guid in set.sceneGUIDs)
                sceneToBakingSet[guid] = set;
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

        internal void UpdateSceneBounds(Scene scene, bool updateGlobalVolumes)
        {
            var volumes = Object.FindObjectsByType<ProbeVolume>(FindObjectsSortMode.InstanceID);
            float prevBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
            int prevMaxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision();

            var sceneGUID = GetSceneGUID(scene);
            var profile = GetBakingSetForScene(sceneGUID);
            if (profile != null)
                ProbeReferenceVolume.instance.SetMinBrickAndMaxSubdiv(profile.minBrickSize, profile.maxSubdivision);
            else
            {
                hasProbeVolumes.TryGetValue(sceneGUID, out bool previousHasPV);
                bool hasPV = volumes.Any(v => v.gameObject.scene == scene);

                if (previousHasPV && !hasPV)
                {
                    hasProbeVolumes.Remove(sceneGUID);
                    sceneBounds.Remove(sceneGUID);
                    EditorUtility.SetDirty(parentAsset);
                }

                if (previousHasPV || !hasPV)
                    return;
            }

            bool boundFound = false;
            Bounds newBound = new Bounds();
            foreach (var volume in volumes)
            {
                bool forceUpdate = updateGlobalVolumes && volume.mode == ProbeVolume.Mode.Global;
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

        internal void OnSceneSaving(Scene scene, string path = null)
        {
            // If we are called from the scene callback, we want to update all global volumes that are potentially affected
            bool updateGlobalVolumes = path != null;

            UpdateSceneBounds(scene, updateGlobalVolumes);
            EnsurePerSceneData(scene);
        }

        internal void OnSceneRemovedFromSet(string sceneGUID)
        {
            sceneToBakingSet.Remove(sceneGUID);

            if (!hasProbeVolumes.TryGetValue(sceneGUID, out bool hasPV) || !hasPV)
            {
                hasProbeVolumes.Remove(sceneGUID);
                sceneBounds.Remove(sceneGUID);
            }
        }

        internal ProbeVolumeBakingSet GetBakingSetForScene(string sceneGUID) => sceneToBakingSet.GetValueOrDefault(sceneGUID, null);
        internal ProbeVolumeBakingSet GetBakingSetForScene(Scene scene) => GetBakingSetForScene(GetSceneGUID(scene));

        internal bool SceneHasProbeVolumes(string sceneGUID) => hasProbeVolumes != null && hasProbeVolumes.TryGetValue(sceneGUID, out var hasPV) && hasPV;
        internal bool SceneHasProbeVolumes(Scene scene) => SceneHasProbeVolumes(GetSceneGUID(scene));
#endif
    }
}
