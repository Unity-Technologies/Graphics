using System;
using System.Collections.Generic;
using Unity.Collections;
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
        static class LightConstantBuffer
        {
            public static int _MainLightPosition;
            public static int _MainLightColor;

            public static int _AdditionalLightsCount;
            public static int _AdditionalLightsPosition;
            public static int _AdditionalLightsColor;
            public static int _AdditionalLightsAttenuation;
            public static int _AdditionalLightsSpotDir;

            public static int _AdditionalLightsBuffer;
        }

        const string k_SetupLightConstants = "Setup Light Constants";
        MixedLightingSetup m_MixedLightingSetup;

        Vector4 k_DefaultLightPosition = new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
        Vector4 k_DefaultLightColor = Color.black;
        Vector4 k_DefaultLightAttenuation = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        Vector4 k_DefaultLightSpotDirection = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);

        Vector4[] m_AdditionalLightPositions;
        Vector4[] m_AdditionalLightColors;
        Vector4[] m_AdditionalLightAttenuations;
        Vector4[] m_AdditionalLightSpotDirections;

        private int maxVisibleAdditionalLights { get; set; }
        private ComputeBuffer perObjectLightIndices { get; set; }

        /// <summary>
        /// Create the pass
        /// </summary>
        public SetupLightweightConstanstPass()
        {
            LightConstantBuffer._MainLightPosition = Shader.PropertyToID("_MainLightPosition");
            LightConstantBuffer._MainLightColor = Shader.PropertyToID("_MainLightColor");
            LightConstantBuffer._AdditionalLightsCount = Shader.PropertyToID("_AdditionalLightsCount");
            LightConstantBuffer._AdditionalLightsPosition = Shader.PropertyToID("_AdditionalLightsPosition");
            LightConstantBuffer._AdditionalLightsColor = Shader.PropertyToID("_AdditionalLightsColor");
            LightConstantBuffer._AdditionalLightsAttenuation = Shader.PropertyToID("_AdditionalLightsAttenuation");
            LightConstantBuffer._AdditionalLightsSpotDir = Shader.PropertyToID("_AdditionalLightsSpotDir");
            LightConstantBuffer._AdditionalLightsBuffer = Shader.PropertyToID("_AdditionalLightsBuffer");

            m_AdditionalLightPositions = new Vector4[0];
            m_AdditionalLightColors = new Vector4[0];
            m_AdditionalLightAttenuations = new Vector4[0];
            m_AdditionalLightSpotDirections = new Vector4[0];
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="maxVisibleAdditionalLights">Maximum number of visible additional lights</param>
        /// <param name="perObjectLightIndices">Buffer holding per object light indicies</param>
        public void Setup(int maxVisibleAdditionalLights, ComputeBuffer perObjectLightIndices)
        {
            this.maxVisibleAdditionalLights = maxVisibleAdditionalLights;
            this.perObjectLightIndices = perObjectLightIndices;

            if (m_AdditionalLightColors.Length != maxVisibleAdditionalLights)
            {
                m_AdditionalLightPositions = new Vector4[maxVisibleAdditionalLights];
                m_AdditionalLightColors = new Vector4[maxVisibleAdditionalLights];
                m_AdditionalLightAttenuations = new Vector4[maxVisibleAdditionalLights];
                m_AdditionalLightSpotDirections = new Vector4[maxVisibleAdditionalLights];
            }
        }

        void InitializeLightConstants(NativeArray<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir)
        {
            lightPos = k_DefaultLightPosition;
            lightColor = k_DefaultLightColor;
            lightAttenuation = k_DefaultLightAttenuation;
            lightSpotDir = k_DefaultLightSpotDirection;

            // When no lights are visible, main light will be set to -1.
            // In this case we initialize it to default values and return
            if (lightIndex < 0)
                return;

            VisibleLight lightData = lights[lightIndex];
            if (lightData.lightType == LightType.Directional)
            {
                Vector4 dir = -lightData.localToWorldMatrix.GetColumn(2);
                lightPos = new Vector4(dir.x, dir.y, dir.z, k_DefaultLightAttenuation.w);
            }
            else
            {
                Vector4 pos = lightData.localToWorldMatrix.GetColumn(3);
                lightPos = new Vector4(pos.x, pos.y, pos.z, k_DefaultLightAttenuation.w);
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
            }

            if (lightData.lightType == LightType.Spot)
            {
                Vector4 dir = lightData.localToWorldMatrix.GetColumn(2);
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

                    // In subtractive light mode, main light direct contribution is baked on lightmap
                    // In this case we setup light position w component as 0.0f so we can remove it's contribution
                    // from realtime light computation
                    if (lightData.lightType == LightType.Directional)
                        lightPos.w = 0.0f;
                }
            }
        }

        void SetupShaderLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            // Clear to default all light constant data
            for (int i = 0; i < maxVisibleAdditionalLights; ++i)
                InitializeLightConstants(lightData.visibleLights, -1, out m_AdditionalLightPositions[i],
                    out m_AdditionalLightColors[i],
                    out m_AdditionalLightAttenuations[i],
                    out m_AdditionalLightSpotDirections[i]);

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
            var lights = lightData.visibleLights;
            if (lightData.additionalLightsCount > 0)
            {
                int additionalLightsCount = 0;
                for (int i = 0; i < lights.Length && additionalLightsCount < maxVisibleAdditionalLights; ++i)
                {
                    VisibleLight light = lights[i];
                    if (light.lightType != LightType.Directional)
                    {
                        InitializeLightConstants(lights, i, out m_AdditionalLightPositions[additionalLightsCount],
                            out m_AdditionalLightColors[additionalLightsCount],
                            out m_AdditionalLightAttenuations[additionalLightsCount],
                            out m_AdditionalLightSpotDirections[additionalLightsCount]);
                        additionalLightsCount++;
                    }
                }

                cmd.SetGlobalVector(LightConstantBuffer._AdditionalLightsCount, new Vector4(lightData.maxPerObjectAdditionalLightsCount,
                    0.0f, 0.0f, 0.0f));

                // if not using a compute buffer, engine will set indices in 2 vec4 constants
                // unity_4LightIndices0 and unity_4LightIndices1
                if (perObjectLightIndices != null)
                    cmd.SetGlobalBuffer(LightConstantBuffer._AdditionalLightsBuffer, perObjectLightIndices);
            }
            else
            {
                cmd.SetGlobalVector(LightConstantBuffer._AdditionalLightsCount, Vector4.zero);
            }

            cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsPosition, m_AdditionalLightPositions);
            cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsColor, m_AdditionalLightColors);
            cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsAttenuation, m_AdditionalLightAttenuations);
            cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsSpotDir, m_AdditionalLightSpotDirections);
        }
        
        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");

            int additionalLightsCount = renderingData.lightData.additionalLightsCount;
            bool additionalLightsPerVertex = renderingData.lightData.shadeAdditionalLightsPerVertex;
            CommandBuffer cmd = CommandBufferPool.Get(k_SetupLightConstants);
            SetupShaderLightConstants(cmd, ref renderingData.lightData);

            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsVertex, additionalLightsCount > 0 && additionalLightsPerVertex);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsPixel, additionalLightsCount > 0 && !additionalLightsPerVertex);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MixedLightingSubtractive, renderingData.lightData.supportsMixedLighting && m_MixedLightingSetup == MixedLightingSetup.Subtractive);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
