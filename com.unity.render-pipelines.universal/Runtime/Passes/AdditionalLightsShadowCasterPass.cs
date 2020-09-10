using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Renders a shadow map atlas for additional shadow-casting Lights.
    /// </summary>
    public partial class AdditionalLightsShadowCasterPass : ScriptableRenderPass
    {
        private static class AdditionalShadowsConstantBuffer
        {
            public static int _AdditionalLightsWorldToShadow;
            public static int _AdditionalShadowParams;
            public static int _AdditionalShadowOffset0;
            public static int _AdditionalShadowOffset1;
            public static int _AdditionalShadowOffset2;
            public static int _AdditionalShadowOffset3;
            public static int _AdditionalShadowmapSize;
        }

        static int m_AdditionalLightsWorldToShadow_SSBO;
        static int m_AdditionalShadowParams_SSBO;
        bool m_UseStructuredBuffer;

        const int k_ShadowmapBufferBits = 16;
        private RenderTargetHandle m_AdditionalLightsShadowmap;
        RenderTexture m_AdditionalLightsShadowmapTexture;

        int m_ShadowmapWidth;
        int m_ShadowmapHeight;

        ShadowSliceData[] m_AdditionalLightsShadowSlices = null;

        int[] m_VisibleLightIndexToAdditionalLightIndex = null;                     // maps a "global" visible light index (index to renderingData.lightData.visibleLights) to an "additional light index" (index to arrays _AdditionalLightsPosition, _AdditionalShadowParams, ...), or -1 if it is not an additional light (i.e if it is the main light)
        int[] m_AdditionalLightIndexToVisibleLightIndex = null;                     // maps additional light index (index to arrays _AdditionalLightsPosition, _AdditionalShadowParams, ...) to its "global" visible light index (index to renderingData.lightData.visibleLights)
        List<int> m_ShadowSliceToAdditionalLightIndex = new List<int>();            // For each shadow slice, store the "additional light indices" of the punctual light that casts it

        Vector4[] m_AdditionalLightIndexToShadowParams = null;                      // per-additional-light shadow info passed to the lighting shader
        Matrix4x4[] m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix = null;   // per-shadow-slice info passed to the lighting shader

        bool m_SupportsBoxFilterForShadows;

        public AdditionalLightsShadowCasterPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;

            AdditionalShadowsConstantBuffer._AdditionalLightsWorldToShadow = Shader.PropertyToID("_AdditionalLightsWorldToShadow");
            AdditionalShadowsConstantBuffer._AdditionalShadowParams = Shader.PropertyToID("_AdditionalShadowParams");
            AdditionalShadowsConstantBuffer._AdditionalShadowOffset0 = Shader.PropertyToID("_AdditionalShadowOffset0");
            AdditionalShadowsConstantBuffer._AdditionalShadowOffset1 = Shader.PropertyToID("_AdditionalShadowOffset1");
            AdditionalShadowsConstantBuffer._AdditionalShadowOffset2 = Shader.PropertyToID("_AdditionalShadowOffset2");
            AdditionalShadowsConstantBuffer._AdditionalShadowOffset3 = Shader.PropertyToID("_AdditionalShadowOffset3");
            AdditionalShadowsConstantBuffer._AdditionalShadowmapSize = Shader.PropertyToID("_AdditionalShadowmapSize");
            m_AdditionalLightsShadowmap.Init("_AdditionalLightsShadowmapTexture");

            m_AdditionalLightsWorldToShadow_SSBO = Shader.PropertyToID("_AdditionalLightsWorldToShadow_SSBO");
            m_AdditionalShadowParams_SSBO = Shader.PropertyToID("_AdditionalShadowParams_SSBO");

            m_UseStructuredBuffer = RenderingUtils.useStructuredBuffer;
            m_SupportsBoxFilterForShadows = Application.isMobilePlatform || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Switch;

            // Preallocated a fixed size. CommandBuffer.SetGlobal* does allow this data to grow.
            int maxVisibleAdditionalLights = UniversalRenderPipeline.maxVisibleAdditionalLights;
            const int maxMainLights = 1;
            int maxVisibleLights = UniversalRenderPipeline.maxVisibleAdditionalLights + maxMainLights;

            m_AdditionalLightIndexToVisibleLightIndex = new int[maxVisibleAdditionalLights];
            m_VisibleLightIndexToAdditionalLightIndex = new int[maxVisibleLights];
            m_AdditionalLightIndexToShadowParams = new Vector4[maxVisibleAdditionalLights];

            if (!m_UseStructuredBuffer)
            {
                // Uniform buffers are faster on some platforms, but they have stricter size limitations
                const int MAX_PUNCTUAL_LIGHT_SHADOW_SLICES_IN_UBO = 545;  // keep in sync with MAX_PUNCTUAL_LIGHT_SHADOW_SLICES_IN_UBO in Shadows.hlsl
                int maxShadowSlices = Math.Min(6*maxVisibleAdditionalLights, MAX_PUNCTUAL_LIGHT_SHADOW_SLICES_IN_UBO);
                m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix = new Matrix4x4[maxShadowSlices];
            }
        }

        private int GetPunctualLightShadowSlicesCount(in LightType lightType)
        {
            switch (lightType)
            {
                case LightType.Spot:
                    return 1;
                case LightType.Point:
                    return 6;
                default:
                    return 0;
            }
        }

        // Magic numbers used to identify light type when rendering shadow receiver.
        // Keep in sync with AdditionalLightRealtimeShadow code in com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl
        private const float LightTypeIdentifierInShadowParams_Spot = 0;
        private const float LightTypeIdentifierInShadowParams_Point = 1;


        // Returns the guard angle that must be added to a frustum angle covering a projection map of resolution sliceResolutionInTexels,
        // in order to also cover a guard band of size guardBandSizeInTexels around the projection map.
        // Formula illustrated in https://i.ibb.co/wpW5Mnf/Calc-Guard-Angle.png
        internal static float CalcGuardAngle(float frustumAngleInDegrees, float guardBandSizeInTexels, float sliceResolutionInTexels)
        {
            float frustumAngle = frustumAngleInDegrees * Mathf.Deg2Rad;
            float halfFrustumAngle = frustumAngle/2;
            float tanHalfFrustumAngle = Mathf.Tan(halfFrustumAngle);

            float halfSliceResolution = sliceResolutionInTexels/2;
            float halfGuardBand = guardBandSizeInTexels/2;
            float factorBetweenAngleTangents = 1 + halfGuardBand/halfSliceResolution;

            float tanHalfGuardAnglePlusHalfFrustumAngle = tanHalfFrustumAngle * factorBetweenAngleTangents;

            float halfGuardAnglePlusHalfFrustumAngle = Mathf.Atan(tanHalfGuardAnglePlusHalfFrustumAngle);
            float halfGuardAngleInRadian = halfGuardAnglePlusHalfFrustumAngle - halfFrustumAngle;

            float guardAngleInRadian = 2*halfGuardAngleInRadian;
            float guardAngleInDegree = guardAngleInRadian * Mathf.Rad2Deg;

            return guardAngleInDegree;
        }

        // Returns the guard angle that must be added to a point light shadow face frustum angle
        // in order to avoid shadows missing at the boundaries between cube faces.
        internal static float GetPointLightShadowFrustumFovBiasInDegrees(int shadowSliceResolution, bool shadowFiltering)
        {
            // Commented-out code below uses the theoretical formula to compute the required guard angle based on the number of additional
            // texels that the projection should cover. It is close to HDRP's HDShadowUtils.CalcGuardAnglePerspective method.
            // However, due to precision issues or other filterings performed at lighting for example, this formula also still requires a fudge factor.
            // Since we only handle a fixed number of resolutions, we use empirical values instead.
            #if false
            float fudgeFactor = 1.5f;
            return fudgeFactor * CalcGuardAngle(90, shadowFiltering? 5 : 1, shadowSliceResolution);
            #endif


            float fovBias = 4.00f;

            // Empirical value found to remove gaps between point light shadow faces in test scenes.
            // We can see that the guard angle is roughly proportional to the inverse of resolution https://docs.google.com/spreadsheets/d/1QrIZJn18LxVKq2-K1XS4EFRZcZdZOJTTKKhDN8Z1b_s
            if (shadowSliceResolution <= 8)
                Debug.LogWarning("Too many additional punctual lights shadows, increase shadow atlas size or remove some shadowed lights");
                // TODO: (If we decide to support it) Investigate why shadows are not rendered when single slice resolution is 8
            else if (shadowSliceResolution <= 16)
                fovBias = 43.0f;
            else if (shadowSliceResolution <= 32)
                fovBias = 18.55f;
            else if (shadowSliceResolution <= 64)
                fovBias = 8.63f;
            else if (shadowSliceResolution <= 128)
                fovBias = 4.13f;
            else if (shadowSliceResolution <= 256)
                fovBias = 2.03f;
            else if (shadowSliceResolution <= 512)
                fovBias = 1.00f;
            else if (shadowSliceResolution <= 1024)
                fovBias = 0.50f;

            if(shadowFiltering)
            {
                if (shadowSliceResolution <= 16)
                    Debug.LogWarning("Too many additional punctual lights shadows to use Soft Shadows. Increase shadow atlas size, remove some shadowed lights or use Hard Shadows.");
                    // With such small resolutions no fovBias can give good visual results
                else if (shadowSliceResolution <= 32)
                    fovBias += 9.35f;
                else if (shadowSliceResolution <= 64)
                    fovBias += 4.07f;
                else if (shadowSliceResolution <= 128)
                    fovBias += 1.77f;
                else if (shadowSliceResolution <= 256)
                    fovBias += 0.85f;
                else if (shadowSliceResolution <= 512)
                    fovBias += 0.39f;
                else if (shadowSliceResolution <= 1024)
                    fovBias += 0.17f;

                // These values were verified to work on platforms for which m_SupportsBoxFilterForShadows is true (Mobile, Switch).
                // TODO: Investigate finer-tuned values for those platforms. Soft shadows are implemented differently for them.
            }

            return fovBias;
        }

        public bool Setup(ref RenderingData renderingData)
        {
            Clear();

            m_ShadowmapWidth = renderingData.shadowData.additionalLightsShadowmapWidth;
            m_ShadowmapHeight = renderingData.shadowData.additionalLightsShadowmapHeight;

            var visibleLights = renderingData.lightData.visibleLights;
            int additionalLightsCount = renderingData.lightData.additionalLightsCount;

            int totalShadowSlicesCount = 0; // number of shadow slices that we will need for all shadowed additional (punctual) lights
            for (int i = 0; i < visibleLights.Length; ++i)
            {
                if (i == renderingData.lightData.mainLightIndex)
                    // Skip main directional light as it is not packed into the shadow atlas
                    continue;

                if (IsValidShadowCastingLight(ref renderingData.lightData, i))
                    totalShadowSlicesCount += GetPunctualLightShadowSlicesCount(visibleLights[i].lightType);
            }

            int atlasWidth = renderingData.shadowData.additionalLightsShadowmapWidth;
            int atlasHeight = renderingData.shadowData.additionalLightsShadowmapHeight;
            // Compute a common sliceResolution that allows to fit all shadow slices in the shadow atlas
            // i.e additional punctual light shadows resolution is adjusted every frame
            int sliceResolution = ShadowUtils.GetMaxTileResolutionInAtlas(atlasWidth, atlasHeight, totalShadowSlicesCount);

            if (m_AdditionalLightsShadowSlices == null || m_AdditionalLightsShadowSlices.Length < totalShadowSlicesCount)
                m_AdditionalLightsShadowSlices = new ShadowSliceData[totalShadowSlicesCount];

            if ( m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix == null ||
                (m_UseStructuredBuffer && (m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix.Length < totalShadowSlicesCount))  ) // m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix can be resized when using SSBO to pass shadow data (no size limitation)
                m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix = new Matrix4x4[totalShadowSlicesCount];

            int validShadowCastingLightsCount = 0;
            bool supportsSoftShadows = renderingData.shadowData.supportsSoftShadows;
            int additionalLightIndex = -1;
            for (int visibleLightIndex = 0; visibleLightIndex < visibleLights.Length && m_ShadowSliceToAdditionalLightIndex.Count < totalShadowSlicesCount; ++visibleLightIndex)
            {
                VisibleLight shadowLight = visibleLights[visibleLightIndex];

                // Skip main directional light as it is not packed into the shadow atlas
                if (visibleLightIndex == renderingData.lightData.mainLightIndex)
                {
                    m_VisibleLightIndexToAdditionalLightIndex[visibleLightIndex] = -1;
                    continue;
                }

                ++additionalLightIndex; // ForwardLights.SetupAdditionalLightConstants skips main Light and thus uses a different index for additional lights
                m_AdditionalLightIndexToVisibleLightIndex[additionalLightIndex] = visibleLightIndex;
                m_VisibleLightIndexToAdditionalLightIndex[visibleLightIndex] = additionalLightIndex;

                LightType lightType = shadowLight.lightType;
                int perLightShadowSlicesCount = GetPunctualLightShadowSlicesCount(lightType);

                int perLightFirstShadowSliceIndex = m_ShadowSliceToAdditionalLightIndex.Count; // shadowSliceIndex within the global array of all additional light shadow slices

                bool isValidShadowCastingLight = false;
                for(int perLightShadowSlice = 0; perLightShadowSlice < perLightShadowSlicesCount; ++perLightShadowSlice)
                {
                    int globalShadowSliceIndex = m_ShadowSliceToAdditionalLightIndex.Count; // shadowSliceIndex within the global array of all additional light shadow slices

                    bool isValidShadowSlice = false;

                    bool lightRangeContainsShadowCasters = renderingData.cullResults.GetShadowCasterBounds(visibleLightIndex, out var shadowCastersBounds);
                    if (lightRangeContainsShadowCasters)
                    {
                        // We need to iterate the lights even though additional lights are disabled because
                        // cullResults.GetShadowCasterBounds() does the fence sync for the shadow culling jobs.
                        if (!renderingData.shadowData.supportsAdditionalLightShadows)
                            continue;

                        if (IsValidShadowCastingLight(ref renderingData.lightData, visibleLightIndex))
                        {
                            if (lightType == LightType.Spot)
                            {
                                bool success = ShadowUtils.ExtractSpotLightMatrix(ref renderingData.cullResults,
                                    ref renderingData.shadowData,
                                    visibleLightIndex,
                                    out var shadowTransform,
                                    out m_AdditionalLightsShadowSlices[globalShadowSliceIndex].viewMatrix,
                                    out m_AdditionalLightsShadowSlices[globalShadowSliceIndex].projectionMatrix,
                                    out m_AdditionalLightsShadowSlices[globalShadowSliceIndex].splitData);

                                if (success)
                                {
                                    m_ShadowSliceToAdditionalLightIndex.Add(additionalLightIndex);
                                    var light = shadowLight.light;
                                    float shadowStrength = light.shadowStrength;
                                    float softShadows = (supportsSoftShadows && light.shadows == LightShadows.Soft) ? 1.0f : 0.0f;
                                    Vector4 shadowParams = new Vector4(shadowStrength, softShadows, LightTypeIdentifierInShadowParams_Spot, perLightFirstShadowSliceIndex);
                                    m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix[globalShadowSliceIndex] = shadowTransform;
                                    m_AdditionalLightIndexToShadowParams[additionalLightIndex] = shadowParams;
                                    isValidShadowSlice = true;
                                    isValidShadowCastingLight = true;
                                }
                            }
                            else if (lightType == LightType.Point)
                            {
                                float fovBias = GetPointLightShadowFrustumFovBiasInDegrees(sliceResolution, (shadowLight.light.shadows==LightShadows.Soft));
                                // Note: the same fovBias will also be used to compute ShadowUtils.GetShadowBias

                                bool success = ShadowUtils.ExtractPointLightMatrix(ref renderingData.cullResults,
                                    ref renderingData.shadowData,
                                    visibleLightIndex,
                                    (CubemapFace)perLightShadowSlice,
                                    fovBias,
                                    out var shadowTransform,
                                    out m_AdditionalLightsShadowSlices[globalShadowSliceIndex].viewMatrix,
                                    out m_AdditionalLightsShadowSlices[globalShadowSliceIndex].projectionMatrix,
                                    out m_AdditionalLightsShadowSlices[globalShadowSliceIndex].splitData);

                                if (success)
                                {
                                    m_ShadowSliceToAdditionalLightIndex.Add(additionalLightIndex);
                                    var light = shadowLight.light;
                                    float shadowStrength = light.shadowStrength;
                                    float softShadows = (supportsSoftShadows && light.shadows == LightShadows.Soft) ? 1.0f : 0.0f;
                                    Vector4 shadowParams = new Vector4(shadowStrength, softShadows, LightTypeIdentifierInShadowParams_Point, perLightFirstShadowSliceIndex);
                                    m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix[globalShadowSliceIndex] = shadowTransform;
                                    m_AdditionalLightIndexToShadowParams[additionalLightIndex] = shadowParams;
                                    isValidShadowSlice = true;
                                    isValidShadowCastingLight = true;
                                }
                            }
                        }
                    }

                    if (!isValidShadowSlice)
                    {
                        Vector4 shadowParams = new Vector4(0 /*shadowStrength*/, 0, 0, -1 /*perLightFirstShadowSliceIndex*/);
                        // shadowParams.x is used in RenderAdditionalShadowMapAtlas to skip shadow map rendering for non-shadow-casting lights
                        // shadowParams.w is used in Lighting shader to find if Additional light casts shadows
                        m_AdditionalLightIndexToShadowParams[additionalLightIndex] = shadowParams;  
                    }
                }

                if(isValidShadowCastingLight)
                    validShadowCastingLightsCount++;
            }

            // Lights that need to be rendered in the shadow map atlas
            if (validShadowCastingLightsCount == 0)
                return false;

            int shadowCastingLightsBufferCount = m_ShadowSliceToAdditionalLightIndex.Count;

            // In the UI we only allow for square shadow map atlas. Here we check if we can fit
            // all shadow slices into half resolution of the atlas and adjust height to have tighter packing.
            int maximumSlices = (m_ShadowmapWidth / sliceResolution) * (m_ShadowmapHeight / sliceResolution);
            if (shadowCastingLightsBufferCount <= (maximumSlices / 2))
                m_ShadowmapHeight /= 2;

            int shadowSlicesPerRow = (atlasWidth / sliceResolution);
            float oneOverAtlasWidth = 1.0f / m_ShadowmapWidth;
            float oneOverAtlasHeight = 1.0f / m_ShadowmapHeight;

            int sliceIndex = 0;
            Matrix4x4 sliceTransform = Matrix4x4.identity;
            sliceTransform.m00 = sliceResolution * oneOverAtlasWidth;
            sliceTransform.m11 = sliceResolution * oneOverAtlasHeight;

            for (int globalShadowSliceIndex = 0; globalShadowSliceIndex < shadowCastingLightsBufferCount; ++globalShadowSliceIndex)
            {
                additionalLightIndex = m_ShadowSliceToAdditionalLightIndex[globalShadowSliceIndex];

                // We can skip the slice if strength is zero.
                if ( Mathf.Approximately(m_AdditionalLightIndexToShadowParams[additionalLightIndex].x, 0.0f)  || Mathf.Approximately(m_AdditionalLightIndexToShadowParams[additionalLightIndex].w, -1.0f))
                    continue;

                m_AdditionalLightsShadowSlices[globalShadowSliceIndex].offsetX = (sliceIndex % shadowSlicesPerRow) * sliceResolution;
                m_AdditionalLightsShadowSlices[globalShadowSliceIndex].offsetY = (sliceIndex / shadowSlicesPerRow) * sliceResolution;
                m_AdditionalLightsShadowSlices[globalShadowSliceIndex].resolution = sliceResolution;

                sliceTransform.m03 = m_AdditionalLightsShadowSlices[globalShadowSliceIndex].offsetX * oneOverAtlasWidth;
                sliceTransform.m13 = m_AdditionalLightsShadowSlices[globalShadowSliceIndex].offsetY * oneOverAtlasHeight;

                // We bake scale and bias to each shadow map in the atlas in the matrix.
                // saves some instructions in shader.
                m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix[globalShadowSliceIndex] = sliceTransform * m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix[globalShadowSliceIndex];

                sliceIndex++;
            }

            return true;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            m_AdditionalLightsShadowmapTexture = ShadowUtils.GetTemporaryShadowTexture(m_ShadowmapWidth, m_ShadowmapHeight, k_ShadowmapBufferBits);
            ConfigureTarget(new RenderTargetIdentifier(m_AdditionalLightsShadowmapTexture));
            ConfigureClear(ClearFlag.All, Color.black);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.shadowData.supportsAdditionalLightShadows)
                RenderAdditionalShadowmapAtlas(ref context, ref renderingData.cullResults, ref renderingData.lightData, ref renderingData.shadowData);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            if (m_AdditionalLightsShadowmapTexture)
            {
                RenderTexture.ReleaseTemporary(m_AdditionalLightsShadowmapTexture);
                m_AdditionalLightsShadowmapTexture = null;
            }
        }

        // Get the "additional light index" (used to index arrays _AdditionalLightsPosition, _AdditionalShadowParams, ...) from the "global" visible light index
        // Function called by Deferred Renderer
        public int GetShadowLightIndexFromLightIndex(int visibleLightIndex)
        {
            if (visibleLightIndex < 0 || visibleLightIndex >= m_VisibleLightIndexToAdditionalLightIndex.Length)
                return -1;

            return m_VisibleLightIndexToAdditionalLightIndex[visibleLightIndex];
        }

        void Clear()
        {
            m_ShadowSliceToAdditionalLightIndex.Clear();
            m_AdditionalLightsShadowmapTexture = null;
        }

        void RenderAdditionalShadowmapAtlas(ref ScriptableRenderContext context, ref CullingResults cullResults, ref LightData lightData, ref ShadowData shadowData)
        {
            NativeArray<VisibleLight> visibleLights = lightData.visibleLights;

            bool additionalLightHasSoftShadows = false;
            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.AdditionalLightsShadow)))
            {
                bool anyShadowSliceRenderer = false;
                int shadowSlicesCount = m_ShadowSliceToAdditionalLightIndex.Count;
                for (int globalShadowSliceIndex = 0; globalShadowSliceIndex < shadowSlicesCount; ++globalShadowSliceIndex)
                {
                    int additionalLightIndex = m_ShadowSliceToAdditionalLightIndex[globalShadowSliceIndex];

                    // we do the shadow strength check here again here because we might have zero strength for non-shadow-casting lights.
                    // In that case we need the shadow data buffer but we can skip rendering them to shadowmap.
                    if ( Mathf.Approximately(m_AdditionalLightIndexToShadowParams[additionalLightIndex].x, 0.0f)  || Mathf.Approximately(m_AdditionalLightIndexToShadowParams[additionalLightIndex].w, -1.0f))
                        continue;

                    int visibleLightIndex = m_AdditionalLightIndexToVisibleLightIndex[additionalLightIndex];

                    VisibleLight shadowLight = visibleLights[visibleLightIndex];

                    ShadowSliceData shadowSliceData = m_AdditionalLightsShadowSlices[globalShadowSliceIndex];

                    var settings = new ShadowDrawingSettings(cullResults, visibleLightIndex);
                    settings.splitData = shadowSliceData.splitData;
                    Vector4 shadowBias = ShadowUtils.GetShadowBias(ref shadowLight, visibleLightIndex,
                        ref shadowData, shadowSliceData.projectionMatrix, shadowSliceData.resolution);
                    ShadowUtils.SetupShadowCasterConstantBuffer(cmd, ref shadowLight, shadowBias);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.CastingPunctualLightShadow, true);
                    ShadowUtils.RenderShadowSlice(cmd, ref context, ref shadowSliceData, ref settings);
                    additionalLightHasSoftShadows |= shadowLight.light.shadows == LightShadows.Soft;
                    anyShadowSliceRenderer = true;
                }

                // We share soft shadow settings for main light and additional lights to save keywords.
                // So we check here if pipeline supports soft shadows and either main light or any additional light has soft shadows
                // to enable the keyword.
                // TODO: In PC and Consoles we can upload shadow data per light and branch on shader. That will be more likely way faster.
                bool mainLightHasSoftShadows = shadowData.supportsMainLightShadows &&
                                               lightData.mainLightIndex != -1 &&
                                               visibleLights[lightData.mainLightIndex].light.shadows ==
                                               LightShadows.Soft;

                bool softShadows = shadowData.supportsSoftShadows &&
                                   (mainLightHasSoftShadows || additionalLightHasSoftShadows);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, anyShadowSliceRenderer);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, softShadows);

                if (anyShadowSliceRenderer)
                    SetupAdditionalLightsShadowReceiverConstants(cmd, ref shadowData, softShadows);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Set constant buffer data that will be used during the lighting/shadowing pass
        void SetupAdditionalLightsShadowReceiverConstants(CommandBuffer cmd, ref ShadowData shadowData, bool softShadows)
        {
            float invShadowAtlasWidth = 1.0f / shadowData.additionalLightsShadowmapWidth;
            float invShadowAtlasHeight = 1.0f / shadowData.additionalLightsShadowmapHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;

            cmd.SetGlobalTexture(m_AdditionalLightsShadowmap.id, m_AdditionalLightsShadowmapTexture);

            if (m_UseStructuredBuffer)
            {
                 // per-light data
                var shadowParamsBuffer = ShaderData.instance.GetAdditionalLightShadowParamsStructuredBuffer(m_AdditionalLightIndexToShadowParams.Length);
                shadowParamsBuffer.SetData(m_AdditionalLightIndexToShadowParams);
                cmd.SetGlobalBuffer(m_AdditionalShadowParams_SSBO, shadowParamsBuffer);

                // per-shadow-slice data
                var shadowSliceMatricesBuffer = ShaderData.instance.GetAdditionalLightShadowSliceMatricesStructuredBuffer(m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix.Length);
                shadowSliceMatricesBuffer.SetData(m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix);
                cmd.SetGlobalBuffer(m_AdditionalLightsWorldToShadow_SSBO, shadowSliceMatricesBuffer);
            }
            else
            {
                cmd.SetGlobalVectorArray(AdditionalShadowsConstantBuffer._AdditionalShadowParams, m_AdditionalLightIndexToShadowParams);                         // per-light data
                cmd.SetGlobalMatrixArray(AdditionalShadowsConstantBuffer._AdditionalLightsWorldToShadow, m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix); // per-shadow-slice data
            }

            if (softShadows)
            {
                if (m_SupportsBoxFilterForShadows)
                {
                    cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowOffset0,
                        new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
                    cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowOffset1,
                        new Vector4(invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
                    cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowOffset2,
                        new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
                    cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowOffset3,
                        new Vector4(invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
                }

                // Currently only used when !SHADER_API_MOBILE but risky to not set them as it's generic
                // enough so custom shaders might use it.
                cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowmapSize, new Vector4(invShadowAtlasWidth, invShadowAtlasHeight,
                    shadowData.additionalLightsShadowmapWidth, shadowData.additionalLightsShadowmapHeight));
            }
        }

        bool IsValidShadowCastingLight(ref LightData lightData, int i)
        {
            if (i == lightData.mainLightIndex)
                return false;

            VisibleLight shadowLight = lightData.visibleLights[i];

            // Directional and light shadows are not supported in the shadow map atlas
            if (shadowLight.lightType == LightType.Directional)
                return false;

            Light light = shadowLight.light;
            return light != null && light.shadows != LightShadows.None && !Mathf.Approximately(light.shadowStrength, 0.0f);
        }
    }
}
