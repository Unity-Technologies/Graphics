#if ENABLE_PATH_TRACING_SRP
using System;
using UnityEngine.PathTracing.Core;

namespace UnityEngine.Rendering.LiveGI
{
    internal class RayTracingRenderPipelineInstance : RenderPipeline
    {

        // Use this variable to a reference to the Render Pipeline Asset that was passed to the constructor
        private RayTracingRenderPipelineAsset renderPipelineAsset;
        private PathTracingContext ptContext;

        public RayTracingRenderPipelineInstance(RayTracingRenderPipelineAsset asset)
        {
            renderPipelineAsset = asset;
            ptContext = new PathTracingContext(PathTracingOutput.FullPathTracer);

            var resources = Util.LoadOrCreateRayTracingResources();
            if (resources != null)
            {
                var activeBackend = ptContext.SelectRayTracingBackend(renderPipelineAsset.settings.raytracingBackend, resources);

                // Set the active backend, in case we request an unsuported backend and fallback to another one
                renderPipelineAsset.settings.raytracingBackend = activeBackend;
            }
        }

        protected override void Dispose(bool disposing)
        {
            ptContext?.Dispose();
        }

        [Obsolete]
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            using var cmd = new CommandBuffer();
            cmd.name = "Path Tracing Command Buffer";

            ptContext.Update(cmd, renderPipelineAsset.settings);

            // Iterate over all Cameras
            foreach (Camera camera in cameras)
            {
                // Update the value of built-in shader variables, based on the current Camera
                context.SetupCameraProperties(camera);

                var additionalData = camera.GetComponent<AdditionalCameraData>();
                if (additionalData == null)
                {
                    additionalData = camera.gameObject.AddComponent<AdditionalCameraData>();
                    additionalData.hideFlags = HideFlags.DontSave;   // Don't show this in inspector
                }
                additionalData.CreatePersistentResources(camera, renderPipelineAsset.settings.denoising);

                Vector4 frustum = PathTracingContext.GetCameraFrustum(camera);

                // Render the path tracing into the camera's target texture
                var scaledSize = additionalData.rayTracingOutput.GetScaledSize();
                ptContext.Render(cmd, scaledSize, frustum, camera.cameraToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, additionalData.previousViewProjection, renderPipelineAsset.settings, additionalData.rayTracingOutput, additionalData.normals, additionalData.motionVectors, additionalData.debugOutput, additionalData.frameIndex);
                var viewProjection = camera.projectionMatrix * camera.worldToCameraMatrix;
                ptContext.Denoise(cmd,additionalData.denoiser, camera.nearClipPlane, camera.farClipPlane, viewProjection, renderPipelineAsset.settings, additionalData.rayTracingOutput, additionalData.normals, additionalData.motionVectors);
                additionalData.UpdateCameraDataPostRender(camera);

                // Blit the path traced frame to the active camera texture
                // Note: we could not render to this directly, as it might not have the required UAV flags
                cmd.Blit(additionalData.rayTracingOutput, camera.activeTexture);

                // Instruct the graphics API to perform all scheduled commands
                context.ExecuteCommandBuffer(cmd);
            }

            context.Submit();
        }

    }
}

#endif
