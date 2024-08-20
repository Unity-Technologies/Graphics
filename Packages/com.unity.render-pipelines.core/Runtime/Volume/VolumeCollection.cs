using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    internal class VolumeCollection
    {
        // Max amount of layers available in Unity
        internal const int k_MaxLayerCount = 32;

        // Cached lists of all volumes (sorted by priority) by layer mask
        readonly Dictionary<int, List<Volume>> m_SortedVolumes = new();

        // Holds all the registered volumes
        readonly List<Volume> m_Volumes = new();

        // Keep track of sorting states for layer masks
        readonly Dictionary<int, bool> m_SortNeeded = new();

        public int count => m_Volumes.Count;

        public bool Register(Volume volume, int layer)
        {
            if (volume == null)
                throw new ArgumentNullException(nameof(volume), "The volume to register is null");

            if (m_Volumes.Contains(volume))
                return false;

            m_Volumes.Add(volume);

            // Look for existing cached layer masks and add it there if needed
            foreach (var kvp in m_SortedVolumes)
            {
                // We add the volume to sorted lists only if the layer match and if it doesn't contain the volume already.
                if ((kvp.Key & (1 << layer)) != 0 && !kvp.Value.Contains(volume))
                    kvp.Value.Add(volume);
            }

            SetLayerIndexDirty(layer);
            return true;
        }

        public bool Unregister(Volume volume, int layer)
        {
            if (volume == null)
                throw new ArgumentNullException(nameof(volume), "The volume to unregister is null");

            m_Volumes.Remove(volume);

            foreach (var kvp in m_SortedVolumes)
            {
                // Skip layer masks this volume doesn't belong to
                if ((kvp.Key & (1 << layer)) == 0)
                    continue;

                kvp.Value.Remove(volume);
            }

            SetLayerIndexDirty(layer);

            return true;
        }

        public bool ChangeLayer(Volume volume, int previousLayerIndex, int currentLayerIndex)
        {
            if (volume == null)
                throw new ArgumentNullException(nameof(volume), "The volume to change layer is null");

            Assert.IsTrue(previousLayerIndex >= 0 && previousLayerIndex <= k_MaxLayerCount, "Invalid layer bit");
            Unregister(volume, previousLayerIndex);

            return Register(volume, currentLayerIndex);
        }

        // Stable insertion sort. Faster than List<T>.Sort() for our needs.
        internal static void SortByPriority(List<Volume> volumes)
        {
            for (int i = 1; i < volumes.Count; i++)
            {
                var temp = volumes[i];
                int j = i - 1;

                // Sort order is ascending
                while (j >= 0 && volumes[j].priority > temp.priority)
                {
                    volumes[j + 1] = volumes[j];
                    j--;
                }

                volumes[j + 1] = temp;
            }
        }

        public List<Volume> GrabVolumes(LayerMask mask)
        {
            List<Volume> list;

            if (!m_SortedVolumes.TryGetValue(mask, out list))
            {
                // New layer mask detected, create a new list and cache all the volumes that belong
                // to this mask in it
                list = new List<Volume>();

                var numVolumes = m_Volumes.Count;
                for (int i = 0; i < numVolumes; i++)
                {
                    var volume = m_Volumes[i];
                    if ((mask & (1 << volume.gameObject.layer)) == 0)
                        continue;

                    list.Add(volume);
                    m_SortNeeded[mask] = true;
                }

                m_SortedVolumes.Add(mask, list);
            }

            // Check sorting state
            if (m_SortNeeded.TryGetValue(mask, out var sortNeeded) && sortNeeded)
            {
                m_SortNeeded[mask] = false;
                SortByPriority(list);
            }

            return list;
        }

        public void SetLayerIndexDirty(int layerIndex)
        {
            Assert.IsTrue(layerIndex >= 0 && layerIndex <= k_MaxLayerCount, "Invalid layer bit");

            foreach (var kvp in m_SortedVolumes)
            {
                var mask = kvp.Key;

                if ((mask & (1 << layerIndex)) != 0)
                    m_SortNeeded[mask] = true;
            }
        }

        public bool IsComponentActiveInMask<T>(LayerMask layerMask)
            where T : VolumeComponent
        {
            int mask = layerMask.value;

            foreach (var kvp in m_SortedVolumes)
            {
                if (kvp.Key != mask)
                    continue;

                foreach (var volume in kvp.Value)
                {
                    if (!volume.enabled || volume.profileRef == null)
                        continue;

                    if (volume.profileRef.TryGet(out T component) && component.active)
                        return true;
                }
            }

            return false;
        }
    }
}
