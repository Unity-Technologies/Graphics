using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// Code samples of the URP global shader variables API. The API documentation shows the regions of this file through
// <code source="..." region="..."/>, so rename a region in the XML documentation too. Keep the empty /// line between
// the <para> before a sample and its <code> element: without it, the documentation shows the sample as code only up to
// its first blank line. This assembly compiles only when the tests of the package compile (UNITY_INCLUDE_TESTS), which
// keeps the samples compiling.

#region temporary-copy
class HalfResolutionOpaquePass : ScriptableRenderPass
{
    static readonly int s_HalfResolutionOpaqueTextureId = Shader.PropertyToID("_HalfResolutionOpaqueTexture");

    class PassData
    {
        public UniversalGlobalShaderData globalShaderData;
        public RendererListHandle rendererList;
        public Vector2Int size;
    }

    public HalfResolutionOpaquePass()
    {
        // The pass reads the descriptors of the camera textures, which the back buffer doesn't have.
        requiresIntermediateTexture = true;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        TextureDesc colorDesc = resourceData.activeColorTexture.GetDescriptor(renderGraph);
        colorDesc.width = Mathf.Max(1, colorDesc.width / 2);
        colorDesc.height = Mathf.Max(1, colorDesc.height / 2);
        colorDesc.name = "_HalfResolutionOpaqueTexture";
        colorDesc.clearBuffer = true;
        TextureDesc depthDesc = resourceData.activeDepthTexture.GetDescriptor(renderGraph);
        depthDesc.width = colorDesc.width;
        depthDesc.height = colorDesc.height;
        depthDesc.name = "_HalfResolutionOpaqueDepth";
        depthDesc.clearBuffer = true;
        TextureHandle color = renderGraph.CreateTexture(colorDesc);

        using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("Half Resolution Opaque",
            out PassData passData);

        // Keep the UniversalGlobalShaderData item in the pass data.
        // Its variables are available only while the render graph executes.
        passData.globalShaderData = frameData.Get<UniversalGlobalShaderData>();
        passData.size = new Vector2Int(colorDesc.width, colorDesc.height);

        DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(new ShaderTagId("UniversalForward"),
            renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
        RendererListParams rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings,
            new FilteringSettings(RenderQueueRange.opaque));
        passData.rendererList = renderGraph.CreateRendererList(rendererListParams);
        builder.UseRendererList(passData.rendererList);

        builder.SetRenderAttachment(color, 0);
        builder.SetRenderAttachmentDepth(renderGraph.CreateTexture(depthDesc), AccessFlags.Write);
        builder.SetGlobalTextureAfterPass(color, s_HalfResolutionOpaqueTextureId);

        // Pushing shader variables changes global state.
        builder.AllowGlobalStateModification(true);

        builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
        {
            // Disposing the copy at the end of the using scope restores the values the later passes read.
            using IUniversalGlobalShaderVariablesTemporaryUploader globals = data.globalShaderData.Get();
            globals._ScreenSize = new Vector4(data.size.x, data.size.y, 1f / data.size.x, 1f / data.size.y);
            globals.Push(context.cmd);

            context.cmd.DrawRendererList(data.rendererList);
        });
    }
}

class HalfResolutionOpaqueFeature : ScriptableRendererFeature
{
    HalfResolutionOpaquePass m_Pass;

    public override void Create()
    {
        m_Pass = new HalfResolutionOpaquePass { renderPassEvent = RenderPassEvent.AfterRenderingOpaques };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_Pass);
    }
}
#endregion

#region temporary-copy-compute
class QuarterResolutionComputePass : ScriptableRenderPass
{
    static readonly int s_ResultId = Shader.PropertyToID("_Result");
    static readonly int s_QuarterResolutionTextureId = Shader.PropertyToID("_QuarterResolutionTexture");

    class PassData
    {
        public UniversalGlobalShaderData globalShaderData;
        public ComputeShader computeShader;
        public int kernel;
        public TextureHandle result;
        public Vector2Int size;
    }

