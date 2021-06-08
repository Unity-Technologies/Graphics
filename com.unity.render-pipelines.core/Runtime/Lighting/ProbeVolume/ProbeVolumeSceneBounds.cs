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
        private int FindInflatingBrickSize(Bounds b)
        {
            var refVol = ProbeReferenceVolume.instance;
            float minSizedDim = Mathf.Min(b.size.x, Mathf.Min(b.size.y, b.size.z));

            float minBrickSize = refVol.MinBrickSize();

            float minSideInBricks = Mathf.CeilToInt(minSizedDim / minBrickSize);
            int subdivLevel = Mathf.FloorToInt(Mathf.Log(minSideInBricks, 3));

            return subdivLevel;
        }

        private void InflateBound(ref Bounds bounds)
        {
            int brickSubDivLevel = FindInflatingBrickSize(bounds);

            float roundingBrickSize = ProbeReferenceVolume.instance.BrickSize(brickSubDivLevel);

            // Make sure we lock the bounds at a grid position.
            bounds.Encapsulate(new Vector3(roundingBrickSize * Mathf.Floor(bounds.min.x / roundingBrickSize),
                roundingBrickSize * Mathf.Floor(bounds.min.y / roundingBrickSize),
                roundingBrickSize * Mathf.Floor(bounds.min.z / roundingBrickSize)));

            Vector3 newSize = Vector3Int.CeilToInt(bounds.size / roundingBrickSize);

            Vector3Int minPos = Vector3Int.FloorToInt(bounds.min);
            Vector3 maxPos = minPos + newSize * roundingBrickSize;
            bounds.Encapsulate(maxPos);
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
