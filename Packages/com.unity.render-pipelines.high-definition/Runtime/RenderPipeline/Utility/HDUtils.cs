using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Rendering;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Various utility functions for HDRP.
    /// </summary>
    public class HDUtils
    {
#if UNITY_EDITOR
        internal const string k_HdrpAssetBuildLabel = "HDRP:IncludeInBuild";
#endif

        internal const SortingCriteria k_OpaqueSortingCriteria = SortingCriteria.CommonOpaque & (~SortingCriteria.QuantizedFrontToBack);

        /// <summary>Returns the render configuration for baked static lighting, this value can be used in a RendererListDesc call to render Lit objects.</summary>
        /// <returns></returns>
        [Obsolete("Use GetRendererConfiguration() instead. #from(2023.2).")]
        public static PerObjectData GetBakedLightingRenderConfig() => PerObjectData.LightProbe | PerObjectData.Lightmaps | PerObjectData.LightProbeProxyVolume;
        /// <summary>Returns the render configuration for baked static lighting with shadow masks, this value can be used in a RendererListDesc call to render Lit objects when shadow masks are enabled.</summary>
        /// <returns></returns>
        [Obsolete("Use GetRendererConfiguration() instead. #from(2023.2).")]
        public static PerObjectData GetBakedLightingWithShadowMaskRenderConfig() => GetBakedLightingRenderConfig() | PerObjectData.OcclusionProbe | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ShadowMask;

        /// <summary>
        /// Returns the render configuration that should be used in a RendererListDesc call to render Lit objects.
        /// </summary>
        /// <param name="apv">True if APV is enabled</param>
        /// <param name="shadowMask">True if shadow mask is enabled</param>
        /// <returns>Returns the render configuration that should be used in a RendererListDesc call to render Lit objects.</returns>
        public static PerObjectData GetRendererConfiguration(bool apv, bool shadowMask)
        {
            PerObjectData   configuration = PerObjectData.Lightmaps;
            if (!apv)       configuration |= PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume;
            if (shadowMask) configuration |= PerObjectData.OcclusionProbe | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ShadowMask;

            return configuration;
        }

        /// <summary>Default HDAdditionalReflectionData</summary>
        static internal HDAdditionalReflectionData s_DefaultHDAdditionalReflectionData { get { return ComponentSingleton<HDAdditionalReflectionData>.instance; } }
        /// <summary>Default HDAdditionalLightData</summary>
        static internal HDAdditionalLightData s_DefaultHDAdditionalLightData { get { return ComponentSingleton<HDAdditionalLightData>.instance; } }
        /// <summary>Default HDAdditionalCameraData</summary>
        static internal HDAdditionalCameraData s_DefaultHDAdditionalCameraData { get { return ComponentSingleton<HDAdditionalCameraData>.instance; } }

        static List<CustomPassVolume> m_TempCustomPassVolumeList = new List<CustomPassVolume>();

        static Texture3D m_ClearTexture3D;
        static RTHandle m_ClearTexture3DRTH;
        /// <summary>
        /// Default 1x1x1 3D texture initialized with Color.clear.
        /// </summary>
        public static Texture3D clearTexture3D
        {
            get
            {
                if (m_ClearTexture3D == null)
                {
                    m_ClearTexture3D = new Texture3D(1, 1, 1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None) { name = "Transparent Texture 3D" };
                    m_ClearTexture3D.SetPixel(0, 0, 0, Color.clear);
                    m_ClearTexture3D.Apply();

                    RTHandles.Release(m_ClearTexture3DRTH);
                    m_ClearTexture3DRTH = null;
                }

                return m_ClearTexture3D;
            }
        }

        /// <summary>
        /// Default 1x1x1 3D RTHandle initialized with Color.clear.
        /// </summary>
        public static RTHandle clearTexture3DRTH
        {
            get
            {
                if (m_ClearTexture3DRTH == null || m_ClearTexture3D == null) // Need to check regular texture as the RTHandle won't null out on domain reload
                {
                    RTHandles.Release(m_ClearTexture3DRTH);
                    m_ClearTexture3DRTH = RTHandles.Alloc(clearTexture3D);
                }

                return m_ClearTexture3DRTH;
            }
        }

        /// <summary>
        /// Returns the HDRP default blit material.
        /// </summary>
        /// <param name="dimension">Dimension of the texture to blit, either 2D or 2D Array.</param>
        /// <param name="singleSlice">Blit only a single slice of the array if applicable.</param>
        /// <returns></returns>
        public static Material GetBlitMaterial(TextureDimension dimension, bool singleSlice = false)
        {
            return Blitter.GetBlitMaterial(dimension, singleSlice);
        }

        /// <summary>
        /// Current HDRP settings.
        /// </summary>
        public static RenderPipelineSettings hdrpSettings
        {
            get
            {
                return HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings;
            }
        }

        internal static List<RenderPipelineMaterial> GetRenderPipelineMaterialList()
        {
            var baseType = typeof(RenderPipelineMaterial);
            var assembly = baseType.Assembly;

            try
            {
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
            catch (System.Reflection.ReflectionTypeLoadException exception)
            {
                foreach (TypeLoadException loaderException in exception.LoaderExceptions)
                {
                    Debug.LogError($"Encountered an exception while attempting to reflect the HDRP assembly to extract all RenderPipelineMaterial types.\nThis exception must be fixed in order to fully initialize HDRP correctly.\n{loaderException.Message}\n{loaderException.TypeName}");
                }

                return null;
            }
        }

        internal static int GetRuntimeDebugPanelWidth(HDCamera hdCamera)
        {
            // 600 is the panel size from 'DebugUI Panel' prefab + 10 pixels of padding
            int width = DebugManager.instance.displayRuntimeUI ? 610 : 0;
            return Math.Min(hdCamera.actualWidth, width);
        }

        /// <summary>Get the aspect ratio of a projection matrix.</summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        internal static float ProjectionMatrixAspect(in Matrix4x4 matrix)
            => - matrix.m11 / matrix.m00;

        /// <summary>
        /// Determine if a projection matrix is off-center (asymmetric).
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        internal static bool IsProjectionMatrixAsymmetric(in Matrix4x4 matrix)
            => matrix.m02 != 0 || matrix.m12 != 0;

        internal static Matrix4x4 ComputePixelCoordToWorldSpaceViewDirectionMatrix(float verticalFoV, Vector2 lensShift, Vector4 screenSize, Matrix4x4 worldToViewMatrix, bool renderToCubemap, float aspectRatio = -1, bool isOrthographic = false)
        {
            Matrix4x4 viewSpaceRasterTransform;

            if (isOrthographic)
            {
                // For ortho cameras, project the skybox with no perspective
                // the same way as builtin does (case 1264647)
                viewSpaceRasterTransform = new Matrix4x4(
                    new Vector4(-2.0f * screenSize.z, 0.0f, 0.0f, 0.0f),
                    new Vector4(0.0f, -2.0f * screenSize.w, 0.0f, 0.0f),
                    new Vector4(1.0f, 1.0f, -1.0f, 0.0f),
                    new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
            }
            else
            {
                // Compose the view space version first.
                // V = -(X, Y, Z), s.t. Z = 1,
                // X = (2x / resX - 1) * tan(vFoV / 2) * ar = x * [(2 / resX) * tan(vFoV / 2) * ar] + [-tan(vFoV / 2) * ar] = x * [-m00] + [-m20]
                // Y = (2y / resY - 1) * tan(vFoV / 2)      = y * [(2 / resY) * tan(vFoV / 2)]      + [-tan(vFoV / 2)]      = y * [-m11] + [-m21]

                aspectRatio = aspectRatio < 0 ? screenSize.x * screenSize.w : aspectRatio;
                float tanHalfVertFoV = Mathf.Tan(0.5f * verticalFoV);

                // Compose the matrix.
                float m21 = (1.0f - 2.0f * lensShift.y) * tanHalfVertFoV;
                float m11 = -2.0f * screenSize.w * tanHalfVertFoV;

                float m20 = (1.0f - 2.0f * lensShift.x) * tanHalfVertFoV * aspectRatio;
                float m00 = -2.0f * screenSize.z * tanHalfVertFoV * aspectRatio;

                if (renderToCubemap)
                {
                    // Flip Y.
                    m11 = -m11;
                    m21 = -m21;
                }

                viewSpaceRasterTransform = new Matrix4x4(new Vector4(m00, 0.0f, 0.0f, 0.0f),
                    new Vector4(0.0f, m11, 0.0f, 0.0f),
                    new Vector4(m20, m21, -1.0f, 0.0f),
                    new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            }

            // Remove the translation component.
            var homogeneousZero = new Vector4(0, 0, 0, 1);
            worldToViewMatrix.SetColumn(3, homogeneousZero);

            // Flip the Z to make the coordinate system left-handed.
            worldToViewMatrix.SetRow(2, -worldToViewMatrix.GetRow(2));

            // Transpose for HLSL.
            return Matrix4x4.Transpose(worldToViewMatrix.transpose * viewSpaceRasterTransform);
        }

        internal static float ComputZPlaneTexelSpacing(float planeDepth, float verticalFoV, float resolutionY)
        {
            float tanHalfVertFoV = Mathf.Tan(0.5f * verticalFoV);
            return tanHalfVertFoV * (2.0f / resolutionY) * planeDepth;
        }

        /// <summary>
        /// Blit a texture using a quad in the current render target.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source texture.</param>
        /// <param name="scaleBiasTex">Scale and bias for the input texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        /// <param name="mipLevelTex">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public static void BlitQuad(CommandBuffer cmd, Texture source, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear)
        {
            Blitter.BlitQuad(cmd, source, scaleBiasTex, scaleBiasRT, mipLevelTex, bilinear);
        }

        /// <summary>
        /// Blit a texture using a quad in the current render target.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source texture.</param>
        /// <param name="textureSize">Source texture size.</param>
        /// <param name="scaleBiasTex">Scale and bias for sampling the input texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        /// <param name="mipLevelTex">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        /// <param name="paddingInPixels">Padding in pixels.</param>
        public static void BlitQuadWithPadding(CommandBuffer cmd, Texture source, Vector2 textureSize, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear, int paddingInPixels)
        {
            Blitter.BlitQuadWithPadding(cmd, source, textureSize, scaleBiasTex, scaleBiasRT, mipLevelTex, bilinear, paddingInPixels);
        }

        /// <summary>
        /// Blit a texture using a quad in the current render target, by performing an alpha blend with the existing content on the render target.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source texture.</param>
        /// <param name="textureSize">Source texture size.</param>
        /// <param name="scaleBiasTex">Scale and bias for sampling the input texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        /// <param name="mipLevelTex">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        /// <param name="paddingInPixels">Padding in pixels.</param>
        public static void BlitQuadWithPaddingMultiply(CommandBuffer cmd, Texture source, Vector2 textureSize, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear, int paddingInPixels)
        {
            Blitter.BlitQuadWithPaddingMultiply(cmd, source, textureSize, scaleBiasTex, scaleBiasRT, mipLevelTex, bilinear, paddingInPixels);
        }

        /// <summary>
        /// Blit a texture (which is a Octahedral projection) using a quad in the current render target.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source texture.</param>
        /// <param name="textureSize">Source texture size.</param>
        /// <param name="scaleBiasTex">Scale and bias for sampling the input texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        /// <param name="mipLevelTex">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        /// <param name="paddingInPixels">Padding in pixels.</param>
        public static void BlitOctahedralWithPadding(CommandBuffer cmd, Texture source, Vector2 textureSize, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear, int paddingInPixels)
        {
            Blitter.BlitOctahedralWithPadding(cmd, source, textureSize, scaleBiasTex, scaleBiasRT, mipLevelTex, bilinear, paddingInPixels);
        }

        /// <summary>
        /// Blit a texture (which is a Octahedral projection) using a quad in the current render target, by performing an alpha blend with the existing content on the render target.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source texture.</param>
        /// <param name="textureSize">Source texture size.</param>
        /// <param name="scaleBiasTex">Scale and bias for sampling the input texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        /// <param name="mipLevelTex">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        /// <param name="paddingInPixels">Padding in pixels.</param>
        public static void BlitOctahedralWithPaddingMultiply(CommandBuffer cmd, Texture source, Vector2 textureSize, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear, int paddingInPixels)
        {
            Blitter.BlitOctahedralWithPaddingMultiply(cmd, source, textureSize, scaleBiasTex, scaleBiasRT, mipLevelTex, bilinear, paddingInPixels);
        }

        /// <summary>
        /// Blit a RTHandle texture.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public static void BlitTexture(CommandBuffer cmd, RTHandle source, Vector4 scaleBias, float mipLevel, bool bilinear)
        {
            Blitter.BlitTexture(cmd, source, scaleBias, mipLevel, bilinear);
        }

        /// <summary>
        /// Blit a RTHandle texture 2D.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public static void BlitTexture2D(CommandBuffer cmd, RTHandle source, Vector4 scaleBias, float mipLevel, bool bilinear)
        {
            Blitter.BlitTexture2D(cmd, source, scaleBias, mipLevel, bilinear);
        }

        /// <summary>
        /// Blit a 2D texture and depth buffer.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="sourceColor">Source Texture for color.</param>
        /// <param name="sourceDepth">Source RenderTexture for depth.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        internal static void BlitColorAndDepth(CommandBuffer cmd, Texture sourceColor, RenderTexture sourceDepth, Vector4 scaleBias, float mipLevel, bool blitDepth)
        {
            Blitter.BlitColorAndDepth(cmd, sourceColor, sourceDepth, scaleBias, mipLevel, blitDepth);
        }

        /// <summary>
        /// Blit a RTHandle texture
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="material">Material to invoke when blitting.</param>
        /// <param name="pass">Pass idx within the material to invoke.</param>
        static void BlitTexture(CommandBuffer cmd, RTHandle source, Vector4 scaleBias, Material material, int pass)
        {
            Blitter.BlitTexture(cmd, source, scaleBias, material, pass);
        }

        // In the context of HDRP, the internal render targets used during the render loop are the same for all cameras, no matter the size of the camera.
        // It means that we can end up rendering inside a partial viewport for one of these "camera space" rendering.
        // In this case, we need to make sure than when we blit from one such camera texture to another, we only blit the necessary portion corresponding to the camera viewport.
        // Here, both source and destination are camera-scaled.
        /// <summary>
        /// Blit a RTHandle to another RTHandle.
        /// This will properly account for partial usage (in term of resolution) of the texture for the current viewport.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="destination">Destination RTHandle.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public static void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, float mipLevel = 0.0f, bool bilinear = false)
        {
            Blitter.BlitCameraTexture(cmd, source, destination, mipLevel, bilinear);
        }

        /// <summary>
        /// Blit a RThandle Texture2D RTHandle to another RTHandle.
        /// This will properly account for partial usage (in term of resolution) of the texture for the current viewport.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="destination">Destination RTHandle.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public static void BlitCameraTexture2D(CommandBuffer cmd, RTHandle source, RTHandle destination, float mipLevel = 0.0f, bool bilinear = false)
        {
            Blitter.BlitCameraTexture2D(cmd, source, destination, mipLevel, bilinear);
        }

        /// <summary>
        /// Blit a RTHandle to another RTHandle.
        /// This will properly account for partial usage (in term of resolution) of the texture for the current viewport.
        /// This overloads allows the user to override the default blit shader
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="destination">Destination RTHandle.</param>
        /// <param name="material">The material to use when blitting</param>
        /// <param name="pass">pass to use of the provided material</param>
        public static void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, Material material, int pass)
        {
            Blitter.BlitCameraTexture(cmd, source, destination, material, pass);
        }

        /// <summary>
        /// Blit a RTHandle to another RTHandle.
        /// This will properly account for partial usage (in term of resolution) of the texture for the current viewport.
        /// This overload allows user to override the scale and bias used when sampling the input RTHandle.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="destination">Destination RTHandle.</param>
        /// <param name="scaleBias">Scale and bias used to sample the input RTHandle.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public static void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, Vector4 scaleBias, float mipLevel = 0.0f, bool bilinear = false)
        {
            Blitter.BlitCameraTexture(cmd, source, destination, scaleBias, mipLevel, bilinear);
        }

        /// <summary>
        /// Blit a RTHandle to another RTHandle.
        /// This will properly account for partial usage (in term of resolution) of the texture for the current viewport.
        /// This overload allows user to override the viewport of the destination RTHandle.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="destination">Destination RTHandle.</param>
        /// <param name="destViewport">Viewport of the destination RTHandle.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public static void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, Rect destViewport, float mipLevel = 0.0f, bool bilinear = false)
        {
            Blitter.BlitCameraTexture(cmd, source, destination, destViewport, mipLevel, bilinear);
        }

        // These method should be used to render full screen triangles sampling auto-scaling RTs.
        // This will set the proper viewport and UV scale.

        /// <summary>
        /// Draw a full screen triangle with a material.
        /// This will automatically set the viewport of the destination RTHandle based on the current camera parameters.
        /// </summary>
        /// <param name="commandBuffer">Command Buffer used for rendering.</param>
        /// <param name="material">Material used for rendering.</param>
        /// <param name="colorBuffer">Destination RTHandle.</param>
        /// <param name="properties">Optional material property block.</param>
        /// <param name="shaderPassId">Optional pass index to use.</param>
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RTHandle colorBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            CoreUtils.SetRenderTarget(commandBuffer, colorBuffer);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        /// <summary>
        /// Draw a full screen triangle with a material.
        /// This will automatically set the viewport of the destination RTHandle based on the current camera parameters.
        /// </summary>
        /// <param name="commandBuffer">Command Buffer used for rendering.</param>
        /// <param name="material">Material used for rendering.</param>
        /// <param name="colorBuffer">Destination RTHandle.</param>
        /// <param name="depthStencilBuffer">Destination Depth Stencil RTHandle.</param>
        /// <param name="properties">Optional material property block.</param>
        /// <param name="shaderPassId">Optional pass index to use.</param>
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RTHandle colorBuffer, RTHandle depthStencilBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            CoreUtils.SetRenderTarget(commandBuffer, colorBuffer, depthStencilBuffer);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        /// <summary>
        /// Draw a full screen triangle with a material.
        /// This will automatically set the viewport of the destination RTHandle based on the current camera parameters.
        /// </summary>
        /// <param name="commandBuffer">Command Buffer used for rendering.</param>
        /// <param name="material">Material used for rendering.</param>
        /// <param name="colorBuffers">Array of RenderTargetIdentifier for multiple render target rendering.</param>
        /// <param name="depthStencilBuffer">Destination Depth Stencil RTHandle.</param>
        /// <param name="properties">Optional material property block.</param>
        /// <param name="shaderPassId">Optional pass index to use.</param>
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RenderTargetIdentifier[] colorBuffers, RTHandle depthStencilBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            CoreUtils.SetRenderTarget(commandBuffer, colorBuffers, depthStencilBuffer);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        /// <summary>
        /// Draw a full screen triangle with a material.
        /// This will render into the destination texture with the specified viewport.
        /// </summary>
        /// <param name="commandBuffer">Command Buffer used for rendering.</param>
        /// <param name="viewport">Destination viewport.</param>
        /// <param name="material">Material used for rendering.</param>
        /// <param name="destination">Destination RenderTargetIdentifier.</param>
        /// <param name="properties">Optional Material Property block.</param>
        /// <param name="shaderPassId">Optional pass index to use.</param>
        /// <param name="depthSlice">Optional depth slice to render to.</param>
        /// <param name="cubemapFace">Optional cubemap face to render to.</param>
        public static void DrawFullScreen(CommandBuffer commandBuffer, Rect viewport, Material material, RenderTargetIdentifier destination, CubemapFace cubemapFace, MaterialPropertyBlock properties = null, int shaderPassId = 0, int depthSlice = -1)
        {
            CoreUtils.SetRenderTarget(commandBuffer, destination, ClearFlag.None, 0, cubemapFace, depthSlice);
            commandBuffer.SetViewport(viewport);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        /// <summary>
        /// Draw a full screen triangle with a material.
        /// This will render into the destination texture with the specified viewport.
        /// </summary>
        /// <param name="commandBuffer">Command Buffer used for rendering.</param>
        /// <param name="viewport">Destination viewport.</param>
        /// <param name="material">Material used for rendering.</param>
        /// <param name="destination">Destination RenderTargetIdentifier.</param>
        /// <param name="properties">Optional Material Property block.</param>
        /// <param name="shaderPassId">Optional pass index to use.</param>
        /// <param name="depthSlice">Optional depth slice to render to.</param>
        public static void DrawFullScreen(CommandBuffer commandBuffer, Rect viewport, Material material, RenderTargetIdentifier destination, MaterialPropertyBlock properties = null, int shaderPassId = 0, int depthSlice = -1)
        {
            DrawFullScreen(commandBuffer, viewport, material, destination, CubemapFace.Unknown, properties, shaderPassId, depthSlice);
        }

        /// <summary>
        /// Draw a full screen triangle with a material.
        /// This will render into the destination texture with the specified viewport.
        /// </summary>
        /// <param name="commandBuffer">Command Buffer used for rendering.</param>
        /// <param name="viewport">Destination viewport.</param>
        /// <param name="material">Material used for rendering.</param>
        /// <param name="depthStencilBuffer">Destination Depth Stencil RTHandle.</param>
        /// <param name="destination">Destination RenderTargetIdentifier.</param>
        /// <param name="properties">Optional Material Property block.</param>
        /// <param name="shaderPassId">Optional pass index to use.</param>
        public static void DrawFullScreen(CommandBuffer commandBuffer, Rect viewport, Material material,
            RenderTargetIdentifier destination, RTHandle depthStencilBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            CoreUtils.SetRenderTarget(commandBuffer, destination, depthStencilBuffer, ClearFlag.None, 0, CubemapFace.Unknown, -1);
            commandBuffer.SetViewport(viewport);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        // Returns mouse coordinates: (x,y) in pixels and (z,w) normalized inside the render target (not the viewport)
        internal static Vector4 GetMouseCoordinates(HDCamera camera)
        {
            // We request the mouse post based on the type of the camera
            Vector2 mousePixelCoord = MousePositionDebug.instance.GetMousePosition(camera.screenSize.y, camera.camera.cameraType == CameraType.SceneView);
            return new Vector4(mousePixelCoord.x, mousePixelCoord.y, RTHandles.rtHandleProperties.rtHandleScale.x * mousePixelCoord.x / camera.screenSize.x, RTHandles.rtHandleProperties.rtHandleScale.y * mousePixelCoord.y / camera.screenSize.y);
        }

        // Returns mouse click coordinates: (x,y) in pixels and (z,w) normalized inside the render target (not the viewport)
        internal static Vector4 GetMouseClickCoordinates(HDCamera camera)
        {
            Vector2 mousePixelCoord = MousePositionDebug.instance.GetMouseClickPosition(camera.screenSize.y);
            return new Vector4(mousePixelCoord.x, mousePixelCoord.y, RTHandles.rtHandleProperties.rtHandleScale.x * mousePixelCoord.x / camera.screenSize.x, RTHandles.rtHandleProperties.rtHandleScale.y * mousePixelCoord.y / camera.screenSize.y);
        }

        // This function check if camera is a CameraPreview, then check if this preview is a regular preview (i.e not a preview from the camera editor)
        internal static bool IsRegularPreviewCamera(Camera camera)
        {
            if (camera.cameraType == CameraType.Preview)
            {
                camera.TryGetComponent<HDAdditionalCameraData>(out var additionalCameraData);
                return (additionalCameraData == null) || !additionalCameraData.isEditorCameraPreview;
            }
            return false;
        }

        // We need these at runtime for RenderPipelineResources upgrade
        internal static string GetHDRenderPipelinePath()
            => "Packages/com.unity.render-pipelines.high-definition/";

        internal static string GetCorePath()
            => "Packages/com.unity.render-pipelines.core/";

        internal static string GetVFXPath()
            => "Packages/com.unity.visualeffectgraph/";

        // It returns the previously set RenderPipelineAsset, assetWasFromQuality is true if the current asset was set through the quality settings
        internal static RenderPipelineAsset SwitchToBuiltinRenderPipeline(out bool assetWasFromQuality)
        {
            var graphicSettingAsset = GraphicsSettings.defaultRenderPipeline;
            assetWasFromQuality = false;
            if (graphicSettingAsset != null)
            {
                // Check if the currently used pipeline is the one from graphics settings
                if (GraphicsSettings.currentRenderPipeline == graphicSettingAsset)
                {
                    GraphicsSettings.defaultRenderPipeline = null;
                    return graphicSettingAsset;
                }
            }
            // If we are here, it means the asset comes from quality settings
            var assetFromQuality = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = null;
            assetWasFromQuality = true;
            return assetFromQuality;
        }

        // Set the renderPipelineAsset, either on the quality settings if it was unset from there or in GraphicsSettings.
        // IMPORTANT: RenderPipelineManager.currentPipeline won't be HDRP until a camera.Render() call is made.
        internal static void RestoreRenderPipelineAsset(bool wasUnsetFromQuality, RenderPipelineAsset renderPipelineAsset)
        {
            if (wasUnsetFromQuality)
            {
                QualitySettings.renderPipeline = renderPipelineAsset;
            }
            else
            {
                GraphicsSettings.defaultRenderPipeline = renderPipelineAsset;
            }
        }

        internal struct PackedMipChainInfo
        {
            public Vector2Int textureSize;
            public int mipLevelCount; // mips contain min (closest) depth
            public int mipLevelCountCheckerboard;
            public Vector2Int[] mipLevelSizes;
            public Vector2Int[] mipLevelOffsets; // mips contain min (closest) depth
            public Vector2Int[] mipLevelOffsetsCheckerboard;

            private Vector2 cachedTextureScale;
            private Vector2Int cachedHardwareTextureSize;
            private int cachedCheckerboardMipCount;

            private bool m_OffsetBufferWillNeedUpdate;

            public void Allocate()
            {
                mipLevelOffsets = new Vector2Int[15];
                mipLevelOffsetsCheckerboard = new Vector2Int[15];
                mipLevelSizes = new Vector2Int[15];
                m_OffsetBufferWillNeedUpdate = true;
            }

            enum PackDirection
            {
                Right,
                Down,
            }

            static Vector2Int NextMipBegin(Vector2Int prevMipBegin, Vector2Int prevMipSize, PackDirection dir)
            {
                Vector2Int mipBegin = prevMipBegin;
                if (dir == PackDirection.Right)
                    mipBegin.x += prevMipSize.x;
                else
                    mipBegin.y += prevMipSize.y;
                return mipBegin;
            }

            // We pack all MIP levels into the top MIP level to avoid the Pow2 MIP chain restriction.
            // We compute the required size iteratively.
            // This function is NOT fast, but it is illustrative, and can be optimized later.
            public void ComputePackedMipChainInfo(Vector2Int viewportSize, int checkerboardMipCount)
            {
                // only support up to 2 mips of checkerboard data being created
                checkerboardMipCount = Mathf.Clamp(checkerboardMipCount, 0, 2);

                bool isHardwareDrsOn = DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled();
                Vector2Int hardwareTextureSize = isHardwareDrsOn ? DynamicResolutionHandler.instance.ApplyScalesOnSize(viewportSize) : viewportSize;
                Vector2 textureScale = isHardwareDrsOn ? new Vector2((float)viewportSize.x / (float)hardwareTextureSize.x, (float)viewportSize.y / (float)hardwareTextureSize.y) : new Vector2(1.0f, 1.0f);

                // We need to mark the buffer dirty in case another camera has a different viewport size
                m_OffsetBufferWillNeedUpdate = true;

                // No work needed.
                if (cachedHardwareTextureSize == hardwareTextureSize && cachedTextureScale == textureScale && cachedCheckerboardMipCount == checkerboardMipCount)
                    return;

                cachedHardwareTextureSize = hardwareTextureSize;
                cachedTextureScale = textureScale;
                cachedCheckerboardMipCount = checkerboardMipCount;

                mipLevelSizes[0] = hardwareTextureSize;
                mipLevelOffsets[0] = Vector2Int.zero;
                mipLevelOffsetsCheckerboard[0] = mipLevelOffsets[0];

                int mipLevel = 0;
                Vector2Int mipSize = hardwareTextureSize;
                bool hasCheckerboard = (checkerboardMipCount != 0);
                int maxCheckboardLevelCount = hasCheckerboard ? (1 + checkerboardMipCount) : 0;
                do
                {
                    mipLevel++;

                    // Round up.
                    mipSize.x = Math.Max(1, (mipSize.x + 1) >> 1);
                    mipSize.y = Math.Max(1, (mipSize.y + 1) >> 1);

                    mipLevelSizes[mipLevel] = mipSize;

                    Vector2Int prevMipSize = mipLevelSizes[mipLevel - 1];
                    Vector2Int prevMipBegin = mipLevelOffsets[mipLevel - 1];
                    Vector2Int prevMipBeginCheckerboard = mipLevelOffsetsCheckerboard[mipLevel - 1];

                    Vector2Int mipBegin = prevMipBegin;
                    Vector2Int mipBeginCheckerboard = prevMipBeginCheckerboard;
                    if (mipLevel == 1)
                    {
                        // first mip always below full resolution
                        mipBegin = NextMipBegin(prevMipBegin, prevMipSize, PackDirection.Down);

                        // pack checkerboard next to it if present
                        if (hasCheckerboard)
                            mipBeginCheckerboard = NextMipBegin(mipBegin, mipSize, PackDirection.Right);
                        else
                            mipBeginCheckerboard = mipBegin;
                    }
                    else
                    {
                        // alternate directions, mip 2 starts with down if checkerboard, right if not
                        bool isOdd = ((mipLevel & 1) != 0);
                        PackDirection dir = (isOdd ^ hasCheckerboard) ? PackDirection.Down : PackDirection.Right;

                        mipBegin = NextMipBegin(prevMipBegin, prevMipSize, dir);
                        mipBeginCheckerboard = NextMipBegin(prevMipBeginCheckerboard, prevMipSize, dir);
                    }

                    mipLevelOffsets[mipLevel] = mipBegin;
                    mipLevelOffsetsCheckerboard[mipLevel] = mipBeginCheckerboard;

                    hardwareTextureSize.x = Math.Max(hardwareTextureSize.x, mipBegin.x + mipSize.x);
                    hardwareTextureSize.y = Math.Max(hardwareTextureSize.y, mipBegin.y + mipSize.y);
                    hardwareTextureSize.x = Math.Max(hardwareTextureSize.x, mipBeginCheckerboard.x + mipSize.x);
                    hardwareTextureSize.y = Math.Max(hardwareTextureSize.y, mipBeginCheckerboard.y + mipSize.y);
                }
                while ((mipSize.x > 1) || (mipSize.y > 1));

                textureSize = new Vector2Int(
                    (int)Mathf.Ceil((float)hardwareTextureSize.x * textureScale.x), (int)Mathf.Ceil((float)hardwareTextureSize.y * textureScale.y));

                mipLevelCount = mipLevel + 1;
                mipLevelCountCheckerboard = hasCheckerboard ? (1 + checkerboardMipCount) : 0;
            }

            public ComputeBuffer GetOffsetBufferData(ComputeBuffer mipLevelOffsetsBuffer)
            {
                if (m_OffsetBufferWillNeedUpdate)
                {
                    mipLevelOffsetsBuffer.SetData(mipLevelOffsets);
                    m_OffsetBufferWillNeedUpdate = false;
                }

                return mipLevelOffsetsBuffer;
            }
        }

        internal static int DivRoundUp(int x, int y) => (x + y - 1) / y;

        internal static Vector2Int DivRoundUp(Vector2Int n, int d) => new Vector2Int(HDUtils.DivRoundUp(n.x, d), HDUtils.DivRoundUp(n.y, d));
        internal static Vector2Int DivRoundUp(Vector2Int n, Vector2Int d) => new Vector2Int(HDUtils.DivRoundUp(n.x, d.x), HDUtils.DivRoundUp(n.y, d.y));
        internal static Vector3Int DivRoundUp(Vector3Int n, int d) => new Vector3Int(HDUtils.DivRoundUp(n.x, d), HDUtils.DivRoundUp(n.y, d), HDUtils.DivRoundUp(n.z, d));

        internal static bool IsQuaternionValid(Quaternion q)
            => (q[0] * q[0] + q[1] * q[1] + q[2] * q[2] + q[3] * q[3]) > float.Epsilon;

        internal static void CheckRTCreated(RenderTexture rt)
        {
            // In some cases when loading a project for the first time in the editor, the internal resource is destroyed.
            // When used as render target, the C++ code will re-create the resource automatically. Since here it's used directly as an UAV, we need to check manually
            if (!rt.IsCreated())
                rt.Create();
        }

        internal static float ComputeViewportScale(int viewportSize, int bufferSize)
        {
            float rcpBufferSize = 1.0f / bufferSize;

            // Scale by (vp_dim / buf_dim).
            return viewportSize * rcpBufferSize;
        }

        internal static float ComputeViewportLimit(int viewportSize, int bufferSize)
        {
            float rcpBufferSize = 1.0f / bufferSize;

            // Clamp to (vp_dim - 0.5) / buf_dim.
            return (viewportSize - 0.5f) * rcpBufferSize;
        }

        internal static Vector4 ComputeViewportScaleAndLimit(Vector2Int viewportSize, Vector2Int bufferSize)
        {
            return new Vector4(ComputeViewportScale(viewportSize.x, bufferSize.x),  // Scale(x)
                ComputeViewportScale(viewportSize.y, bufferSize.y),                 // Scale(y)
                ComputeViewportLimit(viewportSize.x, bufferSize.x),                 // Limit(x)
                ComputeViewportLimit(viewportSize.y, bufferSize.y));                // Limit(y)
        }

        // Note: If you add new platform in this function, think about adding support in IsSupportedBuildTarget() function below
        internal static bool IsSupportedGraphicDevice(GraphicsDeviceType graphicDevice)
        {
            if (graphicDevice == GraphicsDeviceType.Switch) // Switch support only enabled when forced by env variable for CI
                return Environment.GetEnvironmentVariable("ENABLE_HDRP_SWITCH_SUPPORT") != null || Application.platform == RuntimePlatform.Switch;

            return (graphicDevice == GraphicsDeviceType.Direct3D11 ||
                graphicDevice == GraphicsDeviceType.Direct3D12 ||
                graphicDevice == GraphicsDeviceType.PlayStation4 ||
                graphicDevice == GraphicsDeviceType.PlayStation5 ||
                graphicDevice == GraphicsDeviceType.PlayStation5NGGC ||
                graphicDevice == GraphicsDeviceType.XboxOne ||
                graphicDevice == GraphicsDeviceType.XboxOneD3D12 ||
                graphicDevice == GraphicsDeviceType.GameCoreXboxOne ||
                graphicDevice == GraphicsDeviceType.GameCoreXboxSeries ||
                graphicDevice == GraphicsDeviceType.Metal ||
                graphicDevice == GraphicsDeviceType.Vulkan);
        }

        internal static bool IsHardwareDynamicResolutionSupportedByDevice(GraphicsDeviceType deviceType)
        {
            // TODO: This information should be exposed through the SystemInfo interface
            return (deviceType != GraphicsDeviceType.Direct3D11 &&
                deviceType != GraphicsDeviceType.OpenGLES3 &&
                deviceType != GraphicsDeviceType.OpenGLCore &&
                deviceType != GraphicsDeviceType.WebGPU);
        }

#if UNITY_EDITOR
        // This function can't be in HDEditorUtils because we need it in HDRenderPipeline.cs (and HDEditorUtils is in an editor asmdef)
        internal static bool IsSupportedBuildTarget(UnityEditor.BuildTarget buildTarget)
        {
            if (buildTarget == UnityEditor.BuildTarget.Switch) // Switch support only enabled when forced by env variable for CI
                return Environment.GetEnvironmentVariable("ENABLE_HDRP_SWITCH_SUPPORT") != null;
            return (buildTarget == UnityEditor.BuildTarget.StandaloneWindows ||
                buildTarget == UnityEditor.BuildTarget.StandaloneWindows64 ||
                buildTarget == UnityEditor.BuildTarget.StandaloneLinux64 ||
                buildTarget == UnityEditor.BuildTarget.StandaloneOSX ||
                buildTarget == UnityEditor.BuildTarget.WSAPlayer ||
                buildTarget == UnityEditor.BuildTarget.XboxOne ||
                buildTarget == UnityEditor.BuildTarget.GameCoreXboxOne ||
                buildTarget == UnityEditor.BuildTarget.GameCoreXboxSeries  ||
                buildTarget == UnityEditor.BuildTarget.PS4 ||
                buildTarget == UnityEditor.BuildTarget.PS5 ||
                // buildTarget == UnityEditor.BuildTarget.iOS || // IOS isn't supported
                buildTarget == UnityEditor.BuildTarget.LinuxHeadlessSimulation);
        }

        internal static bool AreGraphicsAPIsSupported(UnityEditor.BuildTarget target, ref GraphicsDeviceType unsupportedGraphicDevice)
        {
            bool editor = false;
#if UNITY_EDITOR
            editor = !UnityEditor.BuildPipeline.isBuildingPlayer;
#endif

            if (editor)  // In the editor we use the current graphics device instead of the list to avoid blocking the rendering if an invalid API is added but not enabled.
            {
                return HDUtils.IsSupportedGraphicDevice(SystemInfo.graphicsDeviceType);
            }
            else
            {
                foreach (var graphicAPI in UnityEditor.PlayerSettings.GetGraphicsAPIs(target))
                {
                    if (!HDUtils.IsSupportedGraphicDevice(graphicAPI))
                    {
                        unsupportedGraphicDevice = graphicAPI;
                        return false;
                    }
                }
            }
            return true;
        }

        internal static OperatingSystemFamily BuildTargetToOperatingSystemFamily(UnityEditor.BuildTarget target)
        {
            switch (target)
            {
                case UnityEditor.BuildTarget.StandaloneOSX:
                    return OperatingSystemFamily.MacOSX;
                case UnityEditor.BuildTarget.StandaloneWindows:
                case UnityEditor.BuildTarget.StandaloneWindows64:
                    return OperatingSystemFamily.Windows;
                case UnityEditor.BuildTarget.StandaloneLinux64:
                    return OperatingSystemFamily.Linux;
                default:
                    return OperatingSystemFamily.Other;
            }
        }

        internal static bool IsSupportedBuildTargetAndDevice(UnityEditor.BuildTarget activeBuildTarget, out GraphicsDeviceType unsupportedGraphicDevice)
        {
            GraphicsDeviceType systemGraphicsDeviceType = SystemInfo.graphicsDeviceType;
            unsupportedGraphicDevice = systemGraphicsDeviceType;

            // If the build target matches the operating system of the editor
            // and if the graphic api is chosen automatically, then only the system's graphic device type matters
            // otherwise, we need to iterate over every graphic api available in the list to track every non-supported APIs
            // if the build target does not match the editor OS, then we have to check using the graphic api list
            bool autoAPI = UnityEditor.PlayerSettings.GetUseDefaultGraphicsAPIs(activeBuildTarget) && (SystemInfo.operatingSystemFamily == HDUtils.BuildTargetToOperatingSystemFamily(activeBuildTarget));

            // If the editor's graphics device type is null though, we still have to iterate the target's graphic api list.
            bool skipCheckingAPIList = autoAPI && systemGraphicsDeviceType != GraphicsDeviceType.Null;

            return skipCheckingAPIList
                ? HDUtils.IsSupportedGraphicDevice(SystemInfo.graphicsDeviceType)
                : HDUtils.AreGraphicsAPIsSupported(activeBuildTarget, ref unsupportedGraphicDevice)
                  && HDUtils.IsSupportedBuildTarget(activeBuildTarget)
                  && HDUtils.IsOperatingSystemSupported(SystemInfo.operatingSystem);
        }

#endif

        internal static bool IsMacOSVersionAtLeast(string os, int majorVersion, int minorVersion, int patchVersion)
        {
            int startIndex = os.LastIndexOf(" ");
            var parts = os.Substring(startIndex + 1).Split('.');
            int currentMajorVersion = Convert.ToInt32(parts[0]);
            int currentMinorVersion = Convert.ToInt32(parts[1]);
            int currentPatchVersion = Convert.ToInt32(parts[2]);

            if (currentMajorVersion < majorVersion) return false;
            if (currentMajorVersion > majorVersion) return true;
            if (currentMinorVersion < minorVersion) return false;
            if (currentMinorVersion > minorVersion) return true;
            if (currentPatchVersion < patchVersion) return false;
            if (currentPatchVersion > patchVersion) return true;
            return true;
        }

        internal static bool IsOperatingSystemSupported(string os)
        {
            // Metal support depends on OS version:
            // macOS 10.11.x doesn't have tessellation / earlydepthstencil support, early driver versions were buggy in general
            // macOS 10.12.x should usually work with AMD, but issues with Intel/Nvidia GPUs. Regardless of the GPU, there are issues with MTLCompilerService crashing with some shaders
            // macOS 10.13.x should work, but active development tests against current OS
            //
            // Has worked experimentally with iOS in the past, but it's not currently supported
            //

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
            {
                if (os.StartsWith("Mac") && !IsMacOSVersionAtLeast(os, 10, 13, 0))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Extract scale and bias from a fade distance to achieve a linear fading starting at 90% of the fade distance.
        /// </summary>
        /// <param name="fadeDistance">Distance at which object should be totally fade</param>
        /// <param name="scale">[OUT] Slope of the fading on the fading part</param>
        /// <param name="bias">[OUT] Ordinate of the fading part at abscissa 0</param>
        internal static void GetScaleAndBiasForLinearDistanceFade(float fadeDistance, out float scale, out float bias)
        {
            // Fade with distance calculation is just a linear fade from 90% of fade distance to fade distance. 90% arbitrarily chosen but should work well enough.
            float distanceFadeNear = 0.9f * fadeDistance;
            scale = 1.0f / (fadeDistance - distanceFadeNear);
            bias = -distanceFadeNear / (fadeDistance - distanceFadeNear);
        }

        /// <summary>
        /// Compute the linear fade distance
        /// </summary>
        /// <param name="distanceToCamera">Distance from the object to fade from the camera</param>
        /// <param name="fadeDistance">Distance at witch the object is totally faded</param>
        /// <returns>Computed fade factor</returns>
        internal static float ComputeLinearDistanceFade(float distanceToCamera, float fadeDistance)
        {
            float scale;
            float bias;
            GetScaleAndBiasForLinearDistanceFade(fadeDistance, out scale, out bias);

            return 1.0f - Mathf.Clamp01(distanceToCamera * scale + bias);
        }

        /// <summary>
        /// Compute the linear fade distance between two position with an additional weight multiplier
        /// </summary>
        /// <param name="position1">Object/camera position</param>
        /// <param name="position2">Camera/object position</param>
        /// <param name="weight">Weight multiplior</param>
        /// <param name="fadeDistance">Distance at witch the object is totally faded</param>
        /// <returns>Computed fade factor</returns>
        internal static float ComputeWeightedLinearFadeDistance(Vector3 position1, Vector3 position2, float weight, float fadeDistance)
        {
            float distanceToCamera = Vector3.Magnitude(position1 - position2);
            float distanceFade = ComputeLinearDistanceFade(distanceToCamera, fadeDistance);
            return distanceFade * weight;
        }

        internal static bool WillCustomPassBeExecuted(HDCamera hdCamera, CustomPassInjectionPoint injectionPoint)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.CustomPass))
                return false;

            bool executed = false;
            CustomPassVolume.GetActivePassVolumes(injectionPoint, m_TempCustomPassVolumeList);
            foreach (var customPassVolume in m_TempCustomPassVolumeList)
            {
                if (customPassVolume == null)
                    return false;

                executed |= customPassVolume.WillExecuteInjectionPoint(hdCamera);
            }

            return executed;
        }

        internal static bool PostProcessIsFinalPass(HDCamera hdCamera, AOVRequestData aovRequest)
        {
            // Post process pass is the final blit only when not in developer mode.
            // In developer mode, we support a range of debug rendering that needs to occur after post processes.
            // In order to simplify writing them, we don't Y-flip in the post process pass but add a final blit at the end of the frame.
            return !aovRequest.isValid && !Debug.isDebugBuild && !WillCustomPassBeExecuted(hdCamera, CustomPassInjectionPoint.AfterPostProcess) && !hdCamera.hasCaptureActions;
        }

        // These two convertion functions are used to store GUID assets inside materials,
        // a unity asset GUID is exactly 16 bytes long which is also a Vector4 so by adding a
        // Vector4 field inside the shader we can store references of an asset inside the material
        // which is actually used to store the reference of the diffusion profile asset
        internal static Vector4 ConvertGUIDToVector4(string guid)
        {
            Vector4 vector;
            byte[] bytes = new byte[16];

            for (int i = 0; i < 16; i++)
                bytes[i] = byte.Parse(guid.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);

            unsafe
            {
                fixed(byte* b = bytes)
                vector = *(Vector4*)b;
            }

            return vector;
        }

        internal static string ConvertVector4ToGUID(Vector4 vector)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            unsafe
            {
                byte* v = (byte*)&vector;
                for (int i = 0; i < 16; i++)
                    sb.Append(v[i].ToString("x2"));
                var guidBytes = new byte[16];
                System.Runtime.InteropServices.Marshal.Copy((IntPtr)v, guidBytes, 0, 16);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Normalize the input color.
        /// </summary>
        /// <param name="color">Input color.</param>
        /// <returns>Normalized color.</returns>
        public static Color NormalizeColor(Color color)
        {
            Vector4 ldrColor = Vector4.Max(color, Vector4.one * 0.0001f);
            color = (ldrColor / ColorUtils.Luminance(ldrColor));
            color.a = 1;

            return color;
        }

        /// <summary>
        /// Draw a renderer list.
        /// </summary>
        /// <param name="renderContext">Current Scriptable Render Context.</param>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="rendererList">Renderer List to render.</param>
        [Obsolete("Please use CoreUtils.DrawRendererList instead. #from(2021.1)")]
        public static void DrawRendererList(ScriptableRenderContext renderContext, CommandBuffer cmd, UnityEngine.Rendering.RendererList rendererList)
        {
            CoreUtils.DrawRendererList(renderContext, cmd, rendererList);
        }

        // $"HDProbe RenderCamera ({probeName}: {face:00} for viewer '{viewerName}')"
        internal unsafe static string ComputeProbeCameraName(string probeName, int face, string viewerName)
        {
            // Interpolate the camera name with as few allocation as possible
            const string pattern1 = "HDProbe RenderCamera (";
            const string pattern2 = ": ";
            const string pattern3 = " for viewer '";
            const string pattern4 = "')";
            const int maxCharCountPerName = 40;
            const int charCountPerNumber = 2;

            probeName = probeName ?? string.Empty;
            viewerName = viewerName ?? "null";

            var probeNameSize = Mathf.Min(probeName.Length, maxCharCountPerName);
            var viewerNameSize = Mathf.Min(viewerName.Length, maxCharCountPerName);
            int size = pattern1.Length + probeNameSize
                + pattern2.Length + charCountPerNumber
                + pattern3.Length + viewerNameSize
                + pattern4.Length;

            var buffer = stackalloc char[size];
            var p = buffer;
            int i, c, s = 0;
            for (i = 0; i < pattern1.Length; ++i, ++p)
                *p = pattern1[i];
            for (i = 0, c = Mathf.Min(probeName.Length, maxCharCountPerName); i < c; ++i, ++p)
                *p = probeName[i];
            s += c;
            for (i = 0; i < pattern2.Length; ++i, ++p)
                *p = pattern2[i];

            // Fast, no-GC index.ToString("2")
            var temp = (face * 205) >> 11;  // 205/2048 is nearly the same as /10
            *(p++) = (char)(temp + '0');
            *(p++) = (char)((face - temp * 10) + '0');
            s += charCountPerNumber;

            for (i = 0; i < pattern3.Length; ++i, ++p)
                *p = pattern3[i];
            for (i = 0, c = Mathf.Min(viewerName.Length, maxCharCountPerName); i < c; ++i, ++p)
                *p = viewerName[i];
            s += c;
            for (i = 0; i < pattern4.Length; ++i, ++p)
                *p = pattern4[i];

            s += pattern1.Length + pattern2.Length + pattern3.Length + pattern4.Length;
            return new string(buffer, 0, s);
        }

        // $"HDRenderPipeline::Render {cameraName}"
        internal unsafe static string ComputeCameraName(string cameraName)
        {
            // Interpolate the camera name with as few allocation as possible
            const string pattern1 = "HDRenderPipeline::Render ";
            const int maxCharCountPerName = 40;

            var cameraNameSize = Mathf.Min(cameraName.Length, maxCharCountPerName);
            int size = pattern1.Length + cameraNameSize;

            var buffer = stackalloc char[size];
            var p = buffer;
            int i, c, s = 0;
            for (i = 0; i < pattern1.Length; ++i, ++p)
                *p = pattern1[i];
            for (i = 0, c = cameraNameSize; i < c; ++i, ++p)
                *p = cameraName[i];
            s += c;

            s += pattern1.Length;
            return new string(buffer, 0, s);
        }

        internal static float ClampFOV(float fov) => Mathf.Clamp(fov, 0.00001f, 179);

        internal static UInt64 GetSceneCullingMaskFromCamera(Camera camera)
        {
#if UNITY_EDITOR
            if (camera.overrideSceneCullingMask != 0)
                return camera.overrideSceneCullingMask;

            if (camera.scene.IsValid())
                return EditorSceneManager.GetSceneCullingMask(camera.scene);

#if UNITY_2020_1_OR_NEWER
            switch (camera.cameraType)
            {
                case CameraType.SceneView:
                    return SceneCullingMasks.MainStageSceneViewObjects;
                default:
                    return SceneCullingMasks.GameViewObjects;
            }
#else
            return 0;
#endif
#else
            return 0;
#endif
        }

        internal static HDAdditionalCameraData TryGetAdditionalCameraDataOrDefault(Camera camera)
        {
            if (camera == null || camera.Equals(null))
                return s_DefaultHDAdditionalCameraData;

            if (camera.TryGetComponent<HDAdditionalCameraData>(out var hdCamera))
                return hdCamera;

            return s_DefaultHDAdditionalCameraData;
        }

        static Dictionary<GraphicsFormat, int> graphicsFormatSizeCache = new Dictionary<GraphicsFormat, int>
        {
            // Init some default format so we don't allocate more memory on the first frame.
            {GraphicsFormat.R8G8B8A8_UNorm, 4},
            {GraphicsFormat.R16G16B16A16_SFloat, 8},
            {GraphicsFormat.RGB_BC6H_SFloat, 1}, // BC6H uses 128 bits for each 4x4 tile which is 8 bits per pixel
        };

        /// <summary>
        /// Compute the size in bytes of a GraphicsFormat. Does not works with compressed formats.
        /// </summary>
        /// <param name="format"></param>
        /// <returns>Size in Bytes</returns>
        internal static int GetFormatSizeInBytes(GraphicsFormat format)
        {
            if (graphicsFormatSizeCache.TryGetValue(format, out var size))
                return size;

            // Compute the size by parsing the enum name: Note that it does not works with compressed formats
            string name = format.ToString();
            int underscoreIndex = name.IndexOf('_');
            name = name.Substring(0, underscoreIndex == -1 ? name.Length : underscoreIndex);

            // Extract all numbers from the format name:
            int bits = 0;
            foreach (Match m in Regex.Matches(name, @"\d+"))
                bits += int.Parse(m.Value);

            size = bits / 8;
            graphicsFormatSizeCache[format] = size;
            return size;
        }

        internal static void DisplayMessageNotification(string msg)
        {
            Debug.LogError(msg);

#if UNITY_EDITOR
            foreach (UnityEditor.SceneView sv in UnityEditor.SceneView.sceneViews)
                sv.ShowNotification(new GUIContent(msg));
#endif
        }

        internal static string GetUnsupportedAPIMessage(string graphicAPI)
        {
            // If we are in the editor they are many possible targets that does not matches the current OS so we use the active build target instead
#if UNITY_EDITOR
            var buildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            string currentPlatform = buildTarget.ToString();
            var osFamily = BuildTargetToOperatingSystemFamily(buildTarget);
            bool isSupportedBuildTarget = IsSupportedBuildTarget(buildTarget);
#else
            string currentPlatform = SystemInfo.operatingSystem;
            var osFamily = SystemInfo.operatingSystemFamily;
            bool isSupportedBuildTarget = true;
#endif

            string os = null;
            switch (osFamily)
            {
                case OperatingSystemFamily.MacOSX:
                    os = "Mac";
                    break;
                case OperatingSystemFamily.Windows:
                    os = "Windows";
                    break;
                case OperatingSystemFamily.Linux:
                    os = "Linux";
                    break;
            }

            string msg;

            if (isSupportedBuildTarget)
                msg = "Platform " + currentPlatform + " with graphics API " + graphicAPI + " is not supported with HDRP";
            else
                msg = "Platform " + currentPlatform + " is not supported with HDRP";

            // Display more information to the users when it should have use Metal instead of OpenGL
            if (graphicAPI.StartsWith("OpenGL"))
            {
                if (SystemInfo.operatingSystem.StartsWith("Mac"))
                    msg += ", use the Metal graphics API instead";
                else if (SystemInfo.operatingSystem.StartsWith("Windows"))
                    msg += ", use the Vulkan graphics API instead";
            }

            msg += ".\nChange the platform/device to a compatible one or remove incompatible graphics APIs.\n";
            if (os != null)
                msg += "To do this, go to Project Settings > Player > Other Settings and modify the Graphics APIs for " + os + " list.";

            return msg;
        }

        internal static int GetTextureHash(Texture texture)
        {
            return CoreUtils.GetTextureHash(texture);
        }

        internal static void ReleaseComponentSingletons()
        {
            ComponentSingleton<HDAdditionalReflectionData>.Release();
            ComponentSingleton<HDAdditionalLightData>.Release();
            ComponentSingleton<HDAdditionalCameraData>.Release();
        }

        internal static float InterpolateOrientation(float fromValue, float toValue, float t)
        {
            // Compute the direct distance
            float directDistance = Mathf.Abs(toValue - fromValue);
            float outputValue = 0.0f;

            // Handle the two cases
            if (fromValue < toValue)
            {
                float upperRange = 360.0f - toValue;
                float lowerRange = fromValue;
                float alternativeDistance = upperRange + lowerRange;
                if (alternativeDistance < directDistance)
                {
                    float targetValue = toValue - 360.0f;
                    outputValue = fromValue + (targetValue - fromValue) * t;
                    if (outputValue < 0.0f)
                        outputValue += 360.0f;
                }
                else
                {
                    outputValue = fromValue + (toValue - fromValue) * t;
                }
            }
            else
            {
                float upperRange = 360.0f - fromValue;
                float lowerRange = toValue;
                float alternativeDistance = upperRange + lowerRange;
                if (alternativeDistance < directDistance)
                {
                    float targetValue = toValue + 360.0f;
                    outputValue = fromValue + (targetValue - fromValue) * t;
                    if (outputValue > 360.0f)
                        outputValue -= 360.0f;
                }
                else
                {
                    outputValue = fromValue + (toValue - fromValue) * t;
                }
            }

            return outputValue;
        }

        internal static void ConvertHDRColorToLDR(Color hdr, out Color ldr, out float intensity)
        {
            // specifies the max byte value to use when decomposing a float color into bytes with exposure
            // this is the value used by Photoshop
            const float k_MaxByteForOverexposedColor = 191;

            hdr.a = 1.0f;
            ldr = hdr;
            intensity = 1.0f;

            var maxColorComponent = hdr.maxColorComponent;
            if (maxColorComponent != 0f)
            {
                // calibrate exposure to the max float color component
                var scaleFactor = k_MaxByteForOverexposedColor / maxColorComponent;

                ldr.r = Mathf.Min(k_MaxByteForOverexposedColor, scaleFactor * hdr.r) / 255f;
                ldr.g = Mathf.Min(k_MaxByteForOverexposedColor, scaleFactor * hdr.g) / 255f;
                ldr.b = Mathf.Min(k_MaxByteForOverexposedColor, scaleFactor * hdr.b) / 255f;

                intensity = 255f / scaleFactor;
            }
        }
    }
}
