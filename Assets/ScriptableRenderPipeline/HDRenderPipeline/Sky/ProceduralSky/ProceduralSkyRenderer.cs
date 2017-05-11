using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ProceduralSkyRenderer : SkyRenderer
    {
        Material m_ProceduralSkyMaterial = null; // Renders a cubemap into a render texture (can be cube or 2D)
        private ProceduralSkySettings m_ProceduralSkySettings;


        public ProceduralSkyRenderer(ProceduralSkySettings proceduralSkySettings)
        {
            m_ProceduralSkySettings = proceduralSkySettings;
        }

        public override void Build()
        {
            m_ProceduralSkyMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/Sky/SkyProcedural");
        }

        public override void Cleanup()
        {
            Utilities.Destroy(m_ProceduralSkyMaterial);
        }

        public override bool IsSkyValid()
        {
            if (m_ProceduralSkyMaterial == null || m_ProceduralSkySettings == null)
                return false;

            return m_ProceduralSkySettings.skyHDRI != null &&
                m_ProceduralSkySettings.worldMieColorRamp != null &&
                m_ProceduralSkySettings.worldRayleighColorRamp != null;
        }

        public override void SetRenderTargets(BuiltinSkyParameters builtinParams)
        {
            // We do not bind the depth buffer as a depth-stencil target since it is
            // bound as a color texture which is then sampled from within the shader.
            Utilities.SetRenderTarget(builtinParams.renderContext, builtinParams.colorBuffer);
        }

        void SetKeywords(BuiltinSkyParameters builtinParams, ProceduralSkySettings param, bool renderForCubemap)
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
            if (!renderForCubemap)
            {
                m_ProceduralSkyMaterial.EnableKeyword("PERFORM_SKY_OCCLUSION_TEST");
            }

            if (param.debugMode != ProceduralSkySettings.ScatterDebugMode.None)
            {
                m_ProceduralSkyMaterial.EnableKeyword("ATMOSPHERICS_DEBUG");
            }
        }

        void SetUniforms(BuiltinSkyParameters builtinParams, ProceduralSkySettings param, bool renderForCubemap, ref MaterialPropertyBlock properties)
        {
            properties.SetTexture("_Cubemap", param.skyHDRI);
            properties.SetVector("_SkyParam", new Vector4(param.exposure, param.multiplier, param.rotation, 0.0f));

            properties.SetMatrix("_InvViewProjMatrix", builtinParams.invViewProjMatrix);
            properties.SetVector("_CameraPosWS", builtinParams.cameraPosWS);
            properties.SetVector("_ScreenSize", builtinParams.screenSize);

            m_ProceduralSkyMaterial.SetInt("_AtmosphericsDebugMode", (int)param.debugMode);

            Vector3 sunDirection = (builtinParams.sunLight != null) ? -builtinParams.sunLight.transform.forward : Vector3.zero;
            m_ProceduralSkyMaterial.SetVector("_SunDirection", sunDirection);

            var pixelRect = new Rect(0f, 0f, builtinParams.screenSize.x, builtinParams.screenSize.y);
            var scale = 1.0f; //(float)(int)occlusionDownscale;
            var depthTextureScaledTexelSize = new Vector4(scale / pixelRect.width,
                    scale / pixelRect.height,
                    -scale / pixelRect.width,
                    -scale / pixelRect.height);
            properties.SetVector("_DepthTextureScaledTexelSize", depthTextureScaledTexelSize);

            /*
            m_ProceduralSkyMaterial.SetFloat("_ShadowBias", useOcclusion ? occlusionBias : 1f);
            m_ProceduralSkyMaterial.SetFloat("_ShadowBiasIndirect", useOcclusion ? occlusionBiasIndirect : 1f);
            m_ProceduralSkyMaterial.SetFloat("_ShadowBiasClouds", useOcclusion ? occlusionBiasClouds : 1f);
            m_ProceduralSkyMaterial.SetVector("_ShadowBiasSkyRayleighMie", useOcclusion ? new Vector4(occlusionBiasSkyRayleigh, occlusionBiasSkyMie, 0f, 0f) : Vector4.zero);
            m_ProceduralSkyMaterial.SetFloat("_OcclusionDepthThreshold", occlusionDepthThreshold);
            m_ProceduralSkyMaterial.SetVector("_OcclusionTexture_TexelSize", ???);
            */

            m_ProceduralSkyMaterial.SetFloat("_WorldScaleExponent", param.worldScaleExponent);
            m_ProceduralSkyMaterial.SetFloat("_WorldNormalDistanceRcp", 1f / param.worldNormalDistance);
            m_ProceduralSkyMaterial.SetFloat("_WorldMieNearScatterPush", -Mathf.Pow(Mathf.Abs(param.worldMieNearScatterPush), param.worldScaleExponent) * Mathf.Sign(param.worldMieNearScatterPush));
            m_ProceduralSkyMaterial.SetFloat("_WorldRayleighNearScatterPush", -Mathf.Pow(Mathf.Abs(param.worldRayleighNearScatterPush), param.worldScaleExponent) * Mathf.Sign(param.worldRayleighNearScatterPush));
            m_ProceduralSkyMaterial.SetFloat("_WorldRayleighDensity", -param.worldRayleighDensity / 100000f);
            m_ProceduralSkyMaterial.SetFloat("_WorldMieDensity", -param.worldMieDensity / 100000f);
            m_ProceduralSkyMaterial.SetFloat("_SkyDepth", 1.0f / param.maxSkyDistance);

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
            m_ProceduralSkyMaterial.SetFloat("_HeightMieNearScatterPush", -Mathf.Pow(Mathf.Abs(param.heightMieNearScatterPush), param.worldScaleExponent) * Mathf.Sign(param.heightMieNearScatterPush));
            m_ProceduralSkyMaterial.SetFloat("_HeightRayleighNearScatterPush", -Mathf.Pow(Mathf.Abs(param.heightRayleighNearScatterPush), param.worldScaleExponent) * Mathf.Sign(param.heightRayleighNearScatterPush));
            // m_ProceduralSkyMaterial.SetFloat("_HeightRayleighDensity", -param.heightRayleighDensity / 100000f);
            // m_ProceduralSkyMaterial.SetFloat("_HeightMieDensity", -param.heightMieDensity / 100000f);
            m_ProceduralSkyMaterial.SetFloat("_HeightSeaLevel", param.heightSeaLevel);
            m_ProceduralSkyMaterial.SetVector("_HeightPlaneShift", param.heightPlaneShift);
            m_ProceduralSkyMaterial.SetFloat("_HeightDistanceRcp", 1f / param.heightDistance);
            m_ProceduralSkyMaterial.SetVector("_HeightRayleighColor", (Vector4)param.heightRayleighColor * param.heightRayleighIntensity);
            m_ProceduralSkyMaterial.SetFloat("_HeightExtinctionFactor", param.heightExtinctionFactor);

            m_ProceduralSkyMaterial.SetVector("_RayleighInScatterPct", new Vector4(1f - param.worldRayleighIndirectScatter, param.worldRayleighIndirectScatter, 0f, 0f));
            m_ProceduralSkyMaterial.SetFloat("_RayleighExtinctionFactor", param.worldRayleighExtinctionFactor);

            m_ProceduralSkyMaterial.SetFloat("_MiePhaseAnisotropy", param.worldMiePhaseAnisotropy);
            m_ProceduralSkyMaterial.SetFloat("_MieExtinctionFactor", param.worldMieExtinctionFactor);

            // Since we use the material for rendering the sky both into the cubemap, and
            // during the fullscreen pass, setting the 'PERFORM_SKY_OCCLUSION_TEST' keyword has no effect.
            properties.SetFloat("_DisableSkyOcclusionTest", renderForCubemap ? 1.0f : 0.0f);
            // We flip the screens-space Y axis in case we follow the D3D convention.
            properties.SetFloat("_FlipY",                   renderForCubemap ? 1.0f : 0.0f);
            // We do not render the height fog into the sky IBL cubemap.
            properties.SetFloat("_HeightRayleighDensity",   renderForCubemap ? -0.0f : -param.heightRayleighDensity / 100000f);
            properties.SetFloat("_HeightMieDensity",        renderForCubemap ? -0.0f : -param.heightMieDensity / 100000f);
        }

        override public void RenderSky(BuiltinSkyParameters builtinParams, SkySettings skyParameters, bool renderForCubemap)
        {
            MaterialPropertyBlock properties = new MaterialPropertyBlock();

            // Define select preprocessor symbols.
            SetKeywords(builtinParams, m_ProceduralSkySettings, renderForCubemap);

            // Set shader constants.
            SetUniforms(builtinParams, m_ProceduralSkySettings, renderForCubemap, ref properties);

            var cmd = new CommandBuffer { name = "" };

            cmd.DrawMesh(builtinParams.skyMesh, Matrix4x4.identity, m_ProceduralSkyMaterial, 0, 0, properties);
            builtinParams.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }
    }
}
