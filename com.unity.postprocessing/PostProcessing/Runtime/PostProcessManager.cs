using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// This manager tracks all volumes in the scene and does all the interpolation work. It is
    /// automatically created as soon as Post-processing is active in a scene.
    /// </summary>
    public sealed class PostProcessManager
    {
        static PostProcessManager s_Instance;

        /// <summary>
        /// The current singleton instance of <see cref="PostProcessManager"/>.
        /// </summary>
        public static PostProcessManager instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new PostProcessManager();

                return s_Instance;
            }
        }

        const int k_MaxLayerCount = 32; // Max amount of layers available in Unity
        readonly PostProcessVolumeDatabase m_Volumes;
        readonly List<PostProcessEffectSettings> m_BaseSettings;
        readonly List<Collider> m_TempColliders;

        /// <summary>
        /// This dictionary maps all <see cref="PostProcessEffectSettings"/> available to their
        /// corresponding <see cref="PostProcessAttribute"/>. It can be used to list all loaded
        /// builtin and custom effects.
        /// </summary>
        public readonly Dictionary<Type, PostProcessAttribute> settingsTypes;

        PostProcessManager()
        {
            m_Volumes = new();
            m_BaseSettings = new List<PostProcessEffectSettings>();
            m_TempColliders = new List<Collider>(5);

            settingsTypes = new Dictionary<Type, PostProcessAttribute>();
            ReloadBaseTypes();
        }

#if UNITY_EDITOR
        // Called every time Unity recompile scripts in the editor. We need this to keep track of
        // any new custom effect the user might add to the project
        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnEditorReload()
        {
            instance.ReloadBaseTypes();
        }

