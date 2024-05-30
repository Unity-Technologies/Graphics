using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using System;

public class OutputURPTexture : ScriptableRendererFeature
{
    [Serializable]
    enum TextureType
    {
        MainLightShadows,
        AddtionalLightShadows,
    }

    class OutputURPTexturePass : ScriptableRenderPass
    {
        TextureType m_TextureType;
        public OutputURPTexturePass(TextureType textureType)
        {
            m_TextureType = textureType;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "Output Texture Pass";
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle sourceTexture;
            switch (m_TextureType)
            {
                case TextureType.MainLightShadows:
                    sourceTexture = resourceData.mainShadowsTexture;
                    break;
                case TextureType.AddtionalLightShadows:
                    sourceTexture = resourceData.additionalShadowsTexture;
                    break;
                default:
                    throw new ArgumentException("Doesn't handle texture type please exstend function to include the type.", "m_TextureType");
            }
            RenderGraphUtils.AddBlitPass(renderGraph, sourceTexture, resourceData.activeColorTexture, Vector2.one, Vector2.zero, passName: passName);
            
        }
    }

    [SerializeField]
    TextureType m_TextureType;
    OutputURPTexturePass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new OutputURPTexturePass(m_TextureType);
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}
