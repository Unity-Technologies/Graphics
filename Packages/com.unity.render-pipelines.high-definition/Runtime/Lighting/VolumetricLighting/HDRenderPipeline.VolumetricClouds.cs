using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Intermediate values for ambient probe evaluation
        ZonalHarmonicsL2 m_PhaseZHClouds;

        // Cloud preset maps
        Texture2D m_CustomPresetMap;
        Texture2D m_CustomLutPresetMap;
        const int k_CustomLutMapResolution = 64;
        readonly Color[] m_CustomLutColorArray = new Color[k_CustomLutMapResolution];

        // Prepass kernels
        int m_ConvertObliqueDepthKernel;
        int m_CloudDownscaleDepthKernel;

        // Cloud rendering kernels
        int m_CloudRenderKernel;

        // Reprojection / First upscale
        int m_ReprojectCloudsKernel;
        int m_ReprojectCloudsRejectionKernel;
        int m_PreUpscaleCloudsKernel;
        int m_PreUpscaleCloudsSkyKernel;

        // Second Upscale + Fog
        int m_UpscaleCloudsKernel;
        int m_UpscaleCloudsPerceptualKernel;
        int m_UpscaleCloudsSkyKernel;

        // Fog only
        int m_CombineCloudsKernel;
        int m_CombineCloudsPerceptualKernel;
        int m_CombineCloudsSkyKernel;

        // Flag that allows us to track the resources that habe been allocated
        bool m_ActiveVolumetricClouds;

        // Combine pass via hardware blending.
        Material m_CloudCombinePass;
        ComputeShader m_CloudsCombineCS;
        int m_CombineCloudsWaterKernel;

        struct VolumetricCloudsCameraData
        {
            public TVolumetricCloudsCameraType cameraType;
            public int traceWidth;
            public int traceHeight;
            public int intermediateWidth;
            public int intermediateHeight;
            public int finalWidth;
            public int finalHeight;
            public int viewCount;
            public bool enableExposureControl;
            public bool lowResolution;
            public bool enableIntegration;
        }

        /// <summary>
        // Struct that lists the data required to perform the volumetric clouds animation
        /// </summary>
        internal struct VolumetricCloudsAnimationData
        {
            public float lastTime;
            public Vector2 cloudOffset;
            public float verticalShapeOffset;
            public float verticalErosionOffset;
        }

        // This property allows us to track the volumetric cloud animation data
        internal VolumetricCloudsAnimationData volumetricCloudsAnimationData;

        void InitializeVolumetricClouds()
        {
            // Keep track of the state for the release
            m_ActiveVolumetricClouds = m_Asset.currentPlatformRenderPipelineSettings.supportVolumetricClouds;
            if (!m_ActiveVolumetricClouds)
                return;

            // Allocate the buffers for ambient probe evaluation
            m_PhaseZHClouds = new ZonalHarmonicsL2();
            m_PhaseZHClouds.coeffs = new float[3];

            // Grab the kernels we need
            ComputeShader volumetricCloudsCS = m_Asset.renderPipelineResources.shaders.volumetricCloudsCS;
            m_ConvertObliqueDepthKernel = volumetricCloudsCS.FindKernel("ConvertObliqueDepth");
            m_CloudDownscaleDepthKernel = volumetricCloudsCS.FindKernel("DownscaleDepth");

            m_ReprojectCloudsKernel = volumetricCloudsCS.FindKernel("ReprojectClouds");
            m_ReprojectCloudsRejectionKernel = volumetricCloudsCS.FindKernel("ReprojectCloudsRejection");
            m_PreUpscaleCloudsKernel = volumetricCloudsCS.FindKernel("PreUpscaleClouds");
            m_PreUpscaleCloudsSkyKernel = volumetricCloudsCS.FindKernel("PreUpscaleCloudsSky");

            m_UpscaleCloudsKernel = volumetricCloudsCS.FindKernel("UpscaleClouds");
            m_UpscaleCloudsPerceptualKernel = volumetricCloudsCS.FindKernel("UpscaleCloudsPerceptual");
            m_UpscaleCloudsSkyKernel = volumetricCloudsCS.FindKernel("UpscaleCloudsSky");

            m_CombineCloudsKernel = volumetricCloudsCS.FindKernel("CombineClouds");
            m_CombineCloudsPerceptualKernel = volumetricCloudsCS.FindKernel("CombineCloudsPerceptual");
            m_CombineCloudsSkyKernel = volumetricCloudsCS.FindKernel("CombineCloudsSky");

            // Create the material needed for the combination
            m_CloudCombinePass = CoreUtils.CreateEngineMaterial(defaultResources.shaders.volumetricCloudsCombinePS);
            m_CloudsCombineCS = m_Asset.renderPipelineResources.shaders.volumetricCloudsCombineCS;
            m_CombineCloudsWaterKernel = m_CloudsCombineCS.FindKernel("CombineCloudsWater");

            ComputeShader volumetricCloudsTraceCS = m_Asset.renderPipelineResources.shaders.volumetricCloudsTraceCS;
            m_CloudRenderKernel = volumetricCloudsTraceCS.FindKernel("RenderClouds");

            // Allocate all the texture initially
            AllocatePresetTextures();

            // Initialize cloud animation
            volumetricCloudsAnimationData.lastTime = -1.0f;
            volumetricCloudsAnimationData.cloudOffset = new Vector2(0.0f, 0.0f);
            volumetricCloudsAnimationData.verticalShapeOffset = 0.0f;
            volumetricCloudsAnimationData.verticalErosionOffset = 0.0f;

            // Initialize the additional sub components
            InitializeVolumetricCloudsMap();
            InitializeVolumetricCloudsShadows();
            InitializeVolumetricCloudsAmbientProbe();
        }

        void ReleaseVolumetricClouds()
        {
            if (!m_ActiveVolumetricClouds)
                return;

            // Destroy the material
            CoreUtils.Destroy(m_CloudCombinePass);

            // Release the additional sub components
            ReleaseVolumetricCloudsMap();
            ReleaseVolumetricCloudsShadows();
            ReleaseVolumetricCloudsAmbientProbe();
        }

        void AllocatePresetTextures()
        {
            m_CustomPresetMap = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None) { name = "Default Cloud Map Texture" };
            m_CustomPresetMap.SetPixel(0, 0, new Color(0.9f, 0.0f, 0.25f, 1.0f));
            m_CustomPresetMap.Apply();
        }

        float Square(float x)
        {
            return x * x;
        }

        float ComputeNormalizationFactor(float earthRadius, float lowerCloudRadius)
        {
            return Mathf.Sqrt((k_EarthRadius + lowerCloudRadius) * (k_EarthRadius + lowerCloudRadius) - k_EarthRadius * earthRadius);
        }

        // The earthRadius
        const float k_EarthRadius = 6378100.0f;

        internal struct CloudModelData
        {
            public float densityMultiplier;

            // Shape
            public float shapeFactor;
            public float shapeScale;

            // Erosion
            public float erosionFactor;
            public float erosionScale;
            public VolumetricClouds.CloudErosionNoise erosionNoise;

            // Micro erosion
            public float microErosionFactor;
            public float microErosionScale;
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

            var densityCurve = clouds.densityCurve.value;
            var erosionCurve = clouds.erosionCurve.value;
            var ambientOcclusionCurve = clouds.ambientOcclusionCurve.value;
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
                    float density = (i == 0 || i == k_CustomLutMapResolution - 1) ? 0 : Mathf.Clamp(densityCurve.Evaluate(currTime), 0.0f, 1.0f);
                    float erosion = Mathf.Clamp(erosionCurve.Evaluate(currTime), 0.0f, 1.0f);
                    float ambientOcclusion = Mathf.Clamp(1.0f - ambientOcclusionCurve.Evaluate(currTime), 0.0f, 1.0f);
                    pixels[i] = new Color(density, erosion, ambientOcclusion, 1.0f);
                }
            }

            m_CustomLutPresetMap.SetPixels(pixels);
            m_CustomLutPresetMap.Apply();
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

        static bool VolumetricCloudsRequireMaxZ(HDCamera hdCamera)
        {
            // Volumetric clouds require the max Z pass if the camera has clouds and we are in non local mode
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();
            return HasVolumetricClouds(hdCamera, in settings) && !settings.localClouds.value;
        }

        Texture2D GetPresetCloudMapTexture()
        {
            // Textures may become null if a new scene was loaded in the editor (and maybe other reasons).
            if (m_CustomPresetMap == null || Object.ReferenceEquals(m_CustomPresetMap, null))
                AllocatePresetTextures();
            return m_CustomPresetMap;
        }

        internal enum TVolumetricCloudsCameraType
        {
            Default,
            RealtimeReflection,
            BakedReflection,
            PlanarReflection,
            Sky
        };

        TVolumetricCloudsCameraType GetCameraType(HDCamera hdCamera)
        {
            if (hdCamera.camera.cameraType == CameraType.Reflection)
            {
                if (GeometryUtils.IsProjectionMatrixOblique(hdCamera.camera.projectionMatrix))
                    return TVolumetricCloudsCameraType.PlanarReflection;
                else
                {
                    if (hdCamera.realtimeReflectionProbe)
                        return TVolumetricCloudsCameraType.RealtimeReflection;
                    else
                        return TVolumetricCloudsCameraType.BakedReflection;
                }
            }
            else
                return TVolumetricCloudsCameraType.Default;
        }

        CloudModelData GetCloudModelData(VolumetricClouds settings)
        {
            CloudModelData cloudModelData;
            // General
            cloudModelData.densityMultiplier = settings.densityMultiplier.value;
            // Shape
            cloudModelData.shapeFactor = settings.shapeFactor.value;
            cloudModelData.shapeScale = settings.shapeScale.value;
            // Erosion
            cloudModelData.erosionFactor = settings.erosionFactor.value;
            cloudModelData.erosionScale = settings.erosionScale.value;
            cloudModelData.erosionNoise = settings.erosionNoiseType.value;
            // Micro erosion
            cloudModelData.microErosionFactor = settings.microErosionFactor.value;
            cloudModelData.microErosionScale = settings.microErosionScale.value;
            return cloudModelData;
        }

        void UpdateShaderVariableslClouds(ref ShaderVariablesClouds cb, HDCamera hdCamera, VolumetricClouds settings,
            in VolumetricCloudsCameraData cameraData, in CloudModelData cloudModelData, bool shadowPass)
        {
            // Convert to kilometers
            cb._LowestCloudAltitude = settings.bottomAltitude.value;

            // When in non local mode, the camera is supposed to be always strictly under the clouds
            // to avoid artifacts due to precision issues, when in non local, the clouds are always 1 meter above the camera.
            if (!settings.localClouds.value)

                cb._LowestCloudAltitude = Mathf.Max(cb._LowestCloudAltitude, 1.0f);

            cb._HighestCloudAltitude = cb._LowestCloudAltitude + settings.altitudeRange.value;
            cb._EarthRadius = Mathf.Lerp(1.0f, 0.025f, settings.earthCurvature.value) * k_EarthRadius;
            cb._CloudRangeSquared.Set(Square(cb._LowestCloudAltitude + cb._EarthRadius), Square(cb._HighestCloudAltitude + cb._EarthRadius));

            cb._NumPrimarySteps = settings.numPrimarySteps.value;
            cb._NumLightSteps = settings.numLightSteps.value;
            // 1000.0f is the maximal distance that a single step can do in theory (otherwise we endup skipping large clouds)
            cb._MaxRayMarchingDistance = Mathf.Min(settings.altitudeRange.value / 8.0f * cb._NumPrimarySteps, hdCamera.camera.farClipPlane);
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
            cb._PhysicallyBasedSun = visualEnvironment.skyType.value == (int)SkyType.PhysicallyBased ? 1 : 0;
            Light currentSun = GetMainLight();
            HDAdditionalLightData additionalLightData = null;
            if (currentSun != null)
            {
                // Grab the target sun additional data
                m_CurrentSunLight.TryGetComponent<HDAdditionalLightData>(out additionalLightData);
                cb._SunDirection = -currentSun.transform.forward;
                cb._SunRight = currentSun.transform.right;
                cb._SunUp = currentSun.transform.up;

                if (!shadowPass)
                {
                    // m_CurrentSunLightDataIndex is supposed to be guaranteed to be non -1 if the current sun is not null
                    cb._SunLightColor = m_GpuLightsBuilder.directionalLights[m_CurrentSunLightDataIndex].color * settings.sunLightDimmer.value;
                }
            }
            else
            {
                cb._SunDirection = Vector3.up;
                cb._SunRight = Vector3.right;
                cb._SunUp = Vector3.forward;
                cb._SunLightColor = Vector3.zero;
            }

            // Compute the theta angle for the wind direction
            float theta = settings.orientation.GetValue(hdCamera) / 180.0f * Mathf.PI;
            // We apply a minus to see something moving in the right direction
            cb._WindDirection = new Vector2(-Mathf.Cos(theta), -Mathf.Sin(theta));
            cb._WindVector = volumetricCloudsAnimationData.cloudOffset;

            cb._VerticalShapeWindDisplacement = volumetricCloudsAnimationData.verticalShapeOffset;
            cb._VerticalErosionWindDisplacement = volumetricCloudsAnimationData.verticalErosionOffset;

            cb._LargeWindSpeed = settings.cloudMapSpeedMultiplier.value;
            cb._MediumWindSpeed = settings.shapeSpeedMultiplier.value;
            cb._SmallWindSpeed = settings.erosionSpeedMultiplier.value;
            cb._AltitudeDistortion = settings.altitudeDistortion.value * 0.25f;

            cb._MultiScattering = 1.0f - settings.multiScattering.value * 0.95f;

            // The density multiplier is not used linearly
            cb._DensityMultiplier = cloudModelData.densityMultiplier * cloudModelData.densityMultiplier * 2.0f;

            // Shape
            cb._ShapeFactor = cloudModelData.shapeFactor;
            cb._ShapeScale = cloudModelData.shapeScale;
            cb._ShapeNoiseOffset = new Vector2(settings.shapeOffset.value.x, settings.shapeOffset.value.z);
            cb._VerticalShapeNoiseOffset = settings.shapeOffset.value.y;

            // Erosion
            cb._ErosionFactor = cloudModelData.erosionFactor;
            cb._ErosionScale = cloudModelData.erosionScale;

            // Micro erosion
            cb._MicroErosionFactor = cloudModelData.microErosionFactor;
            cb._MicroErosionScale = cloudModelData.microErosionScale;

            // If the sun has moved more than 2.0°, reduce significantly the history accumulation
            float sunAngleDifference = 0.0f;
            if (additionalLightData != null)
                sunAngleDifference = Quaternion.Angle(additionalLightData.previousTransform.rotation, additionalLightData.transform.localToWorldMatrix.rotation);
            cb._CloudHistoryInvalidation = Mathf.Lerp(1.0f, 0.0f, Mathf.Clamp((sunAngleDifference) / 10.0f, 0.0f, 1.0f));
            cb._TemporalAccumulationFactor = settings.temporalAccumulationFactor.value;

            if (settings.fadeInMode.value == VolumetricClouds.CloudFadeInMode.Automatic)
            {
                cb._FadeInStart = Mathf.Max((cb._HighestCloudAltitude - cb._LowestCloudAltitude) * 0.2f, hdCamera.camera.nearClipPlane);
                cb._FadeInDistance = (cb._HighestCloudAltitude - cb._LowestCloudAltitude) * 0.3f;
            }
            else
            {
                cb._FadeInStart = Mathf.Max(settings.fadeInStart.value, hdCamera.camera.nearClipPlane);
                cb._FadeInDistance = settings.fadeInDistance.value;
            }

            cb._FinalScreenSize.Set((float)cameraData.finalWidth, (float)cameraData.finalHeight, 1.0f / (float)cameraData.finalWidth, 1.0f / (float)cameraData.finalHeight);
            cb._IntermediateScreenSize.Set((float)cameraData.intermediateWidth, (float)cameraData.intermediateHeight, 1.0f / (float)cameraData.intermediateWidth, 1.0f / (float)cameraData.intermediateHeight);
            cb._TraceScreenSize.Set((float)cameraData.traceWidth, (float)cameraData.traceHeight, 1.0f / (float)cameraData.traceWidth, 1.0f / (float)cameraData.traceHeight);

            float absoluteCloudHighest = cb._HighestCloudAltitude + cb._EarthRadius;
            cb._MaxCloudDistance = Mathf.Sqrt(absoluteCloudHighest * absoluteCloudHighest - cb._EarthRadius * cb._EarthRadius);
            cb._ErosionOcclusion = settings.erosionOcclusion.value;
            cb._ErosionFactorCompensation = ErosionNoiseTypeToErosionCompensation(settings.erosionNoiseType.value);

            // If this is a planar reflection, we need to compute the non oblique matrices
            cb._IsPlanarReflection = (cameraData.cameraType == TVolumetricCloudsCameraType.PlanarReflection) ? 1 : 0;
            if (cb._IsPlanarReflection == 1)
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
                cb._WorldSpaceShadowCenter = new Vector4(hdCamera.camera.transform.position.x, hdCamera.camera.transform.position.y, hdCamera.camera.transform.position.z, 0.0f);

                if (HasVolumetricCloudsShadows(hdCamera, settings))
                {
                    float scaleX = Mathf.Abs(Vector3.Dot(cb._SunRight, Vector3.Normalize(new Vector3(cb._SunRight.x, 0.0f, cb._SunRight.z))));
                    float scaleY = Mathf.Abs(Vector3.Dot(cb._SunUp, Vector3.Normalize(new Vector3(cb._SunUp.x, 0.0f, cb._SunUp.z))));
                    cb._ShadowRegionSize = new Vector2(groundShadowSize * scaleX, groundShadowSize * scaleY);
                }
            }

            cb._EnableFastToneMapping = cameraData.enableExposureControl ? 1 : 0;

            cb._LowResolutionEvaluation = cameraData.lowResolution ? 1 : 0;
            cb._EnableIntegration = cameraData.enableIntegration ? 1 : 0;
            cb._RenderForSky = cameraData.cameraType == TVolumetricCloudsCameraType.Sky ? 1 : 0;

            // The valid max z mask is only valid if we are in non local mode
            cb._ValidMaxZMask = settings.localClouds.value ? 0 : 1;

            unsafe
            {
                for (int p = 0; p < 4; ++p)
                    for (int i = 0; i < 9; ++i)
                        cb._DistanceBasedWeights[12 * p + i] = BilateralUpsample.distanceBasedWeights_3x3[9 * p + i];
            }
        }

        struct VolumetricCloudCommonData
        {
            // Resolution parameters
            public TVolumetricCloudsCameraType cameraType;
            public bool enableExposureControl;
            public bool localClouds;
            public bool microErosion;
            public bool simplePreset;

            // Static textures
            public Texture3D worley128RGBA;
            public Texture3D erosionNoise;
            public Texture cloudMapTexture;
            public Texture cloudLutTexture;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public Light sunLight;

            // Compute shader and kernels
            public ComputeShader volumetricCloudsCS;
            public ComputeShader volumetricCloudsTraceCS;

            // Cloud constant buffer buffer
            public ShaderVariablesClouds cloudsCB;
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

        void FillVolumetricCloudsCommonData(bool enableExposureControl, VolumetricClouds settings, TVolumetricCloudsCameraType cameraType, in CloudModelData cloudModelData, ref VolumetricCloudCommonData commonData)
        {
            commonData.cameraType = cameraType;
            commonData.localClouds = settings.localClouds.value;
            commonData.volumetricCloudsCS = m_Asset.renderPipelineResources.shaders.volumetricCloudsCS;
            commonData.volumetricCloudsTraceCS = m_Asset.renderPipelineResources.shaders.volumetricCloudsTraceCS;

            // Static textures
            commonData.simplePreset = settings.cloudControl.value == VolumetricClouds.CloudControl.Simple;
            if (commonData.simplePreset)
            {
                commonData.cloudMapTexture = GetPresetCloudMapTexture();
                PrepareCustomLutData(settings);
                commonData.cloudLutTexture = m_CustomLutPresetMap;
                commonData.microErosion = settings.cloudSimpleMode.value == VolumetricClouds.CloudSimpleMode.Quality;
            }
            else if (settings.cloudControl.value == VolumetricClouds.CloudControl.Advanced)
            {
                commonData.cloudMapTexture = m_AdvancedCloudMap;
                commonData.cloudLutTexture = m_Asset.renderPipelineResources.textures.cloudLutRainAO;
                commonData.microErosion = settings.microErosion.value;
            }
            else
            {
                commonData.cloudMapTexture = settings.cloudMap.value != null ? settings.cloudMap.value : Texture2D.blackTexture;
                commonData.cloudLutTexture = settings.cloudLut.value != null ? settings.cloudLut.value : Texture2D.blackTexture;
                commonData.microErosion = settings.microErosion.value;
            }

            commonData.worley128RGBA = m_Asset.renderPipelineResources.textures.worleyNoise128RGBA;
            commonData.erosionNoise = ErosionNoiseTypeToTexture(cloudModelData.erosionNoise);
            BlueNoise blueNoise = GetBlueNoiseManager();
            commonData.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();
            commonData.sunLight = GetMainLight();
            commonData.enableExposureControl = enableExposureControl;
        }

        void UpdateVolumetricClouds(HDCamera hdCamera, in VolumetricClouds settings)
        {
            // The system needs to be reset if this is the first frame or the history is not from the previous frame
            if (volumetricCloudsAnimationData.lastTime == -1.0f || !EvaluateVolumetricCloudsHistoryValidity(hdCamera, settings.localClouds.value))
            {
                // This is the first frame for the system
                volumetricCloudsAnimationData.lastTime = hdCamera.time;
            }
            else
            {
                // Compute the delta time
                float deltaTime = hdCamera.time - volumetricCloudsAnimationData.lastTime;

                // Compute the theta angle for the wind direction
                float theta = settings.orientation.GetValue(hdCamera) / 180.0f * Mathf.PI;

                // Compute the wind direction
                Vector2 windDirection = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));

                // Conversion  from km/h to m/s  is the 0.277778f factor
                // We apply a minus to see something moving in the right direction
                Vector2 windVector = 0.277778f * deltaTime * settings.globalWindSpeed.GetValue(hdCamera) * -windDirection;

                // Animate the offsets
                volumetricCloudsAnimationData.cloudOffset += windVector;
                volumetricCloudsAnimationData.verticalShapeOffset += -settings.verticalShapeWindSpeed.value * deltaTime * 0.277778f;
                volumetricCloudsAnimationData.verticalErosionOffset += -settings.verticalErosionWindSpeed.value * deltaTime * 0.277778f;

                // Update the time
                volumetricCloudsAnimationData.lastTime = hdCamera.time;
            }
        }

        class VolumetricCloudsCombineOpaqueData
        {
            // Material
            public Material cloudsCombineMaterial;
            public ShaderVariablesClouds cloudsCB;

            // Input buffer
            public TextureHandle volumetricCloudsBuffer;

            // Output buffer
            public TextureHandle colorBuffer;
        }

        void CombineVolumetricCloudsOpaque(RenderGraph renderGraph, HDCamera hdCamera, in VolumetricClouds settings, TVolumetricCloudsCameraType cameraType, TextureHandle colorBuffer, TextureHandle volumetricClouds)
        {
            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsCombineOpaqueData>("Volumetric Clouds Combine", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricCloudsCombine)))
            {
                builder.EnableAsyncCompute(false);

                // Parameters
                passData.cloudsCombineMaterial = m_CloudCombinePass;
                float perceptualBlending = settings.perceptualBlending.value;
                passData.cloudsCB._CubicTransmittance = cameraType == TVolumetricCloudsCameraType.Default && hdCamera.msaaEnabled ? perceptualBlending : 0;

                // Input buffer
                passData.volumetricCloudsBuffer = builder.ReadTexture(volumetricClouds);

                // Output buffer
                passData.colorBuffer = builder.WriteTexture(colorBuffer);

                builder.SetRenderFunc(
                    (VolumetricCloudsCombineOpaqueData data, RenderGraphContext ctx) =>
                    {
                        ConstantBuffer.Push(ctx.cmd, data.cloudsCB, data.cloudsCombineMaterial, HDShaderIDs._ShaderVariablesClouds);
                        data.cloudsCombineMaterial.SetTexture(HDShaderIDs._VolumetricCloudsLightingTexture, data.volumetricCloudsBuffer);
                        HDUtils.DrawFullScreen(ctx.cmd, data.cloudsCombineMaterial, data.colorBuffer, null, 0);
                    });
            }
        }

        class VolumetricCloudsCombineWaterData
        {
            // Dispatch params
            public int tileX;
            public int tileY;
            public int viewCount;

            // Material
            public ComputeShader cloudsCombineCS;
            public int kernel;

            // Input buffer
            public TextureHandle volumetricCloudsLighting;
            public TextureHandle volumetricCloudsDepth;
            public TextureHandle colorBuffer;
            public TextureHandle depthStencilBuffer;

            // Output buffer
            public TextureHandle outputColorBuffer;
        }

        TextureHandle CombineVolumetricCloudsWater(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle colorBuffer, TextureHandle depthStencilBuffer,
            VolumetricCloudsOutput volumetricCloudsOutput)
        {
            // If the camera doesn't have clouds or water, we shouldn't do anything
            WaterRendering waterSettings = hdCamera.volumeStack.GetComponent<WaterRendering>();

            if (!volumetricCloudsOutput.valid || !waterSettings.enable.value
                || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.Water)
                || WaterSurface.instanceCount == 0
                || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.TransparentObjects))
                return colorBuffer;

            // TODO: Dispatch indirect on water tiles.
            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsCombineWaterData>("Volumetric Clouds Combine", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricCloudsCombine)))
            {
                builder.EnableAsyncCompute(false);

                // Parameters
                passData.tileX = (hdCamera.actualWidth + 7) / 8;
                passData.tileY = (hdCamera.actualHeight + 7) / 8;
                passData.viewCount = hdCamera.viewCount;
                passData.cloudsCombineCS = m_CloudsCombineCS;
                passData.kernel = m_CombineCloudsWaterKernel;

                // Input buffers
                passData.volumetricCloudsLighting = builder.ReadTexture(volumetricCloudsOutput.lightingBuffer);
                passData.volumetricCloudsDepth = builder.ReadTexture(volumetricCloudsOutput.depthBuffer);
                passData.colorBuffer = builder.WriteTexture(colorBuffer);
                passData.depthStencilBuffer = builder.WriteTexture(depthStencilBuffer);

                // Output buffers
                passData.outputColorBuffer = builder.WriteTexture(renderGraph.CreateTexture(colorBuffer));

                builder.SetRenderFunc(
                    (VolumetricCloudsCombineWaterData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetComputeTextureParam(data.cloudsCombineCS, data.kernel, HDShaderIDs._VolumetricCloudsLightingTexture, data.volumetricCloudsLighting);
                        ctx.cmd.SetComputeTextureParam(data.cloudsCombineCS, data.kernel, HDShaderIDs._VolumetricCloudsDepthTexture, data.volumetricCloudsDepth);
                        ctx.cmd.SetComputeTextureParam(data.cloudsCombineCS, data.kernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.cloudsCombineCS, data.kernel, HDShaderIDs._StencilTexture, data.depthStencilBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeTextureParam(data.cloudsCombineCS, data.kernel, HDShaderIDs._CameraColorTexture, data.colorBuffer);
                        ctx.cmd.SetComputeTextureParam(data.cloudsCombineCS, data.kernel, HDShaderIDs._CameraColorTextureRW, data.outputColorBuffer);
                        ctx.cmd.DispatchCompute(data.cloudsCombineCS, data.kernel, data.tileX, data.tileY, data.viewCount);
                    });

                return passData.outputColorBuffer;
            }
        }

        struct VolumetricCloudsOutput
        {
            public TextureHandle lightingBuffer;
            public TextureHandle depthBuffer;
            public bool valid;
        }

        VolumetricCloudsOutput RenderVolumetricClouds(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthPyramid, TextureHandle motionVector, TextureHandle volumetricLighting, TextureHandle maxZMask)
        {
            // If the current volume does not enable the feature, quit right away.
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();
            bool skipCloudRendering = m_CurrentDebugDisplaySettings.DebugHideSky(hdCamera) || !HasVolumetricClouds(hdCamera, in settings);
#if UNITY_EDITOR
            skipCloudRendering |= !hdCamera.camera.renderCloudsInSceneView; 
#endif
            if (skipCloudRendering)
            {
                VolumetricCloudsOutput emptyClouds = new VolumetricCloudsOutput();
                emptyClouds.lightingBuffer = renderGraph.defaultResources.whiteTextureXR;
                emptyClouds.depthBuffer = renderGraph.defaultResources.blackTextureXR;
                emptyClouds.valid = false;
                return emptyClouds;
            }

            // Make sure the volumetric clouds are animated properly
            UpdateVolumetricClouds(hdCamera, in settings);

            // Evaluate which version of the clouds we should be using
            TVolumetricCloudsCameraType cameraType = GetCameraType(hdCamera);
            bool accumulationClouds = cameraType == TVolumetricCloudsCameraType.Default || cameraType == TVolumetricCloudsCameraType.PlanarReflection;
            bool fullResolutionClouds = cameraType == TVolumetricCloudsCameraType.BakedReflection;

            // Render the clouds
            VolumetricCloudsOutput output;
            if (accumulationClouds)
                output = RenderVolumetricClouds_Accumulation(renderGraph, hdCamera, cameraType, colorBuffer, depthPyramid, motionVector, volumetricLighting, maxZMask);
            else if (fullResolutionClouds)
                output = RenderVolumetricClouds_FullResolution(renderGraph, hdCamera, cameraType, colorBuffer, depthPyramid, motionVector, volumetricLighting, maxZMask);
            else
                output = RenderVolumetricClouds_LowResolution(renderGraph, hdCamera, cameraType, colorBuffer, depthPyramid, motionVector, volumetricLighting, maxZMask);

            // Push the texture to the debug menu
            if (m_CurrentDebugDisplaySettings.data.volumetricCloudDebug == VolumetricCloudsDebug.Lighting)
                PushFullScreenDebugTexture(m_RenderGraph, output.lightingBuffer, FullScreenDebugMode.VolumetricClouds);
            else
                PushFullScreenDebugTexture(m_RenderGraph, output.depthBuffer, FullScreenDebugMode.VolumetricClouds, GraphicsFormat.R32_SFloat);

            // Now that the volumetric clouds texture has been generated, combine it with the opaque and sky
            CombineVolumetricCloudsOpaque(renderGraph, hdCamera, settings, cameraType, colorBuffer, output.lightingBuffer);

            // Return the scattering and transmittance
            return output;
        }

        void PreRenderVolumetricClouds(RenderGraph renderGraph, HDCamera hdCamera)
        {
            if (m_CurrentDebugDisplaySettings.DebugHideSky(hdCamera))
                return;

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
