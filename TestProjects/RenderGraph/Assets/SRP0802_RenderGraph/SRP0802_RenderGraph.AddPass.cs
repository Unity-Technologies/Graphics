using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

// PIPELINE ADD PASS --------------------------------------------------------------------------------------------
// This pass does a image effect that Albedo + Emission = final color
public partial class SRP0802_RenderGraph
{
    Material m_material;

    class SRP0802_AddPassData
    {
        public TextureHandle m_Albedo;
        public TextureHandle m_Emission;
    }

    public void Render_SRP0802_AddPass(RenderGraph graph, TextureHandle albedo, TextureHandle emission)
    {
        if(m_material == null) m_material = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/CustomSRP/SRP0802_RenderGraph/FinalColor"));

        using (var builder = graph.AddRenderPass<SRP0802_AddPassData>("Add Pass", out var passData, new ProfilingSampler("Add Pass Profiler" ) ) )
        {
            //Textures
            passData.m_Albedo = builder.ReadTexture(albedo);
            passData.m_Emission = builder.ReadTexture(emission);
            
            //Builder
            builder.SetRenderFunc((SRP0802_AddPassData data, RenderGraphContext context) => 
            {
                m_material.SetTexture("_CameraAlbedoTexture",data.m_Albedo);
                m_material.SetTexture("_CameraEmissionTexture",data.m_Emission);
                context.cmd.Blit( null, BuiltinRenderTextureType.CameraTarget, m_material );
            });
        }
    }
}
