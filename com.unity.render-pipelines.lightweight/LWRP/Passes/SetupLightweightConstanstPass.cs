using System.Collections.Generic;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class SetupLightweightConstanstPass : ScriptableRenderPass
    {
        public static class PerCameraBuffer
        {
            public static int _MainLightPosition;
            public static int _MainLightColor;
            public static int _MainLightCookie;
            public static int _WorldToLight;

            public static int _AdditionalLightCount;
            public static int _AdditionalLightPosition;
            public static int _AdditionalLightColor;
            public static int _AdditionalLightDistanceAttenuation;
            public static int _AdditionalLightSpotDir;
            public static int _AdditionalLightSpotAttenuation;

            public static int _LightIndexBuffer;

            public static int _ScaledScreenParams;
        }

        MixedLightingSetup m_MixedLightingSetup;

        Vector4 k_DefaultLightPosition = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        Vector4 k_DefaultLightColor = Color.black;
        Vector4 k_DefaultLightAttenuation = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        Vector4 k_DefaultLightSpotDirection = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        Vector4 k_DefaultLightSpotAttenuation = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);

        Vector4[] m_LightPositions;
        Vector4[] m_LightColors;
        Vector4[] m_LightDistanceAttenuations;
        Vector4[] m_LightSpotDirections;
        Vector4[] m_LightSpotAttenuations;

        public SetupLightweightConstanstPass(LightweightForwardRenderer renderer) : base(renderer)
        {
            PerCameraBuffer._MainLightPosition = Shader.PropertyToID("_MainLightPosition");
            PerCameraBuffer._MainLightColor = Shader.PropertyToID("_MainLightColor");
            PerCameraBuffer._MainLightCookie = Shader.PropertyToID("_MainLightCookie");
            PerCameraBuffer._WorldToLight = Shader.PropertyToID("_WorldToLight");
            PerCameraBuffer._AdditionalLightCount = Shader.PropertyToID("_AdditionalLightCount");
            PerCameraBuffer._AdditionalLightPosition = Shader.PropertyToID("_AdditionalLightPosition");
            PerCameraBuffer._AdditionalLightColor = Shader.PropertyToID("_AdditionalLightColor");
            PerCameraBuffer._AdditionalLightDistanceAttenuation = Shader.PropertyToID("_AdditionalLightDistanceAttenuation");
            PerCameraBuffer._AdditionalLightSpotDir = Shader.PropertyToID("_AdditionalLightSpotDir");
            PerCameraBuffer._AdditionalLightSpotAttenuation = Shader.PropertyToID("_AdditionalLightSpotAttenuation");
            PerCameraBuffer._LightIndexBuffer = Shader.PropertyToID("_LightIndexBuffer");

            int maxVisibleLocalLights = renderer.maxVisibleLocalLights;
            m_LightPositions = new Vector4[maxVisibleLocalLights];
            m_LightColors = new Vector4[maxVisibleLocalLights];
            m_LightDistanceAttenuations = new Vector4[maxVisibleLocalLights];
            m_LightSpotDirections = new Vector4[maxVisibleLocalLights];
            m_LightSpotAttenuations = new Vector4[maxVisibleLocalLights];
        }

        void InitializeLightConstants(List<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightDistanceAttenuation, out Vector4 lightSpotDir,
            out Vector4 lightSpotAttenuation)
        {
            lightPos = k_DefaultLightPosition;
            lightColor = k_DefaultLightColor;
            lightDistanceAttenuation = k_DefaultLightSpotAttenuation;
            lightSpotDir = k_DefaultLightSpotDirection;
            lightSpotAttenuation = k_DefaultLightAttenuation;

            // When no lights are visible, main light will be set to -1.
            // In this case we initialize it to default values and return
            if (lightIndex < 0)
                return;

            VisibleLight lightData = lights[lightIndex];
            if (lightData.lightType == LightType.Directional)
            {
                Vector4 dir = -lightData.localToWorld.GetColumn(2);
                lightPos = new Vector4(dir.x, dir.y, dir.z, 0.0f);
            }
            else
            {
                Vector4 pos = lightData.localToWorld.GetColumn(3);
                lightPos = new Vector4(pos.x, pos.y, pos.z, 1.0f);
            }

            // VisibleLight.finalColor already returns color in active color space
            lightColor = lightData.finalColor;

            // Directional Light attenuation is initialize so distance attenuation always be 1.0
            if (lightData.lightType != LightType.Directional)
            {
                // Light attenuation in lightweight matches the unity vanilla one.
                // attenuation = 1.0 / 1.0 + distanceToLightSqr * quadraticAttenuation
                // then a smooth factor is applied to linearly fade attenuation to light range
                // the attenuation smooth factor starts having effect at 80% of light range
                // smoothFactor = (lightRangeSqr - distanceToLightSqr) / (lightRangeSqr - fadeStartDistanceSqr)
                // We rewrite smoothFactor to be able to pre compute the constant terms below and apply the smooth factor
                // with one MAD instruction
                // smoothFactor =  distanceSqr * (1.0 / (fadeDistanceSqr - lightRangeSqr)) + (-lightRangeSqr / (fadeDistanceSqr - lightRangeSqr)
                //                 distanceSqr *           oneOverFadeRangeSqr             +              lightRangeSqrOverFadeRangeSqr
                float lightRangeSqr = lightData.range * lightData.range;
                float fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
                float fadeRangeSqr = (fadeStartDistanceSqr - lightRangeSqr);
                float oneOverFadeRangeSqr = 1.0f / fadeRangeSqr;
                float lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr;
                float quadAtten = 25.0f / lightRangeSqr;
                lightDistanceAttenuation = new Vector4(quadAtten, oneOverFadeRangeSqr, lightRangeSqrOverFadeRangeSqr, 1.0f);
            }

            if (lightData.lightType == LightType.Spot)
            {
                Vector4 dir = lightData.localToWorld.GetColumn(2);
                lightSpotDir = new Vector4(-dir.x, -dir.y, -dir.z, 0.0f);

                // Spot Attenuation with a linear falloff can be defined as
                // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
                // This can be rewritten as
                // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
                // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
                // If we precompute the terms in a MAD instruction
                float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * lightData.spotAngle * 0.5f);
                // We neeed to do a null check for particle lights
                // This should be changed in the future
                // Particle lights will use an inline function
                float cosInnerAngle;
                if (lightData.light != null)
                    cosInnerAngle = Mathf.Cos(LightmapperUtils.ExtractInnerCone(lightData.light) * 0.5f);
                else
                    cosInnerAngle = Mathf.Cos((2.0f * Mathf.Atan(Mathf.Tan(lightData.spotAngle * 0.5f * Mathf.Deg2Rad) * (64.0f - 18.0f) / 64.0f)) * 0.5f);
                float smoothAngleRange = Mathf.Max(0.001f, cosInnerAngle - cosOuterAngle);
                float invAngleRange = 1.0f / smoothAngleRange;
                float add = -cosOuterAngle * invAngleRange;
                lightSpotAttenuation = new Vector4(invAngleRange, add, 0.0f);
            }

            Light light = lightData.light;

            // TODO: Add support to shadow mask
            if (light != null && light.bakingOutput.mixedLightingMode == MixedLightingMode.Subtractive && light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
            {
                if (m_MixedLightingSetup == MixedLightingSetup.None && lightData.light.shadows != LightShadows.None)
                {
                    m_MixedLightingSetup = MixedLightingSetup.Subtractive;
                    lightDistanceAttenuation.w = 0.0f;
                }
            }
        }

        void SetupShaderLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            // Clear to default all light constant data
            for (int i = 0; i < renderer.maxVisibleLocalLights; ++i)
                InitializeLightConstants(lightData.visibleLights, -1, out m_LightPositions[i],
                    out m_LightColors[i],
                    out m_LightDistanceAttenuations[i],
                    out m_LightSpotDirections[i],
                    out m_LightSpotAttenuations[i]);

            m_MixedLightingSetup = MixedLightingSetup.None;

            // Main light has an optimized shader path for main light. This will benefit games that only care about a single light.
            // Lightweight pipeline also supports only a single shadow light, if available it will be the main light.
            SetupMainLightConstants(cmd, ref lightData);
            SetupAdditionalLightConstants(cmd, ref lightData);
        }

        void SetupMainLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            Vector4 lightPos, lightColor, lightDistanceAttenuation, lightSpotDir, lightSpotAttenuation;
            List<VisibleLight> lights = lightData.visibleLights;
            InitializeLightConstants(lightData.visibleLights, lightData.mainLightIndex, out lightPos, out lightColor, out lightDistanceAttenuation, out lightSpotDir, out lightSpotAttenuation);

            if (lightData.mainLightIndex >= 0)
            {
                VisibleLight mainLight = lights[lightData.mainLightIndex];
                Light mainLightRef = mainLight.light;

                if (LightweightPipeline.IsSupportedCookieType(mainLight.lightType) && mainLightRef.cookie != null)
                {
                    Matrix4x4 lightCookieMatrix;
                    LightweightPipeline.GetLightCookieMatrix(mainLight, out lightCookieMatrix);
                    cmd.SetGlobalTexture(PerCameraBuffer._MainLightCookie, mainLightRef.cookie);
                    cmd.SetGlobalMatrix(PerCameraBuffer._WorldToLight, lightCookieMatrix);
                }
            }

            cmd.SetGlobalVector(PerCameraBuffer._MainLightPosition, new Vector4(lightPos.x, lightPos.y, lightPos.z, lightDistanceAttenuation.w));
            cmd.SetGlobalVector(PerCameraBuffer._MainLightColor, lightColor);
        }

        void SetupAdditionalLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            int maxVisibleLocalLights = renderer.maxVisibleLocalLights;
            List<VisibleLight> lights = lightData.visibleLights;
            if (lightData.totalAdditionalLightsCount > 0)
            {
                int localLightsCount = 0;
                for (int i = 0; i < lights.Count && localLightsCount < maxVisibleLocalLights; ++i)
                {
                    VisibleLight light = lights[i];
                    if (light.lightType != LightType.Directional)
                    {
                        InitializeLightConstants(lights, i, out m_LightPositions[localLightsCount],
                            out m_LightColors[localLightsCount],
                            out m_LightDistanceAttenuations[localLightsCount],
                            out m_LightSpotDirections[localLightsCount],
                            out m_LightSpotAttenuations[localLightsCount]);
                        localLightsCount++;
                    }
                }

                cmd.SetGlobalVector(PerCameraBuffer._AdditionalLightCount, new Vector4(lightData.pixelAdditionalLightsCount,
                    lightData.totalAdditionalLightsCount, 0.0f, 0.0f));

                // if not using a compute buffer, engine will set indices in 2 vec4 constants
                // unity_4LightIndices0 and unity_4LightIndices1
                if (renderer.perObjectLightIndices != null)
                    cmd.SetGlobalBuffer("_LightIndexBuffer", renderer.perObjectLightIndices);
            }
            else
            {
                cmd.SetGlobalVector(PerCameraBuffer._AdditionalLightCount, Vector4.zero);
            }

            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightPosition, m_LightPositions);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightColor, m_LightColors);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightDistanceAttenuation, m_LightDistanceAttenuations);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightSpotDir, m_LightSpotDirections);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightSpotAttenuation, m_LightSpotAttenuations);
        }

        void SetShaderKeywords(CommandBuffer cmd, ref CameraData cameraData, ref LightData lightData, ref ShadowData shadowData)
        {
            int vertexLightsCount = lightData.totalAdditionalLightsCount - lightData.pixelAdditionalLightsCount;

            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.AdditionalLights, lightData.totalAdditionalLightsCount > 0);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.MixedLightingSubtractive, m_MixedLightingSetup == MixedLightingSetup.Subtractive);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.VertexLights, vertexLightsCount > 0);

            // TODO: We have to discuss cookie approach on LWRP.
            // CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.MainLightCookieText, mainLightIndex != -1 && LightweightUtils.IsSupportedCookieType(visibleLights[mainLightIndex].lightType) && visibleLights[mainLightIndex].light.cookie != null);

            LightShadows directionalShadowQuality = shadowData.renderedDirectionalShadowQuality;
            LightShadows localShadowQuality = shadowData.renderedLocalShadowQuality;

            // Currently shadow filtering keyword is shared between local and directional shadows.
            bool hasSoftShadows = (directionalShadowQuality == LightShadows.Soft || localShadowQuality == LightShadows.Soft) &&
                                  shadowData.supportsSoftShadows;

            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.DirectionalShadows, directionalShadowQuality != LightShadows.None);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.LocalShadows, localShadowQuality != LightShadows.None);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.SoftShadows, hasSoftShadows);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.CascadeShadows, shadowData.directionalLightCascadeCount > 1);

            // TODO: Remove this. legacy particles support will be removed from Unity in 2018.3. This should be a shader_feature instead with prop exposed in the Standard particles shader.
            CoreUtils.SetKeyword(cmd, "SOFTPARTICLES_ON", cameraData.requiresSoftParticles);
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("SetupShaderConstants");
            SetupShaderLightConstants(cmd, ref renderingData.lightData);
            SetShaderKeywords(cmd, ref renderingData.cameraData, ref renderingData.lightData, ref renderingData.shadowData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
