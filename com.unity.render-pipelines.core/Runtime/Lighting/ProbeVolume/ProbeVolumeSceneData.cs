using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;

namespace UnityEngine.Experimental.Rendering
{
    // Add Profile and baking settings.

    [System.Serializable]
    /// <summary> A class containing info about the bounds defined by the probe volumes in various scenes. </summary>
    public class ProbeVolumeSceneData : ISerializationCallbackReceiver
    {
        static PropertyInfo s_SceneGUID = typeof(Scene).GetProperty("guid", System.Reflection.BindingFlags.NonPublic | BindingFlags.Instance);
        string GetSceneGUID(Scene scene)
        {
            Debug.Assert(s_SceneGUID != null, "Reflection for scene GUID failed");
            return (string)s_SceneGUID.GetValue(scene);
        }

        [System.Serializable]
        struct SerializableBoundItem
        {
            [SerializeField] public string scenePath;
            [SerializeField] public Bounds bounds;
        }

        [System.Serializable]
        struct SerializableHasPVItem
        {
            [SerializeField] public string scenePath;
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
            [SerializeField] public string sceneGUID;
            [SerializeField] public ProbeVolumeBakingProcessSettings settings;
        }

        [SerializeField] List<SerializableBoundItem> serializedBounds;
        [SerializeField] List<SerializableHasPVItem> serializedHasVolumes;
        [SerializeField] List<SerializablePVProfile> serializedProfiles;
        [SerializeField] List<SerializablePVBakeSettings> serializedBakeSettings;

        Object m_ParentAsset = null;
        /// <summary> A dictionary containing the Bounds defined by probe volumes for each scene (scene path is the key of the dictionary). </summary>
        public Dictionary<string, Bounds> sceneBounds;
        internal Dictionary<string, bool> hasProbeVolumes;
        internal Dictionary<string, ProbeReferenceVolumeProfile> sceneProfiles;
        internal Dictionary<string, ProbeVolumeBakingProcessSettings> sceneBakingSettings;

        /// <summary>Constructor for ProbeVolumeSceneData. </summary>
        /// <param name="parentAsset">The asset holding this ProbeVolumeSceneData, it will be dirtied every time scene bounds or settings are changed. </param>
        public ProbeVolumeSceneData(Object parentAsset)
        {
            m_ParentAsset = parentAsset;
            sceneBounds = new Dictionary<string, Bounds>();
            hasProbeVolumes = new Dictionary<string, bool>();
            sceneProfiles = new Dictionary<string, ProbeReferenceVolumeProfile>();
            sceneBakingSettings = new Dictionary<string, ProbeVolumeBakingProcessSettings>();

            serializedBounds = new List<SerializableBoundItem>();
            serializedHasVolumes = new List<SerializableHasPVItem>();
            serializedProfiles = new List<SerializablePVProfile>();
            serializedBakeSettings = new List<SerializablePVBakeSettings>();
        }

