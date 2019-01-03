using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [ImageEffectAllowedInSceneView]
    public class AdditionalCameraData : MonoBehaviour
    {
        [Tooltip("If enabled shadows will render for this camera.")]
        [FormerlySerializedAs("renderShadows"), SerializeField] bool m_RenderShadows = true;

        [Tooltip("If enabled depth texture will render for this camera bound as _CameraDepthTexture.")]
        [FormerlySerializedAs("requiresDepthTexture"), SerializeField] bool m_RequiresDepthTexture = false;

        [Tooltip("If enabled opaque color texture will render for this camera and bound as _CameraOpaqueTexture.")]
        [FormerlySerializedAs("requiresColorTexture"), SerializeField] bool m_RequiresColorTexture = false;

        [HideInInspector]
        [SerializeField]
        float m_Version = 1;

        public float version
        {
            get { return m_Version; }
        }

        public bool renderShadows
        {
            get { return m_RenderShadows; }
            set { m_RenderShadows = value; }
        }

        public bool requiresDepthTexture
        {
            get { return m_RequiresDepthTexture; }
            set { m_RequiresDepthTexture = value; }
        }

        public bool requiresColorTexture
        {
            get { return m_RequiresColorTexture; }
            set { m_RequiresColorTexture = value; }
        }
    }
}
