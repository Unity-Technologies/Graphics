namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [DisallowMultipleComponent, ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class HDAdditionalCameraData : MonoBehaviour
    {
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

        // To be able to turn on/off FrameSettings properties at runtime for debugging purpose without affecting the original one
        // we create a runtime copy (m_ActiveFrameSettings that is used, and any parametrization is done on serialized frameSettings)
        public FrameSettings serializedFrameSettings = new FrameSettings(); // Serialize frameSettings

        // Not serialized, not visible
        FrameSettings m_FrameSettings = new FrameSettings();
        public FrameSettings GetFrameSettings()
        {
            return m_FrameSettings;
        }

        bool isRegisterDebug = false;
        Camera m_camera;
        string m_CameraRegisterName;

        void RegisterDebug()
        {
            if (!isRegisterDebug)
            {
                FrameSettings.RegisterDebug(m_camera.name, GetFrameSettings());
                m_CameraRegisterName = m_camera.name;
                isRegisterDebug = true;
            }
        }

        void UnRegisterDebug()
        {
            if (isRegisterDebug)
            {
                FrameSettings.UnRegisterDebug(m_CameraRegisterName);
                isRegisterDebug = false;
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

            serializedFrameSettings.CopyTo(m_FrameSettings);

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

        void OnValidate()
        {
            // Modification of frameSettings in the inspector will call OnValidate().
            // We do a copy of the settings to those effectively used
            serializedFrameSettings.CopyTo(m_FrameSettings);
        }

        void OnDisable()
        {
            UnRegisterDebug();
        }
    }
}
