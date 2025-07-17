using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

class DrawRenderersFromPostProcess : CustomPass
{
    public LayerMask layerMask;
    CustomPassContext currentFrameContext;
    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // Setup code here
    }

    protected override void Execute(CustomPassContext ctx)
    {
        currentFrameContext = ctx;
    }

    protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
    {
        cullingParameters.cullingMask |= (uint)(int)layerMask;
    }

    public void ExecuteFromPostProcess(CommandBuffer cmd)
    {
        var state = new RenderStateBlock(RenderStateMask.Depth);
        state.depthState = new DepthState(true, CompareFunction.LessEqual);
        PerObjectData renderConfig = HDUtils.GetRendererConfiguration(currentFrameContext.hdCamera.frameSettings.IsEnabled(FrameSettingsField.AdaptiveProbeVolume), currentFrameContext.hdCamera.frameSettings.IsEnabled(FrameSettingsField.Shadowmask));

        var forwardShaderTags = new ShaderTagId[]
        {
            HDShaderPassNames.s_ForwardName,            // HD Lit shader
            HDShaderPassNames.s_ForwardOnlyName,        // HD Unlit shader
            HDShaderPassNames.s_SRPDefaultUnlitName,    // Cross SRP Unlit shader
            HDShaderPassNames.s_EmptyName,              // Add an empty slot for the override material
        };


        var result = new UnityEngine.Rendering.RendererUtils.RendererListDesc(forwardShaderTags, currentFrameContext.cullingResults, currentFrameContext.hdCamera.camera)
        {
            rendererConfiguration = renderConfig,
            renderQueueRange = RenderQueueRange.all,
            sortingCriteria = SortingCriteria.CommonTransparent,
            overrideMaterial = null,
            overrideMaterialPassIndex = 0,
            excludeObjectMotionVectors = false,
            renderingLayerMask = uint.MaxValue,
            layerMask = layerMask,
            stateBlock = state,
        };

        var renderCtx = currentFrameContext.renderContext;
        CoreUtils.DrawRendererList(cmd, renderCtx.CreateRendererList(result));
    }

    protected override void Cleanup()
    {
        // Cleanup code
    }
}