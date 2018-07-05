namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class LightweightAdditionalCameraData : MonoBehaviour
    {
        [Tooltip("If enabled shadows will render for this camera.")]
        public bool renderShadows = true;

        [Tooltip("If enabled depth texture will render for this camera bound as _CameraDepthTexture.")]
        public bool requiresDepthTexture = false;

        [Tooltip("If enabled opaque color texture will render for this camera and bound as _CameraOpaqueTexture.")]
        public bool requiresColorTexture = false;

        [HideInInspector]
        [SerializeField]
        float m_Version = 1;

        public float version
        {
            get { return m_Version; }
        }
    }
}
