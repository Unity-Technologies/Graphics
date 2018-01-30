using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class ColorPyramid
    {
        static readonly int _Size = Shader.PropertyToID("_Size");
        static readonly int _Source = Shader.PropertyToID("_Source");
        static readonly int _Result = Shader.PropertyToID("_Result");

        ComputeShader m_ColorPyramidCS;

        RenderTextureDescriptor m_RenderTextureDescriptor;
        int[] m_ColorPyramidMips = new int[0];
        int m_ColorPyramidKernel;

        public RenderTextureDescriptor renderTextureDescriptor { get { return m_RenderTextureDescriptor; } }

        public int usedMipMapCount { get { return Mathf.Min(bufferMipMapCount, m_ColorPyramidMips.Length); } }

        public int bufferMipMapCount
        {
            get
            {
                var minSize = Mathf.Min(renderTextureDescriptor.width, renderTextureDescriptor.height);
                return Mathf.FloorToInt(Mathf.Log(minSize, 2f));
            }
        }

        public ColorPyramid(ComputeShader colorPyramidCS, int[] mipIds)
        {
            m_ColorPyramidCS = colorPyramidCS;

            m_ColorPyramidKernel = m_ColorPyramidCS.FindKernel("KMain");
            m_ColorPyramidMips = mipIds;
        }

        public void RenderPyramidColor(
            HDCamera hdCamera,
            CommandBuffer cmd, 
            ScriptableRenderContext renderContext,
            RenderTargetIdentifier colorTexture,
            RenderTargetIdentifier targetTexture)
        {
            var colorPyramidDesc = renderTextureDescriptor;

            var lodCount = bufferMipMapCount;
            if (lodCount > m_ColorPyramidMips.Length)
            {
                Debug.LogWarningFormat("Cannot compute all mipmaps of the color pyramid, max texture size supported: {0}", (2 << m_ColorPyramidMips.Length).ToString());
                lodCount = m_ColorPyramidMips.Length;
            }

            // Copy mip 0
            cmd.CopyTexture(colorTexture, 0, 0, targetTexture, 0, 0);
            var last = colorTexture;

            colorPyramidDesc.sRGB = false;
            colorPyramidDesc.enableRandomWrite = true;
            colorPyramidDesc.useMipMap = false;

            for (var i = 0; i < lodCount; i++)
            {
                colorPyramidDesc.width = colorPyramidDesc.width >> 1;
                colorPyramidDesc.height = colorPyramidDesc.height >> 1;

                // TODO: Add proper stereo support to the compute job

                cmd.ReleaseTemporaryRT(m_ColorPyramidMips[i + 1]);
                cmd.GetTemporaryRT(m_ColorPyramidMips[i + 1], colorPyramidDesc, FilterMode.Bilinear);
                cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorPyramidKernel, _Source, last);
                cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorPyramidKernel, _Result, m_ColorPyramidMips[i + 1]);
                cmd.SetComputeVectorParam(m_ColorPyramidCS, _Size, new Vector4(colorPyramidDesc.width, colorPyramidDesc.height, 1f / colorPyramidDesc.width, 1f / colorPyramidDesc.height));
                cmd.DispatchCompute(
                    m_ColorPyramidCS,
                    m_ColorPyramidKernel,
                    Mathf.CeilToInt(colorPyramidDesc.width / 8f),
                    Mathf.CeilToInt(colorPyramidDesc.height / 8f),
                    1);
                cmd.CopyTexture(m_ColorPyramidMips[i + 1], 0, 0, targetTexture, 0, i + 1);

                last = m_ColorPyramidMips[i + 1];
            }

            for (int i = 0; i < lodCount; i++)
                cmd.ReleaseTemporaryRT(m_ColorPyramidMips[i + 1]);
        }

        public void Initialize(HDCamera hdCamera, bool enableStereo)
        {
            var desc = PyramidUtils.CalculateRenderTextureDescriptor(hdCamera, enableStereo);
            desc.colorFormat = RenderTextureFormat.ARGBHalf;
            m_RenderTextureDescriptor = desc;
        }
    }
}