        /// <summary>Set a reference to the object holding this ProbeVolumeSceneData.</summary>
        /// <param name="parentAsset">The object holding this ProbeVolumeSceneData, it will be dirtied every time scene bounds or settings are changed. </param>
        public void SetParentObject(Object parent)
        {
            m_ParentAsset = parent;
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

            foreach (var boundItem in serializedBounds)
            {
                sceneBounds.Add(boundItem.scenePath, boundItem.bounds);
            }

            foreach (var boundItem in serializedHasVolumes)
            {
                hasProbeVolumes.Add(boundItem.scenePath, boundItem.hasProbeVolumes);
            }

            foreach (var profileItem in serializedProfiles)
            {
                sceneProfiles.Add(profileItem.sceneGUID, profileItem.profile);
            }

            foreach (var settingsItem in serializedBakeSettings)
            {
                sceneBakingSettings.Add(settingsItem.sceneGUID, settingsItem.settings);
            }
        }

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        public void OnBeforeSerialize()
        {
            // We haven't initialized the bounds, no need to do anything here.
            if (sceneBounds == null || hasProbeVolumes == null || sceneBakingSettings == null || sceneProfiles == null ||
                serializedBounds == null || serializedHasVolumes == null || serializedBakeSettings == null || serializedProfiles == null) return;

            serializedBounds.Clear();
            serializedHasVolumes.Clear();
            serializedProfiles.Clear();
            serializedBakeSettings.Clear();

            foreach (var k in sceneBounds.Keys)
            {
                SerializableBoundItem item;
                item.scenePath = k;
                item.bounds = sceneBounds[k];
                serializedBounds.Add(item);
            }

            foreach (var k in hasProbeVolumes.Keys)
            {
                SerializableHasPVItem item;
                item.scenePath = k;
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
        }

#if UNITY_EDITOR
        private int FindInflatingBrickSize(Vector3 size)
        {
            var refVol = ProbeReferenceVolume.instance;
            float minSizedDim = Mathf.Min(size.x, Mathf.Min(size.y, size.z));

            float minBrickSize = refVol.MinBrickSize();

            float minSideInBricks = Mathf.CeilToInt(minSizedDim / minBrickSize);
            int subdivLevel = Mathf.FloorToInt(Mathf.Log(minSideInBricks, 3));

            return subdivLevel;
        }

        private void InflateBound(ref Bounds bounds)
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
            float rightPaddingSubdivLevel = ProbeReferenceVolume.instance.BrickSize(FindInflatingBrickSize(new Vector3(maxPadding.x, originalBounds.size.y, originalBounds.size.z)));
            float leftPaddingSubdivLevel = ProbeReferenceVolume.instance.BrickSize(FindInflatingBrickSize(new Vector3(minPadding.x, originalBounds.size.y, originalBounds.size.z)));
            float topPaddingSubdivLevel = ProbeReferenceVolume.instance.BrickSize(FindInflatingBrickSize(new Vector3(originalBounds.size.x, maxPadding.y, originalBounds.size.z)));
            float bottomPaddingSubdivLevel = ProbeReferenceVolume.instance.BrickSize(FindInflatingBrickSize(new Vector3(originalBounds.size.x, minPadding.y, originalBounds.size.z)));
            float forwardPaddingSubdivLevel = ProbeReferenceVolume.instance.BrickSize(FindInflatingBrickSize(new Vector3(originalBounds.size.x, originalBounds.size.y, maxPadding.z)));
            float backPaddingSubdivLevel = ProbeReferenceVolume.instance.BrickSize(FindInflatingBrickSize(new Vector3(originalBounds.size.x, originalBounds.size.y, minPadding.z)));

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

        internal void UpdateSceneBounds(Scene scene)
        {
            // If we have not yet loaded any asset, we haven't initialized the probe reference volume with any info from the profile.
            // As a result we need to prime with the profile info directly stored here.
            {
                var profile = GetProfileForScene(scene);
                Debug.Assert(profile != null);
                ProbeReferenceVolume.instance.SetTRS(Vector3.zero, Quaternion.identity, profile.minBrickSize);
                ProbeReferenceVolume.instance.SetMaxSubdivision(profile.maxSubdivision);
            }

            var volumes = UnityEngine.GameObject.FindObjectsOfType<ProbeVolume>();
            bool boundFound = false;
            Bounds newBound = new Bounds();
            foreach (var volume in volumes)
            {
                if (volume.globalVolume)
                    volume.UpdateGlobalVolume(scene);

                var scenePath = volume.gameObject.scene.path;
                if (scenePath == scene.path)
                {
                    var pos = volume.gameObject.transform.position;
                    var extent = volume.GetExtents();

                    Bounds localBounds = new Bounds(pos, extent);

                    InflateBound(ref localBounds);

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
            }

            if (boundFound)
            {
                if (sceneBounds == null)
                {
                    sceneBounds = new Dictionary<string, Bounds>();
                    hasProbeVolumes = new Dictionary<string, bool>();
                }

                if (sceneBounds.ContainsKey(scene.path))
                {
                    sceneBounds[scene.path] = newBound;
                }
                else
                {
                    sceneBounds.Add(scene.path, newBound);
                }
            }

            if (hasProbeVolumes.ContainsKey(scene.path))
                hasProbeVolumes[scene.path] = boundFound;
            else
                hasProbeVolumes.Add(scene.path, boundFound);

            if (m_ParentAsset != null)
            {
                EditorUtility.SetDirty(m_ParentAsset);
            }
        }

        // It is important this is called after UpdateSceneBounds is called!
        internal void EnsurePerSceneData(Scene scene)
        {
            if (hasProbeVolumes.ContainsKey(scene.path) && hasProbeVolumes[scene.path])
            {
                var perSceneData = UnityEngine.GameObject.FindObjectsOfType<ProbeVolumePerSceneData>();
                var sceneGUID = GetSceneGUID(scene);

                bool foundPerSceneData = false;
                foreach (var data in perSceneData)
                {
                    if (GetSceneGUID(data.gameObject.scene) == sceneGUID)
                    {
                        foundPerSceneData = true;
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

        internal void OnSceneSaved(Scene scene)
        {
            UpdateSceneBounds(scene);
            EnsurePerSceneData(scene);
        }


        internal void SetProfileForScene(Scene scene, ProbeReferenceVolumeProfile profile)
        {
            if (sceneProfiles == null) sceneProfiles = new Dictionary<string, ProbeReferenceVolumeProfile>();

            var sceneGUID = GetSceneGUID(scene);
            sceneProfiles[sceneGUID] = profile;
        }

        internal void SetBakeSettingsForScene(Scene scene, ProbeDilationSettings dilationSettings, VirtualOffsetSettings virtualOffsetSettings)
        {
            if (sceneBakingSettings == null) sceneBakingSettings = new Dictionary<string, ProbeVolumeBakingProcessSettings>();

            var sceneGUID = GetSceneGUID(scene);

            ProbeVolumeBakingProcessSettings settings = new ProbeVolumeBakingProcessSettings();
            settings.dilationSettings = dilationSettings;
            settings.virtualOffsetSettings = virtualOffsetSettings;
            sceneBakingSettings[sceneGUID] = settings;
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

            return new ProbeVolumeBakingProcessSettings();
        }
#endif
    }
}
