using System;
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
            TemporalAntialiasing
        }

        public ClearColorMode clearColorMode = ClearColorMode.Sky;
        [ColorUsage(true, true)]
        public Color backgroundColorHDR = new Color(0.025f, 0.07f, 0.19f, 0.0f);
        public bool clearDepth = true;
        

        [Tooltip("LayerMask HDRP uses for Volume interpolation for this Camera.")]
        public LayerMask volumeLayerMask = -1;

        public Transform volumeAnchorOverride;

        public AntialiasingMode antialiasing = AntialiasingMode.None;
        public bool dithering = false;

        // Physical parameters
        public HDPhysicalCamera physicalParameters = new HDPhysicalCamera();

        public FlipYMode flipYMode;

        [Tooltip("Skips rendering settings to directly render in fullscreen (Useful for video).")]
        public bool fullscreenPassthrough = false;

        [Tooltip("Allows you to override the default settings for this Renderer.")]
        public bool customRenderingSettings = false;

        public bool invertFaceCulling = false;

        public LayerMask probeLayerMask = ~0;

        // Event used to override HDRP rendering for this particular camera.
        public event Action<ScriptableRenderContext, HDCamera> customRender;
        public bool hasCustomRender { get { return customRender != null; } }
        
        public FrameSettings renderingPathCustomFrameSettings = FrameSettings.defaultCamera;
        public FrameSettingsOverrideMask renderingPathCustomFrameSettingsOverrideMask;
        public FrameSettingsRenderType defaultFrameSettings;

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
