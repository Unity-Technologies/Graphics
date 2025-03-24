using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class RenderWithSubsurfaceScattering : CustomPass
{
    public LayerMask sssMask;

    protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
    {
        cullingParameters.cullingMask |= (uint)(int)sssMask;
    }

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
    }
    
    FieldInfo sssBuffer = typeof(CustomPassContext).GetField("sssBuffer", BindingFlags.Instance | BindingFlags.NonPublic);
    FieldInfo diffuseLightingBuffer = typeof(CustomPassContext).GetField("diffuseLightingBuffer", BindingFlags.Instance | BindingFlags.NonPublic);

    // Warning: This is not compatible with Virtual Texturing!
    protected override void Execute(CustomPassContext ctx)
    {
        if (injectionPoint == CustomPassInjectionPoint.AfterOpaqueColor)
        {
            var sssBufferHandle = sssBuffer.GetValue(ctx) as RTHandle;
            var diffuseLightingBufferHandle = diffuseLightingBuffer.GetValue(ctx) as RTHandle;

            if (sssBufferHandle == null || diffuseLightingBufferHandle == null)
            {
                Debug.LogError("Couldn't fetch the SSS Buffer, no custom SSS Objects will be rendered");
                return;
            }

            RenderTargetIdentifier[] colorBuffers =
            {
                ctx.cameraColorBuffer,
                diffuseLightingBufferHandle,
                sssBufferHandle,
            };
            var overrideDepthBlock = new RenderStateBlock(RenderStateMask.Depth);
            overrideDepthBlock.depthState = new DepthState(true, CompareFunction.LessEqual);
            CoreUtils.SetRenderTarget(ctx.cmd, colorBuffers, ctx.cameraDepthBuffer, ClearFlag.None);
            CustomPassUtils.DrawRenderers(ctx, sssMask, overrideRenderState: overrideDepthBlock);
        }

        if (injectionPoint == CustomPassInjectionPoint.AfterOpaqueDepthAndNormal)
        {
            var overrideDepthBlock = new RenderStateBlock(RenderStateMask.Depth);
            overrideDepthBlock.depthState = new DepthState(true, CompareFunction.LessEqual);
            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraDepthBuffer);
            CustomPassUtils.DrawRenderers(ctx, sssMask, overrideRenderState: overrideDepthBlock);
        }
    }

    protected override void Cleanup()
    {
    }
}