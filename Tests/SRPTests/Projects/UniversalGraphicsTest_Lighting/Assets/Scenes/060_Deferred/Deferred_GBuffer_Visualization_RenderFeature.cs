using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class Deferred_GBuffer_Visualization_RenderFeature : ScriptableRendererFeature
{
    public Shader visualizeShader;
    Material m_Material;

    static readonly string[] s_ShaderKeywordNames =
    {
        "GBUFFER_0",
        "GBUFFER_1",
        "GBUFFER_2",
        "GBUFFER_ALPHA_A",
        "GBUFFER_ALPHA_B",
        "GBUFFER_RL",
        "GBUFFER_SM",
    };

    LocalKeyword[] m_ShaderKeywords;

    class CustomRenderPass : ScriptableRenderPass
    {
        internal Material m_Material;
        internal LocalKeyword[] m_ShaderKeywords;

        public void Setup(Material material,  LocalKeyword[] shaderKeywords)
        {
            m_Material = material;
            m_ShaderKeywords = shaderKeywords;
        }

        static void ExecutePass(RasterCommandBuffer cmd, Material material, Vector2Int targetSize, LocalKeyword[] shaderKeywords)
        {
            if (material == null)
                return;

            const int Width = 3;
            const int Height = 3;

            int tileWidthPixel = targetSize.x / Width;
            int tileHeightPixel = targetSize.y / Height;

            LocalKeyword gBufferKeywordOld = shaderKeywords[shaderKeywords.Length - 1];

            for (int y = 0; y < Height; ++y)
            {
                int yCoord = tileHeightPixel * y;

                for (int x = 0; x < Width; ++x)
                {
                    int gBufferIdx = y * Width + x;
                    int xCoord = tileWidthPixel * x;

                    cmd.SetKeyword(material, gBufferKeywordOld, false);

                    if (gBufferIdx < shaderKeywords.Length)
                    {
                        LocalKeyword gBufferKeyword = shaderKeywords[gBufferIdx];
                        cmd.SetKeyword(material, gBufferKeyword, true);
                        gBufferKeywordOld = gBufferKeyword;
                    }

                    cmd.SetViewport(new Rect(xCoord, yCoord, tileWidthPixel, tileHeightPixel));
                    cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3, 1);
                }
            }
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        private class PassData
        {
            internal Material material;
            internal Vector2Int targetSize;
            internal TextureHandle[] gBufferHandles;
            internal LocalKeyword[] shaderKeywords;
        }

        private static readonly int s_GbufferLightingIndex = 3; // _GBuffer3 is the activeColorTexture

        private static readonly string[] s_GBufferTexBindslotNames =
        {
            "_GBuffer0",
            "_GBuffer1",
            "_GBuffer2",
            "", // Unused, color target
            "_CameraDepthTexture",
            "_GBuffer4",
            "_GBuffer5",
            "_GBuffer6"
        };

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Test GBuffer visualization.", out var passData))
            {
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.WriteAll);

                passData.gBufferHandles = resourceData.gBuffer;

                // We cannot use the input attachment path here, because we do not want fbfetch
                // so instead, we will fake the non fbfetch path even tho it's available.
                // Basically, we're emulating compatibility (no NRP) mode here.
                for (int i = 0; i < resourceData.gBuffer.Length; i++)
                {
                    if (i == s_GbufferLightingIndex)
                        continue;

                    builder.UseTexture(resourceData.gBuffer[i]);
                }

                passData.material = m_Material;
                passData.targetSize = resourceData.activeColorTexture.GetDescriptor(renderGraph).CalculateFinalDimensions();
                passData.shaderKeywords = m_ShaderKeywords;

                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(false, true, Color.yellow);

                    for (int i = 0; i < data.gBufferHandles.Length; i++)
                    {
                        if (i == s_GbufferLightingIndex)
                            continue;

                        context.cmd.SetGlobalTexture(s_GBufferTexBindslotNames[i], data.gBufferHandles[i]); // Bind as normal textures
                    }

                    ExecutePass(context.cmd, data.material, data.targetSize, data.shaderKeywords);
                });
            }
        }
    }

    CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass {
            // Configures where the render pass should be injected.
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents,
        };
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!GetMaterials())
        {
            Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.", GetType().Name, name);
            return;
        }

        m_ScriptablePass.Setup(m_Material, m_ShaderKeywords);
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);
    }

    bool GetMaterials()
    {
        if (m_Material == null && visualizeShader != null)
        {
            m_Material = CoreUtils.CreateEngineMaterial(visualizeShader);
        }

        if (m_ShaderKeywords == null)
        {
            m_ShaderKeywords = new LocalKeyword[s_ShaderKeywordNames.Length];

            for (int i = 0; i < s_ShaderKeywordNames.Length; i++)
            {
                m_ShaderKeywords[i] = new LocalKeyword(m_Material.shader, s_ShaderKeywordNames[i]);
            }
        }

        return m_Material != null;
    }
}
