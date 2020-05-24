using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class Outline : CustomPass
{
    public LayerMask    outlineLayer = 0;
    [ColorUsage(false, true)]
    public Color        outlineColor = Color.black;
    public float        threshold = 1;

    // To make sure the shader will ends up in the build, we keep it's reference in the custom pass
    [SerializeField, HideInInspector]
    Shader                  outlineShader;

    Material                fullscreenOutline;
    MaterialPropertyBlock   outlineProperties;
    ShaderTagId[]           shaderTags;
    RTHandle                outlineBuffer;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        outlineShader = Shader.Find("Hidden/Outline");
        fullscreenOutline = CoreUtils.CreateEngineMaterial(outlineShader);
        outlineProperties = new MaterialPropertyBlock();

        // List all the materials that will be replaced in the frame
        shaderTags = new ShaderTagId[3]
        {
            new ShaderTagId("Forward"),
            new ShaderTagId("ForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit"),
        };

        outlineBuffer = RTHandles.Alloc(
            Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
            colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
            useDynamicScale: true, name: "Outline Buffer"
        );
    }

    void DrawOutlineMeshes(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
    {
        var result = new RendererListDesc(shaderTags, cullingResult, hdCamera.camera)
        {
            // We need the lighting render configuration to support rendering lit objects
            rendererConfiguration = PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.Lightmaps,
            renderQueueRange = RenderQueueRange.all,
            sortingCriteria = SortingCriteria.BackToFront,
            excludeObjectMotionVectors = false,
            layerMask = outlineLayer,
        };

        CoreUtils.SetRenderTarget(cmd, outlineBuffer, ClearFlag.Color);
        HDUtils.DrawRendererList(renderContext, cmd, RendererList.Create(result));
    }

    protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera camera, CullingResults cullingResult)
    {
        DrawOutlineMeshes(renderContext, cmd, camera, cullingResult);

        SetCameraRenderTarget(cmd);

        outlineProperties.SetColor("_OutlineColor", outlineColor);
        outlineProperties.SetTexture("_OutlineBuffer", outlineBuffer);
        outlineProperties.SetFloat("_Threshold", threshold);
        CoreUtils.DrawFullScreen(cmd, fullscreenOutline, outlineProperties, shaderPassId: 0);
    }

    protected override void Cleanup()
    {
        CoreUtils.Destroy(fullscreenOutline);
        outlineBuffer.Release();
    }
}
