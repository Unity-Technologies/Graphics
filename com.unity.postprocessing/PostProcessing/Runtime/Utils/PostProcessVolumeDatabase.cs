using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Rendering.PostProcessing
{
    internal readonly struct PostProcessVolumeMeta : IEquatable<PostProcessVolumeMeta>
    {
        public readonly float priority;
        public readonly int layer;

        public PostProcessVolumeMeta(float priority, int layer)
        {
            this.priority = priority;
            this.layer = layer;
        }

        public bool Equals(PostProcessVolumeMeta other)
        {
            return priority.Equals(other.priority) && layer == other.layer;
        }

        public override bool Equals(object obj)
        {
            return obj is PostProcessVolumeMeta other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (priority.GetHashCode() * 397) ^ layer;
            }
        }

        public static bool operator ==(in PostProcessVolumeMeta l, in PostProcessVolumeMeta r)
        {
            return l.Equals(r);
        }

        public static bool operator !=(in PostProcessVolumeMeta l, in PostProcessVolumeMeta r)
        {
            return !l.Equals(r);
        }
    }

    internal class PostProcessVolumeDatabase
    {
        /// <summary>
        ///     Index by LayerMask, the value is a sorted list by priority (in increasing order).
        /// </summary>
        private readonly Dictionary<int, List<Entry>> m_IndexByMask = new();

        /// <summary>
        ///     LayerMasks where the corresponding layer masks requires a priority resorting
        /// </summary>
        private readonly HashSet<int> m_LayerMasksToResort = new();

        private readonly List<Entry> m_Volumes = new();

        [Flags]
        enum FindOptions
        {
            None = 0,
            BuildOnCacheMiss = 1 << 0,
            SortIfRequired = 1 << 1
        }

        /// <summary>
        ///     Look in cache for the highest priority volume for a specific layer mask
        ///     The cache won't be lazy built on a cache miss
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="volume"></param>
        /// <returns></returns>
        public Exception GetHighestPriorityVolumeInMaskInCache(LayerMask mask, out PostProcessVolume volume)
        {
            Exception e;
            volume = null;
            if ((e = FindSortedVolumes(mask, FindOptions.SortIfRequired, out var sortedVolumes)) != null)
                return e;
            if (sortedVolumes == null || sortedVolumes.Count == 0) return null;

            // list is assumed to be sorted at this point
            // highest priority is the last element
            volume = sortedVolumes.Last().volume;
            return null;
        }

        public Exception Add(PostProcessVolume volume, PostProcessVolumeMeta meta)
        {
            var entry = new Entry { volume = volume, meta = meta };
            var mask = 1 << meta.layer;
            m_Volumes.Add(entry);

            AddEntryInIndex(entry, mask);

            return null;
        }

        private void AddEntryInIndex(Entry entry, int mask)
        {
            foreach (var kvp in m_IndexByMask)
            {
                if ((kvp.Key & mask) == 0) continue;

                kvp.Value.Add(entry);
                m_LayerMasksToResort.Add(kvp.Key);
            }
        }

        public void Remove(PostProcessVolume volume)
        {
            // Find the entry
            var entry = m_Volumes.Find(v => v.volume == volume);
            if (entry == null) return;

            // Remove in the volume list
            m_Volumes.Remove(entry);

            RemoveEntryFromIndex(entry);
        }

        private void RemoveEntryFromIndex(Entry entry)
        {
            var mask = 1 << entry.meta.layer;
            foreach (var kvp in m_IndexByMask)
            {
                // Skip layer masks this volume doesn't belong to
                if ((kvp.Key & mask) == 0)
                    continue;

                kvp.Value.Remove(entry);
            }

            // We don't need to sort again those lists
            // after a deletion the order is correct.
        }

        /// <summary>
        ///     Find the sorted volumes for a specific layer mask.
        ///     The sort is by increasing order.
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="sortedVolumes"></param>
        /// <returns></returns>
        public Exception FindSortedVolumesByLayerMask(LayerMask mask, List<PostProcessVolume> sortedVolumes)
        {
            Exception e;
            if ((e = FindSortedVolumes(mask, FindOptions.SortIfRequired | FindOptions.BuildOnCacheMiss,
                out var sortedEntries)) != null)
                return e;

            sortedVolumes.AddRange(sortedEntries.Select(v => v.volume));
            return null;
        }

        Exception FindSortedVolumes(LayerMask mask, FindOptions options, out List<Entry> sortedVolumes)
        {
            sortedVolumes = null;

            if (!m_IndexByMask.TryGetValue(mask, out sortedVolumes) && (options & FindOptions.BuildOnCacheMiss) != 0)
            {
                // New layer mask detected, create a new list and cache all the volumes that belong
                // to this mask in it
                sortedVolumes = new List<Entry>();

                // Build the index for this mask
                foreach (var volume in m_Volumes)
                {
                    if ((mask & (1 << volume.meta.layer)) == 0)
                        continue;

                    sortedVolumes.Add(volume);
                    m_LayerMasksToResort.Add(mask);
                }

                m_IndexByMask.Add(mask, sortedVolumes);
            }

            // Update the sorting if required
            if (m_LayerMasksToResort.Contains(mask) && (options & FindOptions.SortIfRequired) != 0)
            {
                m_LayerMasksToResort.Remove(mask);
                InsertionSort(sortedVolumes, new VolumePriorityComparer());
            }

            return null;
        }

        public void UpdateVolumeLayerMeta(PostProcessVolume volume, PostProcessVolumeMeta newMeta)
        {
            var entry = m_Volumes.Find(e => e.volume == volume);
            if (entry != null)
            {
                // this is a modification
                if (entry.meta.layer != newMeta.layer)
                {
                    RemoveEntryFromIndex(entry);
                    AddEntryInIndex(entry, newMeta.layer);
                }

                // We use else if as a Adding and Removing will update the priority
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                else if (entry.meta.priority != newMeta.priority)
                {
                    // find all layer to update
                    var mask = (1 << newMeta.layer);
                    foreach (var entries in m_IndexByMask)
                    {
                        if ((entries.Key & mask) != 0)
                            m_LayerMasksToResort.Add(entries.Key);
                    }
                }
            }
            else
            {
                // this is an add
                Add(volume, newMeta);
            }
        }

        // Custom insertion sort. First sort will be slower but after that it'll be faster than
        // using List<T>.Sort() which is also unstable by nature.
        // Sort order is ascending.
        private static Exception InsertionSort<T, TComparer>(List<T> values, TComparer comparer)
            where TComparer : IComparer<T>
        {
            if (values == null)
                return new ArgumentNullException(nameof(values));

            for (var i = 1; i < values.Count; i++)
            {
                var temp = values[i];
                var j = i - 1;

                while (j >= 0 && comparer.Compare(values[j], temp) > 0)
                {
                    values[j + 1] = values[j];
                    j--;
                }

                values[j + 1] = temp;
            }

            return null;
        }

        private class Entry : IEquatable<Entry>
        {
            public PostProcessVolumeMeta meta;
            public PostProcessVolume volume;

            public bool Equals(Entry other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return meta.Equals(other.meta) && Equals(volume, other.volume);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((Entry)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (meta.GetHashCode() * 397) ^ (volume != null ? volume.GetHashCode() : 0);
                }
            }
        }

        private struct VolumePriorityComparer : IComparer<Entry>
        {
            public int Compare(Entry x, Entry y)
            {
                return Comparer<float>.Default.Compare(x.volume.priority, y.volume.priority);
            }
        }
    }
}
