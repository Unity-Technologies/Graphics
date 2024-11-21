using System;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class VolumetricCloudsSystem
    {
        // Flag that allows us to track the resources that have been allocated
        bool m_ActiveVolumetricClouds = false;
        HDRenderPipeline m_RenderPipeline;
        VolumetricCloudsRuntimeResources m_RuntimeResources;

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

        // Combine pass via hardware blending.
        Material m_CloudCombinePass;

        LocalKeyword m_OutputFogTransmittanceKeyword;

        float m_CloudsAnimationLastTime;
        internal VolumetricClouds.AnimationData m_CloudsAnimationData;

        struct VolumetricCloudsCameraData
        {
            public TVolumetricCloudsCameraType cameraType;
            public int traceWidth;
            public int traceHeight;
            public int intermediateWidth;
            public int intermediateHeight;
            public int finalWidth;
            public int finalHeight;
            public bool enableExposureControl;
            public bool lowResolution;
            public bool enableIntegration;
        }

        public void Initialize(HDRenderPipeline hdPipeline)
        {
            m_RenderPipeline = hdPipeline;
            m_ActiveVolumetricClouds = hdPipeline.asset.currentPlatformRenderPipelineSettings.supportVolumetricClouds;
            if (!m_ActiveVolumetricClouds)
                return;

            m_RuntimeResources = GraphicsSettings.GetRenderPipelineSettings<VolumetricCloudsRuntimeResources>();

            // Allocate the buffers for ambient probe evaluation
            m_PhaseZHClouds = new ZonalHarmonicsL2();
            m_PhaseZHClouds.coeffs = new float[3];

            // Grab the kernels we need
            m_VolumetricCloudsCS = m_RuntimeResources.volumetricCloudsCS;

            m_ReprojectCloudsKernel = m_VolumetricCloudsCS.FindKernel("ReprojectClouds");
            m_ReprojectCloudsRejectionKernel = m_VolumetricCloudsCS.FindKernel("ReprojectCloudsRejection");
            m_PreUpscaleCloudsKernel = m_VolumetricCloudsCS.FindKernel("PreUpscaleClouds");

            m_UpscaleCloudsKernel = m_VolumetricCloudsCS.FindKernel("UpscaleClouds");
            m_UpscaleCloudsPerceptualKernel = m_VolumetricCloudsCS.FindKernel("UpscaleCloudsPerceptual");

            m_CombineCloudsKernel = m_VolumetricCloudsCS.FindKernel("CombineClouds");
            m_CombineCloudsPerceptualKernel = m_VolumetricCloudsCS.FindKernel("CombineCloudsPerceptual");

            // Create the material needed for the combination
            m_CloudCombinePass = CoreUtils.CreateEngineMaterial(m_RuntimeResources.volumetricCloudsCombinePS);

            m_OutputFogTransmittanceKeyword = new LocalKeyword(m_CloudCombinePass.shader, "OUTPUT_TRANSMITTANCE_BUFFER");

            m_VolumetricCloudsTraceCS = m_RuntimeResources.volumetricCloudsTraceCS;
            m_CloudRenderKernel = m_VolumetricCloudsTraceCS.FindKernel("RenderClouds");

            // Allocate all the texture initially
            AllocatePresetTextures();

            // Initialize cloud animation
            m_CloudsAnimationLastTime = -1.0f;
            m_CloudsAnimationData = new()
            {
                cloudOffset = new Vector2(0.0f, 0.0f),
                verticalShapeOffset = 0.0f,
                verticalErosionOffset = 0.0f,
            };

            // Initialize the additional sub components
            InitializeVolumetricCloudsMap();
            InitializeVolumetricCloudsShadows();
            InitializeVolumetricCloudsAmbientProbe();
        }

        public void Cleanup()
        {
            if (!m_ActiveVolumetricClouds)
                return;

            // Destroy allocated resources
            CoreUtils.Destroy(m_CloudCombinePass);
            CoreUtils.Destroy(m_CustomPresetMap);
            CoreUtils.Destroy(m_CustomLutPresetMap);

            // Release the additional sub components
            ReleaseVolumetricCloudsMap();
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
        static internal bool HasVolumetricClouds(HDCamera hdCamera, in VolumetricClouds settings)
        {
            // If the current volume does not enable the feature, quit right away.
            return hdCamera.frameSettings.IsEnabled(FrameSettingsField.VolumetricClouds) && settings.enable.value;
        }

        static internal bool HasVolumetricClouds(HDCamera hdCamera)
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
            cb._MaxStepSize = settings.altitudeRange.value / 8.0f;

            cb._CloudMapTiling.Set(settings.cloudTiling.value.x, settings.cloudTiling.value.y, settings.cloudOffset.value.x, settings.cloudOffset.value.y);

            cb._ScatteringTint = Color.white - settings.scatteringTint.value * 0.75f;
            cb._PowderEffectIntensity = settings.powderEffectIntensity.value;
            cb._NormalizationFactor = ComputeNormalizationFactor(hdCamera.planet.radius, (cb._LowestCloudAltitude + cb._HighestCloudAltitude) * 0.5f - hdCamera.planet.radius);

            // We need 16 samples per pixel and we are alternating between 4 pixels (16 x 4 = 64)
            int frameIndex = HDRenderPipeline.RayTracingFrameIndex(hdCamera, 64);
            cb._AccumulationFrameIndex = frameIndex / 4;
            cb._SubPixelIndex = frameIndex % 4;

            // PB Sun/Sky settings
            Light currentSun = m_RenderPipeline.GetMainLight();
            HDAdditionalLightData additionalLightData = null;
            if (currentSun != null)
            {
                // Grab the target sun additional data
                additionalLightData = m_RenderPipeline.GetMainLightAdditionalData();
                cb._SunLightColor = additionalLightData.EvaluateLightColor() * settings.sunLightDimmer.value * additionalLightData.lightDimmer;
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
            cb._WindVector = m_CloudsAnimationData.cloudOffset;

            cb._VerticalShapeWindDisplacement = m_CloudsAnimationData.verticalShapeOffset;
            cb._VerticalErosionWindDisplacement = m_CloudsAnimationData.verticalErosionOffset;

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
            cb._ImprovedTransmittanceBlend = settings.perceptualBlending.value;

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

            if (!shadowPass)
                UpdateMatricesForXR(ref cb, hdCamera);

            Vector3 cameraPosPS = hdCamera.mainViewConstants.worldSpaceCameraPos - hdCamera.planet.center;
            Vector3 prevCameraPosPS = cameraPosPS + hdCamera.mainViewConstants.prevWorldSpaceCameraPos; // prev pos is camera relative

            float previousNearPlane = math.max(GetCloudNearPlane(prevCameraPosPS, cb._LowestCloudAltitude, cb._HighestCloudAltitude), hdCamera.camera.nearClipPlane);
            cb._CloudNearPlane = math.max(GetCloudNearPlane(cameraPosPS, cb._LowestCloudAltitude, cb._HighestCloudAltitude), hdCamera.camera.nearClipPlane);
            cb._NearPlaneReprojection = cb._CloudNearPlane / previousNearPlane;

            cb._EnableFastToneMapping = cameraData.enableExposureControl ? 1 : 0;

            bool quarterRes = cameraData.intermediateWidth != cameraData.finalWidth;
            cb._LowResolutionEvaluation = cameraData.lowResolution ? 1 : 0;
            cb._EnableIntegration = cameraData.enableIntegration ? 1 : 0;
            cb._CameraSpace = hdCamera.planet.renderingSpace == RenderingSpace.Camera ? 1 : 0;
            cb._ValidSceneDepth = cameraData.cameraType != TVolumetricCloudsCameraType.Sky ? 1 : 0;
            cb._IntermediateResolutionScale = cameraData.intermediateWidth == cameraData.finalWidth ? 1u : 2u;
            cb._ReprojDepthMipOffset = hdCamera.depthBufferMipChainInfo.mipLevelOffsetsCheckerboard[quarterRes ? 1 : 0];

            unsafe
            {
                for (int p = 0; p < 4; ++p)
                    for (int i = 0; i < 9; ++i)
                        cb._DistanceBasedWeights[12 * p + i] = BilateralUpsample.distanceBasedWeights_3x3[9 * p + i];
            }
        }

        unsafe internal void UpdateMatricesForXR(ref ShaderVariablesClouds cb, HDCamera hdCamera)
        {
            for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
            {
                var vp = hdCamera.m_XRViewConstants[viewIndex].prevViewProjMatrix;

                // Correct prev view proj matrix for local mode
                if (hdCamera.planet.renderingSpace == RenderingSpace.Camera)
                    vp *= Matrix4x4.Translate(new Vector3(0.0f, hdCamera.m_XRViewConstants[viewIndex].prevWorldSpaceCameraPos.y, 0.0f));

                for (int j = 0; j < 16; ++j)
                {
                    cb._CloudsPixelCoordToViewDirWS[viewIndex * 16 + j] = hdCamera.m_XRViewConstants[viewIndex].pixelCoordToViewDirWS[j];
                    cb._CameraPrevViewProjection[viewIndex * 16 + j] = vp[j];
                }
            }
        }

        struct VolumetricCloudCommonData
        {
            // Resolution parameters
            public TVolumetricCloudsCameraType cameraType;
            public bool enableExposureControl;
            public bool perceptualBlending;
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
                    return m_RuntimeResources.worleyNoise32RGB;
                case VolumetricClouds.CloudErosionNoise.Perlin32:
                    return m_RuntimeResources.perlinNoise32RGB;
            }
            return m_RuntimeResources.worleyNoise32RGB;
        }

        void FillVolumetricCloudsCommonData(HDCamera hdCamera, bool enableExposureControl, VolumetricClouds settings, TVolumetricCloudsCameraType cameraType, in CloudModelData cloudModelData, ref VolumetricCloudCommonData commonData)
        {
            commonData.cameraType = cameraType;
            commonData.volumetricCloudsCS = m_VolumetricCloudsCS;
            commonData.volumetricCloudsTraceCS = m_VolumetricCloudsTraceCS;
            commonData.renderKernel = m_CloudRenderKernel;
            commonData.pbrSkyActive = hdCamera.volumeStack.GetComponent<VisualEnvironment>().skyType.value == (int)SkyType.PhysicallyBased;
            commonData.traceForSky = cameraType == TVolumetricCloudsCameraType.Sky;
            commonData.perceptualBlending = cameraType == TVolumetricCloudsCameraType.Default && !hdCamera.msaaEnabled && settings.perceptualBlending.value > 0.0f;

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
                commonData.cloudLutTexture = m_RuntimeResources.cloudLutRainAO;
                commonData.microErosion = settings.microErosion.value;
            }
            else
            {
                commonData.cloudMapTexture = settings.cloudMap.value != null ? settings.cloudMap.value : Texture2D.blackTexture;
                commonData.cloudLutTexture = settings.cloudLut.value != null ? settings.cloudLut.value : Texture2D.blackTexture;
                commonData.microErosion = settings.microErosion.value;
            }

            commonData.worley128RGBA = m_RuntimeResources.worleyNoise128RGBA;
            commonData.erosionNoise = ErosionNoiseTypeToTexture(cloudModelData.erosionNoise);
            commonData.ditheredTextureSet = m_RenderPipeline.GetBlueNoiseManager().DitheredTextureSet8SPP();
            commonData.sunLight = m_RenderPipeline.GetMainLight();
            commonData.enableExposureControl = enableExposureControl;
        }

        void UpdateVolumetricClouds(HDCamera hdCamera, in VolumetricClouds settings)
        {
            // don't update cloud animation for anything but the main camera
            if (GetCameraType(hdCamera) != TVolumetricCloudsCameraType.Default)
                return;

            // The system needs to updated if the previous frame history is valid
            if (EvaluateVolumetricCloudsHistoryValidity(hdCamera))
            {
                float totalTime = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
                float deltaTime = totalTime - m_CloudsAnimationLastTime;
                if (m_CloudsAnimationLastTime == -1.0f)
                    deltaTime = 0.0f;

                #if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPaused)
                    deltaTime = 0.0f;
                #endif

                // Conversion from km/h to m/s is the 0.277778f factor
                // We apply a minus to see something moving in the right direction
                deltaTime *= -0.277778f;

                // Compute the wind direction
                float theta = settings.orientation.GetValue(hdCamera) / 180.0f * Mathf.PI;
                Vector2 windDirection = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));

                // Animate the offsets
                m_CloudsAnimationLastTime = totalTime;
                m_CloudsAnimationData.cloudOffset += deltaTime * settings.globalWindSpeed.GetValue(hdCamera) * windDirection;
                m_CloudsAnimationData.verticalShapeOffset += deltaTime * settings.verticalShapeWindSpeed.value;
                m_CloudsAnimationData.verticalErosionOffset += deltaTime * settings.verticalErosionWindSpeed.value;
            }
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

            public bool needOpticalFogTransmittance;
            public LocalKeyword outputFogTransmittanceKeyword;
        }

        internal void CombineVolumetricClouds(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle resolvedDepthBuffer, in HDRenderPipeline.TransparentPrepassOutput transparentPrepass, ref TextureHandle opticalFogTransmittance)
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
                int opticalFogBufferIndex = 1;

                if (passData.perPixelSorting)
                {
                    builder.UseDepthBuffer(transparentPrepass.depthBufferPreRefraction, DepthAccess.Read); // Dummy buffer to avoid 'Setting MRT without a depth buffer is not supported'
                    builder.UseColorBuffer(transparentPrepass.beforeRefraction, 1);
                    builder.UseColorBuffer(transparentPrepass.beforeRefractionAlpha, 2);
                    opticalFogBufferIndex = 3;
                }

                passData.needOpticalFogTransmittance = LensFlareCommonSRP.IsCloudLayerOpacityNeeded(hdCamera.camera);
                passData.outputFogTransmittanceKeyword = m_OutputFogTransmittanceKeyword;

                if (passData.needOpticalFogTransmittance)
                {
                    if (!opticalFogTransmittance.IsValid())
                        opticalFogTransmittance = renderGraph.CreateTexture(HDRenderPipeline.GetOpticalFogTransmittanceDesc(hdCamera));
                    builder.UseDepthBuffer(transparentPrepass.depthBufferPreRefraction, DepthAccess.Read); // Dummy buffer to avoid 'Setting MRT without a depth buffer is not supported'
                    builder.UseColorBuffer(opticalFogTransmittance, opticalFogBufferIndex);
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

                        ctx.cmd.SetKeyword(data.cloudsCombineMaterial, data.outputFogTransmittanceKeyword, data.needOpticalFogTransmittance);

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

        internal void RenderVolumetricClouds(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthPyramid,
            ref HDRenderPipeline.TransparentPrepassOutput transparentPrepass, ref TextureHandle opticalFogTransmittance)
        {
            // If the current volume does not enable the feature, quit right away.
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();
            bool skipCloudRendering = m_RenderPipeline.m_CurrentDebugDisplaySettings.DebugHideVolumetricClouds(hdCamera) || !HasVolumetricClouds(hdCamera, in settings);
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
                transparentPrepass.clouds = RenderVolumetricClouds_Accumulation(renderGraph, hdCamera, cameraType, colorBuffer, depthPyramid);
            else if (fullResolutionClouds)
                transparentPrepass.clouds = RenderVolumetricClouds_FullResolution(renderGraph, hdCamera, cameraType, colorBuffer, depthPyramid);
            else // realtime reflection
                transparentPrepass.clouds = RenderVolumetricClouds_LowResolution(renderGraph, hdCamera, cameraType, colorBuffer, depthPyramid);

            // Push the texture to the debug menu
            if (m_RenderPipeline.m_CurrentDebugDisplaySettings.data.volumetricCloudDebug == VolumetricCloudsDebug.Lighting)
                m_RenderPipeline.PushFullScreenDebugTexture(renderGraph, transparentPrepass.clouds.lightingBuffer, FullScreenDebugMode.VolumetricClouds);
            else
                m_RenderPipeline.PushFullScreenDebugTexture(renderGraph, transparentPrepass.clouds.depthBuffer, FullScreenDebugMode.VolumetricClouds, GraphicsFormat.R32_SFloat);
        }

        internal void PreRenderVolumetricClouds(RenderGraph renderGraph, HDCamera hdCamera)
        {
            if (m_RenderPipeline.m_CurrentDebugDisplaySettings.DebugHideSky(hdCamera))
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

        GraphicsFormat GetCloudsColorFormat(VolumetricClouds settings, bool isHistoryBuffer)
        {
            // When neighborhood clamping is disabled, using R11G11B10 format for reprojection causes color shift
            if (!settings.ghostingReduction.value && isHistoryBuffer) return GraphicsFormat.R16G16B16A16_SFloat;
            return (GraphicsFormat)m_RenderPipeline.asset.currentPlatformRenderPipelineSettings.colorBufferFormat;
        }

        void CreateTracingTextures(RenderGraph renderGraph, RenderGraphBuilder builder, VolumetricClouds settings, float scale, out TextureHandle cloudsLighting, out TextureHandle cloudsDepth)
        {
            cloudsLighting = builder.CreateTransientTexture(new TextureDesc(Vector2.one * scale, true, true)
            { format = GetCloudsColorFormat(settings, false), enableRandomWrite = true, name = "Traced Clouds Lighting" });

            cloudsDepth = builder.CreateTransientTexture(new TextureDesc(Vector2.one * scale, true, true)
            { format = GraphicsFormat.R16G16_SFloat, enableRandomWrite = true, name = "Traced Clouds Depth" });
        }

        void CreateIntermediateTextures(RenderGraph renderGraph, RenderGraphBuilder builder, VolumetricClouds settings, out TextureHandle intermediate1, out TextureHandle intermediate2)
        {
            intermediate1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
            { format = GetCloudsColorFormat(settings, false), enableRandomWrite = true, name = "Temporary Clouds Lighting Buffer 1" });

            intermediate2 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
            { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporary Clouds Lighting Buffer 2" });
        }

        void CreateOutputTextures(RenderGraph renderGraph, RenderGraphBuilder builder, VolumetricClouds settings, out TextureHandle cloudsLighting, out TextureHandle cloudsDepth)
        {
            cloudsLighting = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { format = GetCloudsColorFormat(settings, false), enableRandomWrite = true, name = "Volumetric Clouds Lighting Texture" }));

            cloudsDepth = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { format = GraphicsFormat.R16G16_SFloat, enableRandomWrite = true, name = "Volumetric Clouds Depth Texture" }));
        }

        static void DoVolumetricCloudsTrace(CommandBuffer cmd, int traceTX, int traceTY, int viewCount, in VolumetricCloudCommonData commonData,
            GraphicsBuffer ambientProbe, RTHandle colorBuffer, RTHandle depthPyramid,
            RTHandle cloudsLightingOutput, RTHandle cloudsDepthOutput)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsTrace)))
            {
                CoreUtils.SetKeyword(cmd, "PERCEPTUAL_TRANSMITTANCE", commonData.perceptualBlending);
                CoreUtils.SetKeyword(cmd, "CLOUDS_SIMPLE_PRESET", commonData.simplePreset);
                CoreUtils.SetKeyword(cmd, "CLOUDS_MICRO_EROSION", commonData.microErosion);
                CoreUtils.SetKeyword(cmd, "PHYSICALLY_BASED_SUN", commonData.pbrSkyActive);
                CoreUtils.SetKeyword(cmd, "TRACE_FOR_SKY", commonData.traceForSky);

                cmd.SetComputeBufferParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._VolumetricCloudsAmbientProbeBuffer, ambientProbe);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._CameraColorTexture, colorBuffer);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._CameraDepthTexture, depthPyramid);

                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._Worley128RGBA, commonData.worley128RGBA);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._ErosionNoise, commonData.erosionNoise);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._CloudMapTexture, commonData.cloudMapTexture);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._CloudLutTexture, commonData.cloudLutTexture);

                // Output buffers
                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._CloudsLightingTextureRW, cloudsLightingOutput);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsTraceCS, commonData.renderKernel, HDShaderIDs._CloudsDepthTextureRW, cloudsDepthOutput);

                cmd.DispatchCompute(commonData.volumetricCloudsTraceCS, commonData.renderKernel, traceTX, traceTY, viewCount);
            }
        }

        static void DoVolumetricCloudsReproject(CommandBuffer cmd, int kernel, int traceTX, int traceTY, int viewCount, in VolumetricCloudCommonData commonData,
            RTHandle tracedCloudsLighting, RTHandle tracedCloudsDepth, RTHandle depthPyramid,
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

                // Input textures
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._CameraDepthTexture, depthPyramid);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._CloudsLightingTexture, tracedCloudsLighting);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._CloudsDepthTexture, tracedCloudsDepth);

                // Output textures
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._CloudsLightingTextureRW, lightingOutput);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._CloudsAdditionalTextureRW, additionalOutput);

                // Re-project from the previous frame
                cmd.DispatchCompute(commonData.volumetricCloudsCS, kernel, traceTX, traceTY, viewCount);
            }
        }

        static void DoVolumetricCloudsUpscale(CommandBuffer cmd, int kernel, int traceTX, int traceTY, int viewCount, in VolumetricCloudCommonData commonData,
            RTHandle currentHistory0Buffer, RTHandle currentHistory1Buffer, RTHandle colorBuffer, RTHandle depthPyramid,
            RTHandle cloudsLighting, RTHandle cloudsDepth)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsUpscale)))
            {
                // Compute the final resolution parameters
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._VolumetricCloudsTexture, currentHistory0Buffer);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._DepthStatusTexture, currentHistory1Buffer);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._CameraColorTexture, colorBuffer);
                cmd.SetComputeTextureParam(commonData.volumetricCloudsCS, kernel, HDShaderIDs._CameraDepthTexture, depthPyramid);

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

    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Volumetric Clouds", Order = 1000), HideInInspector]
    class VolumetricCloudsRuntimeResources : IRenderPipelineResources
    {
        public int version => 0;

        #region Shaders
        [Header("Shaders")]
        [SerializeField, ResourcePath("Runtime/Lighting/VolumetricClouds/VolumetricClouds.compute")]
        private ComputeShader m_VolumetricCloudsCS;

        public ComputeShader volumetricCloudsCS
        {
            get => m_VolumetricCloudsCS;
            set => this.SetValueAndNotify(ref m_VolumetricCloudsCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/VolumetricClouds/VolumetricCloudsTrace.compute")]
        private ComputeShader m_VolumetricCloudsTraceCS;

        public ComputeShader volumetricCloudsTraceCS
        {
            get => m_VolumetricCloudsTraceCS;
            set => this.SetValueAndNotify(ref m_VolumetricCloudsTraceCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/VolumetricClouds/VolumetricCloudsTraceShadows.compute")]
        private ComputeShader m_VolumetricCloudsTraceShadowsCS;

        public ComputeShader volumetricCloudsTraceShadowsCS
        {
            get => m_VolumetricCloudsTraceShadowsCS;
            set => this.SetValueAndNotify(ref m_VolumetricCloudsTraceShadowsCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/VolumetricClouds/VolumetricCloudsShadowFilter.compute")]
        private ComputeShader m_VolumetricCloudsShadowFilterCS;

        public ComputeShader volumetricCloudsShadowFilterCS
        {
            get => m_VolumetricCloudsShadowFilterCS;
            set => this.SetValueAndNotify(ref m_VolumetricCloudsShadowFilterCS, value);
        }

        [SerializeField, ResourcePath("Editor/Lighting/VolumetricClouds/CloudMapGenerator.compute")]
        private ComputeShader m_VolumetricCloudMapGeneratorCS;

        public ComputeShader volumetricCloudMapGeneratorCS
        {
            get => m_VolumetricCloudMapGeneratorCS;
            set => this.SetValueAndNotify(ref m_VolumetricCloudMapGeneratorCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/VolumetricClouds/VolumetricCloudsCombine.shader")]
        private Shader m_VolumetricCloudsCombinePS;

        public Shader volumetricCloudsCombinePS
        {
            get => m_VolumetricCloudsCombinePS;
            set => this.SetValueAndNotify(ref m_VolumetricCloudsCombinePS, value);
        }
        #endregion

        #region Textures
        [Header("Textures")]
        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/VolumetricClouds/CloudLutRainAO.png")]
        private Texture2D m_CloudLutRainAO;
        public Texture2D cloudLutRainAO
        {
            get => m_CloudLutRainAO;
            set => this.SetValueAndNotify(ref m_CloudLutRainAO, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/VolumetricClouds/WorleyNoise128RGBA.png")]
        private Texture3D m_WorleyNoise128RGBA;
        public Texture3D worleyNoise128RGBA
        {
            get => m_WorleyNoise128RGBA;
            set => this.SetValueAndNotify(ref m_WorleyNoise128RGBA, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/VolumetricClouds/WorleyNoise32RGB.png")]
        private Texture3D m_WorleyNoise32RGB;
        public Texture3D worleyNoise32RGB
        {
            get => m_WorleyNoise32RGB;
            set => this.SetValueAndNotify(ref m_WorleyNoise32RGB, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/VolumetricClouds/PerlinNoise32RGB.png")]
        private Texture3D m_PerlinNoise32RGB;
        public Texture3D perlinNoise32RGB
        {
            get => m_PerlinNoise32RGB;
            set => this.SetValueAndNotify(ref m_PerlinNoise32RGB, value);
        }
        #endregion
    }
}
