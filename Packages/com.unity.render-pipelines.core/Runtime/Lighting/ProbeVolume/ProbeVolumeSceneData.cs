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

        [SerializeField] internal SerializedDictionary<string, ProbeVolumeBakingSet> sceneToBakingSet = new SerializedDictionary<string, ProbeVolumeBakingSet>();
        [SerializeField] internal List<ProbeVolumeBakingSet> bakingSets;

        internal Object parentAsset = null;

        [SerializeField] internal SerializedDictionary<string, Bounds> sceneBounds;
        [SerializeField] internal SerializedDictionary<string, bool> hasProbeVolumes;

        /// <summary>
        /// Constructor for ProbeVolumeSceneData.
        /// </summary>
        /// <param name="parentAsset">The asset holding this ProbeVolumeSceneData, it will be dirtied every time scene bounds or settings are changed.</param>
        public ProbeVolumeSceneData(Object parentAsset)
        {
            SetParentObject(parentAsset);
        }

        /// <summary>Set a reference to the object holding this ProbeVolumeSceneData.</summary>
        /// <param name="parent">The object holding this ProbeVolumeSceneData, it will be dirtied every time scene bounds or settings are changed. </param>
        public void SetParentObject(Object parent)
        {
            parentAsset = parent;

            if (bakingSets == null) bakingSets = new();
            if (sceneBounds == null) sceneBounds = new();
            if (hasProbeVolumes == null) hasProbeVolumes = new();
            if (sceneToBakingSet == null) sceneToBakingSet = new();

            SyncBakingSets();
#if UNITY_EDITOR
            MigrateBakingSets();
#endif
        }

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
#if UNITY_EDITOR
            // Migration code
            EditorApplication.delayCall += MigrateBakingSets;
#endif
        }

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

#if UNITY_EDITOR
        void MigrateBakingSets()
        {
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
        }
#endif


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

        internal ProbeVolumeBakingSet GetBakingSetForScene(string sceneGUID) => sceneToBakingSet?.GetValueOrDefault(sceneGUID, null);

        bool TryGetPerSceneData(string sceneGUID, out ProbeVolumePerSceneData perSceneData)
        {
            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
            {
                if (GetSceneGUID(data.gameObject.scene) == sceneGUID)
                {
                    perSceneData = data;
                    return true;
                }
            }

            perSceneData = null;
            return false;
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

        internal void UpdateSceneBounds(Scene scene, bool onSceneSave)
        {
            var volumes = Object.FindObjectsByType<ProbeVolume>(FindObjectsSortMode.InstanceID);
            float prevBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
            int prevMaxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision();

            var sceneGUID = GetSceneGUID(scene);
            var profile = GetBakingSetForScene(sceneGUID);
            if (profile != null)
            {
                if (onSceneSave)
                    ProbeReferenceVolume.instance.SetMinBrickAndMaxSubdiv(profile.minBrickSize, profile.maxSubdivision);
                else
                    ProbeReferenceVolume.instance.SetMinBrickAndMaxSubdiv(ProbeVolumeBakingSet.GetMinBrickSize(profile.minDistanceBetweenProbes), ProbeVolumeBakingSet.GetMaxSubdivision(profile.simplificationLevels));
            }
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
                if (!TryGetPerSceneData(sceneGUID, out var data))
                {
                    GameObject go = new GameObject("ProbeVolumePerSceneData");
                    go.hideFlags |= HideFlags.HideInHierarchy;
                    var perSceneData = go.AddComponent<ProbeVolumePerSceneData>();
                    perSceneData.sceneGUID = GetSceneGUID(scene);
                    SceneManager.MoveGameObjectToScene(go, scene);
                }
                else
                {
                    data.sceneGUID = sceneGUID; // Upgrade for older scenes.
                }
            }
        }

        internal void OnSceneSaving(Scene scene, string path = null)
        {
            // If we are called from the scene callback, we want to update all global volumes that are potentially affected
            bool onSceneSave = path != null;

            UpdateSceneBounds(scene, onSceneSave);
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

        internal ProbeVolumeBakingSet GetBakingSetForScene(Scene scene) => GetBakingSetForScene(GetSceneGUID(scene));

        internal bool SceneHasProbeVolumes(string sceneGUID) => hasProbeVolumes != null && hasProbeVolumes.TryGetValue(sceneGUID, out var hasPV) && hasPV;
        internal bool SceneHasProbeVolumes(Scene scene) => SceneHasProbeVolumes(GetSceneGUID(scene));

#endif
    }
}
