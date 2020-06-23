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

    class CapsuleOcclusionSystem
    {
        const int k_LUTWidth = 128;
        const int k_LUTHeight = 64;
        const int k_LUTDepth = 4;

        // Culling resources
        ComputeBuffer m_VisibleCapsuleOccludersBuffer = null;
        ComputeBuffer m_VisibleCapsuleOccludersDataBuffer = null;

        private const int k_MaxVisibleCapsuleOccludersCount = 256;
        List<OrientedBBox> m_VisibleCapsuleOccludersBounds = null;
        List<EllipsoidOccluderData> m_VisibleCapsuleOccludersData = null;




        //
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


        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Culling related code 
        internal CapsuleOccluderList PrepareVisibleCapsuleOccludersList(HDCamera hdCamera, CommandBuffer cmd, float time)
        {
            CapsuleOccluderList capsuleOccluderVolumes = new CapsuleOccluderList();
            //if (!Fog.IsVolumetricFogEnabled(hdCamera))
            //    return capsuleOccluderVolumes; 
            //TODO: add a flag to enable and disable this.

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PrepareVisibleCapsuleOccludersList)))
            {
                Vector3 camPosition = hdCamera.camera.transform.position;
                Vector3 camOffset = Vector3.zero;// World-origin-relative

                if (ShaderConfig.s_CameraRelativeRendering != 0)
                {
                    camOffset = camPosition; // Camera-relative
                }

                m_VisibleCapsuleOccludersBounds.Clear();
                m_VisibleCapsuleOccludersData.Clear();

                // Collect all visible finite volume data, and upload it to the GPU.
                var occluders = EllipsoidOccluderManager.manager.PrepareEllipsoidOccludersData(cmd, hdCamera, time);

                for (int i = 0; i < Math.Min(occluders.Count, k_MaxVisibleCapsuleOccludersCount); i++)
                {
                    EllipsoidOccluder occluder = occluders[i];

                    // TODO: cache these?
                    EllipsoidOccluderData data = occluder.ConvertToEngineData(camOffset);

                    Vector3 positionRWS = new Vector3(data.positionRWS_radius.x, data.positionRWS_radius.y, data.positionRWS_radius.z);
                    Vector3 directionWS = new Vector3(data.directionWS_scaling.x, data.directionWS_scaling.y, data.directionWS_scaling.z);
                    Quaternion rotationWS = Quaternion.FromToRotation(Vector3.forward, directionWS);
                    Vector3 scaleWS = Vector3.one * data.positionRWS_radius.w * 2.0f * occluder.influenceRadiusScale;
                    scaleWS.z *= data.directionWS_scaling.w;

                    // TODO: cache these?
                    var obb = new OrientedBBox(Matrix4x4.TRS(positionRWS, rotationWS, scaleWS));

                    // Frustum cull on the CPU for now. TODO: do it on the GPU.
                    // TODO: account for custom near and far planes of the V-Buffer's frustum.
                    // It's typically much shorter (along the Z axis) than the camera's frustum.
                    if (GeometryUtils.Overlap(obb, hdCamera.frustum, 6, 8))
                    {
                        m_VisibleCapsuleOccludersBounds.Add(obb);
                        m_VisibleCapsuleOccludersData.Add(data);
                    }
                }

                m_VisibleCapsuleOccludersBuffer.SetData(m_VisibleCapsuleOccludersBounds);
                m_VisibleCapsuleOccludersDataBuffer.SetData(m_VisibleCapsuleOccludersData);

                // Fill the struct with pointers in order to share the data with the light loop.
                capsuleOccluderVolumes.bounds = m_VisibleCapsuleOccludersBounds;
                capsuleOccluderVolumes.occluders = m_VisibleCapsuleOccludersData;

                return capsuleOccluderVolumes;
            }
        }
        internal void InitializeCapsuleOccluders()
        {
            /*m_SupportVolumetrics = asset.currentPlatformRenderPipelineSettings.supportVolumetrics;

            if (!m_SupportVolumetrics)
                return
                */

            //m_VolumeVoxelizationCS = defaultResources.shaders.volumeVoxelizationCS;
            CreateCapsuleOccluderBuffers();
        }

        internal void CleanupCapsuleOccluders()
        {
            DestroyCapsuleOccluderBuffers();
        }

        internal void CreateCapsuleOccluderBuffers()
        {
            m_VisibleCapsuleOccludersBounds = new List<OrientedBBox>();
            m_VisibleCapsuleOccludersData = new List<EllipsoidOccluderData>();
            m_VisibleCapsuleOccludersBuffer = new ComputeBuffer(k_MaxVisibleCapsuleOccludersCount, Marshal.SizeOf(typeof(OrientedBBox)));
            m_VisibleCapsuleOccludersDataBuffer = new ComputeBuffer(k_MaxVisibleCapsuleOccludersCount, Marshal.SizeOf(typeof(EllipsoidOccluderData)));
        }

        internal void DestroyCapsuleOccluderBuffers()
        {
            CoreUtils.SafeRelease(m_VisibleCapsuleOccludersBuffer);
            CoreUtils.SafeRelease(m_VisibleCapsuleOccludersDataBuffer);

            m_VisibleCapsuleOccludersData = null; // free()
            m_VisibleCapsuleOccludersBounds = null; // free()
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------


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
