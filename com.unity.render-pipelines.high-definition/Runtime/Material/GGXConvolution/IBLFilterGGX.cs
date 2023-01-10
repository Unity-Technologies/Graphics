using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class IBLFilterGGX : IBLFilterBSDF
    {
        static readonly int k_PlanarReflectionFilterTex0ID = Shader.PropertyToID("PlanarReflectionFilterTex0");
        static readonly int k_PlanarReflectionFilterTex1ID = Shader.PropertyToID("PlanarReflectionFilterTex1");
        static readonly int k_PlanarReflectionFilterDepthTex0ID = Shader.PropertyToID("PlanarReflectionFilterDepthTex0");
        static readonly int k_PlanarReflectionFilterDepthTex1ID = Shader.PropertyToID("PlanarReflectionFilterDepthTex1");

        RenderTexture m_GgxIblSampleData;
        int m_GgxIblMaxSampleCount = TextureCache.isMobileBuildTarget ? 34 : 89;   // Width
        const int k_GgxIblMipCountMinusOne = 6;    // Height (UNITY_SPECCUBE_LOD_STEPS)

        ComputeShader m_ComputeGgxIblSampleDataCS;
        int m_ComputeGgxIblSampleDataKernel = -1;

        ComputeShader m_BuildProbabilityTablesCS;
        int m_ConditionalDensitiesKernel = -1;
        int m_MarginalRowDensitiesKernel = -1;

        // Planar reflection filtering
        ComputeShader m_PlanarReflectionFilteringCS;
        int m_PlanarReflectionDepthConversionKernel = -1;
        int m_PlanarReflectionDownScaleKernel = -1;
        int m_PlanarReflectionFilteringKernel = -1;
        const int k_DefaultPlanarResolution = 512;
        // Intermediate variables
        Vector4 currentScreenSize = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        MaterialPropertyBlock m_MaterialPropertyBlock = new MaterialPropertyBlock();


        public IBLFilterGGX(HDRenderPipelineRuntimeResources renderPipelineResources, MipGenerator mipGenerator)
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
                m_ComputeGgxIblSampleDataCS = m_RenderPipelineResources.shaders.computeGgxIblSampleDataCS;
                m_ComputeGgxIblSampleDataKernel = m_ComputeGgxIblSampleDataCS.FindKernel("ComputeGgxIblSampleData");
            }

            if (!m_BuildProbabilityTablesCS)
            {
                m_BuildProbabilityTablesCS = m_RenderPipelineResources.shaders.buildProbabilityTablesCS;
                m_ConditionalDensitiesKernel = m_BuildProbabilityTablesCS.FindKernel("ComputeConditionalDensities");
                m_MarginalRowDensitiesKernel = m_BuildProbabilityTablesCS.FindKernel("ComputeMarginalRowDensities");
            }

            if (!m_convolveMaterial)
            {
                m_convolveMaterial = CoreUtils.CreateEngineMaterial(m_RenderPipelineResources.shaders.GGXConvolvePS);
            }

            if (!m_GgxIblSampleData)
            {
                m_GgxIblSampleData = new RenderTexture(m_GgxIblMaxSampleCount, k_GgxIblMipCountMinusOne, 0, GraphicsFormat.R16G16B16A16_SFloat);
                m_GgxIblSampleData.useMipMap = false;
                m_GgxIblSampleData.autoGenerateMips = false;
                m_GgxIblSampleData.enableRandomWrite = true;
                m_GgxIblSampleData.filterMode = FilterMode.Point;
                m_GgxIblSampleData.name = CoreUtils.GetRenderTargetAutoName(m_GgxIblMaxSampleCount, k_GgxIblMipCountMinusOne, 1, GraphicsFormat.R16G16B16A16_SFloat, "GGXIblSampleData");
                m_GgxIblSampleData.hideFlags = HideFlags.HideAndDontSave;
                m_GgxIblSampleData.Create();

                InitializeGgxIblSampleData(cmd);
            }

            if (!m_PlanarReflectionFilteringCS)
            {
                m_PlanarReflectionFilteringCS = m_RenderPipelineResources.shaders.planarReflectionFilteringCS;
                m_PlanarReflectionDepthConversionKernel = m_PlanarReflectionFilteringCS.FindKernel("DepthConversion");
                m_PlanarReflectionDownScaleKernel = m_PlanarReflectionFilteringCS.FindKernel("DownScale");
                m_PlanarReflectionFilteringKernel = m_PlanarReflectionFilteringCS.FindKernel("FilterPlanarReflection");
            }

            for (int i = 0; i < 6; ++i)
            {
                var lookAt = Matrix4x4.LookAt(Vector3.zero, CoreUtils.lookAtList[i], CoreUtils.upVectorList[i]);
                m_faceWorldToViewMatrixMatrices[i] = lookAt * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API. ...
            }
        }

        void InitializeGgxIblSampleData(CommandBuffer cmd)
        {
            m_ComputeGgxIblSampleDataCS.SetTexture(m_ComputeGgxIblSampleDataKernel, "outputResult", m_GgxIblSampleData);
            cmd.DispatchCompute(m_ComputeGgxIblSampleDataCS, m_ComputeGgxIblSampleDataKernel, 1, 1, 1);
        }

        private static RenderTextureDescriptor MakeRenderTextureDescriptor(int texWidth, int texHeight, GraphicsFormat format, bool useMipMap)
        {
            return new RenderTextureDescriptor
            {
                dimension = TextureDimension.Tex2D,
                width = texWidth,
                height = texHeight,
                volumeDepth = TextureXR.slices,
                graphicsFormat = format,
                enableRandomWrite = true,
                useDynamicScale = false,
                useMipMap = useMipMap,
                msaaSamples = 1
            };
        }

        private static void CreateIntermediateTextures(CommandBuffer cmd, int texWidth, int texHeight)
        {
            var hdPipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
            var probeFormat = (GraphicsFormat)hdPipeline.currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionProbeFormat;

            cmd.GetTemporaryRT(k_PlanarReflectionFilterTex0ID, MakeRenderTextureDescriptor(texWidth, texHeight, probeFormat, true));
            cmd.GetTemporaryRT(k_PlanarReflectionFilterTex1ID, MakeRenderTextureDescriptor(texWidth, texHeight, probeFormat, false));
            cmd.GetTemporaryRT(k_PlanarReflectionFilterDepthTex0ID, MakeRenderTextureDescriptor(texWidth, texHeight, GraphicsFormat.R32_SFloat, true));
            cmd.GetTemporaryRT(k_PlanarReflectionFilterDepthTex1ID, MakeRenderTextureDescriptor(texWidth, texHeight, GraphicsFormat.R32_SFloat, false));
        }

        private static void ReleaseItrermediateTextures(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(k_PlanarReflectionFilterTex0ID);
            cmd.ReleaseTemporaryRT(k_PlanarReflectionFilterTex1ID);
            cmd.ReleaseTemporaryRT(k_PlanarReflectionFilterDepthTex0ID);
            cmd.ReleaseTemporaryRT(k_PlanarReflectionFilterDepthTex1ID);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_convolveMaterial);
            CoreUtils.Destroy(m_GgxIblSampleData);
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

                m_MaterialPropertyBlock.SetTexture("_MainTex", source);
                m_MaterialPropertyBlock.SetFloat("_InvOmegaP", invOmegaP);

                for (int mip = 1; mip < (int)EnvConstants.ConvolutionMipCount; ++mip)
                {
                    m_MaterialPropertyBlock.SetFloat("_Level", mip);

                    for (int face = 0; face < 6; ++face)
                    {
                        var faceSize = new Vector4(source.width >> mip, source.height >> mip, 1.0f / (source.width >> mip), 1.0f / (source.height >> mip));
                        var transform = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(0.5f * Mathf.PI, Vector2.zero, faceSize, worldToViewMatrices[face], true);

                        m_MaterialPropertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, transform);

                        CoreUtils.SetRenderTarget(cmd, target, ClearFlag.None, mip, (CubemapFace)face);
                        CoreUtils.DrawFullScreen(cmd, m_convolveMaterial, m_MaterialPropertyBlock);
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

        void BuildColorAndDepthMipChain(CommandBuffer cmd, RenderTexture sourceColor, RenderTexture sourceDepth, ref PlanarTextureFilteringParameters planarTextureFilteringParameters)
        {
            int currentTexWidth = sourceColor.width;
            int currentTexHeight = sourceColor.height;

            // The first color level can be copied straight away in the mip chain.
            cmd.CopyTexture(sourceColor, 0, 0, 0, 0, sourceColor.width, sourceColor.height, k_PlanarReflectionFilterTex0ID, 0, 0, 0, 0);

            // For depth it is a bit trickier, we want to convert the depth from oblique space to non-oblique space due to the poor interpolation properties of the oblique matrix
            cmd.SetComputeVectorParam(m_PlanarReflectionFilteringCS, HDShaderIDs._CaptureCameraPositon, planarTextureFilteringParameters.captureCameraPosition);
            cmd.SetComputeMatrixParam(m_PlanarReflectionFilteringCS, HDShaderIDs._CaptureCameraVP_NO, planarTextureFilteringParameters.captureCameraVP_NonOblique);
            cmd.SetComputeMatrixParam(m_PlanarReflectionFilteringCS, HDShaderIDs._CaptureCameraIVP, planarTextureFilteringParameters.captureCameraIVP);
            currentScreenSize.Set(currentTexWidth, currentTexHeight, 1.0f / currentTexWidth, 1.0f / currentTexHeight);
            cmd.SetComputeVectorParam(m_PlanarReflectionFilteringCS, HDShaderIDs._CaptureCurrentScreenSize, currentScreenSize);
            cmd.SetComputeFloatParam(m_PlanarReflectionFilteringCS, HDShaderIDs._CaptureCameraFarPlane, planarTextureFilteringParameters.captureFarPlane);

            // Input textures
            cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDepthConversionKernel, HDShaderIDs._DepthTextureOblique, sourceDepth);

            // Output textures
            cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDepthConversionKernel, HDShaderIDs._DepthTextureNonOblique, k_PlanarReflectionFilterDepthTex0ID);

            // Compute the dispatch parameters and evaluate the new mip
            int tileSize = 8;
            int numTilesXHR = (currentTexWidth + (tileSize - 1)) / tileSize;
            int numTilesYHR = (currentTexHeight + (tileSize - 1)) / tileSize;
            cmd.DispatchCompute(m_PlanarReflectionFilteringCS, m_PlanarReflectionDepthConversionKernel, numTilesXHR, numTilesYHR, 1);

            // Move to the next mip and build the chain
            int currentMipSource = 0;
            int texWidthHalf = sourceColor.width >> 1;
            int texHeightHalf = sourceColor.height >> 1;

            // Until we have a 2x2 texture, continue
            while (texWidthHalf >= 2 && texHeightHalf >= 2)
            {
                // Constant inputs
                cmd.SetComputeIntParam(m_PlanarReflectionFilteringCS, HDShaderIDs._SourceMipIndex, currentMipSource);

                // Input textures
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, HDShaderIDs._ReflectionColorMipChain, k_PlanarReflectionFilterTex0ID);
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, HDShaderIDs._HalfResReflectionBuffer, k_PlanarReflectionFilterTex1ID);

                // Output textures
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, HDShaderIDs._DepthTextureMipChain, k_PlanarReflectionFilterDepthTex0ID);
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, HDShaderIDs._HalfResDepthBuffer, k_PlanarReflectionFilterDepthTex1ID);
                currentScreenSize.Set(currentTexWidth, currentTexHeight, 1.0f / currentTexWidth, 1.0f / currentTexHeight);
                cmd.SetComputeVectorParam(m_PlanarReflectionFilteringCS, HDShaderIDs._CaptureCurrentScreenSize, currentScreenSize);

                // Compute the dispatch parameters and evaluate the new mip
                int numTilesXHRHalf = (texWidthHalf + (tileSize - 1)) / tileSize;
                int numTilesYHRHalf = (texHeightHalf + (tileSize - 1)) / tileSize;
                cmd.DispatchCompute(m_PlanarReflectionFilteringCS, m_PlanarReflectionDownScaleKernel, numTilesXHRHalf, numTilesYHRHalf, 1);

                // Given that mip to mip in compute doesn't work, we have to do this :(
                cmd.CopyTexture(k_PlanarReflectionFilterTex1ID, 0, 0, 0, 0, texWidthHalf, texHeightHalf, k_PlanarReflectionFilterTex0ID, 0, currentMipSource + 1, 0, 0);
                cmd.CopyTexture(k_PlanarReflectionFilterDepthTex1ID, 0, 0, 0, 0, texWidthHalf, texHeightHalf, k_PlanarReflectionFilterDepthTex0ID, 0, currentMipSource + 1, 0, 0);

                // Update the parameters for the next mip
                currentTexWidth = currentTexWidth >> 1;
                currentTexHeight = currentTexHeight >> 1;
                texWidthHalf = texWidthHalf >> 1;
                texHeightHalf = texHeightHalf >> 1;
                currentMipSource++;
            }
        }

        override public void FilterPlanarTexture(CommandBuffer cmd, RenderTexture source, ref PlanarTextureFilteringParameters planarTextureFilteringParameters, RenderTexture target)
        {
            // Init the mip descent
            int texWidth = source.width;
            int texHeight = source.height;

            // First we need to copy the Mip0 (that matches perfectly smooth surface), no processing to be done on it
            cmd.CopyTexture(source, 0, 0, 0, 0, texWidth, texHeight, target, 0, 0, 0, 0);

            // If we are smooth reflection then the work is finish
            if (planarTextureFilteringParameters.smoothPlanarReflection)
                return;

            CreateIntermediateTextures(cmd, texWidth, texHeight);

            // Then we need to build a mip chain (one for color, one for depth) that we will sample later on in the process
            BuildColorAndDepthMipChain(cmd, source, planarTextureFilteringParameters.captureCameraDepthBuffer, ref planarTextureFilteringParameters);

            // Initialize the parameters for the descent
            int mipIndex = 1;
            int tileSize = 8;
            // Based on the initial texture resolution, the number of available mips for us to read from is variable and is based on the maximal texture width
            int numMipsChain = (int)(Mathf.Log((float)texWidth, 2.0f) - 1.0f);
            texWidth = texWidth >> 1;
            texHeight = texHeight >> 1;

            // Loop until we have the right amount of mips
            while (mipIndex < (int)EnvConstants.ConvolutionMipCount)
            {
                // Evaluate the dispatch parameters
                int numTilesXHR = (texWidth + (tileSize - 1)) / tileSize;
                int numTilesYHR = (texHeight + (tileSize - 1)) / tileSize;

                // Set input textures
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionFilteringKernel, HDShaderIDs._DepthTextureMipChain, k_PlanarReflectionFilterDepthTex0ID);
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionFilteringKernel, HDShaderIDs._ReflectionColorMipChain, k_PlanarReflectionFilterTex0ID);

                // Input constant parameters required
                cmd.SetComputeVectorParam(m_PlanarReflectionFilteringCS, HDShaderIDs._CaptureBaseScreenSize, planarTextureFilteringParameters.captureCameraScreenSize);
                currentScreenSize.Set(texWidth, texHeight, 1.0f / texWidth, 1.0f / texHeight);
                cmd.SetComputeVectorParam(m_PlanarReflectionFilteringCS, HDShaderIDs._CaptureCurrentScreenSize, currentScreenSize);
                cmd.SetComputeIntParam(m_PlanarReflectionFilteringCS, HDShaderIDs._SourceMipIndex, mipIndex);
                cmd.SetComputeIntParam(m_PlanarReflectionFilteringCS, HDShaderIDs._MaxMipLevels, numMipsChain);
                cmd.SetComputeFloatParam(m_PlanarReflectionFilteringCS, HDShaderIDs._RTScaleFactor, 1.0f);
                cmd.SetComputeVectorParam(m_PlanarReflectionFilteringCS, HDShaderIDs._ReflectionPlaneNormal, planarTextureFilteringParameters.probeNormal);
                cmd.SetComputeVectorParam(m_PlanarReflectionFilteringCS, HDShaderIDs._ReflectionPlanePosition, planarTextureFilteringParameters.probePosition);
                cmd.SetComputeVectorParam(m_PlanarReflectionFilteringCS, HDShaderIDs._CaptureCameraPositon, planarTextureFilteringParameters.captureCameraPosition);
                cmd.SetComputeMatrixParam(m_PlanarReflectionFilteringCS, HDShaderIDs._CaptureCameraIVP_NO, planarTextureFilteringParameters.captureCameraIVP_NonOblique);
                cmd.SetComputeFloatParam(m_PlanarReflectionFilteringCS, HDShaderIDs._CaptureCameraFOV, planarTextureFilteringParameters.captureFOV * Mathf.PI / 180.0f);

                // Set output textures
                cmd.SetComputeTextureParam(m_PlanarReflectionFilteringCS, m_PlanarReflectionFilteringKernel, HDShaderIDs._FilteredPlanarReflectionBuffer, k_PlanarReflectionFilterTex1ID);

                // Evaluate the next convolution
                cmd.DispatchCompute(m_PlanarReflectionFilteringCS, m_PlanarReflectionFilteringKernel, numTilesXHR, numTilesYHR, 1);

                // Copy the convoluted texture into the next mip and move on
                cmd.CopyTexture(k_PlanarReflectionFilterTex1ID, 0, 0, 0, 0, texWidth, texHeight, target, 0, mipIndex, 0, 0);

                // Move to the next mip
                texWidth = texWidth >> 1;
                texHeight = texHeight >> 1;
                mipIndex++;
            }

            ReleaseItrermediateTextures(cmd);
        }
    }
}
