using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [DisallowMultipleComponent, ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class HDAdditionalCameraData : MonoBehaviour, ISerializationCallbackReceiver
    {
#pragma warning disable 414 // CS0414 The private field '...' is assigned but its value is never used
        // We can't rely on Unity for our additional data, we need to version it ourself.
        [SerializeField]
        float m_Version = 1.0f;
#pragma warning restore 414

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

        public RenderingPath    renderingPath;
        [Tooltip("Layer Mask used for the volume interpolation for this camera.")]
        public LayerMask        volumeLayerMask = -1;

        // To be able to turn on/off FrameSettings properties at runtime for debugging purpose without affecting the original one
        // we create a runtime copy (m_ActiveFrameSettings that is used, and any parametrization is done on serialized frameSettings)
        [SerializeField]
        [FormerlySerializedAs("serializedFrameSettings")]
        FrameSettings    m_FrameSettings = new FrameSettings(); // Serialize frameSettings

        // Not serialized, not visible
        FrameSettings m_FrameSettingsRuntime = new FrameSettings();
        public FrameSettings GetFrameSettings()
        {
            return m_FrameSettingsRuntime;
        }

        bool    m_IsDebugRegistered = false;
        Camera  m_camera;
        string  m_CameraRegisterName;

        void RegisterDebug()
        {
            if (!m_IsDebugRegistered)
            {
                FrameSettings.RegisterDebug(m_camera.name, GetFrameSettings());
                m_CameraRegisterName = m_camera.name;
                m_IsDebugRegistered = true;
            }
        }

        void UnRegisterDebug()
        {
            if (m_IsDebugRegistered)
            {
                FrameSettings.UnRegisterDebug(m_CameraRegisterName);
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

            m_FrameSettings.CopyTo(m_FrameSettingsRuntime);

            RegisterDebug();
        }

        void Update()
        {
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
            // We do a copy of the settings to those effectively used
            m_FrameSettings.CopyTo(m_FrameSettingsRuntime);
        }
    }
}
