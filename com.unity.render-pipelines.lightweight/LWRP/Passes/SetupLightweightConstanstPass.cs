using System.Collections.Generic;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    /// <summary>
    /// Configure the shader constants needed by the render pipeline
    ///
    /// This pass configures constants that LWRP uses when rendering.
    /// For example, you can execute this pass before you render opaque
    /// objects, to make sure that lights are configured correctly.
    /// </summary>
    public class SetupLightweightConstanstPass : ScriptableRenderPass
    {
        public static class LightConstantBuffer
        {
            public static int _MainLightPosition;
            public static int _MainLightColor;

            public static int _AdditionalLightCount;
            public static int _AdditionalLightPosition;
            public static int _AdditionalLightColor;
            public static int _AdditionalLightAttenuation;
            public static int _AdditionalLightSpotDir;

            public static int _LightIndexBuffer;
        }

        const string k_SetupLightConstants = "Setup Light Constants";
        MixedLightingSetup m_MixedLightingSetup;

        Vector4 k_DefaultLightPosition = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        Vector4 k_DefaultLightColor = Color.black;
        Vector4 k_DefaultLightAttenuation = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        Vector4 k_DefaultLightSpotDirection = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);

        Vector4[] m_LightPositions;
        Vector4[] m_LightColors;
        Vector4[] m_LightAttenuations;
        Vector4[] m_LightSpotDirections;

        private int maxVisibleLocalLights { get; set; }
        private ComputeBuffer perObjectLightIndices { get; set; }

        /// <summary>
        /// Create the pass
        /// </summary>
        public SetupLightweightConstanstPass()
        {
            LightConstantBuffer._MainLightPosition = Shader.PropertyToID("_MainLightPosition");
            LightConstantBuffer._MainLightColor = Shader.PropertyToID("_MainLightColor");
            LightConstantBuffer._AdditionalLightCount = Shader.PropertyToID("_AdditionalLightCount");
            LightConstantBuffer._AdditionalLightPosition = Shader.PropertyToID("_AdditionalLightPosition");
            LightConstantBuffer._AdditionalLightColor = Shader.PropertyToID("_AdditionalLightColor");
            LightConstantBuffer._AdditionalLightAttenuation = Shader.PropertyToID("_AdditionalLightAttenuation");
            LightConstantBuffer._AdditionalLightSpotDir = Shader.PropertyToID("_AdditionalLightSpotDir");
            LightConstantBuffer._LightIndexBuffer = Shader.PropertyToID("_LightIndexBuffer");

            m_LightPositions = new Vector4[0];
            m_LightColors = new Vector4[0];
            m_LightAttenuations = new Vector4[0];
            m_LightSpotDirections = new Vector4[0];
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="maxVisibleLocalLights">Maximum number of allowed visible local lights</param>
        /// <param name="perObjectLightIndices">Buffer holding per object light indicies</param>
        public void Setup(int maxVisibleLocalLights, ComputeBuffer perObjectLightIndices)
        {
            this.maxVisibleLocalLights = maxVisibleLocalLights;
            this.perObjectLightIndices = perObjectLightIndices;

            if (m_LightColors.Length != maxVisibleLocalLights)
            {
                m_LightPositions = new Vector4[maxVisibleLocalLights];
                m_LightColors = new Vector4[maxVisibleLocalLights];
                m_LightAttenuations = new Vector4[maxVisibleLocalLights];
                m_LightSpotDirections = new Vector4[maxVisibleLocalLights];
            }
        }

        void InitializeLightConstants(List<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir)
        {
            lightPos = k_DefaultLightPosition;
            lightColor = k_DefaultLightColor;
            lightAttenuation = k_DefaultLightAttenuation;
            lightSpotDir = k_DefaultLightSpotDirection;

            float subtractiveMixedLighting = 0.0f;

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
                lightPos = new Vector4(pos.x, pos.y, pos.z, 0.0f);
            }

            // VisibleLight.finalColor already returns color in active color space
            lightColor = lightData.finalColor;

            // Directional Light attenuation is initialize so distance attenuation always be 1.0
            if (lightData.lightType != LightType.Directional)
            {
                // Light attenuation in lightweight matches the unity vanilla one.
                // attenuation = 1.0 / distanceToLightSqr
                // We offer two different smoothing factors.
                // The smoothing factors make sure that the light intensity is zero at the light range limit.
                // The first smoothing factor is a linear fade starting at 80 % of the light range.
                // smoothFactor = (lightRangeSqr - distanceToLightSqr) / (lightRangeSqr - fadeStartDistanceSqr)
                // We rewrite smoothFactor to be able to pre compute the constant terms below and apply the smooth factor
                // with one MAD instruction
                // smoothFactor =  distanceSqr * (1.0 / (fadeDistanceSqr - lightRangeSqr)) + (-lightRangeSqr / (fadeDistanceSqr - lightRangeSqr)
                //                 distanceSqr *           oneOverFadeRangeSqr             +              lightRangeSqrOverFadeRangeSqr

                // The other smoothing factor matches the one used in the Unity lightmapper but is slower than the linear one.
                // smoothFactor = (1.0 - saturate((distanceSqr * 1.0 / lightrangeSqr)^2))^2
                float lightRangeSqr = lightData.range * lightData.range;
                float fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
                float fadeRangeSqr = (fadeStartDistanceSqr - lightRangeSqr);
                float oneOverFadeRangeSqr = 1.0f / fadeRangeSqr;
                float lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr;
                float oneOverLightRangeSqr = 1.0f / Mathf.Max(0.0001f, lightData.range * lightData.range);

                // On mobile: Use the faster linear smoothing factor.
                // On other devices: Use the smoothing factor that matches the GI.
                lightAttenuation.x = Application.isMobilePlatform ? oneOverFadeRangeSqr : oneOverLightRangeSqr;
                lightAttenuation.y = lightRangeSqrOverFadeRangeSqr;
                subtractiveMixedLighting = 1.0f;
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
                lightAttenuation.z = invAngleRange;
                lightAttenuation.w = add;
            }

            Light light = lightData.light;

            // TODO: Add support to shadow mask
            if (light != null && light.bakingOutput.mixedLightingMode == MixedLightingMode.Subtractive && light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
            {
                if (m_MixedLightingSetup == MixedLightingSetup.None && lightData.light.shadows != LightShadows.None)
                {
                    m_MixedLightingSetup = MixedLightingSetup.Subtractive;
                    subtractiveMixedLighting = 0.0f;
                }
            }

            // Use the w component of the light position to indicate subtractive mixed light mode.
            // The only directional light is the main light, and the rest are punctual lights.
            // The main light will always have w = 0 and the additional lights have w = 1.
            lightPos.w = subtractiveMixedLighting;
        }

        void SetupShaderLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            // Clear to default all light constant data
            for (int i = 0; i < maxVisibleLocalLights; ++i)
                InitializeLightConstants(lightData.visibleLights, -1, out m_LightPositions[i],
                    out m_LightColors[i],
                    out m_LightAttenuations[i],
                    out m_LightSpotDirections[i]);

            m_MixedLightingSetup = MixedLightingSetup.None;

            // Main light has an optimized shader path for main light. This will benefit games that only care about a single light.
            // Lightweight pipeline also supports only a single shadow light, if available it will be the main light.
            SetupMainLightConstants(cmd, ref lightData);
            SetupAdditionalLightConstants(cmd, ref lightData);
        }

        void SetupMainLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir;
            InitializeLightConstants(lightData.visibleLights, lightData.mainLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir);

            cmd.SetGlobalVector(LightConstantBuffer._MainLightPosition, lightPos);
            cmd.SetGlobalVector(LightConstantBuffer._MainLightColor, lightColor);
        }

        void SetupAdditionalLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
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
                            out m_LightAttenuations[localLightsCount],
                            out m_LightSpotDirections[localLightsCount]);
                        localLightsCount++;
                    }
                }

                cmd.SetGlobalVector(LightConstantBuffer._AdditionalLightCount, new Vector4(lightData.pixelAdditionalLightsCount,
                    lightData.totalAdditionalLightsCount, 0.0f, 0.0f));

                // if not using a compute buffer, engine will set indices in 2 vec4 constants
                // unity_4LightIndices0 and unity_4LightIndices1
                if (perObjectLightIndices != null)
                    cmd.SetGlobalBuffer(LightConstantBuffer._LightIndexBuffer, perObjectLightIndices);
            }
            else
            {
                cmd.SetGlobalVector(LightConstantBuffer._AdditionalLightCount, Vector4.zero);
            }

            cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightPosition, m_LightPositions);
            cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightColor, m_LightColors);
            cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightAttenuation, m_LightAttenuations);
            cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightSpotDir, m_LightSpotDirections);
        }

        void SetShaderKeywords(CommandBuffer cmd, ref CameraData cameraData, ref LightData lightData, ref ShadowData shadowData)
        {
            int vertexLightsCount = lightData.totalAdditionalLightsCount - lightData.pixelAdditionalLightsCount;

            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.AdditionalLights, lightData.totalAdditionalLightsCount > 0);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.MixedLightingSubtractive, m_MixedLightingSetup == MixedLightingSetup.Subtractive);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.VertexLights, vertexLightsCount > 0);

            List<VisibleLight> visibleLights = lightData.visibleLights;

            // If shadows were resolved in screen space we don't sample shadowmap in lit shader. In that case we just set softDirectionalShadows to false.
            bool softDirectionalShadows = shadowData.renderDirectionalShadows && !shadowData.requiresScreenSpaceShadowResolve &&
                shadowData.supportsSoftShadows && lightData.mainLightIndex != -1 &&
                visibleLights[lightData.mainLightIndex].light.shadows == LightShadows.Soft;

            bool softLocalShadows = false;
            if (shadowData.renderLocalShadows && shadowData.supportsSoftShadows)
            {
                List<int> visibleLocalLightIndices = lightData.visibleLocalLightIndices;
                for (int i = 0; i < visibleLocalLightIndices.Count; ++i)
                {
                    if (visibleLights[visibleLocalLightIndices[i]].light.shadows == LightShadows.Soft)
                    {
                        softLocalShadows = true;
                        break;
                    }
                }
            }

            // Currently shadow filtering keyword is shared between local and directional shadows.
            bool hasSoftShadows = softDirectionalShadows || softLocalShadows;

            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.DirectionalShadows, shadowData.renderDirectionalShadows);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.LocalShadows, shadowData.renderLocalShadows);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.SoftShadows, hasSoftShadows);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.CascadeShadows, shadowData.directionalLightCascadeCount > 1);

            // TODO: Remove this. legacy particles support will be removed from Unity in 2018.3. This should be a shader_feature instead with prop exposed in the Standard particles shader.
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.SoftParticles, cameraData.requiresSoftParticles);
        }
        
        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_SetupLightConstants);
            SetupShaderLightConstants(cmd, ref renderingData.lightData);
            SetShaderKeywords(cmd, ref renderingData.cameraData, ref renderingData.lightData, ref renderingData.shadowData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
