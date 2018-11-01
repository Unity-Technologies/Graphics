using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public enum CameraOverrideOption
    {
        Off,
        On,
        UsePipelineSettings,
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [ImageEffectAllowedInSceneView]
    public class LWRPAdditionalCameraData : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Tooltip("If enabled shadows will render for this camera.")]
        [FormerlySerializedAs("renderShadows"), SerializeField]
        bool m_RenderShadows = true;

        [Tooltip("If enabled depth texture will render for this camera bound as _CameraDepthTexture.")]
        [SerializeField]
        CameraOverrideOption m_RequiresDepthTextureOption = CameraOverrideOption.UsePipelineSettings;

        [Tooltip("If enabled opaque color texture will render for this camera and bound as _CameraOpaqueTexture.")]
        [SerializeField]
        CameraOverrideOption m_RequiresOpaqueTextureOption = CameraOverrideOption.UsePipelineSettings;

        // Deprecated:
        [FormerlySerializedAs("requiresDepthTexture"), SerializeField]
        bool m_RequiresDepthTexture = false;

        [FormerlySerializedAs("requiresColorTexture"), SerializeField]
        bool m_RequiresColorTexture = false;

        [HideInInspector] [SerializeField] float m_Version = 2;

        public float version
        {
            get { return m_Version; }
        }

        public bool renderShadows
        {
            get { return m_RenderShadows; }
            set { m_RenderShadows = value; }
        }

        public CameraOverrideOption requiresDepthOption
        {
            get { return m_RequiresDepthTextureOption; }
            set { m_RequiresDepthTextureOption = value; }
        }

        public CameraOverrideOption requiresColorOption
        {
            get { return m_RequiresOpaqueTextureOption; }
            set { m_RequiresOpaqueTextureOption = value; }
        }

        public bool requiresDepthTexture
        {
            get
            {
                if (m_RequiresDepthTextureOption == CameraOverrideOption.UsePipelineSettings)
                {
                    LightweightRenderPipelineAsset asset = GraphicsSettings.renderPipelineAsset as LightweightRenderPipelineAsset;
                    return asset.supportsCameraDepthTexture;
                }
                else
                {
                    return m_RequiresDepthTextureOption == CameraOverrideOption.On;
                }
            }
            set { m_RequiresDepthTextureOption = (value) ? CameraOverrideOption.On : CameraOverrideOption.Off; }
        }

        public bool requiresColorTexture
        {
            get
            {
                if (m_RequiresOpaqueTextureOption == CameraOverrideOption.UsePipelineSettings)
                {
                    LightweightRenderPipelineAsset asset = GraphicsSettings.renderPipelineAsset as LightweightRenderPipelineAsset;
                    return asset.supportsCameraOpaqueTexture;
                }
                else
                {
                    return m_RequiresOpaqueTextureOption == CameraOverrideOption.On;
                }
            }
            set { m_RequiresOpaqueTextureOption = (value) ? CameraOverrideOption.On : CameraOverrideOption.Off; }
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (version <= 1)
            {
                m_RequiresDepthTextureOption = (m_RequiresDepthTexture) ? CameraOverrideOption.On : CameraOverrideOption.Off;
                m_RequiresOpaqueTextureOption = (m_RequiresColorTexture) ? CameraOverrideOption.On : CameraOverrideOption.Off;
            }
        }
    }
}
