using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// This pass is a simplified version of the URP CopyDepthPass. This pass copies the depth texture to an RTHandle.
// Unlike the original URP CopyDepthPass, this example does not use the _CameraDepthTexture texture, and demonstrates how to copy from the depth buffer to a custom RTHandle instead.
public class DepthBlitCopyDepthPass : ScriptableRenderPass
{
    private const string k_PassName = "DepthBlitCopyDepthPass";
    private readonly int m_DepthBufferId = Shader.PropertyToID("_CameraDepthAttachment");
    private Vector4 m_ScaleBias = new Vector4(1f, 1f, 0f, 0f);
    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_PassName);
    public RTHandle depthRT; // The RTHandle for storing the depth texture
    private RenderTextureDescriptor m_Desc;
    private FilterMode m_FilterMode;
    private TextureWrapMode m_WrapMode;
    private string m_Name;
    private Material m_CopyDepthMaterial;
    private GlobalKeyword m_Keyword_DepthMsaa2;
    private GlobalKeyword m_Keyword_DepthMsaa4;
    private GlobalKeyword m_Keyword_DepthMsaa8;
    private GlobalKeyword m_Keyword_OutputDepth;

    class PassData
    {
        public Material copyDepthMaterial;
        public TextureHandle source;
        public Vector4 scaleBias;
        public int depthBufferId;
        public GlobalKeyword keyword_DepthMsaa2;
        public GlobalKeyword keyword_DepthMsaa4;
        public GlobalKeyword keyword_DepthMsaa8;
        public GlobalKeyword keyword_OutputDepth;
    }

    public DepthBlitCopyDepthPass(RenderPassEvent evt, Shader copyDepthShader, 
        RenderTextureDescriptor desc, FilterMode filterMode, TextureWrapMode wrapMode, string name)
    {
        renderPassEvent = evt;
        m_Desc = desc;
        m_FilterMode = filterMode;
        m_WrapMode = wrapMode;
        m_Name = name;
        m_CopyDepthMaterial = copyDepthShader != null ? CoreUtils.CreateEngineMaterial(copyDepthShader) : null;
        m_Keyword_DepthMsaa2 = GlobalKeyword.Create(ShaderKeywordStrings.DepthMsaa2);
        m_Keyword_DepthMsaa4 = GlobalKeyword.Create(ShaderKeywordStrings.DepthMsaa4);
        m_Keyword_DepthMsaa8 = GlobalKeyword.Create(ShaderKeywordStrings.DepthMsaa8);
        m_Keyword_OutputDepth = GlobalKeyword.Create(ShaderKeywordStrings._OUTPUT_DEPTH);
    }

#if URP_COMPATIBILITY_MODE // Compatibility Mode is being removed
#pragma warning disable 618, 672 // Type or member is obsolete, Member overrides obsolete member

    // Set the RTHandle as the output target in the Compatibility mode.
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        // Create an RTHandle for storing the depth
        RenderingUtils.ReAllocateHandleIfNeeded(ref depthRT, m_Desc, m_FilterMode, m_WrapMode, name: m_Name );
        ConfigureTarget(depthRT);
    }

    // Unity calls the Execute method in the Compatibility mode
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;
        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        // Bind the depth buffer to material
        RTHandle source = cameraData.renderer.cameraDepthTargetHandle;
        m_CopyDepthMaterial.SetTexture(m_DepthBufferId, source);

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            // Enable an MSAA shader keyword based on the source texture MSAA sample count.
            int cameraSamples = source.rt.antiAliasing;
            cmd.SetKeyword(m_Keyword_DepthMsaa2, cameraSamples == 2);
            cmd.SetKeyword(m_Keyword_DepthMsaa4, cameraSamples == 4);
            cmd.SetKeyword(m_Keyword_DepthMsaa8, cameraSamples == 8);

            // This example does not copy the depth values back to the depth buffer, so we disable this keyword.
            cmd.SetKeyword(m_Keyword_OutputDepth, false);

            // Perform the blit operation
            Blitter.BlitTexture(cmd, source, m_ScaleBias, m_CopyDepthMaterial, 0);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }

#pragma warning restore 618, 672
#endif

    // Unity calls the RecordRenderGraph method to add and configure one or more render passes in the render graph system.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        DepthBlitFeature.TexRefData texRefData = frameData.GetOrCreate<DepthBlitFeature.TexRefData>();

        // Avoid blitting from the backbuffer
        if (resourceData.isActiveTargetBackBuffer)
            return;
        
        // Create an RTHandle for storing the depth
        RenderingUtils.ReAllocateHandleIfNeeded(ref depthRT, m_Desc, m_FilterMode, m_WrapMode, name: m_Name );

        // Set the texture resources for this render graph instance.
        TextureHandle src = resourceData.cameraDepth;
        TextureHandle dest = renderGraph.ImportTexture(depthRT);
        texRefData.depthTextureHandle = dest;

        if(!src.IsValid() || !dest.IsValid())
            return;

        using (var builder = renderGraph.AddRasterRenderPass<PassData>(k_PassName, out var passData, m_ProfilingSampler))
        {
            passData.copyDepthMaterial = m_CopyDepthMaterial;
            passData.source = src;
            passData.scaleBias = m_ScaleBias;
            passData.depthBufferId = m_DepthBufferId;
            passData.keyword_DepthMsaa2 = m_Keyword_DepthMsaa2;
            passData.keyword_DepthMsaa4 = m_Keyword_DepthMsaa4;
            passData.keyword_DepthMsaa8 = m_Keyword_DepthMsaa8;
            passData.keyword_OutputDepth = m_Keyword_OutputDepth;

            builder.UseTexture(src, AccessFlags.Read);
            builder.SetRenderAttachment(dest, 0, AccessFlags.Write);
            builder.AllowGlobalStateModification(true);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                // Enable an MSAA shader keyword based on the source texture MSAA sample count
                RTHandle sourceTex = data.source;
                int cameraSamples = sourceTex.rt.antiAliasing;
                context.cmd.SetKeyword(data.keyword_DepthMsaa2, cameraSamples == 2);
                context.cmd.SetKeyword(data.keyword_DepthMsaa4, cameraSamples == 4);
                context.cmd.SetKeyword(data.keyword_DepthMsaa8, cameraSamples == 8);

                // This example does not copy the depth values back to the depth buffer, so we disable this keyword.
                context.cmd.SetKeyword(data.keyword_OutputDepth, false);

                // Bind the depth buffer to the material
                data.copyDepthMaterial.SetTexture(data.depthBufferId, data.source);

                // Perform the blit operation
                Blitter.BlitTexture(context.cmd, data.source, data.scaleBias, data.copyDepthMaterial, 0);
            });
        }
    }

    public void Dispose()
    {
        depthRT?.Release();
        CoreUtils.Destroy(m_CopyDepthMaterial);
    }
}
