using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

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
        int m_CloudDownscaleDepthKernel;

        // Cloud rendering kernels
        int m_CloudRenderKernel;

        // Reprojection / First upscale
        int m_ReprojectCloudsKernel;
        int m_ReprojectCloudsRejectionKernel;
        int m_PreUpscaleCloudsKernel;

        // Second Upscale + Fog
        int m_UpscaleCloudsKernel;
        int m_UpscaleCloudsPerceptualKernel;

        // Fog only
        int m_CombineCloudsKernel;
        int m_CombineCloudsPerceptualKernel;

        // Flag that allows us to track the resources that habe been allocated
        bool m_ActiveVolumetricClouds;

        // Combine pass via hardware blending.
        Material m_CloudCombinePass;

        // The animation time is shared for all cameras
        internal float m_VolumetricCloudsTime;

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
            public bool maxZMaskValidity;
        }

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
            m_VolumetricCloudsCS = runtimeShaders.volumetricCloudsCS;
            m_CloudDownscaleDepthKernel = m_VolumetricCloudsCS.FindKernel("DownscaleDepth");

            m_ReprojectCloudsKernel = m_VolumetricCloudsCS.FindKernel("ReprojectClouds");
            m_ReprojectCloudsRejectionKernel = m_VolumetricCloudsCS.FindKernel("ReprojectCloudsRejection");
            m_PreUpscaleCloudsKernel = m_VolumetricCloudsCS.FindKernel("PreUpscaleClouds");

            m_UpscaleCloudsKernel = m_VolumetricCloudsCS.FindKernel("UpscaleClouds");
            m_UpscaleCloudsPerceptualKernel = m_VolumetricCloudsCS.FindKernel("UpscaleCloudsPerceptual");

            m_CombineCloudsKernel = m_VolumetricCloudsCS.FindKernel("CombineClouds");
            m_CombineCloudsPerceptualKernel = m_VolumetricCloudsCS.FindKernel("CombineCloudsPerceptual");

            // Create the material needed for the combination
            m_CloudCombinePass = CoreUtils.CreateEngineMaterial(runtimeShaders.volumetricCloudsCombinePS);

            m_VolumetricCloudsTraceCS = runtimeShaders.volumetricCloudsTraceCS;
            m_CloudRenderKernel = m_VolumetricCloudsTraceCS.FindKernel("RenderClouds");

            // Allocate all the texture initially
            AllocatePresetTextures();

            // Initialize cloud animation
            m_VolumetricCloudsTime = -1.0f;

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
            // TODO: figure out what this function does
            const float k_EarthRadius = 6378100.0f;
            return Mathf.Sqrt((k_EarthRadius + lowerCloudRadius) * (k_EarthRadius + lowerCloudRadius) - k_EarthRadius * earthRadius);
        }

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
            Sky
        };

        TVolumetricCloudsCameraType GetCameraType(HDCamera hdCamera)
        {
            if (hdCamera.camera.cameraType == CameraType.Reflection)
            {
                if (hdCamera.realtimeReflectionProbe)
                    return TVolumetricCloudsCameraType.RealtimeReflection;
                else
                    return TVolumetricCloudsCameraType.BakedReflection;
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

        void UpdateShaderVariablesClouds(ref ShaderVariablesClouds cb, HDCamera hdCamera, VolumetricClouds settings,
            in VolumetricCloudsCameraData cameraData, in CloudModelData cloudModelData, bool shadowPass)
        {
            // Planet properties
            cb._LowestCloudAltitude = hdCamera.planet.radius + settings.bottomAltitude.value;
            cb._HighestCloudAltitude = cb._LowestCloudAltitude + settings.altitudeRange.value;

            cb._NumPrimarySteps = settings.numPrimarySteps.value;
            cb._NumLightSteps = settings.numLightSteps.value;

            cb._CloudMapTiling.Set(settings.cloudTiling.value.x, settings.cloudTiling.value.y, settings.cloudOffset.value.x, settings.cloudOffset.value.y);

            cb._ScatteringTint = Color.white - settings.scatteringTint.value * 0.75f;
            cb._PowderEffectIntensity = settings.powderEffectIntensity.value;
            cb._NormalizationFactor = ComputeNormalizationFactor(hdCamera.planet.radius, (cb._LowestCloudAltitude + cb._HighestCloudAltitude) * 0.5f - hdCamera.planet.radius);

            // We need 16 samples per pixel and we are alternating between 4 pixels (16 x 4 = 64)
            int frameIndex = RayTracingFrameIndex(hdCamera, 64);
            cb._AccumulationFrameIndex = frameIndex / 4;
            cb._SubPixelIndex = frameIndex % 4;

            // PB Sun/Sky settings
            Light currentSun = GetMainLight();
            HDAdditionalLightData additionalLightData = null;
            if (currentSun != null)
            {
                // Grab the target sun additional data
                m_CurrentSunLight.TryGetComponent<HDAdditionalLightData>(out additionalLightData);
                // m_CurrentSunLightDataIndex is supposed to be guaranteed to be non -1 if the current sun is not null
                cb._SunLightColor = m_GpuLightsBuilder.directionalLights[m_CurrentSunLightDataIndex].color * settings.sunLightDimmer.value;
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
            cb._WindVector = hdCamera.volumetricCloudsAnimationData.cloudOffset;

            cb._VerticalShapeWindDisplacement = hdCamera.volumetricCloudsAnimationData.verticalShapeOffset;
            cb._VerticalErosionWindDisplacement = hdCamera.volumetricCloudsAnimationData.verticalErosionOffset;

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

            cb._ErosionOcclusion = settings.erosionOcclusion.value;
            cb._ErosionFactorCompensation = ErosionNoiseTypeToErosionCompensation(settings.erosionNoiseType.value);

            // Correct prev view proj matrix for local mode
            cb._CameraPrevViewProjection = hdCamera.mainViewConstants.prevViewProjMatrix;
            if (hdCamera.planet.renderingSpace == RenderingSpace.Camera)
                cb._CameraPrevViewProjection *= Matrix4x4.Translate(new Vector3(0.0f, hdCamera.mainViewConstants.prevWorldSpaceCameraPos.y, 0.0f));

            cb._CloudsPixelCoordToViewDirWS = hdCamera.mainViewConstants.pixelCoordToViewDirWS;

            Vector3 cameraPosPS = hdCamera.mainViewConstants.worldSpaceCameraPos - hdCamera.planet.center;
            Vector3 prevCameraPosPS = cameraPosPS + hdCamera.mainViewConstants.prevWorldSpaceCameraPos; // prev pos is camera relative

            float previousNearPlane = math.max(GetCloudNearPlane(prevCameraPosPS, cb._LowestCloudAltitude, cb._HighestCloudAltitude), hdCamera.camera.nearClipPlane);
            cb._CloudNearPlane = math.max(GetCloudNearPlane(cameraPosPS, cb._LowestCloudAltitude, cb._HighestCloudAltitude), hdCamera.camera.nearClipPlane);
            cb._NearPlaneReprojection = cb._CloudNearPlane / previousNearPlane;

            cb._EnableFastToneMapping = cameraData.enableExposureControl ? 1 : 0;

            cb._LowResolutionEvaluation = cameraData.lowResolution ? 1 : 0;
            cb._EnableIntegration = cameraData.enableIntegration ? 1 : 0;
            cb._CameraSpace = hdCamera.planet.renderingSpace == RenderingSpace.Camera ? 1 : 0;
            cb._ValidSceneDepth = cameraData.cameraType != TVolumetricCloudsCameraType.Sky ? 1 : 0;
            cb._ValidMaxZMask = cameraData.maxZMaskValidity ? 1 : 0;

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
            public bool microErosion;
            public bool simplePreset;
            public bool pbrSkyActive;
            public bool traceForSky;

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
            public int renderKernel;

            // Cloud constant buffer buffer
            public ShaderVariablesClouds cloudsCB;
        }

        Texture3D ErosionNoiseTypeToTexture(VolumetricClouds.CloudErosionNoise noiseType)
        {
            switch (noiseType)
            {
                case VolumetricClouds.CloudErosionNoise.Worley32:
                    return runtimeTextures.worleyNoise32RGB;
                case VolumetricClouds.CloudErosionNoise.Perlin32:
                    return runtimeTextures.perlinNoise32RGB;
            }
            return runtimeTextures.worleyNoise32RGB;
        }

        void FillVolumetricCloudsCommonData(HDCamera hdCamera, bool enableExposureControl, VolumetricClouds settings, TVolumetricCloudsCameraType cameraType, in CloudModelData cloudModelData, ref VolumetricCloudCommonData commonData)
        {
            commonData.cameraType = cameraType;
            commonData.volumetricCloudsCS = m_VolumetricCloudsCS;
            commonData.volumetricCloudsTraceCS = m_VolumetricCloudsTraceCS;
            commonData.renderKernel = m_CloudRenderKernel;
            commonData.pbrSkyActive = hdCamera.volumeStack.GetComponent<VisualEnvironment>().skyType.value == (int)SkyType.PhysicallyBased;
            commonData.traceForSky = cameraType == TVolumetricCloudsCameraType.Sky;

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
                commonData.cloudLutTexture = runtimeTextures.cloudLutRainAO;
                commonData.microErosion = settings.microErosion.value;
            }
            else
            {
                commonData.cloudMapTexture = settings.cloudMap.value != null ? settings.cloudMap.value : Texture2D.blackTexture;
                commonData.cloudLutTexture = settings.cloudLut.value != null ? settings.cloudLut.value : Texture2D.blackTexture;
                commonData.microErosion = settings.microErosion.value;
            }

            commonData.worley128RGBA = runtimeTextures.worleyNoise128RGBA;
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

            // Animation time should be independent from camera
            float totalTime = Time.realtimeSinceStartup;

            // The system needs to updated if this is not the first frame and the previous frame history is valid
            if (m_VolumetricCloudsTime != -1.0f && EvaluateVolumetricCloudsHistoryValidity(hdCamera))
            {
                // Compute the delta time
                float deltaTime = totalTime - m_VolumetricCloudsTime;

                #if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPaused)
                    deltaTime = 0.0f;
                #endif

                // Compute the theta angle for the wind direction
                float theta = settings.orientation.GetValue(hdCamera) / 180.0f * Mathf.PI;

                // Compute the wind direction
                Vector2 windDirection = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));

                // Conversion  from km/h to m/s  is the 0.277778f factor
                // We apply a minus to see something moving in the right direction
                Vector2 windVector = 0.277778f * deltaTime * settings.globalWindSpeed.GetValue(hdCamera) * -windDirection;

                // Animate the offsets
                hdCamera.volumetricCloudsAnimationData.cloudOffset += windVector;
                hdCamera.volumetricCloudsAnimationData.verticalShapeOffset += -settings.verticalShapeWindSpeed.value * deltaTime * 0.277778f;
                hdCamera.volumetricCloudsAnimationData.verticalErosionOffset += -settings.verticalErosionWindSpeed.value * deltaTime * 0.277778f;
            }

            // Update the time
            m_VolumetricCloudsTime = totalTime;
        }

        class VolumetricCloudsCombineOpaqueData
        {
            // Material
            public Material cloudsCombineMaterial;
            public bool perPixelSorting;
            public Matrix4x4 pixelCoordToViewDir;

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
                passData.pixelCoordToViewDir = hdCamera.mainViewConstants.pixelCoordToViewDirWS;

                // Input buffers
                passData.volumetricCloudsLightingTexture = builder.ReadTexture(transparentPrepass.clouds.lightingBuffer);
                passData.volumetricCloudsDepthTexture = builder.ReadTexture(transparentPrepass.clouds.depthBuffer);

                if (passData.perPixelSorting)
                {
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
                        data.cloudsCombineMaterial.SetTexture(HDShaderIDs._VolumetricCloudsDepthTexture, data.volumetricCloudsDepthTexture);
                        data.cloudsCombineMaterial.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, data.pixelCoordToViewDir);

                        if (data.perPixelSorting)
                        {
                            data.cloudsCombineMaterial.SetTexture(HDShaderIDs._RefractiveDepthBuffer, data.depthAndStencil, RenderTextureSubElement.Depth);
                            data.cloudsCombineMaterial.SetTexture(HDShaderIDs._StencilTexture, data.depthAndStencil, RenderTextureSubElement.Stencil);
                            data.cloudsCombineMaterial.SetBuffer(HDShaderIDs._WaterCameraHeightBuffer, data.cameraHeightBuffer);
                            data.cloudsCombineMaterial.SetBuffer(HDShaderIDs._WaterSurfaceProfiles, data.waterSurfaceProfiles);
                            data.cloudsCombineMaterial.SetTexture(HDShaderIDs._WaterGBufferTexture3, data.waterGBuffer3);
                            data.cloudsCombineMaterial.SetBuffer(HDShaderIDs._WaterLineBuffer, data.waterLine);
                        }

                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.cloudsCombineMaterial, data.perPixelSorting ? 7 : 0, MeshTopology.Triangles, 3);
                    });
            }
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
            bool accumulationClouds = cameraType == TVolumetricCloudsCameraType.Default;
            bool fullResolutionClouds = cameraType == TVolumetricCloudsCameraType.BakedReflection;

            // Render the clouds
            if (accumulationClouds)
                transparentPrepass.clouds = RenderVolumetricClouds_Accumulation(renderGraph, hdCamera, cameraType, colorBuffer, depthPyramid, motionVector, volumetricLighting, maxZMask);
            else if (fullResolutionClouds)
                transparentPrepass.clouds = RenderVolumetricClouds_FullResolution(renderGraph, hdCamera, cameraType, colorBuffer, depthPyramid, motionVector, volumetricLighting, maxZMask);
            else // realtime reflection
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

        static void DoVolumetricCloudsPrepare(CommandBuffer cmd, int kernel, int traceTX, int traceTY, int viewCount, in VolumetricCloudCommonData commonData,
            RTHandle depthPyramid, RTHandle halfResDepthBuffer)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsPrepare)))
            {
                // Compute the alternative version of the mip 1 of the depth (min instead of max that is required to handle high frequency meshes (vegetation, hair)
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._DepthTexture, depthPyramid);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._HalfResDepthBufferRW, halfResDepthBuffer);
                cmd.DispatchCompute(commonData.volumetricCloudsCS, kernel, traceTX, traceTY, viewCount);
            }
        }

        static void DoVolumetricCloudsTrace(CommandBuffer cmd, int traceTX, int traceTY, int viewCount, in VolumetricCloudCommonData commonData,
            RTHandle maxZMask, RTHandle volumetricLightingTexture, RTHandle sceneDepth, GraphicsBuffer ambientProbe,
            RTHandle cloudsLightingOutput, RTHandle cloudsDepthOutput)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsTrace)))
            {
                CoreUtils.SetKeyword(cmd, "CLOUDS_SIMPLE_PRESET", commonData.simplePreset);
                CoreUtils.SetKeyword(cmd, "CLOUDS_MICRO_EROSION", commonData.microErosion);
                CoreUtils.SetKeyword(cmd, "PHYSICALLY_BASED_SUN", commonData.pbrSkyActive);
                CoreUtils.SetKeyword(cmd, "TRACE_FOR_SKY", commonData.traceForSky);

                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._MaxZMaskTexture, maxZMask);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._VBufferLighting, volumetricLightingTexture);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._VolumetricCloudsSourceDepth, sceneDepth);

                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._Worley128RGBA, commonData.worley128RGBA);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._ErosionNoise, commonData.erosionNoise);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._CloudMapTexture, commonData.cloudMapTexture);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._CloudLutTexture, commonData.cloudLutTexture);
                cmd.SetComputeBufferParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._VolumetricCloudsAmbientProbeBuffer, ambientProbe);

                // Output buffers
                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._CloudsLightingTextureRW, cloudsLightingOutput);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._CloudsDepthTextureRW, cloudsDepthOutput);

                cmd.DispatchCompute(commonData.volumetricCloudsTraceCS, commonData.renderKernel, traceTX, traceTY, viewCount);
            }
        }

        static void DoVolumetricCloudsReproject(CommandBuffer cmd, int kernel, int traceTX, int traceTY, int viewCount, in VolumetricCloudCommonData commonData,
            RTHandle halfResCloudsLighting, RTHandle halfResCloudsDepth, RTHandle halfResDepthBuffer,
            bool withHistory, bool clearHistory, RTHandle previousHistory0Buffer, RTHandle previousHistory1Buffer,
            RTHandle lightingOutput, RTHandle additionalOutput)
        {
            var marker = withHistory ? HDProfileId.VolumetricCloudsReproject : HDProfileId.VolumetricCloudsPreUpscale;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(marker)))
            {
                if (withHistory)
                {
                    if (clearHistory)
                    {
                        CoreUtils.SetRenderTarget(cmd, previousHistory0Buffer, clearFlag: ClearFlag.Color, clearColor: Color.black);
                        CoreUtils.SetRenderTarget(cmd, previousHistory1Buffer, clearFlag: ClearFlag.Color, clearColor: Color.black);
                    }

                    // History buffers
                    cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._HistoryVolumetricClouds0Texture, previousHistory0Buffer);
                    cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._HistoryVolumetricClouds1Texture, previousHistory1Buffer);
                }

                // Re-project the result from the previous frame
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._CloudsLightingTexture, halfResCloudsLighting);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._CloudsDepthTexture, halfResCloudsDepth);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._HalfResDepthBuffer, halfResDepthBuffer);

                // Output textures
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._CloudsLightingTextureRW, lightingOutput);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._CloudsAdditionalTextureRW, additionalOutput);

                // Re-project from the previous frame
                cmd.DispatchCompute(commonData.volumetricCloudsCS, kernel, traceTX, traceTY, viewCount);
            }
        }

        static void DoVolumetricCloudsUpscale(CommandBuffer cmd, int kernel, int traceTX, int traceTY, int viewCount, in VolumetricCloudCommonData commonData,
            RTHandle currentHistory0Buffer, RTHandle currentHistory1Buffer, RTHandle colorBuffer, RTHandle currentDepthBuffer,
            RTHandle cloudsLighting, RTHandle cloudsDepth)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsUpscale)))
            {
                // Compute the final resolution parameters
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._VolumetricCloudsTexture, currentHistory0Buffer);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._DepthStatusTexture, currentHistory1Buffer);

                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._CameraColorTexture, colorBuffer);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._DepthTexture, currentDepthBuffer);

                // Output clouds texture (scattering + transmittance)
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._VolumetricCloudsLightingTextureRW, cloudsLighting);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._VolumetricCloudsDepthTextureRW, cloudsDepth);
                cmd.DispatchCompute(commonData.volumetricCloudsCS, kernel, traceTX, traceTY, viewCount);
            }
        }

        static float GetCloudNearPlane(float3 originPS, float lowerBoundPS, float higherBoundPS)
        {
            float radialDistance = math.length(originPS);
            float rcpRadialDistance = math.rcp(radialDistance);
            float cosChi = 1.0f;
            Vector2 tInner = PhysicallyBasedSky.IntersectSphere(lowerBoundPS, cosChi, radialDistance, rcpRadialDistance);
            Vector2 tOuter = PhysicallyBasedSky.IntersectSphere(higherBoundPS, -cosChi, radialDistance, rcpRadialDistance);

            if (tInner.x < 0.0 && tInner.y >= 0.0) // Below the lower bound
                return tInner.y;
            else // Inside or above the cloud volume
                return math.max(tOuter.x, 0.0f);
        }
    }
}
