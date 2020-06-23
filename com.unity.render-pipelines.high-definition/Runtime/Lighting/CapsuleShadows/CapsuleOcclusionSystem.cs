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
        const int k_LUTWidth = 128;
        const int k_LUTHeight = 64;
        const int k_LUTDepth = 4;

        private bool m_LUTReady = false;
        private RTHandle m_CapsuleSoftShadowLUT;
        private RenderPipelineResources m_Resources;
        private RenderPipelineSettings m_Settings;

        private RTHandle m_CapsuleOcclusions;

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

        // TODO: This assumes is shadows from sun.
        internal void RenderCapsuleOcclusions(CommandBuffer cmd, HDCamera hdCamera, RTHandle occlusionTexture, Light sunLight)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.CapsuleOcclusion)))
            {
                var cs = m_Resources.shaders.capsuleOcclusionCS;
                var kernel = cs.FindKernel("CapsuleOcclusion");

                var aoSettings = hdCamera.volumeStack.GetComponent<CapsuleAmbientOcclusion>();

                cs.shaderKeywords = null;
                cs.EnableKeyword("DIRECTIONAL_SHADOW");
                cs.EnableKeyword("SPECULAR_OCCLUSION");
                if (aoSettings.intensity.value > 0.0f)
                    cs.EnableKeyword("AMBIENT_OCCLUSION");


                cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._CapsuleOccludersDatas, m_VisibleCapsuleOccludersDataBuffer);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OcclusionTexture, occlusionTexture);

                // Shadow setup is super temporary. 

                var sunDir = sunLight.transform.forward;
                // softness to be derived from angular diameter.
                int softnessIndex = 3;
                cmd.SetComputeVectorParam(cs, HDShaderIDs._CapsuleShadowParameters, new Vector4(sunDir.x, sunDir.y, sunDir.z, softnessIndex));
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CapsuleShadowLUT, m_CapsuleSoftShadowLUT);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CapsuleOcclusions, m_CapsuleOcclusions);

                int dispatchX = HDUtils.DivRoundUp(hdCamera.actualWidth, 16);
                int dispatchY = HDUtils.DivRoundUp(hdCamera.actualHeight, 16);

                cmd.DispatchCompute(cs, kernel, dispatchX, dispatchY, hdCamera.viewCount);
            }
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

        internal void GenerateCapsuleSoftShadowsLUT(CommandBuffer cmd)
        {
            if(!m_LUTReady)
            {
                var cs = m_Resources.shaders.capsuleShadowLUTGeneratorCS;
                var kernel = cs.FindKernel("CapsuleShadowLUTGeneration");

                cmd.SetComputeVectorParam(cs, HDShaderIDs._LUTGenParameters, new Vector4(k_LUTWidth, k_LUTHeight, k_LUTDepth, 0));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._LUTConeCosAngles, new Vector4( Mathf.Cos(0.5f * 10.0f * Mathf.Deg2Rad), Mathf.Cos(0.5f * 30.0f * Mathf.Deg2Rad), Mathf.Cos(0.5f * 50.0f * Mathf.Deg2Rad), Mathf.Cos(0.5f * 70.0f * Mathf.Deg2Rad)));

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CapsuleShadowLUT, m_CapsuleSoftShadowLUT);

                int groupCountX = k_LUTWidth / 8;
                int groupCountY = k_LUTHeight / 8;
                int groupCountZ = k_LUTDepth / 4;

                cmd.DispatchCompute(cs, kernel, groupCountX, groupCountY, groupCountZ);

                m_LUTReady = true;
            }
        }
    }
}
