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
    public class ProbeVolumeSceneBounds : ISerializationCallbackReceiver
    {
        [System.Serializable]
        private struct SerializableBoundItem
        {
            [SerializeField] public string scenePath;
            [SerializeField] public Bounds bounds;
        }

        [System.Serializable]
        private struct SerializableHasPVItem
        {
            [SerializeField] public string scenePath;
            [SerializeField] public bool hasProbeVolumes;
        }

        [SerializeField] private List<SerializableBoundItem> serializedBounds;
        [SerializeField] private List<SerializableHasPVItem> serializedHasVolumes;

        private Object m_ParentAsset = null;
        public Dictionary<string, Bounds> sceneBounds;
        internal Dictionary<string, bool> hasProbeVolumes;

        public ProbeVolumeSceneBounds(Object parentAsset)
        {
            m_ParentAsset = parentAsset;
            sceneBounds = new Dictionary<string, Bounds>();
            hasProbeVolumes = new Dictionary<string, bool>();
            serializedBounds = new List<SerializableBoundItem>();
            serializedHasVolumes = new List<SerializableHasPVItem>();
        }

        public void SetParentObject(Object parent)
        {
            m_ParentAsset = parent;
        }

        public void OnAfterDeserialize()
        {
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

        public void OnBeforeSerialize()
        {
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
        internal void UpdateSceneBounds(Scene scene)
        {
            var volumes = UnityEngine.GameObject.FindObjectsOfType<ProbeVolume>();
            bool boundFound = false;
            Bounds newBound = new Bounds();
            foreach (var volume in volumes)
            {
                var scenePath = volume.gameObject.scene.path;
                if (scenePath == scene.path)
                {
                    var pos = volume.gameObject.transform.position;
                    var extent = volume.GetExtents();
                    Bounds localBounds = new Bounds(pos, extent);

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
