using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEngine.Experimental.Rendering
{
    [System.Serializable]
    /// <summary> A class containing info about the bounds defined by the probe volumes in various scenes. </summary>
    public class ProbeVolumeSceneBounds : ISerializationCallbackReceiver
    {
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

        [SerializeField] List<SerializableBoundItem> serializedBounds;
        [SerializeField] List<SerializableHasPVItem> serializedHasVolumes;

        Object m_ParentAsset = null;
        /// <summary> A dictionary containing the Bounds defined by probe volumes for each scene (scene path is the key of the dictionary). </summary>
        public Dictionary<string, Bounds> sceneBounds;
        internal Dictionary<string, bool> hasProbeVolumes;

        /// <summary>Constructor for ProbeVolumeSceneBounds. </summary>
        /// <param name="parentAsset">The asset holding this ProbeVolumeSceneBounds, it will be dirtied every time scene bounds are updated. </param>
        public ProbeVolumeSceneBounds(Object parentAsset)
        {
            m_ParentAsset = parentAsset;
            sceneBounds = new Dictionary<string, Bounds>();
            hasProbeVolumes = new Dictionary<string, bool>();
            serializedBounds = new List<SerializableBoundItem>();
            serializedHasVolumes = new List<SerializableHasPVItem>();
        }

        /// <summary>Set a reference to the object holding this ProbeVolumeSceneBounds.</summary>
        /// <param name="parentAsset">The object holding this ProbeVolumeSceneBounds, it will be dirtied every time scene bounds are updated. </param>
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
            if (serializedBounds == null || serializedHasVolumes == null) return;

            sceneBounds = new Dictionary<string, Bounds>();
            hasProbeVolumes = new Dictionary<string, bool>();
            foreach (var boundItem in serializedBounds)
            {
                sceneBounds.Add(boundItem.scenePath, boundItem.bounds);
            }

            foreach (var boundItem in serializedHasVolumes)
            {
                hasProbeVolumes.Add(boundItem.scenePath, boundItem.hasProbeVolumes);
            }
        }

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        public void OnBeforeSerialize()
        {
            // We haven't initialized the bounds, no need to do anything here.
            if (sceneBounds == null || hasProbeVolumes == null ||
                serializedBounds == null || serializedHasVolumes == null) return;

            serializedBounds.Clear();
            serializedHasVolumes.Clear();
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

#endif
    }
}
