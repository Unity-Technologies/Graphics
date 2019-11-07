using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.Rendering;
//using UnityEngine.Experimental.Rendering;
//using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering
{
    class ImportantSampler2D
    {
        //ComputeShader m_Shader;
        //int k_SampleKernel_xyzw2x_8;
        //int k_SampleKernel_xyzw2x_1;
        ComputeShader m_ComputeCDF;
        ComputeShader m_ComputeInvCDF;

        Texture2D       m_CFDinv; // Cumulative Function Distribution Inverse
        ComputeBuffer   m_GeneratedSamples;
        RTHandle        m_Temp0; // Used for Ping/Pong to compute the sum of column
        RTHandle        m_Temp1;

        int             m_Width;
        int             m_Height;

        static int m_SumPerThread0 = 16;
        static int m_SumPerThread1 =  4;

        public ImportantSampler2D()
        {
            //m_Shader = shader;
            //k_SampleKernel_xyzw2x_8 = m_Shader.FindKernel("KSampleCopy4_1_x_8");
            //k_SampleKernel_xyzw2x_1 = m_Shader.FindKernel("KSampleCopy4_1_x_1");
            var hdrp = HDRenderPipeline.defaultAsset;
            m_ComputeCDF = hdrp.renderPipelineResources.shaders.sum2DCS;
        }

        public void Init(Texture2D pdfDensity, CommandBuffer cmd)
        {
            //if (pdfDensity == null)
            //{
            //    return;
            //}

            m_Width     = pdfDensity.width;
            m_Height    = pdfDensity.height;
            //RTHandles desc = RTHandles.Alloc(width: m_Width, height: m_Height/m_SumPerThread0, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, depthBufferBits: 0, enableRandomWrite: true);;
            //desc.enableRandomWrite = true;

            //m_Temp0 = new RenderTexture(m_Width, m_Height/m_SumPerThread0,                   1, GraphicsFormat.R32G32B32A32_SFloat);
            //m_Temp1 = new RenderTexture(m_Width, m_Height/(m_SumPerThread0*m_SumPerThread1), 1, GraphicsFormat.R32G32B32A32_SFloat);

            m_Temp0 = RTHandles.Alloc(  m_Width, m_Height/m_SumPerThread0, 1,
                                        DepthBits.None,
                                        GraphicsFormat.R32G32B32A32_SFloat,
                                        FilterMode.Trilinear,
                                        TextureWrapMode.Repeat,
                                        TextureDimension.Tex2D,
                                        true);
            m_Temp1 = RTHandles.Alloc(  m_Width, m_Height/(m_SumPerThread0*m_SumPerThread1), 1,
                                        DepthBits.None,
                                        GraphicsFormat.R32G32B32A32_SFloat,
                                        FilterMode.Trilinear,
                                        TextureWrapMode.Repeat,
                                        TextureDimension.Tex2D,
                                        true);
            //m_Temp0 = RTHandles.Alloc(width: m_Width, height: m_Height/m_SumPerThread0,                   slices: 1, colorFormat: GraphicsFormat.R32G32B32A32_SFloat);
            //m_Temp1 = RTHandles.Alloc(width: m_Width, height: m_Height/(m_SumPerThread0*m_SumPerThread1), slices: 1, colorFormat: GraphicsFormat.R32G32B32A32_SFloat);

            Texture2D readBack = new Texture2D(m_Width, m_Height/m_SumPerThread0, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None);

            int passCount = Mathf.RoundToInt(Mathf.Log((float)m_Height, 2.0f));

            cmd.SetComputeTextureParam(m_ComputeCDF, 0, HDShaderIDs._Input, pdfDensity);
            cmd.SetComputeTextureParam(m_ComputeCDF, 0, HDShaderIDs._Output, m_Temp0);
            int numTilesY = (m_Height + (m_SumPerThread0 - 1))/m_SumPerThread0;
            cmd.DispatchCompute(m_ComputeCDF, 0, m_Width, numTilesY, 1 );

            RenderTexture.active = m_Temp0;
            readBack.ReadPixels(new Rect(0.0f, 0.0f, m_Width, m_Height/m_SumPerThread0), 0, 0);
            readBack.Apply();
            RenderTexture.active = null;

            byte[] bytes = readBack.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
            System.IO.File.WriteAllBytes(@"C:\UProjects\005\Assets\MidSum.exr", bytes);
            CoreUtils.Destroy(readBack);
            CoreUtils.Destroy(m_Temp0);
            CoreUtils.Destroy(m_Temp1);
        }

        public void GenerateSamples(uint samplesCount)
        {
            
        }

        /*
        static readonly int _RectOffset = Shader.PropertyToID("_RectOffset");
        static readonly int _Result1 = Shader.PropertyToID("_Result1");
        static readonly int _Source4 = Shader.PropertyToID("_Source4");
        static int[] _IntParams = new int[2];

        void SampleCopyChannel(
            CommandBuffer cmd,
            Rendering.RectInt rect,
            int _source,
            RenderTargetIdentifier source,
            int _target,
            RenderTargetIdentifier target,
            int slices,
            int kernel8,
            int kernel1)
        {
            Rendering.RectInt main, topRow, rightCol, topRight;
            unsafe
            {
                Rendering.RectInt* dispatch1Rects = stackalloc Rendering.RectInt[3];
                int dispatch1RectCount = 0;
                Rendering.RectInt dispatch8Rect = Rendering.RectInt.zero;

                if (TileLayoutUtils.TryLayoutByTiles(
                    rect,
                    8,
                    out main,
                    out topRow,
                    out rightCol,
                    out topRight))
                {
                    if (topRow.width > 0 && topRow.height > 0)
                    {
                        dispatch1Rects[dispatch1RectCount] = topRow;
                        ++dispatch1RectCount;
                    }
                    if (rightCol.width > 0 && rightCol.height > 0)
                    {
                        dispatch1Rects[dispatch1RectCount] = rightCol;
                        ++dispatch1RectCount;
                    }
                    if (topRight.width > 0 && topRight.height > 0)
                    {
                        dispatch1Rects[dispatch1RectCount] = topRight;
                        ++dispatch1RectCount;
                    }
                    dispatch8Rect = main;
                }
                else if (rect.width > 0 && rect.height > 0)
                {
                    dispatch1Rects[dispatch1RectCount] = rect;
                    ++dispatch1RectCount;
                }

                cmd.SetComputeTextureParam(m_Shader, kernel8, _source, source);
                cmd.SetComputeTextureParam(m_Shader, kernel1, _source, source);
                cmd.SetComputeTextureParam(m_Shader, kernel8, _target, target);
                cmd.SetComputeTextureParam(m_Shader, kernel1, _target, target);

                if (dispatch8Rect.width > 0 && dispatch8Rect.height > 0)
                {
                    var r = dispatch8Rect;
                    // Use intermediate array to avoid garbage
                    _IntParams[0] = r.x;
                    _IntParams[1] = r.y;
                    cmd.SetComputeIntParams(m_Shader, _RectOffset, _IntParams);
                    cmd.DispatchCompute(m_Shader, kernel8, (int)Mathf.Max(r.width / 8, 1), (int)Mathf.Max(r.height / 8, 1), slices);
                }

                for (int i = 0, c = dispatch1RectCount; i < c; ++i)
                {
                    var r = dispatch1Rects[i];
                    // Use intermediate array to avoid garbage
                    _IntParams[0] = r.x;
                    _IntParams[1] = r.y;
                    cmd.SetComputeIntParams(m_Shader, _RectOffset, _IntParams);
                    cmd.DispatchCompute(m_Shader, kernel1, (int)Mathf.Max(r.width, 1), (int)Mathf.Max(r.height, 1), slices);
                }
            }
        }
        public void SampleCopyChannel_xyzw2x(CommandBuffer cmd, RTHandle source, RTHandle target, Rendering.RectInt rect)
        {
            Debug.Assert(source.rt.volumeDepth == target.rt.volumeDepth);
            SampleCopyChannel(cmd, rect, _Source4, source, _Result1, target, source.rt.volumeDepth, k_SampleKernel_xyzw2x_8, k_SampleKernel_xyzw2x_1);
        }
        */
    }
}
