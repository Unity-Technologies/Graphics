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
        int m_ReprojectCloudsKernel;
        int m_ReprojectCloudsRejectionKernel;
        int m_PreUpscaleCloudsKernel;
        int m_UpscaleAndCombineCloudsKernelColorCopy;
        int m_UpscaleAndCombineCloudsKernelColorRW;
        int m_CombineCloudsKernelColorCopy;
        int m_CombineCloudsKernelColorRW;

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
            m_ReprojectCloudsKernel = volumetricCloudsCS.FindKernel("ReprojectClouds");
            m_ReprojectCloudsRejectionKernel = volumetricCloudsCS.FindKernel("ReprojectCloudsRejection");
            m_PreUpscaleCloudsKernel = volumetricCloudsCS.FindKernel("PreUpscaleClouds");
            m_UpscaleAndCombineCloudsKernelColorCopy = volumetricCloudsCS.FindKernel("UpscaleAndCombineClouds_ColorCopy");
            m_UpscaleAndCombineCloudsKernelColorRW = volumetricCloudsCS.FindKernel("UpscaleAndCombineClouds_ColorRW");
            m_CombineCloudsKernelColorCopy = volumetricCloudsCS.FindKernel("CombineClouds_ColorCopy");
            m_CombineCloudsKernelColorRW = volumetricCloudsCS.FindKernel("CombineClouds_ColorRW");

            // Create the material needed for the combination
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

            // Destroy the material
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

        internal enum TVolumetricCloudsCameraType
        {
            Default,
            RealtimeReflection,
            BakedReflection,
            PlanarReflection
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
            return cloudModelData;
        }

        void UpdateShaderVariableslClouds(ref ShaderVariablesClouds cb, HDCamera hdCamera, VolumetricClouds settings,
            in VolumetricCloudsCameraData cameraData, in CloudModelData cloudModelData, bool shadowPass)
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

                cb._SunLightColor = Vector3.zero;
                cb._ExposureSunColor = 0;
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
            cb._ShapeFactor = cloudModelData.shapeFactor;
            cb._ShapeScale = cloudModelData.shapeScale;
            cb._ErosionFactor = cloudModelData.erosionFactor;
            cb._ErosionScale = cloudModelData.erosionScale;
            cb._ShapeNoiseOffset = new Vector2(settings.shapeOffsetX.value, settings.shapeOffsetZ.value);
            cb._VerticalShapeNoiseOffset = settings.shapeOffsetY.value;

            // If the sun has moved more than 2.0°, reduce significantly the history accumulation
            float sunAngleDifference = 0.0f;
            if (m_CurrentSunLightAdditionalLightData != null)
                sunAngleDifference = Quaternion.Angle(m_CurrentSunLightAdditionalLightData.previousTransform.rotation, m_CurrentSunLightAdditionalLightData.transform.localToWorldMatrix.rotation);
            float sunAttenuation = sunAngleDifference > 2.0f ? 0.5f : 1.0f;
            cb._TemporalAccumulationFactor = settings.temporalAccumulationFactor.value * sunAttenuation;

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
            if (cameraData.cameraType == TVolumetricCloudsCameraType.PlanarReflection)
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

            cb._EnableFastToneMapping = cameraData.enableExposureControl ? 1 : 0;
            cb._LowResolutionEvaluation = cameraData.lowResolution ? 1 : 0;
            cb._EnableIntegration = cameraData.enableIntegration ? 1 : 0;

            unsafe
            {
                for (int p = 0; p < 4; ++p)
                    for (int i = 0; i < 9; ++i)
                        cb._DistanceBasedWeights[12 * p + i] = m_DistanceBasedWeights[9 * p + i];
            }
        }

        struct VolumetricCloudCommonData
        {
            // Resolution parameters
            public TVolumetricCloudsCameraType cameraType;
            public bool enableExposureControl;
            public bool localClouds;

            // Static textures
            public Texture3D worley128RGBA;
            public Texture3D erosionNoise;
            public Texture cloudMapTexture;
            public Texture cloudLutTexture;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public Light sunLight;

            // Compute shader and kernels
            public ComputeShader volumetricCloudsCS;

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

        void FillVolumetricCloudsCommonData(HDCamera hdCamera, VolumetricClouds settings, TVolumetricCloudsCameraType cameraType, in CloudModelData cloudModelData, ref VolumetricCloudCommonData commonData)
        {
            commonData.cameraType = cameraType;
            commonData.localClouds = settings.localClouds.value;
            commonData.volumetricCloudsCS = m_Asset.renderPipelineResources.shaders.volumetricCloudsCS;

            // Static textures
            if (settings.cloudControl.value == VolumetricClouds.CloudControl.Simple)
            {
                commonData.cloudMapTexture = GetPresetCloudMapTexture(settings.cloudPreset.value);
                if (settings.cloudPreset.value == VolumetricClouds.CloudPresets.Custom)
                {
                    PrepareCustomLutData(settings);
                    commonData.cloudLutTexture = m_CustomLutPresetMap;
                }
                else
                    commonData.cloudLutTexture = m_Asset.renderPipelineResources.textures.cloudLutRainAO;
            }
            else if (settings.cloudControl.value == VolumetricClouds.CloudControl.Advanced)
            {
                commonData.cloudMapTexture = m_AdvancedCloudMap;
                commonData.cloudLutTexture = m_Asset.renderPipelineResources.textures.cloudLutRainAO;
            }
            else
            {
                commonData.cloudMapTexture = settings.cloudMap.value != null ? settings.cloudMap.value : Texture2D.blackTexture;
                commonData.cloudLutTexture = settings.cloudLut.value != null ? settings.cloudLut.value : Texture2D.blackTexture;
            }

            commonData.worley128RGBA = m_Asset.renderPipelineResources.textures.worleyNoise128RGBA;
            commonData.erosionNoise = ErosionNoiseTypeToTexture(cloudModelData.erosionNoise);
            BlueNoise blueNoise = GetBlueNoiseManager();
            commonData.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();
            commonData.sunLight = GetMainLight();
            commonData.enableExposureControl = hdCamera.exposureControlFS;
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

                // Animate the offsets
                hdCamera.volumetricCloudsAnimationData.cloudOffset += windVector;
                hdCamera.volumetricCloudsAnimationData.verticalShapeOffset += -settings.verticalShapeWindSpeed.value * delaTime * 0.277778f;
                hdCamera.volumetricCloudsAnimationData.verticalErosionOffset += -settings.verticalErosionWindSpeed.value * delaTime * 0.277778f;

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

            // Evaluate which version of the clouds we should be using
            TVolumetricCloudsCameraType cameraType = GetCameraType(hdCamera);
            bool accumulationClouds = cameraType == TVolumetricCloudsCameraType.Default || cameraType == TVolumetricCloudsCameraType.PlanarReflection;
            bool fullResolutionClouds = cameraType == TVolumetricCloudsCameraType.BakedReflection;

            if (accumulationClouds)
            {
                RenderVolumetricClouds_Accumulation(renderGraph, hdCamera, cameraType, colorBuffer, depthPyramid, motionVector, volumetricLighting);
                // Make sure to mark the history frame index validity.
                PropagateVolumetricCloudsHistoryValidity(hdCamera, settings.localClouds.value);
            }
            else if (fullResolutionClouds)
                RenderVolumetricClouds_FullResolution(renderGraph, hdCamera, cameraType, colorBuffer, depthPyramid, motionVector, volumetricLighting);
            else
                RenderVolumetricClouds_LowResolution(renderGraph, hdCamera, cameraType, colorBuffer, depthPyramid, motionVector, volumetricLighting);
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