    readonly ComputeShader m_ComputeShader;
    readonly int m_Kernel;

    public QuarterResolutionComputePass(ComputeShader computeShader)
    {
        m_ComputeShader = computeShader;
        m_Kernel = computeShader.FindKernel("CSMain");

        // The pass reads the descriptor of the camera color texture, which the back buffer doesn't have.
        requiresIntermediateTexture = true;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        TextureDesc desc = resourceData.activeColorTexture.GetDescriptor(renderGraph);
        desc.width = Mathf.Max(1, desc.width / 4);
        desc.height = Mathf.Max(1, desc.height / 4);
        desc.name = "_QuarterResolutionTexture";
        desc.enableRandomWrite = true;
        desc.msaaSamples = MSAASamples.None;

        using IComputeRenderGraphBuilder builder = renderGraph.AddComputePass<PassData>("Quarter Resolution Compute",
            out PassData passData);
        passData.globalShaderData = frameData.Get<UniversalGlobalShaderData>();
        passData.computeShader = m_ComputeShader;
        passData.kernel = m_Kernel;
        passData.result = renderGraph.CreateTexture(desc);
        passData.size = new Vector2Int(desc.width, desc.height);
        builder.UseTexture(passData.result, AccessFlags.Write);
        builder.SetGlobalTextureAfterPass(passData.result, s_QuarterResolutionTextureId);

        // With loose uniforms, PushToCompute() pushes the values globally, which changes global state.
        builder.AllowGlobalStateModification(true);

        builder.SetRenderFunc(static (PassData data, ComputeGraphContext context) =>
        {
            using IUniversalGlobalShaderVariablesTemporaryUploader globals = data.globalShaderData.Get();
            globals._ScreenSize = new Vector4(data.size.x, data.size.y, 1f / data.size.x, 1f / data.size.y);
            globals.PushToCompute(context.cmd, data.computeShader);

            context.cmd.SetComputeTextureParam(data.computeShader, data.kernel, s_ResultId, data.result);
            context.cmd.DispatchCompute(data.computeShader, data.kernel, (data.size.x + 7) / 8, (data.size.y + 7) / 8, 1);
        });
    }
}
#endregion

#region global-variables
class SharpenTexturesPass : ScriptableRenderPass
{
    class PassData
    {
        public UniversalGlobalShaderData globalShaderData;
        public float mipBias;
    }

    public float mipBias = -1f;

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        using IUnsafeRenderGraphBuilder builder = renderGraph.AddUnsafePass<PassData>("Sharpen Textures",
            out PassData passData);
        passData.globalShaderData = frameData.Get<UniversalGlobalShaderData>();
        passData.mipBias = mipBias;

        // The pass changes what the later passes read, so it must allow global state modification.
        builder.AllowGlobalStateModification(true);

        builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
        {
            IUniversalGlobalShaderVariablesUploader globals = data.globalShaderData.GetGlobal();
            globals._GlobalMipBias = new Vector2(data.mipBias, Mathf.Pow(2f, data.mipBias));
            globals.Push(context.cmd);
        });
    }
}
#endregion

#region restore-camera-values
class RestoreCameraValuesPass : ScriptableRenderPass
{
    class PassData
    {
        public UniversalGlobalShaderData globalShaderData;
        public bool keepMipBias;
    }

    public bool keepMipBias;

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        using IUnsafeRenderGraphBuilder builder = renderGraph.AddUnsafePass<PassData>("Restore Camera Values",
            out PassData passData);
        passData.globalShaderData = frameData.Get<UniversalGlobalShaderData>();
        passData.keepMipBias = keepMipBias;
        builder.AllowGlobalStateModification(true);

        builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
        {
            IUniversalGlobalShaderVariablesUploader globals = data.globalShaderData.GetGlobal();
            if (!data.keepMipBias)
            {
                globals.PushDefault(context.cmd);
                return;
            }

            // ResetToDefault() doesn't push, so the reset and the kept mip bias reach the GPU in a single push.
            Vector2 mipBias = globals._GlobalMipBias;
            globals.ResetToDefault();
            globals._GlobalMipBias = mipBias;
            globals.Push(context.cmd);
        });
    }
}
#endregion

