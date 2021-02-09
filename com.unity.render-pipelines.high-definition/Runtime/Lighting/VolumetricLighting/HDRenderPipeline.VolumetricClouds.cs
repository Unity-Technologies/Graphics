using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Intermediate values for ambient probe evaluation
        Vector4[] m_PackedCoeffsClouds;
        ZonalHarmonicsL2 m_PhaseZHClouds;

        // Cloud preset maps
        Texture2D m_SparsePresetMap;
        Texture2D m_CloudyPresetMap;
        Texture2D m_OvercastPresetMap;
        Texture2D m_StormCloudsPresetMap;

        // The set of kernels that are required
        int m_CloudDownscaleDepthKernel;
        int m_CloudRenderKernel;
        int m_CloudReprojectKernel;
        int m_UpscaleAndCombineCloudsKernel;

        void InitializeVolumetricClouds()
        {
            // Allocate the buffers for ambient probe evaluation
            m_PackedCoeffsClouds = new Vector4[7];
            m_PhaseZHClouds = new ZonalHarmonicsL2();
            m_PhaseZHClouds.coeffs = new float[3];

            // Grab the kernels we need
            ComputeShader volumetricCloudsCS = m_Asset.renderPipelineResources.shaders.volumetricCloudsCS;
            m_CloudDownscaleDepthKernel = volumetricCloudsCS.FindKernel("DownscaleDepth");
            m_CloudRenderKernel = volumetricCloudsCS.FindKernel("RenderClouds");
            m_CloudReprojectKernel = volumetricCloudsCS.FindKernel("ReprojectClouds");
            m_UpscaleAndCombineCloudsKernel = volumetricCloudsCS.FindKernel("UpscaleAndCombineClouds");

            // Allocate all the texture initially
            AllocatePresetTextures();

            // Initialize the additional sub components
            InitializeVolumetricCloudsMap();
            InitializeVolumetricCloudsShadows();
        }

        void ReleaseVolumetricClouds()
        {
            // Release the additional sub components
            ReleaseVolumetricCloudsMap();
            ReleaseVolumetricCloudsShadows();
        }

        void AllocatePresetTextures()
        {
            // Build our default cloud map
            m_SparsePresetMap = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None) { name = "Default Sparse Texture" };
            m_SparsePresetMap.SetPixel(0, 0, new Color(0.9f, 0.0f, 0.0625f, 1.0f));
            m_SparsePresetMap.Apply();

            m_CloudyPresetMap = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None) { name = "Default Cloudy Texture" };
            m_CloudyPresetMap.SetPixel(0, 0, new Color(0.9f, 0.2f, 0.0625f, 1.0f));
            m_CloudyPresetMap.Apply();

            m_OvercastPresetMap = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None) { name = "Default Overcast Texture" };
            m_OvercastPresetMap.SetPixel(0, 0, new Color(0.5f, 0.0f, 0.375f, 1.0f));
            m_OvercastPresetMap.Apply();

            m_StormCloudsPresetMap = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None) { name = "Default Storm Texture" };
            m_StormCloudsPresetMap.SetPixel(0, 0, new Color(1.0f, 0.8f, 0.375f, 1.0f));
            m_StormCloudsPresetMap.Apply();
        }

        // Function that fills the buffer with the ambient probe values
        unsafe void SetPreconvolvedAmbientLightProbe(ref ShaderVariablesClouds cb, HDCamera hdCamera, VolumetricClouds settings)
        {
            SphericalHarmonicsL2 probeSH = SphericalHarmonicMath.UndoCosineRescaling(m_SkyManager.GetAmbientProbe(hdCamera));
            probeSH = SphericalHarmonicMath.RescaleCoefficients(probeSH, settings.ambientLightProbeDimmer.value);
            ZonalHarmonicsL2.GetCornetteShanksPhaseFunction(m_PhaseZHClouds, 0.0f);
            SphericalHarmonicsL2 finalSH = SphericalHarmonicMath.PremultiplyCoefficients(SphericalHarmonicMath.Convolve(probeSH, m_PhaseZHClouds));

            SphericalHarmonicMath.PackCoefficients(m_PackedCoeffsClouds, finalSH);
            for (int i = 0; i < 7; i++)
                for (int j = 0; j < 4; ++j)
                    cb._AmbientProbeCoeffs[i * 4 + j] = m_PackedCoeffsClouds[i][j];
        }

        // Allocation of the first history buffer
        static RTHandle VolumetricClouds0HistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one * 0.5f, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                name: string.Format("{0}_CloudsHistory0Buffer{1}", viewName, frameIndex));
        }

        // Allocation of the second history buffer
        static RTHandle VolumetricClouds1HistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one * 0.5f, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                name: string.Format("{0}_CloudsHistory1Buffer{1}", viewName, frameIndex));
        }

        // Functions to request the history buffers
        static RTHandle RequestCurrentVolumetricCloudsHistoryTexture0(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0,
                VolumetricClouds0HistoryBufferAllocatorFunction, 2);
        }

        static RTHandle RequestPreviousVolumetricCloudsHistoryTexture0(HDCamera hdCamera)
        {
            return hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0,
                VolumetricClouds0HistoryBufferAllocatorFunction, 2);
        }

        static RTHandle RequestCurrentVolumetricCloudsHistoryTexture1(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds1)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds1,
                VolumetricClouds1HistoryBufferAllocatorFunction, 2);
        }

        static RTHandle RequestPreviousVolumetricCloudsHistoryTexture1(HDCamera hdCamera)
        {
            return hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds1)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds1,
                VolumetricClouds1HistoryBufferAllocatorFunction, 2);
        }

        // Function to evaluate if a camera should have volumetric clouds
        static bool HasVolumetricClouds(HDCamera hdCamera, in VolumetricClouds settings)
        {
            // If the current volume does not enable the feature, quit right away.
            return hdCamera.frameSettings.IsEnabled(FrameSettingsField.VolumetricClouds) && settings.enable.value;
        }

        static bool HasVolumetricClouds(HDCamera hdCamera)
        {
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();
            // If the current volume does not enable the feature, quit right away.
            return HasVolumetricClouds(hdCamera, in settings);
        }

  
        struct VolumetricCloudsParameters
        {
            // Resolution parameters
            public int traceWidth;
            public int traceHeight;
            public int intermediateWidth;
            public int intermediateHeight;
            public int finalWidth;
            public int finalHeight;
            public int viewCount;
            public bool historyValidity;
            public Vector2Int previousViewportSize;

            // Static textures
            public Texture3D worley128RGBA;
            public Texture3D worley32RGB;
            public Texture cloudMapTexture;
            public Texture cloudLutTexture;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public Light sunLight;

            // Compute shader and kernels
            public ComputeShader volumetricCloudsCS;
            public int depthDownscaleKernel;
            public int renderKernel;
            public int reprojectKernel;
            public int upscaleAndCombineKernel;
            public int shadowsKernel;

            // Cloud constant buffer buffer
            public ShaderVariablesClouds cloudsCB;
        }

        float Square(float x)
        {
            return x * x;
        }

        float ComputeNormalizationFactor(float earthRadius, float lowerCloudRadius)
        {
            return Mathf.Sqrt((earthRadius + lowerCloudRadius) * (earthRadius + lowerCloudRadius) - earthRadius * earthRadius);
        }

        void GetPresetCloudMapValues(VolumetricClouds.CloudPresets preset, out float densityMultiplier, out float shapeFactor, out float shapeScale, out float erosionFactor, out float erosionScale)
        {
            switch (preset)
            {
                case VolumetricClouds.CloudPresets.Sparse:
                {
                    densityMultiplier = 0.836660027f;
                    shapeFactor = 1.0f;
                    shapeScale = 0.8f;
                    erosionFactor = 0.5f;
                    erosionScale = 1.175f;
                    return;
                }
                case VolumetricClouds.CloudPresets.Cloudy:
                {
                    densityMultiplier = 0.894427191f;
                    shapeFactor = 0.9f;
                    shapeScale = 0.8f;
                    erosionFactor = 0.45f;
                    erosionScale = 1.1f;
                    return;
                }
                case VolumetricClouds.CloudPresets.Overcast:
                {
                    densityMultiplier = 0.25f;
                    shapeFactor = 0.7f;
                    shapeScale = 1.0f;
                    erosionFactor = 0.5f;
                    erosionScale = 1.0f;
                    return;
                }
                case VolumetricClouds.CloudPresets.StormClouds:
                {
                    densityMultiplier = 0.632455532f;
                    shapeFactor = 0.7f;
                    shapeScale = 0.875f;
                    erosionFactor = 0.6f;
                    erosionScale = 1.0f;
                    return;
                }
            }

            // Default unused values
            densityMultiplier = 0.6f;
            shapeFactor = 0.6f;
            shapeScale = 1.0f;
            erosionFactor = 0.6f;
            erosionScale = 1.0f;
        }

        // The earthRadius
        const float earthRadius = 6378100.0f;

        void UpdateShaderVariableslClouds(ref ShaderVariablesClouds cb, HDCamera hdCamera, VolumetricClouds settings, HDUtils.PackedMipChainInfo info, in VolumetricCloudsParameters parameters, bool shadowPass)
        {
            // Convert to kilometers
            cb._LowestCloudAltitude = settings.lowestCloudAltitude.value;
            cb._HighestCloudAltitude = settings.lowestCloudAltitude.value + settings.cloudThickness.value;
            cb._EarthRadius = Mathf.Lerp(1.0f, 0.025f, settings.earthCurvature.value) * earthRadius;
            cb._CloudRangeSquared.Set(Square(cb._LowestCloudAltitude + cb._EarthRadius), Square(cb._HighestCloudAltitude + cb._EarthRadius));

            cb._NumPrimarySteps = settings.numPrimarySteps.value;
            cb._NumLightSteps = settings.numLightSteps.value;
            // 1500.0f is the maximal distance that a single step can do in theory (otherwise we endup skipping large clouds)
            cb._MaxRayMarchingDistance = Mathf.Min(1500.0f * cb._NumPrimarySteps, hdCamera.camera.farClipPlane);
            cb._CloudMapTiling.Set(settings.cloudTiling.value.x, settings.cloudTiling.value.y, settings.cloudOffset.value.x, settings.cloudOffset.value.y);

            cb._ScatteringDirection = settings.scatteringDirection.value;
            cb._ScatteringTint = Color.white - settings.scatteringTint.value * 0.75f;
            cb._PowderEffectIntensity = settings.powderEffectIntensity.value;
            cb._NormalizationFactor = ComputeNormalizationFactor(cb._EarthRadius, (cb._LowestCloudAltitude + cb._HighestCloudAltitude) * 0.5f);

            // We need 16 samples per pixel and we are alternating between 4 pixels (16 x 4 = 64)
            int frameIndex = RayTracingFrameIndex(hdCamera, 64);
            cb._AccumulationFrameIndex = frameIndex / 4;
            cb._SubPixelIndex = frameIndex % 4;

            // PB Sun/Sky settings
            var visualEnvironment = hdCamera.volumeStack.GetComponent<VisualEnvironment>();
            cb._PhysicallyBasedSun =  visualEnvironment.skyType.value == (int)SkyType.PhysicallyBased ? 1 : 0;
            Light currentSun = GetCurrentSunLight();
            if (currentSun != null)
            {
                // Grab the target sun additional data
                HDAdditionalLightData additionalLightData;
                m_CurrentSunLight.TryGetComponent<HDAdditionalLightData>(out additionalLightData);
                cb._SunDirection = -currentSun.transform.forward;
                cb._SunRight = currentSun.transform.right;
                cb._SunUp = currentSun.transform.up;

                cb._SunLightColor = m_lightList.directionalLights[0].color;

                cb._ExposureSunColor = 1;
            }
            else
            {
                cb._SunDirection = Vector3.up;
                cb._SunRight = Vector3.right;
                cb._SunUp = Vector3.forward;

                cb._SunLightColor = Vector3.one;
                cb._ExposureSunColor = 0;
            }

            // Compute the theta angle for the wind direction
            float theta = settings.orientation.value / 180.0f * Mathf.PI;
            // We apply a minus to see something moving in the right direction
            cb._WindDirection = new Vector2(-Mathf.Cos(theta),- Mathf.Sin(theta));
            cb._WindVector = hdCamera.volumetricCloudsAnimationData.cloudOffset;

            cb._LargeWindSpeed = settings.cloudMapSpeedMultiplier.value;
            cb._MediumWindSpeed = settings.shapeSpeedMultiplier.value;
            cb._SmallWindSpeed = settings.erosionSpeedMultiplier.value;

            cb._MultiScattering = 1.0f - settings.multiScattering.value * 0.8f;

            if (settings.cloudControl.value == VolumetricClouds.CloudControl.Simple && settings.cloudPreset.value != VolumetricClouds.CloudPresets.Custom)
            {
                GetPresetCloudMapValues(settings.cloudPreset.value, out cb._DensityMultiplier, out cb._ShapeFactor, out cb._ShapeScale, out cb._ErosionFactor, out cb._ErosionScale);
            }
            else
            {
                // The density multiplier is not used linearly
                float densityMultiplier = settings.densityMultiplier.value * 2.0f;
                cb._DensityMultiplier = densityMultiplier * densityMultiplier;
                cb._ShapeFactor = settings.shapeFactor.value;
                cb._ShapeScale = Mathf.Lerp(0.5f, 2.0f, settings.shapeScale.value);
                cb._ErosionFactor = settings.erosionFactor.value;
                cb._ErosionScale = Mathf.Lerp(0.5f, 2.0f, settings.erosionScale.value);
            }

            // If the sun has moved more than 2.0°, reduce significantly the history accumulation
            float sunAngleDifference = 0.0f;
            if (m_CurrentSunLightAdditionalLightData != null)
                sunAngleDifference = Quaternion.Angle(m_CurrentSunLightAdditionalLightData.previousTransform.rotation, m_CurrentSunLightAdditionalLightData.transform.localToWorldMatrix.rotation);
            float sunAttenuation = sunAngleDifference > 2.0f ? 0.5f : 1.0f;
            cb._TemporalAccumulationFactor = settings.temporalAccumulationFactor.value * sunAttenuation;

            cb._FinalScreenSize.Set((float)parameters.finalWidth, (float)parameters.finalHeight, 1.0f / (float)parameters.finalWidth, 1.0f / (float)parameters.finalHeight);
            cb._IntermediateScreenSize.Set((float)parameters.intermediateWidth, (float)parameters.intermediateHeight, 1.0f / (float)parameters.intermediateWidth, 1.0f / (float)parameters.intermediateHeight);
            cb._TraceScreenSize.Set((float)parameters.traceWidth, (float)parameters.traceHeight, 1.0f / (float)parameters.traceWidth, 1.0f / (float)parameters.traceHeight);

            cb._DepthMipOffset = new Vector2(info.mipLevelOffsets[1].x, info.mipLevelOffsets[1].y);

            float absoluteCloudHighest = cb._HighestCloudAltitude + cb._EarthRadius;
            cb._MaxCloudDistance = Mathf.Sqrt(absoluteCloudHighest * absoluteCloudHighest - cb._EarthRadius * cb._EarthRadius);

            // Evaluate the ambient probe data
            SetPreconvolvedAmbientLightProbe(ref cb, hdCamera, settings);

            if (shadowPass)
            {
                // Resolution of the cloud shadow
                cb._ShadowCookieResolution = (int)settings.shadowResolution.value;
                cb._ShadowIntensity = settings.shadowOpacity.value;
                cb._ShadowFallbackValue = 1.0f - settings.shadowOpacityFallback.value;
                cb._ShadowPlaneOffset = settings.shadowPlaneHeightOffset.value;

                // Compute Size of the shadow on the ground
                float groundShadowSize = settings.shadowDistance.value;

                if (HasVolumetricCloudsShadows(hdCamera, settings))
                {
                    float scaleX = Mathf.Abs(Vector3.Dot(cb._SunRight, Vector3.Normalize(new Vector3(cb._SunRight.x, 0.0f, cb._SunRight.z))));
                    float scaleY = Mathf.Abs(Vector3.Dot(cb._SunUp, Vector3.Normalize(new Vector3(cb._SunUp.x, 0.0f, cb._SunUp.z))));
                    cb._ShadowRegionSize = new Vector2(groundShadowSize * scaleX, groundShadowSize * scaleY);
                }
            }
        }

        Texture2D GetPresetCloudMapTexture(VolumetricClouds.CloudPresets preset)
        {
            // Textures may become null if a new scene was loaded in the editor (and maybe other reasons).
            if (m_SparsePresetMap == null || Object.ReferenceEquals(m_SparsePresetMap, null))
                AllocatePresetTextures();

            switch (preset)
            {
                case VolumetricClouds.CloudPresets.Sparse:
                    return m_SparsePresetMap;
                case VolumetricClouds.CloudPresets.Cloudy:
                    return m_CloudyPresetMap;
                case VolumetricClouds.CloudPresets.Overcast:
                    return m_OvercastPresetMap;
                case VolumetricClouds.CloudPresets.StormClouds:
                    return m_StormCloudsPresetMap;
                case VolumetricClouds.CloudPresets.Custom:
                    return m_CloudyPresetMap;
            }
            return Texture2D.blackTexture;
        }

        VolumetricCloudsParameters PrepareVolumetricCloudsParameters(HDCamera hdCamera, VolumetricClouds settings, HDUtils.PackedMipChainInfo info, bool shadowPass, bool historyValidity)
        {
            VolumetricCloudsParameters parameters = new VolumetricCloudsParameters();
            // We need to make sure that the allocated size of the history buffers and the dispatch size are perfectly equal.
            // The ideal approach would be to have a function for that returns the converted size from a viewport and texture size.
            // but for now we do it like this.
            // Final resolution at which the effect should be exported
            parameters.finalWidth = hdCamera.actualWidth;
            parameters.finalHeight = hdCamera.actualHeight;
            // Intermediate resolution at which the effect is accumulated
            parameters.intermediateWidth = Mathf.RoundToInt(0.5f * hdCamera.actualWidth);
            parameters.intermediateHeight = Mathf.RoundToInt(0.5f * hdCamera.actualHeight);
            // Resolution at which the effect is traced
            parameters.traceWidth = Mathf.RoundToInt(0.25f * hdCamera.actualWidth);
            parameters.traceHeight = Mathf.RoundToInt(0.25f * hdCamera.actualHeight);
            parameters.viewCount = hdCamera.viewCount;
            parameters.previousViewportSize = hdCamera.historyRTHandleProperties.previousViewportSize;
            parameters.historyValidity = historyValidity;
            
            // Compute shader and kernels
            parameters.volumetricCloudsCS = m_Asset.renderPipelineResources.shaders.volumetricCloudsCS;
            parameters.depthDownscaleKernel = m_CloudDownscaleDepthKernel;
            parameters.renderKernel = m_CloudRenderKernel;
            parameters.reprojectKernel = m_CloudReprojectKernel;
            parameters.upscaleAndCombineKernel = m_UpscaleAndCombineCloudsKernel;
            parameters.shadowsKernel = m_ComputeShadowCloudsKernel;

            // Static textures
            if (settings.cloudControl.value == VolumetricClouds.CloudControl.Simple)
            {
                parameters.cloudMapTexture = GetPresetCloudMapTexture(settings.cloudPreset.value);
                parameters.cloudLutTexture = m_Asset.renderPipelineResources.textures.cloudLutRainAO;
            }
            else if (settings.cloudControl.value == VolumetricClouds.CloudControl.Advanced)
            {
                parameters.cloudMapTexture = m_AdvancedCloudMap;
                parameters.cloudLutTexture = m_Asset.renderPipelineResources.textures.cloudLutRainAO;
            }
            else
            {
                parameters.cloudMapTexture = settings.cloudMap.value != null ? settings.cloudMap.value : Texture2D.blackTexture;
                parameters.cloudLutTexture = settings.cloudLut.value != null ? settings.cloudLut.value : Texture2D.blackTexture;
            }

            parameters.worley128RGBA = m_Asset.renderPipelineResources.textures.worleyNoise128RGBA;
            parameters.worley32RGB = m_Asset.renderPipelineResources.textures.worleyNoise32RGB;
            BlueNoise blueNoise = GetBlueNoiseManager();
            parameters.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();
            parameters.sunLight = GetCurrentSunLight();

            // Update the constant buffer
            UpdateShaderVariableslClouds(ref parameters.cloudsCB, hdCamera, settings, info, parameters, shadowPass);

            return parameters;
        }

        static void TraceVolumetricClouds(CommandBuffer cmd, VolumetricCloudsParameters parameters,
            RTHandle colorBuffer, RTHandle depthPyramid, TextureHandle motionVectors, TextureHandle volumetricLightingTexture, TextureHandle scatteringFallbackTexture,
            RTHandle currentHistory0Buffer, RTHandle previousHistory0Buffer,
            RTHandle currentHistory1Buffer, RTHandle previousHistory1Buffer,
            RTHandle intermediateLightingBuffer0, RTHandle intermediateLightingBuffer1, RTHandle intermediateDepthBuffer0, RTHandle intermediateDepthBuffer1)
        {
            // Compute the number of tiles to evaluate
            int traceTX = (parameters.traceWidth + (8 - 1)) / 8;
            int traceTY = (parameters.traceHeight + (8 - 1)) / 8;

            // Compute the number of tiles to evaluate
            int intermediateTX = (parameters.intermediateWidth + (8 - 1)) / 8;
            int intermediateTY = (parameters.intermediateHeight + (8 - 1)) / 8;

            // Bind the sampling textures
            BlueNoise.BindDitheredTextureSet(cmd, parameters.ditheredTextureSet);

            // We need to make sure that the allocated size of the history buffers and the dispatch size are perfectly equal.
            // The ideal approach would be to have a function for that returns the converted size from a viewport and texture size.
            // but for now we do it like this.
            Vector2Int previousViewportSize = previousHistory0Buffer.GetScaledSize(parameters.previousViewportSize);
            parameters.cloudsCB._HistoryViewportSize = new Vector2(previousViewportSize.x, previousViewportSize.y);
            parameters.cloudsCB._HistoryBufferSize = new Vector2(previousHistory0Buffer.rt.width, previousHistory0Buffer.rt.height);

            // Bind the constant buffer
            ConstantBuffer.Push(cmd, parameters.cloudsCB, parameters.volumetricCloudsCS, HDShaderIDs._ShaderVariablesClouds);

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsPrepare)))
            {
                // Compute the alternative version of the mip 1 of the depth (min instead of max that is required to handle high frequency meshes (vegetation, hair)
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.depthDownscaleKernel, HDShaderIDs._DepthTexture, depthPyramid);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.depthDownscaleKernel, HDShaderIDs._HalfResDepthBufferRW, intermediateDepthBuffer0);
                cmd.DispatchCompute(parameters.volumetricCloudsCS, parameters.depthDownscaleKernel, intermediateTX, intermediateTY, parameters.viewCount);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsTrace)))
            {
                // Ray-march the clouds for this frame
                CoreUtils.SetKeyword(cmd, "PHYSICALLY_BASED_SUN", parameters.cloudsCB._PhysicallyBasedSun == 1);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._HalfResDepthBuffer, intermediateDepthBuffer0);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._HistoryVolumetricClouds1Texture, previousHistory1Buffer);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._Worley128RGBA, parameters.worley128RGBA);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._Worley32RGB, parameters.worley32RGB);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CloudMapTexture, parameters.cloudMapTexture);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CloudLutTexture, parameters.cloudLutTexture);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CloudsLightingTextureRW, intermediateLightingBuffer0);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CloudsDepthTextureRW, intermediateDepthBuffer1);
                cmd.DispatchCompute(parameters.volumetricCloudsCS, parameters.renderKernel, traceTX, traceTY, parameters.viewCount);

                CoreUtils.SetKeyword(cmd, "PHYSICALLY_BASED_SUN", false);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsReproject)))
            {
                if (!parameters.historyValidity)
                    CoreUtils.SetRenderTarget(cmd, previousHistory1Buffer, clearFlag: ClearFlag.Color, clearColor: Color.black);

                // Re-project the result from the previous frame
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.reprojectKernel, HDShaderIDs._CloudsLightingTexture, intermediateLightingBuffer0);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.reprojectKernel, HDShaderIDs._CloudsDepthTexture, intermediateDepthBuffer1);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.reprojectKernel, HDShaderIDs._HalfResDepthBuffer, intermediateDepthBuffer0);

                // History buffers
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.reprojectKernel, HDShaderIDs._HistoryVolumetricClouds0Texture, previousHistory0Buffer);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.reprojectKernel, HDShaderIDs._HistoryVolumetricClouds1Texture, previousHistory1Buffer);

                // Output textures
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.reprojectKernel, HDShaderIDs._CloudsLightingTextureRW, currentHistory0Buffer);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.reprojectKernel, HDShaderIDs._CloudsAdditionalTextureRW, currentHistory1Buffer);

                // Re-project from the previous frame
                cmd.DispatchCompute(parameters.volumetricCloudsCS, parameters.reprojectKernel, intermediateTX, intermediateTY, parameters.viewCount);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsUpscaleAndCombine)))
            {
                // Compute the final resolution parameters
                int finalTX = (parameters.finalWidth + (8 - 1)) / 8;
                int finalTY = (parameters.finalHeight + (8 - 1)) / 8;
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._DepthTexture, depthPyramid);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._DepthStatusTexture, currentHistory1Buffer);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._VolumetricCloudsTexture, currentHistory0Buffer);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._CameraColorTextureRW, colorBuffer);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._VBufferLighting, volumetricLightingTexture);
                if (parameters.cloudsCB._PhysicallyBasedSun == 0)
                {
                    cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._AirSingleScatteringTexture, scatteringFallbackTexture);
                    cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._AerosolSingleScatteringTexture, scatteringFallbackTexture);
                    cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._MultipleScatteringTexture, scatteringFallbackTexture);
                }
                cmd.DispatchCompute(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, finalTX, finalTY, parameters.viewCount);
            }
        }

        class VolumetricCloudsData
        {
            public VolumetricCloudsParameters parameters;
            public TextureHandle colorBuffer;
            public TextureHandle depthPyramid;
            public TextureHandle motionVectors;
            public TextureHandle volumetricLighting;
            public TextureHandle scatteringFallbackTexture;
            public TextureHandle previousHistoryBuffer0;
            public TextureHandle currentHistoryBuffer0;
            public TextureHandle previousHistoryBuffer1;
            public TextureHandle currentHistoryBuffer1;
            public TextureHandle intermediateBuffer0;
            public TextureHandle intermediateBuffer1;
            public TextureHandle intermediateBufferDepth0;
            public TextureHandle intermediateBufferDepth1;
        }

        private bool EvaluateVolumetricCloudsHistoryValidity(HDCamera hdCamera)
        {
            // Evaluate the history validity
            return hdCamera.EffectHistoryValidity(HDCamera.HistoryEffectSlot.VolumetricClouds, false, false);
        }

        private void PropagateVolumetricCloudsHistoryValidity(HDCamera hdCamera)
        {
            hdCamera.PropagateEffectHistoryValidity(HDCamera.HistoryEffectSlot.VolumetricClouds, false, false);
        }

        TextureHandle TraceVolumetricClouds(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthPyramid, TextureHandle motionVectors, TextureHandle volumetricLighting, HDUtils.PackedMipChainInfo info)
        {
            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsData>("Generating the rays for RTR", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricClouds)))
            {
                builder.EnableAsyncCompute(false);
                VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

                passData.parameters = PrepareVolumetricCloudsParameters(hdCamera, settings, info, false, EvaluateVolumetricCloudsHistoryValidity(hdCamera));
                passData.colorBuffer = builder.ReadTexture(builder.WriteTexture(colorBuffer));
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.motionVectors = builder.ReadTexture(motionVectors);
                passData.volumetricLighting = builder.ReadTexture(volumetricLighting);
                passData.scatteringFallbackTexture = renderGraph.defaultResources.blackTexture3DXR;

                passData.currentHistoryBuffer0 = renderGraph.ImportTexture(RequestCurrentVolumetricCloudsHistoryTexture0(hdCamera));
                passData.previousHistoryBuffer0 = renderGraph.ImportTexture(RequestPreviousVolumetricCloudsHistoryTexture0(hdCamera));
                passData.currentHistoryBuffer1 = renderGraph.ImportTexture(RequestCurrentVolumetricCloudsHistoryTexture1(hdCamera));
                passData.previousHistoryBuffer1 = renderGraph.ImportTexture(RequestPreviousVolumetricCloudsHistoryTexture1(hdCamera));

                passData.intermediateBuffer0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporary Clouds Lighting Buffer 0" });
                passData.intermediateBuffer1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporary Clouds Lighting Buffer 1 " });
                passData.intermediateBufferDepth0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                    { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Temporary Clouds Depth Buffer 0" });
                passData.intermediateBufferDepth1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                    { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Temporary Clouds Depth Buffer 1" });

                builder.SetRenderFunc(
                    (VolumetricCloudsData data, RenderGraphContext ctx) =>
                    {
                        TraceVolumetricClouds(ctx.cmd, data.parameters,
                            data.colorBuffer, data.depthPyramid, data.motionVectors, data.volumetricLighting, data.scatteringFallbackTexture,
                            data.currentHistoryBuffer0, data.previousHistoryBuffer0, data.currentHistoryBuffer1, data.previousHistoryBuffer1,
                            data.intermediateBuffer0, data.intermediateBuffer1, data.intermediateBufferDepth0, data.intermediateBufferDepth1);
                    });

                PushFullScreenDebugTexture(m_RenderGraph, passData.currentHistoryBuffer0, FullScreenDebugMode.VolumetricClouds);

                return passData.colorBuffer;
            }
        }

        void UpdateVolumetricClouds(HDCamera hdCamera, in VolumetricClouds settings)
        {
            // The system needs to be reset if this is the first frame or the history is not from the previous frame
            if (hdCamera.volumetricCloudsAnimationData.lastTime == -1.0f || !EvaluateVolumetricCloudsHistoryValidity(hdCamera))
            {
                // This is the first frame for the system
                hdCamera.volumetricCloudsAnimationData.lastTime = hdCamera.time;
                hdCamera.volumetricCloudsAnimationData.cloudOffset = Vector2.zero;
            }
            else
            {
                // Compute the delta time
                float delaTime = hdCamera.time - hdCamera.volumetricCloudsAnimationData.lastTime;

                // Compute the theta angle for the wind direction
                float theta = settings.orientation.value / 180.0f * Mathf.PI;

                // Compute the wind direction
                Vector2 windDirection = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));

                // Conversion  from km/h to m/s  is the 0.277778f factor
                // We apply a minus to see something moving in the right direction
                Vector2 windVector = -windDirection * settings.globalWindSpeed.value * delaTime * 0.277778f;

                // Animate the offset
                hdCamera.volumetricCloudsAnimationData.cloudOffset += windVector;

                // Update the time
                hdCamera.volumetricCloudsAnimationData.lastTime = hdCamera.time;
            }
        }

        void RenderVolumetricClouds(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthPyramid, TextureHandle motionVector, TextureHandle volumetricLighting, HDUtils.PackedMipChainInfo info)
        {
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

            // If the current volume does not enable the feature, quit right away.
            if (!HasVolumetricClouds(hdCamera, in settings) || hdCamera.camera.cameraType == CameraType.Reflection)
                return;

            // Make sure the volumetric clouds are animated properly
            UpdateVolumetricClouds(hdCamera, in settings);

            // Render the clouds
            TraceVolumetricClouds(renderGraph, hdCamera, colorBuffer, depthPyramid, motionVector, volumetricLighting, info);

            // Make sure to mark the history frame index validity.
            PropagateVolumetricCloudsHistoryValidity(hdCamera);
        }

        void PreRenderVolumetricClouds(RenderGraph renderGraph, HDCamera hdCamera, HDUtils.PackedMipChainInfo info)
        {
            if (hdCamera.camera.cameraType == CameraType.Reflection)
                return;

            // Grab the volume settings
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

            // Render the shadows
            PreRenderVolumetricCloudsShadows(renderGraph, hdCamera, info, in settings);

            // Evaluate the cloud map
            PreRenderVolumetricCloudMap(renderGraph, hdCamera, in settings);
        }
    }
}
