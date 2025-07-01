using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

class BatchLayerCustomPass : CustomPass
{
    public int batchLayerMask = 0;
    public Material overrideMat = null;
    private int m_overrideMatPass = 0;

    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        m_ShaderPasses = new ShaderTagId[]
        {
            HDShaderPassNames.s_GBufferName,// HD Lit shader
        };

        if (overrideMat != null)
        {
            m_overrideMatPass = overrideMat.FindPass(HDShaderPassNames.s_GBufferStr);
        }
    }

    private ShaderTagId[] m_ShaderPasses;

    protected override void Execute(CustomPassContext ctx)
    {
        var stateBlock = new RenderStateBlock(0)
        {
            depthState = new DepthState(true, CompareFunction.Always),
            // We disable the stencil when the depth is overwritten but we don't write to it, to prevent writing to the stencil.
            stencilState = new StencilState(false, (byte)(UserStencilUsage.AllUserBits), (byte)(UserStencilUsage.AllUserBits), CompareFunction.Always, StencilOp.Keep, StencilOp.Keep, StencilOp.Keep),
            stencilReference = 0,
        };
        
        var result = new UnityEngine.Rendering.RendererUtils.RendererListDesc(m_ShaderPasses, ctx.cullingResults, ctx.hdCamera.camera)
        {
            rendererConfiguration = HDUtils.GetBakedLightingRenderConfig(),
            renderQueueRange = GetRenderQueueRange(RenderQueueType.AllOpaque),
            sortingCriteria = SortingCriteria.CommonOpaque,
            excludeObjectMotionVectors = false,
            overrideShader = null,
            overrideMaterial = overrideMat,
            overrideMaterialPassIndex = m_overrideMatPass,
            overrideShaderPassIndex = m_overrideMatPass,
            stateBlock = stateBlock,
            layerMask = ~0,
            batchLayerMask = (uint)batchLayerMask,
        };

        var renderCtx = ctx.renderContext;
        var rendererList = renderCtx.CreateRendererList(result);
        CoreUtils.DrawRendererList(ctx.cmd, rendererList);
    }

    protected override void Cleanup()
    {
        // Cleanup code
    }
}
