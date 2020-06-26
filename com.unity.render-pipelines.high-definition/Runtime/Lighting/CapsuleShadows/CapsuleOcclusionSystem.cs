using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{

    [GenerateHLSL]
    enum CapsuleOcclusionType
    {
        None,
        AmbientOcclusion = (1 << 0),
        SpecularOcclusion = (1 << 1),
        DirectionalShadows = (1 << 2)
    }

    internal struct CapsuleOccluderList
    {
        public List<OrientedBBox> bounds;
        public List<EllipsoidOccluderData> occluders;
    }

    partial class CapsuleOcclusionSystem
    {
        const int k_LUTWidth = 1024;   // TODO: This is large temporarily just to make the punctual don't look too bad, but realistically more thought should be put into this. 
        const int k_LUTHeight = 128;
        const int k_LUTDepth = 1;

        private bool m_LUTReady = false;
        private float m_LUTConeApertureUsed = -1.0f;
        private RTHandle m_CapsuleSoftShadowLUT;
        private RenderPipelineResources m_Resources;
        private RenderPipelineSettings m_Settings;

        private RTHandle m_CapsuleOcclusions;

        private HDAdditionalLightData m_LightForShadows = null;

        internal CapsuleOcclusionSystem(HDRenderPipelineAsset hdAsset, RenderPipelineResources defaultResources)
        {
            m_Settings = hdAsset.currentPlatformRenderPipelineSettings;
            m_Resources = defaultResources;

            AllocRTs();
        }

        internal void InvalidateLUT()
        {
            m_LUTReady = false;
        }

        internal void AllocRTs()
        {
            // Enough precision?
            m_CapsuleSoftShadowLUT = RTHandles.Alloc(k_LUTWidth, k_LUTHeight, k_LUTDepth, colorFormat: GraphicsFormat.R8_UNorm,
                                                    dimension: TextureDimension.Tex3D,
                                                    enableRandomWrite: true,
                                                    name: "Capsule Soft Shadows LUT");

            m_CapsuleOcclusions = RTHandles.Alloc(Vector2.one, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R8G8_UNorm, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: "Capsule Occlusions");
        }

        internal void ClearLightForShadows()
        {
            m_LightForShadows = null;
        }

        internal void SetLightForShadows(HDAdditionalLightData light)
        {
            m_LightForShadows = light;
        }

        internal bool IsLightCurrentLightCastingShadows(HDAdditionalLightData light)
        {
            return m_LightForShadows == light;
        }

        internal bool SpecOcclusionOrShadowEnabled(HDCamera hdCamera)
        {
            var specularOcclusionSettings = hdCamera.volumeStack.GetComponent<CapsuleSpecularOcclusion>();
            var shadowSettings = hdCamera.volumeStack.GetComponent<CapsuleSoftShadows>();

            return
                  specularOcclusionSettings.intensity.value > 0.0f ||
                  shadowSettings.intensity.value > 0.0f;

        }
        internal bool AnyEffectIsActive(HDCamera hdCamera)
        {
            var aoSettings = hdCamera.volumeStack.GetComponent<CapsuleAmbientOcclusion>();

            // TODO: Need to add frame settings

            bool anyEnabled = aoSettings.intensity.value > 0.0f ||
                              SpecOcclusionOrShadowEnabled(hdCamera);


            return anyEnabled;
        }

        internal void GenerateCapsuleSoftShadowsLUT(CommandBuffer cmd, HDCamera hdCamera)
        {
            if (!AnyEffectIsActive(hdCamera)) return;

            var shadowSettings = hdCamera.volumeStack.GetComponent<CapsuleSoftShadows>();

            if (m_LUTConeApertureUsed != shadowSettings.coneAperture.value)
            {
                m_LUTReady = false;
                m_LUTConeApertureUsed = shadowSettings.coneAperture.value;
            }
            if (!m_LUTReady)
            {
                var cs = m_Resources.shaders.capsuleShadowLUTGeneratorCS;
                var kernel = cs.FindKernel("CapsuleShadowLUTGeneration");

                cmd.SetComputeVectorParam(cs, HDShaderIDs._LUTGenParameters, new Vector4(k_LUTWidth, k_LUTHeight, k_LUTDepth, Mathf.Cos(Mathf.Deg2Rad * 0.5f * m_LUTConeApertureUsed)));

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CapsuleShadowLUT, m_CapsuleSoftShadowLUT);

                int groupCountX = k_LUTWidth / 8;
                int groupCountY = k_LUTHeight / 8;
                int groupCountZ = 1;

                cmd.DispatchCompute(cs, kernel, groupCountX, groupCountY, groupCountZ);

                m_LUTReady = true;
            }
        }

        // TODO: This assumes is shadows from sun.
        internal void RenderCapsuleOcclusions(CommandBuffer cmd, HDCamera hdCamera, RTHandle occlusionTexture, Light sunLight)
        {
            if (!AnyEffectIsActive(hdCamera)) return;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.CapsuleOcclusion)))
            {
                var cs = m_Resources.shaders.capsuleOcclusionCS;
                var kernel = cs.FindKernel("CapsuleOcclusion");

                var aoSettings = hdCamera.volumeStack.GetComponent<CapsuleAmbientOcclusion>();
                var specularOcclusionSettings = hdCamera.volumeStack.GetComponent<CapsuleSpecularOcclusion>();
                var shadowSettings = hdCamera.volumeStack.GetComponent<CapsuleSoftShadows>();

                cs.shaderKeywords = null;
                
                if (aoSettings.intensity.value > 0.0f) { cs.EnableKeyword("AMBIENT_OCCLUSION"); }
                if (specularOcclusionSettings.intensity.value > 0.0f) { cs.EnableKeyword("SPECULAR_OCCLUSION"); }
                if (specularOcclusionSettings.monteCarlo.value) { cs.EnableKeyword("MONTE_CARLO"); }
                if (shadowSettings.intensity.value > 0.0f) { cs.EnableKeyword("DIRECTIONAL_SHADOW"); }
                

                cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._CapsuleOccludersDatas, m_VisibleCapsuleOccludersDataBuffer);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OcclusionTexture, occlusionTexture);

                // Shadow setup is super temporary. We should instead query the dominant direction from directional light maps. 
                // TODO: Disable feature if sunLight is null.
                var sunDir = (sunLight != null) ? sunLight.transform.forward : -Vector3.up;
                // softness to be derived from angular diameter.
                // For now a somewhat randomly set.

                bool isDir = true;
                Vector3 posOrAxis = new Vector3(-sunDir.x, -sunDir.y, -sunDir.z);
                if(m_LightForShadows != null)
                {
                    posOrAxis = m_LightForShadows.transform.position - hdCamera.camera.transform.position; // move to camera relative
                    isDir = false;
                }

                cmd.SetComputeVectorParam(cs, HDShaderIDs._CapsuleShadowParameters, new Vector4(posOrAxis.x, posOrAxis.y, posOrAxis.z, shadowSettings.coneAperture.value / 89.0f));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._CapsuleShadowParameters2, new Vector4(isDir ? 1 : 0, 0, 0, 0));

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CapsuleShadowLUT, m_CapsuleSoftShadowLUT);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CapsuleOcclusions, m_CapsuleOcclusions);

                if (specularOcclusionSettings.monteCarlo.value)
                {
                    // Same as BlueNoise.BindDitheredRNGData1SPP() but binding to this compute shader, instead of binding globally.
                    // cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OwenScrambledTexture, m_Resources.textures.owenScrambled256Tex);
                    // cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ScramblingTileXSPP, m_Resources.textures.scramblingTile1SPP);
                    // cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RankingTileXSPP, m_Resources.textures.rankingTile1SPP);
                    // cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ScramblingTexture, m_Resources.textures.scramblingTex);

                    // Same as BlueNoise.BindDitheredRNGData8SPP() but binding to this compute shader, instead of binding globally.
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OwenScrambledTexture, m_Resources.textures.owenScrambled256Tex);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ScramblingTileXSPP, m_Resources.textures.scramblingTile8SPP);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RankingTileXSPP, m_Resources.textures.rankingTile8SPP);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ScramblingTexture, m_Resources.textures.scramblingTex);
                }



                cmd.SetComputeVectorParam(cs, HDShaderIDs._CapsuleOcclusionIntensities, new Vector4(aoSettings.intensity.value, specularOcclusionSettings.intensity.value, shadowSettings.intensity.value, 0));

                int dispatchX = HDUtils.DivRoundUp(hdCamera.actualWidth, 16);
                int dispatchY = HDUtils.DivRoundUp(hdCamera.actualHeight, 16);

                cmd.DispatchCompute(cs, kernel, dispatchX, dispatchY, hdCamera.viewCount);

                
            }
        }

        internal void PushGlobalTextures(CommandBuffer cmd, HDCamera hdCamera)
        {
            cmd.SetGlobalTexture(HDShaderIDs._CapsuleOcclusionsTexture,  m_CapsuleOcclusions);
        }

        internal void PushDebugTextures(CommandBuffer cmd, HDCamera hdCamera, RTHandle occlusionTexture)
        {
            (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(hdCamera, cmd, occlusionTexture, FullScreenDebugMode.SSAO);
            (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(hdCamera, cmd, m_CapsuleOcclusions, FullScreenDebugMode.CapsuleSoftShadows);
            (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(hdCamera, cmd, m_CapsuleOcclusions, FullScreenDebugMode.CapsuleSpecularOcclusion);
        }

        internal void Cleanup()
        {
            RTHandles.Release(m_CapsuleSoftShadowLUT);
            RTHandles.Release(m_CapsuleOcclusions);
        }
    }
}
