using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class DBufferManager : MRTBufferManager
    {
        ComputeBuffer   m_PropertyMaskBuffer;
        ComputeShader   m_ClearPropertyMaskBufferShader;
        int m_ClearPropertyMaskBufferKernel;


        public DBufferManager()
            : base(Decal.GetMaterialDBufferCount())
        {
            Debug.Assert(m_BufferCount <= 4);
        }

        public RTHandle[] GetRTHandles() { return m_RTs; }

        public ComputeBuffer propertyMaskBuffer
        {
            get
            {
                return m_PropertyMaskBuffer;
            }
        }

        public int clearPropertyMaskBufferKernel
        {
            get
            {
                return m_ClearPropertyMaskBufferKernel;
            }
        }

        public ComputeShader clearPropertyMaskBufferShader
        {
            get
            {
                return m_ClearPropertyMaskBufferShader;
            }
        }

        public int GetPropertyMaskBufferSize(int width, int height)
        {
            int propertyMaskBufferSize = ((width + 7) / 8) * ((height + 7) / 8);
            propertyMaskBufferSize = ((propertyMaskBufferSize + 63) / 64) * 64;
            return propertyMaskBufferSize;
        }

        public override void CreateBuffers()
        {
            GraphicsFormat[] rtFormat;
            Decal.GetMaterialDBufferDescription(out rtFormat);

            for (int dbufferIndex = 0; dbufferIndex < m_BufferCount; ++dbufferIndex)
            {
                m_RTs[dbufferIndex] = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: rtFormat[dbufferIndex], dimension: TextureXR.dimension, useDynamicScale: true, name: string.Format("DBuffer{0}", dbufferIndex));
                m_RTIDs[dbufferIndex] = m_RTs[dbufferIndex].nameID;
                m_TextureShaderIDs[dbufferIndex] = HDShaderIDs._DBufferTexture[dbufferIndex];
            }
        }

        public void InitializeHDRPResouces(HDRenderPipelineAsset asset)
        {
            m_ClearPropertyMaskBufferShader = asset.renderPipelineResources.shaders.decalClearPropertyMaskBufferCS;
            m_ClearPropertyMaskBufferKernel = m_ClearPropertyMaskBufferShader.FindKernel("CSMain");
        }

        public void ReleaseResolutionDependentBuffers()
        {
            if(m_PropertyMaskBuffer != null)
            {
                m_PropertyMaskBuffer.Dispose();
                m_PropertyMaskBuffer = null;
            }
        }

        public void AllocResolutionDependentBuffers(int width, int height)
        {
            m_PropertyMaskBuffer = new ComputeBuffer(GetPropertyMaskBufferSize(width, height), 4);
        }

        override public void DestroyBuffers()
        {
            base.DestroyBuffers();
        }

        public void BindBlackTextures(CommandBuffer cmd)
        {
            for (int i = 0; i < m_BufferCount; ++i)
            {
                cmd.SetGlobalTexture(m_TextureShaderIDs[i], TextureXR.GetBlackTexture());
            }
        }
    }
}
