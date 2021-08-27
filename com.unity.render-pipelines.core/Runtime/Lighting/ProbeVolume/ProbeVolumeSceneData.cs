using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

using System.Reflection;

// TODO! TO CONSIDER: All this referencing scenes via their path, is that reliable? Shall we use some other thing? What happens upon renaming? Do we have a persistent value?

namespace UnityEngine.Experimental.Rendering
{
    [System.Serializable]
    /// <summary> A class containing scene info about related to probe volumes. </summary>
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
        struct SerializableAssetItem
        {
            [SerializeField] public string sceneGUID;
            [SerializeField] public APVSceneAssets assets;
        }

        [SerializeField] List<SerializableBoundItem> serializedBounds;
        [SerializeField] List<SerializableHasPVItem> serializedHasVolumes;
        [SerializeField] List<SerializableAssetItem> serializedAssets;

        Object m_ParentAsset = null;
        /// <summary> A dictionary containing the Bounds defined by probe volumes for each scene (scene path is the key of the dictionary). </summary>
        public Dictionary<string, Bounds> sceneBounds;
        internal Dictionary<string, bool> hasProbeVolumes;

        // !! IMPORTANT !! At the moment we are assuming only one asset per scene. This is an assumption propagated throughout the system a fair bit
        // and this imposes a strong constraint about it. This will not be the case as we'd want to do blending between different assets or simply having different states.
        // When this will be the case we need to change this so the dictionary should include the state metadata and a way to know which one is the one considered "active" (i.e. to be actually loaded).
        // This is why the abstraction away in a class that at the moment just returns a single asset.
        [System.Serializable]
        internal class APVSceneAssets
        {
            public ProbeVolumeAsset asset;

            // This will need to take some metadata input to check if we already have an asset with a given metadata set in the collection to decide whether to replace or add.
            // Currently always replacing.
            public void AddAsset(ProbeVolumeAsset asset)
            {
                this.asset = asset;
            }

            // This is very temp. It will need to take as input the active meta data settings and grab from the collection the right asset.
            public ProbeVolumeAsset GetActiveAsset()
            {
                return asset;
            }
        }
        internal Dictionary<string, APVSceneAssets> sceneAssets;

        // !! IMPORTANT !! This is a binary loaded or not value, however when we have states, this will need to also store what state is actually loaded and upon state change we need to verify if
        // we have the right state loaded for a given scene. Essentially instead of bool we'll need to store the current state, whatever representation it'll take.
        internal Dictionary<string, bool> sceneAssetLoadingStatus;

        /// <summary>Constructor for ProbeVolumeSceneData. </summary>
        /// <param name="parentAsset">The asset holding this ProbeVolumeSceneData, it will be dirtied every time scene bounds are updated or an asset is changed. </param>
        public ProbeVolumeSceneData(Object parentAsset)
        {
            m_ParentAsset = parentAsset;
            sceneBounds = new Dictionary<string, Bounds>();
            hasProbeVolumes = new Dictionary<string, bool>();
            sceneAssets = new Dictionary<string, APVSceneAssets>();
            sceneAssetLoadingStatus = new Dictionary<string, bool>();

            serializedBounds = new List<SerializableBoundItem>();
            serializedHasVolumes = new List<SerializableHasPVItem>();
            serializedAssets = new List<SerializableAssetItem>();
        }

        /// <summary>Set a reference to the object holding this ProbeVolumeSceneData.</summary>
        /// <param name="parentAsset">The object holding this ProbeVolumeSceneData, it will be dirtied every time scene bounds are updated. </param>
        public void SetParentObject(Object parent)
        {
            m_ParentAsset = parent;
        }

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // We haven't initialized the data, no need to do anything here.
            if (serializedBounds == null || serializedHasVolumes == null || serializedAssets == null) return;

            sceneBounds = new Dictionary<string, Bounds>();
            hasProbeVolumes = new Dictionary<string, bool>();
            sceneAssets = new Dictionary<string, APVSceneAssets>();

            foreach (var boundItem in serializedBounds)
            {
                sceneBounds.Add(boundItem.scenePath, boundItem.bounds);
            }

            foreach (var boundItem in serializedHasVolumes)
            {
                hasProbeVolumes.Add(boundItem.scenePath, boundItem.hasProbeVolumes);
            }

