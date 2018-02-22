using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [DisallowMultipleComponent, ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class HDAdditionalCameraData : MonoBehaviour, ISerializationCallbackReceiver
    {
        // The light culling use standard projection matrices (non-oblique)
        // If the user overrides the projection matrix with an oblique one
        // He must also provide a callback to get the equivalent non oblique for the culling
        public delegate Matrix4x4 NonObliqueProjectionGetter(Camera camera);

#pragma warning disable 414 // CS0414 The private field '...' is assigned but its value is never used
        // We can't rely on Unity for our additional data, we need to version it ourself.
        [SerializeField]
        float m_Version = 1.0f;
#pragma warning restore 414

        Camera m_camera;

        // This struct allow to add specialized path in HDRenderPipeline (can be use to render mini map or planar reflection etc...)
        // A rendering path is the list of rendering pass that will be executed at runtime and depends on the associated FrameSettings
        // Default is the default rendering path define by the HDRendeRPipelineAsset FrameSettings.
        // Custom allow users to define the FrameSettigns for this path
        // Then enum can contain either preset of FrameSettings or hard coded path
        // Unlit below is a hard coded path (a path that can't be implemented only with FrameSettings)
        public enum RenderingPath
        {
            Default,
            Custom,  // Fine grained
            Unlit  // Hard coded path
        };

        public enum ClearColorMode
        {
            Sky,
            BackgroundColor,
            None
        };

        public ClearColorMode clearColorMode = ClearColorMode.Sky;
        [ColorUsage(true, true)]
        public Color backgroundColorHDR = new Color(0.025f, 0.07f, 0.19f, 0.0f);
        public bool clearDepth = true;

        public RenderingPath    renderingPath;
        [Tooltip("Layer Mask used for the volume interpolation for this camera.")]
        public LayerMask        volumeLayerMask = -1;

        // Physical parameters
        public float aperture = 8f;
        public float shutterSpeed = 1f / 200f;
        public float iso = 400f;

        // To be able to turn on/off FrameSettings properties at runtime for debugging purpose without affecting the original one
        // we create a runtime copy (m_ActiveFrameSettings that is used, and any parametrization is done on serialized frameSettings)
        [SerializeField]
        [FormerlySerializedAs("serializedFrameSettings")]
        FrameSettings    m_FrameSettings = new FrameSettings(); // Serialize frameSettings

        // Not serialized, visible only in the debug windows
        FrameSettings m_FrameSettingsRuntime = new FrameSettings();

        bool m_frameSettingsIsDirty = true;

        // Use for debug windows
        // When camera name change we need to update the name in DebugWindows.
        // This is the purpose of this class
        bool    m_IsDebugRegistered = false;
        string  m_CameraRegisterName;

        // This is the function use outside to access FrameSettings. It return the current state of FrameSettings for the camera
        // taking into account the customization via the debug menu
        public FrameSettings GetFrameSettings()
        {
            return m_FrameSettingsRuntime;
        }

        // This function is call at the beginning of camera loop in HDRenderPipeline.Render()
        // It allow to correctly init the m_FrameSettingsRuntime to use.
        // If the camera use defaultFrameSettings it must be copied in m_FrameSettingsRuntime
        // otherwise it is the serialized m_FrameSettings that are used
        // This is required so each camera have its own debug settings even if they all use the RenderingPath.Default path
        // and important at Runtime as Default Camera from Scene Preview doesn't exist
        // assetFrameSettingsIsDirty is the current dirty frame settings state of HDRenderPipelineAsset
        // if it is dirty and camera use RenderingPath.Default, we need to update it
        // defaultFrameSettings are the settings store in the HDRenderPipelineAsset
        public void UpdateDirtyFrameSettings(bool assetFrameSettingsIsDirty, FrameSettings defaultFrameSettings)
        {
            if (m_frameSettingsIsDirty || assetFrameSettingsIsDirty)
            {
                // We do a copy of the settings to those effectively used
                if (renderingPath == RenderingPath.Default)
                {
                    defaultFrameSettings.CopyTo(m_FrameSettingsRuntime);
                }
                else
                {
                    m_FrameSettings.CopyTo(m_FrameSettingsRuntime);
                }

                m_frameSettingsIsDirty = false;
            }
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
                // Note that we register m_FrameSettingsRuntime, so manipulating it in the Debug windows
                // doesn't affect the serialized version
                if (m_camera.cameraType != CameraType.Preview && m_camera.cameraType != CameraType.Reflection)
                {
                    FrameSettings.RegisterDebug(m_camera.name, GetFrameSettings());
                }
                m_CameraRegisterName = m_camera.name;
                m_IsDebugRegistered = true;
            }
        }

        void UnRegisterDebug()
        {
            if (m_IsDebugRegistered)
            {
                if (m_camera.cameraType != CameraType.Preview && m_camera.cameraType != CameraType.Reflection)
                {
                    FrameSettings.UnRegisterDebug(m_CameraRegisterName);
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
            m_camera.allowHDR = false;

            //  Tag as dirty so frameSettings are correctly initialize at next HDRenderPipeline.Render() call
            m_frameSettingsIsDirty = true;

            RegisterDebug();
        }

        void Update()
        {
            // We need to detect name change in the editor and update debug windows accordingly
#if UNITY_EDITOR
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

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            // This is call on load or when this settings are change.
            // When FrameSettings are manipulated or RenderPath change we reset them to reflect the change, discarding all the Debug Windows change.
            // Tag as dirty so frameSettings are correctly initialize at next HDRenderPipeline.Render() call
            m_frameSettingsIsDirty = true;
        }
    }
}
