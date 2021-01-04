using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using System.Linq;
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Unity Monobehavior that manages the execution of custom passes.
    /// It provides
    /// </summary>
    [ExecuteAlways]
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Custom-Pass" + Documentation.endURL)]
    public class CustomPassVolume : MonoBehaviour
    {
        /// <summary>
        /// Whether or not the volume is global. If true, the component will ignore all colliders attached to it
        /// </summary>
        public bool isGlobal = true;

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

        // The current active custom pass volume is simply the smallest overlapping volume with the trigger transform
        static HashSet<CustomPassVolume>    m_ActivePassVolumes = new HashSet<CustomPassVolume>();
        static List<CustomPassVolume>       m_OverlappingPassVolumes = new List<CustomPassVolume>();

        List<Collider>          m_Colliders = new List<Collider>();
        List<Collider>          m_OverlappingColliders = new List<Collider>();

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
            UnityEditor.SceneVisibilityManager.visibilityChanged -= UpdateCustomPassVolumeVisibility;
            UnityEditor.SceneVisibilityManager.visibilityChanged += UpdateCustomPassVolumeVisibility;
#endif
        }

        void OnDisable()
        {
            UnRegister(this);
            CleanupPasses();
#if UNITY_EDITOR
            UnityEditor.SceneVisibilityManager.visibilityChanged -= UpdateCustomPassVolumeVisibility;
#endif
        }

#if UNITY_EDITOR
        void UpdateCustomPassVolumeVisibility()
        {
            visible = !UnityEditor.SceneVisibilityManager.instance.IsHidden(gameObject);
        }

#endif

        bool IsVisible(HDCamera hdCamera)
        {
#if UNITY_EDITOR
            // Scene visibility
            if (hdCamera.camera.cameraType == CameraType.SceneView && !visible)
                return false;
#endif

            // We never execute volume if the layer is not within the culling layers of the camera
            if ((hdCamera.volumeLayerMask & (1 << gameObject.layer)) == 0)
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
                // Ignore volumes that are not in the camera layer mask
                if ((camera.volumeLayerMask & (1 << volume.gameObject.layer)) == 0)
                    continue;

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
            m_OverlappingPassVolumes.Sort((v1, v2) => {
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
            CullingResults?  result = null;

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

        void OnDrawGizmos()
        {
            if (isGlobal || m_Colliders.Count == 0 || !enabled)
                return;

            var scale = transform.localScale;
            var invScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
            Gizmos.color = CoreRenderPipelinePreferences.volumeGizmoColor;

            // Draw a separate gizmo for each collider
            foreach (var collider in m_Colliders)
            {
                if (!collider || !collider.enabled)
                    continue;

                // We'll just use scaling as an approximation for volume skin. It's far from being
                // correct (and is completely wrong in some cases). Ultimately we'd use a distance
                // field or at least a tesselate + push modifier on the collider's mesh to get a
                // better approximation, but the current Gizmo system is a bit limited and because
                // everything is dynamic in Unity and can be changed at anytime, it's hard to keep
                // track of changes in an elegant way (which we'd need to implement a nice cache
                // system for generated volume meshes).
                switch (collider)
                {
                    case BoxCollider c:
                        Gizmos.DrawCube(c.center, c.size);
                        if (fadeRadius > 0)
                        {
                            // invert te scale for the fade radius because it's in fixed units
                            Vector3 s = new Vector3(
                                (fadeRadius * 2) / scale.x,
                                (fadeRadius * 2) / scale.y,
                                (fadeRadius * 2) / scale.z
                            );
                            Gizmos.DrawWireCube(c.center, c.size + s);
                        }
                        break;
                    case SphereCollider c:
                        // For sphere the only scale that is used is the transform.x
                        Matrix4x4 oldMatrix = Gizmos.matrix;
                        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one * scale.x);
                        Gizmos.DrawSphere(c.center, c.radius);
                        if (fadeRadius > 0)
                            Gizmos.DrawWireSphere(c.center, c.radius + fadeRadius / scale.x);
                        Gizmos.matrix = oldMatrix;
                        break;
                    case MeshCollider c:
                        // Only convex mesh m_Colliders are allowed
                        if (!c.convex)
                            c.convex = true;

                        // Mesh pivot should be centered or this won't work
                        Gizmos.DrawMesh(c.sharedMesh);

                        // We don't display the Gizmo for fade distance mesh because the distances would be wrong
                        break;
                    default:
                        // Nothing for capsule (DrawCapsule isn't exposed in Gizmo), terrain, wheel and
                        // other m_Colliders...
                        break;
                }
            }
        }

#endif
    }
}
