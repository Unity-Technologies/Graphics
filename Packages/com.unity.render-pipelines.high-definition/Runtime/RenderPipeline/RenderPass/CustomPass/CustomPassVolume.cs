using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using System.Linq;
using System;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Unity Monobehavior that manages the execution of custom passes.
    /// It provides
    /// </summary>
    [ExecuteAlways]
    [HDRPHelpURLAttribute("Custom-Pass")]
    public class CustomPassVolume : MonoBehaviour, IVolume
    {
        [SerializeField, FormerlySerializedAs("isGlobal")]
        private bool m_IsGlobal = true;

        /// <summary>
        /// Whether or not the volume is global. If true, the component will ignore all colliders attached to it
        /// </summary>
        public bool isGlobal
        {
            get => m_IsGlobal;
            set => m_IsGlobal = value;
        }

        /// <summary>
        /// Distance where the volume start to be rendered, the fadeValue field in C# will be updated to the normalized blend factor for your custom C# passes
        /// In the fullscreen shader pass and DrawRenderers shaders you can access the _FadeValue
        /// </summary>
        [Min(0)]
        public float fadeRadius;

        /// <summary>
        /// The volume priority, used to determine the execution order when there is multiple volumes with the same injection point.
        /// </summary>
        [Tooltip("Sets the Volume priority in the stack. A higher value means higher priority. You can use negative values.")]
        public float priority;

        /// <summary>
        /// List of custom passes to execute
        /// </summary>
        [SerializeReference]
        public List<CustomPass> customPasses = new List<CustomPass>();

        /// <summary>
        /// Where the custom passes are going to be injected in HDRP
        /// </summary>
        public CustomPassInjectionPoint injectionPoint = CustomPassInjectionPoint.BeforeTransparent;

        [SerializeField]
        internal Camera m_TargetCamera;

        /// <summary>
        /// Use this field to force the custom pass volume to be executed only for one camera.
        /// </summary>
        public Camera targetCamera
        {
            /// <summary>
            /// Get the target camera of the custom pass. The target camera can be null if the custom pass is in local or global mode.
            /// </summary>
            get => useTargetCamera ? m_TargetCamera : null;
            /// <summary>
            /// Sets the target camera of the custom pass volume, this will bypass the volume mode (local or global) and the volume mask of the camera.
            /// </summary>
            /// <value>The new camera value. A null value will disable target camera mode and fall back to local or global.</value>
            set
            {
                m_TargetCamera = value;
                useTargetCamera = value != null;
            }
        }

        /// <summary>
        /// Fade value between 0 and 1. it represent how close you camera is from the collider of the custom pass.
        /// 0 when the camera is outside the volume + fade radius and 1 when it is inside the collider.
        /// </summary>
        /// <value>The fade value that should be applied to the custom pass effect</value>
        public float fadeValue { get; private set; }

#if UNITY_EDITOR
        [System.NonSerialized]
        bool visible = true;
#endif

        [SerializeField]
        internal bool useTargetCamera;

        // The current active custom pass volume is simply the smallest overlapping volume with the trigger transform
        static HashSet<CustomPassVolume> m_ActivePassVolumes = new HashSet<CustomPassVolume>();
        static List<CustomPassVolume> m_OverlappingPassVolumes = new List<CustomPassVolume>();

        internal List<Collider> m_Colliders = new List<Collider>();

        /// <summary>
        /// The colliders of the volume if <see cref="isGlobal"/> is false
        /// </summary>
        public List<Collider> colliders => m_Colliders;

        List<Collider> m_OverlappingColliders = new List<Collider>();

        static List<CustomPassInjectionPoint> m_InjectionPoints;
        static List<CustomPassInjectionPoint> injectionPoints
        {
            get
            {
                if (m_InjectionPoints == null)
                    m_InjectionPoints = Enum.GetValues(typeof(CustomPassInjectionPoint)).Cast<CustomPassInjectionPoint>().ToList();
                return m_InjectionPoints;
            }
        }

        void OnEnable()
        {
            // Remove null passes in case of something happens during the deserialization of the passes
            customPasses.RemoveAll(c => c is null);
            GetComponents(m_Colliders);
            Register(this);

#if UNITY_EDITOR
            SceneVisibilityManager.visibilityChanged -= UpdateCustomPassVolumeVisibility;
            SceneVisibilityManager.visibilityChanged += UpdateCustomPassVolumeVisibility;
#endif
        }

        void OnDisable()
        {
            UnRegister(this);
            CleanupPasses();
#if UNITY_EDITOR
            SceneVisibilityManager.visibilityChanged -= UpdateCustomPassVolumeVisibility;
#endif
        }

#if UNITY_EDITOR
        void UpdateCustomPassVolumeVisibility()
        {
            visible = !SceneVisibilityManager.instance.IsHidden(gameObject);
        }

#endif


        bool IsVisible(HDCamera hdCamera)
        {
#if UNITY_EDITOR
            // Scene visibility
            if (hdCamera.camera.cameraType == CameraType.SceneView && !visible)
                return false;

            // Prefab context mode visibility
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                // Check if the current volume is inside the currently edited prefab
                bool isVolumeInPrefab = gameObject.scene == stage.scene;

                if (!isVolumeInPrefab && stage.mode == PrefabStage.Mode.InIsolation)
                    return false;

                if (!isVolumeInPrefab)
                {
                    // Prefab context is hidden and the current volume is outside of the prefab so we don't render the effects
                    if (CoreUtils.IsSceneViewPrefabStageContextHidden())
                        return false;
                }
            }

#endif

            if (useTargetCamera)
                return targetCamera == hdCamera.camera;

            // We never execute volume if the layer is not within the culling layers of the camera
            // Special case for the scene view: we can't easily change it's volume later mask, so by default we show all custom passes
            if (hdCamera.camera.cameraType != CameraType.SceneView && (hdCamera.volumeLayerMask & (1 << gameObject.layer)) == 0)
                return false;

            return true;
        }

        internal bool Execute(RenderGraph renderGraph, HDCamera hdCamera, CullingResults cullingResult, CullingResults cameraCullingResult, in CustomPass.RenderTargets targets)
        {
            bool executed = false;

            if (!IsVisible(hdCamera))
                return false;

            foreach (var pass in customPasses)
            {
                if (pass != null && pass.WillBeExecuted(hdCamera))
                {
                    pass.ExecuteInternal(renderGraph, hdCamera, cullingResult, cameraCullingResult, targets, this);
                    executed = true;
                }
            }

            return executed;
        }

        internal bool WillExecuteInjectionPoint(HDCamera hdCamera)
        {
            bool executed = false;

            if (!IsVisible(hdCamera))
                return false;

            foreach (var pass in customPasses)
            {
                if (pass != null && pass.WillBeExecuted(hdCamera))
                    executed = true;
            }

            return executed;
        }

        internal void CleanupPasses()
        {
            foreach (var pass in customPasses)
                pass.CleanupPassInternal();
        }

        static void Register(CustomPassVolume volume) => m_ActivePassVolumes.Add(volume);

        static void UnRegister(CustomPassVolume volume) => m_ActivePassVolumes.Remove(volume);

        internal static void Update(HDCamera camera)
        {
            var triggerPos = camera.volumeAnchor.position;

            m_OverlappingPassVolumes.Clear();

            // Traverse all volumes
            foreach (var volume in m_ActivePassVolumes)
            {
                if (!volume.IsVisible(camera))
                    continue;

                if (volume.useTargetCamera)
                {
                    if (volume.targetCamera == camera.camera)
                        m_OverlappingPassVolumes.Add(volume);
                    continue;
                }

                // Global volumes always have influence
                if (volume.isGlobal)
                {
                    volume.fadeValue = 1.0f;
                    m_OverlappingPassVolumes.Add(volume);
                    continue;
                }

                // If volume isn't global and has no collider, skip it as it's useless
                if (volume.m_Colliders.Count == 0)
                    continue;

                volume.m_OverlappingColliders.Clear();

                float sqrFadeRadius = Mathf.Max(float.Epsilon, volume.fadeRadius * volume.fadeRadius);
                float minSqrDistance = 1e20f;

                foreach (var collider in volume.m_Colliders)
                {
                    if (!collider || !collider.enabled)
                        continue;

                    // We don't support concave colliders
                    if (collider is MeshCollider m && !m.convex)
                        continue;

                    var closestPoint = collider.ClosestPoint(triggerPos);
                    var d = (closestPoint - triggerPos).sqrMagnitude;

                    minSqrDistance = Mathf.Min(minSqrDistance, d);

                    // Update the list of overlapping colliders
                    if (d <= sqrFadeRadius)
                        volume.m_OverlappingColliders.Add(collider);
                }

                // update the fade value:
                volume.fadeValue = 1.0f - Mathf.Clamp01(Mathf.Sqrt(minSqrDistance / sqrFadeRadius));

                if (volume.m_OverlappingColliders.Count > 0)
                    m_OverlappingPassVolumes.Add(volume);
            }

            // Sort the overlapping volumes by priority order (smaller first, then larger and finally globals)
            m_OverlappingPassVolumes.Sort((v1, v2) =>
            {
                float GetVolumeExtent(CustomPassVolume volume)
                {
                    float extent = 0;
                    foreach (var collider in volume.m_OverlappingColliders)
                        extent += collider.bounds.extents.magnitude;
                    return extent;
                }

                // Sort by priority and then by volume extent
                if (v1.priority == v2.priority)
                {
                    if (v1.isGlobal && v2.isGlobal) return 0;
                    if (v1.isGlobal) return 1;
                    if (v2.isGlobal) return -1;

                    return GetVolumeExtent(v1).CompareTo(GetVolumeExtent(v2));
                }
                else
                {
                    return v2.priority.CompareTo(v1.priority);
                }
            });
        }

        internal void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
        {
            foreach (var pass in customPasses)
            {
                if (pass != null && pass.enabled)
                    pass.InternalAggregateCullingParameters(ref cullingParameters, hdCamera);
            }
        }

        internal static CullingResults? Cull(ScriptableRenderContext renderContext, HDCamera hdCamera)
        {
            CullingResults? result = null;

            // We need to sort the volumes first to know which one will be executed
            // TODO: cache the results per camera in the HDRenderPipeline so it's not executed twice per camera
            Update(hdCamera);

            // For each injection points, we gather the culling results for
            hdCamera.camera.TryGetCullingParameters(out var cullingParameters);

            // By default we don't want the culling to return any objects
            cullingParameters.cullingMask = 0;
            cullingParameters.cullingOptions = CullingOptions.None;

            foreach (var volume in m_OverlappingPassVolumes)
                volume?.AggregateCullingParameters(ref cullingParameters, hdCamera);

            // If we don't have anything to cull or the pass is asking for the same culling layers than the camera, we don't have to re-do the culling
            if (cullingParameters.cullingMask != 0 && (cullingParameters.cullingMask & hdCamera.camera.cullingMask) != cullingParameters.cullingMask)
                result = renderContext.Cull(ref cullingParameters);

            return result;
        }

        internal static void Cleanup()
        {
            foreach (var pass in m_ActivePassVolumes)
            {
                pass.CleanupPasses();
            }
        }

        /// <summary>
        /// Gets the currently active Custom Pass Volume for a given injection point.
        /// Note this function returns only the first active volume, not the others that will be executed.
        /// </summary>
        /// <param name="injectionPoint">The injection point to get the currently active Custom Pass Volume for.</param>
        /// <returns>Returns the Custom Pass Volume instance associated with the injection point.</returns>
        [Obsolete("In order to support multiple custom pass volume per injection points, please use GetActivePassVolumes.")]
        public static CustomPassVolume GetActivePassVolume(CustomPassInjectionPoint injectionPoint)
        {
            var volumes = new List<CustomPassVolume>();
            GetActivePassVolumes(injectionPoint, volumes);
            return volumes.FirstOrDefault();
        }

        /// <summary>
        /// Gets the currently active Custom Pass Volume for a given injection point.
        /// </summary>
        /// <param name="injectionPoint">The injection point to get the currently active Custom Pass Volume for.</param>
        /// <param name="volumes">The list of custom pass volumes to popuplate with the active volumes.</param>
        public static void GetActivePassVolumes(CustomPassInjectionPoint injectionPoint, List<CustomPassVolume> volumes)
        {
            volumes.Clear();
            foreach (var volume in m_OverlappingPassVolumes)
                if (volume.injectionPoint == injectionPoint)
                    volumes.Add(volume);
        }

        /// <summary>
        /// Add a pass of type passType in the active pass list
        /// </summary>
        /// <typeparam name="T">The type of the CustomPass to create</typeparam>
        /// <returns>The new custom</returns>
        public CustomPass AddPassOfType<T>() where T : CustomPass => AddPassOfType(typeof(T));

        /// <summary>
        /// Add a pass of type passType in the active pass list
        /// </summary>
        /// <param name="passType">The type of the CustomPass to create</param>
        /// <returns>The new custom</returns>
        public CustomPass AddPassOfType(Type passType)
        {
            if (!typeof(CustomPass).IsAssignableFrom(passType))
            {
                Debug.LogError($"Can't add pass type {passType} to the list because it does not inherit from CustomPass.");
                return null;
            }

            var customPass = Activator.CreateInstance(passType) as CustomPass;
            customPasses.Add(customPass);
            return customPass;
        }

#if UNITY_EDITOR
        // In the editor, we refresh the list of colliders at every frame because it's frequent to add/remove them
        void Update() => GetComponents(m_Colliders);
#endif
    }
}
