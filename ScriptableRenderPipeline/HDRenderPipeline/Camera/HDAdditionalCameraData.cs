namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [DisallowMultipleComponent, ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class HDAdditionalCameraData : MonoBehaviour
    {
        // This struct allow to add specialized path in HDRenderPipeline (can be use to render mini map or planar reflection etc...)
        public enum RenderingPath
        {
            Default,
            Unlit,  // Preset
            Custom  // Fine grained
        };

        public RenderingPath    renderingPath;

        // To be able to turn on/off FrameSettings properties at runtime for debugging purpose without affecting the original one
        // we create a runtime copy (m_effectiveFrameSettings that is used, and any parametrization is done on serialized frameSettings)
        public FrameSettings frameSettings = new FrameSettings(); // Serialize frameSettings

        // Not serialized, not visible
        FrameSettings m_effectiveFrameSettings = new FrameSettings();
        public FrameSettings GetEffectiveFrameSettings()
        {
            return m_effectiveFrameSettings;
        }

        bool isRegisterDebug = false;
        Camera m_camera;

        void OnEnable()
        {
            // Be sure legacy HDR option is disable on camera as it cause banding in SceneView. Yes, it is a contradiction, but well, Unity...
            // When HDR option is enabled, Unity render in FP16 then convert to 8bit with a stretch copy (this cause banding as it should be convert to sRGB (or other color appropriate color space)), then do a final shader with sRGB conversion
            // When LDR, unity render in 8bitSRGB, then do a final shader with sRGB conversion
            // What should be done is just in our Post process we convert to sRGB and store in a linear 10bit, but require C++ change...
            m_camera = GetComponent<Camera>();
            m_camera.allowHDR = false;

            frameSettings.CopyTo(m_effectiveFrameSettings);

            if (!isRegisterDebug)
            {
                FrameSettings.RegisterDebug(m_camera.name, GetEffectiveFrameSettings());
                isRegisterDebug = true;
            }
        }
    }
}
