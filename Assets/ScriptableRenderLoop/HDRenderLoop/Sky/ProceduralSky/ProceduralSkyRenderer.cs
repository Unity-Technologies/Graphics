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
        Gradient m_DefaultWorldRayleighColorRamp = null;
        Gradient m_DefaultWorldMieColorRamp = null;

        override public void Build()
        {
            m_ProceduralSkyMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderLoop/Sky/SkyProcedural");

            if (m_DefaultWorldRayleighColorRamp == null)
            {
                m_DefaultWorldRayleighColorRamp = new Gradient();
                m_DefaultWorldRayleighColorRamp.SetKeys(
                    new[] { new GradientColorKey(new Color(0.3f, 0.4f, 0.6f), 0f),
                            new GradientColorKey(new Color(0.5f, 0.6f, 0.8f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f),
                            new GradientAlphaKey(1f, 1f) }
                );
            }

            if (m_DefaultWorldMieColorRamp == null)
            {
                m_DefaultWorldMieColorRamp = new Gradient();
                m_DefaultWorldMieColorRamp.SetKeys(
                    new[] { new GradientColorKey(new Color(0.95f, 0.75f, 0.5f), 0f),
                            new GradientColorKey(new Color(1f, 0.9f, 8.0f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f),
                            new GradientAlphaKey(1f, 1f) }
                );
            }
        }

        override public void Cleanup()
        {
            Utilities.Destroy(m_ProceduralSkyMaterial);
        }


        override public bool IsSkyValid(SkyParameters skyParameters)
        {
            //ProceduralSkyParameters proceduralSkyParams = GetParameters(skyParameters);
            return true; // TODO: See with Evgenii what makes it valid or invalid.
        }

        void UpdateKeywords(bool enable, ProceduralSkyParameters param)
        {
            m_ProceduralSkyMaterial.DisableKeyword("ATMOSPHERICS_OCCLUSION");
            m_ProceduralSkyMaterial.DisableKeyword("ATMOSPHERICS_OCCLUSION_FULLSKY");
            m_ProceduralSkyMaterial.DisableKeyword("ATMOSPHERICS_OCCLUSION_EDGE_FIXUP");
            m_ProceduralSkyMaterial.DisableKeyword("ATMOSPHERICS_SUNRAYS");
            m_ProceduralSkyMaterial.DisableKeyword("ATMOSPHERICS_DEBUG");

            if (enable)
            {
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

                if (param.debugMode != ProceduralSkyParameters.ScatterDebugMode.None)
                {
                    m_ProceduralSkyMaterial.EnableKeyword("ATMOSPHERICS_DEBUG");
                }
            }
        }
        void UpdateStaticUniforms(ProceduralSkyParameters param)
        {
            m_ProceduralSkyMaterial.SetTexture("_SkyDomeCubemap", param.skyDomeCubemap);
            m_ProceduralSkyMaterial.SetFloat("_SkyDomeExposure", param.skyDomeExposure);
            m_ProceduralSkyMaterial.SetColor("_SkyDomeTint", param.skyDomeTint);

            /*
            m_ProceduralSkyMaterial.SetFloat("_ShadowBias", useOcclusion ? occlusionBias : 1f);
            m_ProceduralSkyMaterial.SetFloat("_ShadowBiasIndirect", useOcclusion ? occlusionBiasIndirect : 1f);
            m_ProceduralSkyMaterial.SetFloat("_ShadowBiasClouds", useOcclusion ? occlusionBiasClouds : 1f);
            m_ProceduralSkyMaterial.SetVector("_ShadowBiasSkyRayleighMie", useOcclusion ? new Vector4(occlusionBiasSkyRayleigh, occlusionBiasSkyMie, 0f, 0f) : Vector4.zero);
            m_ProceduralSkyMaterial.SetFloat("_OcclusionDepthThreshold", occlusionDepthThreshold);
            */

            m_ProceduralSkyMaterial.SetFloat("_WorldScaleExponent", param.worldScaleExponent);

            m_ProceduralSkyMaterial.SetFloat("_WorldNormalDistanceRcp", 1f / param.worldNormalDistance);
            m_ProceduralSkyMaterial.SetFloat("_WorldNearScatterPush", -Mathf.Pow(Mathf.Abs(param.worldNearScatterPush), param.worldScaleExponent) * Mathf.Sign(param.worldNearScatterPush));

            m_ProceduralSkyMaterial.SetFloat("_WorldRayleighDensity", -param.worldRayleighDensity / 100000f);
            m_ProceduralSkyMaterial.SetFloat("_MiePhaseAnisotropy", param.worldMiePhaseAnisotropy);
            m_ProceduralSkyMaterial.SetVector("_RayleighInScatterPct", new Vector4(1f - param.worldRayleighIndirectScatter, param.worldRayleighIndirectScatter, 0f, 0f));

            m_ProceduralSkyMaterial.SetFloat("_HeightNormalDistanceRcp", 1f / param.heightNormalDistance);
            m_ProceduralSkyMaterial.SetFloat("_HeightNearScatterPush", -Mathf.Pow(Mathf.Abs(param.heightNearScatterPush), param.worldScaleExponent) * Mathf.Sign(param.heightNearScatterPush));
            m_ProceduralSkyMaterial.SetFloat("_HeightRayleighDensity", -param.heightRayleighDensity / 100000f);

            m_ProceduralSkyMaterial.SetFloat("_HeightSeaLevel", param.heightSeaLevel);
            m_ProceduralSkyMaterial.SetFloat("_HeightDistanceRcp", 1f / param.heightDistance);
            m_ProceduralSkyMaterial.SetVector("_HeightPlaneShift", param.heightPlaneShift);
            m_ProceduralSkyMaterial.SetVector("_HeightRayleighColor", (Vector4)param.heightRayleighColor * param.heightRayleighIntensity);
            m_ProceduralSkyMaterial.SetFloat("_HeightExtinctionFactor", param.heightExtinctionFactor);
            m_ProceduralSkyMaterial.SetFloat("_RayleighExtinctionFactor", param.worldRayleighExtinctionFactor);
            m_ProceduralSkyMaterial.SetFloat("_MieExtinctionFactor", param.worldMieExtinctionFactor);

            Gradient worldRayleighColorRamp = param.worldRayleighColorRamp != null ? param.worldRayleighColorRamp : m_DefaultWorldRayleighColorRamp;
            Gradient worldMieColorRamp = param.worldMieColorRamp != null ? param.worldMieColorRamp : m_DefaultWorldMieColorRamp;

            var rayleighColorM20 = worldRayleighColorRamp.Evaluate(0.00f);
            var rayleighColorM10 = worldRayleighColorRamp.Evaluate(0.25f);
            var rayleighColorO00 = worldRayleighColorRamp.Evaluate(0.50f);
            var rayleighColorP10 = worldRayleighColorRamp.Evaluate(0.75f);
            var rayleighColorP20 = worldRayleighColorRamp.Evaluate(1.00f);

            var mieColorM20 = worldMieColorRamp.Evaluate(0.00f);
            var mieColorO00 = worldMieColorRamp.Evaluate(0.50f);
            var mieColorP20 = worldMieColorRamp.Evaluate(1.00f);

            m_ProceduralSkyMaterial.SetVector("_RayleighColorM20", (Vector4)rayleighColorM20 * param.worldRayleighColorIntensity);
            m_ProceduralSkyMaterial.SetVector("_RayleighColorM10", (Vector4)rayleighColorM10 * param.worldRayleighColorIntensity);
            m_ProceduralSkyMaterial.SetVector("_RayleighColorO00", (Vector4)rayleighColorO00 * param.worldRayleighColorIntensity);
            m_ProceduralSkyMaterial.SetVector("_RayleighColorP10", (Vector4)rayleighColorP10 * param.worldRayleighColorIntensity);
            m_ProceduralSkyMaterial.SetVector("_RayleighColorP20", (Vector4)rayleighColorP20 * param.worldRayleighColorIntensity);

            m_ProceduralSkyMaterial.SetVector("_MieColorM20", (Vector4)mieColorM20 * param.worldMieColorIntensity);
            m_ProceduralSkyMaterial.SetVector("_MieColorO00", (Vector4)mieColorO00 * param.worldMieColorIntensity);
            m_ProceduralSkyMaterial.SetVector("_MieColorP20", (Vector4)mieColorP20 * param.worldMieColorIntensity);

            m_ProceduralSkyMaterial.SetInt("_AtmosphericsDebugMode", (int)param.debugMode);
        }

        void UpdateDynamicUniforms(BuiltinSkyParameters builtinParams, ProceduralSkyParameters param)
        {
            /* For now, we only use the directional light we are attached to, and the current camera. */

            var trackedYaw = param.skyDomeTrackedYawRotation ? param.skyDomeTrackedYawRotation.eulerAngles.y : 0f;
            m_ProceduralSkyMaterial.SetMatrix("_SkyDomeRotation",
                 Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(param.skyDomeRotation.x, 0f, 0f), Vector3.one)
               * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, param.skyDomeRotation.y - trackedYaw, 0f), Vector3.one)
               * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, param.skyDomeVerticalFlip ? -1f : 1f, 1f))
            );

            Vector3 sunDirection = (builtinParams.sunLight != null) ? -builtinParams.sunLight.transform.forward : Vector3.zero;
            m_ProceduralSkyMaterial.SetVector("_SunDirection", sunDirection);
            m_ProceduralSkyMaterial.SetFloat("_WorldMieDensity", -param.worldMieDensity / 100000f);
            m_ProceduralSkyMaterial.SetFloat("_HeightMieDensity", -param.heightMieDensity / 100000f);

            // TODO : We can't pass the camera in builtinParams because when SkyManager call RenderSky for rendering into a cubemap we don't have an actual Camera.
            // Maybe pass the rect/viewport directly?
            var pixelRect = Camera.current ? Camera.current.pixelRect
                                           : new Rect(0f, 0f, Screen.width, Screen.height);
            var scale = 1.0f; //(float)(int)occlusionDownscale;
            var depthTextureScaledTexelSize = new Vector4(scale / pixelRect.width,
                                                          scale / pixelRect.height,
                                                         -scale / pixelRect.width,
                                                         -scale / pixelRect.height);
            m_ProceduralSkyMaterial.SetVector("_DepthTextureScaledTexelSize", depthTextureScaledTexelSize);

            m_ProceduralSkyMaterial.SetMatrix("_InvViewProjMatrix", builtinParams.invViewProjMatrix);
        }

        override public void RenderSky(BuiltinSkyParameters builtinParams, SkyParameters skyParameters)
        {
            ProceduralSkyParameters proceduralSkyParams = GetParameters(skyParameters);

            // Define select preprocessor symbols.
            UpdateKeywords(true, proceduralSkyParams);

            // Julien: what is it supposed to do?
            if (proceduralSkyParams.depthTexture == ProceduralSkyParameters.DepthTexture.Disable)
            {
                // Disable depth texture rendering.
                Camera.current.depthTextureMode = DepthTextureMode.None;
            }

            // Set shader constants.
            UpdateStaticUniforms(proceduralSkyParams);
            UpdateDynamicUniforms(builtinParams, proceduralSkyParams);

            m_ProceduralSkyMaterial.SetTexture("_Cubemap", proceduralSkyParams.skyDomeCubemap);
            m_ProceduralSkyMaterial.SetVector("_SkyParam", new Vector4(proceduralSkyParams.exposure, proceduralSkyParams.multiplier, proceduralSkyParams.rotation, 0.0f));

            var cmd = new CommandBuffer { name = "" };
            cmd.DrawMesh(builtinParams.skyMesh, Matrix4x4.identity, m_ProceduralSkyMaterial);
            builtinParams.renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }
    }
}
