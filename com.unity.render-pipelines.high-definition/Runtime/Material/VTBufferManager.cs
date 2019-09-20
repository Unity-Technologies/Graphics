using UnityEngine.Experimental.Rendering;


namespace  UnityEngine.Rendering.HighDefinition
{
#if ENABLE_VIRTUALTEXTURES
    public class VTBufferManager
    {
        const int Scale = 16; //Keep in sync with TextureStack.hlsl

        RTHandle opaqueHandle = null;
        Vector2 m_scale = new Vector2(1.0f / (float)Scale, 1.0f / (float)Scale);

        VirtualTextureResolver m_Resolver = new VirtualTextureResolver();

        public void CreateBuffers()
        {
            opaqueHandle = RTHandles.Alloc(m_scale, TextureXR.slices,
                colorFormat: GraphicsFormat.R8G8B8A8_UNorm, useDynamicScale: false,
                name: "VTFeedbackBuffer_opaque", enableRandomWrite: true);
        }

        public void BeginRender()
        {
            uint width = (uint)Mathf.Max(Mathf.RoundToInt(m_scale.x * RTHandles.maxWidth), 1);
            uint height = (uint)Mathf.Max(Mathf.RoundToInt(m_scale.y * RTHandles.maxHeight), 1);

            m_Resolver.Init(width, height);
        }

        public void Resolve(CommandBuffer cmd, int width, int height)
        {
            uint resolveWidth = (uint)Mathf.Max(Mathf.RoundToInt(m_scale.x * width), 1);
            uint resolveHeight = (uint)Mathf.Max(Mathf.RoundToInt(m_scale.y * height), 1);

            m_Resolver.Process(cmd, opaqueHandle.nameID, 0, resolveWidth, 0, resolveHeight, 0, 0);
        }

        public void DestroyBuffers()
        {
            RTHandles.Release(opaqueHandle);
            opaqueHandle = null;
            m_Resolver.Dispose();
        }

        public RenderTargetIdentifier GetOpaqueRTI()
        {
            return opaqueHandle.nameID;
        }

        public void Clear(CommandBuffer cmd)
        {
            CoreUtils.SetRenderTarget(cmd, opaqueHandle.nameID, ClearFlag.Color, Color.white);
        }
    }
#endif
}
