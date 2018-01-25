using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDUtils
    {
        public const RendererConfiguration k_RendererConfigurationBakedLighting = RendererConfiguration.PerObjectLightProbe | RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbeProxyVolume;
        public const RendererConfiguration k_RendererConfigurationBakedLightingWithShadowMask = k_RendererConfigurationBakedLighting | RendererConfiguration.PerObjectOcclusionProbe | RendererConfiguration.PerObjectOcclusionProbeProxyVolume | RendererConfiguration.PerObjectShadowMask;

        public static List<RenderPipelineMaterial> GetRenderPipelineMaterialList()
        {
            var baseType = typeof(RenderPipelineMaterial);
            var assembly = baseType.Assembly;

            var types = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(baseType))
                .Select(Activator.CreateInstance)
                .Cast<RenderPipelineMaterial>()
                .ToList();

            // Note: If there is a need for an optimization in the future of this function, user can
            // simply fill the materialList manually by commenting the code abode and returning a
            // custom list of materials they use in their game.
            //
            // return new List<RenderPipelineMaterial>
            // {
            //    new Lit(),
            //    new Unlit(),
            //    ...
            // };

            return types;
        }

        public static Matrix4x4 GetViewProjectionMatrix(Matrix4x4 worldToViewMatrix, Matrix4x4 projectionMatrix)
        {
            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            var gpuProj = GL.GetGPUProjectionMatrix(projectionMatrix, false);
            var gpuVP = gpuProj *  worldToViewMatrix * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API.

            return gpuVP;
        }

        // Helper to help to display debug info on screen
        static float s_OverlayLineHeight = -1.0f;
        public static void NextOverlayCoord(ref float x, ref float y, float overlayWidth, float overlayHeight, float width)
        {
            x += overlayWidth;
            s_OverlayLineHeight = Mathf.Max(overlayHeight, s_OverlayLineHeight);
            // Go to next line if it goes outside the screen.
            if (x + overlayWidth > width)
            {
                x = 0;
                y -= s_OverlayLineHeight;
                s_OverlayLineHeight = -1.0f;
            }
        }


        public static Matrix4x4 ComputePixelCoordToWorldSpaceViewDirectionMatrix(float verticalFoV, Vector4 screenSize, Matrix4x4 worldToViewMatrix, bool renderToCubemap)
        {
            // Compose the view space version first.
            // V = -(X, Y, Z), s.t. Z = 1,
            // X = (2x / resX - 1) * tan(vFoV / 2) * ar = x * [(2 / resX) * tan(vFoV / 2) * ar] + [-tan(vFoV / 2) * ar] = x * [-m00] + [-m20]
            // Y = (2y / resY - 1) * tan(vFoV / 2)      = y * [(2 / resY) * tan(vFoV / 2)]      + [-tan(vFoV / 2)]      = y * [-m11] + [-m21]
            float tanHalfVertFoV = Mathf.Tan(0.5f * verticalFoV);
            float aspectRatio    = screenSize.x * screenSize.w;

            // Compose the matrix.
            float m21 = tanHalfVertFoV;
            float m20 = tanHalfVertFoV * aspectRatio;
            float m00 = -2.0f * screenSize.z * m20;
            float m11 = -2.0f * screenSize.w * m21;
            float m33 = -1.0f;

            if (renderToCubemap)
            {
                // Flip Y.
                m11 = -m11;
                m21 = -m21;
            }

            var viewSpaceRasterTransform = new Matrix4x4(new Vector4(m00, 0.0f, 0.0f, 0.0f),
                                                         new Vector4(0.0f, m11, 0.0f, 0.0f),
                                                         new Vector4(m20, m21, m33, 0.0f),
                                                         new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            // Remove the translation component.
            var homogeneousZero = new Vector4(0, 0, 0, 1);
            worldToViewMatrix.SetColumn(3, homogeneousZero);

            // Flip the Z to make the coordinate system left-handed.
            worldToViewMatrix.SetRow(2, -worldToViewMatrix.GetRow(2));

            // Transpose for HLSL.
            return Matrix4x4.Transpose(worldToViewMatrix.transpose * viewSpaceRasterTransform);
        }

        // This set of RenderTarget management methods is supposed to be used when rendering into a camera dependent render texture.
        // This will automatically set the viewport based on the camera size and the RTHandle scaling info.
        public static void SetRenderTarget(CommandBuffer cmd, HDCamera camera, RTHandle buffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            cmd.SetRenderTarget(buffer, miplevel, cubemapFace, depthSlice);
            SetViewport(cmd, camera, buffer);
            if (clearFlag != ClearFlag.None)
                cmd.ClearRenderTarget((clearFlag & ClearFlag.Depth) != 0, (clearFlag & ClearFlag.Color) != 0, clearColor);
        }

        public static void SetRenderTarget(CommandBuffer cmd, HDCamera camera, RTHandle buffer, ClearFlag clearFlag = ClearFlag.None, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            CoreUtils.SetRenderTarget(cmd, buffer, clearFlag, CoreUtils.clearColorAllBlack, miplevel, cubemapFace, depthSlice);
            SetViewport(cmd, camera, buffer);
        }

        public static void SetRenderTarget(CommandBuffer cmd, HDCamera camera, RTHandle colorBuffer, RTHandle depthBuffer, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer, ClearFlag.None, CoreUtils.clearColorAllBlack, miplevel, cubemapFace, depthSlice);
            SetViewport(cmd, camera, colorBuffer);
        }

        public static void SetRenderTarget(CommandBuffer cmd, HDCamera camera, RTHandle colorBuffer, RTHandle depthBuffer, ClearFlag clearFlag, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer, clearFlag, CoreUtils.clearColorAllBlack, miplevel, cubemapFace, depthSlice);
            SetViewport(cmd, camera, colorBuffer);
        }

        public static void SetRenderTarget(CommandBuffer cmd, HDCamera camera, RTHandle colorBuffer, RTHandle depthBuffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            cmd.SetRenderTarget(colorBuffer, depthBuffer, miplevel, cubemapFace, depthSlice);
            SetViewport(cmd, camera, colorBuffer);
            if (clearFlag != ClearFlag.None)
                cmd.ClearRenderTarget((clearFlag & ClearFlag.Depth) != 0, (clearFlag & ClearFlag.Color) != 0, clearColor);
        }

        public static void SetRenderTarget(CommandBuffer cmd, HDCamera camera, RenderTargetIdentifier[] colorBuffers, RTHandle depthBuffer)
        {
            CoreUtils.SetRenderTarget(cmd, colorBuffers, depthBuffer, ClearFlag.None, CoreUtils.clearColorAllBlack);
            SetViewport(cmd, camera, depthBuffer);
        }

        public static void SetRenderTarget(CommandBuffer cmd, HDCamera camera, RenderTargetIdentifier[] colorBuffers, RTHandle depthBuffer, ClearFlag clearFlag = ClearFlag.None)
        {
            CoreUtils.SetRenderTarget(cmd, colorBuffers, depthBuffer, clearFlag, CoreUtils.clearColorAllBlack);
            SetViewport(cmd, camera, depthBuffer);
        }

        public static void SetRenderTarget(CommandBuffer cmd, HDCamera camera, RenderTargetIdentifier[] colorBuffers, RTHandle depthBuffer, ClearFlag clearFlag, Color clearColor)
        {
            cmd.SetRenderTarget(colorBuffers, depthBuffer);
            SetViewport(cmd, camera, depthBuffer);
            if (clearFlag != ClearFlag.None)
                cmd.ClearRenderTarget((clearFlag & ClearFlag.Depth) != 0, (clearFlag & ClearFlag.Color) != 0, clearColor);
        }

        public static void SetViewport(CommandBuffer cmd, HDCamera camera, RTHandle target)
        {
            // Scaling viewport is done for auto-scaling render targets.
            // In the context of HDRP, every auto-scaled RT is scaled against the maximum RTHandles reference size (that can only grow).
            // When we render using a camera whose viewport is smaller than the RTHandles reference size (and thus smaller than the RT actual size), we need to set it explicitly (otherwise, native code will set the viewport at the size of the RT)
            // For auto-scaled RTs (like for example a half-resolution RT), we need to scale this viewport accordingly.
            // For non scaled RTs we just do nothing, the native code will set the viewport at the size of the RT anyway.
            if (target.useScaling)
            {
                Debug.Assert(camera != null, "Missing HDCamera when setting up Render Target with auto-scale and Viewport.");
                Vector2Int scaledViewportSize = target.GetScaledSize(new Vector2Int(camera.actualWidth, camera.actualHeight));
                cmd.SetViewport(new Rect(0.0f, 0.0f, scaledViewportSize.x, scaledViewportSize.y));
            }
        }

        // In the context of HDRP, the internal render targets used during the render loop are the same for all cameras, no matter the size of the camera.
        // It means that we can end up rendering inside a partial viewport for one of these "camera space" rendering.
        // In this case, we need to make sure than when we blit from one such camera texture to another, we only blit the necessary portion corresponding to the camera viewport.
        public static void BlitCameraTexture(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            BlitCameraTexture(cmd, camera, source, (RenderTargetIdentifier)destination);
        }

        public static void BlitCameraTexture(CommandBuffer cmd, HDCamera camera, RTHandle source, RenderTargetIdentifier destination)
        {
            cmd.Blit(source, destination, new Vector2(camera.scaleBias.x, camera.scaleBias.y), Vector2.zero);
        }
    }
}
