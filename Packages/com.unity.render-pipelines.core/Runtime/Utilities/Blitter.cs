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
    /// <summary>
    /// Various blit (texture copy) utilities for the Scriptable Render Pipelines.
    /// </summary>
    public static class Blitter
    {
        static Material s_Blit;
        static Material s_BlitTexArray;
        static Material s_BlitTexArraySingleSlice;
        static Material s_BlitColorAndDepth;

        static MaterialPropertyBlock s_PropertyBlock = new MaterialPropertyBlock();

        static Mesh s_TriangleMesh;
        static Mesh s_QuadMesh;

        static LocalKeyword s_DecodeHdrKeyword;

        static class BlitShaderIDs
        {
            public static readonly int _BlitTexture = Shader.PropertyToID("_BlitTexture");
            public static readonly int _BlitCubeTexture = Shader.PropertyToID("_BlitCubeTexture");
            public static readonly int _BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
            public static readonly int _BlitScaleBiasRt = Shader.PropertyToID("_BlitScaleBiasRt");
            public static readonly int _BlitMipLevel = Shader.PropertyToID("_BlitMipLevel");
            public static readonly int _BlitTextureSize = Shader.PropertyToID("_BlitTextureSize");
            public static readonly int _BlitPaddingSize = Shader.PropertyToID("_BlitPaddingSize");
            public static readonly int _BlitDecodeInstructions = Shader.PropertyToID("_BlitDecodeInstructions");
            public static readonly int _InputDepth = Shader.PropertyToID("_InputDepthTexture");
        }

        // This enum needs to be in sync with the shader pass names and indices of the Blit.shader in every pipeline.
        enum BlitShaderPassNames
        {
            Nearest = 0,
            Bilinear = 1,
            NearestQuad = 2,
            BilinearQuad = 3,
            NearestQuadPadding = 4,
            BilinearQuadPadding = 5,
            NearestQuadPaddingRepeat = 6,
            BilinearQuadPaddingRepeat = 7,
            BilinearQuadPaddingOctahedral = 8,
            NearestQuadPaddingAlphaBlend = 9,
            BilinearQuadPaddingAlphaBlend = 10,
            NearestQuadPaddingAlphaBlendRepeat = 11,
            BilinearQuadPaddingAlphaBlendRepeat = 12,
            BilinearQuadPaddingAlphaBlendOctahedral = 13,
            CubeToOctahedral = 14,
            CubeToOctahedralLuminance = 15,
            CubeToOctahedralAlpha = 16,
            CubeToOctahedralRed = 17,
            BilinearQuadLuminance = 18,
            BilinearQuadAlpha = 19,
            BilinearQuadRed = 20,
            NearestCubeToOctahedralPadding = 21,
            BilinearCubeToOctahedralPadding = 22,
        }

        enum BlitColorAndDepthPassNames
        {
            ColorOnly = 0,
            ColorAndDepth = 1,
        }

        // This maps the requested shader indices to actual existing shader indices. When running in a build, it's possible
        // that some shader pass are stripped or removed, causing a shift in all shader pass index. In this case, hardcoded
        // shader passes become invalid. This array prevent this error from happening.
        static int[] s_BlitShaderPassIndicesMap;
        static int[] s_BlitColorAndDepthShaderPassIndicesMap;

        /// <summary>
        /// Initialize Blitter resources. Must be called once before any use
        /// </summary>
        /// <param name="blitPS"></param> Blit shader
        /// <param name="blitColorAndDepthPS"></param> Blit shader
        public static void Initialize(Shader blitPS, Shader blitColorAndDepthPS)
        {
            if (s_Blit != null)
            {
                throw new Exception("Blitter is already initialized. Please only initialize the blitter once or you will leak engine resources. If you need to re-initialize the blitter with different shaders destroy & recreate it.");
            }

            // NOTE NOTE NOTE NOTE NOTE NOTE
            // If you create something here you must also destroy it in Cleanup()
            // or it will leak during enter/leave play mode cycles
            // NOTE NOTE NOTE NOTE NOTE NOTE
            s_Blit = CoreUtils.CreateEngineMaterial(blitPS);
            s_BlitColorAndDepth = CoreUtils.CreateEngineMaterial(blitColorAndDepthPS);

            s_DecodeHdrKeyword = new LocalKeyword(blitPS, "BLIT_DECODE_HDR");

            // With texture array enabled, we still need the normal blit version for other systems like atlas
            if (TextureXR.useTexArray)
            {
                s_Blit.EnableKeyword("DISABLE_TEXTURE2D_X_ARRAY");
                s_BlitTexArray = CoreUtils.CreateEngineMaterial(blitPS);
                s_BlitTexArraySingleSlice = CoreUtils.CreateEngineMaterial(blitPS);
                s_BlitTexArraySingleSlice.EnableKeyword("BLIT_SINGLE_SLICE");
            }

            if (SystemInfo.graphicsShaderLevel < 30)
            {
                /*UNITY_NEAR_CLIP_VALUE*/
                float nearClipZ = -1;
                if (SystemInfo.usesReversedZBuffer)
                    nearClipZ = 1;

                if (!s_TriangleMesh)
                {
                    s_TriangleMesh = new Mesh();
                    s_TriangleMesh.vertices = GetFullScreenTriangleVertexPosition(nearClipZ);
                    s_TriangleMesh.uv = GetFullScreenTriangleTexCoord();
                    s_TriangleMesh.triangles = new int[3] { 0, 1, 2 };
                }

                if (!s_QuadMesh)
                {
                    s_QuadMesh = new Mesh();
                    s_QuadMesh.vertices = GetQuadVertexPosition(nearClipZ);
                    s_QuadMesh.uv = GetQuadTexCoord();
                    s_QuadMesh.triangles = new int[6] { 0, 1, 2, 0, 2, 3 };
                }

                // Should match Common.hlsl
                static Vector3[] GetFullScreenTriangleVertexPosition(float z /*= UNITY_NEAR_CLIP_VALUE*/)
                {
                    var r = new Vector3[3];
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 uv = new Vector2((i << 1) & 2, i & 2);
                        r[i] = new Vector3(uv.x * 2.0f - 1.0f, uv.y * 2.0f - 1.0f, z);
                    }
                    return r;
                }

                // Should match Common.hlsl
                static Vector2[] GetFullScreenTriangleTexCoord()
                {
                    var r = new Vector2[3];
                    for (int i = 0; i < 3; i++)
                    {
                        if (SystemInfo.graphicsUVStartsAtTop)
                            r[i] = new Vector2((i << 1) & 2, 1.0f - (i & 2));
                        else
                            r[i] = new Vector2((i << 1) & 2, i & 2);
                    }
                    return r;
                }

                // Should match Common.hlsl
                static Vector3[] GetQuadVertexPosition(float z /*= UNITY_NEAR_CLIP_VALUE*/)
                {
                    var r = new Vector3[4];
                    for (uint i = 0; i < 4; i++)
                    {
                        uint topBit = i >> 1;
                        uint botBit = (i & 1);
                        float x = topBit;
                        float y = 1 - (topBit + botBit) & 1; // produces 1 for indices 0,3 and 0 for 1,2
                        r[i] = new Vector3(x, y, z);
                    }
                    return r;
                }

                // Should match Common.hlsl
                static Vector2[] GetQuadTexCoord()
                {
                    var r = new Vector2[4];
                    for (uint i = 0; i < 4; i++)
                    {
                        uint topBit = i >> 1;
                        uint botBit = (i & 1);
                        float u = topBit;
                        float v = (topBit + botBit) & 1; // produces 0 for indices 0,3 and 1 for 1,2
                        if (SystemInfo.graphicsUVStartsAtTop)
                            v = 1.0f - v;

                        r[i] = new Vector2(u, v);
                    }
                    return r;
                }
            }
            
            // Build shader pass map:
            var passNames = Enum.GetNames(typeof(BlitShaderPassNames));
            s_BlitShaderPassIndicesMap = new int[passNames.Length];
            for (int i = 0; i < passNames.Length; i++)
                s_BlitShaderPassIndicesMap[i] = s_Blit.FindPass(passNames[i]);
            
            passNames = Enum.GetNames(typeof(BlitColorAndDepthPassNames));
            s_BlitColorAndDepthShaderPassIndicesMap = new int[passNames.Length];
            for (int i = 0; i < passNames.Length; i++)
                s_BlitColorAndDepthShaderPassIndicesMap[i] = s_BlitColorAndDepth.FindPass(passNames[i]);
        }

        /// <summary>
        /// Release Blitter resources.
        /// </summary>
        public static void Cleanup()
        {
            CoreUtils.Destroy(s_Blit);
            s_Blit = null;
            CoreUtils.Destroy(s_BlitColorAndDepth);
            s_BlitColorAndDepth = null;
            CoreUtils.Destroy(s_BlitTexArray);
            s_BlitTexArray = null;
            CoreUtils.Destroy(s_BlitTexArraySingleSlice);
            s_BlitTexArraySingleSlice = null;
            CoreUtils.Destroy(s_TriangleMesh);
            s_TriangleMesh = null;
            CoreUtils.Destroy(s_QuadMesh);
            s_QuadMesh = null;
        }

        /// <summary>
        /// Returns the default blit material.
        /// </summary>
        /// <param name="dimension">Dimension of the texture to blit, either 2D or 2D Array.</param>
        /// <param name="singleSlice">Blit only a single slice of the array if applicable.</param>
        /// <returns></returns>
        static public Material GetBlitMaterial(TextureDimension dimension, bool singleSlice = false)
        {
            bool useTexArray = dimension == TextureDimension.Tex2DArray;
            return useTexArray ? (singleSlice ? s_BlitTexArraySingleSlice : s_BlitTexArray) : s_Blit;
        }

        static private void DrawTriangle(RasterCommandBuffer cmd, Material material, int shaderPass)
        {
            DrawTriangle(cmd.m_WrappedCommandBuffer, material, shaderPass);
        }

        static private void DrawTriangle(CommandBuffer cmd, Material material, int shaderPass)
        {
            if (SystemInfo.graphicsShaderLevel < 30)
                cmd.DrawMesh(s_TriangleMesh, Matrix4x4.identity, material, 0, shaderPass, s_PropertyBlock);
            else
                cmd.DrawProcedural(Matrix4x4.identity, material, shaderPass, MeshTopology.Triangles, 3, 1, s_PropertyBlock);
        }

        static internal void DrawQuad(CommandBuffer cmd, Material material, int shaderPass)
        {
            if (SystemInfo.graphicsShaderLevel < 30)
                cmd.DrawMesh(s_QuadMesh, Matrix4x4.identity, material, 0, shaderPass, s_PropertyBlock);
            else
                cmd.DrawProcedural(Matrix4x4.identity, material, shaderPass, MeshTopology.Quads, 4, 1, s_PropertyBlock);
        }

        /// <summary>
        /// Blit a RTHandle texture.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public static void BlitTexture(RasterCommandBuffer cmd, RTHandle source, Vector4 scaleBias, float mipLevel, bool bilinear)
        {
            BlitTexture(cmd.m_WrappedCommandBuffer, source, scaleBias, mipLevel, bilinear);
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
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevel);
            BlitTexture(cmd, source, scaleBias, GetBlitMaterial(TextureXR.dimension), s_BlitShaderPassIndicesMap[bilinear ? 1 : 0]);
        }

        /// <summary>
        /// Blit a RTHandle texture 2D.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public static void BlitTexture2D(RasterCommandBuffer cmd, RTHandle source, Vector4 scaleBias, float mipLevel, bool bilinear)
        {
            BlitTexture2D(cmd.m_WrappedCommandBuffer, source, scaleBias, mipLevel, bilinear);
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
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevel);
            BlitTexture(cmd, source, scaleBias, GetBlitMaterial(TextureDimension.Tex2D), s_BlitShaderPassIndicesMap[bilinear ? 1 : 0]);
        }

        /// <summary>
        /// Blit a 2D texture and depth buffer.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="sourceColor">Source Texture for color.</param>
        /// <param name="sourceDepth">Source RenderTexture for depth.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="blitDepth">Enable depth blit.</param>
        public static void BlitColorAndDepth(RasterCommandBuffer cmd, Texture sourceColor, RenderTexture sourceDepth, Vector4 scaleBias, float mipLevel, bool blitDepth)
        {
            BlitColorAndDepth(cmd.m_WrappedCommandBuffer, sourceColor, sourceDepth, scaleBias, mipLevel, blitDepth);
        }

        /// <summary>
        /// Blit a 2D texture and depth buffer.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="sourceColor">Source Texture for color.</param>
        /// <param name="sourceDepth">Source RenderTexture for depth.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="blitDepth">Enable depth blit.</param>
        public static void BlitColorAndDepth(CommandBuffer cmd, Texture sourceColor, RenderTexture sourceDepth, Vector4 scaleBias, float mipLevel, bool blitDepth)
        {
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevel);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBias);
            s_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, sourceColor);
            if (blitDepth)
                s_PropertyBlock.SetTexture(BlitShaderIDs._InputDepth, sourceDepth, RenderTextureSubElement.Depth);
            DrawTriangle(cmd, s_BlitColorAndDepth, s_BlitColorAndDepthShaderPassIndicesMap[blitDepth ? 1 : 0]);
        }

        /// <summary>
        /// Blit a RTHandle texture
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="material">Material to invoke when blitting.</param>
        /// <param name="pass">Pass idx within the material to invoke.</param>
        public static void BlitTexture(RasterCommandBuffer cmd, RTHandle source, Vector4 scaleBias, Material material, int pass)
        {
            BlitTexture(cmd.m_WrappedCommandBuffer, source, scaleBias, material, pass);
        }

        /// <summary>
        /// Blit a RTHandle texture
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="material">Material to invoke when blitting.</param>
        /// <param name="pass">Pass idx within the material to invoke.</param>
        public static void BlitTexture(CommandBuffer cmd, RTHandle source, Vector4 scaleBias, Material material, int pass)
        {
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBias);
            s_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            DrawTriangle(cmd, material, pass);
        }

        /// <summary>
        /// Blit a RTHandle texture
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source render target.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="material">Material to invoke when blitting.</param>
        /// <param name="pass">Pass idx within the material to invoke.</param>
        public static void BlitTexture(RasterCommandBuffer cmd, RenderTargetIdentifier source, Vector4 scaleBias, Material material, int pass)
        {
            BlitTexture(cmd.m_WrappedCommandBuffer, source, scaleBias, material, pass);
        }

        /// <summary>
        /// Blit a RTHandle texture
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source render target.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="material">Material to invoke when blitting.</param>
        /// <param name="pass">Pass idx within the material to invoke.</param>
        public static void BlitTexture(CommandBuffer cmd, RenderTargetIdentifier source, Vector4 scaleBias, Material material, int pass)
        {
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBias);
            // Unfortunately there is no function bind a RenderTargetIdentifier with a property block so we have to bind it globally.
            cmd.SetGlobalTexture(BlitShaderIDs._BlitTexture, source);
            DrawTriangle(cmd, material, pass);
        }

        /// <summary>
        /// Blit a Texture with a specified material. The reference name "_BlitTexture" will be used to bind the input texture.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source render target.</param>
        /// <param name="destination">Destination render target.</param>
        /// <param name="material">Material to invoke when blitting.</param>
        /// <param name="pass">Pass idx within the material to invoke.</param>
        public static void BlitTexture(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, int pass)
        {
            // Unfortunately there is no function bind a RenderTargetIdentifier with a property block so we have to bind it globally.
            cmd.SetGlobalTexture(BlitShaderIDs._BlitTexture, source);
            cmd.SetRenderTarget(destination);
            DrawTriangle(cmd, material, pass);
        }

        /// <summary>
        /// Blit a Texture with a specified material. The reference name "_BlitTexture" will be used to bind the input texture.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source render target.</param>
        /// <param name="destination">Destination render target.</param>
        /// <param name="loadAction">Load action.</param>
        /// <param name="storeAction">Store action.</param>
        /// <param name="material">Material to invoke when blitting.</param>
        /// <param name="pass">Pass idx within the material to invoke.</param>
        public static void BlitTexture(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction, Material material, int pass)
        {
            // Unfortunately there is no function bind a RenderTargetIdentifier with a property block so we have to bind it globally.
            cmd.SetGlobalTexture(BlitShaderIDs._BlitTexture, source);
            cmd.SetRenderTarget(destination, loadAction, storeAction);
            DrawTriangle(cmd, material, pass);
        }

        /// <summary>
        /// Blit a quad with a given Material. Set the destination parameter before using this method.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="scaleBias">Scale and bias values for sampling the input texture.</param>
        /// <param name="material">Material to invoke when blitting.</param>
        /// <param name="pass">Pass index within the Material to invoke.</param>
        public static void BlitTexture(CommandBuffer cmd, Vector4 scaleBias, Material material, int pass)
        {
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBias);
            DrawTriangle(cmd, material, pass);
        }

        /// <summary>
        /// Blit a quad with a given Material. Set the destination parameter before using this method.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="scaleBias">Scale and bias values for sampling the input texture.</param>
        /// <param name="material">Material to invoke when blitting.</param>
        /// <param name="pass">Pass index within the Material to invoke.</param>
        public static void BlitTexture(RasterCommandBuffer cmd, Vector4 scaleBias, Material material, int pass)
        {
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBias);
            DrawTriangle(cmd, material, pass);
        }

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
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
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
        public static void BlitCameraTexture2D(CommandBuffer cmd, RTHandle source, RTHandle destination, float mipLevel = 0.0f, bool bilinear = false)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
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
        public static void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, Material material, int pass)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            // Will set the correct camera viewport as well.
            CoreUtils.SetRenderTarget(cmd, destination);
            BlitTexture(cmd, source, viewportScale, material, pass);
        }

        /// <summary>
        /// Blit a RTHandle to another RTHandle.
        /// This will properly account for partial usage (in term of resolution) of the texture for the current viewport.
        /// This overloads allows the user to override the default blit shader
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="destination">Destination RTHandle.</param>
        /// <param name="loadAction">Load action.</param>
        /// <param name="storeAction">Store action.</param>
        /// <param name="material">The material to use when blitting</param>
        /// <param name="pass">pass to use of the provided material</param>
        public static void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction, Material material, int pass)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            // Will set the correct camera viewport as well.
            CoreUtils.SetRenderTarget(cmd, destination, loadAction, storeAction, ClearFlag.None, Color.clear);
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
        public static void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, Vector4 scaleBias, float mipLevel = 0.0f, bool bilinear = false)
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
        public static void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, Rect destViewport, float mipLevel = 0.0f, bool bilinear = false)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
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
        public static void BlitQuad(CommandBuffer cmd, Texture source, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear)
        {
            s_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);

            DrawQuad(cmd, GetBlitMaterial(source.dimension), s_BlitShaderPassIndicesMap[bilinear ? 3 : 2]);
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
            s_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitTextureSize, textureSize);
            s_PropertyBlock.SetInt(BlitShaderIDs._BlitPaddingSize, paddingInPixels);
            if (source.wrapMode == TextureWrapMode.Repeat)
                DrawQuad(cmd, GetBlitMaterial(source.dimension), s_BlitShaderPassIndicesMap[bilinear ? 7 : 6]);
            else
                DrawQuad(cmd, GetBlitMaterial(source.dimension), s_BlitShaderPassIndicesMap[bilinear ? 5 : 4]);
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
            s_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitTextureSize, textureSize);
            s_PropertyBlock.SetInt(BlitShaderIDs._BlitPaddingSize, paddingInPixels);
            if (source.wrapMode == TextureWrapMode.Repeat)
                DrawQuad(cmd, GetBlitMaterial(source.dimension), s_BlitShaderPassIndicesMap[bilinear ? 12 : 11]);
            else
                DrawQuad(cmd, GetBlitMaterial(source.dimension), s_BlitShaderPassIndicesMap[bilinear ? 10 : 9]);
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
            s_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitTextureSize, textureSize);
            s_PropertyBlock.SetInt(BlitShaderIDs._BlitPaddingSize, paddingInPixels);
            DrawQuad(cmd, GetBlitMaterial(source.dimension), s_BlitShaderPassIndicesMap[8]);
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
            s_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitTextureSize, textureSize);
            s_PropertyBlock.SetInt(BlitShaderIDs._BlitPaddingSize, paddingInPixels);
            DrawQuad(cmd, GetBlitMaterial(source.dimension), s_BlitShaderPassIndicesMap[13]);
        }

        /// <summary>
        /// Blit a cube texture into 2d texture as octahedral quad. (projection)
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source cube texture.</param>
        /// <param name="mipLevelTex">Mip level to sample.</param>
        /// /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        public static void BlitCubeToOctahedral2DQuad(CommandBuffer cmd, Texture source, Vector4 scaleBiasRT, int mipLevelTex)
        {
            s_PropertyBlock.SetTexture(BlitShaderIDs._BlitCubeTexture, source);
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, new Vector4(1, 1, 0, 0));
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            DrawQuad(cmd, GetBlitMaterial(source.dimension), s_BlitShaderPassIndicesMap[14]);
        }

        /// <summary>
        /// Blit a cube texture into 2d texture as octahedral quad with padding. (projection)
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source cube texture.</param>
        /// <param name="textureSize">Source texture size.</param>
        /// <param name="mipLevelTex">Mip level to sample.</param>
        /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        /// <param name="paddingInPixels">Padding in pixels.</param>
        /// <param name="decodeInstructions">The purpose of this parameter is to blit HDR-encoded values to a non HDR texture. Use values from API that produce HDR-encoded values, for example <see cref="ReflectionProbe.textureHDRDecodeValues"/>. If this parameter is null, HDR decoding is disabled.</param>
        public static void BlitCubeToOctahedral2DQuadWithPadding(CommandBuffer cmd, Texture source, Vector2 textureSize, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear, int paddingInPixels, Vector4? decodeInstructions = null)
        {
            var material = GetBlitMaterial(source.dimension);

            s_PropertyBlock.SetTexture(BlitShaderIDs._BlitCubeTexture, source);
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, new Vector4(1, 1, 0, 0));
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitTextureSize, textureSize);
            s_PropertyBlock.SetInt(BlitShaderIDs._BlitPaddingSize, paddingInPixels);

            cmd.SetKeyword(material, s_DecodeHdrKeyword, decodeInstructions.HasValue);
            if (decodeInstructions.HasValue)
            {
                s_PropertyBlock.SetVector(BlitShaderIDs._BlitDecodeInstructions, decodeInstructions.Value);
            }

            DrawQuad(cmd, material, s_BlitShaderPassIndicesMap[bilinear ? 22 : 21]);
            cmd.SetKeyword(material, s_DecodeHdrKeyword, false);
        }

        /// <summary>
        /// Blit a cube texture into 2d texture as octahedral quad. (projection)
        /// Conversion between single and multi channel formats.
        /// RGB(A) to YYYY (luminance).
        /// R to RRRR.
        /// A to AAAA.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        /// <param name="mipLevelTex">Mip level to blit.</param>
        public static void BlitCubeToOctahedral2DQuadSingleChannel(CommandBuffer cmd, Texture source, Vector4 scaleBiasRT, int mipLevelTex)
        {
            int pass = 15;
            uint sourceChnCount = GraphicsFormatUtility.GetComponentCount(source.graphicsFormat);
            if (sourceChnCount == 1)
            {
                if (GraphicsFormatUtility.IsAlphaOnlyFormat(source.graphicsFormat))
                    pass = 16;
                if (GraphicsFormatUtility.GetSwizzleR(source.graphicsFormat) == FormatSwizzle.FormatSwizzleR)
                    pass = 17;
            }

            s_PropertyBlock.SetTexture(BlitShaderIDs._BlitCubeTexture, source);
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, new Vector4(1, 1, 0, 0));
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            DrawQuad(cmd, GetBlitMaterial(source.dimension), s_BlitShaderPassIndicesMap[pass]);
        }

        /// <summary>
        /// Bilinear Blit a texture using a quad in the current render target.
        /// Conversion between single and multi channel formats.
        /// RGB(A) to YYYY (luminance).
        /// R to RRRR.
        /// A to AAAA.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source texture.</param>
        /// <param name="scaleBiasTex">Scale and bias for the input texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        /// <param name="mipLevelTex">Mip level to blit.</param>
        public static void BlitQuadSingleChannel(CommandBuffer cmd, Texture source, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex)
        {
            int pass = 18;
            uint sourceChnCount = GraphicsFormatUtility.GetComponentCount(source.graphicsFormat);
            if (sourceChnCount == 1)
            {
                if (GraphicsFormatUtility.IsAlphaOnlyFormat(source.graphicsFormat))
                    pass = 19;
                if (GraphicsFormatUtility.GetSwizzleR(source.graphicsFormat) == FormatSwizzle.FormatSwizzleR)
                    pass = 20;
            }

            s_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);

            DrawQuad(cmd, GetBlitMaterial(source.dimension), s_BlitShaderPassIndicesMap[pass]);
        }
    }
}
