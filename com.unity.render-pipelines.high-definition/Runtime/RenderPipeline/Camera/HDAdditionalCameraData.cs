using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class HDPhysicalCamera
    {
        public const float kMinAperture = 1f;
        public const float kMaxAperture = 32f;
        public const int kMinBladeCount = 3;
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

        // Property binding / validation
        public int iso
        {
            get => m_Iso;
            set => m_Iso = Mathf.Max(value, 1);
        }

        public float shutterSpeed
        {
            get => m_ShutterSpeed;
            set => m_ShutterSpeed = Mathf.Max(value, 0f);
        }

        public float aperture
        {
            get => m_Aperture;
            set => m_Aperture = Mathf.Clamp(value, kMinAperture, kMaxAperture);
        }

        public int bladeCount
        {
            get => m_BladeCount;
            set => m_BladeCount = Mathf.Clamp(value, kMinBladeCount, kMaxBladeCount);
        }

        public Vector2 curvature
        {
            get => m_Curvature;
            set
            {
                m_Curvature.x = Mathf.Max(value.x, kMinAperture);
                m_Curvature.y = Mathf.Min(value.y, kMaxAperture);
            }
        }

        public float barrelClipping
        {
            get => m_BarrelClipping;
            set => m_BarrelClipping = Mathf.Clamp01(value);
        }

        public float anamorphism
        {
            get => m_Anamorphism;
            set => m_Anamorphism = Mathf.Clamp(value, -1f, 1f);
        }

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

    [DisallowMultipleComponent, ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public partial class HDAdditionalCameraData : MonoBehaviour
    {
        public enum FlipYMode
        {
            Automatic,
            ForceFlipY
        }

        // The light culling use standard projection matrices (non-oblique)
        // If the user overrides the projection matrix with an oblique one
        // He must also provide a callback to get the equivalent non oblique for the culling
        public delegate Matrix4x4 NonObliqueProjectionGetter(Camera camera);

        Camera m_camera;

        public enum ClearColorMode
        {
            Sky,
            Color,
            None
        };

        public enum AntialiasingMode
        {
            None,
            FastApproximateAntialiasing,
            TemporalAntialiasing,
            SubpixelMorphologicalAntiAliasing
        }

        public enum SMAAQualityLevel
        {
            Low,
            Medium,
            High
        }


        public ClearColorMode clearColorMode = ClearColorMode.Sky;
        [ColorUsage(true, true)]
        public Color backgroundColorHDR = new Color(0.025f, 0.07f, 0.19f, 0.0f);
        public bool clearDepth = true;


        [Tooltip("LayerMask HDRP uses for Volume interpolation for this Camera.")]
        public LayerMask volumeLayerMask = 1;

        public Transform volumeAnchorOverride;

        public AntialiasingMode antialiasing = AntialiasingMode.None;
        public SMAAQualityLevel SMAAQuality = SMAAQualityLevel.High;
        public bool dithering = false;
        public bool stopNaNs = false;

        // Physical parameters
        public HDPhysicalCamera physicalParameters = new HDPhysicalCamera();

        public FlipYMode flipYMode;

        [Tooltip("Skips rendering settings to directly render in fullscreen (Useful for video).")]
        public bool fullscreenPassthrough = false;

        [Tooltip("Allows dynamic resolution on buffers linked to this camera.")]
        public bool allowDynamicResolution = false;

        [Tooltip("Allows you to override the default settings for this Renderer.")]
        public bool customRenderingSettings = false;

        public bool invertFaceCulling = false;

        public LayerMask probeLayerMask = ~0;

        // Event used to override HDRP rendering for this particular camera.
        public event Action<ScriptableRenderContext, HDCamera> customRender;
        public bool hasCustomRender { get { return customRender != null; } }

        [SerializeField, FormerlySerializedAs("renderingPathCustomFrameSettings")]
        FrameSettings m_RenderingPathCustomFrameSettings = FrameSettings.defaultCamera;
        public FrameSettingsOverrideMask renderingPathCustomFrameSettingsOverrideMask;
        public FrameSettingsRenderType defaultFrameSettings;

        public ref FrameSettings renderingPathCustomFrameSettings => ref m_RenderingPathCustomFrameSettings;

        AOVRequestDataCollection m_AOVRequestDataCollection = new AOVRequestDataCollection(null);

        /// <summary>Set AOV requests to use.</summary>
        /// <param name="aovRequests">Describes the requests to execute.</param>
        /// <example>
        /// <code>
        /// using System.Collections.Generic;
        /// using UnityEngine;
        /// using UnityEngine.Experimental.Rendering;
        /// using UnityEngine.Experimental.Rendering.HDPipeline;
        /// using UnityEngine.Experimental.Rendering.HDPipeline.Attributes;
        ///
        /// [ExecuteAlways]
        /// [RequireComponent(typeof(Camera))]
        /// [RequireComponent(typeof(HDAdditionalCameraData))]
        /// public class SetupAOVCallbacks : MonoBehaviour
        /// {
        ///     private static RTHandleSystem.RTHandle m_ColorRT;
        ///
        ///     [SerializeField] private Texture m_Target;
        ///     [SerializeField] private DebugFullScreen m_DebugFullScreen;
        ///     [SerializeField] private DebugLightFilterMode m_DebugLightFilter;
        ///     [SerializeField] private MaterialSharedProperty m_MaterialSharedProperty;
        ///     [SerializeField] private LightingProperty m_LightingProperty;
        ///     [SerializeField] private AOVBuffers m_BuffersToCopy;
        ///     [SerializeField] private List<GameObject> m_IncludedLights;
        ///
        ///
        ///     void OnEnable()
        ///     {
        ///         var aovRequest = new AOVRequest(AOVRequest.@default)
        ///             .SetLightFilter(m_DebugLightFilter);
        ///         if (m_DebugFullScreen != DebugFullScreen.None)
        ///             aovRequest = aovRequest.SetFullscreenOutput(m_DebugFullScreen);
        ///         if (m_MaterialSharedProperty != MaterialSharedProperty.None)
        ///             aovRequest = aovRequest.SetFullscreenOutput(m_MaterialSharedProperty);
        ///         if (m_LightingProperty != LightingProperty.None)
        ///             aovRequest = aovRequest.SetFullscreenOutput(m_LightingProperty);
        ///
        ///         var add = GetComponent<HDAdditionalCameraData>();
        ///         add.SetAOVRequests(
        ///             new AOVRequestBuilder()
        ///                 .Add(
        ///                     aovRequest,
        ///                     bufferId => m_ColorRT ?? (m_ColorRT = RTHandles.Alloc(512, 512)),
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
        ///         var add = GetComponent<HDAdditionalCameraData>();
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

        public bool IsDebugRegistred()
        {
            return m_IsDebugRegistered;
        }

        // When we are a preview, there is no way inside Unity to make a distinction between camera preview and material preview.
        // This property allow to say that we are an editor camera preview when the type is preview.
        public bool isEditorCameraPreview { get; set; }

        // This is use to copy data into camera for the Reset() workflow in camera editor
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

            // We must not copy the following
            //data.m_IsDebugRegistered = m_IsDebugRegistered;
            //data.m_CameraRegisterName = m_CameraRegisterName;
            //data.isEditorCameraPreview = isEditorCameraPreview;
        }

        // For custom projection matrices
        // Set the proper getter
        public NonObliqueProjectionGetter nonObliqueProjectionGetter = GeometryUtils.CalculateProjectionMatrix;

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
                if (/*m_camera.cameraType != CameraType.Preview &&*/ m_camera.cameraType != CameraType.Reflection)
                {
                    DebugDisplaySettings.RegisterCamera(m_camera, this);
                }
                m_CameraRegisterName = m_camera.name;
                m_IsDebugRegistered = true;
            }
        }

        void UnRegisterDebug()
        {
            if (m_camera == null)
                return;

            if (m_IsDebugRegistered)
            {
                // Note camera's preview camera is registered with preview type but then change to game type that lead to issue.
                // Do not attempt to not register them till this issue persist.
                if (/*m_camera.cameraType != CameraType.Preview &&*/ m_camera.cameraType != CameraType.Reflection)
                {
                    DebugDisplaySettings.UnRegisterCamera(m_camera, this);
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
            m_camera = GetComponent<Camera>();
            if (m_camera == null)
                return;

            m_camera.allowMSAA = false; // We don't use this option in HD (it is legacy MSAA) and it produce a warning in the inspector UI if we let it
            m_camera.allowHDR = false;

            RegisterDebug();
        }

        void Update()
        {
            // We need to detect name change in the editor and update debug windows accordingly
#if UNITY_EDITOR
            // Caution: Object.name generate 48B of garbage at each frame here !
            if (m_camera.name != m_CameraRegisterName)
            {
                UnRegisterDebug();
                RegisterDebug();
            }
#endif
        }

        void OnDisable()
        {
            UnRegisterDebug();
        }

        // This is called at the creation of the HD Additional Camera Data, to convert the legacy camera settings to HD
        public static void InitDefaultHDAdditionalCameraData(HDAdditionalCameraData cameraData)
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

        public void ExecuteCustomRender(ScriptableRenderContext renderContext, HDCamera hdCamera)
        {
            if (customRender != null)
            {
                customRender(renderContext, hdCamera);
            }
        }
    }
}
