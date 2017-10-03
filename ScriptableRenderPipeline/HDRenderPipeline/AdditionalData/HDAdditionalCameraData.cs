namespace UnityEngine.Experimental.Rendering
{
    // This struct allow to add specialized path in HDRenderPipeline (can be use to render mini map or planar reflection etc...)
    public enum RenderingPathHDRP { Default, Unlit };

    [DisallowMultipleComponent, ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class HDAdditionalCameraData : MonoBehaviour
    {
        public RenderingPathHDRP renderingPath;
    }
}
