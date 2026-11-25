using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

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
        Material m_DepthBandsMaterial;

        public OutputURPTexturePass(TextureType textureType, Material depthBandsMaterial)
        {
            m_TextureType = textureType;
            m_DepthBandsMaterial = depthBandsMaterial;
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

            var blitParameters = new RenderGraphUtils.BlitMaterialParameters(sourceTexture, resourceData.activeColorTexture, m_DepthBandsMaterial, 0);
            renderGraph.AddBlitPass(blitParameters, passName);
        }
    }

    [SerializeField]
    TextureType m_TextureType;
    [SerializeField]
    Shader m_DepthBandsShader;
    Material m_DepthBandsMaterial;
    OutputURPTexturePass m_ScriptablePass;

    public override void Create()
    {
        m_DepthBandsMaterial = CoreUtils.CreateEngineMaterial(m_DepthBandsShader);
        m_ScriptablePass = new OutputURPTexturePass(m_TextureType, m_DepthBandsMaterial);
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_DepthBandsMaterial);
    }
}
