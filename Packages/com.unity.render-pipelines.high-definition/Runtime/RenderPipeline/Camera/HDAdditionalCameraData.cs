using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Holds the physical settings set on cameras.
    /// </summary>
    [Serializable]
    [Obsolete("Properties have been migrated to Camera class", false)]
    public struct HDPhysicalCamera
    {
        /// <summary>
        /// The minimum allowed aperture.
        /// </summary>
        public const float kMinAperture = 0.7f;

        /// <summary>
        /// The maximum allowed aperture.
        /// </summary>
        public const float kMaxAperture = 32f;

        /// <summary>
        /// The minimum blade count for the aperture diaphragm.
        /// </summary>
        public const int kMinBladeCount = 3;

        /// <summary>
        /// The maximum blade count for the aperture diaphragm.
        /// </summary>
        public const int kMaxBladeCount = 11;

        // Camera body
        [SerializeField] [Min(1f)] int m_Iso;
        [SerializeField] [Min(0f)] float m_ShutterSpeed;

        // Lens
        // Note: focalLength is already defined in the regular camera component
        [SerializeField] [Range(Camera.kMinAperture, Camera.kMaxAperture)] float m_Aperture;
        [SerializeField] [Min(0.1f)] float m_FocusDistance;

        // Aperture shape
        [SerializeField] [Range(Camera.kMinBladeCount, Camera.kMaxBladeCount)] int m_BladeCount;
        [SerializeField] Vector2 m_Curvature;
        [SerializeField] [Range(0f, 1f)] float m_BarrelClipping;
        [SerializeField] [Range(-1f, 1f)] float m_Anamorphism;

        /// <summary>
        /// The focus distance of the lens. The Depth of Field Volume override uses this value if you set focusDistanceMode to FocusDistanceMode.Camera.
        /// </summary>
        public float focusDistance
        {
            get => m_FocusDistance;
            set => m_FocusDistance = Mathf.Max(value, 0.1f);
        }

        /// <summary>
        /// The sensor sensitivity (ISO).
        /// </summary>
        public int iso
        {
            get => m_Iso;
            set => m_Iso = Mathf.Max(value, 1);
        }

        /// <summary>
        /// The exposure time, in second.
        /// </summary>
        public float shutterSpeed
        {
            get => m_ShutterSpeed;
            set => m_ShutterSpeed = Mathf.Max(value, 0f);
        }

        /// <summary>
        /// The aperture number, in f-stop.
        /// </summary>
        public float aperture
        {
            get => m_Aperture;
            set => m_Aperture = Mathf.Clamp(value, Camera.kMinAperture, Camera.kMaxAperture);
        }

        /// <summary>
        /// The number of diaphragm blades.
        /// </summary>
        public int bladeCount
        {
            get => m_BladeCount;
            set => m_BladeCount = Mathf.Clamp(value, Camera.kMinBladeCount, Camera.kMaxBladeCount);
        }

        /// <summary>
        /// Maps an aperture range to blade curvature.
        /// </summary>
        public Vector2 curvature
        {
            get => m_Curvature;
            set
            {
                m_Curvature.x = Mathf.Max(value.x, Camera.kMinAperture);
                m_Curvature.y = Mathf.Min(value.y, Camera.kMaxAperture);
            }
        }

        /// <summary>
        /// The strength of the "cat eye" effect on bokeh (optical vignetting).
        /// </summary>
        public float barrelClipping
        {
            get => m_BarrelClipping;
            set => m_BarrelClipping = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Stretches the sensor to simulate an anamorphic look. Positive values distort the Camera
        /// vertically, negative will distort the Camera horizontally.
        /// </summary>
        public float anamorphism
        {
            get => m_Anamorphism;
            set => m_Anamorphism = Mathf.Clamp(value, -1f, 1f);
        }

        /// <summary>
        /// Copies the settings of this instance to another instance.
        /// </summary>
        /// <param name="c">The instance to copy the settings to.</param>
        [Obsolete("The CopyTo method is obsolete and does not work anymore. Use the assignement operator instead to get a copy of the HDPhysicalCamera parameters.", true)]
        public void CopyTo(HDPhysicalCamera c)
        {
        }

        /// <summary>
        /// A set of default physical camera parameters.
        /// </summary>
        /// <returns>Returns a set of default physical camera parameters.</returns>
        public static HDPhysicalCamera GetDefaults()
        {
            HDPhysicalCamera val = new HDPhysicalCamera();
            val.iso = 200;
            val.shutterSpeed = 1f / 200f;
            val.aperture = 16;
            val.focusDistance = 10;
            val.bladeCount = 5;
            val.curvature = new Vector2(2f, 11f);
            val.barrelClipping = 0.25f;
            val.anamorphism = 0;
            return val;
        }
    }

    /// <summary>
    /// Additional component that holds HDRP specific parameters for Cameras.
    /// </summary>
    [HDRPHelpURLAttribute("hdrp-camera-component-reference")]
    [AddComponentMenu("")] // Hide in menu
    [DisallowMultipleComponent, ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public partial class HDAdditionalCameraData : MonoBehaviour, IFrameSettingsHistoryContainer, IAdditionalData
    {
        /// <summary>
        /// How the camera should handle vertically flipping the frame at the end of rendering.
        /// </summary>
        public enum FlipYMode
        {
            /// <summary>Handle flip automatically.</summary>
            Automatic,
            /// <summary>For vertical flip.</summary>
            ForceFlipY
        }

        /// <summary>
        /// Type of buffers that can be accessed for this camera.
        /// </summary>
        [Flags]
        public enum BufferAccessType
        {
            /// <summary>Depth buffer.</summary>
            Depth = 1,
            /// <summary>Normal buffer.</summary>
            Normal = 1 << 1,
            /// <summary>Color buffer.</summary>
            Color = 1 << 2
        }

        /// <summary>
        /// Structure used to access graphics buffers for this camera.
        /// </summary>
        public struct BufferAccess
        {
            internal BufferAccessType bufferAccess;

            internal void Reset()
            {
                bufferAccess = 0;
            }

            /// <summary>
            /// Request access to a list of buffer in the form of a bitfield.
            /// </summary>
            /// <param name="flags">List of buffers that need to be accessed.</param>
            public void RequestAccess(BufferAccessType flags)
            {
                bufferAccess |= flags;
            }
        }

        // The light culling use standard projection matrices (non-oblique)
        // If the user overrides the projection matrix with an oblique one
        // He must also provide a callback to get the equivalent non oblique for the culling
        /// <summary>
        /// Returns the non oblique projection matrix for a particular camera.
        /// </summary>
        /// <param name="camera">Requested camera.</param>
        /// <returns>The non oblique projection matrix for a particular camera.</returns>
        public delegate Matrix4x4 NonObliqueProjectionGetter(Camera camera);

        [ExcludeCopy]
        Camera m_Camera;

        /// <summary>
        /// Clear mode for the camera background.
        /// </summary>
        public enum ClearColorMode
        {
            /// <summary>Clear the background with the sky.</summary>
            Sky,
            /// <summary>Clear the background with a constant color.</summary>
            Color,
            /// <summary>Don't clear the background.</summary>
            None
        };

        /// <summary>
        /// Anti-aliasing mode.
        /// </summary>
        public enum AntialiasingMode
        {
            /// <summary>No Anti-aliasing.</summary>
            [InspectorName("No Anti-aliasing")]
            None,
            /// <summary>FXAA.</summary>
            [InspectorName("Fast Approximate Anti-aliasing (FXAA)")]
            FastApproximateAntialiasing,
            /// <summary>Temporal anti-aliasing.</summary>
            [InspectorName("Temporal Anti-aliasing (TAA)")]
            TemporalAntialiasing,
            /// <summary>SMAA.</summary>
            [InspectorName("Subpixel Morphological Anti-aliasing (SMAA)")]
            SubpixelMorphologicalAntiAliasing
        }

        /// <summary>
        /// SMAA quality level.
        /// </summary>
        public enum SMAAQualityLevel
        {
            /// <summary>Low quality.</summary>
            Low,
            /// <summary>Medium quality.</summary>
            Medium,
            /// <summary>High quality.</summary>
            High
        }

        /// <summary>
        /// TAA quality level.
        /// </summary>
        public enum TAAQualityLevel
        {
            /// <summary>Low quality.</summary>
            Low,
            /// <summary>Medium quality.</summary>
            Medium,
            /// <summary>High quality.</summary>
            High
        }

        /// <summary>
        /// TAA Sharpen mode.
        /// </summary>
        public enum TAASharpenMode
        {
            /// <summary>Low quality.</summary>
            LowQuality,
            /// <summary>Sharpen with a separate pass after TAA.</summary>
            PostSharpen,
            /// <summary>Run a Contrast Adaptive Sharpening pass after TAA.</summary>
            ContrastAdaptiveSharpening
        }

        /// <summary>Clear mode for the camera background.</summary>
        public ClearColorMode clearColorMode = ClearColorMode.Sky;
        /// <summary>HDR color used for clearing the camera background.</summary>
        [ColorUsage(true, true)]
        public Color backgroundColorHDR = new Color(0.025f, 0.07f, 0.19f, 0.0f);
        /// <summary>Clear depth as well as color.</summary>
        public bool clearDepth = true;

        /// <summary>Layer mask used to select which volumes will influence this camera.</summary>
        [Tooltip("LayerMask HDRP uses for Volume interpolation for this Camera.")]
        public LayerMask volumeLayerMask = 1;

        /// <summary>Optional transform override for the position where volumes are interpolated.</summary>
        public Transform volumeAnchorOverride;

        /// <summary>Anti-aliasing mode.</summary>
        public AntialiasingMode antialiasing = AntialiasingMode.None;
        /// <summary>Quality of the anti-aliasing when using SMAA.</summary>
        public SMAAQualityLevel SMAAQuality = SMAAQualityLevel.High;
        /// <summary>Use dithering to filter out minor banding.</summary>
        public bool dithering = false;
        /// <summary>Use a pass to eliminate NaNs contained in the color buffer before post-processing.</summary>
        public bool stopNaNs = false;

        /// <summary>Strength of the sharpening component of temporal anti-aliasing.</summary>
        [Range(0, 2)]
        public float taaSharpenStrength = 0.5f;

        /// <summary>Quality of the anti-aliasing when using TAA.</summary>
        public TAAQualityLevel TAAQuality = TAAQualityLevel.Medium;

        /// <summary>How is the sharpening run sharpening.</summary>
        public TAASharpenMode taaSharpenMode = TAASharpenMode.LowQuality;

        /// <summary>How much to reduce the ringing from the TAA post-process sharpening. Note that some ringing might be visually desirable and that any value different than 0 will incur into a small additional cost.</summary>
        [Range(0, 1)]
        public float taaRingingReduction = 0.0f;

        /// <summary>Strength of the sharpening of the history sampled for TAA.</summary>
        [Range(0, 1)]
        public float taaHistorySharpening = 0.35f;

        /// <summary>Drive the anti-flicker mechanism. With high values flickering might be reduced, but it can lead to more ghosting or disocclusion artifacts.</summary>
        [Range(0.0f, 1.0f)]
        public float taaAntiFlicker = 0.5f;

        /// <summary>Larger is this value, more likely history will be rejected when current and reprojected history motion vector differ by a substantial amount.
        /// Larger values can decrease ghosting but will also reintroduce aliasing on the aforementioned cases.</summary>
        [Range(0.0f, 1.0f)]
        public float taaMotionVectorRejection = 0.0f;

        /// <summary>When enabled, ringing artifacts (dark or strangely saturated edges) caused by history sharpening will be improved. This comes at a potential loss of sharpness upon motion.</summary>
        public bool taaAntiHistoryRinging = false;

        /// <summary> Determines how much the history buffer is blended together with current frame result. Higher values means more history contribution. </summary>
        [Range(HDRenderPipeline.TAABaseBlendFactorMin, HDRenderPipeline.TAABaseBlendFactorMax)]
        public float taaBaseBlendFactor = 0.875f;

        /// <summary> Scale to apply to the jittering applied when TAA is enabled. </summary>
        [Range(0.1f, 1.0f)]
        public float taaJitterScale = 1.0f;

        /// <summary>Physical camera parameters.</summary>
        [ValueCopy] // reference should not be same. only content.
        [Obsolete("Physical camera properties have been migrated to Camera.", false)]
#pragma warning disable CS0618
        public HDPhysicalCamera physicalParameters = HDPhysicalCamera.GetDefaults();
#pragma warning restore CS0618

        /// <summary>Vertical flip mode.</summary>
        public FlipYMode flipYMode;

        /// <summary>Enable XR rendering.</summary>
        public bool xrRendering = true;

        /// <summary>Skips rendering settings to directly render in fullscreen (Useful for video).</summary>
        [Tooltip("Skips rendering settings to directly render in fullscreen (Useful for video).")]
        public bool fullscreenPassthrough = false;

        /// <summary>Allows dynamic resolution on buffers linked to this camera.</summary>
        [Tooltip("Allows dynamic resolution on buffers linked to this camera.")]
        public bool allowDynamicResolution = false;

        /// <summary>Allows you to override the default frame settings for this camera.</summary>
        [Tooltip("Allows you to override the default settings for this camera.")]
        public bool customRenderingSettings = false;

        /// <summary>Invert face culling.</summary>
        public bool invertFaceCulling = false;

        /// <summary>Probe layer mask.</summary>
        public LayerMask probeLayerMask = ~0;

        /// <summary>Enable to retain history buffers even if the camera is disabled.</summary>
        public bool hasPersistentHistory = false;

        /// <summary>Screen size used when Screen Coordinates Override is active.</summary>
        public Vector4 screenSizeOverride;

        /// <summary>Transform used when Screen Coordinates Override is active.</summary>
        public Vector4 screenCoordScaleBias;

        /// <summary>Allow NVIDIA Deep Learning Super Sampling (DLSS) on this camera.</summary>
        [Tooltip("Allow NVIDIA Deep Learning Super Sampling (DLSS) on this camera")]
        public bool allowDeepLearningSuperSampling = true;

        /// <summary>If set to true, NVIDIA Deep Learning Super Sampling (DLSS) will utilize the Quality setting set on this camera instead of the one specified in the quality asset.</summary>
        [Tooltip("If set to true, NVIDIA Deep Learning Super Sampling (DLSS) will utilize the Quality setting set on this camera instead of the one specified in the quality asset.")]
        public bool deepLearningSuperSamplingUseCustomQualitySettings = false;

        /// <summary>Selects a performance quality setting for NVIDIA Deep Learning Super Sampling (DLSS) for this camera of this project.</summary>
        [Tooltip("Selects a performance quality setting for NVIDIA Deep Learning Super Sampling (DLSS) for this camera of this project.")]
        public uint deepLearningSuperSamplingQuality = 0;

        /// <summary>If set to true, NVIDIA Deep Learning Super Sampling (DLSS) will utilize the Quality setting set on this camera instead of the one specified in the quality asset of this project.</summary>
        [Tooltip("If set to true, NVIDIA Deep Learning Super Sampling (DLSS) will utilize the attributes (Optimal Settings and Sharpness) specified on this camera, instead of the ones specified in the quality asset of this project.")]
        public bool deepLearningSuperSamplingUseCustomAttributes = false;

        /// <summary>Sets the sharpness and scale automatically for NVIDIA Deep Learning Super Sampling (DLSS) for this camera, depending on the values of quality settings.</summary>
        [Tooltip("Sets the sharpness and scale automatically for NVIDIA Deep Learning Super Sampling (DLSS) for this camera, depending on the values of quality settings.")]
        public bool deepLearningSuperSamplingUseOptimalSettings = true;

        /// <summary>Sets the Sharpening value for NVIDIA Deep Learning Super Sampling (DLSS) for this camera.</summary>
        [Tooltip("Sets the Sharpening value for NVIDIA Deep Learning Super Sampling (DLSS) for this camera.")]
        [Range(0, 1)]
        public float deepLearningSuperSamplingSharpening = 0;

        /// <summary>Allow FidelityFX Super Resolution (FSR2) on this camera.</summary>
        [Tooltip("Allow FidelityFX Super Resolution (FSR2) on this camera.")]
        public bool allowFidelityFX2SuperResolution = true;

        /// <summary>If set to true, AMD FidelityFX 2.0 Super Resolution (FSR2) will utilize the Quality setting set on this camera instead of the one specified in the quality asset.</summary>
        [Tooltip("If set to true, AMD FidelityFX 2.0 Super Resolution (FSR2) will utilize the Quality setting set on this camera instead of the one specified in the quality asset.")]
        public bool fidelityFX2SuperResolutionUseCustomQualitySettings = false;

        /// <summary>Selects a performance quality setting for AMD FidelityFX 2.0 Super Resolution (FSR2) for this camera of this project.</summary>
        [Tooltip("Selects a performance quality setting for AMD FidelityFX 2.0 Super Resolution (FSR2) for this camera of this project.")]
        public uint fidelityFX2SuperResolutionQuality = 0;

        /// <summary>If set to true, AMD FidelityFX 2.0 Super Resolution (FSR2) will utilize the attributes (Optimal Settings and Sharpness) specified on this camera instead of the ones specified in the quality asset of this project.</summary>
        [Tooltip("If set to true, AMD FidelityFX 2.0 Super Resolution (FSR2) will utilize the attributes (Optimal Settings and Sharpness) specified on this camera, instead of the ones specified in the quality asset of this project.")]
        public bool fidelityFX2SuperResolutionUseCustomAttributes = false;

        /// <summary>Sets the scale automatically for AMD FidelityFX 2.0 Super Resolution (FSR2) for this camera, depending on the values of quality settings.</summary>
        [Tooltip("Sets the scale automatically for AMD FidelityFX 2.0 Super Resolution (FSR2) for this camera, depending on the values of quality settings.")]
        public bool fidelityFX2SuperResolutionUseOptimalSettings = true;

        /// <summary>Enables the Sharpening pass for AMD FidelityFX 2.0 Super Resolution (FSR2) for this camera.</summary>
        [Tooltip("Enables the Sharpening pass for AMD FidelityFX 2.0 Super Resolution (FSR2) for this camera.")]
        public bool fidelityFX2SuperResolutionEnableSharpening = false;

        /// <summary>Sets the Sharpening value for AMD FidelityFX 2.0 Super Resolution (FSR2) for this camera.</summary>
        [Tooltip("Sets the Sharpening value for AMD FidelityFX 2.0 Super Resolution (FSR2) for this camera.")]
        [Range(0, 1)]
        public float fidelityFX2SuperResolutionSharpening = 0;

        /// internal state set by the runtime wether DLSS is enabled or not on this camera, depending on the results of all other settings.
        [ExcludeCopy]
        internal bool cameraCanRenderDLSS = false;

        /// internal state set by the runtime whether FSR2 is enabled or not on this camera, depending on the results of all other settings.
        [ExcludeCopy]
        internal bool cameraCanRenderFSR2 = false;

        /// internal state set by the runtime whether STP is enabled or not on this camera, depending on the results of all other settings.
        [ExcludeCopy]
        internal bool cameraCanRenderSTP = false;

        /// <summary>If set to true, AMD FidelityFX Super Resolution (FSR) will utilize the sharpness setting set on this camera instead of the one specified in the quality asset.</summary>
        [Tooltip("If set to true, AMD FidelityFX Super Resolution (FSR) will utilize the sharpness setting set on this camera instead of the one specified in the quality asset.")]
        public bool fsrOverrideSharpness = false;

        /// <summary>Sets this camera's sharpness value for AMD FidelityFX Super Resolution.</summary>
        [Tooltip("Sets this camera's sharpness value for AMD FidelityFX Super Resolution 1.0 (FSR).")]
        [Range(0, 1)]
        public float fsrSharpness = FSRUtils.kDefaultSharpnessLinear;

        /// <summary>Event used to override HDRP rendering for this particular camera.</summary>
        public event Action<ScriptableRenderContext, HDCamera> customRender;
        /// <summary>True if any Custom Render event is registered for this camera.</summary>
        public bool hasCustomRender { get { return customRender != null; } }
        /// <summary>
        /// Delegate used to request access to various buffers of this camera.
        /// </summary>
        /// <param name="bufferAccess">Ref to a BufferAccess structure on which users should specify which buffer(s) they need.</param>
        public delegate void RequestAccessDelegate(ref BufferAccess bufferAccess);
        /// <summary>RequestAccessDelegate used to request access to various buffers of this camera.</summary>
        public event RequestAccessDelegate requestGraphicsBuffer;

        /// <summary>The object used as a target for centering the Exposure's Procedural Mask metering mode when target object option is set (See Exposure Volume Component).</summary>
        public GameObject exposureTarget = null;

        /// <summary> Mip bias used on texture samplers during material rendering </summary>
        public float materialMipBias = 0;

        internal float probeCustomFixedExposure = 1.0f;

        [ExcludeCopy]
        internal float deExposureMultiplier = 1.0f;

        [SerializeField, FormerlySerializedAs("renderingPathCustomFrameSettings")]
        FrameSettings m_RenderingPathCustomFrameSettings = FrameSettingsDefaults.Get(FrameSettingsRenderType.Camera);

        /// <summary>Mask specifying which frame settings are overridden when using custom frame settings.</summary>
        public FrameSettingsOverrideMask renderingPathCustomFrameSettingsOverrideMask;

        /// <summary>When using default frame settings, specify which type of frame settings to use.</summary>
        public FrameSettingsRenderType defaultFrameSettings;

        /// <summary>Custom frame settings.</summary>
        public ref FrameSettings renderingPathCustomFrameSettings => ref m_RenderingPathCustomFrameSettings;

        bool IFrameSettingsHistoryContainer.hasCustomFrameSettings
            => customRenderingSettings;

        FrameSettingsOverrideMask IFrameSettingsHistoryContainer.frameSettingsMask
            => renderingPathCustomFrameSettingsOverrideMask;

        FrameSettings IFrameSettingsHistoryContainer.frameSettings
            => m_RenderingPathCustomFrameSettings;

        [ExcludeCopy]
        FrameSettingsHistory m_RenderingPathHistory = new FrameSettingsHistory()
        {
            defaultType = FrameSettingsRenderType.Camera
        };

        FrameSettingsHistory IFrameSettingsHistoryContainer.frameSettingsHistory
        {
            get => m_RenderingPathHistory;
            set => m_RenderingPathHistory = value;
        }

        string IFrameSettingsHistoryContainer.panelName
            => m_CameraRegisterName;

        /// <summary>
        /// .
        /// </summary>
        /// <returns>.</returns>
        Action IDebugData.GetReset()
        //caution: we actually need to retrieve the right
        //m_FrameSettingsHistory as it is a struct so no direct
        // => m_FrameSettingsHistory.TriggerReset
            => () => m_RenderingPathHistory.TriggerReset();

        [ExcludeCopy]
        internal ProfilingSampler profilingSampler;

        [ExcludeCopy]
        AOVRequestDataCollection m_AOVRequestDataCollection = new AOVRequestDataCollection(null);

        /// <summary>Set AOV requests to use.</summary>
        /// <param name="aovRequests">Describes the requests to execute.</param>
        /// <example>
        /// <code>
        /// using System.Collections.Generic;
        /// using UnityEngine;
        /// using UnityEngine.Rendering;
        /// using UnityEngine.Rendering.HighDefinition;
        /// using UnityEngine.Rendering.HighDefinition.Attributes;
        ///
        /// [ExecuteAlways]
        /// [RequireComponent(typeof(Camera))]
        /// [RequireComponent(typeof(HDAdditionalCameraData))]
        /// public class SetupAOVCallbacks : MonoBehaviour
        /// {
        ///     private static RTHandle m_ColorRT;
        ///
        ///     [SerializeField] private Texture m_Target;
        ///     [SerializeField] private DebugFullScreen m_DebugFullScreen;
        ///     [SerializeField] private DebugLightFilterMode m_DebugLightFilter;
        ///     [SerializeField] private MaterialSharedProperty m_MaterialSharedProperty;
        ///     [SerializeField] private LightingProperty m_LightingProperty;
        ///     [SerializeField] private AOVBuffers m_BuffersToCopy;
        ///     [SerializeField] private List&lt;GameObject&gt; m_IncludedLights;
        ///
        ///
        ///     void OnEnable()
        ///     {
        ///         var aovRequest = new AOVRequest(AOVRequest.NewDefault())
        ///             .SetLightFilter(m_DebugLightFilter);
        ///         if (m_DebugFullScreen != DebugFullScreen.None)
        ///             aovRequest = aovRequest.SetFullscreenOutput(m_DebugFullScreen);
        ///         if (m_MaterialSharedProperty != MaterialSharedProperty.None)
        ///             aovRequest = aovRequest.SetFullscreenOutput(m_MaterialSharedProperty);
        ///         if (m_LightingProperty != LightingProperty.None)
        ///             aovRequest = aovRequest.SetFullscreenOutput(m_LightingProperty);
        ///
        ///         var add = GetComponent&lt;HDAdditionalCameraData&gt;();
        ///         add.SetAOVRequests(
        ///             new AOVRequestBuilder()
        ///                 .Add(
        ///                     aovRequest,
        ///                     bufferId =&gt; m_ColorRT ?? (m_ColorRT = RTHandles.Alloc(512, 512)),
        ///                     m_IncludedLights.Count > 0 ? m_IncludedLights : null,
        ///                     new []{ m_BuffersToCopy },
        ///                     (cmd, textures, properties) =>
        ///                     {
        ///                         if (m_Target != null)
        ///                             cmd.Blit(textures[0], m_Target);
        ///                     })
        ///                 .Build()
        ///         );
        ///     }
        ///
        ///     private void OnGUI()
        ///     {
        ///         GUI.DrawTexture(new Rect(10, 10, 512, 256), m_Target);
        ///     }
        ///
        ///     void OnDisable()
        ///     {
        ///         var add = GetComponent&lt;HDAdditionalCameraData&gt;();
        ///         add.SetAOVRequests(null);
        ///     }
        ///
        ///     void OnValidate()
        ///     {
        ///         OnDisable();
        ///         OnEnable();
        ///     }
        /// }
        /// </code>
        ///
        /// Example use case:
        /// * Export Normals: use MaterialSharedProperty.Normals and AOVBuffers.Color
        /// * Export Color before post processing: use AOVBuffers.Color
        /// * Export Color after post processing: use AOVBuffers.Output
        /// * Export Depth stencil: use AOVBuffers.DepthStencil
        /// * Export AO: use MaterialSharedProperty.AmbientOcclusion and AOVBuffers.Color
        /// </example>
        public void SetAOVRequests(AOVRequestDataCollection aovRequests)
            => m_AOVRequestDataCollection = aovRequests;

        /// <summary>
        /// Use this property to get the aov requests.
        ///
        /// It is never null.
        /// </summary>
        public IEnumerable<AOVRequestData> aovRequests =>
            m_AOVRequestDataCollection ?? (m_AOVRequestDataCollection = new AOVRequestDataCollection(null));

        // Use for debug windows
        // When camera name change we need to update the name in DebugWindows.
        // This is the purpose of this class
        [ExcludeCopy]
        bool m_IsDebugRegistered = false;
        [ExcludeCopy]
        string m_CameraRegisterName;

        // When we are a preview, there is no way inside Unity to make a distinction between camera preview and material preview.
        // This property allow to say that we are an editor camera preview when the type is preview.
        /// <summary>
        /// Unity support two type of preview: Camera preview and material preview. This property allow to know that we are an editor camera preview when the type is preview.
        /// </summary>
        [field: ExcludeCopy]
        public bool isEditorCameraPreview { get; internal set; }

        // This is use to copy data into camera for the Reset() workflow in camera editor
        /// <summary>
        /// Copy HDAdditionalCameraData.
        /// </summary>
        /// <param name="data">Component to copy to.</param>
        public void CopyTo(HDAdditionalCameraData data)
        {
            data.clearColorMode = clearColorMode;
            data.backgroundColorHDR = backgroundColorHDR;
            data.clearDepth = clearDepth;
            data.customRenderingSettings = customRenderingSettings;
            data.volumeLayerMask = volumeLayerMask;
            data.volumeAnchorOverride = volumeAnchorOverride;
            data.antialiasing = antialiasing;
            data.dithering = dithering;
            data.xrRendering = xrRendering;
            data.SMAAQuality = SMAAQuality;
            data.stopNaNs = stopNaNs;
            data.taaSharpenStrength = taaSharpenStrength;
            data.TAAQuality = TAAQuality;
            data.taaSharpenMode = taaSharpenMode;
            data.taaRingingReduction = taaRingingReduction;
            data.taaHistorySharpening = taaHistorySharpening;
            data.taaAntiFlicker = taaAntiFlicker;
            data.taaMotionVectorRejection = taaMotionVectorRejection;
            data.taaAntiHistoryRinging = taaAntiHistoryRinging;
            data.taaBaseBlendFactor = taaBaseBlendFactor;
            data.taaJitterScale = taaJitterScale;
            data.flipYMode = flipYMode;
            data.fullscreenPassthrough = fullscreenPassthrough;
            data.allowDynamicResolution = allowDynamicResolution;
            data.invertFaceCulling = invertFaceCulling;
            data.probeLayerMask = probeLayerMask;
            data.hasPersistentHistory = hasPersistentHistory;
            data.exposureTarget = exposureTarget;
#pragma warning disable CS0618
            data.physicalParameters = physicalParameters;
#pragma warning restore CS0618

            data.renderingPathCustomFrameSettings = renderingPathCustomFrameSettings;
            data.renderingPathCustomFrameSettingsOverrideMask = renderingPathCustomFrameSettingsOverrideMask;
            data.defaultFrameSettings = defaultFrameSettings;

            data.probeCustomFixedExposure = probeCustomFixedExposure;

            data.allowDeepLearningSuperSampling = allowDeepLearningSuperSampling;
            data.deepLearningSuperSamplingUseCustomQualitySettings = deepLearningSuperSamplingUseCustomQualitySettings;
            data.deepLearningSuperSamplingQuality = deepLearningSuperSamplingQuality;
            data.deepLearningSuperSamplingUseCustomAttributes = deepLearningSuperSamplingUseCustomAttributes;
            data.deepLearningSuperSamplingUseOptimalSettings = deepLearningSuperSamplingUseOptimalSettings;
            data.deepLearningSuperSamplingSharpening = deepLearningSuperSamplingSharpening;

            data.allowFidelityFX2SuperResolution = allowFidelityFX2SuperResolution;
            data.fidelityFX2SuperResolutionUseCustomQualitySettings = fidelityFX2SuperResolutionUseCustomQualitySettings;
            data.fidelityFX2SuperResolutionQuality = fidelityFX2SuperResolutionQuality;
            data.fidelityFX2SuperResolutionUseCustomAttributes = fidelityFX2SuperResolutionUseCustomAttributes;
            data.fidelityFX2SuperResolutionUseOptimalSettings = fidelityFX2SuperResolutionUseOptimalSettings;
            data.fidelityFX2SuperResolutionEnableSharpening = fidelityFX2SuperResolutionEnableSharpening;
            data.fidelityFX2SuperResolutionSharpening = fidelityFX2SuperResolutionSharpening;
            data.fsrOverrideSharpness = fsrOverrideSharpness;
            data.fsrSharpness = fsrSharpness;

            data.materialMipBias = materialMipBias;

            data.screenSizeOverride = screenSizeOverride;
            data.screenCoordScaleBias = screenCoordScaleBias;

            // We must not copy the following
            //data.m_IsDebugRegistered = m_IsDebugRegistered;
            //data.m_CameraRegisterName = m_CameraRegisterName;
            //data.isEditorCameraPreview = isEditorCameraPreview;
        }

        // For custom projection matrices
        // Set the proper getter
        /// <summary>
        /// Specify a custom getter for non oblique projection matrix.
        /// </summary>
        [ExcludeCopy]
        public NonObliqueProjectionGetter nonObliqueProjectionGetter = GeometryUtils.CalculateProjectionMatrix;

        /// <summary>
        /// Returns the non oblique projection matrix for this camera.
        /// </summary>
        /// <param name="camera">Requested camera.</param>
        /// <returns>The non oblique projection matrix for this camera.</returns>
        public Matrix4x4 GetNonObliqueProjection(Camera camera)
        {
            return nonObliqueProjectionGetter(camera);
        }

        void RegisterDebug()
        {
            if (!m_IsDebugRegistered)
            {
                // Note that we register FrameSettingsHistory, so manipulating FrameSettings in the Debug windows
                // doesn't affect the serialized version
                // Note camera's preview camera is registered with preview type but then change to game type that lead to issue.
                // Do not attempt to not register them till this issue persist.
                m_CameraRegisterName = name;
                if (m_Camera.cameraType != CameraType.Preview && m_Camera.cameraType != CameraType.Reflection)
                {
                    DebugDisplaySettings.RegisterCamera(this);
                }
                m_IsDebugRegistered = true;
            }
        }

        void UnRegisterDebug()
        {
            if (m_IsDebugRegistered)
            {
                // Note camera's preview camera is registered with preview type but then change to game type that lead to issue.
                // Do not attempt to not register them till this issue persist.
                if (m_Camera.cameraType != CameraType.Preview && m_Camera?.cameraType != CameraType.Reflection)
                {
                    DebugDisplaySettings.UnRegisterCamera(this);
                }
                m_IsDebugRegistered = false;
            }
        }

        void OnEnable()
        {
            if(GraphicsSettings.currentRenderPipelineAssetType != typeof(HDRenderPipelineAsset))
                return;

            // Be sure legacy HDR option is disable on camera as it cause banding in SceneView. Yes, it is a contradiction, but well, Unity...
            // When HDR option is enabled, Unity render in FP16 then convert to 8bit with a stretch copy (this cause banding as it should be convert to sRGB (or other color appropriate color space)), then do a final shader with sRGB conversion
            // When LDR, unity render in 8bitSRGB, then do a final shader with sRGB conversion
            // What should be done is just in our Post process we convert to sRGB and store in a linear 10bit, but require C++ change...
            m_Camera = GetComponent<Camera>();
            if (m_Camera == null)
                return;

            m_Camera.allowMSAA = false; // We don't use this option in HD (it is legacy MSAA) and it produce a warning in the inspector UI if we let it
            m_Camera.allowHDR = false;

            // By doing that, we force the update of frame settings debug data once. Otherwise, when the Rendering Debugger is opened,
            // Wrong data is registered to the undo system because it did not get the chance to be updated once.
            FrameSettings dummy = new FrameSettings(); //don't require full init as will be fully reset in AggregateFrameSettings
            if (GraphicsSettings.TryGetRenderPipelineSettings<RenderingPathFrameSettings>(out var renderingPathFrameSettings))
                FrameSettingsHistory.AggregateFrameSettings(renderingPathFrameSettings, ref dummy, m_Camera, this, HDRenderPipeline.currentAsset, null);

            RegisterDebug();

#if UNITY_EDITOR
            UpdateDebugCameraName();
            UnityEditor.EditorApplication.hierarchyChanged += UpdateDebugCameraName;
#endif
        }

        void UpdateDebugCameraName()
        {
            // Move the garbage generated by accessing name outside of HDRP
            profilingSampler = new ProfilingSampler(HDUtils.ComputeCameraName(name));

            if (name != m_CameraRegisterName)
            {
                UnRegisterDebug();
                RegisterDebug();
            }
        }

        void OnDisable()
        {
            UnRegisterDebug();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.hierarchyChanged -= UpdateDebugCameraName;
#endif
        }

        // This is called at the creation of the HD Additional Camera Data, to convert the legacy camera settings to HD
        internal static void InitDefaultHDAdditionalCameraData(HDAdditionalCameraData cameraData)
        {
            var camera = cameraData.gameObject.GetComponent<Camera>();

            cameraData.clearDepth = camera.clearFlags != CameraClearFlags.Nothing;

            if (camera.clearFlags == CameraClearFlags.Skybox)
                cameraData.clearColorMode = ClearColorMode.Sky;
            else if (camera.clearFlags == CameraClearFlags.SolidColor)
                cameraData.clearColorMode = ClearColorMode.Color;
            else     // None
                cameraData.clearColorMode = ClearColorMode.None;
        }

        internal void ExecuteCustomRender(ScriptableRenderContext renderContext, HDCamera hdCamera)
        {
            if (customRender != null)
            {
                customRender(renderContext, hdCamera);
            }
        }

        internal BufferAccessType GetBufferAccess()
        {
            BufferAccess result = new BufferAccess();
            requestGraphicsBuffer?.Invoke(ref result);
            return result.bufferAccess;
        }

        /// <summary>
        /// Returns the requested graphics buffer.
        /// Users should use the requestGraphicsBuffer event to make sure that the required buffers are requested first.
        /// Note that depending on the current frame settings some buffers may not be available.
        /// </summary>
        /// <param name="type">Type of the requested buffer.</param>
        /// <returns>Requested buffer as a RTHandle. Can be null if the buffer is not available.</returns>
        public RTHandle GetGraphicsBuffer(BufferAccessType type)
        {
            HDCamera hdCamera = HDCamera.GetOrCreate(m_Camera);
            if ((type & BufferAccessType.Color) != 0)
                return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);
            else if ((type & BufferAccessType.Depth) != 0)
                return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
            else if ((type & BufferAccessType.Normal) != 0)
                return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal);
            else
                return null;
        }
    }
}