            foreach (var assetItem in serializedAssets)
            {
                sceneAssets.Add(assetItem.sceneGUID, assetItem.assets);
            }
        }

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        public void OnBeforeSerialize()
        {
            // We haven't initialized the bounds, no need to do anything here.
            if (sceneBounds == null || hasProbeVolumes == null || sceneAssets == null ||
                serializedBounds == null || serializedHasVolumes == null || serializedAssets == null) return;

            serializedBounds.Clear();
            serializedHasVolumes.Clear();
            serializedAssets.Clear();
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


            foreach (var k in sceneAssets.Keys)
            {
                SerializableAssetItem item;
                item.sceneGUID = k;
                item.assets = sceneAssets[k];
                serializedAssets.Add(item);
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

        // IMPORTANT TODO: This will require a metadata item together with the asset to make sure the asset is inserted correctly in the list of assets belonging to a scene.
        internal void AddAsset(Scene scene, ProbeVolumeAsset asset)
        {
            var sceneGUID = GetSceneGUID(scene);
            if (sceneAssets == null)
            {
                sceneAssets = new Dictionary<string, APVSceneAssets>();
            }

            if (!sceneAssets.ContainsKey(sceneGUID))
            {
                sceneAssets.Add(sceneGUID, new APVSceneAssets());
            }

            sceneAssets[sceneGUID].AddAsset(asset);

            if (m_ParentAsset != null)
            {
                EditorUtility.SetDirty(m_ParentAsset);
            }
        }

        internal void ClearAsset(Scene scene)
        {
            var sceneGUID = GetSceneGUID(scene);
            if (sceneAssets != null && sceneAssets.ContainsKey(sceneGUID))
            {
                sceneAssets.Remove(sceneGUID);
            }
            if (sceneAssetLoadingStatus != null && sceneAssetLoadingStatus.ContainsKey(sceneGUID))
            {
                sceneAssetLoadingStatus.Remove(sceneGUID);
            }
        }

        internal ProbeVolumeAsset GetActiveAsset(Scene scene)
        {
            var sceneGUID = GetSceneGUID(scene);
            if (sceneAssets != null && sceneAssets.ContainsKey(sceneGUID))
            {
                return sceneAssets[sceneGUID].GetActiveAsset();
            }
            return null;
        }

#endif
        // The logic to check if scene needs their asset loading can be optimized.

        // TODO: This status will be a state whenever we introduce them instead of a binary yes/no.
        internal void SetSceneAssetLoaded(string sceneGUID, bool status)
        {
            if (sceneAssetLoadingStatus == null)
                sceneAssetLoadingStatus = new Dictionary<string, bool>();

            sceneAssetLoadingStatus[sceneGUID] = status;
        }

        // IMPORTANT TODO: This is now returning the active asset, when we have state it will need to return the asset for the scene and a given state.
        internal List<ProbeVolumeAsset> GetAssetsForLoadedScenes(out List<string> sceneGUIDs)
        {
            sceneGUIDs = new List<string>();
            List<ProbeVolumeAsset> assets = new List<ProbeVolumeAsset>();
            int sceneCount = SceneManagement.SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    var sceneGUID = GetSceneGUID(scene);
                    if (sceneAssets.ContainsKey(sceneGUID))
                    {
                        var asset = sceneAssets[sceneGUID].GetActiveAsset();
                        sceneGUIDs.Add(sceneGUID);
                        assets.Add(asset);
                    }
                }
            }

            return assets;
        }

        // TODO: Add a state here when we have it.
        internal void FlushPendingAssets()
        {
            if (sceneAssets == null) return;

            if (sceneAssetLoadingStatus == null)
                sceneAssetLoadingStatus = new Dictionary<string, bool>();

            List<ProbeVolumeAsset> assetsToAdd = GetAssetsForLoadedScenes(out List<string> sceneGUIDs);

            for (int i = 0; i < assetsToAdd.Count; ++i)
            {
                var sceneGUID = sceneGUIDs[i];
                var asset = assetsToAdd[i];
                if (!sceneAssetLoadingStatus.ContainsKey(sceneGUID) || !sceneAssetLoadingStatus[sceneGUID])
                {
                    ProbeReferenceVolume.instance.AddPendingAssetLoading(asset);
                    SetSceneAssetLoaded(sceneGUID, true); // Assume we are loading the assets before calling the flushing again.
                }
            }
        }

        // TODO: This needs to be optimized, it is dumb to loop through all. But this is not going to happen often, so can push the optimization to later.
        // But needs to be done.
        internal void SetAssetAsUnloaded(ProbeVolumeAsset asset)
        {
            if (sceneAssets == null) return;
            foreach (var assetDictionaryEntry in sceneAssets)
            {
                if (asset.GetSerializedFullPath() == assetDictionaryEntry.Value.GetActiveAsset().GetSerializedFullPath())
                {
                    SetSceneAssetLoaded(assetDictionaryEntry.Key, false);
                }
            }
        }

        internal void OnSceneUnloaded(Scene scene)
        {
            var sceneGUID = GetSceneGUID(scene);
            if (sceneAssets.ContainsKey(sceneGUID))
            {
                var asset = sceneAssets[sceneGUID].GetActiveAsset();
                ProbeReferenceVolume.instance.AddPendingAssetRemoval(asset);
                SetSceneAssetLoaded(sceneGUID, false);
            }
        }
    }
}
