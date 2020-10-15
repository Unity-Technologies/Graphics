using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
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
        None,
        /// <summary>FXAA.</summary>
        FastApproximateAntialiasing,
        /// <summary>Temporal anti-aliasing.</summary>
        TemporalAntialiasing,
        /// <summary>SMAA.</summary>
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

    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "HDRP-Camera" + Documentation.endURL)]
    [CustomExtensionName("HDRP", typeof(HDRenderPipeline))]
    public partial class HDCameraExtension : Camera.Extension, IFrameSettingsHistoryContainer
    {
        #region Transferable State

        /// <summary>
        /// States that we can copy from a camera to another one
        /// </summary>
        [Serializable]
        internal struct TransferableState
        {
            public ClearColorMode clearColorMode;
            [ColorUsage(true, true)]
            public Color backgroundColorHDR;
            public bool clearDepth;
            [Tooltip("LayerMask HDRP uses for Volume interpolation for this Camera.")] //TODO: remove this
            public LayerMask volumeLayerMask;
            public Transform volumeAnchorOverride;
            public AntialiasingMode antialiasing;
            public SMAAQualityLevel SMAAQuality;
            public bool dithering;
            public bool stopNaNs;
            [Range(0, 2)]
            public float taaSharpenStrength;
            public TAAQualityLevel TAAQuality;
            [Range(0, 1)]
            public float taaHistorySharpening;
            [Range(0.0f, 1.0f)]
            public float taaAntiFlicker;
            [Range(0.0f, 1.0f)]
            public float taaMotionVectorRejection;
            public bool taaAntiHistoryRinging;
            public HDPhysicalCamera physicalParameters;
            public FlipYMode flipYMode;
            public bool xrRendering;
            [Tooltip("Skips rendering settings to directly render in fullscreen (Useful for video).")] //TODO: remove this
            public bool fullscreenPassthrough;
            [Tooltip("Allows dynamic resolution on buffers linked to this camera.")] //TODO: remove this
            public bool allowDynamicResolution;
            [Tooltip("Allows you to override the default settings for this camera.")] //TODO: remove this
            public bool customRenderingSettings;
            public bool invertFaceCulling;
            public LayerMask probeLayerMask;
            public bool hasPersistentHistory;
            public GameObject exposureTarget;
            public FrameSettings renderingPathCustomFrameSettings;
            public FrameSettingsOverrideMask renderingPathCustomFrameSettingsOverrideMask;
            public FrameSettingsRenderType defaultFrameSettings;
            
            //non public state that need to be copied / reset
            internal float probeCustomFixedExposure;

            public void Reset()
            {
                clearColorMode = ClearColorMode.Sky;
                backgroundColorHDR = new Color(0.025f, 0.07f, 0.19f, 0.0f);
                clearDepth = true;
                volumeLayerMask = 1;
                volumeAnchorOverride = default;
                antialiasing = AntialiasingMode.None;
                SMAAQuality = SMAAQualityLevel.High;
                dithering = false;
                stopNaNs = false;
                taaSharpenStrength = 0.5f;
                TAAQuality = TAAQualityLevel.Medium;
                taaHistorySharpening = 0.35f;
                taaAntiFlicker = 0.5f;
                taaMotionVectorRejection = 0.0f;
                taaAntiHistoryRinging = false;
                flipYMode = default;
                xrRendering = true;
                fullscreenPassthrough = false;
                allowDynamicResolution = false;
                customRenderingSettings = false;
                invertFaceCulling = false;
                probeLayerMask = ~0;
                hasPersistentHistory = false;
                exposureTarget = default;
                renderingPathCustomFrameSettings = FrameSettings.NewDefaultCamera();
                renderingPathCustomFrameSettingsOverrideMask = default;
                defaultFrameSettings = default;

                physicalParameters.Reset();

                probeCustomFixedExposure = 1.0f;
            }

            public static TransferableState Create()
            {
                TransferableState result = default;
                result.Reset();
                return result;
            }
        }
        [SerializeField] TransferableState m_TransferableState = TransferableState.Create();

        [Obsolete("Only exist to allow migration from HDAdditionalCameraData. Should be removed.")]
        internal ref TransferableState state => ref m_TransferableState;

        // This is use to copy data into camera for the Reset() workflow in camera editor
        /// <summary>
        /// Copy HDCameraExtension.
        /// </summary>
        /// <param name="data">Extension to copy to.</param>
        public void CopyTo(HDCameraExtension data)
            => data.m_TransferableState = m_TransferableState;

        /// <summary>Clear mode for the camera background.</summary>
        public ClearColorMode clearColorMode
        {
            get => m_TransferableState.clearColorMode;
            set => m_TransferableState.clearColorMode = value;
        }
        /// <summary>HDR color used for clearing the camera background.</summary>
        public Color backgroundColorHDR
        {
            get => m_TransferableState.backgroundColorHDR;
            set => m_TransferableState.backgroundColorHDR = value;
        }
        /// <summary>Clear depth as well as color.</summary>
        public bool clearDepth
        {
            get => m_TransferableState.clearDepth;
            set => m_TransferableState.clearDepth = value;
        }

        /// <summary>Layer mask used to select which volumes will influence this camera.</summary>
        public LayerMask volumeLayerMask
        {
            get => m_TransferableState.volumeLayerMask;
            set => m_TransferableState.volumeLayerMask = value;
        }

        /// <summary>Optional transform override for the position where volumes are interpolated.</summary>
        public Transform volumeAnchorOverride
        {
            get => m_TransferableState.volumeAnchorOverride;
            set => m_TransferableState.volumeAnchorOverride = value;
        }

        /// <summary>Anti-aliasing mode.</summary>
        public AntialiasingMode antialiasing
        {
            get => m_TransferableState.antialiasing;
            set => m_TransferableState.antialiasing = value;
        }
        /// <summary>Quality of the anti-aliasing when using SMAA.</summary>
        public SMAAQualityLevel SMAAQuality
        {
            get => m_TransferableState.SMAAQuality;
            set => m_TransferableState.SMAAQuality = value;
        }
        /// <summary>Use dithering to filter out minor banding.</summary>
        public bool dithering
        {
            get => m_TransferableState.dithering;
            set => m_TransferableState.dithering = value;
        }
        /// <summary>Use a pass to eliminate NaNs contained in the color buffer before post-processing.</summary>
        public bool stopNaNs
        {
            get => m_TransferableState.stopNaNs;
            set => m_TransferableState.stopNaNs = value;
        }

        /// <summary>Strength of the sharpening component of temporal anti-aliasing.</summary>
        public float taaSharpenStrength
        {
            get => m_TransferableState.taaSharpenStrength;
            set => m_TransferableState.taaSharpenStrength = value;
        }

        /// <summary>Quality of the anti-aliasing when using TAA.</summary>
        public TAAQualityLevel TAAQuality
        {
            get => m_TransferableState.TAAQuality;
            set => m_TransferableState.TAAQuality = value;
        }

        /// <summary>Strength of the sharpening of the history sampled for TAA.</summary>
        public float taaHistorySharpening
        {
            get => m_TransferableState.taaHistorySharpening;
            set => m_TransferableState.taaHistorySharpening = value;
        }

        /// <summary>Drive the anti-flicker mechanism. With high values flickering might be reduced, but it can lead to more ghosting or disocclusion artifacts.</summary>
        public float taaAntiFlicker
        {
            get => m_TransferableState.taaAntiFlicker;
            set => m_TransferableState.taaAntiFlicker = value;
        }

        /// <summary>Larger is this value, more likely history will be rejected when current and reprojected history motion vector differ by a substantial amount. 
        /// Larger values can decrease ghosting but will also reintroduce aliasing on the aforementioned cases.</summary>
        public float taaMotionVectorRejection
        {
            get => m_TransferableState.taaMotionVectorRejection;
            set => m_TransferableState.taaMotionVectorRejection = value;
        }

        /// <summary>When enabled, ringing artifacts (dark or strangely saturated edges) caused by history sharpening will be improved. This comes at a potential loss of sharpness upon motion.</summary>
        public bool taaAntiHistoryRinging
        {
            get => m_TransferableState.taaAntiHistoryRinging;
            set => m_TransferableState.taaAntiHistoryRinging = value;
        }

        /// <summary>Physical camera parameters.</summary>
        public HDPhysicalCamera physicalParameters
        {
            get => m_TransferableState.physicalParameters;
            set => m_TransferableState.physicalParameters = value;
        }

        /// <summary>Vertical flip mode.</summary>
        public FlipYMode flipYMode
        {
            get => m_TransferableState.flipYMode;
            set => m_TransferableState.flipYMode = value;
        }

        /// <summary>Enable XR rendering.</summary>
        public bool xrRendering
        {
            get => m_TransferableState.xrRendering;
            set => m_TransferableState.xrRendering = value;
        }

        /// <summary>Skips rendering settings to directly render in fullscreen (Useful for video).</summary>
        public bool fullscreenPassthrough
        {
            get => m_TransferableState.fullscreenPassthrough;
            set => m_TransferableState.fullscreenPassthrough = value;
        }

        /// <summary>Allows dynamic resolution on buffers linked to this camera.</summary>
        public bool allowDynamicResolution
        {
            get => m_TransferableState.allowDynamicResolution;
            set => m_TransferableState.allowDynamicResolution = value;
        }

        /// <summary>Allows you to override the default frame settings for this camera.</summary>
        public bool customRenderingSettings
        {
            get => m_TransferableState.customRenderingSettings;
            set => m_TransferableState.customRenderingSettings = value;
        }

        /// <summary>Invert face culling.</summary>
        public bool invertFaceCulling
        {
            get => m_TransferableState.invertFaceCulling;
            set => m_TransferableState.invertFaceCulling = value;
        }

        /// <summary>Probe layer mask.</summary>
        public LayerMask probeLayerMask
        {
            get => m_TransferableState.probeLayerMask;
            set => m_TransferableState.probeLayerMask = value;
        }

        /// <summary>Enable to retain history buffers even if the camera is disabled.</summary>
        public bool hasPersistentHistory
        {
            get => m_TransferableState.hasPersistentHistory;
            set => m_TransferableState.hasPersistentHistory = value;
        }

        /// <summary>The object used as a target for centering the Exposure's Procedural Mask metering mode when target object option is set (See Exposure Volume Component).</summary>
        public GameObject exposureTarget
        {
            get => m_TransferableState.exposureTarget;
            set => m_TransferableState.exposureTarget = value;
        }

        internal float probeCustomFixedExposure
        {
            get => m_TransferableState.probeCustomFixedExposure;
            set => m_TransferableState.probeCustomFixedExposure = value;
        }

        #endregion

        #region Frame Settings

        /// <summary>Mask specifying which frame settings are overridden when using custom frame settings.</summary>
        public FrameSettingsOverrideMask renderingPathCustomFrameSettingsOverrideMask
        {
            get => m_TransferableState.renderingPathCustomFrameSettingsOverrideMask;
            set => m_TransferableState.renderingPathCustomFrameSettingsOverrideMask = value;
        }
        /// <summary>When using default frame settings, specify which type of frame settings to use.</summary>
        public FrameSettingsRenderType defaultFrameSettings
        {
            get => m_TransferableState.defaultFrameSettings;
            set => m_TransferableState.defaultFrameSettings = value;
        }
        /// <summary>Custom frame settings.</summary>
        public ref FrameSettings renderingPathCustomFrameSettings => ref m_TransferableState.renderingPathCustomFrameSettings;

        bool IFrameSettingsHistoryContainer.hasCustomFrameSettings
            => customRenderingSettings;

        FrameSettingsOverrideMask IFrameSettingsHistoryContainer.frameSettingsMask
            => renderingPathCustomFrameSettingsOverrideMask;

        FrameSettings IFrameSettingsHistoryContainer.frameSettings
            => renderingPathCustomFrameSettings;
        
        FrameSettingsHistory IFrameSettingsHistoryContainer.frameSettingsHistory
        {
            get => m_RenderingPathHistory;
            set => m_RenderingPathHistory = value;
        }

        #endregion

        #region Debug Menu
        
        // Use for debug windows
        // When camera name change we need to update the name in DebugWindows.
        // This is the purpose of this class
        bool m_IsDebugRegistered = false;
        string m_CameraRegisterName;
        
        // Used to keep aggregation history in FrameSettings for DebugMenu
        FrameSettingsHistory m_RenderingPathHistory = new FrameSettingsHistory()
        {
            defaultType = FrameSettingsRenderType.Camera
        };

        internal ProfilingSampler profilingSampler;

        string IFrameSettingsHistoryContainer.panelName
            => m_CameraRegisterName;

        Action IDebugData.GetReset()
                //caution: we actually need to retrieve the right
                //m_FrameSettingsHistory as it is a struct so no direct
                // => m_FrameSettingsHistory.TriggerReset
                => () => m_RenderingPathHistory.TriggerReset();
        
        void RegisterDebug()
        {
            if (!m_IsDebugRegistered)
            {
                // Note that we register FrameSettingsHistory, so manipulating FrameSettings in the Debug windows
                // doesn't affect the serialized version
                // Note camera's preview camera is registered with preview type but then change to game type that lead to issue.
                // Do not attempt to not register them till this issue persist.
                m_CameraRegisterName = cameraHandler.name;
                if (cameraHandler.cameraType != CameraType.Preview && cameraHandler.cameraType != CameraType.Reflection)
                {
                    DebugDisplaySettings.RegisterCamera(this);
                    VolumeDebugSettings.RegisterCamera(this);
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
                if (cameraHandler.cameraType != CameraType.Preview && cameraHandler.cameraType != CameraType.Reflection)
                {
                    VolumeDebugSettings.UnRegisterCamera(this);
                    DebugDisplaySettings.UnRegisterCamera(this);
                }
                m_IsDebugRegistered = false;
            }
        }

        void UpdateDebugCameraName()
        {
            // Move the garbage generated by accessing name outside of HDRP
            profilingSampler = new ProfilingSampler(HDUtils.ComputeCameraName(cameraHandler.name));

            if (cameraHandler.name != m_CameraRegisterName)
            {
                UnRegisterDebug();
                RegisterDebug();
            }
        }

        #endregion

        #region Buffer

        /// <summary>
        /// Structure used to access graphics buffers for this camera.
        /// </summary>
        public struct BufferAccess
        {
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
 
        /// <summary>
        /// Delegate used to request access to various buffers of this camera.
        /// </summary>
        /// <param name="bufferAccess">Ref to a BufferAccess structure on which users should specify which buffer(s) they need.</param>
        public delegate void RequestAccessDelegate(ref BufferAccess bufferAccess);
        /// <summary>RequestAccessDelegate used to request access to various buffers of this camera.</summary>
        public event RequestAccessDelegate requestGraphicsBuffer;
        
        internal BufferAccess.BufferAccessType GetBufferAccess()
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
        public RTHandle GetGraphicsBuffer(BufferAccess.BufferAccessType type)
        {
            HDCamera hdCamera = HDCamera.GetOrCreate(cameraHandler);
            if ((type & BufferAccess.BufferAccessType.Color) != 0)
                return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);
            else if ((type & BufferAccess.BufferAccessType.Depth) != 0)
                return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
            else if ((type & BufferAccess.BufferAccessType.Normal) != 0)
                return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal);
            else
                return null;
        }

        #endregion

        #region AOV Request
        
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
        ///         var cam = GetComponent&lt;Camera&gt;();
        ///         var add = cam.extension as HDCameraExtension;
        ///         if (add == null)
        ///         {
        ///             if (!cam.HasExtension&lt;HDCameraExtension&gt;())
        ///                 cam.CreateExtension&lt;HDCameraExtension&gt;();
        ///             add = cam.SwitchActiveExtensionTo&lt;HDCameraExtension&gt;();
        ///         }
        /// 
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
        ///         var add = GetComponent&lt;Camera&gt;()?.GetExtension&lt;HDCameraExtension&gt;();
        ///         add?.SetAOVRequests(null);
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
        public IEnumerable<AOVRequestData> aovRequests
            => m_AOVRequestDataCollection ?? (m_AOVRequestDataCollection = new AOVRequestDataCollection(null));

        #endregion

        #region Custom Render

        /// <summary>Event used to override HDRP rendering for this particular camera.</summary>
        public event Action<ScriptableRenderContext, HDCamera> customRender;
        /// <summary>True if any Custom Render event is registered for this camera.</summary>
        public bool hasCustomRender { get { return customRender != null; } }

        internal void ExecuteCustomRender(ScriptableRenderContext renderContext, HDCamera hdCamera)
        {
            if (customRender != null)
            {
                customRender(renderContext, hdCamera);
            }
        }

        #endregion
        
        // When we are a preview, there is no way inside Unity to make a distinction between camera preview and material preview.
        // This property allow to say that we are an editor camera preview when the type is preview.
        /// <summary>
        /// Unity support two type of preview: Camera preview and material preview. This property allow to know that we are an editor camera preview when the type is preview.
        /// </summary>
        public bool isEditorCameraPreview { get; internal set; }

        // The light culling use standard projection matrices (non-oblique)
        // If the user overrides the projection matrix with an oblique one
        // He must also provide a callback to get the equivalent non oblique for the culling
        /// <summary>
        /// Returns the non oblique projection matrix for a particular camera.
        /// </summary>
        /// <param name="camera">Requested camera.</param>
        /// <returns>The non oblique projection matrix for a particular camera.</returns>
        public delegate Matrix4x4 NonObliqueProjectionGetter(Camera camera);
        
        // For custom projection matrices
        // Set the proper getter
        /// <summary>
        /// Specify a custom getter for non oblique projection matrix.
        /// </summary>
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

        void OnEnable()
        {
            // Be sure legacy HDR option is disable on camera as it cause banding in SceneView. Yes, it is a contradiction, but well, Unity...
            // When HDR option is enabled, Unity render in FP16 then convert to 8bit with a stretch copy (this cause banding as it should be convert to sRGB (or other color appropriate color space)), then do a final shader with sRGB conversion
            // When LDR, unity render in 8bitSRGB, then do a final shader with sRGB conversion
            // What should be done is just in our Post process we convert to sRGB and store in a linear 10bit, but require C++ change...
            cameraHandler.allowMSAA = false; // We don't use this option in HD (it is legacy MSAA) and it produce a warning in the inspector UI if we let it
            cameraHandler.allowHDR = false;

            RegisterDebug();

#if UNITY_EDITOR
            UpdateDebugCameraName();
            UnityEditor.EditorApplication.hierarchyChanged += UpdateDebugCameraName;
#endif
        }

        void OnDisable()
        {
            UnRegisterDebug();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.hierarchyChanged -= UpdateDebugCameraName;
#endif
        }

        void Awake()
            => SanitizeClearFlags();

        // Usage intent: When we go back from another SRP, we must sanitize the way HDRP handle clear flags
        void SanitizeClearFlags()
        {
            clearDepth = cameraHandler.clearFlags != CameraClearFlags.Nothing;

            if (cameraHandler.clearFlags == CameraClearFlags.Skybox)
                clearColorMode = ClearColorMode.Sky;
            else if (cameraHandler.clearFlags == CameraClearFlags.SolidColor)
                clearColorMode = ClearColorMode.Color;
            else     // None
                clearColorMode = ClearColorMode.None;
        }
        
        internal Camera camera => cameraHandler;
    }
}
