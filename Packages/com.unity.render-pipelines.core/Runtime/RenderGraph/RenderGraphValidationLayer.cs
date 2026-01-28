namespace UnityEngine.Rendering.RenderGraphModule
{
    // The validation layer is not guaranteed that all of the builder interface methods will be called.
    // This will be added adhoc when needed. Adding them is a feature request, not a bug.
    internal class RenderGraphValidationLayer : IRasterRenderGraphBuilder, IBaseRenderGraphBuilder
    {
        public struct RenderPassInfo
        {
            public RenderGraphPassType type;
            public string name;
        }
        virtual public void OnPassAddedBegin(in RenderPassInfo renderPassInfo) { }
        virtual public void OnPassAddedDispose() { }
        virtual public void Clear() { } 

        // Implement IRasterRenderGraphBuilder and IBaseRenderGraphBuilder with stubs so that the implementation is not mandatory in the implementing class.
        virtual public void UseTexture(in TextureHandle input, AccessFlags flags) { }
        virtual public void UseGlobalTexture(int propertyId, AccessFlags flags) { }
        virtual public void UseAllGlobalTextures(bool enable) { }
        virtual public void SetGlobalTextureAfterPass(in TextureHandle input, int propertyId) { }
        virtual public BufferHandle UseBuffer(in BufferHandle input, AccessFlags flags) { return input; }
        virtual public void SetRenderAttachment(TextureHandle tex, int index, AccessFlags flags, int mipLevel, int depthSlice) { }
        virtual public void SetRenderAttachmentDepth(TextureHandle tex, AccessFlags flags, int mipLevel, int depthSlice) { }
        virtual public void SetInputAttachment(TextureHandle tex, int index, AccessFlags flags, int mipLevel, int depthSlice) { }

        // These aren't called and currently not supported for validation
        public TextureHandle CreateTransientTexture(in TextureDesc desc) { return TextureHandle.nullHandle; }
        public TextureHandle CreateTransientTexture(in TextureHandle texture) { return TextureHandle.nullHandle; }
        public BufferHandle CreateTransientBuffer(in BufferDesc desc) { return BufferHandle.nullHandle; }
        public BufferHandle CreateTransientBuffer(in BufferHandle computebuffer) { return BufferHandle.nullHandle; }
        public void UseRendererList(in RendererListHandle input) { }
        public void EnableAsyncCompute(bool value) { }
        public void AllowPassCulling(bool value) { }
        public void AllowGlobalStateModification(bool value) { }
        public void EnableFoveatedRasterization(bool value) { }
        public void GenerateDebugData(bool value) { }
        public TextureHandle SetRandomAccessAttachment(TextureHandle tex, int index, AccessFlags flags) { return TextureHandle.nullHandle; }
        public BufferHandle UseBufferRandomAccess(BufferHandle tex, int index, AccessFlags flags = AccessFlags.Read) { return tex; }
        public BufferHandle UseBufferRandomAccess(BufferHandle tex, int index, bool preserveCounterValue, AccessFlags flags) { return tex; }
        public void SetShadingRateImageAttachment(in TextureHandle tex) { }
        public void SetShadingRateFragmentSize(ShadingRateFragmentSize shadingRateFragmentSize) { }
        public void SetShadingRateCombiner(ShadingRateCombinerStage stage, ShadingRateCombiner combiner) { }
        public void SetExtendedFeatureFlags(ExtendedFeatureFlags extendedFeatureFlags) { }
        public void SetRenderFunc<PassData>(BaseRenderFunc<PassData, RasterGraphContext> renderFunc) where PassData : class, new() { }
        virtual public void Dispose() { }
    }
}
