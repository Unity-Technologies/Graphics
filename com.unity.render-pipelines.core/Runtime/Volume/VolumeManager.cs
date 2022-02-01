using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    using UnityObject = UnityEngine.Object;

    /// <summary>
    /// A global manager that tracks all the Volumes in the currently loaded Scenes and does all the
    /// interpolation work.
    /// </summary>
    public sealed partial class VolumeManager
    {
        static readonly Lazy<VolumeManager> s_Instance = new Lazy<VolumeManager>(() => new VolumeManager());

        /// <summary>
        /// The current singleton instance of <see cref="VolumeManager"/>.
        /// </summary>
        public static VolumeManager instance => s_Instance.Value;

        internal static List<(string, Type)> GetSupportedVolumeComponents(Type currentPipelineType)
        {
            if (s_SupportedVolumeComponentsForRenderPipeline.TryGetValue(currentPipelineType,
                out var supportedVolumeComponents))
                return supportedVolumeComponents;

            supportedVolumeComponents = FilterVolumeComponentTypes(
                VolumeManager.instance.baseComponentTypeArray, currentPipelineType);
            s_SupportedVolumeComponentsForRenderPipeline[currentPipelineType] = supportedVolumeComponents;

            return supportedVolumeComponents;
        }

        static List<(string, Type)> FilterVolumeComponentTypes(Type[] types, Type currentPipelineType)
        {
            var volumes = new List<(string, Type)>();
            foreach (var t in types)
            {
                string path = string.Empty;

                var attrs = t.GetCustomAttributes(false);

                bool skipComponent = false;

                // Look for the attributes of this volume component and decide how is added and if it needs to be skipped
                foreach (var attr in attrs)
                {
                    switch (attr)
                    {
                        case VolumeComponentMenu attrMenu:
                        {
                            path = attrMenu.menu;
                            if (attrMenu is VolumeComponentMenuForRenderPipeline supportedOn)
                                skipComponent |= Array.IndexOf(supportedOn.pipelineTypes, currentPipelineType) == -1;
                            break;
                        }
                        case HideInInspector attrHide:
                        case ObsoleteAttribute attrDeprecated:
                            skipComponent = true;
                            break;
                    }
                }

                if (skipComponent)
                    continue;

                // If no attribute or in case something went wrong when grabbing it, fallback to a
                // beautified class name
                if (string.IsNullOrEmpty(path))
                {
#if UNITY_EDITOR
                    path = ObjectNames.NicifyVariableName(t.Name);
#else
                    path = t.Name;
#endif
                }


                volumes.Add((path, t));
            }

            return volumes
                .OrderBy(i => i.Item1)
                .ToList();
        }

        static readonly Dictionary<Type, List<(string, Type)>> s_SupportedVolumeComponentsForRenderPipeline = new();

        // Max amount of layers available in Unity
        const int k_MaxLayerCount = 32;

        // Cached lists of all volumes (sorted by priority) by layer mask
        readonly Dictionary<int, List<Volume>> m_SortedVolumes;

        // Holds all the registered volumes
        readonly List<Volume> m_Volumes;

        // Keep track of sorting states for layer masks
        readonly Dictionary<int, bool> m_SortNeeded;

        // Recycled list used for volume traversal
        readonly List<Collider> m_TempColliders;

        VolumeManager()
        {
            m_SortedVolumes = new Dictionary<int, List<Volume>>();
            m_Volumes = new List<Volume>();
            m_SortNeeded = new Dictionary<int, bool>();
            m_TempColliders = new List<Collider>(8);
        }

        /// <summary>
        /// Registers a new Volume in the manager. Unity does this automatically when a new Volume is
        /// enabled, or its layer changes, but you can use this function to force-register a Volume
        /// that is currently disabled.
        /// </summary>
        /// <param name="volume">The volume to register.</param>
        /// <param name="layer">The LayerMask that this volume is in.</param>
        /// <seealso cref="Unregister"/>
        public void Register(Volume volume, int layer)
        {
            m_Volumes.Add(volume);

            // Look for existing cached layer masks and add it there if needed
            foreach (var kvp in m_SortedVolumes)
            {
                // We add the volume to sorted lists only if the layer match and if it doesn't contain the volume already.
                if ((kvp.Key & (1 << layer)) != 0 && !kvp.Value.Contains(volume))
                    kvp.Value.Add(volume);
            }

            SetLayerDirty(layer);
        }

        /// <summary>
        /// Unregisters a Volume from the manager. Unity does this automatically when a Volume is
        /// disabled or goes out of scope, but you can use this function to force-unregister a Volume
        /// that you added manually while it was disabled.
        /// </summary>
        /// <param name="volume">The Volume to unregister.</param>
        /// <param name="layer">The LayerMask that this Volume is in.</param>
        /// <seealso cref="Register"/>
        public void Unregister(Volume volume, int layer)
        {
            m_Volumes.Remove(volume);

            foreach (var kvp in m_SortedVolumes)
            {
                // Skip layer masks this volume doesn't belong to
                if ((kvp.Key & (1 << layer)) == 0)
                    continue;

                kvp.Value.Remove(volume);
            }
        }

        /// <summary>
        /// Checks if a <see cref="VolumeComponent"/> is active in a given LayerMask.
        /// </summary>
        /// <typeparam name="T">A type derived from <see cref="VolumeComponent"/></typeparam>
        /// <param name="layerMask">The LayerMask to check against</param>
        /// <returns><c>true</c> if the component is active in the LayerMask, <c>false</c>
        /// otherwise.</returns>
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

        internal void SetLayerDirty(int layer)
        {
            Assert.IsTrue(layer >= 0 && layer <= k_MaxLayerCount, "Invalid layer bit");

            foreach (var kvp in m_SortedVolumes)
            {
                var mask = kvp.Key;

                if ((mask & (1 << layer)) != 0)
                    m_SortNeeded[mask] = true;
            }
        }

        internal void UpdateVolumeLayer(Volume volume, int prevLayer, int newLayer)
        {
            Assert.IsTrue(prevLayer >= 0 && prevLayer <= k_MaxLayerCount, "Invalid layer bit");
            Unregister(volume, prevLayer);
            Register(volume, newLayer);
        }

        // Go through all listed components and lerp overridden values in the global state
        void OverrideData(VolumeStack stack, List<VolumeComponent> components, float interpFactor)
        {
            foreach (var component in components)
            {
                if (!component.active)
                    continue;

                if (!stack.archetype.ContainsType(VolumeComponentType.FromTypeUnsafe(component.GetType())))
                    continue;

                var state = stack.GetComponent(component.GetType());
                component.Override(state, interpFactor);
            }
        }

        /// <summary>
        /// Updates the Volume manager and stores the result in a custom <see cref="VolumeStack"/>.
        /// </summary>
        /// <param name="stack">The stack to store the blending result into.</param>
        /// <param name="trigger">A reference Transform to consider for positional Volume blending.
        /// </param>
        /// <param name="layerMask">The LayerMask that Unity uses to filter Volumes that it should consider
        /// for blending.</param>
        /// <seealso cref="VolumeStack"/>
        public void Update(VolumeStack stack, Transform trigger, LayerMask layerMask)
        {
            Assert.IsNotNull(stack);

            stack.CheckStack();

            if (stack.archetype.GetOrAddDefaultState(out var defaultState))
                // Start by resetting the global state to default values
                defaultState.ReplaceData(stack);

            bool onlyGlobal = trigger == null;
            var triggerPos = onlyGlobal ? Vector3.zero : trigger.position;

            // Sort the cached volume list(s) for the given layer mask if needed and return it
            var volumes = GrabVolumes(layerMask);

            Camera camera = null;
            // Behavior should be fine even if camera is null
            if (!onlyGlobal)
                trigger.TryGetComponent<Camera>(out camera);

            // Traverse all volumes
            foreach (var volume in volumes)
            {
                if (volume == null)
                    continue;

#if UNITY_EDITOR
                // Skip volumes that aren't in the scene currently displayed in the scene view
                if (!IsVolumeRenderedByCamera(volume, camera))
                    continue;
#endif

                // Skip disabled volumes and volumes without any data or weight
                if (!volume.enabled || volume.profileRef == null || volume.weight <= 0f)
                    continue;

                // Global volumes always have influence
                if (volume.isGlobal)
                {
                    OverrideData(stack, volume.profileRef.components, Mathf.Clamp01(volume.weight));
                    continue;
                }

                if (onlyGlobal)
                    continue;

                // If volume isn't global and has no collider, skip it as it's useless
                var colliders = m_TempColliders;
                volume.GetComponents(colliders);
                if (colliders.Count == 0)
                    continue;

                // Find closest distance to volume, 0 means it's inside it
                float closestDistanceSqr = float.PositiveInfinity;

                foreach (var collider in colliders)
                {
                    if (!collider.enabled)
                        continue;

                    var closestPoint = collider.ClosestPoint(triggerPos);
                    var d = (closestPoint - triggerPos).sqrMagnitude;

                    if (d < closestDistanceSqr)
                        closestDistanceSqr = d;
                }

                colliders.Clear();
                float blendDistSqr = volume.blendDistance * volume.blendDistance;

                // Volume has no influence, ignore it
                // Note: Volume doesn't do anything when `closestDistanceSqr = blendDistSqr` but we
                //       can't use a >= comparison as blendDistSqr could be set to 0 in which case
                //       volume would have total influence
                if (closestDistanceSqr > blendDistSqr)
                    continue;

                // Volume has influence
                float interpFactor = 1f;

                if (blendDistSqr > 0f)
                    interpFactor = 1f - (closestDistanceSqr / blendDistSqr);

                // No need to clamp01 the interpolation factor as it'll always be in [0;1[ range
                OverrideData(stack, volume.profileRef.components, interpFactor * Mathf.Clamp01(volume.weight));
            }
        }

        /// <summary>
        /// Get all volumes on a given layer mask sorted by influence.
        /// </summary>
        /// <param name="layerMask">The LayerMask that Unity uses to filter Volumes that it should consider.</param>
        /// <returns>An array of volume.</returns>
        public Volume[] GetVolumes(LayerMask layerMask)
        {
            var volumes = GrabVolumes(layerMask);
            volumes.RemoveAll(v => v == null);
            return volumes.ToArray();
        }

        List<Volume> GrabVolumes(LayerMask mask)
        {
            List<Volume> list;

            if (!m_SortedVolumes.TryGetValue(mask, out list))
            {
                // New layer mask detected, create a new list and cache all the volumes that belong
                // to this mask in it
                list = new List<Volume>();

                foreach (var volume in m_Volumes)
                {
                    if ((mask & (1 << volume.gameObject.layer)) == 0)
                        continue;

                    list.Add(volume);
                    m_SortNeeded[mask] = true;
                }

                m_SortedVolumes.Add(mask, list);
            }

            // Check sorting state
            bool sortNeeded;
            if (m_SortNeeded.TryGetValue(mask, out sortNeeded) && sortNeeded)
            {
                m_SortNeeded[mask] = false;
                SortByPriority(list);
            }

            return list;
        }

        // Stable insertion sort. Faster than List<T>.Sort() for our needs.
        static void SortByPriority(List<Volume> volumes)
        {
            Assert.IsNotNull(volumes, "Trying to sort volumes of non-initialized layer");

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

        static bool IsVolumeRenderedByCamera(Volume volume, Camera camera)
        {
#if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
            // IsGameObjectRenderedByCamera does not behave correctly when camera is null so we have to catch it here.
            return camera == null ? true : UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(volume.gameObject, camera);
#else
            return true;
#endif
        }
    }
}
