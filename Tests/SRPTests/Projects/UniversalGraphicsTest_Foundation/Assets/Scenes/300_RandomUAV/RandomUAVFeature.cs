using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RandomUAVFeature : ScriptableRendererFeature
{
    public Shader randomUAVFillShader;
    public Shader randomUAVReadWriteShader;
    public Shader randomUAVFinalOutputShader;
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering;

    private Material m_RandomUAVFillMaterial;
    private Material m_RandomUAVReadWriteMaterial;
    private Material m_RandomUAVFinalOutputMaterial;
    private RandomUAVPass m_RandomUAVPass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_RandomUAVPass = new RandomUAVPass(name, name + "_Output");
        m_RandomUAVPass.renderPassEvent = renderPassEvent;
    }

    /// <inheritdoc/>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!GetMaterials())
        {
            Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.", GetType().Name, name);
            return;
        }

        m_RandomUAVPass.renderPassEvent = renderPassEvent;
        m_RandomUAVPass.Setup(renderer, m_RandomUAVFillMaterial, m_RandomUAVReadWriteMaterial, m_RandomUAVFinalOutputMaterial);
        renderer.EnqueuePass(m_RandomUAVPass);
    }

    private bool GetMaterials()
    {
        if (m_RandomUAVFillMaterial == null && randomUAVFillShader != null)
            m_RandomUAVFillMaterial = CoreUtils.CreateEngineMaterial(randomUAVFillShader);

        if (m_RandomUAVReadWriteMaterial == null && randomUAVReadWriteShader != null)
            m_RandomUAVReadWriteMaterial = CoreUtils.CreateEngineMaterial(randomUAVReadWriteShader);

        if (m_RandomUAVFinalOutputMaterial == null && randomUAVFinalOutputShader != null)
            m_RandomUAVFinalOutputMaterial = CoreUtils.CreateEngineMaterial(randomUAVFinalOutputShader);

        return m_RandomUAVReadWriteMaterial != null && m_RandomUAVFinalOutputMaterial != null;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        m_RandomUAVPass = null;
        CoreUtils.Destroy(m_RandomUAVReadWriteMaterial);
    }

    class RandomUAVPass : ScriptableRenderPass
    {
        private Material m_RandomUAVFillMaterial;
        private Material m_RandomUAVReadWriteMaterial;
        private Material m_RandomUAVFinalOutputMaterial;
        private ScriptableRenderer m_Renderer;
        private ProfilingSampler m_ProfilingSampler;
        private ProfilingSampler m_ProfilingOutputSampler;
        private int m_ImageSizePropertyID = Shader.PropertyToID("_ImageSize");

        public RandomUAVPass(string profilerTag, string profilerTagOutput)
        {
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            m_ProfilingOutputSampler = new ProfilingSampler(profilerTagOutput);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Don't do anything as this is a RenderGraph only feature
        }

        public void Setup(ScriptableRenderer renderer, Material randomUAVFillMaterial, Material randomUAVReadWriteMaterial, Material randomUAVFinalOutputMaterial)
        {
            m_Renderer = renderer;
            m_RandomUAVFillMaterial = randomUAVFillMaterial;
            m_RandomUAVReadWriteMaterial = randomUAVReadWriteMaterial;
            m_RandomUAVFinalOutputMaterial = randomUAVFinalOutputMaterial;
            ConfigureInput(ScriptableRenderPassInput.Color);
        }

        class UAVResources : ContextItem
        {
            public TextureHandle uavTextureBuffer { get; set; }
            public BufferHandle uavBuffer { get; set; }

            public override void Reset()
            {
                uavTextureBuffer = TextureHandle.nullHandle;
                uavBuffer = BufferHandle.nullHandle;
            }
        }

        private class PassData
        {
            internal ProfilingSampler profilingSampler;
            internal Material material;
        }

        /// <inheritdoc/>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UAVResources UAVResources = frameData.GetOrCreate<UAVResources>();

            TextureHandle dummyTarget;
            Vector4 imageSize;

            // Draw something to the target and UAV
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("UAV Fill Pass", out PassData passData, m_ProfilingSampler))
            {
                // Setup the dummy Render Target
                RenderTextureDescriptor targetDesc = cameraData.cameraTargetDescriptor;
                targetDesc.depthBufferBits = 0;
                targetDesc.msaaSamples = 1;
                dummyTarget = UniversalRenderer.CreateRenderGraphTexture(renderGraph, targetDesc, "dummyTarget", false, FilterMode.Bilinear);
                imageSize = new Vector4(targetDesc.width, targetDesc.height, 0, 0);

                // Setup the random UAV Texture Target
                RenderTextureDescriptor randomUAVDescriptor = new(targetDesc.width, targetDesc.height, targetDesc.colorFormat) { enableRandomWrite = true };
                UAVResources.uavTextureBuffer = UniversalRenderer.CreateRenderGraphTexture(renderGraph, randomUAVDescriptor, "_UAVTextureBuffer", false, FilterMode.Bilinear);

                // Setup the random UAV Buffer Target
                int count = targetDesc.width * targetDesc.height;
                const int stride = sizeof(float) * 4;
                BufferDesc bufferDesc =  new(count, stride) { name = "_UAVBuffer" };
                UAVResources.uavBuffer = renderGraph.CreateBuffer(bufferDesc);

                // Setup up the pass data
                passData.profilingSampler = m_ProfilingSampler;
                passData.material = m_RandomUAVFillMaterial;
                passData.material.SetVector(m_ImageSizePropertyID, imageSize);

                // Setup up the builder
                builder.SetRenderAttachment(dummyTarget, 0, AccessFlags.Write);
                builder.SetRandomAccessAttachment(UAVResources.uavTextureBuffer, 1, AccessFlags.ReadWrite);
                builder.UseBufferRandomAccess(UAVResources.uavBuffer, 2, AccessFlags.ReadWrite);
                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext.cmd));
                builder.AllowPassCulling(false);
            }

            // Draw something to the target and UAV
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("UAV Read / Write Pass", out PassData passData, m_ProfilingSampler))
            {
                // Setup up the pass data
                passData.profilingSampler = m_ProfilingSampler;
                passData.material = m_RandomUAVReadWriteMaterial;
                passData.material.SetVector(m_ImageSizePropertyID, imageSize);

                // Setup up the builder
                builder.SetRenderAttachment(dummyTarget, 0, AccessFlags.Write);
                builder.SetRandomAccessAttachment(UAVResources.uavTextureBuffer, 1, AccessFlags.ReadWrite);
                builder.UseBufferRandomAccess(UAVResources.uavBuffer, 2, AccessFlags.ReadWrite);
                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext.cmd));
                builder.AllowPassCulling(false);
            }

            // Display the UAV texture on top of the active color texture
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("UAV Read Pass", out PassData passData, m_ProfilingSampler))
            {
                // Setup up the pass data
                passData.profilingSampler = m_ProfilingOutputSampler;
                passData.material = m_RandomUAVFinalOutputMaterial;
                passData.material.SetVector(m_ImageSizePropertyID, imageSize);

                // Setup up the builder
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRandomAccessAttachment(UAVResources.uavTextureBuffer, 1, AccessFlags.Read);
                builder.UseBufferRandomAccess(UAVResources.uavBuffer, 2, AccessFlags.Read);
                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext.cmd));
                builder.AllowPassCulling(false);
            }
        }

        private static void ExecutePass(PassData passData, RasterCommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, passData.profilingSampler))
            {
                Blitter.BlitTexture(cmd, Vector2.one, passData.material, 0);
            }
        }
    }
}


