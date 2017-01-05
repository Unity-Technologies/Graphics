using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    public class ProceduralSkyRenderer
        : SkyRenderer<ProceduralSkyParameters>
    {
        Material m_ProceduralSkyMaterial = null; // Renders a cubemap into a render texture (can be cube or 2D)

        override public void Build()
        {
            m_ProceduralSkyMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/Sky/SkyProcedural");
        }

        override public void Cleanup()
        {
            Utilities.Destroy(m_ProceduralSkyMaterial);
        }

        override public bool IsSkyValid(SkyParameters skyParameters)
        {
            ProceduralSkyParameters allParams = GetParameters(skyParameters);

            return allParams.skyHDRI != null &&
                   allParams.worldMieColorRamp != null &&
                   allParams.worldRayleighColorRamp != null;
        }

        void SetKeywords(BuiltinSkyParameters builtinParams, ProceduralSkyParameters param)
        {
            // Ensure that all preprocessor symbols are initially undefined.
            m_ProceduralSkyMaterial.DisableKeyword("ATMOSPHERICS");
            m_ProceduralSkyMaterial.DisableKeyword("ATMOSPHERICS_PER_PIXEL");
            m_ProceduralSkyMaterial.DisableKeyword("ATMOSPHERICS_DEBUG");
            m_ProceduralSkyMaterial.DisableKeyword("ATMOSPHERICS_OCCLUSION");
            m_ProceduralSkyMaterial.DisableKeyword("ATMOSPHERICS_OCCLUSION_EDGE_FIXUP");
            m_ProceduralSkyMaterial.DisableKeyword("ATMOSPHERICS_OCCLUSION_FULLSKY");
            m_ProceduralSkyMaterial.DisableKeyword("ATMOSPHERICS_SUNRAYS");
            m_ProceduralSkyMaterial.DisableKeyword("PERFORM_SKY_OCCLUSION_TEST");

            m_ProceduralSkyMaterial.EnableKeyword("ATMOSPHERICS_PER_PIXEL");

            /*
            if (useOcclusion)
            {
                m_ProceduralSkyMaterial.EnableKeyword("ATMOSPHERICS_OCCLUSION");
                if(occlusionDepthFixup && occlusionDownscale != OcclusionDownscale.x1)
                    m_ProceduralSkyMaterial.EnableKeyword("ATMOSPHERICS_OCCLUSION_EDGE_FIXUP");
                if(occlusionFullSky)
                    m_ProceduralSkyMaterial.EnableKeyword("ATMOSPHERICS_OCCLUSION_FULLSKY");
            }
            */

            // Expected to be valid for the sky pass, and invalid for the cube map generation pass.
            if (builtinParams.depthBuffer != BuiltinSkyParameters.invalidRTI)
            {
                m_ProceduralSkyMaterial.EnableKeyword("PERFORM_SKY_OCCLUSION_TEST");
            }

            if (param.debugMode != ProceduralSkyParameters.ScatterDebugMode.None)
            {
                m_ProceduralSkyMaterial.EnableKeyword("ATMOSPHERICS_DEBUG");
            }
        }

        void SetUniforms(BuiltinSkyParameters builtinParams, ProceduralSkyParameters param)
        {
            m_ProceduralSkyMaterial.SetTexture("_Cubemap", param.skyHDRI);
            m_ProceduralSkyMaterial.SetVector("_SkyParam", new Vector4(param.exposure, param.multiplier, param.rotation, 0.0f));
            m_ProceduralSkyMaterial.SetMatrix("_ViewProjMatrix", builtinParams.viewProjMatrix);
            m_ProceduralSkyMaterial.SetMatrix("_InvViewProjMatrix", builtinParams.invViewProjMatrix);
            m_ProceduralSkyMaterial.SetVector("_CameraPosWS", builtinParams.cameraPosWS);
            m_ProceduralSkyMaterial.SetVector("_ScreenSize", builtinParams.screenSize);

            m_ProceduralSkyMaterial.SetInt("_AtmosphericsDebugMode", (int)param.debugMode);

            Vector3 sunDirection = (builtinParams.sunLight != null) ? -builtinParams.sunLight.transform.forward : Vector3.zero;
            m_ProceduralSkyMaterial.SetVector("_SunDirection", sunDirection);

            /*
            m_ProceduralSkyMaterial.SetFloat("_ShadowBias", useOcclusion ? occlusionBias : 1f);
            m_ProceduralSkyMaterial.SetFloat("_ShadowBiasIndirect", useOcclusion ? occlusionBiasIndirect : 1f);
            m_ProceduralSkyMaterial.SetFloat("_ShadowBiasClouds", useOcclusion ? occlusionBiasClouds : 1f);
            m_ProceduralSkyMaterial.SetVector("_ShadowBiasSkyRayleighMie", useOcclusion ? new Vector4(occlusionBiasSkyRayleigh, occlusionBiasSkyMie, 0f, 0f) : Vector4.zero);
            m_ProceduralSkyMaterial.SetFloat("_OcclusionDepthThreshold", occlusionDepthThreshold);
            m_ProceduralSkyMaterial.SetVector("_OcclusionTexture_TexelSize", ???);
            */

            var pixelRect = new Rect(0f, 0f, builtinParams.screenSize.x, builtinParams.screenSize.y);
            var scale = 1.0f; //(float)(int)occlusionDownscale;
            var depthTextureScaledTexelSize = new Vector4(scale / pixelRect.width,
                                                          scale / pixelRect.height,
                                                         -scale / pixelRect.width,
                                                         -scale / pixelRect.height);
            m_ProceduralSkyMaterial.SetVector("_DepthTextureScaledTexelSize", depthTextureScaledTexelSize);

            m_ProceduralSkyMaterial.SetFloat("_WorldScaleExponent", param.worldScaleExponent);
            m_ProceduralSkyMaterial.SetFloat("_WorldNormalDistanceRcp", 1f / param.worldNormalDistance);
            m_ProceduralSkyMaterial.SetFloat("_WorldNearScatterPush", -Mathf.Pow(Mathf.Abs(param.worldNearScatterPush), param.worldScaleExponent) * Mathf.Sign(param.worldNearScatterPush));
            m_ProceduralSkyMaterial.SetFloat("_WorldRayleighDensity", -param.worldRayleighDensity / 100000f);
            m_ProceduralSkyMaterial.SetFloat("_WorldMieDensity", -param.worldMieDensity / 100000f);

            var rayleighColorM20 = param.worldRayleighColorRamp.Evaluate(0.00f);
            var rayleighColorM10 = param.worldRayleighColorRamp.Evaluate(0.25f);
            var rayleighColorO00 = param.worldRayleighColorRamp.Evaluate(0.50f);
            var rayleighColorP10 = param.worldRayleighColorRamp.Evaluate(0.75f);
            var rayleighColorP20 = param.worldRayleighColorRamp.Evaluate(1.00f);

            var mieColorM20 = param.worldMieColorRamp.Evaluate(0.00f);
            var mieColorO00 = param.worldMieColorRamp.Evaluate(0.50f);
            var mieColorP20 = param.worldMieColorRamp.Evaluate(1.00f);

            m_ProceduralSkyMaterial.SetVector("_RayleighColorM20", (Vector4)rayleighColorM20 * param.worldRayleighColorIntensity);
            m_ProceduralSkyMaterial.SetVector("_RayleighColorM10", (Vector4)rayleighColorM10 * param.worldRayleighColorIntensity);
            m_ProceduralSkyMaterial.SetVector("_RayleighColorO00", (Vector4)rayleighColorO00 * param.worldRayleighColorIntensity);
            m_ProceduralSkyMaterial.SetVector("_RayleighColorP10", (Vector4)rayleighColorP10 * param.worldRayleighColorIntensity);
            m_ProceduralSkyMaterial.SetVector("_RayleighColorP20", (Vector4)rayleighColorP20 * param.worldRayleighColorIntensity);

            m_ProceduralSkyMaterial.SetVector("_MieColorM20", (Vector4)mieColorM20 * param.worldMieColorIntensity);
            m_ProceduralSkyMaterial.SetVector("_MieColorO00", (Vector4)mieColorO00 * param.worldMieColorIntensity);
            m_ProceduralSkyMaterial.SetVector("_MieColorP20", (Vector4)mieColorP20 * param.worldMieColorIntensity);

            m_ProceduralSkyMaterial.SetFloat("_HeightNormalDistanceRcp", 1f / param.heightNormalDistance);
            m_ProceduralSkyMaterial.SetFloat("_HeightNearScatterPush", -Mathf.Pow(Mathf.Abs(param.heightNearScatterPush), param.worldScaleExponent) * Mathf.Sign(param.heightNearScatterPush));
            m_ProceduralSkyMaterial.SetFloat("_HeightRayleighDensity", -param.heightRayleighDensity / 100000f);
            m_ProceduralSkyMaterial.SetFloat("_HeightMieDensity", -param.heightMieDensity / 100000f);
            m_ProceduralSkyMaterial.SetFloat("_HeightSeaLevel", param.heightSeaLevel);
            m_ProceduralSkyMaterial.SetVector("_HeightPlaneShift", param.heightPlaneShift);
            m_ProceduralSkyMaterial.SetFloat("_HeightDistanceRcp", 1f / param.heightDistance);
            m_ProceduralSkyMaterial.SetVector("_HeightRayleighColor", (Vector4)param.heightRayleighColor * param.heightRayleighIntensity);
            m_ProceduralSkyMaterial.SetFloat("_HeightExtinctionFactor", param.heightExtinctionFactor);

            m_ProceduralSkyMaterial.SetVector("_RayleighInScatterPct", new Vector4(1f - param.worldRayleighIndirectScatter, param.worldRayleighIndirectScatter, 0f, 0f));
            m_ProceduralSkyMaterial.SetFloat("_RayleighExtinctionFactor", param.worldRayleighExtinctionFactor);

            m_ProceduralSkyMaterial.SetFloat("_MiePhaseAnisotropy", param.worldMiePhaseAnisotropy);
            m_ProceduralSkyMaterial.SetFloat("_MieExtinctionFactor", param.worldMieExtinctionFactor);

        }

        override public void RenderSky(BuiltinSkyParameters builtinParams, SkyParameters skyParameters)
        {
            ProceduralSkyParameters proceduralSkyParams = GetParameters(skyParameters);

            // Define select preprocessor symbols.
            SetKeywords(builtinParams, proceduralSkyParams);

            // Set shader constants.
            SetUniforms(builtinParams, proceduralSkyParams);

            var cmd = new CommandBuffer { name = "" };
            if (builtinParams.depthBuffer != BuiltinSkyParameters.invalidRTI)
            {
                cmd.SetGlobalTexture("_CameraDepthTexture", builtinParams.depthBuffer);
            }
            cmd.DrawMesh(builtinParams.skyMesh, Matrix4x4.identity, m_ProceduralSkyMaterial);
            builtinParams.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }
    }
}