#endif

        void CleanBaseTypes()
        {
            settingsTypes.Clear();

            foreach (var settings in m_BaseSettings)
                RuntimeUtilities.Destroy(settings);

            m_BaseSettings.Clear();
        }

        // This will be called only once at runtime and everytime script reload kicks-in in the
        // editor as we need to keep track of any compatible post-processing effects in the project
        void ReloadBaseTypes()
        {
            CleanBaseTypes();

            // Rebuild the base type map
            var types = RuntimeUtilities.GetAllTypesDerivedFrom<PostProcessEffectSettings>()
                .Where(
                    t => t.IsDefined(typeof(PostProcessAttribute), false)
                    && !t.IsAbstract
                );

            foreach (var type in types)
            {
                settingsTypes.Add(type, type.GetAttribute<PostProcessAttribute>());

                // Create an instance for each effect type, these will be used for the lowest
                // priority global volume as we need a default state when exiting volume ranges
                var inst = (PostProcessEffectSettings)ScriptableObject.CreateInstance(type);
                inst.SetAllOverridesTo(true, false);
                m_BaseSettings.Add(inst);
            }
        }

        /// <summary>
        /// Gets a list of all volumes currently affecting the given layer. Results aren't sorted
        /// and the list isn't cleared.
        /// </summary>
        /// <param name="layer">The layer to look for</param>
        /// <param name="results">A list to store the volumes found</param>
        /// <param name="skipDisabled">Should we skip disabled volumes?</param>
        /// <param name="skipZeroWeight">Should we skip 0-weight volumes?</param>
        public void GetActiveVolumes(PostProcessLayer layer, List<PostProcessVolume> results, bool skipDisabled = true, bool skipZeroWeight = true)
        {
            // If no trigger is set, only global volumes will have influence
            int mask = layer.volumeLayer.value;
            var volumeTrigger = layer.volumeTrigger;
            bool onlyGlobal = volumeTrigger == null;
            var triggerPos = onlyGlobal ? Vector3.zero : volumeTrigger.position;

            // Sort the cached volume list(s) for the given layer mask if needed and return it
            using (ListPool<PostProcessVolume>.Get(out var volumes))
            {
                Exception e;
                if ((e = m_Volumes.FindSortedVolumesByLayerMask(mask, volumes)) != null)
                {
                    Debug.LogException(e);
                    return;
                }

                // Traverse all volumes
                foreach (var volume in volumes)
                {
                    // Skip disabled volumes and volumes without any data or weight
                    if ((skipDisabled && !volume.enabled) || volume.profileRef == null || (skipZeroWeight && volume.weight <= 0f))
                        continue;

                    // Global volume always have influence
                    if (volume.isGlobal)
                    {
                        results.Add(volume);
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

                        var closestPoint = collider.ClosestPoint(triggerPos); // 5.6-only API
                        var d = ((closestPoint - triggerPos) / 2f).sqrMagnitude;

                        if (d < closestDistanceSqr)
                            closestDistanceSqr = d;
                    }

                    colliders.Clear();
                    float blendDistSqr = volume.blendDistance * volume.blendDistance;

                    // Check for influence
                    if (closestDistanceSqr <= blendDistSqr)
                        results.Add(volume);
                }
            }
        }

        /// <summary>
        /// Gets the highest priority volume affecting a given layer.
        /// </summary>
        /// <param name="layer">The layer to look for</param>
        /// <returns>The highest priority volume affecting the layer</returns>
        public PostProcessVolume GetHighestPriorityVolume(PostProcessLayer layer)
        {
            if (layer == null)
                throw new ArgumentNullException("layer");

            return GetHighestPriorityVolume(layer.volumeLayer);
        }

        /// <summary>
        /// Gets the highest priority volume affecting <see cref="PostProcessLayer"/> in a given
        /// <see cref="LayerMask"/>.
        /// </summary>
        /// <param name="mask">The layer mask to look for</param>
        /// <returns>The highest priority volume affecting the layer mask</returns>
        /// <seealso cref="PostProcessLayer.volumeLayer"/>
        public PostProcessVolume GetHighestPriorityVolume(LayerMask mask)
        {
            Exception e;
            if ((e = m_Volumes.GetHighestPriorityVolumeInMaskInCache(mask, out var output)) != null)
                Debug.LogException(e);

            return output;
        }

        /// <summary>
        /// Helper method to spawn a new volume in the scene.
        /// </summary>
        /// <param name="layer">The unity layer to put the volume in</param>
        /// <param name="priority">The priority to set this volume to</param>
        /// <param name="settings">A list of effects to put in this volume</param>
        /// <returns></returns>
        public PostProcessVolume QuickVolume(int layer, float priority, params PostProcessEffectSettings[] settings)
        {
            var gameObject = new GameObject()
            {
                name = "Quick Volume",
                layer = layer,
                hideFlags = HideFlags.HideAndDontSave
            };

            var volume = gameObject.AddComponent<PostProcessVolume>();
            volume.priority = priority;
            volume.isGlobal = true;
            var profile = volume.profile;

            foreach (var s in settings)
            {
                Assert.IsNotNull(s, "Trying to create a volume with null effects");
                profile.AddSettings(s);
            }

            return volume;
        }

        internal void UpdateVolumeLayerMeta(PostProcessVolume volume, PostProcessVolumeMeta newMeta)
        {
            m_Volumes.UpdateVolumeLayerMeta(volume, newMeta);
        }

        internal void Register(PostProcessVolume volume, PostProcessVolumeMeta meta)
        {
            m_Volumes.Add(volume, meta);
        }

        internal void Unregister(PostProcessVolume volume)
        {
            m_Volumes.Remove(volume);
        }

        // Faster version of OverrideSettings to force replace values in the global state
        void ReplaceData(PostProcessLayer postProcessLayer)
        {
            foreach (var settings in m_BaseSettings)
            {
                var target = postProcessLayer.GetBundle(settings.GetType()).settings;
                int count = settings.parameters.Count;

                for (int i = 0; i < count; i++)
                    target.parameters[i].SetValue(settings.parameters[i]);
            }
        }

        internal void UpdateSettings(PostProcessLayer postProcessLayer, Camera camera)
        {
            // Reset to base state
            ReplaceData(postProcessLayer);

            // If no trigger is set, only global volumes will have influence
            int mask = postProcessLayer.volumeLayer.value;
            var volumeTrigger = postProcessLayer.volumeTrigger;
            bool onlyGlobal = volumeTrigger == null;
            var triggerPos = onlyGlobal ? Vector3.zero : volumeTrigger.position;

            // Sort the cached volume list(s) for the given layer mask if needed and return it
            using (ListPool<PostProcessVolume>.Get(out var volumes))
            {
                Exception e;
                if ((e = m_Volumes.FindSortedVolumesByLayerMask(mask, volumes)) != null)
                {
                    Debug.LogException(e);
                    return;
                }

                // Traverse all volumes
                foreach (var volume in volumes)
                {
    #if UNITY_EDITOR
                    // Skip volumes that aren't in the scene currently displayed in the scene view
                    if (!IsVolumeRenderedByCamera(volume, camera))
                        continue;
    #endif

                    // Skip disabled volumes and volumes without any data or weight
                    if (!volume.enabled || volume.profileRef == null || volume.weight <= 0f)
                        continue;

                    var settings = volume.profileRef.settings;

                    // Global volume always have influence
                    if (volume.isGlobal)
                    {
                        postProcessLayer.OverrideSettings(settings, Mathf.Clamp01(volume.weight));
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

                        var closestPoint = collider.ClosestPoint(triggerPos); // 5.6-only API
                        var d = ((closestPoint - triggerPos) / 2f).sqrMagnitude;

                        if (d < closestDistanceSqr)
                            closestDistanceSqr = d;
                    }

                    colliders.Clear();
                    float blendDistSqr = volume.blendDistance * volume.blendDistance;

                    // Volume has no influence, ignore it
                    // Note: Volume doesn't do anything when `closestDistanceSqr = blendDistSqr` but
                    //       we can't use a >= comparison as blendDistSqr could be set to 0 in which
                    //       case volume would have total influence
                    if (closestDistanceSqr > blendDistSqr)
                        continue;

                    // Volume has influence
                    float interpFactor = 1f;

                    if (blendDistSqr > 0f)
                        interpFactor = 1f - (closestDistanceSqr / blendDistSqr);

                    // No need to clamp01 the interpolation factor as it'll always be in [0;1[ range
                    postProcessLayer.OverrideSettings(settings, interpFactor * Mathf.Clamp01(volume.weight));
                }
            }
        }

        static bool IsVolumeRenderedByCamera(PostProcessVolume volume, Camera camera)
        {
#if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
            return UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(volume.gameObject, camera);
#else
            return true;
#endif
        }
    }
}
