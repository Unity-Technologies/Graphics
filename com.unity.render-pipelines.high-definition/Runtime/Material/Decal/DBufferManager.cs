using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class DBufferManager : MRTBufferManager
    {       
        ComputeBuffer   m_PropertyMaskBuffer;
        int m_PropertyMaskBufferSize;
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

        public int propertyMaskBufferSize
        {
            get
            {
                return m_PropertyMaskBufferSize;
            }
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

        public void AllocResolutionDependentBuffers(HDCamera hdCamera, int width, int height)
        {   
            m_PropertyMaskBufferSize = ((width + 7) / 8) * ((height + 7) / 8);
            m_PropertyMaskBufferSize = ((m_PropertyMaskBufferSize + 63) / 64) * 64; // round off to nearest multiple of 64 for ease of use in CS
            m_PropertyMaskBuffer = new ComputeBuffer(m_PropertyMaskBufferSize, 4);
        }

        override public void DestroyBuffers()
        {
            base.DestroyBuffers();
            ReleaseResolutionDependentBuffers();
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
