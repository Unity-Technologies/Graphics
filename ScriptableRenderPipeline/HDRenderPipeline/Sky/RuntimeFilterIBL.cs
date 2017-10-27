using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class IBLFilterGGX
    {
        RenderTexture m_GgxIblSampleData;
        int           m_GgxIblMaxSampleCount          = TextureCache.isMobileBuildTarget ? 34 : 89;   // Width
        const int     k_GgxIblMipCountMinusOne        = 6;    // Height (UNITY_SPECCUBE_LOD_STEPS)

        ComputeShader m_ComputeGgxIblSampleDataCS;
        int           m_ComputeGgxIblSampleDataKernel = -1;

        ComputeShader m_BuildProbabilityTablesCS;
        int           m_ConditionalDensitiesKernel    = -1;
        int           m_MarginalRowDensitiesKernel    = -1;

        Material      m_GgxConvolveMaterial; // Convolves a cubemap with GGX

        RenderPipelineResources m_RenderPipelinesResources;

        public bool supportMis
        {
            get { return !TextureCache.isMobileBuildTarget; }
        }

        public IBLFilterGGX(RenderPipelineResources renderPipelinesResources)
        {
            m_RenderPipelinesResources = renderPipelinesResources;
        }

        public bool IsInitialized()
        {
            return m_GgxIblSampleData != null;
        }

        public void Initialize(CommandBuffer cmd)
        {
            if (!m_ComputeGgxIblSampleDataCS)
            {
                m_ComputeGgxIblSampleDataCS     = m_RenderPipelinesResources.computeGgxIblSampleData;
                m_ComputeGgxIblSampleDataKernel = m_ComputeGgxIblSampleDataCS.FindKernel("ComputeGgxIblSampleData");
            }

            if (!m_BuildProbabilityTablesCS && supportMis)
            {
                m_BuildProbabilityTablesCS   = m_RenderPipelinesResources.buildProbabilityTables;
                m_ConditionalDensitiesKernel = m_BuildProbabilityTablesCS.FindKernel("ComputeConditionalDensities");
                m_MarginalRowDensitiesKernel = m_BuildProbabilityTablesCS.FindKernel("ComputeMarginalRowDensities");
            }

            if (!m_GgxConvolveMaterial)
            {
                m_GgxConvolveMaterial = CoreUtils.CreateEngineMaterial(m_RenderPipelinesResources.GGXConvolve);
            }

            if (!m_GgxIblSampleData)
            {
                m_GgxIblSampleData = new RenderTexture(m_GgxIblMaxSampleCount, k_GgxIblMipCountMinusOne, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                m_GgxIblSampleData.useMipMap = false;
                m_GgxIblSampleData.autoGenerateMips = false;
                m_GgxIblSampleData.enableRandomWrite = true;
                m_GgxIblSampleData.filterMode = FilterMode.Point;
                m_GgxIblSampleData.Create();

                m_ComputeGgxIblSampleDataCS.SetTexture(m_ComputeGgxIblSampleDataKernel, "output", m_GgxIblSampleData);

                using (new ProfilingSample(cmd, "Compute GGX IBL Sample Data"))
                {
                    cmd.DispatchCompute(m_ComputeGgxIblSampleDataCS, m_ComputeGgxIblSampleDataKernel, 1, 1, 1);
                }
            }
        }

        void FilterCubemapCommon(CommandBuffer cmd,
            Texture source, RenderTexture target, int mipCount,
            Matrix4x4[] worldToViewMatrices)
        {
            // Solid angle associated with a texel of the cubemap.
            float invOmegaP = (6.0f * source.width * source.width) / (4.0f * Mathf.PI);

            m_GgxConvolveMaterial.SetTexture("_MainTex", source);
            m_GgxConvolveMaterial.SetTexture("_GgxIblSamples", m_GgxIblSampleData);
            m_GgxConvolveMaterial.SetFloat("_InvOmegaP", invOmegaP);

            for (int mip = 1; mip < ((int)EnvConstants.SpecCubeLodStep + 1); ++mip)
            {
                using (new ProfilingSample(cmd, "Filter Cubemap Mip {0}", mip))
                {
                    for (int face = 0; face < 6; ++face)
                    {
                        var faceSize = new Vector4(source.width >> mip, source.height >> mip, 1.0f / (source.width >> mip), 1.0f / (source.height >> mip));
                        var transform = SkyManager.ComputePixelCoordToWorldSpaceViewDirectionMatrix(0.5f * Mathf.PI, faceSize, worldToViewMatrices[face], true);

                        var props = new MaterialPropertyBlock();
                        props.SetFloat("_Level", mip);
                        props.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, transform);

                        CoreUtils.SetRenderTarget(cmd, target, ClearFlag.None, mip, (CubemapFace)face);
                        CoreUtils.DrawFullScreen(cmd, m_GgxConvolveMaterial, props);
                    }
                }
            }
        }

        // Filters MIP map levels (other than 0) with GGX using BRDF importance sampling.
        public void FilterCubemap(CommandBuffer cmd,
            Texture source, RenderTexture target, int mipCount,
            Matrix4x4[] worldToViewMatrices)
        {
            m_GgxConvolveMaterial.DisableKeyword("USE_MIS");

            FilterCubemapCommon(cmd, source, target, mipCount, worldToViewMatrices);
        }

        // Filters MIP map levels (other than 0) with GGX using multiple importance sampling.
        public void FilterCubemapMIS(CommandBuffer cmd,
            Texture source, RenderTexture target, int mipCount,
            RenderTexture conditionalCdf, RenderTexture marginalRowCdf,
            Matrix4x4[] worldToViewMatrices)
        {
            // Bind the input cubemap.
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "envMap", source);

            // Bind the outputs.
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "conditionalDensities", conditionalCdf);
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "marginalRowDensities", marginalRowCdf);
            m_BuildProbabilityTablesCS.SetTexture(m_MarginalRowDensitiesKernel, "marginalRowDensities", marginalRowCdf);

            int numRows = conditionalCdf.height;

            using (new ProfilingSample(cmd, "Build Probability Tables"))
            {
                cmd.DispatchCompute(m_BuildProbabilityTablesCS, m_ConditionalDensitiesKernel, numRows, 1, 1);
                cmd.DispatchCompute(m_BuildProbabilityTablesCS, m_MarginalRowDensitiesKernel, 1, 1, 1);
            }

            m_GgxConvolveMaterial.EnableKeyword("USE_MIS");
            m_GgxConvolveMaterial.SetTexture("_ConditionalDensities", conditionalCdf);
            m_GgxConvolveMaterial.SetTexture("_MarginalRowDensities", marginalRowCdf);

            FilterCubemapCommon(cmd, source, target, mipCount, worldToViewMatrices);
        }
    }
}
