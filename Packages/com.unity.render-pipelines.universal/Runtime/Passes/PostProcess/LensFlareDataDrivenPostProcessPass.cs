using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class LensFlareDataDrivenPostProcessPass : ScriptableRenderPass, IDisposable
    {
        Material m_Material;
        bool m_IsValid;

        // Settings
        public PaniniProjection paniniProjection { get; set; }  // Note: dependency to another pass

        // Input

        // Output
        public TextureHandle destinationTexture { get; set; }

        public LensFlareDataDrivenPostProcessPass(Shader shader)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = null;

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
        }

        public bool IsValid()
        {
            return m_IsValid;
        }

        private class LensFlarePassData
        {
            internal TextureHandle destinationTexture;
            internal UniversalCameraData cameraData;
            internal Material material;
            internal Rect viewport;
            internal float paniniDistance;
            internal float paniniCropToFit;
            internal float width;
            internal float height;
            internal bool usePanini;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            Assertions.Assert.IsTrue(destinationTexture.IsValid(), $"Destination texture must be set for LensFlareDataDrivenPostProcessPass.");

            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            // Reset keywords
            m_Material.shaderKeywords = null;

            var desc = destinationTexture.GetDescriptor(renderGraph);

            if (LensFlareCommonSRP.IsOcclusionRTCompatible())
                LensFlareDataDrivenComputeOcclusion(renderGraph, resourceData, cameraData, in desc);

            RenderLensFlareDataDriven(renderGraph, resourceData, cameraData, destinationTexture, in desc);
        }

        void LensFlareDataDrivenComputeOcclusion(RenderGraph renderGraph, UniversalResourceData resourceData, UniversalCameraData cameraData, in TextureDesc dstDesc)
        {
            using (var builder = renderGraph.AddUnsafePass<LensFlarePassData>("Lens Flare Compute Occlusion", out var passData, ProfilingSampler.Get(URPProfileId.LensFlareDataDrivenComputeOcclusion)))
            {
                TextureHandle occlusionHandle = renderGraph.ImportTexture(LensFlareCommonSRP.occlusionRT);
                passData.destinationTexture = occlusionHandle;
                builder.UseTexture(occlusionHandle, AccessFlags.Write);
                passData.cameraData = cameraData;
                passData.viewport = cameraData.pixelRect;
                passData.material = m_Material;
                passData.width = (float)dstDesc.width;
                passData.height = (float)dstDesc.height;
                if (paniniProjection.IsActive())
                {
                    passData.usePanini = true;
                    passData.paniniDistance = paniniProjection.distance.value;
                    passData.paniniCropToFit = paniniProjection.cropToFit.value;
                }
                else
                {
                    passData.usePanini = false;
                    passData.paniniDistance = 1.0f;
                    passData.paniniCropToFit = 1.0f;
                }

                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);

                builder.SetRenderFunc(
                    static (LensFlarePassData data, UnsafeGraphContext ctx) =>
                    {
                        Camera camera = data.cameraData.camera;
                        Experimental.Rendering.XRPass xr = data.cameraData.xr;

                        Matrix4x4 nonJitteredViewProjMatrix0;
                        int xrId0;
#if ENABLE_VR && ENABLE_XR_MODULE
                        // Not VR or Multi-Pass
                        if (xr.enabled)
                        {
                            if (xr.singlePassEnabled)
                            {
                                nonJitteredViewProjMatrix0 = GL.GetGPUProjectionMatrix(data.cameraData.GetProjectionMatrixNoJitter(0), true) * data.cameraData.GetViewMatrix(0);
                                xrId0 = 0;
                            }
                            else
                            {
                                var gpuNonJitteredProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
                                nonJitteredViewProjMatrix0 = gpuNonJitteredProj * camera.worldToCameraMatrix;
                                xrId0 = data.cameraData.xr.multipassId;
                            }
                        }
                        else
                        {
                            nonJitteredViewProjMatrix0 = GL.GetGPUProjectionMatrix(data.cameraData.GetProjectionMatrixNoJitter(0), true) * data.cameraData.GetViewMatrix(0);
                            xrId0 = 0;
                        }
#else
                        var gpuNonJitteredProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
                        nonJitteredViewProjMatrix0 = gpuNonJitteredProj * camera.worldToCameraMatrix;
                        xrId0 = xr.multipassId;
#endif

                        LensFlareCommonSRP.ComputeOcclusion(
                            data.material, camera, xr, xr.multipassId,
                            data.width, data.height,
                            data.usePanini, data.paniniDistance, data.paniniCropToFit, true,
                            camera.transform.position,
                            nonJitteredViewProjMatrix0,
                            ctx.cmd,
                            false, false, null, null);


#if ENABLE_VR && ENABLE_XR_MODULE
                        if (xr.enabled && xr.singlePassEnabled)
                        {
                            for (int xrIdx = 1; xrIdx < xr.viewCount; ++xrIdx)
                            {
                                Matrix4x4 gpuVPXR = GL.GetGPUProjectionMatrix(data.cameraData.GetProjectionMatrixNoJitter(xrIdx), true) * data.cameraData.GetViewMatrix(xrIdx);

                                // Bypass single pass version
                                LensFlareCommonSRP.ComputeOcclusion(
                                    data.material, camera, xr, xrIdx,
                                    data.width, data.height,
                                    data.usePanini, data.paniniDistance, data.paniniCropToFit, true,
                                    camera.transform.position,
                                    gpuVPXR,
                                    ctx.cmd,
                                    false, false, null, null);
                            }
                        }
#endif
                    });
            }
        }

        void RenderLensFlareDataDriven(RenderGraph renderGraph, UniversalResourceData resourceData, UniversalCameraData cameraData, in TextureHandle destination, in TextureDesc srcDesc)
        {
            using (var builder = renderGraph.AddUnsafePass<LensFlarePassData>("Lens Flare Data Driven Pass", out var passData, ProfilingSampler.Get(URPProfileId.LensFlareDataDriven)))
            {
                // Use WriteTexture here because DoLensFlareDataDrivenCommon will call SetRenderTarget internally.
                // TODO RENDERGRAPH: convert SRP core lens flare to be rendergraph friendly
                passData.destinationTexture = destination;
                builder.UseTexture(destination, AccessFlags.Write);
                passData.cameraData = cameraData;
                passData.material = m_Material;
                passData.width = (float)srcDesc.width;
                passData.height = (float)srcDesc.height;
                passData.viewport.x = 0.0f;
                passData.viewport.y = 0.0f;
                passData.viewport.width = (float)srcDesc.width;
                passData.viewport.height = (float)srcDesc.height;
                if (paniniProjection.IsActive())
                {
                    passData.usePanini = true;
                    passData.paniniDistance = paniniProjection.distance.value;
                    passData.paniniCropToFit = paniniProjection.cropToFit.value;
                }
                else
                {
                    passData.usePanini = false;
                    passData.paniniDistance = 1.0f;
                    passData.paniniCropToFit = 1.0f;
                }
                if (LensFlareCommonSRP.IsOcclusionRTCompatible())
                {
                    TextureHandle occlusionHandle = renderGraph.ImportTexture(LensFlareCommonSRP.occlusionRT);
                    builder.UseTexture(occlusionHandle, AccessFlags.Read);
                }
                else
                {
                    builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                }

                builder.SetRenderFunc(static (LensFlarePassData data, UnsafeGraphContext ctx) =>
                {
                    Camera camera = data.cameraData.camera;
                    Experimental.Rendering.XRPass xr = data.cameraData.xr;

#if ENABLE_VR && ENABLE_XR_MODULE
                    // Not VR or Multi-Pass
                    if (!xr.enabled ||
                        (xr.enabled && !xr.singlePassEnabled))
#endif
                    {
                        var gpuNonJitteredProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
                        Matrix4x4 nonJitteredViewProjMatrix0 = gpuNonJitteredProj * camera.worldToCameraMatrix;

                        LensFlareCommonSRP.DoLensFlareDataDrivenCommon(
                            data.material, data.cameraData.camera, data.viewport, xr, data.cameraData.xr.multipassId,
                            data.width, data.height,
                            data.usePanini, data.paniniDistance, data.paniniCropToFit,
                            true,
                            camera.transform.position,
                            nonJitteredViewProjMatrix0,
                            ctx.cmd,
                            false, false, null, null,
                            data.destinationTexture,
                            (Light light, Camera cam, Vector3 wo) => { return GetLensFlareLightAttenuation(light, cam, wo); },
                            false);
                    }
#if ENABLE_VR && ENABLE_XR_MODULE
                    else
                    {
                        for (int xrIdx = 0; xrIdx < xr.viewCount; ++xrIdx)
                        {
                            Matrix4x4 nonJitteredViewProjMatrix_k = GL.GetGPUProjectionMatrix(data.cameraData.GetProjectionMatrixNoJitter(xrIdx), true) * data.cameraData.GetViewMatrix(xrIdx);

                            LensFlareCommonSRP.DoLensFlareDataDrivenCommon(
                                data.material, data.cameraData.camera, data.viewport, xr, data.cameraData.xr.multipassId,
                                data.width, data.height,
                                data.usePanini, data.paniniDistance, data.paniniCropToFit,
                                true,
                                camera.transform.position,
                                nonJitteredViewProjMatrix_k,
                                ctx.cmd,
                                false, false, null, null,
                                data.destinationTexture,
                                (Light light, Camera cam, Vector3 wo) => { return GetLensFlareLightAttenuation(light, cam, wo); },
                                false);
                        }
                    }
#endif
                });
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetLensFlareLightAttenuation(Light light, Camera cam, Vector3 wo)
        {
            // Must always be true
            if (light != null)
            {
                switch (light.type)
                {
                    case LightType.Directional:
                        return LensFlareCommonSRP.ShapeAttenuationDirLight(light.transform.forward, cam.transform.forward);
                    case LightType.Point:
                        return LensFlareCommonSRP.ShapeAttenuationPointLight();
                    case LightType.Spot:
                        return LensFlareCommonSRP.ShapeAttenuationSpotConeLight(light.transform.forward, wo, light.spotAngle, light.innerSpotAngle / 180.0f);
                    default:
                        return 1.0f;
                }
            }

            return 1.0f;
        }

    }
}
