using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    using NewFlipYMode = FlipYMode;
    using NewClearColorMode = ClearColorMode;
    using NewAntialiasingMode = AntialiasingMode;
    using NewSMAAQualityLevel = SMAAQualityLevel;
    using NewTAAQualityLevel = TAAQualityLevel;

    /// <summary>
    /// Additional component that holds HDRP specific parameters for Cameras.
    /// </summary>
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "HDRP-Camera" + Documentation.endURL)]
    [AddComponentMenu("")] // Hide in menu
    [DisallowMultipleComponent, ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public partial class HDAdditionalCameraData : MonoBehaviour
    {
        HDCameraExtension m_Extension;
        HDCameraExtension extension
        {
            get
            {
                if (m_Extension == null)
                {
                    Camera camera = GetComponent<Camera>();
                    if (camera.extension is HDCameraExtension extension)
                    {
                        m_Extension = extension;
                    }
                    else
                    {
                        //handling case where user removed the extension manually
                        if (!camera.HasExtension<HDCameraExtension>())
                            camera.CreateExtension<HDCameraExtension>();

                        m_Extension = camera.SwitchActiveExtensionTo<HDCameraExtension>();
                    }
                }

                return m_Extension;
            }
        }

        /// <summary>
        /// How the camera should handle vertically flipping the frame at the end of rendering.
        /// </summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.FlipYMode instead.")]
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
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.BufferAccess.BufferAccessType instead.")]
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
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.BufferAccess instead.")]
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
            [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.BufferAccess.RequestAccess instead.")]
            public void RequestAccess(BufferAccessType flags)
            {
                bufferAccess |= flags;
            }

            //conversion operator for former event redirection
#pragma warning disable CS0618 // Type or member is obsolete
            public static explicit operator BufferAccess(HDCameraExtension.BufferAccess bufferAccess)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                var result = new BufferAccess();
                result.bufferAccess = (BufferAccessType)bufferAccess.bufferAccess;
                return result;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            public static explicit operator HDCameraExtension.BufferAccess(BufferAccess bufferAccess)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                var result = new HDCameraExtension.BufferAccess();
                result.bufferAccess = (HDCameraExtension.BufferAccess.BufferAccessType)bufferAccess.bufferAccess;
                return result;
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
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.NonObliqueProjectionGetter instead.")]
        public delegate Matrix4x4 NonObliqueProjectionGetter(Camera camera);

        /// <summary>
        /// Clear mode for the camera background.
        /// </summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.ClearColorMode instead.")]
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
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.AntialiasingMode instead.")]
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
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.SMAAQualityLevel instead.")]
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
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.TAAQualityLevel instead.")]
        public enum TAAQualityLevel
        {
            /// <summary>Low quality.</summary>
            Low,
            /// <summary>Medium quality.</summary>
            Medium,
            /// <summary>High quality.</summary>
            High
        }

        /// <summary>Clear mode for the camera background.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.clearColorMode instead.")]
        public ClearColorMode clearColorMode
        {
            get => (ClearColorMode)extension.clearColorMode;
            set => extension.clearColorMode = (NewClearColorMode)value;
        }
        /// <summary>HDR color used for clearing the camera background.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.backgroundColorHDR instead.")]
        public Color backgroundColorHDR
        {
            get => extension.backgroundColorHDR;
            set => extension.backgroundColorHDR = value;
        }
        /// <summary>Clear depth as well as color.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.clearDepth instead.")]
        public bool clearDepth
        {
            get => extension.clearDepth;
            set => extension.clearDepth = value;
        }

        /// <summary>Layer mask used to select which volumes will influence this camera.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.volumeLayerMask instead.")]
        public LayerMask volumeLayerMask
        {
            get => extension.volumeLayerMask;
            set => extension.volumeLayerMask = value;
        }

        /// <summary>Optional transform override for the position where volumes are interpolated.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.volumeAnchorOverride instead.")]
        public Transform volumeAnchorOverride
        {
            get => extension.volumeAnchorOverride;
            set => extension.volumeAnchorOverride = value;
        }

        /// <summary>Anti-aliasing mode.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.antialiasing instead.")]
        public AntialiasingMode antialiasing
        {
            get => (AntialiasingMode)extension.antialiasing;
            set => extension.antialiasing = (NewAntialiasingMode)value;
        }
        /// <summary>Quality of the anti-aliasing when using SMAA.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.SMAAQuality instead.")]
        public SMAAQualityLevel SMAAQuality
        {
            get => (SMAAQualityLevel)extension.SMAAQuality;
            set => extension.SMAAQuality = (NewSMAAQualityLevel)value;
        }
        /// <summary>Use dithering to filter out minor banding.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.dithering instead.")]
        public bool dithering
        {
            get => extension.dithering;
            set => extension.dithering = value;
        }
        /// <summary>Use a pass to eliminate NaNs contained in the color buffer before post-processing.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.stopNaNs instead.")]
        public bool stopNaNs
        {
            get => extension.stopNaNs;
            set => extension.stopNaNs = value;
        }

        /// <summary>Strength of the sharpening component of temporal anti-aliasing.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.taaSharpenStrength instead.")]
        public float taaSharpenStrength
        {
            get => extension.taaSharpenStrength;
            set => extension.taaSharpenStrength = value;
        }

        /// <summary>Quality of the anti-aliasing when using TAA.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.TAAQuality instead.")]
        public TAAQualityLevel TAAQuality
        {
            get => (TAAQualityLevel)extension.TAAQuality;
            set => extension.TAAQuality = (NewTAAQualityLevel)value;
        }

        /// <summary>Strength of the sharpening of the history sampled for TAA.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.taaHistorySharpening instead.")]
        public float taaHistorySharpening
        {
            get => extension.taaHistorySharpening;
            set => extension.taaHistorySharpening = value;
        }

        /// <summary>Drive the anti-flicker mechanism. With high values flickering might be reduced, but it can lead to more ghosting or disocclusion artifacts.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.taaAntiFlicker instead.")]
        public float taaAntiFlicker
        {
            get => extension.taaAntiFlicker;
            set => extension.taaAntiFlicker = value;
        }

        /// <summary>Larger is this value, more likely history will be rejected when current and reprojected history motion vector differ by a substantial amount. 
        /// Larger values can decrease ghosting but will also reintroduce aliasing on the aforementioned cases.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.taaMotionVectorRejection instead.")]
        public float taaMotionVectorRejection
        {
            get => extension.taaMotionVectorRejection;
            set => extension.taaMotionVectorRejection = value;
        }

        /// <summary>When enabled, ringing artifacts (dark or strangely saturated edges) caused by history sharpening will be improved. This comes at a potential loss of sharpness upon motion.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.taaAntiHistoryRinging instead.")]
        public bool taaAntiHistoryRinging
        {
            get => extension.taaAntiHistoryRinging;
            set => extension.taaAntiHistoryRinging = value;
        }

        /// <summary>Physical camera parameters.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.physicalParameters instead.")]
        public HDPhysicalCamera physicalParameters
        {
            get => extension.physicalParameters;
            set => extension.physicalParameters = value;
        }

        /// <summary>Vertical flip mode.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.flipYMode instead.")]
        public FlipYMode flipYMode
        {
            get => (FlipYMode)extension.flipYMode;
            set => extension.flipYMode = (NewFlipYMode)value;
        }

        /// <summary>Enable XR rendering.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.xrRendering instead.")]
        public bool xrRendering
        {
            get => extension.xrRendering;
            set => extension.xrRendering = value;
        }

        /// <summary>Skips rendering settings to directly render in fullscreen (Useful for video).</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.fullscreenPassthrough instead.")]
        public bool fullscreenPassthrough
        {
            get => extension.fullscreenPassthrough;
            set => extension.fullscreenPassthrough = value;
        }

        /// <summary>Allows dynamic resolution on buffers linked to this camera.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.allowDynamicResolution instead.")]
        public bool allowDynamicResolution
        {
            get => extension.allowDynamicResolution;
            set => extension.allowDynamicResolution = value;
        }

        /// <summary>Allows you to override the default frame settings for this camera.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.customRenderingSettings instead.")]
        public bool customRenderingSettings
        {
            get => extension.customRenderingSettings;
            set => extension.customRenderingSettings = value;
        }

        /// <summary>Invert face culling.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.invertFaceCulling instead.")]
        public bool invertFaceCulling
        {
            get => extension.invertFaceCulling;
            set => extension.invertFaceCulling = value;
        }

        /// <summary>Probe layer mask.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.probeLayerMask instead.")]
        public LayerMask probeLayerMask
        {
            get => extension.probeLayerMask;
            set => extension.probeLayerMask = value;
        }

        /// <summary>Enable to retain history buffers even if the camera is disabled.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.hasPersistentHistory instead.")]
        public bool hasPersistentHistory
        {
            get => extension.hasPersistentHistory;
            set => extension.hasPersistentHistory = value;
        }

        /// <summary>Event used to override HDRP rendering for this particular camera.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.customRender instead.")]
        public event Action<ScriptableRenderContext, HDCamera> customRender
        {
            add => extension.customRender += value;
            remove => extension.customRender -= value;
        }

        /// <summary>True if any Custom Render event is registered for this camera.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.hasCustomRender instead.")]
        public bool hasCustomRender => extension.hasCustomRender;

        /// <summary>
        /// Delegate used to request access to various buffers of this camera.
        /// </summary>
        /// <param name="bufferAccess">Ref to a BufferAccess structure on which users should specify which buffer(s) they need.</param>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.RequestAccessDelegate instead.")]
        public delegate void RequestAccessDelegate(ref BufferAccess bufferAccess);

#pragma warning disable CS0618 // Type or member is obsolete
        //This is to keep remove hability on requestGraphicsBuffer event
        Dictionary<RequestAccessDelegate, HDCameraExtension.RequestAccessDelegate> m_Refs_requestGraphicsBuffer = new Dictionary<RequestAccessDelegate, HDCameraExtension.RequestAccessDelegate>();
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>RequestAccessDelegate used to request access to various buffers of this camera.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.RequestAccessDelegate instead.")]
        public event RequestAccessDelegate requestGraphicsBuffer
        {
            add
            {
                //We need to register the transformed call (see m_Refs_requestGraphicsBuffer) to be able to remove it later.
                //The transformation convert data between HDAdditionalCameraData.BufferAccess and HDCameraExtension.BufferAccess to call extension event with old BufferAccess format.
                extension.requestGraphicsBuffer += (m_Refs_requestGraphicsBuffer[value] = new HDCameraExtension.RequestAccessDelegate(
                    (ref HDCameraExtension.BufferAccess bufferAccess) =>
                    {
                        BufferAccess oldFormat = (BufferAccess)bufferAccess;
                        value(ref oldFormat);
                        bufferAccess.bufferAccess = (HDCameraExtension.BufferAccess.BufferAccessType)oldFormat.bufferAccess;
                    }));
            }
            remove
            {
                if (!m_Refs_requestGraphicsBuffer.ContainsKey(value))
                    return;
                extension.requestGraphicsBuffer -= m_Refs_requestGraphicsBuffer[value];
                m_Refs_requestGraphicsBuffer.Remove(value);
            }
        }

        /// <summary>The object used as a target for centering the Exposure's Procedural Mask metering mode when target object option is set (See Exposure Volume Component).</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.exposureTarget instead.")]
        public GameObject exposureTarget
        {
            get => extension.exposureTarget;
            set => extension.exposureTarget = value;
        }

        /// <summary>Mask specifying which frame settings are overridden when using custom frame settings.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.renderingPathCustomFrameSettingsOverrideMask instead.")]
        public FrameSettingsOverrideMask renderingPathCustomFrameSettingsOverrideMask
        {
            get => extension.renderingPathCustomFrameSettingsOverrideMask;
            set => extension.renderingPathCustomFrameSettingsOverrideMask = value;
        }
        /// <summary>When using default frame settings, specify which type of frame settings to use.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.defaultFrameSettings instead.")]
        public FrameSettingsRenderType defaultFrameSettings
        {
            get => extension.defaultFrameSettings;
            set => extension.defaultFrameSettings = value;
        }
        /// <summary>Custom frame settings.</summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.renderingPathCustomFrameSettings instead.")]
        public ref FrameSettings renderingPathCustomFrameSettings
            => ref extension.renderingPathCustomFrameSettings;

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
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.SetAOVRequests instead.")]
        public void SetAOVRequests(AOVRequestDataCollection aovRequests)
            => extension.SetAOVRequests(aovRequests);

        /// <summary>
        /// Use this property to get the aov requests.
        ///
        /// It is never null.
        /// </summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.aovRequests instead.")]
        public IEnumerable<AOVRequestData> aovRequests
            => extension.aovRequests;

        // When we are a preview, there is no way inside Unity to make a distinction between camera preview and material preview.
        // This property allow to say that we are an editor camera preview when the type is preview.
        /// <summary>
        /// Unity support two type of preview: Camera preview and material preview. This property allow to know that we are an editor camera preview when the type is preview.
        /// </summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.isEditorCameraPreview instead.")]
        public bool isEditorCameraPreview
            => extension.isEditorCameraPreview;

        // This is use to copy data into camera for the Reset() workflow in camera editor
        /// <summary>
        /// Copy HDAdditionalCameraData.
        /// </summary>
        /// <param name="data">Component to copy to.</param>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.CopyTo instead.")]
        public void CopyTo(HDAdditionalCameraData data)
            => extension.CopyTo(data.extension);

        // For custom projection matrices
        // Set the proper getter
        /// <summary>
        /// Specify a custom getter for non oblique projection matrix.
        /// </summary>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.nonObliqueProjectionGetter instead.")]
        public NonObliqueProjectionGetter nonObliqueProjectionGetter
        {
            get => new NonObliqueProjectionGetter(extension.nonObliqueProjectionGetter);
            set => extension.nonObliqueProjectionGetter = new HDCameraExtension.NonObliqueProjectionGetter(value);
        }

        /// <summary>
        /// Returns the non oblique projection matrix for this camera.
        /// </summary>
        /// <param name="camera">Requested camera.</param>
        /// <returns>The non oblique projection matrix for this camera.</returns>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.GetNonObliqueProjection instead.")]
        public Matrix4x4 GetNonObliqueProjection(Camera camera)
            => extension.GetNonObliqueProjection(camera);

        /// <summary>
        /// Returns the requested graphics buffer.
        /// Users should use the requestGraphicsBuffer event to make sure that the required buffers are requested first.
        /// Note that depending on the current frame settings some buffers may not be available.
        /// </summary>
        /// <param name="type">Type of the requested buffer.</param>
        /// <returns>Requested buffer as a RTHandle. Can be null if the buffer is not available.</returns>
        [Obsolete("Use UnityEngine.Rendering.HighDefinition.HDCameraExtension.GetGraphicsBuffer instead.")]
        public RTHandle GetGraphicsBuffer(BufferAccessType type)
            => extension.GetGraphicsBuffer((HDCameraExtension.BufferAccess.BufferAccessType)type);
    }
}
