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
        Texture2D m_StormyPresetMap;
        Texture2D m_CustomLutPresetMap;
        const int k_CustomLutMapResolution = 32;
        readonly Color[] m_CustomLutColorArray = new Color[k_CustomLutMapResolution];

        // The set of kernels that are required
        int m_ConvertObliqueDepthKernel;
        int m_CloudDownscaleDepthKernel;
        int m_CloudRenderKernel;
        int m_CloudReprojectKernel;
        int m_UpscaleAndCombineCloudsKernelColorCopy;
        int m_UpscaleAndCombineCloudsKernelColorRW;

        // Combine pass via hardware blending, used in case of MSAA color target.
        Material m_CloudCombinePass;

        // This is the representation of the half resolution neighborhood
        // |-----|-----|-----|
        // |     |     |     |
        // |-----|-----|-----|
        // |     |     |     |
        // |-----|-----|-----|
        // |     |     |     |
        // |-----|-----|-----|

        // This is the representation of the full resolution neighborhood
        // |-----|-----|-----|
        // |     |     |     |
        // |-----|--|--|-----|
        // |     |--|--|     |
        // |-----|--|--|-----|
        // |     |     |     |
        // |-----|-----|-----|

        // The base is centered at (0, 0) at the center of the center pixel:
        // The 4 full res pixels are centered {L->R, T->B} at {-0.25, -0.25}, {0.25, -0.25}
        //                                                    {-0.25, 0.25}, {0.25, 0.25}
        //
        // The 9 half res pixels are placed {L->R, T->B} at {-1.0, -1.0}, {0.0, -1.0}, {1.0, -1.0}
        //                                                  {-1.0, 0.0}, {0.0, 0.0}, {1.0, 0.0}
        //                                                  {-1.0, 1.0}, {0.0, 1.0}, {1.0, 1.0}

        // Set of pre-generated weights (L->R, T->B). After experimentation, the final weighting function is exp(-distance^2)
        static float[] m_DistanceBasedWeights = new float[] { 0.324652f, 0.535261f, 0.119433f, 0.535261f, 0.882497f, 0.196912f, 0.119433f, 0.196912f, 0.0439369f,
                                                              0.119433f, 0.535261f, 0.324652f, 0.196912f, 0.882497f, 0.535261f, 0.0439369f, 0.196912f, 0.119433f,
                                                              0.119433f, 0.196912f, 0.0439369f, 0.535261f, 0.882497f, 0.196912f, 0.324652f, 0.535261f, 0.119433f,
                                                              0.0439369f, 0.196912f, 0.119433f, 0.196912f, 0.882497f, 0.535261f, 0.119433f, 0.535261f, 0.324652f};

        void InitializeVolumetricClouds()
        {
            if (!m_Asset.currentPlatformRenderPipelineSettings.supportVolumetricClouds)
                return;

            // Allocate the buffers for ambient probe evaluation
            m_PackedCoeffsClouds = new Vector4[7];
            m_PhaseZHClouds = new ZonalHarmonicsL2();
            m_PhaseZHClouds.coeffs = new float[3];

            // Grab the kernels we need
            ComputeShader volumetricCloudsCS = m_Asset.renderPipelineResources.shaders.volumetricCloudsCS;
            m_ConvertObliqueDepthKernel = volumetricCloudsCS.FindKernel("ConvertObliqueDepth");
            m_CloudDownscaleDepthKernel = volumetricCloudsCS.FindKernel("DownscaleDepth");
            m_CloudRenderKernel = volumetricCloudsCS.FindKernel("RenderClouds");
            m_CloudReprojectKernel = volumetricCloudsCS.FindKernel("ReprojectClouds");
            m_UpscaleAndCombineCloudsKernelColorCopy = volumetricCloudsCS.FindKernel("UpscaleAndCombineClouds_ColorCopy");
            m_UpscaleAndCombineCloudsKernelColorRW = volumetricCloudsCS.FindKernel("UpscaleAndCombineClouds_ColorRW");

            m_CloudCombinePass = CoreUtils.CreateEngineMaterial(defaultResources.shaders.volumetricCloudsCombinePS);

            // Allocate all the texture initially
            AllocatePresetTextures();

            // Initialize the additional sub components
            InitializeVolumetricCloudsMap();
            InitializeVolumetricCloudsShadows();
        }

        void ReleaseVolumetricClouds()
        {
            if (!m_Asset.currentPlatformRenderPipelineSettings.supportVolumetricClouds)
                return;

            CoreUtils.Destroy(m_CloudCombinePass);

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
            m_CloudyPresetMap.SetPixel(0, 0, new Color(0.9f, 0.0f, 0.2f, 1.0f));
            m_CloudyPresetMap.Apply();

            m_OvercastPresetMap = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None) { name = "Default Overcast Texture" };
            m_OvercastPresetMap.SetPixel(0, 0, new Color(0.5f, 0.0f, 1.0f, 1.0f));
            m_OvercastPresetMap.Apply();

            m_StormyPresetMap = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None) { name = "Default Storm Texture" };
            m_StormyPresetMap.SetPixel(0, 0, new Color(1.0f, 0.0f, 0.80f, 1.0f));
            m_StormyPresetMap.Apply();
        }

        void PrepareCustomLutData(in VolumetricClouds clouds)
        {
            if (m_CustomLutPresetMap == null)
            {
                m_CustomLutPresetMap = new Texture2D(1, k_CustomLutMapResolution, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None)
                {
                    name = "Custom LUT Curve",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                m_CustomLutPresetMap.hideFlags = HideFlags.HideAndDontSave;
            }

            var pixels = m_CustomLutColorArray;

            var densityCurve = clouds.customDensityCurve.value;
            var erosionCurve = clouds.customErosionCurve.value;
            var ambientOcclusionCurve = clouds.customAmbientOcclusionCurve.value;
            if (densityCurve == null || densityCurve.length == 0)
            {
                for (int i = 0; i < k_CustomLutMapResolution; i++)
                    pixels[i] = Color.white;
            }
            else
            {
                float step = 1.0f / (k_CustomLutMapResolution - 1f);

                for (int i = 0; i < k_CustomLutMapResolution; i++)
                {
                    float currTime = step * i;
                    float density = Mathf.Clamp(densityCurve.Evaluate(currTime), 0.0f, 1.0f);
                    float erosion = Mathf.Clamp(erosionCurve.Evaluate(currTime), 0.0f, 1.0f);;
                    float ambientOcclusion = Mathf.Clamp(1.0f - ambientOcclusionCurve.Evaluate(currTime), 0.0f, 1.0f);
                    pixels[i] = new Color(density, erosion, ambientOcclusion, 1.0f);
                }
            }

            m_CustomLutPresetMap.SetPixels(pixels);
            m_CustomLutPresetMap.Apply();
        }

        // Ref: "Efficient Evaluation of Irradiance Environment Maps" from ShaderX 2
        Vector3 SHEvalLinearL0L1(Vector3 N, Vector4 shAr, Vector4 shAg, Vector4 shAb)
        {
            Vector4 vA = new Vector4(N.x, N.y, N.z, 1.0f);

            Vector3 x1;
            // Linear (L1) + constant (L0) polynomial terms
            x1.x = Vector4.Dot(shAr, vA);
            x1.y = Vector4.Dot(shAg, vA);
            x1.z = Vector4.Dot(shAb, vA);

            return x1;
        }

        Vector3 SHEvalLinearL2(Vector3 N, Vector4 shBr, Vector4 shBg, Vector4 shBb, Vector4 shC)
        {
            Vector3 x2;
            // 4 of the quadratic (L2) polynomials
            Vector4 vB = new Vector4(N.x * N.y, N.y * N.z, N.z * N.z, N.z * N.x);
            x2.x = Vector4.Dot(shBr, vB);
            x2.y = Vector4.Dot(shBg, vB);
            x2.z = Vector4.Dot(shBb, vB);

            // Final (5th) quadratic (L2) polynomial
            float vC = N.x * N.x - N.y * N.y;
            Vector3 x3 = new Vector3(0.0f, 0.0f, 0.0f);
            x3.x = shC.x * vC;
            x3.x = shC.y * vC;
            x3.x = shC.z * vC;
            return x2 + x3;
        }

        Vector3 EvaluateAmbientProbe(Vector3 direction)
        {
            Vector4 shAr = m_PackedCoeffsClouds[0];
            Vector4 shAg = m_PackedCoeffsClouds[1];
            Vector4 shAb = m_PackedCoeffsClouds[2];
            Vector4 shBr = m_PackedCoeffsClouds[3];
            Vector4 shBg = m_PackedCoeffsClouds[4];
            Vector4 shBb = m_PackedCoeffsClouds[5];
            Vector4 shCr = m_PackedCoeffsClouds[6];

            // Linear + constant polynomial terms
            Vector3 res = SHEvalLinearL0L1(direction, shAr, shAg, shAb);

            // Quadratic polynomials
            res += SHEvalLinearL2(direction, shBr, shBg, shBb, shCr);

            // Return the result
            return res;
        }

        // Function that fills the buffer with the ambient probe values
        unsafe void SetPreconvolvedAmbientLightProbe(ref ShaderVariablesClouds cb, HDCamera hdCamera, VolumetricClouds settings)
        {
            SphericalHarmonicsL2 probeSH = SphericalHarmonicMath.UndoCosineRescaling(m_SkyManager.GetAmbientProbe(hdCamera));
            probeSH = SphericalHarmonicMath.RescaleCoefficients(probeSH, settings.ambientLightProbeDimmer.value);
            ZonalHarmonicsL2.GetCornetteShanksPhaseFunction(m_PhaseZHClouds, 0.0f);
            SphericalHarmonicsL2 finalSH = SphericalHarmonicMath.PremultiplyCoefficients(SphericalHarmonicMath.Convolve(probeSH, m_PhaseZHClouds));

            SphericalHarmonicMath.PackCoefficients(m_PackedCoeffsClouds, finalSH);

            cb._AmbientProbeTop = EvaluateAmbientProbe(Vector3.up);
            cb._AmbientProbeBottom = EvaluateAmbientProbe(Vector3.down);
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
            public bool localClouds;
            public bool historyValidity;
            public bool planarReflection;
            public bool needExtraColorBufferCopy;
            public bool enableExposureControl;
            public Vector2Int previousViewportSize;

            // Static textures
            public Texture3D worley128RGBA;
            public Texture3D erosionNoise;
            public Texture cloudMapTexture;
            public Texture cloudLutTexture;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public Light sunLight;

            // Compute shader and kernels
            public ComputeShader volumetricCloudsCS;
            public int convertObliqueDepthKernel;
            public int depthDownscaleKernel;
            public int renderKernel;
            public int reprojectKernel;
            public int upscaleAndCombineKernel;
            public int shadowsKernel;

            // Cloud constant buffer buffer
            public ShaderVariablesClouds cloudsCB;

            // MSAA support
            public bool needsTemporaryBuffer;
            public Material cloudCombinePass;
        }

        float Square(float x)
        {
            return x * x;
        }

        float ComputeNormalizationFactor(float earthRadius, float lowerCloudRadius)
        {
            return Mathf.Sqrt((k_EarthRadius + lowerCloudRadius) * (k_EarthRadius + lowerCloudRadius) - k_EarthRadius * earthRadius);
        }

        void GetPresetCloudMapValues(VolumetricClouds.CloudPresets preset, out CloudModelData cloudModelData)
        {
            switch (preset)
            {
                case VolumetricClouds.CloudPresets.Sparse:
                {
                    cloudModelData.densityMultiplier = 0.2f;
                    cloudModelData.shapeFactor = 0.9f;
                    cloudModelData.shapeScale = 2.0f;
                    cloudModelData.erosionFactor = 0.6f;
                    cloudModelData.erosionScale = 50.0f;
                    cloudModelData.erosionNoise = VolumetricClouds.CloudErosionNoise.Perlin32;
                    return;
                }
                case VolumetricClouds.CloudPresets.Cloudy:
                {
                    cloudModelData.densityMultiplier = 0.3f;
                    cloudModelData.shapeFactor = 0.85f;
                    cloudModelData.shapeScale = 2.5f;
                    cloudModelData.erosionFactor = 0.7f;
                    cloudModelData.erosionScale = 55.0f;
                    cloudModelData.erosionNoise = VolumetricClouds.CloudErosionNoise.Perlin32;
                    return;
                }
                case VolumetricClouds.CloudPresets.Overcast:
                {
                    cloudModelData.densityMultiplier = 0.25f;
                    cloudModelData.shapeFactor = 0.5f;
                    cloudModelData.shapeScale = 6.0f;
                    cloudModelData.erosionFactor = 0.5f;
                    cloudModelData.erosionScale = 40.0f;
                    cloudModelData.erosionNoise = VolumetricClouds.CloudErosionNoise.Perlin32;
                    return;
                }
                case VolumetricClouds.CloudPresets.Stormy:
                {
                    cloudModelData.densityMultiplier = 0.3f;
                    cloudModelData.shapeFactor = 0.9f;
                    cloudModelData.shapeScale =  2.0f;
                    cloudModelData.erosionFactor = 0.8f;
                    cloudModelData.erosionScale = 50.0f;
                    cloudModelData.erosionNoise = VolumetricClouds.CloudErosionNoise.Perlin32;
                    return;
                }
            }

            // Default unused values
            cloudModelData.densityMultiplier = 0.38729833462f;
            cloudModelData.shapeFactor = 0.6f;
            cloudModelData.shapeScale = 0.33333333333f;
            cloudModelData.erosionFactor = 0.6f;
            cloudModelData.erosionScale = 0.33333333333f;
            cloudModelData.erosionNoise = VolumetricClouds.CloudErosionNoise.Perlin32;
        }

        // The earthRadius
        const float k_EarthRadius = 6378100.0f;

        internal struct CloudModelData
        {
            public float densityMultiplier;
            public float shapeFactor;
            public float shapeScale;
            public float erosionFactor;
            public float erosionScale;
            public VolumetricClouds.CloudErosionNoise erosionNoise;
        }

        float ErosionNoiseTypeToErosionCompensation(VolumetricClouds.CloudErosionNoise noiseType)
        {
            switch (noiseType)
            {
                case VolumetricClouds.CloudErosionNoise.Worley32:
                    return 1.0f;
                case VolumetricClouds.CloudErosionNoise.Perlin32:
                    return 0.75f;
            }
            return 1.0f;
        }

        void UpdateShaderVariableslClouds(ref ShaderVariablesClouds cb, HDCamera hdCamera, VolumetricClouds settings, ref VolumetricCloudsParameters parameters, bool shadowPass)
        {
            // Convert to kilometers
            cb._LowestCloudAltitude = settings.lowestCloudAltitude.value;
            cb._HighestCloudAltitude = settings.lowestCloudAltitude.value + settings.cloudThickness.value;
            cb._EarthRadius = Mathf.Lerp(1.0f, 0.025f, settings.earthCurvature.value) * k_EarthRadius;
            cb._CloudRangeSquared.Set(Square(cb._LowestCloudAltitude + cb._EarthRadius), Square(cb._HighestCloudAltitude + cb._EarthRadius));

            cb._NumPrimarySteps = settings.numPrimarySteps.value;
            cb._NumLightSteps = settings.numLightSteps.value;
            // 1000.0f is the maximal distance that a single step can do in theory (otherwise we endup skipping large clouds)
            cb._MaxRayMarchingDistance = Mathf.Min(settings.cloudThickness.value / 8.0f *  cb._NumPrimarySteps, hdCamera.camera.farClipPlane);
            cb._CloudMapTiling.Set(settings.cloudTiling.value.x, settings.cloudTiling.value.y, settings.cloudOffset.value.x, settings.cloudOffset.value.y);

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
            Light currentSun = GetMainLight();
            if (currentSun != null)
            {
                // Grab the target sun additional data
                HDAdditionalLightData additionalLightData;
                m_CurrentSunLight.TryGetComponent<HDAdditionalLightData>(out additionalLightData);
                cb._SunDirection = -currentSun.transform.forward;
                cb._SunRight = currentSun.transform.right;
                cb._SunUp = currentSun.transform.up;

                if (!shadowPass)
                {
                    cb._SunLightColor = m_lightList.directionalLights[0].color;
                }

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
            float theta = settings.orientation.GetValue(hdCamera) / 180.0f * Mathf.PI;
            // We apply a minus to see something moving in the right direction
            cb._WindDirection = new Vector2(-Mathf.Cos(theta), -Mathf.Sin(theta));
            cb._WindVector = hdCamera.volumetricCloudsAnimationData.cloudOffset;

            cb._LargeWindSpeed = settings.cloudMapSpeedMultiplier.value;
            cb._MediumWindSpeed = settings.shapeSpeedMultiplier.value;
            cb._SmallWindSpeed = settings.erosionSpeedMultiplier.value;
            cb._AltitudeDistortion = settings.altitudeDistortion.value * 0.25f;

            cb._MultiScattering = 1.0f - settings.multiScattering.value * 0.95f;

            CloudModelData cloudModelData;
            if (settings.cloudControl.value == VolumetricClouds.CloudControl.Simple && settings.cloudPreset.value != VolumetricClouds.CloudPresets.Custom)
            {
                GetPresetCloudMapValues(settings.cloudPreset.value, out cloudModelData);
            }
            else
            {
                cloudModelData.densityMultiplier = settings.densityMultiplier.value;
                cloudModelData.shapeFactor = settings.shapeFactor.value;
                cloudModelData.shapeScale = settings.shapeScale.value;
                cloudModelData.erosionFactor = settings.erosionFactor.value;
                cloudModelData.erosionScale = settings.erosionScale.value;
                cloudModelData.erosionNoise = settings.erosionNoiseType.value;
            }
            parameters.erosionNoise = ErosionNoiseTypeToTexture(cloudModelData.erosionNoise);

            // The density multiplier is not used linearly
            cb._DensityMultiplier = cloudModelData.densityMultiplier * cloudModelData.densityMultiplier * 2.0f;
            cb._ShapeFactor = cloudModelData.shapeFactor;
            cb._ShapeScale = cloudModelData.shapeScale;
            cb._ErosionFactor = cloudModelData.erosionFactor;
            cb._ErosionScale = cloudModelData.erosionScale;
            cb._ShapeNoiseOffset = new Vector2(settings.shapeOffsetX.value, settings.shapeOffsetZ.value);

            // If the sun has moved more than 2.0°, reduce significantly the history accumulation
            float sunAngleDifference = 0.0f;
            if (m_CurrentSunLightAdditionalLightData != null)
                sunAngleDifference = Quaternion.Angle(m_CurrentSunLightAdditionalLightData.previousTransform.rotation, m_CurrentSunLightAdditionalLightData.transform.localToWorldMatrix.rotation);
            float sunAttenuation = sunAngleDifference > 2.0f ? 0.5f : 1.0f;
            cb._TemporalAccumulationFactor = settings.temporalAccumulationFactor.value * sunAttenuation;

            cb._FinalScreenSize.Set((float)parameters.finalWidth, (float)parameters.finalHeight, 1.0f / (float)parameters.finalWidth, 1.0f / (float)parameters.finalHeight);
            cb._IntermediateScreenSize.Set((float)parameters.intermediateWidth, (float)parameters.intermediateHeight, 1.0f / (float)parameters.intermediateWidth, 1.0f / (float)parameters.intermediateHeight);
            cb._TraceScreenSize.Set((float)parameters.traceWidth, (float)parameters.traceHeight, 1.0f / (float)parameters.traceWidth, 1.0f / (float)parameters.traceHeight);

            float absoluteCloudHighest = cb._HighestCloudAltitude + cb._EarthRadius;
            cb._MaxCloudDistance = Mathf.Sqrt(absoluteCloudHighest * absoluteCloudHighest - cb._EarthRadius * cb._EarthRadius);
            cb._ErosionOcclusion = settings.erosionOcclusion.value;
            cb._ErosionFactorCompensation = ErosionNoiseTypeToErosionCompensation(settings.erosionNoiseType.value);

            // If this is a planar reflection, we need to compute the non oblique matrices
            if (hdCamera.camera.cameraType == CameraType.Reflection)
            {
                // Build a non-oblique projection matrix
                var projectionMatrixNonOblique = Matrix4x4.Perspective(hdCamera.camera.fieldOfView, hdCamera.camera.aspect, hdCamera.camera.nearClipPlane, hdCamera.camera.farClipPlane);

                // Convert the projection matrix to its  GPU version
                var gpuProjNonOblique = GL.GetGPUProjectionMatrix(projectionMatrixNonOblique, true);

                // Fetch the view and previous view matrix
                Matrix4x4 gpuView = hdCamera.mainViewConstants.viewMatrix;
                Matrix4x4 prevGpuView = hdCamera.mainViewConstants.prevViewMatrix;

                // Build the non oblique view projection matrix
                var vpNonOblique = gpuProjNonOblique * gpuView;
                var prevVpNonOblique = gpuProjNonOblique * prevGpuView;

                // Output the non oblique matrices
                cb._CameraViewProjection_NO = vpNonOblique;
                cb._CameraInverseViewProjection_NO = vpNonOblique.inverse;
                cb._CameraPrevViewProjection_NO = prevVpNonOblique;
            }

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

                // The world space camera will be required but the global constant buffer will not be injected yet.
                cb._WorldSpaceShadowCenter = new Vector2(hdCamera.camera.transform.position.x, hdCamera.camera.transform.position.z);

                if (HasVolumetricCloudsShadows(hdCamera, settings))
                {
                    float scaleX = Mathf.Abs(Vector3.Dot(cb._SunRight, Vector3.Normalize(new Vector3(cb._SunRight.x, 0.0f, cb._SunRight.z))));
                    float scaleY = Mathf.Abs(Vector3.Dot(cb._SunUp, Vector3.Normalize(new Vector3(cb._SunUp.x, 0.0f, cb._SunUp.z))));
                    cb._ShadowRegionSize = new Vector2(groundShadowSize * scaleX, groundShadowSize * scaleY);
                }
            }

            cb._EnableFastToneMapping = parameters.enableExposureControl ? 1 : 0;

            unsafe
            {
                for (int p = 0; p < 4; ++p)
                    for (int i = 0; i < 9; ++i)
                        cb._DistanceBasedWeights[12 * p + i] = m_DistanceBasedWeights[9 * p + i];
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
                case VolumetricClouds.CloudPresets.Stormy:
                    return m_StormyPresetMap;
                case VolumetricClouds.CloudPresets.Custom:
                    return m_CloudyPresetMap;
            }
            return Texture2D.blackTexture;
        }

        Texture3D ErosionNoiseTypeToTexture(VolumetricClouds.CloudErosionNoise noiseType)
        {
            switch (noiseType)
            {
                case VolumetricClouds.CloudErosionNoise.Worley32:
                    return m_Asset.renderPipelineResources.textures.worleyNoise32RGB;
                case VolumetricClouds.CloudErosionNoise.Perlin32:
                    return m_Asset.renderPipelineResources.textures.perlinNoise32RGB;
            }
            return m_Asset.renderPipelineResources.textures.worleyNoise32RGB;
        }

        VolumetricCloudsParameters PrepareVolumetricCloudsParameters(HDCamera hdCamera, VolumetricClouds settings, bool shadowPass, bool historyValidity)
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
            parameters.planarReflection = (hdCamera.camera.cameraType == CameraType.Reflection);
            parameters.localClouds = settings.localClouds.value;

            // MSAA support
            parameters.needsTemporaryBuffer = hdCamera.msaaEnabled;
            parameters.cloudCombinePass = m_CloudCombinePass;

            parameters.needExtraColorBufferCopy = (GetColorBufferFormat() == GraphicsFormat.B10G11R11_UFloatPack32 &&
                // On PC and Metal, but not on console.
                (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12 ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan));

            // In case of MSAA, we no longer require the preliminary copy as there is no longer a need for RW of the color buffer.
            parameters.needExtraColorBufferCopy &= !parameters.needsTemporaryBuffer;

            // Compute shader and kernels
            parameters.volumetricCloudsCS = m_Asset.renderPipelineResources.shaders.volumetricCloudsCS;
            parameters.convertObliqueDepthKernel = m_ConvertObliqueDepthKernel;
            parameters.depthDownscaleKernel = m_CloudDownscaleDepthKernel;
            parameters.renderKernel = m_CloudRenderKernel;
            parameters.reprojectKernel = m_CloudReprojectKernel;
            parameters.upscaleAndCombineKernel = parameters.needExtraColorBufferCopy ? m_UpscaleAndCombineCloudsKernelColorCopy : m_UpscaleAndCombineCloudsKernelColorRW;
            parameters.shadowsKernel = m_ComputeShadowCloudsKernel;

            // Static textures
            if (settings.cloudControl.value == VolumetricClouds.CloudControl.Simple)
            {
                parameters.cloudMapTexture = GetPresetCloudMapTexture(settings.cloudPreset.value);
                if (settings.cloudPreset.value == VolumetricClouds.CloudPresets.Custom)
                {
                    PrepareCustomLutData(settings);
                    parameters.cloudLutTexture = m_CustomLutPresetMap;
                }
                else
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
            BlueNoise blueNoise = GetBlueNoiseManager();
            parameters.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();
            parameters.sunLight = GetMainLight();
            parameters.enableExposureControl = hdCamera.exposureControlFS;

            // Update the constant buffer
            UpdateShaderVariableslClouds(ref parameters.cloudsCB, hdCamera, settings, ref parameters, shadowPass);

            return parameters;
        }

        static void TraceVolumetricClouds(CommandBuffer cmd, VolumetricCloudsParameters parameters,
            RTHandle colorBuffer, RTHandle depthPyramid, TextureHandle motionVectors, TextureHandle volumetricLightingTexture, TextureHandle scatteringFallbackTexture,
            RTHandle currentHistory0Buffer, RTHandle previousHistory0Buffer,
            RTHandle currentHistory1Buffer, RTHandle previousHistory1Buffer,
            RTHandle intermediateLightingBuffer0, RTHandle intermediateLightingBuffer1, RTHandle intermediateDepthBuffer0, RTHandle intermediateDepthBuffer1, RTHandle intermediateDepthBuffer2,
            RTHandle intermediateColorBuffer, RTHandle intermediateUpscaleBuffer)
        {
            // Compute the number of tiles to evaluate
            int traceTX = (parameters.traceWidth + (8 - 1)) / 8;
            int traceTY = (parameters.traceHeight + (8 - 1)) / 8;

            // Compute the number of tiles to evaluate
            int intermediateTX = (parameters.intermediateWidth + (8 - 1)) / 8;
            int intermediateTY = (parameters.intermediateHeight + (8 - 1)) / 8;

            // Compute the number of tiles to evaluate
            int finalTX = (parameters.finalWidth + (8 - 1)) / 8;
            int finalTY = (parameters.finalHeight + (8 - 1)) / 8;

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
            CoreUtils.SetKeyword(cmd, "PLANAR_REFLECTION_CAMERA", parameters.planarReflection);
            CoreUtils.SetKeyword(cmd, "LOCAL_VOLUMETRIC_CLOUDS", parameters.localClouds);

            RTHandle currentDepthBuffer = depthPyramid;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsPrepare)))
            {
                if (parameters.planarReflection)
                {
                    // In order to be able to work with planar
                    cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.convertObliqueDepthKernel, HDShaderIDs._DepthTexture, depthPyramid);
                    cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.convertObliqueDepthKernel, HDShaderIDs._DepthBufferRW, intermediateDepthBuffer2);
                    cmd.DispatchCompute(parameters.volumetricCloudsCS, parameters.convertObliqueDepthKernel, finalTX, finalTY, parameters.viewCount);
                    currentDepthBuffer = intermediateDepthBuffer2;
                }

                // Compute the alternative version of the mip 1 of the depth (min instead of max that is required to handle high frequency meshes (vegetation, hair)
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.depthDownscaleKernel, HDShaderIDs._DepthTexture, currentDepthBuffer);
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
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._ErosionNoise, parameters.erosionNoise);
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
                if (parameters.needExtraColorBufferCopy)
                {
                    HDUtils.BlitCameraTexture(cmd, colorBuffer, intermediateColorBuffer);
                }

                // Compute the final resolution parameters
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._DepthTexture, currentDepthBuffer);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._DepthStatusTexture, currentHistory1Buffer);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._VolumetricCloudsTexture, currentHistory0Buffer);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._CameraColorTextureRW, colorBuffer);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._CameraColorTexture, intermediateColorBuffer);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._VBufferLighting, volumetricLightingTexture);
                if (parameters.cloudsCB._PhysicallyBasedSun == 0)
                {
                    cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._AirSingleScatteringTexture, scatteringFallbackTexture);
                    cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._AerosolSingleScatteringTexture, scatteringFallbackTexture);
                    cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._MultipleScatteringTexture, scatteringFallbackTexture);
                }

                if (parameters.needsTemporaryBuffer)
                {
                    CoreUtils.SetKeyword(cmd, "USE_INTERMEDIATE_BUFFER", true);

                    // Provide this second upscaling + combine strategy in case a temporary buffer is requested (ie MSAA).
                    // In the case of an MSAA color target, we cannot use the in-place blending of the clouds with the color target.
                    cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._VolumetricCloudsUpscaleTextureRW, intermediateUpscaleBuffer);

                    // Perform the upscale into an intermediate buffer.
                    cmd.DispatchCompute(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, finalTX, finalTY, parameters.viewCount);

                    parameters.cloudCombinePass.SetTexture(HDShaderIDs._VolumetricCloudsUpscaleTextureRW, intermediateUpscaleBuffer);

                    // Composite the clouds into the MSAA target via hardware blending.
                    HDUtils.DrawFullScreen(cmd, parameters.cloudCombinePass, colorBuffer);

                    CoreUtils.SetKeyword(cmd, "USE_INTERMEDIATE_BUFFER", false);
                }
                else
                {
                    cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._CameraColorTextureRW, colorBuffer);

                    // Perform the upscale and combine with the color buffer in place.
                    cmd.DispatchCompute(parameters.volumetricCloudsCS, parameters.upscaleAndCombineKernel, finalTX, finalTY, parameters.viewCount);
                }
            }
            CoreUtils.SetKeyword(cmd, "PLANAR_REFLECTION_CAMERA", false);
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
            public TextureHandle intermediateBufferDepth2;
            public TextureHandle intermediateBufferUpscale;

            public TextureHandle intermediateColorBufferCopy;
        }

        int CombineVolumetricCLoudsHistoryStateToMask(bool localClouds)
        {
            // Combine the flags to define the current mask (we use the custom bit 0 to track the locality of the clouds.
            return (localClouds ? (int)HDCamera.HistoryEffectFlags.CustomBit0 : 0);
        }

        private bool EvaluateVolumetricCloudsHistoryValidity(HDCamera hdCamera, bool localClouds)
        {
            // Evaluate the history validity
            int flagMask = CombineVolumetricCLoudsHistoryStateToMask(localClouds);
            return hdCamera.EffectHistoryValidity(HDCamera.HistoryEffectSlot.VolumetricClouds, flagMask);
        }

        private void PropagateVolumetricCloudsHistoryValidity(HDCamera hdCamera, bool localClouds)
        {
            int flagMask = CombineVolumetricCLoudsHistoryStateToMask(localClouds);
            hdCamera.PropagateEffectHistoryValidity(HDCamera.HistoryEffectSlot.VolumetricClouds, flagMask);
        }

        TextureHandle TraceVolumetricClouds(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthPyramid, TextureHandle motionVectors, TextureHandle volumetricLighting)
        {
            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsData>("Generating the rays for RTR", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricClouds)))
            {
                builder.EnableAsyncCompute(false);
                VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

                passData.parameters = PrepareVolumetricCloudsParameters(hdCamera, settings, false, EvaluateVolumetricCloudsHistoryValidity(hdCamera, settings.localClouds.value));
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


                passData.intermediateColorBufferCopy = passData.parameters.needExtraColorBufferCopy ? builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GetColorBufferFormat(), enableRandomWrite = true, name = "Temporary Color Buffer" }) : renderGraph.defaultResources.blackTextureXR;

                if (passData.parameters.needsTemporaryBuffer)
                {
                    passData.intermediateBufferUpscale = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                        { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporary Clouds Upscaling Buffer" });
                }
                else
                {
                    passData.intermediateBufferUpscale = renderGraph.defaultResources.blackTexture;
                }

                if (passData.parameters.planarReflection)
                {
                    passData.intermediateBufferDepth2 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                        { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Temporary Clouds Depth Buffer 2" });
                }
                else
                {
                    passData.intermediateBufferDepth2 = renderGraph.defaultResources.blackTexture;
                }

                builder.SetRenderFunc(
                    (VolumetricCloudsData data, RenderGraphContext ctx) =>
                    {
                        TraceVolumetricClouds(ctx.cmd, data.parameters,
                            data.colorBuffer, data.depthPyramid, data.motionVectors, data.volumetricLighting, data.scatteringFallbackTexture,
                            data.currentHistoryBuffer0, data.previousHistoryBuffer0, data.currentHistoryBuffer1, data.previousHistoryBuffer1,
                            data.intermediateBuffer0, data.intermediateBuffer1, data.intermediateBufferDepth0, data.intermediateBufferDepth1, data.intermediateBufferDepth2,
                            data.intermediateColorBufferCopy, data.intermediateBufferUpscale);
                    });

                PushFullScreenDebugTexture(m_RenderGraph, passData.currentHistoryBuffer0, FullScreenDebugMode.VolumetricClouds);

                return passData.colorBuffer;
            }
        }

        void UpdateVolumetricClouds(HDCamera hdCamera, in VolumetricClouds settings)
        {
            // The system needs to be reset if this is the first frame or the history is not from the previous frame
            if (hdCamera.volumetricCloudsAnimationData.lastTime == -1.0f || !EvaluateVolumetricCloudsHistoryValidity(hdCamera, settings.localClouds.value))
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
                float theta = settings.orientation.GetValue(hdCamera) / 180.0f * Mathf.PI;

                // Compute the wind direction
                Vector2 windDirection = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));

                // Conversion  from km/h to m/s  is the 0.277778f factor
                // We apply a minus to see something moving in the right direction
                Vector2 windVector = -windDirection * settings.globalWindSpeed.GetValue(hdCamera) * delaTime * 0.277778f;

                // Animate the offset
                hdCamera.volumetricCloudsAnimationData.cloudOffset += windVector;

                // Update the time
                hdCamera.volumetricCloudsAnimationData.lastTime = hdCamera.time;
            }
        }

        void RenderVolumetricClouds(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthPyramid, TextureHandle motionVector, TextureHandle volumetricLighting)
        {
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

            // If the current volume does not enable the feature, quit right away.
            if (!HasVolumetricClouds(hdCamera, in settings))
                return;

            // Make sure the volumetric clouds are animated properly
            UpdateVolumetricClouds(hdCamera, in settings);

            // Render the clouds
            TraceVolumetricClouds(renderGraph, hdCamera, colorBuffer, depthPyramid, motionVector, volumetricLighting);

            // Make sure to mark the history frame index validity.
            PropagateVolumetricCloudsHistoryValidity(hdCamera, settings.localClouds.value);
        }

        void PreRenderVolumetricClouds(RenderGraph renderGraph, HDCamera hdCamera)
        {
            // Grab the volume settings
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

            // If the clouds are enabled on this camera
            if (!HasVolumetricClouds(hdCamera, in settings))
                return;

            // Given that the rendering of the shadow happens before the render graph execution, we can only have the display debug here (and not during the light data build).
            if (HasVolumetricCloudsShadows(hdCamera))
            {
                RTHandle currentHandle = RequestVolumetricCloudsShadowTexture(settings);
                PushFullScreenDebugTexture(m_RenderGraph, renderGraph.ImportTexture(currentHandle), FullScreenDebugMode.VolumetricCloudsShadow, xrTexture: false);
            }

            // Evaluate the cloud map
            PreRenderVolumetricCloudMap(renderGraph, hdCamera, in settings);
        }
    }
}
