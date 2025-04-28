using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Serialization;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Holds information about whether to override certain camera rendering options from the render pipeline asset.
    /// When set to <c>Off</c> option will be disabled regardless of what is set on the pipeline asset.
    /// When set to <c>On</c> option will be enabled regardless of what is set on the pipeline asset.
    /// When set to <c>UsePipelineSetting</c> value set in the <see cref="UniversalRenderPipelineAsset"/>.
    /// </summary>
    public enum CameraOverrideOption
    {
        /// <summary>
        /// Use this to disable regardless of what is set on the pipeline asset.
        /// </summary>
        Off,

        /// <summary>
        /// Use this to enable regardless of what is set on the pipeline asset.
        /// </summary>
        On,

        /// <summary>
        /// Use this to choose the setting set on the pipeline asset.
        /// </summary>
        [InspectorName("Use settings from Render Pipeline Asset")]
        UsePipelineSettings,
    }

    /// <summary>
    /// Holds information about the post-processing anti-aliasing mode.
    /// When set to <c>None</c> no post-processing anti-aliasing pass will be performed.
    /// When set to <c>Fast</c> a fast approximated anti-aliasing pass will render when resolving the camera to screen.
    /// When set to <c>SubpixelMorphologicalAntiAliasing</c> SMAA pass will render when resolving the camera to screen.
    /// You can choose the SMAA quality by setting <see cref="AntialiasingQuality"/>.
    /// </summary>
    public enum AntialiasingMode
    {
        /// <summary>
        /// Use this to have no post-processing anti-aliasing pass performed.
        /// </summary>
        [InspectorName("No Anti-aliasing")]
        None,

        /// <summary>
        /// Use this to have a fast approximated anti-aliasing pass rendered when resolving the camera to screen
        /// </summary>
        [InspectorName("Fast Approximate Anti-aliasing (FXAA)")]
        FastApproximateAntialiasing,

        /// <summary>
        /// Use this to have a <c>SubpixelMorphologicalAntiAliasing</c> SMAA pass rendered when resolving the camera to screen
        /// You can choose the SMAA quality by setting <see cref="AntialiasingQuality"/>.
        /// </summary>
        [InspectorName("Subpixel Morphological Anti-aliasing (SMAA)")]
        SubpixelMorphologicalAntiAliasing,

        /// <summary>
        /// Use this to have a temporal anti-aliasing pass rendered when resolving camera to screen.
        /// </summary>
        [InspectorName("Temporal Anti-aliasing (TAA)")]
        TemporalAntiAliasing,
    }

    /// <summary>
    /// Holds information about the render type of a camera. Options are Base or Overlay.
    /// Base rendering type allows the camera to render to either the screen or to a texture.
    /// Overlay rendering type allows the camera to render on top of a previous camera output, thus compositing camera results.
    /// </summary>
    public enum CameraRenderType
    {
        /// <summary>
        /// Use this to select the base camera render type.
        /// Base rendering type allows the camera to render to either the screen or to a texture.
        /// </summary>
        Base,

        /// <summary>
        /// Use this to select the overlay camera render type.
        /// Overlay rendering type allows the camera to render on top of a previous camera output, thus compositing camera results.
        /// </summary>
        Overlay,
    }

    /// <summary>
    /// Controls <c>SubpixelMorphologicalAntiAliasing</c> SMAA anti-aliasing quality.
    /// </summary>
    public enum AntialiasingQuality
    {
        /// <summary>
        /// Use this to select the low <c>SubpixelMorphologicalAntiAliasing</c> SMAA quality
        /// </summary>
        Low,

        /// <summary>
        /// Use this to select the medium <c>SubpixelMorphologicalAntiAliasing</c> SMAA quality
        /// </summary>
        Medium,

        /// <summary>
        /// Use this to select the high <c>SubpixelMorphologicalAntiAliasing</c> SMAA quality
        /// </summary>
        High
    }

    /// <summary>
    /// Contains extension methods for Camera class.
    /// </summary>
    /// <remarks>
    /// This class adds extension methods to the <see cref="Camera"/> class.
    /// It provides helper methods for retrieving the <see cref="UniversalAdditionalCameraData"/>, and
    /// getting and setting volume update settings.
    /// </remarks>
    /// <example>
    /// <para>This sample code shows how to get <see cref="UniversalAdditionalCameraData"/> and manually update the <see cref="VolumeStack"/> at runtime.</para>
    /// <code>
    /// using UnityEngine;
    /// using UnityEngine.Rendering.Universal;
    ///
    /// public class UpdateVolumeStackOnCamera : MonoBehaviour
    /// {
    ///     // Reference to the camera component
    ///     [SerializeField] private Camera cameraToUpdate;
    ///
    ///     // Toggle to make the script update the volume framework
    ///     [SerializeField] private bool shouldUpdateVolumeFramework;
    ///
    ///
    ///     // Update is called once per frame
    ///     private void Update()
    ///     {
    ///         // Leave if the volume framework shouldn't be updated
    ///         if (!shouldUpdateVolumeFramework)
    ///             return;
    ///
    ///         // Mark that we shouldn't update the volume framework in the next frame
    ///         shouldUpdateVolumeFramework = false;
    ///
    ///         // Check if we have a reference to the camera
    ///         if (cameraToUpdate == null)
    ///         {
    ///             Debug.LogError("UpdateVolumeStackOnCamera.Update(): cameraToUpdate has not been assigned.");
    ///             return;
    ///         }
    ///
    ///         // Make the camera use the ViaScripting mode so the volume framework only updates when the script calls UpdateVolumeStack().
    ///         VolumeFrameworkUpdateMode volumeFrameworkUpdateMode = cameraToUpdate.GetVolumeFrameworkUpdateMode();
    ///         if (volumeFrameworkUpdateMode != VolumeFrameworkUpdateMode.ViaScripting)
    ///         {
    ///             cameraToUpdate.SetVolumeFrameworkUpdateMode(VolumeFrameworkUpdateMode.ViaScripting);
    ///         }
    ///
    ///         // Update the volume framework
    ///         cameraToUpdate.UpdateVolumeStack();
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="Camera"/>
    /// <seealso cref="UniversalAdditionalCameraData"/>
    /// <seealso cref="VolumeFrameworkUpdateMode"/>
    /// <seealso cref="LayerMask"/>
    /// <seealso cref="Transform"/>
    /// <seealso cref="VolumeManager"/>
    /// <seealso cref="VolumeStack"/>
    public static class CameraExtensions
    {
        /// <summary>
        /// Provides additional camera data component that's attached to the given camera.
        /// </summary>
        /// <remarks>
        /// The Universal Render Pipeline exposes various additional rendering data in a separate component named 'UniversalAdditionalCameraData'.
        /// This method returns the additional data component for the given camera or creates one if it doesn't exist yet.
        /// </remarks>
        /// <param name="camera">The <see cref="Camera"/> to use for getting the <see cref="UniversalAdditionalCameraData"/>.</param>
        /// <returns>The <see cref="UniversalAdditionalCameraData"/> for this camera.</returns>
        public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera camera)
        {
            var gameObject = camera.gameObject;
            bool componentExists = gameObject.TryGetComponent<UniversalAdditionalCameraData>(out var cameraData);
            if (!componentExists)
                cameraData = gameObject.AddComponent<UniversalAdditionalCameraData>();

            return cameraData;
        }

        /// <summary>
        /// Returns the update frequency for the volume framework associated with the camera.
        /// </summary>
        /// <remarks>
        /// Each camera can be set to update the volume framework in each frame, via scripting or using the setting
        /// set in the <see cref="UniversalRenderPipelineAsset"/>. This method returns the setting for the given camera.
        /// </remarks>
        /// <param name="camera">The <see cref="Camera"/> to use for getting the <see cref="VolumeFrameworkUpdateMode"/>.</param>
        /// <returns>The <see cref="VolumeFrameworkUpdateMode"/> set on this camera.</returns>
        public static VolumeFrameworkUpdateMode GetVolumeFrameworkUpdateMode(this Camera camera)
        {
            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            return cameraData.volumeFrameworkUpdateMode;
        }

        /// <summary>
        /// Sets the update frequency for the volume framework associated with the camera.
        /// </summary>
        /// <remarks>
        /// Each camera can be set to update the volume framework in each frame, via scripting or using the setting
        /// set in the <see cref="UniversalRenderPipelineAsset"/>. This method is used to change that setting for the given camera.
        /// </remarks>
        /// <param name="camera">The <see cref="Camera"/> to use for setting the <see cref="VolumeStack"/>.</param>
        /// <param name="mode">The <see cref="VolumeFrameworkUpdateMode"/> value to set.</param>
        public static void SetVolumeFrameworkUpdateMode(this Camera camera, VolumeFrameworkUpdateMode mode)
        {
            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            if (cameraData.volumeFrameworkUpdateMode == mode)
                return;

            bool requiredUpdatePreviously = cameraData.requiresVolumeFrameworkUpdate;
            cameraData.volumeFrameworkUpdateMode = mode;

            // We only update the local volume stacks for cameras set to ViaScripting.
            // Otherwise it will be updated in every frame.
            // We also check the previous value to make sure we're not updating when
            // switching between Camera ViaScripting and the URP Asset set to ViaScripting
            if (requiredUpdatePreviously && !cameraData.requiresVolumeFrameworkUpdate)
                camera.UpdateVolumeStack(cameraData);
        }

        /// <summary>
        /// Updates the volume stack for this camera.
        /// </summary>
        /// <remarks>
        /// This method only updates the <see cref="VolumeStack"/> when the camera has `ViaScripting` enabled or if
        /// the camera is set to `UsePipelineSettings` and the Render Pipeline Asset is set to `ViaScripting`.
        /// </remarks>
        /// <param name="camera">The <see cref="Camera"/> to use for updating the <see cref="VolumeStack"/>.</param>
        public static void UpdateVolumeStack(this Camera camera)
        {
            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            camera.UpdateVolumeStack(cameraData);
        }

        /// <summary>
        /// Updates the volume stack for this camera.
        /// </summary>
        /// <remarks>
        /// This method only updates the <see cref="VolumeStack"/> when the camera has `ViaScripting` enabled or if
        /// the camera is set to `UsePipelineSettings` and the Render Pipeline Asset is set to `ViaScripting`.
        /// </remarks>
        /// <param name="camera">The <see cref="Camera"/> to use for updating the <see cref="VolumeStack"/>.</param>
        /// <param name="cameraData">The <see cref="UniversalAdditionalCameraData"/> component on the camera that should be updated.</param>
        public static void UpdateVolumeStack(this Camera camera, UniversalAdditionalCameraData cameraData)
        {
            Assert.IsNotNull(cameraData, "cameraData can not be null when updating the volume stack.");

            // UUM-91000: The UpdateVolumeStack may happens before the pipeline is constructed.
            // Repro: enter play mode with a script that trigger this API at Start.
            if (!VolumeManager.instance.isInitialized)
            {
                Debug.LogError($"{nameof(UpdateVolumeStack)} must not be called before {nameof(VolumeManager)}.{nameof(VolumeManager.instance)}.{nameof(VolumeManager.instance.Initialize)}. If you tries calling this from Awake or Start, try instead to use the {nameof(RenderPipelineManager)}.{nameof(RenderPipelineManager.activeRenderPipelineCreated)} callback to be sure your render pipeline is fully initialized before calling this.");
                return;
            }

            // We only update the local volume stacks for cameras set to ViaScripting.
            // Otherwise it will be updated in the frame.
            if (cameraData.requiresVolumeFrameworkUpdate)
                return;

            // Create stack for camera
            if (cameraData.volumeStack == null)
                cameraData.GetOrCreateVolumeStack();

            camera.GetVolumeLayerMaskAndTrigger(cameraData, out LayerMask layerMask, out Transform trigger);
            VolumeManager.instance.Update(cameraData.volumeStack, trigger, layerMask);
        }

        /// <summary>
        /// Destroys the <see cref="VolumeStack"/> for this camera.
        /// </summary>
        /// <remarks>
        /// Each camera can hold a local <see cref="VolumeStack"/>, which contains the state of a volume blending update.
        /// A global stack is available by default in <see cref="VolumeManager"/> but you can also create your own using
        /// <see cref="VolumeManager.CreateStack"/> if you need to update the manager with specific
        /// settings and store the results for later use.
        /// </remarks>
        /// <param name="camera">The <see cref="Camera"/> to use for destroying the <see cref="VolumeStack"/>.</param>
        public static void DestroyVolumeStack(this Camera camera)
        {
            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            camera.DestroyVolumeStack(cameraData);
        }

        /// <summary>
        /// Destroys the <see cref="VolumeStack"/> for this camera.
        /// </summary>
        /// <remarks>
        /// Each camera can hold a local <see cref="VolumeStack"/>, which contains the state of a Volume blending update.
        /// A global stack is available by default in <see cref="VolumeManager"/> but you can also create your own using
        /// <see cref="VolumeManager.CreateStack"/> if you need to update the manager with specific
        /// settings and store the results for later use.
        /// </remarks>
        /// <param name="camera">The <see cref="Camera"/> to use for destroying the <see cref="VolumeStack"/>.</param>
        /// <param name="cameraData">The <see cref="UniversalAdditionalCameraData"/> component on the camera that should be used.</param>
        public static void DestroyVolumeStack(this Camera camera, UniversalAdditionalCameraData cameraData)
        {
            if (cameraData == null || cameraData.volumeStack == null)
                return;

            cameraData.volumeStack = null;
        }

        /// <summary>
        /// Returns the mask and trigger assigned for volumes on the camera.
        /// </summary>
        /// <remarks>
        /// Each camera has a volume layer mask, containing the selected scene-layers affecting this camera, and
        /// a Transform that acts as a trigger for volume blending. This method returns those settings for the given camera.
        /// </remarks>
        /// <param name="camera">The <see cref="Camera"/> to use for getting the <see cref="LayerMask"/> and trigger.</param>
        /// <param name="cameraData">The <see cref="UniversalAdditionalCameraData"/> component on the camera that should be used.</param>
        /// <param name="layerMask">The resulting <see cref="LayerMask"/> used on this <see cref="Camera"/>.</param>
        /// <param name="trigger">The resulting <see cref="Transform"/> trigger used on this <see cref="Camera"/>.</param>
        internal static void GetVolumeLayerMaskAndTrigger(this Camera camera, UniversalAdditionalCameraData cameraData, out LayerMask layerMask, out Transform trigger)
        {
            // Default values when there's no additional camera data available
            layerMask = 1; // "Default"
            trigger = camera.transform;

            if (cameraData != null)
            {
                layerMask = cameraData.volumeLayerMask;
                trigger = (cameraData.volumeTrigger != null) ? cameraData.volumeTrigger : trigger;
            }
            else if (camera.cameraType == CameraType.SceneView)
            {
                // Try to mirror the MainCamera volume layer mask for the scene view - do not mirror the target
                var mainCamera = Camera.main;
                UniversalAdditionalCameraData mainAdditionalCameraData = null;

                if (mainCamera != null && mainCamera.TryGetComponent(out mainAdditionalCameraData))
                {
                    layerMask = mainAdditionalCameraData.volumeLayerMask;
                }

                trigger = (mainAdditionalCameraData != null && mainAdditionalCameraData.volumeTrigger != null) ? mainAdditionalCameraData.volumeTrigger : trigger;
            }
        }
    }

    static class CameraTypeUtility
    {
        static string[] s_CameraTypeNames = Enum.GetNames(typeof(CameraRenderType)).ToArray();

        public static string GetName(this CameraRenderType type)
        {
            int typeInt = (int)type;
            if (typeInt < 0 || typeInt >= s_CameraTypeNames.Length)
                typeInt = (int)CameraRenderType.Base;
            return s_CameraTypeNames[typeInt];
        }
    }

    /// <summary>
    /// Class containing various additional camera data used by URP.
    /// </summary>
    /// <remarks>
    /// The Universal Render Pipeline (URP) adds this class to every camera.
    /// It contains URP-specific data and methods that the render pipeline uses.
    /// </remarks>
    /// <example>
    /// <para>This sample code shows how settings can be retrieved and logged in runtime.</para>
    /// <code>
    /// using UnityEngine;
    /// using UnityEngine.Rendering.Universal;
    ///
    /// public class LogCameraInformation : MonoBehaviour
    /// {
    ///     // Reference to the camera component
    ///     [SerializeField] private Camera cameraToInspect;
    ///
    ///     // Start is called once before the first execution of Update after the MonoBehaviour is created
    ///     private void Start()
    ///     {
    ///         // Make sure there's a reference to the camera. If not, log an error and return.
    ///         if (cameraToInspect == null)
    ///         {
    ///             Debug.LogError("LogCameraInformation.Start(): cameraToInspect has not been assigned.");
    ///             return;
    ///         }
    ///
    ///         // Get the UniversalAdditionalCameraData component from the camera
    ///         UniversalAdditionalCameraData additionalCameraData = cameraToInspect.GetUniversalAdditionalCameraData();
    ///
    ///         // Print to the console some of the settings contained in the UniversalAdditionalCameraData component.
    ///         PrintCameraInformation(ref additionalCameraData);
    ///     }
    ///
    ///     // Print to the console some of the available settings in the UniversalAdditionalCameraData component.
    ///     private static void PrintCameraInformation(ref UniversalAdditionalCameraData cameraData)
    ///     {
    ///         string cameraInformation = $"Camera \"{cameraData.name}\" information:\n" +
    ///                                    $"- renderShadows: {cameraData.renderShadows}\n" +
    ///                                    $"- requiresDepthOption: {cameraData.requiresDepthOption}\n" +
    ///                                    $"- requiresColorOption: {cameraData.requiresColorOption}\n" +
    ///                                    $"- renderType: {cameraData.renderType}\n" +
    ///                                    $"- cameraStack.Count: {cameraData.cameraStack.Count}\n" +
    ///                                    $"- clearDepth: {cameraData.clearDepth}\n" +
    ///                                    $"- requiresDepthTexture: {cameraData.requiresDepthTexture}\n" +
    ///                                    $"- requiresColorTexture: {cameraData.requiresColorTexture}\n" +
    ///                                    $"- volumeLayerMask: {cameraData.volumeLayerMask}\n" +
    ///                                    $"- requiresVolumeFrameworkUpdate: {cameraData.requiresVolumeFrameworkUpdate}\n" +
    ///                                    $"- renderPostProcessing: {cameraData.renderPostProcessing}\n" +
    ///                                    $"- antialiasing: {cameraData.antialiasing}\n" +
    ///                                    $"- antialiasingQuality: {cameraData.antialiasingQuality}\n" +
    ///                                    $"- taaSettings:\n" +
    ///                                    $"  - quality: {cameraData.taaSettings.quality}\n" +
    ///                                    $"  - baseBlendFactor: {cameraData.taaSettings.baseBlendFactor}\n" +
    ///                                    $"  - jitterScale: {cameraData.taaSettings.jitterScale}\n" +
    ///                                    $"  - mipBias: {cameraData.taaSettings.mipBias}\n" +
    ///                                    $"  - varianceClampScale: {cameraData.taaSettings.varianceClampScale}\n" +
    ///                                    $"  - contrastAdaptiveSharpening: {cameraData.taaSettings.contrastAdaptiveSharpening}\n" +
    ///                                    $"- resetHistory: {cameraData.resetHistory}\n" +
    ///                                    $"- stopNaN: {cameraData.stopNaN}\n" +
    ///                                    $"- dithering: {cameraData.dithering}\n" +
    ///                                    $"- allowXRRendering: {cameraData.allowXRRendering}\n" +
    ///                                    $"- useScreenCoordOverride: {cameraData.useScreenCoordOverride}\n" +
    ///                                    $"- screenCoordScaleBias: {cameraData.screenCoordScaleBias}\n" +
    ///                                    $"- allowHDROutput: {cameraData.allowHDROutput}\n";
    ///         Debug.Log(cameraInformation);
    ///     }
    /// }
    /// </code>
    /// </example>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways] // NOTE: This is required to get calls to OnDestroy() always. Graphics resources are released in OnDestroy().
    [URPHelpURL("universal-additional-camera-data")]
    public partial class UniversalAdditionalCameraData : MonoBehaviour, ISerializationCallbackReceiver, IAdditionalData
    {
        const string k_GizmoPath = "Packages/com.unity.render-pipelines.universal/Editor/Gizmos/";
        const string k_BaseCameraGizmoPath = k_GizmoPath + "Camera_Base.png";
        const string k_OverlayCameraGizmoPath = k_GizmoPath + "Camera_Base.png";
        const string k_PostProcessingGizmoPath = k_GizmoPath + "Camera_PostProcessing.png";

        [FormerlySerializedAs("renderShadows"), SerializeField]
        bool m_RenderShadows = true;

        [SerializeField]
        CameraOverrideOption m_RequiresDepthTextureOption = CameraOverrideOption.UsePipelineSettings;

        [SerializeField]
        CameraOverrideOption m_RequiresOpaqueTextureOption = CameraOverrideOption.UsePipelineSettings;

        [SerializeField] CameraRenderType m_CameraType = CameraRenderType.Base;
        [SerializeField] List<Camera> m_Cameras = new List<Camera>();
        [SerializeField] int m_RendererIndex = -1;

        [SerializeField] LayerMask m_VolumeLayerMask = 1; // "Default"
        [SerializeField] Transform m_VolumeTrigger = null;
        [SerializeField] VolumeFrameworkUpdateMode m_VolumeFrameworkUpdateModeOption = VolumeFrameworkUpdateMode.UsePipelineSettings;

        [SerializeField] bool m_RenderPostProcessing = false;
        [SerializeField] AntialiasingMode m_Antialiasing = AntialiasingMode.None;
        [SerializeField] AntialiasingQuality m_AntialiasingQuality = AntialiasingQuality.High;
        [SerializeField] bool m_StopNaN = false;
        [SerializeField] bool m_Dithering = false;
        [SerializeField] bool m_ClearDepth = true;
        [SerializeField] bool m_AllowXRRendering = true;
        [SerializeField] bool m_AllowHDROutput = true;

        [SerializeField] bool m_UseScreenCoordOverride;
        [SerializeField] Vector4 m_ScreenSizeOverride;
        [SerializeField] Vector4 m_ScreenCoordScaleBias;

        [NonSerialized] Camera m_Camera;
        // Deprecated:
        [FormerlySerializedAs("requiresDepthTexture"), SerializeField]
        bool m_RequiresDepthTexture = false;

        [FormerlySerializedAs("requiresColorTexture"), SerializeField]
        bool m_RequiresColorTexture = false;

        // These persist over multiple frames
        [NonSerialized] MotionVectorsPersistentData m_MotionVectorsPersistentData = new MotionVectorsPersistentData();

        // The URP camera history texture manager. Persistent per camera textures.
        [NonSerialized] internal UniversalCameraHistory m_History = new UniversalCameraHistory();

        [SerializeField] internal TemporalAA.Settings m_TaaSettings = TemporalAA.Settings.Create();
        
        static UniversalAdditionalCameraData s_DefaultAdditionalCameraData = null;
        internal static UniversalAdditionalCameraData defaultAdditionalCameraData
        {
            get
            {
                if (s_DefaultAdditionalCameraData == null)
                    s_DefaultAdditionalCameraData = new UniversalAdditionalCameraData();

                return s_DefaultAdditionalCameraData;
            }
        }

#if UNITY_EDITOR
        internal new Camera camera
#else
        internal Camera camera
#endif
        {
            get
            {
                if (!m_Camera)
                {
                    gameObject.TryGetComponent<Camera>(out m_Camera);
                }
                return m_Camera;
            }
        }

        void Start()
        {
            // Need to ensure correct behavoiur for overlay cameras settings their clear flag to nothing.
            // This can't be done in the upgrade since the camera component can't be retrieved in the deserialization phase.
            // OnValidate ensure future cameras won't have this issue.
            if (m_CameraType == CameraRenderType.Overlay)
                camera.clearFlags = CameraClearFlags.Nothing;
        }


        /// <summary>
        /// Controls if this camera should render shadows.
        /// </summary>
        public bool renderShadows
        {
            get => m_RenderShadows;
            set => m_RenderShadows = value;
        }

        /// <summary>
        /// Controls if a camera should render depth.
        /// The depth is available to be bound in shaders as _CameraDepthTexture.
        /// <see cref="CameraOverrideOption"/>
        /// </summary>
        public CameraOverrideOption requiresDepthOption
        {
            get => m_RequiresDepthTextureOption;
            set => m_RequiresDepthTextureOption = value;
        }

        /// <summary>
        /// Controls if a camera should copy the color contents of a camera after rendering opaques.
        /// The color texture is available to be bound in shaders as _CameraOpaqueTexture.
        /// </summary>
        public CameraOverrideOption requiresColorOption
        {
            get => m_RequiresOpaqueTextureOption;
            set => m_RequiresOpaqueTextureOption = value;
        }

        /// <summary>
        /// Returns the camera renderType.
        /// <see cref="CameraRenderType"/>.
        /// </summary>
        public CameraRenderType renderType
        {
            get => m_CameraType;
            set => m_CameraType = value;
        }

        /// <summary>
        /// Returns the stacked Overlay cameras.
        /// Only Base cameras can have such a stack, it will return null otherwise.
        /// <see cref="CameraRenderType"/>.
        /// </summary>
        public List<Camera> cameraStack
        {
            get
            {
                if (renderType != CameraRenderType.Base)
                {
                    var camera = gameObject.GetComponent<Camera>();
                    Debug.LogWarning(string.Format("{0}: This camera is of {1} type. Only Base cameras can have a camera stack.", camera.name, renderType));
                    return null;
                }

                if (!scriptableRenderer.SupportsCameraStackingType(CameraRenderType.Base))
                {
                    var camera = gameObject.GetComponent<Camera>();
                    Debug.LogWarning(string.Format("{0}: This camera has a ScriptableRenderer that doesn't support camera stacking. Camera stack is null.", camera.name));
                    return null;
                }
                return m_Cameras;
            }
        }

        internal void UpdateCameraStack()
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "Update camera stack");
#endif
            int prev = m_Cameras.Count;
            m_Cameras.RemoveAll(cam => cam == null);
            int curr = m_Cameras.Count;
            int removedCamsCount = prev - curr;
            if (removedCamsCount != 0)
            {
                Debug.LogWarning(name + ": " + removedCamsCount + " camera overlay" + (removedCamsCount > 1 ? "s" : "") + " no longer exists and will be removed from the camera stack.");
            }
        }

        /// <summary>
        /// If true, this camera will clear depth value before rendering. Only valid for Overlay cameras.
        /// </summary>
        public bool clearDepth
        {
            get => m_ClearDepth;
        }

        /// <summary>
        /// Returns true if this camera needs to render depth information in a texture.
        /// If enabled, depth texture is available to be bound and read from shaders as _CameraDepthTexture after rendering skybox.
        /// </summary>
        public bool requiresDepthTexture
        {
            get
            {
                if (m_RequiresDepthTextureOption == CameraOverrideOption.UsePipelineSettings)
                {
                    return UniversalRenderPipeline.asset.supportsCameraDepthTexture;
                }
                else
                {
                    return m_RequiresDepthTextureOption == CameraOverrideOption.On;
                }
            }
            set { m_RequiresDepthTextureOption = (value) ? CameraOverrideOption.On : CameraOverrideOption.Off; }
        }

        /// <summary>
        /// Returns true if this camera requires to color information in a texture.
        /// If enabled, color texture is available to be bound and read from shaders as _CameraOpaqueTexture after rendering skybox.
        /// </summary>
        public bool requiresColorTexture
        {
            get
            {
                if (m_RequiresOpaqueTextureOption == CameraOverrideOption.UsePipelineSettings)
                {
                    return UniversalRenderPipeline.asset.supportsCameraOpaqueTexture;
                }
                else
                {
                    return m_RequiresOpaqueTextureOption == CameraOverrideOption.On;
                }
            }
            set { m_RequiresOpaqueTextureOption = (value) ? CameraOverrideOption.On : CameraOverrideOption.Off; }
        }

        /// <summary>
        /// Returns the <see cref="ScriptableRenderer"/> that is used to render this camera.
        /// </summary>
        public ScriptableRenderer scriptableRenderer
        {
            get
            {
                if (UniversalRenderPipeline.asset is null)
                    return null;
                if (!UniversalRenderPipeline.asset.ValidateRendererData(m_RendererIndex))
                {
                    int defaultIndex = UniversalRenderPipeline.asset.m_DefaultRendererIndex;
                    Debug.LogWarning(
                        $"Renderer at <b>index {m_RendererIndex.ToString()}</b> is missing for camera <b>{camera.name}</b>, falling back to Default Renderer. <b>{UniversalRenderPipeline.asset.m_RendererDataList[defaultIndex].name}</b>",
                        UniversalRenderPipeline.asset);
                    return UniversalRenderPipeline.asset.GetRenderer(defaultIndex);
                }
                return UniversalRenderPipeline.asset.GetRenderer(m_RendererIndex);
            }
        }

        /// <summary>
        /// Use this to set this Camera's current <see cref="ScriptableRenderer"/> to one listed on the Render Pipeline Asset. Takes an index that maps to the list on the Render Pipeline Asset.
        /// </summary>
        /// <param name="index">The index that maps to the RendererData list on the currently assigned Render Pipeline Asset</param>
        public void SetRenderer(int index)
        {
            m_RendererIndex = index;
        }

        /// <summary>
        /// Returns the selected scene-layers affecting this camera.
        /// </summary>
        public LayerMask volumeLayerMask
        {
            get => m_VolumeLayerMask;
            set => m_VolumeLayerMask = value;
        }

        /// <summary>
        /// Returns the Transform that acts as a trigger for Volume blending.
        /// </summary>
        public Transform volumeTrigger
        {
            get => m_VolumeTrigger;
            set => m_VolumeTrigger = value;
        }

        /// <summary>
        /// Returns the selected mode for Volume Frame Updates.
        /// </summary>
        internal VolumeFrameworkUpdateMode volumeFrameworkUpdateMode
        {
            get => m_VolumeFrameworkUpdateModeOption;
            set => m_VolumeFrameworkUpdateModeOption = value;
        }

        /// <summary>
        /// Returns true if this camera requires the volume framework to be updated every frame.
        /// </summary>
        public bool requiresVolumeFrameworkUpdate
        {
            get
            {
                if (m_VolumeFrameworkUpdateModeOption == VolumeFrameworkUpdateMode.UsePipelineSettings)
                {
                    return UniversalRenderPipeline.asset.volumeFrameworkUpdateMode != VolumeFrameworkUpdateMode.ViaScripting;
                }

                return m_VolumeFrameworkUpdateModeOption == VolumeFrameworkUpdateMode.EveryFrame;
            }
        }

        /// <summary>
        /// Container for volume stacks in order to reuse stacks and avoid
        /// creating new ones every time a new camera is instantiated.
        /// </summary>
        private static List<VolumeStack> s_CachedVolumeStacks;

        /// <summary>
        /// Returns the current volume stack used by this camera.
        /// </summary>
        public VolumeStack volumeStack
        {
            get => m_VolumeStack;
            set
            {
                // If the volume stack is being removed,
                // add it back to the list so it can be reused later
                if (value == null && m_VolumeStack != null && m_VolumeStack.isValid)
                {
                    if (s_CachedVolumeStacks == null)
                        s_CachedVolumeStacks = new List<VolumeStack>(4);

                    s_CachedVolumeStacks.Add(m_VolumeStack);
                }

                m_VolumeStack = value;
            }
        }
        VolumeStack m_VolumeStack = null;

        /// <summary>
        /// Tries to retrieve a volume stack from the container
        /// and creates a new one if that fails.
        /// </summary>
        internal void GetOrCreateVolumeStack()
        {
            // Try first to reuse a volume stack
            if (s_CachedVolumeStacks != null && s_CachedVolumeStacks.Count > 0)
            {
                int index = s_CachedVolumeStacks.Count - 1;
                var stack = s_CachedVolumeStacks[index];
                s_CachedVolumeStacks.RemoveAt(index);
                if (stack.isValid)
                    volumeStack = stack;
            }

            // Create a new stack if was not possible to reuse an old one
            if (volumeStack == null)
                volumeStack = VolumeManager.instance.CreateStack();
        }

        /// <summary>
        /// Returns true if this camera should render post-processing.
        /// </summary>
        public bool renderPostProcessing
        {
            get => m_RenderPostProcessing;
            set => m_RenderPostProcessing = value;
        }

        /// <summary>
        /// Returns the current anti-aliasing mode used by this camera.
        /// <see cref="AntialiasingMode"/>.
        /// </summary>
        public AntialiasingMode antialiasing
        {
            get => m_Antialiasing;
            set => m_Antialiasing = value;
        }

        /// <summary>
        /// Returns the current anti-aliasing quality used by this camera.
        /// <see cref="antialiasingQuality"/>.
        /// </summary>
        public AntialiasingQuality antialiasingQuality
        {
            get => m_AntialiasingQuality;
            set => m_AntialiasingQuality = value;
        }

        /// <summary>
        /// Returns the current temporal anti-aliasing settings used by this camera.
        /// </summary>
        public ref TemporalAA.Settings taaSettings
        {
            get { return ref m_TaaSettings; }
        }

        /// <summary>
        /// Returns the URP camera history texture read access.
        /// Used to register requests and to read the existing history textures by external systems.
        /// </summary>
        public ICameraHistoryReadAccess history => m_History;

        // Returns the URP camera history texture manager with full access for internal systems.
        // NOTE: Only the pipeline should write/render history textures. Should be kept internal.
        //
        // The history is camera specific. The UniversalAdditionalCameraData is the URP specific camera (data).
        // Therefore it owns the UniversalCameraHistory. The history should follow the camera lifetime.
        internal UniversalCameraHistory historyManager => m_History;

        /// <summary>
        /// Motion data that persists over a frame.
        /// </summary>
        internal MotionVectorsPersistentData motionVectorsPersistentData => m_MotionVectorsPersistentData;

        /// <summary>
        /// Reset post-process history for Temporal Anti-aliasing (TAA).
        /// </summary>
        public bool resetHistory
        {
            get => m_TaaSettings.resetHistoryFrames != 0;
            set
            {
                m_TaaSettings.resetHistoryFrames += value ? 1 : 0;
                m_MotionVectorsPersistentData.Reset();

                // Reset the jitter period for consistent test results.
                // Not technically history, but this is here to avoid adding testing only public API.
                m_TaaSettings.jitterFrameCountOffset = -Time.frameCount;
            }
        }

        /// <summary>
        /// Returns true if this camera should automatically replace NaN/Inf in shaders by a black pixel to avoid breaking some effects.
        /// </summary>
        public bool stopNaN
        {
            get => m_StopNaN;
            set => m_StopNaN = value;
        }

        /// <summary>
        /// Returns true if this camera applies 8-bit dithering to the final render to reduce color banding
        /// </summary>
        public bool dithering
        {
            get => m_Dithering;
            set => m_Dithering = value;
        }

        /// <summary>
        /// Returns true if this camera allows render in XR.
        /// </summary>
        public bool allowXRRendering
        {
            get => m_AllowXRRendering;
            set => m_AllowXRRendering = value;
        }

        /// <summary>
        /// Returns true if the camera uses Screen Coordinates Override.
        /// </summary>
        public bool useScreenCoordOverride
        {
            get => m_UseScreenCoordOverride;
            set => m_UseScreenCoordOverride = value;
        }

        /// <summary>
        /// Screen size used when Screen Coordinates Override is active.
        /// </summary>
        public Vector4 screenSizeOverride
        {
            get => m_ScreenSizeOverride;
            set => m_ScreenSizeOverride = value;
        }

        /// <summary>
        /// Transform applied to screen coordinates when Screen Coordinates Override is active.
        /// </summary>
        public Vector4 screenCoordScaleBias
        {
            get => m_ScreenCoordScaleBias;
            set => m_ScreenCoordScaleBias = value;
        }

        /// <summary>
        /// Returns true if this camera allows outputting to HDR displays.
        /// </summary>
        public bool allowHDROutput
        {
            get => m_AllowHDROutput;
            set => m_AllowHDROutput = value;
        }

        /// <inheritdoc/>
        public void OnValidate()
        {
            if (m_CameraType == CameraRenderType.Overlay && m_Camera != null)
            {
                m_Camera.clearFlags = CameraClearFlags.Nothing;
            }
        }

        /// <inheritdoc/>
        public void OnDrawGizmos()
        {
            string gizmoName = "";
            Color tint = Color.white;

            if (m_CameraType == CameraRenderType.Base)
            {
                gizmoName = k_BaseCameraGizmoPath;
            }
            else if (m_CameraType == CameraRenderType.Overlay)
            {
                gizmoName = k_OverlayCameraGizmoPath;
            }

#if UNITY_2019_2_OR_NEWER
#if UNITY_EDITOR
            if (Selection.activeObject == gameObject)
            {
                // Get the preferences selection color
                tint = SceneView.selectedOutlineColor;
            }
#endif
            if (!string.IsNullOrEmpty(gizmoName))
            {
                Gizmos.DrawIcon(transform.position, gizmoName, true, tint);
            }

            if (renderPostProcessing)
            {
                Gizmos.DrawIcon(transform.position, k_PostProcessingGizmoPath, true, tint);
            }
#else
            if (renderPostProcessing)
            {
                Gizmos.DrawIcon(transform.position, k_PostProcessingGizmoPath);
            }
            Gizmos.DrawIcon(transform.position, gizmoName);
#endif
        }

        /// <inheritdoc/>
        public void OnDestroy()
        {
            //You cannot call scriptableRenderer here. If you where not in URP, this will actually create the renderer.
            //This can occurs in cross pipeline but also on Dedicated Server where the gfx device do not run. (UUM-75237)
            //Use GetRawRenderer() instead.

            m_Camera.DestroyVolumeStack(this);
            if (camera.cameraType != CameraType.SceneView)
                GetRawRenderer()?.ReleaseRenderTargets();
            m_History?.Dispose();
            m_History = null;
        }


        ScriptableRenderer GetRawRenderer()
        {
            if (UniversalRenderPipeline.asset is null)
                return null;

            ReadOnlySpan<ScriptableRenderer> renderers = UniversalRenderPipeline.asset.renderers;
            if (renderers == null || renderers.IsEmpty)
                return null;

            if (m_RendererIndex >= renderers.Length || m_RendererIndex < 0)
                return null;

            return renderers[m_RendererIndex];
        }
        
        enum Version
        {
            Initial = 0,
            DepthAndOpaqueTextureOptions = 2,
            
            Count
        }
        
        [SerializeField] Version m_Version = Version.Count;

        // This piece of code is needed because some objects could have been created before existence of Version enum
        /// <summary>OnBeforeSerialize needed to handle migration before the versioning system was in place.</summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (m_Version == Version.Count) // serializing a newly created object
                m_Version = Version.Count - 1; // mark as up to date
        }

        /// <summary>OnAfterDeserialize needed to handle migration before the versioning system was in place.</summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (m_Version == Version.Count) // deserializing and object without version
                m_Version = Version.Initial; // reset to run the migration
            
            if (m_Version < Version.DepthAndOpaqueTextureOptions)
            {
                m_RequiresDepthTextureOption = (m_RequiresDepthTexture) ? CameraOverrideOption.On : CameraOverrideOption.Off;
                m_RequiresOpaqueTextureOption = (m_RequiresColorTexture) ? CameraOverrideOption.On : CameraOverrideOption.Off;
                m_Version = Version.DepthAndOpaqueTextureOptions;
            }
        }
    }
}
