using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class IBLFilterGGX
    {
        RenderTexture m_GgxIblSampleData              = null;
        int           k_GgxIblMaxSampleCount          = TextureCache.isMobileBuildTarget ? 34 : 89;   // Width
        const int     k_GgxIblMipCountMinusOne        = 6;    // Height (UNITY_SPECCUBE_LOD_STEPS)

        ComputeShader m_ComputeGgxIblSampleDataCS     = null;
        int           m_ComputeGgxIblSampleDataKernel = -1;

        ComputeShader m_BuildProbabilityTablesCS      = null;
        int           m_ConditionalDensitiesKernel    = -1;
        int           m_MarginalRowDensitiesKernel    = -1;

        Material      m_GgxConvolveMaterial           = null; // Convolves a cubemap with GGX

        bool          m_SupportMIS = !TextureCache.isMobileBuildTarget;

        RenderPipelineResources m_RenderPipelinesResources;

        public IBLFilterGGX(RenderPipelineResources renderPipelinesResources)
        {
            m_RenderPipelinesResources = renderPipelinesResources;
        }

        public bool IsInitialized()
        {
            return m_GgxIblSampleData != null;
        }

        public bool SupportMIS
        {
            get { return m_SupportMIS; }
        }

        public void Initialize(ScriptableRenderContext context)
        {
            if (!m_ComputeGgxIblSampleDataCS)
            {
                m_ComputeGgxIblSampleDataCS     = m_RenderPipelinesResources.computeGgxIblSampleData;
                m_ComputeGgxIblSampleDataKernel = m_ComputeGgxIblSampleDataCS.FindKernel("ComputeGgxIblSampleData");
            }

            if (!m_BuildProbabilityTablesCS && SupportMIS)
            {
                m_BuildProbabilityTablesCS   = m_RenderPipelinesResources.buildProbabilityTables;
                m_ConditionalDensitiesKernel = m_BuildProbabilityTablesCS.FindKernel("ComputeConditionalDensities");
                m_MarginalRowDensitiesKernel = m_BuildProbabilityTablesCS.FindKernel("ComputeMarginalRowDensities");
            }

            if (!m_GgxConvolveMaterial)
            {
                m_GgxConvolveMaterial = Utilities.CreateEngineMaterial(m_RenderPipelinesResources.GGXConvolve);
            }

            if (!m_GgxIblSampleData)
            {
                m_GgxIblSampleData = new RenderTexture(k_GgxIblMaxSampleCount, k_GgxIblMipCountMinusOne, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                m_GgxIblSampleData.useMipMap = false;
                m_GgxIblSampleData.autoGenerateMips = false;
                m_GgxIblSampleData.enableRandomWrite = true;
                m_GgxIblSampleData.filterMode = FilterMode.Point;
                m_GgxIblSampleData.Create();

                m_ComputeGgxIblSampleDataCS.SetTexture(m_ComputeGgxIblSampleDataKernel, "output", m_GgxIblSampleData);

                var cmd = new CommandBuffer() { name = "Compute GGX IBL Sample Data" };
                cmd.DispatchCompute(m_ComputeGgxIblSampleDataCS, m_ComputeGgxIblSampleDataKernel, 1, 1, 1);
                context.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }
        }

        void FilterCubemapCommon(ScriptableRenderContext context,
            Texture source, RenderTexture target, int mipCount,
            Mesh[] cubemapFaceMesh)
        {
            // Solid angle associated with a texel of the cubemap.
            float invOmegaP = (6.0f * source.width * source.width) / (4.0f * Mathf.PI);

            m_GgxConvolveMaterial.SetTexture("_MainTex", source);
            m_GgxConvolveMaterial.SetTexture("_GgxIblSamples", m_GgxIblSampleData);
            m_GgxConvolveMaterial.SetFloat("_LastLevel", mipCount - 1);
            m_GgxConvolveMaterial.SetFloat("_InvOmegaP", invOmegaP);

            for (int mip = 1; mip < ((int)EnvConstants.SpecCubeLodStep + 1); ++mip)
            {
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                props.SetFloat("_Level", mip);

                for (int face = 0; face < 6; ++face)
                {
                    Utilities.SetRenderTarget(context, target, ClearFlag.ClearNone, mip, (CubemapFace)face);

                    var cmd = new CommandBuffer { name = "" };
                    cmd.DrawMesh(cubemapFaceMesh[face], Matrix4x4.identity, m_GgxConvolveMaterial, 0, 0, props);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Dispose();
                }
            }
        }

        // Filters MIP map levels (other than 0) with GGX using BRDF importance sampling.
        public void FilterCubemap(ScriptableRenderContext context,
            Texture source, RenderTexture target, int mipCount,
            Mesh[] cubemapFaceMesh)
        {
            m_GgxConvolveMaterial.DisableKeyword("USE_MIS");

            FilterCubemapCommon(context, source, target, mipCount, cubemapFaceMesh);
        }

        // Filters MIP map levels (other than 0) with GGX using multiple importance sampling.
        public void FilterCubemapMIS(ScriptableRenderContext context,
            Texture source, RenderTexture target, int mipCount,
            RenderTexture conditionalCdf, RenderTexture marginalRowCdf,
            Mesh[] cubemapFaceMesh)
        {
            // Bind the input cubemap.
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "envMap", source);

            // Bind the outputs.
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "conditionalDensities", conditionalCdf);
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "marginalRowDensities", marginalRowCdf);
            m_BuildProbabilityTablesCS.SetTexture(m_MarginalRowDensitiesKernel, "marginalRowDensities", marginalRowCdf);

            int numRows = conditionalCdf.height;

            var cmd = new CommandBuffer() { name = "Build Probability Tables" };
            cmd.DispatchCompute(m_BuildProbabilityTablesCS, m_ConditionalDensitiesKernel, numRows, 1, 1);
            cmd.DispatchCompute(m_BuildProbabilityTablesCS, m_MarginalRowDensitiesKernel, 1, 1, 1);
            context.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            m_GgxConvolveMaterial.EnableKeyword("USE_MIS");
            m_GgxConvolveMaterial.SetTexture("_ConditionalDensities", conditionalCdf);
            m_GgxConvolveMaterial.SetTexture("_MarginalRowDensities", marginalRowCdf);

            FilterCubemapCommon(context, source, target, mipCount, cubemapFaceMesh);
        }
    }
}
