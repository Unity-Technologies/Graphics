namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // This struct allow to add specialized path in HDRenderPipeline (can be use to render mini map or planar reflection etc...)
    public enum RenderingPathHDRP { Default, Unlit };

    [DisallowMultipleComponent, ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class HDAdditionalCameraData : MonoBehaviour
    {
        public RenderingPathHDRP renderingPath;

        Camera m_camera;

        void OnEnable()
        {
            // Be sure legacy HDR option is disable on camera as it cause banding in SceneView. Yes, it is a contradiction, but well, Unity...
            // When HDR option is enabled, Unity render in FP16 then convert to 8bit with a stretch copy (this cause banding as it should be convert to sRGB (or other color appropriate color space)), then do a final shader with sRGB conversion
            // When LDR, unity render in 8bitSRGB, then do a final shader with sRGB conversion 
            // What should be done is just in our Post process we convert to sRGB and store in a linear 10bit, but require C++ change...
            m_camera = GetComponent<Camera>();
            m_camera.allowHDR = false;
        }
    }
}
