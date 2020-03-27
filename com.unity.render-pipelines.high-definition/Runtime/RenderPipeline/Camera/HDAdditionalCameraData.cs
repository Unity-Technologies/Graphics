using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Holds the physical settings set on cameras.
    /// </summary>
    [Serializable]
    public class HDPhysicalCamera
    {
        /// <summary>
        /// The minimum allowed aperture.
        /// </summary>
        public const float kMinAperture = 1f;

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
        [SerializeField] [Min(1f)] int m_Iso = 200;
        [SerializeField] [Min(0f)] float m_ShutterSpeed = 1f / 200f;

        // Lens
        // Note: focalLength is already defined in the regular camera component
        [SerializeField] [Range(kMinAperture, kMaxAperture)] float m_Aperture = 16f;

        // Aperture shape
        [SerializeField] [Range(kMinBladeCount, kMaxBladeCount)] int m_BladeCount = 5;
        [SerializeField] Vector2 m_Curvature = new Vector2(2f, 11f);
        [SerializeField] [Range(0f, 1f)] float m_BarrelClipping = 0.25f;
        [SerializeField] [Range(-1f, 1f)] float m_Anamorphism = 0f;

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
            set => m_Aperture = Mathf.Clamp(value, kMinAperture, kMaxAperture);
        }

        /// <summary>
        /// The number of diaphragm blades.
        /// </summary>
        public int bladeCount
        {
            get => m_BladeCount;
            set => m_BladeCount = Mathf.Clamp(value, kMinBladeCount, kMaxBladeCount);
        }

        /// <summary>
        /// Maps an aperture range to blade curvature.
        /// </summary>
        public Vector2 curvature
        {
            get => m_Curvature;
            set
            {
                m_Curvature.x = Mathf.Max(value.x, kMinAperture);
                m_Curvature.y = Mathf.Min(value.y, kMaxAperture);
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
        public void CopyTo(HDPhysicalCamera c)
        {
            c.iso = iso;
            c.shutterSpeed = shutterSpeed;
            c.aperture = aperture;
            c.bladeCount = bladeCount;
            c.curvature = curvature;
            c.barrelClipping = barrelClipping;
            c.anamorphism = anamorphism;
        }
    }

    /// <summary>
    /// Additional component that holds HDRP specific parameters for Cameras.
    /// </summary>
    [HelpURL(Documentation.baseURL + Documentation.releaseVersion + Documentation.subURL + "HDRP-Camera" + Documentation.endURL)]
    [DisallowMultipleComponent, ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public partial class HDAdditionalCameraData : MonoBehaviour, IFrameSettingsHistoryContainer
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
        public float taaSharpenStrength = 0.6f;

        /// <summary>Physical camera parameters.</summary>
        public HDPhysicalCamera physicalParameters = new HDPhysicalCamera();

        /// <summary>Vertical flip mode.</summary>
        public FlipYMode flipYMode;

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

        internal float probeCustomFixedExposure = 1.0f;

        [SerializeField, FormerlySerializedAs("renderingPathCustomFrameSettings")]
        FrameSettings m_RenderingPathCustomFrameSettings = FrameSettings.NewDefaultCamera();
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

        internal ProfilingSampler profilingSampler;

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
        bool m_IsDebugRegistered = false;
        string m_CameraRegisterName;

        // When we are a preview, there is no way inside Unity to make a distinction between camera preview and material preview.
        // This property allow to say that we are an editor camera preview when the type is preview.
        /// <summary>
        /// Unity support two type of preview: Camera preview and material preview. This property allow to know that we are an editor camera preview when the type is preview.
        /// </summary>
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
            physicalParameters.CopyTo(data.physicalParameters);

            data.renderingPathCustomFrameSettings = renderingPathCustomFrameSettings;
            data.renderingPathCustomFrameSettingsOverrideMask = renderingPathCustomFrameSettingsOverrideMask;
            data.defaultFrameSettings = defaultFrameSettings;

            data.probeCustomFixedExposure = probeCustomFixedExposure;

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
            // Be sure legacy HDR option is disable on camera as it cause banding in SceneView. Yes, it is a contradiction, but well, Unity...
            // When HDR option is enabled, Unity render in FP16 then convert to 8bit with a stretch copy (this cause banding as it should be convert to sRGB (or other color appropriate color space)), then do a final shader with sRGB conversion
            // When LDR, unity render in 8bitSRGB, then do a final shader with sRGB conversion
            // What should be done is just in our Post process we convert to sRGB and store in a linear 10bit, but require C++ change...
            m_Camera = GetComponent<Camera>();
            if (m_Camera == null)
                return;

            m_Camera.allowMSAA = false; // We don't use this option in HD (it is legacy MSAA) and it produce a warning in the inspector UI if we let it
            m_Camera.allowHDR = false;

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
                return  hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);
            else if ((type & BufferAccessType.Depth) != 0)
                return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
            else if ((type & BufferAccessType.Normal) != 0)
                return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal);
            else
                return null;
        }
    }
}