#region temporary-copy-camera-values
class CameraValuesLayerPass : ScriptableRenderPass
{
    class PassData
    {
        public UniversalGlobalShaderData globalShaderData;
        public RendererListHandle rendererList;
    }

    public LayerMask layerMask;

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("Camera Values Layer",
            out PassData passData);
        passData.globalShaderData = frameData.Get<UniversalGlobalShaderData>();

        DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(new ShaderTagId("UniversalForward"),
            renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
        RendererListParams rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings,
            new FilteringSettings(RenderQueueRange.opaque, layerMask));
        passData.rendererList = renderGraph.CreateRendererList(rendererListParams);
        builder.UseRendererList(passData.rendererList);
        builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
        builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);
        builder.AllowGlobalStateModification(true);

        builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
        {
            using IUniversalGlobalShaderVariablesTemporaryUploader globals = data.globalShaderData.Get();
            globals.ResetToDefault();
            globals.Push(context.cmd);

            context.cmd.DrawRendererList(data.rendererList);
        });
    }
}
#endregion

#region both-renderers
class CameraTargetSizePass : ScriptableRenderPass
{
    class PassData
    {
        // Only one of them is set, depending on the renderer that runs the pass.
        public UniversalGlobalShaderData globalShaderData;
        public Universal2DGlobalShaderData globalShaderData2D;
        public Vector2Int size;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        Camera camera = frameData.Get<UniversalCameraData>().camera;

        using IUnsafeRenderGraphBuilder builder = renderGraph.AddUnsafePass<PassData>("Camera Target Size",
            out PassData passData);
        if (frameData.Contains<UniversalGlobalShaderData>())
            passData.globalShaderData = frameData.Get<UniversalGlobalShaderData>();
        else
            passData.globalShaderData2D = frameData.Get<Universal2DGlobalShaderData>();
        passData.size = new Vector2Int(camera.pixelWidth, camera.pixelHeight);
        builder.AllowGlobalStateModification(true);

        builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
        {
            // IGlobalShaderVariablesCommonUploader has the variables that both renderers fill.
            IGlobalShaderVariablesCommonUploader globals;
            if (data.globalShaderData != null)
                globals = data.globalShaderData.GetGlobal();
            else
                globals = data.globalShaderData2D.GetGlobal();

            globals._ScreenSize = new Vector4(data.size.x, data.size.y, 1f / data.size.x, 1f / data.size.y);
            globals.Push(context.cmd);
        });
    }
}
#endregion

#region renderer-2d
class FrozenTimeSpritesPass : ScriptableRenderPass
{
    class PassData
    {
        public Universal2DGlobalShaderData globalShaderData;
        public RendererListHandle rendererList;
        public float time;
    }

    public LayerMask layerMask;
    public float frozenTime;

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("Frozen Time Sprites",
            out PassData passData);
        passData.globalShaderData = frameData.Get<Universal2DGlobalShaderData>();
        passData.time = frozenTime;

        DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(new ShaderTagId("Universal2D"),
            renderingData, cameraData, lightData, SortingCriteria.CommonTransparent);
        RendererListParams rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings,
            new FilteringSettings(RenderQueueRange.transparent, layerMask));
        passData.rendererList = renderGraph.CreateRendererList(rendererListParams);
        builder.UseRendererList(passData.rendererList);
        builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
        builder.AllowGlobalStateModification(true);

        builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
        {
            using IUniversal2DGlobalShaderVariablesTemporaryUploader globals = data.globalShaderData.Get();
            float time = data.time;
            globals._Time = new Vector4(time / 20f, time, time * 2f, time * 3f);
            globals._SinTime = new Vector4(Mathf.Sin(time / 8f), Mathf.Sin(time / 4f), Mathf.Sin(time / 2f),
                Mathf.Sin(time));
            globals._CosTime = new Vector4(Mathf.Cos(time / 8f), Mathf.Cos(time / 4f), Mathf.Cos(time / 2f),
                Mathf.Cos(time));
            globals._TimeParameters = new Vector4(time, Mathf.Sin(time), Mathf.Cos(time), 0f);
            globals.Push(context.cmd);

            context.cmd.DrawRendererList(data.rendererList);
        });
    }
}
#endregion

