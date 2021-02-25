using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Assertions;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    public class Blitter : IDisposable
    {
        Material m_Blit;
        Material m_BlitTexArray;
        Material m_BlitTexArraySingleSlice;
        Material m_BlitColorAndDepth;

        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

        private static class BlitShaderIDs
        {
            public static readonly int _BlitTexture     = Shader.PropertyToID("_BlitTexture");
            public static readonly int _BlitScaleBias   = Shader.PropertyToID("_BlitScaleBias");
            public static readonly int _BlitScaleBiasRt = Shader.PropertyToID("_BlitScaleBiasRt");
            public static readonly int _BlitMipLevel    = Shader.PropertyToID("_BlitMipLevel");
            public static readonly int _BlitTextureSize = Shader.PropertyToID("_BlitTextureSize");
            public static readonly int _BlitPaddingSize = Shader.PropertyToID("_BlitPaddingSize");
            public static readonly int _InputDepth      = Shader.PropertyToID("_InputDepthTexture");
        }

        public Blitter(Shader blitPS, Shader blitColorAndDepthPS)
        {
            m_Blit = CoreUtils.CreateEngineMaterial(blitPS);
            m_BlitColorAndDepth = CoreUtils.CreateEngineMaterial(blitColorAndDepthPS);

            // With texture array enabled, we still need the normal blit version for other systems like atlas
            if (TextureXR.useTexArray)
            {
                m_Blit.EnableKeyword("DISABLE_TEXTURE2D_X_ARRAY");
                m_BlitTexArray = CoreUtils.CreateEngineMaterial(blitPS);
                m_BlitTexArraySingleSlice = CoreUtils.CreateEngineMaterial(blitColorAndDepthPS);
                m_BlitTexArraySingleSlice.EnableKeyword("BLIT_SINGLE_SLICE");
            }
        }

        public void Dispose() => Dispose(true);
        protected virtual void Dispose(bool isDisposing)
        {
            CoreUtils.Destroy(m_Blit);
            CoreUtils.Destroy(m_BlitTexArray);
            CoreUtils.Destroy(m_BlitTexArraySingleSlice);
        }

        /// <summary>
        /// Returns the default blit material.
        /// </summary>
        /// <param name="dimension">Dimension of the texture to blit, either 2D or 2D Array.</param>
        /// <param name="singleSlice">Blit only a single slice of the array if applicable.</param>
        /// <returns></returns>
        public Material GetBlitMaterial(TextureDimension dimension, bool singleSlice = false)
        {
            bool useTexArray = dimension == TextureDimension.Tex2DArray;
            return useTexArray ? (singleSlice ? m_BlitTexArraySingleSlice : m_BlitTexArray) : m_Blit;
        }

        /// <summary>
        /// Blit a RTHandle texture.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public void BlitTexture(CommandBuffer cmd, RTHandle source, Vector4 scaleBias, float mipLevel, bool bilinear)
        {
            m_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevel);
            BlitTexture(cmd, source, scaleBias, GetBlitMaterial(TextureXR.dimension), bilinear ? 1 : 0);
        }

        /// <summary>
        /// Blit a RTHandle texture 2D.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public void BlitTexture2D(CommandBuffer cmd, RTHandle source, Vector4 scaleBias, float mipLevel, bool bilinear)
        {
            m_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevel);
            BlitTexture(cmd, source, scaleBias, GetBlitMaterial(TextureDimension.Tex2D), bilinear ? 1 : 0);
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
        public void BlitColorAndDepth(CommandBuffer cmd, Texture sourceColor, RenderTexture sourceDepth, Vector4 scaleBias, float mipLevel, bool blitDepth)
        {
            m_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevel);
            m_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBias);
            m_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, sourceColor);
            if (blitDepth)
                m_PropertyBlock.SetTexture(BlitShaderIDs._InputDepth, sourceDepth, RenderTextureSubElement.Depth);
            cmd.DrawProcedural(Matrix4x4.identity, m_BlitColorAndDepth, blitDepth ? 1 : 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }

        /// <summary>
        /// Blit a RTHandle texture
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="material">Material to invoke when blitting.</param>
        /// <param name="pass">Pass idx within the material to invoke.</param>
        public void BlitTexture(CommandBuffer cmd, RTHandle source, Vector4 scaleBias, Material material, int pass)
        {
            m_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBias);
            m_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
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
        public void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, float mipLevel = 0.0f, bool bilinear = false)
        {
            Vector2 viewportScale = new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y);
            // Will set the correct camera viewport as well.
            CoreUtils.SetRenderTarget(cmd, destination);
            BlitTexture(cmd, source, viewportScale, mipLevel, bilinear);
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
        public void BlitCameraTexture2D(CommandBuffer cmd, RTHandle source, RTHandle destination, float mipLevel = 0.0f, bool bilinear = false)
        {
            Vector2 viewportScale = new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y);
            // Will set the correct camera viewport as well.
            CoreUtils.SetRenderTarget(cmd, destination);
            BlitTexture2D(cmd, source, viewportScale, mipLevel, bilinear);
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
        public void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, Material material, int pass)
        {
            Vector2 viewportScale = new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y);
            // Will set the correct camera viewport as well.
            CoreUtils.SetRenderTarget(cmd, destination);
            BlitTexture(cmd, source, viewportScale, material, pass);
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
        public void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, Vector4 scaleBias, float mipLevel = 0.0f, bool bilinear = false)
        {
            // Will set the correct camera viewport as well.
            CoreUtils.SetRenderTarget(cmd, destination);
            BlitTexture(cmd, source, scaleBias, mipLevel, bilinear);
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
        public void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, Rect destViewport, float mipLevel = 0.0f, bool bilinear = false)
        {
            Vector2 viewportScale = new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y);
            CoreUtils.SetRenderTarget(cmd, destination);
            cmd.SetViewport(destViewport);
            BlitTexture(cmd, source, viewportScale, mipLevel, bilinear);
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
        public void BlitQuad(CommandBuffer cmd, Texture source, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear)
        {
            m_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            m_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            m_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            m_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);

            cmd.DrawProcedural(Matrix4x4.identity, GetBlitMaterial(source.dimension), bilinear ? 3 : 2, MeshTopology.Quads, 4, 1, m_PropertyBlock);
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
        public void BlitQuadWithPadding(CommandBuffer cmd, Texture source, Vector2 textureSize, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear, int paddingInPixels)
        {
            m_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            m_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            m_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            m_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            m_PropertyBlock.SetVector(BlitShaderIDs._BlitTextureSize, textureSize);
            m_PropertyBlock.SetInt(BlitShaderIDs._BlitPaddingSize, paddingInPixels);
            if (source.wrapMode == TextureWrapMode.Repeat)
                cmd.DrawProcedural(Matrix4x4.identity, GetBlitMaterial(source.dimension), bilinear ? 7 : 6, MeshTopology.Quads, 4, 1, m_PropertyBlock);
            else
                cmd.DrawProcedural(Matrix4x4.identity, GetBlitMaterial(source.dimension), bilinear ? 5 : 4, MeshTopology.Quads, 4, 1, m_PropertyBlock);
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
        public void BlitQuadWithPaddingMultiply(CommandBuffer cmd, Texture source, Vector2 textureSize, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear, int paddingInPixels)
        {
            m_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            m_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            m_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            m_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            m_PropertyBlock.SetVector(BlitShaderIDs._BlitTextureSize, textureSize);
            m_PropertyBlock.SetInt(BlitShaderIDs._BlitPaddingSize, paddingInPixels);
            if (source.wrapMode == TextureWrapMode.Repeat)
                cmd.DrawProcedural(Matrix4x4.identity, GetBlitMaterial(source.dimension), bilinear ? 12 : 11, MeshTopology.Quads, 4, 1, m_PropertyBlock);
            else
                cmd.DrawProcedural(Matrix4x4.identity, GetBlitMaterial(source.dimension), bilinear ? 10 : 9, MeshTopology.Quads, 4, 1, m_PropertyBlock);
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
        public void BlitOctahedralWithPadding(CommandBuffer cmd, Texture source, Vector2 textureSize, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear, int paddingInPixels)
        {
            m_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            m_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            m_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            m_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            m_PropertyBlock.SetVector(BlitShaderIDs._BlitTextureSize, textureSize);
            m_PropertyBlock.SetInt(BlitShaderIDs._BlitPaddingSize, paddingInPixels);
            cmd.DrawProcedural(Matrix4x4.identity, GetBlitMaterial(source.dimension), 8, MeshTopology.Quads, 4, 1, m_PropertyBlock);
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
        public void BlitOctahedralWithPaddingMultiply(CommandBuffer cmd, Texture source, Vector2 textureSize, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear, int paddingInPixels)
        {
            m_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            m_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            m_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            m_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            m_PropertyBlock.SetVector(BlitShaderIDs._BlitTextureSize, textureSize);
            m_PropertyBlock.SetInt(BlitShaderIDs._BlitPaddingSize, paddingInPixels);
            cmd.DrawProcedural(Matrix4x4.identity, GetBlitMaterial(source.dimension), 13, MeshTopology.Quads, 4, 1, m_PropertyBlock);
        }

        // TODO: replace with GraphicsFormatUtilities.GetBlockSize(), for non-block format should be size in bytes of 1x1 pixel
        static Dictionary<GraphicsFormat, int> graphicsFormatSizeCache = new Dictionary<GraphicsFormat, int>
        {
            // Init some default format so we don't allocate more memory on the first frame.
            {GraphicsFormat.R8G8B8A8_UNorm, 4},
            {GraphicsFormat.R16G16B16A16_SFloat, 8},
            {GraphicsFormat.RGB_BC6H_SFloat, 1}, // BC6H uses 128 bits for each 4x4 tile which is 8 bits per pixel
        };

        // TODO: remove and replace with GraphicsFormatUtilities.GetBlockSize(), for non-block format should be size in bytes of 1x1 pixel
        /// <summary>
        /// Compute the size in bytes of a GraphicsFormat. Does not works with compressed formats.
        /// </summary>
        /// <param name="format"></param>
        /// <returns>Size in Bytes</returns>
        public static int GetFormatSizeInBytes(GraphicsFormat format)
        {
            if (graphicsFormatSizeCache.TryGetValue(format, out var size))
            {
                Assert.IsTrue(size == GraphicsFormatUtility.GetBlockSize(format));
                return size;
            }


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

            Assert.IsTrue(size == GraphicsFormatUtility.GetBlockSize(format));
            return size;
        }
    }
}
