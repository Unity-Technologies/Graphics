using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class IBLFilterGGX : IBLFilterBSDF
    {
        RenderTexture m_GgxIblSampleData;
        int           m_GgxIblMaxSampleCount          = TextureCache.isMobileBuildTarget ? 34 : 89;   // Width
        const int     k_GgxIblMipCountMinusOne        = 6;    // Height (UNITY_SPECCUBE_LOD_STEPS)

        ComputeShader m_ComputeGgxIblSampleDataCS;
        int           m_ComputeGgxIblSampleDataKernel = -1;

        ComputeShader m_BuildProbabilityTablesCS;
        int           m_ConditionalDensitiesKernel    = -1;
        int           m_MarginalRowDensitiesKernel    = -1;

        // Planar reflection filtering
        ComputeShader m_PlanarReflectionFilteringCS;
        Texture2D m_ThetaValuesTexture;
        int           m_PlanarReflectionFilteringKernel = -1;
        int           m_PlanarReflectionDownScaleKernel = -1;
        RTHandle      m_PlanarReflectionFilterTex0;
        RTHandle      m_PlanarReflectionFilterTex1;
        RTHandle      m_PlanarReflectionFilterDepthTex0;
        RTHandle      m_PlanarReflectionFilterDepthTex1;

        public IBLFilterGGX(RenderPipelineResources renderPipelineResources, MipGenerator mipGenerator)
        {
            m_RenderPipelineResources = renderPipelineResources;
            m_MipGenerator = mipGenerator;
        }

        public override bool IsInitialized()
        {
            return m_GgxIblSampleData != null;
        }

        public override void Initialize(CommandBuffer cmd)
        {
            if (!m_ComputeGgxIblSampleDataCS)
            {
                m_ComputeGgxIblSampleDataCS     = m_RenderPipelineResources.shaders.computeGgxIblSampleDataCS;
                m_ComputeGgxIblSampleDataKernel = m_ComputeGgxIblSampleDataCS.FindKernel("ComputeGgxIblSampleData");
            }

            if (!m_BuildProbabilityTablesCS)
            {
                m_BuildProbabilityTablesCS   = m_RenderPipelineResources.shaders.buildProbabilityTablesCS;
                m_ConditionalDensitiesKernel = m_BuildProbabilityTablesCS.FindKernel("ComputeConditionalDensities");
                m_MarginalRowDensitiesKernel = m_BuildProbabilityTablesCS.FindKernel("ComputeMarginalRowDensities");
            }

            if (!m_convolveMaterial)
            {
                m_convolveMaterial = CoreUtils.CreateEngineMaterial(m_RenderPipelineResources.shaders.GGXConvolvePS);
            }

            if (!m_GgxIblSampleData)
            {
                m_GgxIblSampleData = new RenderTexture(m_GgxIblMaxSampleCount, k_GgxIblMipCountMinusOne, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                m_GgxIblSampleData.useMipMap = false;
                m_GgxIblSampleData.autoGenerateMips = false;
                m_GgxIblSampleData.enableRandomWrite = true;
                m_GgxIblSampleData.filterMode = FilterMode.Point;
                m_GgxIblSampleData.name = CoreUtils.GetRenderTargetAutoName(m_GgxIblMaxSampleCount, k_GgxIblMipCountMinusOne, 1, RenderTextureFormat.ARGBHalf, "GGXIblSampleData");
                m_GgxIblSampleData.hideFlags = HideFlags.HideAndDontSave;
                m_GgxIblSampleData.Create();

                InitializeGgxIblSampleData(cmd);
            }

            if (!m_PlanarReflectionFilteringCS)
            {
                m_PlanarReflectionFilteringCS     = m_RenderPipelineResources.shaders.planarReflectionFilteringCS;
                m_PlanarReflectionFilteringKernel = m_PlanarReflectionFilteringCS.FindKernel("FilterPlanarReflection");
                m_PlanarReflectionDownScaleKernel = m_PlanarReflectionFilteringCS.FindKernel("DownScaleReflection");
                m_ThetaValuesTexture = m_RenderPipelineResources.textures.ThetaValues;
            }

            for (int i = 0; i < 6; ++i)
            {
                var lookAt = Matrix4x4.LookAt(Vector3.zero, CoreUtils.lookAtList[i], CoreUtils.upVectorList[i]);
                m_faceWorldToViewMatrixMatrices[i] = lookAt * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API. ...
            }

            m_PlanarReflectionFilterTex0 = RTHandles.Alloc(512, 512, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "PlanarReflectionTextureIntermediate0");
            m_PlanarReflectionFilterTex1 = RTHandles.Alloc(512, 512, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "PlanarReflectionTextureIntermediate1");
            m_PlanarReflectionFilterDepthTex0 = RTHandles.Alloc(512, 512, TextureXR.slices, colorFormat: GraphicsFormat.R32_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "PlanarReflectionTextureIntermediateDepth0");
            m_PlanarReflectionFilterDepthTex1 = RTHandles.Alloc(512, 512, TextureXR.slices, colorFormat: GraphicsFormat.R32_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "PlanarReflectionTextureIntermediateDepth1");
        }

        void InitializeGgxIblSampleData(CommandBuffer cmd)
        {
            m_ComputeGgxIblSampleDataCS.SetTexture(m_ComputeGgxIblSampleDataKernel, "outputResult", m_GgxIblSampleData);
            cmd.DispatchCompute(m_ComputeGgxIblSampleDataCS, m_ComputeGgxIblSampleDataKernel, 1, 1, 1);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_convolveMaterial);
            CoreUtils.Destroy(m_GgxIblSampleData);
            RTHandles.Release(m_PlanarReflectionFilterTex0);
            RTHandles.Release(m_PlanarReflectionFilterTex1);
            RTHandles.Release(m_PlanarReflectionFilterDepthTex0);
            RTHandles.Release(m_PlanarReflectionFilterDepthTex1);
        }

        void FilterCubemapCommon(CommandBuffer cmd,
            Texture source, RenderTexture target,
            Matrix4x4[] worldToViewMatrices)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.FilterCubemapGGX)))
            {
                int mipCount = 1 + (int)Mathf.Log(source.width, 2.0f);
                if (mipCount < (int)EnvConstants.ConvolutionMipCount)
                {
                    Debug.LogWarning("RenderCubemapGGXConvolution: Cubemap size is too small for GGX convolution, needs at least " + (int)EnvConstants.ConvolutionMipCount + " mip levels");
                    return;
                }

                // Copy the first mip
                for (int f = 0; f < 6; f++)
                {
                    cmd.CopyTexture(source, f, 0, target, f, 0);
                }

                // Solid angle associated with a texel of the cubemap.
                float invOmegaP = (6.0f * source.width * source.width) / (4.0f * Mathf.PI);

                if (!m_GgxIblSampleData.IsCreated())
                {
                    m_GgxIblSampleData.Create();
                    InitializeGgxIblSampleData(cmd);
                }

                m_convolveMaterial.SetTexture("_GgxIblSamples", m_GgxIblSampleData);

                var props = new MaterialPropertyBlock();
                props.SetTexture("_MainTex", source);
                props.SetFloat("_InvOmegaP", invOmegaP);

                for (int mip = 1; mip < (int)EnvConstants.ConvolutionMipCount; ++mip)
                {
                    props.SetFloat("_Level", mip);

                    for (int face = 0; face < 6; ++face)
                    {
                        var faceSize = new Vector4(source.width >> mip, source.height >> mip, 1.0f / (source.width >> mip), 1.0f / (source.height >> mip));
                        var transform = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(0.5f * Mathf.PI, Vector2.zero, faceSize, worldToViewMatrices[face], true);

                        props.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, transform);

                        CoreUtils.SetRenderTarget(cmd, target, ClearFlag.None, mip, (CubemapFace)face);
                        CoreUtils.DrawFullScreen(cmd, m_convolveMaterial, props);
                    }
                }
            }
        }

        // Filters MIP map levels (other than 0) with GGX using multiple importance sampling.
        override public void FilterCubemapMIS(CommandBuffer cmd,
            Texture source, RenderTexture target,
            RenderTexture conditionalCdf, RenderTexture marginalRowCdf)
        {
            // Bind the input cubemap.
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "envMap", source);

            // Bind the outputs.
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "conditionalDensities", conditionalCdf);
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "marginalRowDensities", marginalRowCdf);
            m_BuildProbabilityTablesCS.SetTexture(m_MarginalRowDensitiesKernel, "marginalRowDensities", marginalRowCdf);

            int numRows = conditionalCdf.height;

            cmd.DispatchCompute(m_BuildProbabilityTablesCS, m_ConditionalDensitiesKernel, numRows, 1, 1);
            cmd.DispatchCompute(m_BuildProbabilityTablesCS, m_MarginalRowDensitiesKernel, 1, 1, 1);

            m_convolveMaterial.EnableKeyword("USE_MIS");
            m_convolveMaterial.SetTexture("_ConditionalDensities", conditionalCdf);
            m_convolveMaterial.SetTexture("_MarginalRowDensities", marginalRowCdf);

            FilterCubemapCommon(cmd, source, target, m_faceWorldToViewMatrixMatrices);
        }

        override public void FilterCubemap(CommandBuffer cmd, Texture source, RenderTexture target)
        {
            FilterCubemapCommon(cmd, source, target, m_faceWorldToViewMatrixMatrices);
        }

        float RoughnessStep(int textureResolution)
        {
            return 1.0f / (Mathf.Log((float)textureResolution, 2.0f) - 2.0f);
        }

        override public void FilterPlanarTexture(CommandBuffer cmd, RenderTexture source, ref PlanarTextureFilteringParameters planarTextureFilteringParameters, RenderTexture target)
        {
            // Texture dimensions
            int texWidth = source.width;
            int texHeight = source.height;

            // First we need to make sure that our intermediate textures are the right size (these textures are squares)
            if (m_PlanarReflectionFilterTex0.rt.width < texWidth)
            {
                RTHandles.Release(m_PlanarReflectionFilterTex0);
                RTHandles.Release(m_PlanarReflectionFilterTex1);
                m_PlanarReflectionFilterTex0 = RTHandles.Alloc(texWidth, texHeight, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: true, name: "PlanarReflectionTextureIntermediate0");
                m_PlanarReflectionFilterTex1 = RTHandles.Alloc(texWidth, texHeight, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "PlanarReflectionTextureIntermediate1");
                m_PlanarReflectionFilterDepthTex0 = RTHandles.Alloc(texWidth, texHeight, TextureXR.slices, colorFormat: GraphicsFormat.R32_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: true, name: "PlanarReflectionTextureIntermediateDepth0");
                m_PlanarReflectionFilterDepthTex1 = RTHandles.Alloc(texWidth, texHeight, TextureXR.slices, colorFormat: GraphicsFormat.R32_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "PlanarReflectionTextureIntermediateDepth1");
            }

            // First we need to build a mip chain that we will sample later on in the process
            cmd.CopyTexture(source, 0, 0, 0, 0, texWidth, texHeight, m_PlanarReflectionFilterTex0, 0, 0, 0, 0);
            cmd.CopyTexture(planarTextureFilteringParameters.captureCameraDepthBuffer, 0, 0, 0, 0, texWidth, texHeight, m_PlanarReflectionFilterDepthTex0, 0, 0, 0, 0);

            // Move to the next mip
            texWidth = texWidth >> 1;
            texHeight = texHeight >> 1;
            int areaTileSize = 8;
            int currentMipLevel = 0;

            while (texWidth >= 8 && texHeight >= 8)
            {
                int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
                int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, HDShaderIDs._ReflectionColorMipChain, m_PlanarReflectionFilterTex0);
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, HDShaderIDs._HalfResReflectionBuffer, m_PlanarReflectionFilterTex1);
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, HDShaderIDs._DepthTexture, m_PlanarReflectionFilterDepthTex0);
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, HDShaderIDs._HalfResDepthBuffer, m_PlanarReflectionFilterDepthTex1);
                cmd.SetComputeIntParam(m_PlanarReflectionFilteringCS, "_MipIndex", currentMipLevel);
                cmd.DispatchCompute(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, numTilesXHR, numTilesYHR, 1);
                cmd.CopyTexture(m_PlanarReflectionFilterTex1, 0, 0, 0, 0, texWidth, texHeight, m_PlanarReflectionFilterTex0, 0, currentMipLevel + 1, 0, 0);
                cmd.CopyTexture(m_PlanarReflectionFilterDepthTex1, 0, 0, 0, 0, texWidth, texHeight, m_PlanarReflectionFilterDepthTex0, 0, currentMipLevel + 1, 0, 0);

                // Move to the next mip
                texWidth = texWidth >> 1;
                texHeight = texHeight >> 1;
                currentMipLevel++;
            }

            // Reset the mip descent
            texWidth = source.width;
            texHeight = source.height;

            // First we need to copy the Mip0 (that matches perfectly smooth surface)
            cmd.CopyTexture(m_PlanarReflectionFilterTex0, 0, 0, 0, 0, texWidth, texHeight, target, 0, 0, 0, 0);
            int mipIndex = 1;

            // Move to the next mip
            texWidth = texWidth >> 1;
            texHeight = texHeight >> 1;

            int numTilesXHRHalf = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesYHRHalf = (texHeight + (areaTileSize - 1)) / areaTileSize;
            cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, HDShaderIDs._ReflectionColorMipChain, source);
            cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, HDShaderIDs._HalfResReflectionBuffer, m_PlanarReflectionFilterTex0);
            cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, HDShaderIDs._DepthTexture, planarTextureFilteringParameters.captureCameraDepthBuffer);
            cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, HDShaderIDs._HalfResDepthBuffer, m_PlanarReflectionFilterDepthTex0);
            cmd.DispatchCompute(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, numTilesXHRHalf, numTilesYHRHalf, 1);

            RTHandle colorSource = m_PlanarReflectionFilterTex0;
            RTHandle colorTarget = m_PlanarReflectionFilterTex1;
            RTHandle depthSource = m_PlanarReflectionFilterDepthTex0;
            RTHandle depthTarget = m_PlanarReflectionFilterDepthTex1;

            float roughnessStep = RoughnessStep(texWidth);
            float currentRoughness = roughnessStep;

            while (texWidth >= 8 && texHeight >= 8)
            {
                // Evaluate the dispatch parameters
                int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
                int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionFilteringKernel, HDShaderIDs._DepthTexture, depthSource);
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionFilteringKernel, HDShaderIDs._ReflectionColorMipChain, colorSource);
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionFilteringKernel, HDShaderIDs._FilteredPlanarReflectionBuffer, colorTarget);
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionFilteringKernel, "_ThetaValuesTexture", m_ThetaValuesTexture);

                // TODO: move to constant buffer
                cmd.SetComputeIntParam(m_PlanarReflectionFilteringCS, HDShaderIDs._FilterSizeRadius, 16);
                cmd.SetComputeVectorParam(m_PlanarReflectionFilteringCS, HDShaderIDs._ReflectionPlaneNormal, planarTextureFilteringParameters.probeNormal);
                cmd.SetComputeVectorParam(m_PlanarReflectionFilteringCS, HDShaderIDs._ReflectionPlanePosition, planarTextureFilteringParameters.probePosition);
                cmd.SetComputeVectorParam(m_PlanarReflectionFilteringCS, HDShaderIDs._CaptureScreenSize, planarTextureFilteringParameters.captureCameraScreenSize);
                cmd.SetComputeVectorParam(m_PlanarReflectionFilteringCS, "_CaptureScreenSize2", new Vector4(texWidth, texHeight, 1.0f / texWidth, 1.0f / texHeight));
                cmd.SetComputeVectorParam(m_PlanarReflectionFilteringCS, HDShaderIDs._CaptureCameraPositon, planarTextureFilteringParameters.captureCameraPosition);
                cmd.SetComputeVectorParam(m_PlanarReflectionFilteringCS, "_CaptureCameraUpVector", planarTextureFilteringParameters.captureCameraUp);
                cmd.SetComputeVectorParam(m_PlanarReflectionFilteringCS, "_CaptureCameraRightVector", planarTextureFilteringParameters.captureCameraRight);
                cmd.SetComputeFloatParam(m_PlanarReflectionFilteringCS, HDShaderIDs._IntegrationRoughness, currentRoughness);
                cmd.SetComputeMatrixParam(m_PlanarReflectionFilteringCS, HDShaderIDs._CaptureCameraIVP, planarTextureFilteringParameters.captureCameraIVP);
                cmd.SetComputeMatrixParam(m_PlanarReflectionFilteringCS, HDShaderIDs._CaptureCameraWorldToView, planarTextureFilteringParameters.captureCameraWorldToView);
                cmd.SetComputeMatrixParam(m_PlanarReflectionFilteringCS, "_CaptureCameraVP", planarTextureFilteringParameters.captureCameraVP);
                cmd.SetComputeFloatParam(m_PlanarReflectionFilteringCS, "_CaptureCameraFOV", planarTextureFilteringParameters.captureFOV * Mathf.PI / 180.0f);

                // Evaluate the next convolution
                cmd.DispatchCompute(m_PlanarReflectionFilteringCS, m_PlanarReflectionFilteringKernel, numTilesXHR, numTilesYHR, 1);

                // Copy the convoluted texture into the next mip and move on
                cmd.CopyTexture(colorTarget, 0, 0, 0, 0, texWidth, texHeight, target, 0, mipIndex, 0, 0);

                // Move to the next mip
                texWidth = texWidth >> 1;
                texHeight = texHeight >> 1;
                mipIndex++;

                numTilesXHRHalf = (texWidth + (areaTileSize - 1)) / areaTileSize;
                numTilesYHRHalf = (texHeight + (areaTileSize - 1)) / areaTileSize;
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, HDShaderIDs._ReflectionColorMipChain, colorSource);
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, HDShaderIDs._HalfResReflectionBuffer, colorTarget);
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, HDShaderIDs._DepthTexture, depthSource);
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, HDShaderIDs._HalfResDepthBuffer, depthTarget);
                cmd.DispatchCompute(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, numTilesXHRHalf, numTilesYHRHalf, 1);

                RTHandle tempTarget = colorTarget;
                colorTarget = colorSource;
                colorSource = tempTarget;
                RTHandle tempTargetD = depthTarget;
                depthTarget = depthSource;
                depthSource = tempTargetD;
                currentRoughness += roughnessStep;
            }
        }
    }
}