#region shadow-structs
class FirstCascadeShadowsPass : ScriptableRenderPass
{
    class PassData
    {
        public UniversalGlobalShaderData globalShaderData;
        public RendererListHandle rendererList;
    }

    public LayerMask layerMask;

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("First Cascade Shadows",
            out PassData passData);
        passData.globalShaderData = frameData.Get<UniversalGlobalShaderData>();

        DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(new ShaderTagId("UniversalForward"),
            renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
        RendererListParams rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings,
            new FilteringSettings(RenderQueueRange.opaque, layerMask));
        passData.rendererList = renderGraph.CreateRendererList(rendererListParams);
        builder.UseRendererList(passData.rendererList);
        builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
        builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);
        builder.AllowGlobalStateModification(true);

        builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
        {
            using IUniversalGlobalShaderVariablesTemporaryUploader globals = data.globalShaderData.Get();

            // The properties return copies of the structs: read the property, change the copy, and assign it back.
            MainLightShadowMatrices matrices = globals._MainLightWorldToShadow;
            MainLightShadowCascadeVectors pcssAtlas = globals._MainLightPCSSAtlas;
            for (int cascade = 1; cascade < MainLightShadowMatrices.count - 1; cascade++)
            {
                matrices[cascade] = matrices.cascade4;
                pcssAtlas[cascade] = pcssAtlas.cascade4;
            }
            globals._MainLightWorldToShadow = matrices;
            globals._MainLightPCSSAtlas = pcssAtlas;
            globals.Push(context.cmd);

            context.cmd.DrawRendererList(data.rendererList);
        });
    }
}
#endregion

#region outside-camera-rendering
static class MeshPreview
{
    static readonly int s_GlobalShaderVariablesId = Shader.PropertyToID("GlobalShaderVariables");
    static readonly int s_ScreenSizeId = Shader.PropertyToID("_ScreenSize");
    static readonly ShaderTagId s_LightModeTag = new ShaderTagId("LightMode");
    static readonly ShaderTagId s_UniversalForwardLightMode = new ShaderTagId("UniversalForward");

    public static void Render(RenderTexture target, Mesh mesh, Material material, Matrix4x4 view, Matrix4x4 projection)
    {
        CommandBuffer cmd = new CommandBuffer { name = "Mesh Preview" };
        cmd.SetRenderTarget(target);
        cmd.ClearRenderTarget(true, true, Color.clear);
        cmd.SetViewProjectionMatrices(view, projection);

        // While Use Persistent Global Constant Buffer is enabled, the buffer that URP bound for its last camera stays
        // bound, and the shaders don't read what SetGlobal* sets. Unbind the buffer first. URP binds it again for its
        // next camera.
        cmd.SetGlobalConstantBuffer((GraphicsBuffer)null, s_GlobalShaderVariablesId, 0, 0);

        // Then set the variables that the shaders read.
        cmd.SetGlobalVector(s_ScreenSizeId, new Vector4(target.width, target.height, 1f / target.width, 1f / target.height));

        int pass = FindPassWithLightMode(material, s_UniversalForwardLightMode);
        if (pass >= 0)
            cmd.DrawMesh(mesh, Matrix4x4.identity, material, 0, pass);
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Release();
    }

    // Pass names differ between URP shaders, for example ForwardLit in URP Lit, but the light mode tag doesn't.
    static int FindPassWithLightMode(Material material, ShaderTagId lightMode)
    {
        for (int i = 0; i < material.passCount; i++)
        {
            if (material.shader.FindPassTagValue(i, s_LightModeTag) == lightMode)
                return i;
        }
        return -1;
    }
}
#endregion
