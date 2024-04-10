using Unity.Mathematics;
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

        // Compute shader kernels
        ComputeShader m_VolumetricCloudsCS;
        ComputeShader m_VolumetricCloudsTraceCS;

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
            m_VolumetricCloudsCS = m_Asset.renderPipelineResources.shaders.volumetricCloudsCS;
            m_ConvertObliqueDepthKernel = m_VolumetricCloudsCS.FindKernel("ConvertObliqueDepth");
            m_CloudDownscaleDepthKernel = m_VolumetricCloudsCS.FindKernel("DownscaleDepth");

            m_ReprojectCloudsKernel = m_VolumetricCloudsCS.FindKernel("ReprojectClouds");
            m_ReprojectCloudsRejectionKernel = m_VolumetricCloudsCS.FindKernel("ReprojectCloudsRejection");
            m_PreUpscaleCloudsKernel = m_VolumetricCloudsCS.FindKernel("PreUpscaleClouds");
            m_PreUpscaleCloudsSkyKernel = m_VolumetricCloudsCS.FindKernel("PreUpscaleCloudsSky");

            m_UpscaleCloudsKernel = m_VolumetricCloudsCS.FindKernel("UpscaleClouds");
            m_UpscaleCloudsPerceptualKernel = m_VolumetricCloudsCS.FindKernel("UpscaleCloudsPerceptual");
            m_UpscaleCloudsSkyKernel = m_VolumetricCloudsCS.FindKernel("UpscaleCloudsSky");

            m_CombineCloudsKernel = m_VolumetricCloudsCS.FindKernel("CombineClouds");
            m_CombineCloudsPerceptualKernel = m_VolumetricCloudsCS.FindKernel("CombineCloudsPerceptual");
            m_CombineCloudsSkyKernel = m_VolumetricCloudsCS.FindKernel("CombineCloudsSky");

            // Create the material needed for the combination
            m_CloudCombinePass = CoreUtils.CreateEngineMaterial(defaultResources.shaders.volumetricCloudsCombinePS);

            m_VolumetricCloudsTraceCS = m_Asset.renderPipelineResources.shaders.volumetricCloudsTraceCS;
            m_CloudRenderKernel = m_VolumetricCloudsTraceCS.FindKernel("RenderClouds");

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

        float EvaluateCloudsEarthRadius(VolumetricClouds settings)
        {
            return Mathf.Lerp(1.0f, 0.025f, settings.earthCurvature.value) * k_EarthRadius;
        }

        float2 EvaluateCloudsLowestAltitude(VolumetricClouds settings)
        {
            float2 cloudAltitude = new float2(0.0f, 0.0f);
            cloudAltitude.x = settings.bottomAltitude.value;
            // When in non local mode, the camera is supposed to be always strictly under the clouds
            // to avoid artifacts due to precision issues, when in non local, the clouds are always 1 meter above the camera.
            if (!settings.localClouds.value)
                cloudAltitude.x = Mathf.Max(cloudAltitude.x, 1.0f);
            cloudAltitude.y = cloudAltitude.x + settings.altitudeRange.value;
            return cloudAltitude;
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
            // Planet properties
            float2 cloudsAltitude = EvaluateCloudsLowestAltitude(settings);
            cb._LowestCloudAltitude = cloudsAltitude.x;
            cb._HighestCloudAltitude = cloudsAltitude.y;
            cb._EarthRadius = EvaluateCloudsEarthRadius(settings);
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
                // m_CurrentSunLightDataIndex is supposed to be guaranteed to be non -1 if the current sun is not null
                cb._SunLightColor = m_GpuLightsBuilder.directionalLights[m_CurrentSunLightDataIndex].color * settings.sunLightDimmer.value * additionalLightData.lightDimmer;
                cb._SunDirection = -currentSun.transform.forward;
            }
            else
            {
                cb._SunLightColor = Vector3.zero;
                cb._SunDirection = Vector3.up;
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
            commonData.volumetricCloudsCS = m_VolumetricCloudsCS;
            commonData.volumetricCloudsTraceCS = m_VolumetricCloudsTraceCS;
            
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
            // don't update cloud animation for anything but the main camera
            if (GetCameraType(hdCamera) != TVolumetricCloudsCameraType.Default)
                return;

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
            public bool perPixelSorting;

            // Input buffers
            public TextureHandle volumetricCloudsLightingTexture;
            public TextureHandle volumetricCloudsDepthTexture;
            public TextureHandle depthAndStencil;

            public BufferHandle waterLine;
            public BufferHandle cameraHeightBuffer;
            public BufferHandle waterSurfaceProfiles;
            public TextureHandle waterGBuffer3;
        }

        void CombineVolumetricClouds(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle resolvedDepthBuffer, in TransparentPrepassOutput transparentPrepass)
        {
            if (!transparentPrepass.clouds.valid)
                return;

            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsCombineOpaqueData>("Volumetric Clouds Combine", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricCloudsCombine)))
            {
                // Parameters
                passData.cloudsCombineMaterial = m_CloudCombinePass;
                passData.perPixelSorting = transparentPrepass.enablePerPixelSorting;

                // Input buffers
                passData.volumetricCloudsLightingTexture = builder.ReadTexture(transparentPrepass.clouds.lightingBuffer);

                if (passData.perPixelSorting)
                {
                    passData.volumetricCloudsDepthTexture = builder.ReadTexture(transparentPrepass.clouds.depthBuffer);
                    passData.depthAndStencil = builder.ReadTexture(resolvedDepthBuffer);

                    passData.waterLine = builder.ReadBuffer(transparentPrepass.waterLine);
                    passData.cameraHeightBuffer = builder.ReadBuffer(transparentPrepass.waterGBuffer.cameraHeight);
                    passData.waterSurfaceProfiles = builder.ReadBuffer(transparentPrepass.waterSurfaceProfiles);
                    passData.waterGBuffer3 = builder.ReadTexture(transparentPrepass.waterGBuffer.waterGBuffer3);
                }

                // Output buffers
                builder.UseColorBuffer(colorBuffer, 0);

                if (passData.perPixelSorting)
                {
                    builder.UseDepthBuffer(transparentPrepass.beforeRefraction, DepthAccess.Read); // Dummy buffer to avoid 'Setting MRT without a depth buffer is not supported'
                    builder.UseColorBuffer(transparentPrepass.beforeRefraction, 1);
                    builder.UseColorBuffer(transparentPrepass.beforeRefractionAlpha, 2);
                }

                builder.SetRenderFunc(
                    (VolumetricCloudsCombineOpaqueData data, RenderGraphContext ctx) =>
                    {
                        data.cloudsCombineMaterial.SetTexture(HDShaderIDs._VolumetricCloudsLightingTexture, data.volumetricCloudsLightingTexture);

                        if (data.perPixelSorting)
                        {
                            data.cloudsCombineMaterial.SetTexture(HDShaderIDs._RefractiveDepthBuffer, data.depthAndStencil, RenderTextureSubElement.Depth);
                            data.cloudsCombineMaterial.SetTexture(HDShaderIDs._StencilTexture, data.depthAndStencil, RenderTextureSubElement.Stencil);
                            data.cloudsCombineMaterial.SetTexture(HDShaderIDs._VolumetricCloudsDepthTexture, data.volumetricCloudsDepthTexture);
                            data.cloudsCombineMaterial.SetBuffer(HDShaderIDs._WaterCameraHeightBuffer, data.cameraHeightBuffer);
                            data.cloudsCombineMaterial.SetBuffer(HDShaderIDs._WaterSurfaceProfiles, data.waterSurfaceProfiles);
                            data.cloudsCombineMaterial.SetTexture(HDShaderIDs._WaterGBufferTexture3, data.waterGBuffer3);
                            data.cloudsCombineMaterial.SetBuffer(HDShaderIDs._WaterLineBuffer, data.waterLine);
                        }

                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.cloudsCombineMaterial, data.perPixelSorting ? 7 : 0, MeshTopology.Triangles, 3);
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

        internal struct VolumetricCloudsOutput
        {
            public TextureHandle lightingBuffer;
            public TextureHandle depthBuffer;
            public bool valid;
        }

        void RenderVolumetricClouds(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthPyramid, TextureHandle motionVector, TextureHandle volumetricLighting, TextureHandle maxZMask, ref TransparentPrepassOutput transparentPrepass)
        {
            // If the current volume does not enable the feature, quit right away.
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();
            bool skipCloudRendering = m_CurrentDebugDisplaySettings.DebugHideVolumetricClouds(hdCamera) || !HasVolumetricClouds(hdCamera, in settings);
#if UNITY_EDITOR
            skipCloudRendering |= !hdCamera.camera.renderCloudsInSceneView;
#endif
            if (skipCloudRendering)
            {
                transparentPrepass.clouds = new VolumetricCloudsOutput()
                {
                    lightingBuffer = renderGraph.defaultResources.whiteTextureXR,
                    depthBuffer = renderGraph.defaultResources.blackTextureXR,
                    valid = false,
                };
                return;
            }

            // Make sure the volumetric clouds are animated properly
            UpdateVolumetricClouds(hdCamera, in settings);

            // Evaluate which version of the clouds we should be using
            TVolumetricCloudsCameraType cameraType = GetCameraType(hdCamera);
            bool accumulationClouds = cameraType == TVolumetricCloudsCameraType.Default || cameraType == TVolumetricCloudsCameraType.PlanarReflection;
            bool fullResolutionClouds = cameraType == TVolumetricCloudsCameraType.BakedReflection;

            // Render the clouds
            if (accumulationClouds)
                transparentPrepass.clouds = RenderVolumetricClouds_Accumulation(renderGraph, hdCamera, cameraType, colorBuffer, depthPyramid, motionVector, volumetricLighting, maxZMask);
            else if (fullResolutionClouds)
                transparentPrepass.clouds = RenderVolumetricClouds_FullResolution(renderGraph, hdCamera, cameraType, colorBuffer, depthPyramid, motionVector, volumetricLighting, maxZMask);
            else
                transparentPrepass.clouds = RenderVolumetricClouds_LowResolution(renderGraph, hdCamera, cameraType, colorBuffer, depthPyramid, motionVector, volumetricLighting, maxZMask);

            // Push the texture to the debug menu
            if (m_CurrentDebugDisplaySettings.data.volumetricCloudDebug == VolumetricCloudsDebug.Lighting)
                PushFullScreenDebugTexture(m_RenderGraph, transparentPrepass.clouds.lightingBuffer, FullScreenDebugMode.VolumetricClouds);
            else
                PushFullScreenDebugTexture(m_RenderGraph, transparentPrepass.clouds.depthBuffer, FullScreenDebugMode.VolumetricClouds, GraphicsFormat.R32_SFloat);
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

            // Evaluate the cloud map
            PreRenderVolumetricCloudMap(renderGraph, hdCamera, in settings);
            
            // Render the cloud shadows
            RenderVolumetricCloudsShadows(renderGraph, hdCamera, in settings);
        }
    }
}
