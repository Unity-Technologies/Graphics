using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Assertions;
using System.Text.RegularExpressions;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.RenderGraphModule;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Various blit (texture copy) utilities for the Scriptable Render Pipelines.
    /// </summary>
    /// <remarks>
    /// The Blitter class works on textures and targets identified with <see cref="Texture"/>,
    /// <see cref="RenderTargetIdentifier"/> but most importantly with <see cref="RTHandle"/>. This enables
    /// copying to / from textures managed by the <see cref="RenderGraphModule.RenderGraph"/>.
    ///
    /// To use the Blitter functionality in the context of custom Scriptable Render Pipelines, you must first create
    /// a blit shader that implements various passes covering the blit variants. To facilitate this, you can create a
    /// modified version of the Universal Render Pipeline <c>CoreBlit.shader</c> as displayed in the documentation of the
    /// <see cref="Initialize"/> method.
    ///
    /// Prior to using the Blitter, you must call <see cref="Initialize"/> once. When the render pipeline is to
    /// be disposed, you must call the <see cref="Cleanup"/> method to dispose of resources created by the Blitter.
    /// </remarks>
    public static class Blitter
    {
        static Material s_Copy;
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
            public static readonly int _BlitTexArraySlice = Shader.PropertyToID("_BlitTexArraySlice");
            public static readonly int _BlitTextureSize = Shader.PropertyToID("_BlitTextureSize");
            public static readonly int _BlitPaddingSize = Shader.PropertyToID("_BlitPaddingSize");
            public static readonly int _BlitDecodeInstructions = Shader.PropertyToID("_BlitDecodeInstructions");
            public static readonly int _InputDepth = Shader.PropertyToID("_InputDepthTexture");
        }

        // This enum needs to be in sync with the shader pass names and indices of the Blit.shader in every pipeline.
        // Keep in sync also with the documentation for the Initialize method below.
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
        /// Initializes the Blitter resources. This must be called once before any use.
        /// </summary>
        /// <param name="blitPS">The shader to use when using the blitting / copying methods which operate only on color.</param>
        /// <param name="blitColorAndDepthPS">The shader to use when using the BlitColorAndDepth methods which operate on both color and depth.</param>
        /// <remarks>
        /// Shaders sent to the <c>blitPS</c> parameter should support multiple passes with the corresponding name:
        /// <list type="bullet">
        /// <item><term>Nearest</term></item>
        /// <item><term>Bilinear</term></item>
        /// <item><term>NearestQuad</term></item>
        /// <item><term>BilinearQuad</term></item>
        /// <item><term>NearestQuadPadding</term></item>
        /// <item><term>BilinearQuadPadding</term></item>
        /// <item><term>NearestQuadPaddingRepeat</term></item>
        /// <item><term>BilinearQuadPaddingRepeat</term></item>
        /// <item><term>BilinearQuadPaddingOctahedral</term></item>
        /// <item><term>NearestQuadPaddingAlphaBlend</term></item>
        /// <item><term>BilinearQuadPaddingAlphaBlend</term></item>
        /// <item><term>NearestQuadPaddingAlphaBlendRepeat</term></item>
        /// <item><term>BilinearQuadPaddingAlphaBlendRepeat</term></item>
        /// <item><term>BilinearQuadPaddingAlphaBlendOctahedral</term></item>
        /// <item><term>CubeToOctahedral</term></item>
        /// <item><term>CubeToOctahedralLuminance</term></item>
        /// <item><term>CubeToOctahedralAlpha</term></item>
        /// <item><term>CubeToOctahedralRed</term></item>
        /// <item><term>BilinearQuadLuminance</term></item>
        /// <item><term>BilinearQuadAlpha</term></item>
        /// <item><term>BilinearQuadRed</term></item>
        /// <item><term>NearestCubeToOctahedralPadding</term></item>
        /// <item><term>BilinearCubeToOctahedralPadding</term></item>
        /// </list>
        /// Basic vertex and fragment shader functions are available in <c>Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl</c> for each of these pass types.
        /// Similarly, the shaders for the <c>blitColorAndDepthPS</c> parameter should support two passes with these names:
        /// <list type="bullet">
        /// <item><term>ColorOnly</term></item>
        /// <item><term>ColorAndDepth</term></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <para>Blit color shader for URP which implements all the above passes with a user defined <c>FragmentURPBlit</c> fragment function to support debug passes
        /// and color space conversion.</para>
        /// <code lang="hlsl" source="../../Documentation~/Examples/Blit.shader"/>
        /// <para>Blit color and depth shader for URP.</para>
        /// <code lang="hlsl" source="../../Documentation~/Examples/BlitColorAndDepth.shader"/>
        /// </example>
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
            s_Copy = CoreUtils.CreateEngineMaterial(GraphicsSettings.GetRenderPipelineSettings<RenderGraphUtilsResources>().coreCopyPS);
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

            /*UNITY_NEAR_CLIP_VALUE*/
            float nearClipZ = -1;
            if (SystemInfo.usesReversedZBuffer)
                nearClipZ = 1;
            if (SystemInfo.graphicsShaderLevel < 30)
            {
                if (!s_TriangleMesh)
                {
                    s_TriangleMesh = new Mesh();
                    s_TriangleMesh.vertices = GetFullScreenTriangleVertexPosition(nearClipZ);
                    s_TriangleMesh.uv = GetFullScreenTriangleTexCoord();
                    s_TriangleMesh.triangles = new int[3] { 0, 1, 2 };
                }
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
        /// Releases all the internal Blitter resources. Must be called when the Blitter object is to be disposed.
        /// </summary>
        public static void Cleanup()
        {
            CoreUtils.Destroy(s_Copy);
            s_Copy = null;
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
        /// Returns the default blit material constructed from the blit shader passed as the first argument of
        /// the <see cref="Initialize(Shader, Shader)"/> method.
        /// </summary>
        /// <param name="dimension">Dimension of the texture to blit, either 2D or 2D Array.</param>
        /// <param name="singleSlice">Blit only a single slice of the array if applicable.</param>
        /// <returns>The default blit material for the specified arguments.</returns>
        static public Material GetBlitMaterial(TextureDimension dimension, bool singleSlice = false)
        {
            var material = (dimension == TextureDimension.Tex2DArray)
                ? (singleSlice ? s_BlitTexArraySingleSlice : s_BlitTexArray)
                : null;
            return material == null ? s_Blit : material;
        }

        static internal void DrawTriangle(RasterCommandBuffer cmd, Material material, int shaderPass)
        {
            DrawTriangle(cmd.m_WrappedCommandBuffer, material, shaderPass);
        }

        static internal void DrawTriangle(CommandBuffer cmd, Material material, int shaderPass)
        {
            DrawTriangle(cmd, material, shaderPass, s_PropertyBlock);
        }

        static internal void DrawTriangle(CommandBuffer cmd, Material material, int shaderPass, MaterialPropertyBlock propertyBlock)
        {
            if (SystemInfo.graphicsShaderLevel < 30)
                cmd.DrawMesh(s_TriangleMesh, Matrix4x4.identity, material, 0, shaderPass, propertyBlock);
            else
                cmd.DrawProcedural(Matrix4x4.identity, material, shaderPass, MeshTopology.Triangles, 3, 1, propertyBlock);
        }

        static internal void DrawQuadMesh(CommandBuffer cmd, Material material, int shaderPass, MaterialPropertyBlock propertyBlock)
        {
            cmd.DrawMesh(s_QuadMesh, Matrix4x4.identity, material, 0, shaderPass, propertyBlock);
        }

        static internal void DrawQuad(RasterCommandBuffer cmd, Material material, int shaderPass, MaterialPropertyBlock propertyBlock)
        {
            DrawQuad(cmd.m_WrappedCommandBuffer, material, shaderPass, propertyBlock);
        }

        static internal void DrawQuad(CommandBuffer cmd, Material material, int shaderPass)
        {
            DrawQuad(cmd, material, shaderPass, s_PropertyBlock);
        }

        static internal void DrawQuad(CommandBuffer cmd, Material material, int shaderPass, MaterialPropertyBlock propertyBlock)
        {
            if (SystemInfo.graphicsShaderLevel < 30)
                cmd.DrawMesh(s_QuadMesh, Matrix4x4.identity, material, 0, shaderPass, propertyBlock);
            else
                cmd.DrawProcedural(Matrix4x4.identity, material, shaderPass, MeshTopology.Quads, 4, 1, propertyBlock);
        }

        internal static bool CanCopyMSAA()
        {
            //Temporary disable most msaa copies as we fix UUM-67324 which is a bit more involved due to the internal work required
            GraphicsDeviceType deviceType = SystemInfo.graphicsDeviceType;
            if (deviceType != GraphicsDeviceType.Metal || deviceType != GraphicsDeviceType.Vulkan)
            {
                return false;
            }

            // This test works since the second pass has the following pragmas and will not be compiled if they are not supported
            // #pragma target 4.5
            // #pragma require msaatex
            return s_Copy.passCount == 2;
        }

        /// <summary>
        /// Copies a texture to another texture using framebuffer fetch.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="force2DForXR">Disable the special handling when XR is active where the source and destination are considered array
        /// textures with a slice for each eye. Setting this to true will consider source and destination as regular 2D textures. When XR is
        /// disabled, textures are always 2D so forcing them to 2D has no impact.</param>
        internal static void CopyTexture(RasterCommandBuffer cmd, bool isMSAA, bool force2DForXR = false)
        {
            if (force2DForXR) cmd.EnableShaderKeyword("DISABLE_TEXTURE2D_X_ARRAY");

            DrawTriangle(cmd, s_Copy, isMSAA ? 1 : 0);

            // Set back the XR texture for regular XR calls
            if (force2DForXR) cmd.DisableShaderKeyword("DISABLE_TEXTURE2D_X_ARRAY");
        }

        /// <summary>
        /// Blits a RTHandle texture.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">RTHandle of the source texture to copy from.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="sourceMipLevel">Mip level to blit from source.</param>
        /// <param name="sourceDepthSlice">Source texture slice index.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        internal static void BlitTexture(CommandBuffer cmd, RTHandle source, Vector4 scaleBias, float sourceMipLevel, int sourceDepthSlice, bool bilinear)
        {
            BlitTexture(cmd, source, scaleBias, GetBlitMaterial(TextureDimension.Tex2D), s_BlitShaderPassIndicesMap[bilinear ? 1 : 0], sourceMipLevel, sourceDepthSlice);
        }

        /// <summary>
        /// Blits a RTHandle texture.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">RTHandle of the source texture to copy from.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="sourceMipLevel">Mip level to blit from source.</param>
        /// <param name="sourceDepthSlice">Source texture slice index.</param>
        internal static void BlitTexture(CommandBuffer cmd, RTHandle source, Vector4 scaleBias, Material material, int pass, float sourceMipLevel, int sourceDepthSlice)
        {
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, sourceMipLevel);
            s_PropertyBlock.SetInt(BlitShaderIDs._BlitTexArraySlice, sourceDepthSlice);
            BlitTexture(cmd, source, scaleBias, material, pass);
        }

        /// <summary>
        /// Adds in a <see cref="RasterCommandBuffer"/> a command to copy an XR compatible texture identified by its <see cref="RTHandle"/> into
        /// the currently bound render target's color buffer.
        /// </summary>
        /// <remarks>
        /// Copying is performed using the blit shader passed as the first argument of
        /// the <see cref="Initialize(Shader, Shader)"/> method.
        ///
        /// This overload is meant for textures and render targets which depend on XR output modes by proper handling, when
        /// necessary, of left / right eye data copying. This generally correspond to textures which represent full screen
        ///
        /// The <c>scaleBias</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets. The operation will always
        /// write to the full destination render target rectangle.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">RTHandle of the source texture to copy from.</param>
        /// <param name="scaleBias">Scale and bias for sampling the source texture.</param>
        /// <param name="mipLevel">Mip level of the source texture to copy from.</param>
        /// <param name="bilinear">Enable bilinear filtering when copying.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Copy the bottom left quadrant of the source texture to the render target using bilinear
        /// // sampling, scaling to the destination render target's full rectangle.
        /// // Configure the scale value to 0.5 because a quadrant has half the width and half the
        /// // height of the texture, and the bias to 0 because the texture coordinate origin is at
        /// // the bottom left.
        /// Blitter.BlitTexture(cmd, source, new Vector4(0.5, 0.5, 0, 0), 0, true);
        ///
        /// // Copy the top half of the source texture's mip level 4 to the render target using nearest
        /// // sampling, scaling to the destination render target's full rectangle.
        /// Blitter.BlitTexture(cmd, source, new Vector4(1, 0.5, 0, 0.5), 4, false);
        /// ]]></code>
        /// </example>
        public static void BlitTexture(RasterCommandBuffer cmd, RTHandle source, Vector4 scaleBias, float mipLevel, bool bilinear)
        {
            BlitTexture(cmd.m_WrappedCommandBuffer, source, scaleBias, mipLevel, bilinear);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to copy an XR compatible texture identified by its <see cref="RTHandle"/> into
        /// the currently bound render target's color buffer.
        /// </summary>
        /// <remarks>
        /// Copying is performed using the blit shader passed as the first argument of
        /// the <see cref="Initialize(Shader, Shader)"/> method.
        ///
        /// This overload is meant for textures and render targets which depend on XR output modes by proper handling, when
        /// necessary, of left / right eye data copying. This generally correspond to textures which represent full screen
        /// data that may differ between eyes.
        ///
        /// The <c>scaleBias</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets. The operation will always
        /// write to the full destination render target rectangle.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">RTHandle of the source texture to copy from.</param>
        /// <param name="scaleBias">Scale and bias for sampling the source texture.</param>
        /// <param name="mipLevel">Mip level of the source texture to copy from.</param>
        /// <param name="bilinear">Enable bilinear filtering when copying.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Copy the bottom left quadrant of the source texture to the render target using bilinear
        /// // sampling, scaling to the destination render target's full rectangle.
        /// // Configure the scale value to 0.5 because a quadrant has half the width and half the
        /// // height of the texture, and the bias to 0 because the texture coordinate origin is at
        /// // the bottom left.
        /// Blitter.BlitTexture(cmd, source, new Vector4(0.5, 0.5, 0, 0), 0, true);
        ///
        /// // Copy the top half of the source texture's mip level 4 to the render target using nearest
        /// // sampling, scaling to the destination render target's full rectangle.
        /// Blitter.BlitTexture(cmd, source, new Vector4(1, 0.5, 0, 0.5), 4, false);
        /// ]]></code>
        /// </example>
        public static void BlitTexture(CommandBuffer cmd, RTHandle source, Vector4 scaleBias, float mipLevel, bool bilinear)
        {
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevel);
            BlitTexture(cmd, source, scaleBias, GetBlitMaterial(TextureXR.dimension), s_BlitShaderPassIndicesMap[bilinear ? 1 : 0]);
        }

        /// <summary>
        /// Adds in a <see cref="RasterCommandBuffer"/> a command to copy a texture identified by its <see cref="RTHandle"/> into
        /// the currently bound render target's color buffer.
        /// </summary>
        /// <remarks>
        /// Copying is performed using the blit shader passed as the first argument of
        /// the <see cref="Initialize(Shader, Shader)"/> method.
        ///
        /// The <c>scaleBias</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets. The operation will always
        /// write to the full destination render target rectangle.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">RTHandle of the source texture to copy from.</param>
        /// <param name="scaleBias">Scale and bias for sampling the source texture.</param>
        /// <param name="mipLevel">Mip level of the source texture to copy from.</param>
        /// <param name="bilinear">Enable bilinear filtering when copying.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Copy the bottom left quadrant of the source texture to the render target using bilinear
        /// // sampling, scaling to the destination render target's full rectangle.
        /// // Configure the scale value to 0.5 because a quadrant has half the width and half the
        /// // height of the texture, and the bias to 0 because the texture coordinate origin is at
        /// // the bottom left.
        /// Blitter.BlitTexture2D(cmd, source, new Vector4(0.5, 0.5, 0, 0), 0, true);
        ///
        /// // Copy the top half of the source texture's mip level 4 to the render target using nearest
        /// // sampling, scaling to the destination render target's full rectangle.
        /// Blitter.BlitTexture2D(cmd, source, new Vector4(1, 0.5, 0, 0.5), 4, false);
        /// ]]></code>
        /// </example>
        public static void BlitTexture2D(RasterCommandBuffer cmd, RTHandle source, Vector4 scaleBias, float mipLevel, bool bilinear)
        {
            BlitTexture2D(cmd.m_WrappedCommandBuffer, source, scaleBias, mipLevel, bilinear);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to copy a texture identified by its <see cref="RTHandle"/> into
        /// the currently bound render target's color buffer.
        /// </summary>
        /// <remarks>
        /// Copying is performed using the blit shader passed as the first argument of
        /// the <see cref="Initialize(Shader, Shader)"/> method.
        ///
        /// The <c>scaleBias</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets. The operation will always
        /// write to the full destination render target rectangle.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">RTHandle of the source texture to copy from.</param>
        /// <param name="scaleBias">Scale and bias for sampling the source texture.</param>
        /// <param name="mipLevel">Mip level of the source texture to copy from.</param>
        /// <param name="bilinear">Enable bilinear filtering when copying.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Copy the bottom left quadrant of the source texture to the render target using bilinear
        /// // sampling, scaling to the destination render target's full rectangle.
        /// // Configure the scale value to 0.5 because a quadrant has half the width and half the
        /// // height of the texture, and the bias to 0 because the texture coordinate origin is at
        /// // the bottom left.
        /// Blitter.BlitTexture2D(cmd, source, new Vector4(0.5, 0.5, 0, 0), 0, true);
        ///
        /// // Copy the top half of the source texture's mip level 4 to the render target using nearest
        /// // sampling, scaling to the destination render target's full rectangle.
        /// Blitter.BlitTexture2D(cmd, source, new Vector4(1, 0.5, 0, 0.5), 4, false);
        /// ]]></code>
        /// </example>
        public static void BlitTexture2D(CommandBuffer cmd, RTHandle source, Vector4 scaleBias, float mipLevel, bool bilinear)
        {
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevel);
            BlitTexture(cmd, source, scaleBias, GetBlitMaterial(TextureDimension.Tex2D), s_BlitShaderPassIndicesMap[bilinear ? 1 : 0]);
        }

        /// <summary>
        /// Adds in a <see cref="RasterCommandBuffer"/> a command to copy two XR compatible color and depth textures into
        /// the currently bound render target's respective color and depth buffer.
        /// </summary>
        /// <remarks>
        /// Although the depth render texture can be passed as a parameter, the copying of the depth information is
        /// optional and must be enabled with the <c>blitDepth</c> parameter.
        /// The copying is done using the <c>blitColorAndDepth</c> shader passed as the second argument of
        /// the <see cref="Initialize(Shader, Shader)"/> method.
        ///
        /// This overload is meant for textures and render targets which depend on XR output modes by proper handling, when
        /// necessary, of left / right eye data copying. This generally corresponds to textures which represent full screen
        /// data that may differ between eyes.
        ///
        /// The <c>scaleBias</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets. The operation will always
        /// write to the full destination render target rectangle.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="sourceColor">Source color texture to copy from.</param>
        /// <param name="sourceDepth">Source depth render texture to copy from.</param>
        /// <param name="scaleBias">Scale and bias for sampling the source texture.</param>
        /// <param name="mipLevel">Mip level of the source texture to copy from.</param>
        /// <param name="blitDepth">Enable copying of the source depth texture.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Copy the bottom left quadrant of a source texture and depth texture to the render target
        /// // using bilinear sampling, scaling to the destination render target's full rectangle.
        /// // Configure the scale value to 0.5 because a quadrant has half the width and half the
        /// // height of the texture, and the bias to 0 because the texture coordinate origin is at
        /// // the bottom left.
        /// Blitter.BlitColorAndDepth(cmd, sourceColor, sourceDepth, new Vector4(0.5, 0.5, 0, 0), 0, true);
        ///
        /// // Copy the top half of mip level 4 of both a source color texture and depth texture to the
        /// // render target using nearest sampling, scaling to the destination render target's full
        /// // rectangle.
        /// Blitter.BlitColorAndDepth(cmd, sourceColor, sourceDepth, new Vector4(1, 0.5, 0, 0.5), 4, true);
        /// ]]></code>
        /// </example>
        public static void BlitColorAndDepth(RasterCommandBuffer cmd, Texture sourceColor, RenderTexture sourceDepth, Vector4 scaleBias, float mipLevel, bool blitDepth)
        {
            BlitColorAndDepth(cmd.m_WrappedCommandBuffer, sourceColor, sourceDepth, scaleBias, mipLevel, blitDepth);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to copy two XR compatible color and depth textures into
        /// the currently bound render target's respective color and depth buffer.
        /// </summary>
        /// <remarks>
        /// Although the depth render texture can be passed as a parameter, the copying of the depth information is
        /// optional and must be enabled with the <c>blitDepth</c> parameter.
        /// The copying is done using the <c>blitColorAndDepth</c> shader passed as the second argument of
        /// the <see cref="Initialize(Shader, Shader)"/> method.
        ///
        /// This overload is meant for textures and render targets which depend on XR output modes by proper handling, when
        /// necessary, of left / right eye data copying. This generally corresponds to textures which represent full screen
        /// data that may differ between eyes.
        ///
        /// The <c>scaleBias</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets. The operation will always
        /// write to the full destination render target rectangle.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="sourceColor">Source color texture to copy from.</param>
        /// <param name="sourceDepth">Source depth render texture to copy from.</param>
        /// <param name="scaleBias">Scale and bias for sampling the source texture.</param>
        /// <param name="mipLevel">Mip level of the source texture to copy from.</param>
        /// <param name="blitDepth">Enable copying of the source depth texture.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Copy the bottom left quadrant of a source texture and depth texture to the render target
        /// // using bilinear sampling, scaling to the destination render target's full rectangle.
        /// // Configure the scale value to 0.5 because a quadrant has half the width and half the
        /// // height of the texture, and the bias to 0 because the texture coordinate origin is at
        /// // the bottom left.
        /// Blitter.BlitColorAndDepth(cmd, sourceColor, sourceDepth, new Vector4(0.5, 0.5, 0, 0), 0, true);
        ///
        /// // Copy the top half of mip level 4 of both a source color texture and depth texture to the
        /// // render target using nearest sampling, scaling to the destination render target's full
        /// // rectangle.
        /// Blitter.BlitColorAndDepth(cmd, sourceColor, sourceDepth, new Vector4(1, 0.5, 0, 0.5), 4, true);
        /// ]]></code>
        /// </example>
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
        /// Adds in a <see cref="RasterCommandBuffer"/> a command to copy a texture identified by its <see cref="RTHandle"/> into
        /// the currently bound render target's color buffer, using a user material and specific shader pass.
        /// </summary>
        /// <remarks>
        /// The <c>source</c> texture will be bound to the "_BlitTexture" shader property.
        /// The <c>scaleBias</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets. The operation will always
        /// write to the full destination render target rectangle.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">RTHandle of the source texture to copy from.</param>
        /// <param name="scaleBias">Scale and bias for sampling the source texture.</param>
        /// <param name="material">The material to use for writing to the destination target.</param>
        /// <param name="pass">The index of the pass to use in the material's shader.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Copy the bottom left quadrant of the source texture to the render target using the first
        /// // pass of a custom material, scaling to the destination render target's full rectangle.
        /// // Configure the scale value to 0.5 because a quadrant has half the width and half the
        /// // height of the texture, and the bias to 0 because the texture coordinate origin is at
        /// // the bottom left.
        /// Blitter.BlitTexture(cmd, source, new Vector4(0.5, 0.5, 0, 0), blitMaterial, 0);
        ///
        /// // Copy the top half of the source texture mip level 4 to the render target using the
        /// // second pass of a custom material, scaling to the destination render target's full
        /// // rectangle.
        /// Blitter.BlitTexture(cmd, source, new Vector4(1, 0.5, 0, 0.5), blitMaterial, 1);
        /// ]]></code>
        /// </example>
        public static void BlitTexture(RasterCommandBuffer cmd, RTHandle source, Vector4 scaleBias, Material material, int pass)
        {
            BlitTexture(cmd.m_WrappedCommandBuffer, source, scaleBias, material, pass);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to copy a texture identified by its <see cref="RTHandle"/> into
        /// the currently bound render target's color buffer, using a user material and specific shader pass.
        /// </summary>
        /// <remarks>
        /// The <c>source</c> texture will be bound to the "_BlitTexture" shader property.
        /// The <c>scaleBias</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets. The operation will always
        /// write to the full destination render target rectangle.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">RTHandle of the source texture to copy from.</param>
        /// <param name="scaleBias">Scale and bias for sampling the source texture.</param>
        /// <param name="material">The material to use for writing to the destination target.</param>
        /// <param name="pass">The index of the pass to use in the material's shader.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Copy the bottom left quadrant of the source texture to the render target using the first
        /// // pass of a custom material, scaling to the destination render target's full rectangle.
        /// // Configure the scale value to 0.5 because a quadrant has half the width and half the
        /// // height of the texture, and the bias to 0 because the texture coordinate origin is at
        /// // the bottom left.
        /// Blitter.BlitTexture(cmd, source, new Vector4(0.5, 0.5, 0, 0), blitMaterial, 0);
        ///
        /// // Copy the top half of the source texture mip level 4 to the render target using the
        /// // second pass of a custom material, scaling to the destination render target's full
        /// // rectangle.
        /// Blitter.BlitTexture(cmd, source, new Vector4(1, 0.5, 0, 0.5), blitMaterial, 1);
        /// ]]></code>
        /// </example>
        public static void BlitTexture(CommandBuffer cmd, RTHandle source, Vector4 scaleBias, Material material, int pass)
        {
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBias);
            s_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            DrawTriangle(cmd, material, pass);
        }

        /// <summary>
        /// Adds in a <see cref="RasterCommandBuffer"/> a command to copy a texture identified by its RenderTargetIdentifier into
        /// the currently bound render target's color buffer, using a user material and specific shader pass.
        /// </summary>
        /// <remarks>
        /// The <c>source</c> texture will be bound to the "_BlitTexture" shader property.
        /// The <c>scaleBias</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets. The operation will always
        /// write to the full destination render target rectangle.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">RenderTargetIdentifier of the source texture to copy from.</param>
        /// <param name="scaleBias">Scale and bias for sampling the source texture.</param>
        /// <param name="material">The material to use for writing to the destination target.</param>
        /// <param name="pass">The index of the pass to use in the material's shader.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Copy the bottom left quadrant of the source texture to the render target using the first
        /// // pass of a custom material, scaling to the destination render target's full rectangle.
        /// // Configure the scale value to 0.5 because a quadrant has half the width and half the
        /// // height of the texture, and the bias to 0 because the texture coordinate origin is at
        /// // the bottom left.
        /// Blitter.BlitTexture(cmd, source, new Vector4(0.5, 0.5, 0, 0), blitMaterial, 0);
        ///
        /// // Copy the top half of the source texture mip level 4 to the render target using the
        /// // second pass of a custom material, scaling to the destination render target's full
        /// // rectangle.
        /// Blitter.BlitTexture(cmd, source, new Vector4(1, 0.5, 0, 0.5), blitMaterial, 1);
        /// ]]></code>
        /// </example>
        public static void BlitTexture(RasterCommandBuffer cmd, RenderTargetIdentifier source, Vector4 scaleBias, Material material, int pass)
        {
            BlitTexture(cmd.m_WrappedCommandBuffer, source, scaleBias, material, pass);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to copy a texture identified by its RenderTargetIdentifier into
        /// the currently bound render target's color buffer, using a user material and specific shader pass.
        /// </summary>
        /// <remarks>
        /// The <c>source</c> texture will be bound to the "_BlitTexture" shader property.
        /// The <c>scaleBias</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets. The operation will always
        /// write to the full destination render target rectangle.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">RenderTargetIdentifier of the source texture to copy from.</param>
        /// <param name="scaleBias">Scale and bias for sampling the source texture.</param>
        /// <param name="material">The material to use for writing to the destination target.</param>
        /// <param name="pass">The index of the pass to use in the material's shader.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Copy the bottom left quadrant of the source texture to the render target using the first
        /// // pass of a custom material, scaling to the destination render target's full rectangle.
        /// // Configure the scale value to 0.5 because a quadrant has half the width and half the
        /// // height of the texture, and the bias to 0 because the texture coordinate origin is at
        /// // the bottom left.
        /// Blitter.BlitTexture(cmd, source, new Vector4(0.5, 0.5, 0, 0), blitMaterial, 0);
        ///
        /// // Copy the top half of the source texture mip level 4 to the render target using the
        /// // second pass of a custom material, scaling to the destination render target's full
        /// // rectangle.
        /// Blitter.BlitTexture(cmd, source, new Vector4(1, 0.5, 0, 0.5), blitMaterial, 1);
        /// ]]></code>
        /// </example>
        public static void BlitTexture(CommandBuffer cmd, RenderTargetIdentifier source, Vector4 scaleBias, Material material, int pass)
        {
            s_PropertyBlock.Clear();
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBias);

            // Unfortunately there is no function bind a RenderTargetIdentifier with a property block so we have to bind it globally.
            cmd.SetGlobalTexture(BlitShaderIDs._BlitTexture, source);
            DrawTriangle(cmd, material, pass);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to copy a texture identified by its RenderTargetIdentifier into
        /// a destination render target, using a user material and specific shader pass.
        /// </summary>
        /// <remarks>
        /// he <c>source</c> texture will be bound to the "_BlitTexture" shader property.
        ///
        /// This overload is equivalent
        /// to <see cref="BlitTexture(CommandBuffer, RenderTargetIdentifier, RenderTargetIdentifier, RenderBufferLoadAction, RenderBufferStoreAction, Material, int)"/>
        /// with the <c>loadAction</c> set to <see cref="RenderBufferLoadAction.Load"/> and the <c>storeAction</c> set to <see cref="RenderBufferStoreAction.Store"/>.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">RenderTargetIdentifier of the source texture to copy from.</param>
        /// <param name="destination">RenderTargetIdentifier of the destination render target to copy to.</param>
        /// <param name="material">The material to use for writing to the destination target.</param>
        /// <param name="pass">The index of the pass to use in the material's shader.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Do a full copy of a source texture to a destination render target using the first pass
        /// // of a custom material, scaling to the destination render target's full rectangle.
        /// Blitter.BlitTexture(cmd, source, dest, blitMaterial, 0);
        /// ]]></code>
        /// </example>
        public static void BlitTexture(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, int pass)
        {
            s_PropertyBlock.Clear();
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, Vector2.one);

            // Unfortunately there is no function bind a RenderTargetIdentifier with a property block so we have to bind it globally.
            cmd.SetGlobalTexture(BlitShaderIDs._BlitTexture, source);
            cmd.SetRenderTarget(destination);
            DrawTriangle(cmd, material, pass);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to copy a texture identified by its RenderTargetIdentifier into
        /// a destination render target, using a user material, specific shader pass and specific load / store actions.
        /// </summary>
        /// <remarks>
        /// The <c>source</c> texture will be bound to the "_BlitTexture" shader property.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">RenderTargetIdentifier of the source texture to copy from.</param>
        /// <param name="destination">RenderTargetIdentifier of the destination render target to copy to.</param>
        /// <param name="loadAction">Load action to perform on the destination render target prior to the copying.</param>
        /// <param name="storeAction">Store action to perform on the destination render target after the copying.</param>
        /// <param name="material">The material to use for writing to the destination target.</param>
        /// <param name="pass">The index of the pass to use in the material's shader.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Do a full copy of a source texture to a destination render target using the first pass
        /// // of a custom material, scaling to the destination render target's full rectangle. Since
        /// // the destination will be overwritten, mark the load action as "Don't care".
        /// Blitter.BlitTexture(cmd, source, dest, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, blitMaterial, 0);
        /// ]]></code>
        /// </example>
        public static void BlitTexture(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction, Material material, int pass)
        {
            s_PropertyBlock.Clear();
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, Vector2.one);

            // Unfortunately there is no function bind a RenderTargetIdentifier with a property block so we have to bind it globally.
            cmd.SetGlobalTexture(BlitShaderIDs._BlitTexture, source);
            cmd.SetRenderTarget(destination, loadAction, storeAction);
            DrawTriangle(cmd, material, pass);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to draw a full screen quad, using a user material and specific shader pass.
        /// </summary>
        /// <remarks>
        /// This method gives you freedom on how to write your blit shader by just taking a material, assumed to
        /// be properly configured with input textures already bound to the material. In this method, the "_BlitScaleBias" shader
        /// property will be set on the material to the <c>scaleBias</c> parameter, prior to the draw.
        ///
        /// The <c>scaleBias</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets. The operation will always
        /// write to the full destination render target rectangle.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="scaleBias">Scale and bias for sampling the source texture.</param>
        /// <param name="material">The material to use for writing to the destination target.</param>
        /// <param name="pass">The index of the pass to use in the material's shader.</param>
        public static void BlitTexture(CommandBuffer cmd, Vector4 scaleBias, Material material, int pass)
        {
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBias);
            DrawTriangle(cmd, material, pass);
        }

        /// <inheritdoc cref="BlitTexture(CommandBuffer, Vector4, Material, int)"/>
        /// <summary>
        /// Adds in a <see cref="RasterCommandBuffer"/> a command to draw a full screen quad, using a user material and specific shader pass.
        /// </summary>
        public static void BlitTexture(RasterCommandBuffer cmd, Vector4 scaleBias, Material material, int pass)
        {
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBias);
            DrawTriangle(cmd, material, pass);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to copy a camera related XR compatible texture identified by
        /// its <see cref="RTHandle"/> into a destination render target.
        /// </summary>
        /// <remarks>
        /// Camera related textures are created with <see cref="RenderGraphModule.RenderGraph.CreateTexture"/>
        /// using <see cref="RenderGraphModule.TextureDesc.TextureDesc(Vector2, bool, bool)"/> or
        /// <see cref="RenderGraphModule.TextureDesc.TextureDesc(ScaleFunc, bool, bool)"/> to
        /// automatically determine their resolution relative to the camera's render target resolution. Compared to the
        /// various <see cref="BlitTexture"/> and <see cref="BlitTexture2D"/> methods, this function automatically handles the
        /// <c>scaleBias</c> parameter. The copy operation will always write to the full destination render target rectangle.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">RTHandle of the source texture to copy from.</param>
        /// <param name="destination">RTHandle of the destination render target to copy to.</param>
        /// <param name="mipLevel">Mip level of the source texture to copy from.</param>
        /// <param name="bilinear">Enable bilinear filtering when copying.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Create an XR texture that has half the width and height of the camera's back buffer.
        /// TextureDesc texDesc = new TextureDesc(new Vector2(0.5f, 0.5f), false, true);
        /// RTHandle source = renderGraph.CreateTexture(texDesc);
        /// // Do a full copy of the texture's first mip level to a destination render target
        /// // scaling with bilinear filtering to the destination render target's full rect.
        /// Blitter.BlitCameraTexture(cmd, source, destination, 0, true);
        /// ]]></code>
        /// </example>
        public static void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, float mipLevel = 0.0f, bool bilinear = false)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            // Will set the correct camera viewport as well.
            CoreUtils.SetRenderTarget(cmd, destination);
            BlitTexture(cmd, source, viewportScale, mipLevel, bilinear);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to copy a camera related texture identified by
        /// its <see cref="RTHandle"/> into a destination render target.
        /// </summary>
        /// <remarks>
        /// Camera related textures are created with the <see cref="RenderGraphModule.RenderGraph.CreateTexture"/>
        /// method using <see cref="RenderGraphModule.TextureDesc.TextureDesc(Vector2,bool,bool)"/> or
        /// <see cref="RenderGraphModule.TextureDesc.TextureDesc(ScaleFunc,bool,bool)"/> to
        /// automatically determine their resolution relative to the camera's render target resolution. Compared to the
        /// various <see cref="BlitTexture"/> and <see cref="BlitTexture2D"/> methods, this function automatically handles the
        /// <c>scaleBias</c> parameter. The copy operation will always write to the full destination render target rectangle.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">RTHandle of the source texture to copy from.</param>
        /// <param name="destination">RTHandle of the destination render target to copy to.</param>
        /// <param name="mipLevel">Mip level of the source texture to copy from.</param>
        /// <param name="bilinear">Enable bilinear filtering when copying.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Create a texture that has half the width and height of the camera's back buffer.
        /// TextureDesc texDesc = new TextureDesc(new Vector2(0.5f, 0.5f), false, false);
        /// RTHandle source = renderGraph.CreateTexture(texDesc);
        /// // Do a full copy of the texture's first mip level to a destination render target
        /// // scaling with bilinear filtering to the destination render target's full rect.
        /// Blitter.BlitCameraTexture(cmd, source, destination, 0, true);
        /// ]]></code>
        /// </example>
        public static void BlitCameraTexture2D(CommandBuffer cmd, RTHandle source, RTHandle destination, float mipLevel = 0.0f, bool bilinear = false)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            // Will set the correct camera viewport as well.
            CoreUtils.SetRenderTarget(cmd, destination);
            BlitTexture2D(cmd, source, viewportScale, mipLevel, bilinear);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to copy a camera related texture identified by
        /// its <see cref="RTHandle"/> into a destination render target, using a user material and specific shader pass.
        /// </summary>
        /// <remarks>
        /// Camera related textures are created with the <see cref="RenderGraphModule.RenderGraph.CreateTexture"/>
        /// method using <see cref="RenderGraphModule.TextureDesc.TextureDesc(Vector2,bool,bool)"/> or
        /// <see cref="RenderGraphModule.TextureDesc.TextureDesc(ScaleFunc,bool,bool)"/> to
        /// automatically determine their resolution relative to the camera's render target resolution. Compared to the
        /// various <see cref="BlitTexture"/> and <see cref="BlitTexture2D"/> methods, this function automatically handles the
        /// <c>scaleBias</c> parameter of these methods. The copy operation will always write to the full destination render target rectangle.
        ///
        /// The "_BlitTexture" shader property will be set to the <c>source</c> texture and the "_BlitScaleBias" shader
        /// property will be set to the appropriate parameter, prior to the draw.
        ///
        /// This overload is equivalent
        /// to <see cref="BlitCameraTexture(CommandBuffer, RTHandle, RTHandle, RenderBufferLoadAction, RenderBufferStoreAction, Material, int)"/>
        /// with the <c>loadAction</c> set to <see cref="RenderBufferLoadAction.Load"/> and the <c>storeAction</c> set
        /// to <see cref="RenderBufferStoreAction.Store"/>.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">RTHandle of the source texture to copy from.</param>
        /// <param name="destination">RTHandle of the destination render target to copy to.</param>
        /// <param name="material">The material to use for writing to the destination target.</param>
        /// <param name="pass">The index of the pass to use in the material's shader.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Create a texture that has half the width and height of the camera's back buffer.
        /// TextureDesc texDesc = new TextureDesc(new Vector2(0.5f, 0.5f), false, false);
        /// RTHandle source = renderGraph.CreateTexture(texDesc);
        /// // Do a full copy of a source texture to a destination render target using the first pass
        /// // of a custom material, scaling to the destination render target's full rectangle.
        /// Blitter.BlitCameraTexture(cmd, source, dest, blitMaterial, 0);
        /// ]]></code>
        /// </example>
        public static void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, Material material, int pass)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            // Will set the correct camera viewport as well.
            CoreUtils.SetRenderTarget(cmd, destination);
            BlitTexture(cmd, source, viewportScale, material, pass);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to copy a camera related texture identified by
        /// its <see cref="RTHandle"/> into a destination render target, using a user material, specific shader pass and specific load / store actions.
        /// </summary>
        /// <remarks>
        /// Camera related textures are created with the <see cref="RenderGraphModule.RenderGraph.CreateTexture"/>
        /// method using <see cref="RenderGraphModule.TextureDesc.TextureDesc(Vector2,bool,bool)"/> or
        /// <see cref="RenderGraphModule.TextureDesc.TextureDesc(ScaleFunc,bool,bool)"/> to
        /// automatically determine their resolution relative to the camera's render target resolution. Compared to the
        /// various <see cref="BlitTexture"/> and <see cref="BlitTexture2D"/> methods, this function automatically handles the
        /// <c>scaleBias</c> parameter. The copy operation will always write to the full destination render target rectangle.
        ///
        /// The "_BlitTexture" shader property will be set to the <c>source</c> texture and the "_BlitScaleBias" shader
        /// property will be set to the appropriate value, prior to the draw.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">RTHandle of the source texture to copy from.</param>
        /// <param name="destination">RTHandle of the destination render target to copy to.</param>
        /// <param name="loadAction">Load action to perform on the destination render target prior to the copying.</param>
        /// <param name="storeAction">Store action to perform on the destination render target after the copying.</param>
        /// <param name="material">The material to use for writing to the destination target.</param>
        /// <param name="pass">The index of the pass to use in the material's shader.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Create a texture that has half the width and height of the camera's back buffer.
        /// TextureDesc texDesc = new TextureDesc(new Vector2(0.5f, 0.5f), false, false);
        /// RTHandle source = renderGraph.CreateTexture(texDesc);
        /// // Do a full copy of a source texture to a destination render target using the first pass
        /// // of a custom material, scaling to the destination render target's full rectangle. Since
        /// // the destination will be overwritten, mark the load action as "Don't care".
        /// Blitter.BlitCameraTexture(cmd, source, dest, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, blitMaterial, 0);
        /// ]]></code>
        /// </example>
        public static void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction, Material material, int pass)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            // Will set the correct camera viewport as well.
            CoreUtils.SetRenderTarget(cmd, destination, loadAction, storeAction, ClearFlag.None, Color.clear);
            BlitTexture(cmd, source, viewportScale, material, pass);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to copy a camera related XR compatible texture identified by
        /// its <see cref="RTHandle"/> into a destination render target, using a user defined scale and bias.
        /// </summary>
        /// <remarks>
        /// The <c>scaleBias</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets. The operation will always
        /// write to the full destination render target rectangle.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">RTHandle of the source texture to copy from.</param>
        /// <param name="destination">RTHandle of the destination render target to copy to.</param>
        /// <param name="scaleBias">Scale and bias for sampling the source texture.</param>
        /// <param name="mipLevel">Mip level of the source texture to copy from.</param>
        /// <param name="bilinear">Enable bilinear filtering when copying.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Create a texture that has half the width and height of the camera's back buffer.
        /// TextureDesc texDesc = new TextureDesc(new Vector2(0.5f, 0.5f), false, false);
        /// RTHandle source = renderGraph.CreateTexture(texDesc);
        /// // Copy the bottom left quadrant of the source texture's second mip level to the
        /// // destination render target, scaling with bilinear filtering to the destination
        /// // render target's full rect.
        /// Blitter.BlitCameraTexture(cmd, source, dest, new Vector4(0.5f, 0.5f, 0f, 0f), 1, true);
        /// ]]></code>
        /// </example>
        public static void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, Vector4 scaleBias, float mipLevel = 0.0f, bool bilinear = false)
        {
            // Will set the correct camera viewport as well.
            CoreUtils.SetRenderTarget(cmd, destination);
            BlitTexture(cmd, source, scaleBias, mipLevel, bilinear);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to copy a camera related XR compatible texture identified by
        /// its <see cref="RTHandle"/> into a destination render target using a custom destination viewport.
        /// </summary>
        /// <remarks>
        /// Camera related textures are created with the <see cref="RenderGraphModule.RenderGraph.CreateTexture"/>
        /// method using <see cref="RenderGraphModule.TextureDesc.TextureDesc(Vector2,bool,bool)"/> or
        /// <see cref="RenderGraphModule.TextureDesc.TextureDesc(ScaleFunc,bool,bool)"/> to
        /// automatically determine their resolution relative to the camera's render target resolution. Compared to the
        /// various <see cref="BlitTexture"/> and <see cref="BlitTexture2D"/> methods, this function automatically handles the
        /// <c>scaleBias</c> parameter. The copy operation will write to the <c>destViewport</c> viewport <see cref="Rect"/>.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">RTHandle of the source texture to copy from.</param>
        /// <param name="destination">RTHandle of the destination render target to copy to.</param>
        /// <param name="destViewport">Rect of the destination viewport to write to.</param>
        /// <param name="mipLevel">Mip level of the source texture to copy from.</param>
        /// <param name="bilinear">Enable bilinear filtering when copying.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Create an XR texture that has half the width and height of the camera's back buffer.
        /// TextureDesc texDesc = new TextureDesc(new Vector2(0.5f, 0.5f), false, true);
        /// RTHandle source = renderGraph.CreateTexture(texDesc);
        /// // Do a full copy of the texture's first mip level to a destination render target
        /// // scaling with bilinear filtering to a custom 512 x 256 pixels viewport.
        /// Blitter.BlitCameraTexture(cmd, source, destination, new Rect(0, 0, 512, 256), 0, true);
        /// ]]></code>
        /// </example>
        public static void BlitCameraTexture(CommandBuffer cmd, RTHandle source, RTHandle destination, Rect destViewport, float mipLevel = 0.0f, bool bilinear = false)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            CoreUtils.SetRenderTarget(cmd, destination);
            cmd.SetViewport(destViewport);
            BlitTexture(cmd, source, viewportScale, mipLevel, bilinear);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to copy an XR compatible texture onto a portion of the current render target.
        /// </summary>
        /// <remarks>
        /// The <c>scaleBiasTex</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets.
        ///
        /// Similarly, the <c>scaleBiasRT</c> parameter controls the rectangle of pixels in the render target to write to by manipulating
        /// the destination quad coordinates. The X and Y coordinates store the scaling factor to apply to texture
        /// coordinates, while the Z and W coordinates store the coordinate offsets.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">The source texture to copy from.</param>
        /// <param name="scaleBiasTex">Scale and bias for sampling the source texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the destination quad.</param>
        /// <param name="mipLevelTex">Mip level of the source texture to sample.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Copy the top right quadrant of the source texture's first mip level to the bottom left
        /// // quadrant of the current render target, without bilinear filtering.
        /// Vector4 topRight = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        /// Vector4 bottomLeft = new Vector4(0.5f, 0.5f, 0.0f, 0.0f);
        /// Blitter.BlitQuad(cmd, source, topRight, bottomLeft, 0, false);
        /// ]]></code>
        /// </example>
        public static void BlitQuad(CommandBuffer cmd, Texture source, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear)
        {
            s_PropertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);

            DrawQuad(cmd, GetBlitMaterial(source.dimension), s_BlitShaderPassIndicesMap[bilinear ? 3 : 2]);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to copy an XR compatible texture onto a portion of the current render target
        /// with support for padding on the destination rect.
        /// </summary>
        /// <remarks>
        /// ![Diagram of the padding parameters](../manual/images/Blitter_BlitQuadWithPadding.svg)
        /// <br/>Diagram detailing the use of the padding, textureSize, scaleBiasTex and scaleBiasRT.
        ///
        /// The source rect is copied to the destination rect along with extra padding pixels taken from the source texture
        /// using the texture's <see cref="Texture.wrapMode"/>. Both source rect pixels and padding pixels are copied inside the
        /// destination rect.
        ///
        /// The <c>scaleBiasTex</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets.
        ///
        /// Similarly, the <c>scaleBiasRT</c> parameter controls the rectangle of pixels in the render target to write to by manipulating
        /// the destination quad coordinates. The X and Y coordinates store the scaling factor to apply to texture
        /// coordinates, while the Z and W coordinates store the coordinate offsets.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">The source texture to copy from.</param>
        /// <param name="textureSize">Source texture size in pixels.</param>
        /// <param name="scaleBiasTex">Scale and bias for sampling the source texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the destination quad.</param>
        /// <param name="mipLevelTex">Mip level of the source texture to sample.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        /// <param name="paddingInPixels">Padding in pixels to add in the destination rect.
        /// This is the total padding on an axis so to have N pixels added to the left, and N pixels to the right, <c>paddingInPixels</c> should be set to 2N.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Create a 512 x 256 texture.
        /// int sourceWidth = 512;
        /// int sourceHeight = 256;
        /// TextureDesc texDesc = new TextureDesc(sourceWidth, sourceHeight, false, false);
        /// RTHandle source = renderGraph.CreateTexture(texDesc);
        /// // Copy the top right quadrant of the source texture's first mip level to the bottom left
        /// // quadrant of the current render target, without bilinear filtering, but with 16 pixels
        /// // of padding.
        /// int paddingInPixelsOneDirection = 16;
        /// // Multiply by two for the total padding along an axis.
        /// int paddingInPixels = 2 * paddingInPixelsOneDirection;
        /// Vector4 topRight = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        /// Vector4 bottomLeft = new Vector4(0.5f, 0.5f, 0.0f, 0.0f);
        /// Vector2 subTextureSize = new Vector2(sourceWidth, sourceHeight);
        /// Blitter.BlitQuadWithPadding(cmd, source, subTextureSize, topRight, bottomLeft, 0, false, paddingInPixels);
        /// ]]></code>
        /// </example>
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
        /// Adds in a <see cref="CommandBuffer"/> a command to blit an XR compatible texture onto a portion of the current render target
        /// with a multiply blend and support for padding on the destination rect.
        /// </summary>
        /// <remarks>
        /// The source rect is blended to the destination rect with a multiplicative blend, along with extra padding pixels taken from the source texture
        /// using the texture's <see cref="Texture.wrapMode"/>. Both source rect pixels and padding pixels are blitted inside the
        /// destination rect. See <see cref="BlitQuadWithPadding"/> for a diagram of how padding is applied.
        ///
        /// The <c>scaleBiasTex</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets.
        ///
        /// Similarly, the <c>scaleBiasRT</c> parameter controls the rectangle of pixels in the render target to write to by manipulating
        /// the destination quad coordinates. The X and Y coordinates store the scaling factor to apply to texture
        /// coordinates, while the Z and W coordinates store the coordinate offsets.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">The source texture to copy from.</param>
        /// <param name="textureSize">Source texture size in pixels.</param>
        /// <param name="scaleBiasTex">Scale and bias for sampling the source texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the destination quad.</param>
        /// <param name="mipLevelTex">Mip level of the source texture to sample.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        /// <param name="paddingInPixels">Padding in pixels to add in the destination rect.
        /// This is the total padding on an axis so to have N pixels added to the left, and N pixels to the right, <c>paddingInPixels</c> should be set to 2N.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Create a 512 x 256 texture.
        /// int sourceWidth = 512;
        /// int sourceHeight = 256;
        /// TextureDesc texDesc = new TextureDesc(sourceWidth, sourceHeight, false, false);
        /// RTHandle source = renderGraph.CreateTexture(texDesc);
        /// // Copy the top right quadrant of the source texture's first mip level to the bottom left
        /// // quadrant of the current render target, without bilinear filtering, with 16 pixels of
        /// // padding.
        /// int paddingInPixelsOneDirection = 16;
        /// // Multiply by two for the total padding along an axis.
        /// int paddingInPixels = 2 * paddingInPixelsOneDirection;
        /// Vector4 topRight = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        /// Vector4 bottomLeft = new Vector4(0.5f, 0.5f, 0.0f, 0.0f);
        /// Vector2 subTextureSize = new Vector2(sourceWidth, sourceHeight);
        /// Blitter.BlitQuadWithPaddingMultiply(cmd, source, subTextureSize, topRight, bottomLeft, 0, false, paddingInPixels);
        /// ]]></code>
        /// </example>
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
        /// Adds in a <see cref="CommandBuffer"/> a command to copy an XR compatible octahedral environment texture
        /// onto a portion of the current render target with support for padding on the destination rect.
        /// </summary>
        /// <remarks>
        /// ![Diagram of the padding parameters](../manual/images/Blitter_BlitOctohedralWithPadding.svg)
        /// <br/>Diagram detailing the use of the padding, textureSize, scaleBiasTex and scaleBiasRT.
        ///
        /// The source rect is copied to the destination rect along with extra padding pixels taken from the source texture
        /// but, compared to <see cref="BlitQuadWithPadding"/>, using a specific octahedral mirror repeat mode. Both source rect pixels
        /// and padding pixels are blitted inside the destination rect.
        ///
        /// The <c>scaleBiasTex</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets.
        ///
        /// Similarly, the <c>scaleBiasRT</c> parameter controls the rectangle of pixels in the render target to write to by manipulating
        /// the destination quad coordinates. The X and Y coordinates store the scaling factor to apply to texture
        /// coordinates, while the Z and W coordinates store the coordinate offsets.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">The source texture to copy from.</param>
        /// <param name="textureSize">Source texture size in pixels.</param>
        /// <param name="scaleBiasTex">Scale and bias for sampling the source texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the destination quad.</param>
        /// <param name="mipLevelTex">Mip level of the source texture to sample.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        /// <param name="paddingInPixels">Padding in pixels to add in the destination rect.
        /// This is the total padding on an axis so to have N pixels added to the left, and N pixels to the right, <c>paddingInPixels</c> should be set to 2N.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Create a 512 x 256 texture.
        /// int sourceWidth = 512;
        /// int sourceHeight = 256;
        /// TextureDesc texDesc = new TextureDesc(sourceWidth, sourceHeight, false, false);
        /// RTHandle source = renderGraph.CreateTexture(texDesc);
        /// // Copy the top right quadrant of the source texture's first mip level to the bottom left
        /// // quadrant of the current render target, without bilinear filtering, with 16 pixels of
        /// // padding.
        /// int paddingInPixelsOneDirection = 16;
        /// // Multiply by two for the total padding along an axis.
        /// int paddingInPixels = 2 * paddingInPixelsOneDirection;
        /// Vector4 topRight = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        /// Vector4 bottomLeft = new Vector4(0.5f, 0.5f, 0.0f, 0.0f);
        /// Vector2 subTextureSize = new Vector2(sourceWidth, sourceHeight);
        /// Blitter.BlitOctahedralWithPadding(cmd, source, subTextureSize, topRight, bottomLeft, 0, false, paddingInPixels);
        /// ]]></code>
        /// </example>
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
        /// Adds in a <see cref="CommandBuffer"/> a command to copy an XR compatible octahedral environment texture
        /// onto a portion of the current render target with a multiply blend and support for padding on the destination rect.
        /// </summary>
        /// <remarks>
        /// The source rect is blended onto the destination rect with a multiplicative blend, with extra padding pixels taken
        /// from the source texture but, compared to <see cref="BlitQuadWithPaddingMultiply"/>, using a specific octahedral mirror
        /// repeat mode. Both source rect pixels and padding pixels are blitted inside the destination rect. See
        /// <see cref="BlitOctahedralWithPadding"/> for a diagram of how padding is applied to the destination.
        ///
        /// The <c>scaleBiasTex</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets.
        ///
        /// Similarly, the <c>scaleBiasRT</c> parameter controls the rectangle of pixels in the render target to write to by manipulating
        /// the destination quad coordinates. The X and Y coordinates store the scaling factor to apply to texture
        /// coordinates, while the Z and W coordinates store the coordinate offsets.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">The source texture to copy from.</param>
        /// <param name="textureSize">Source texture size in pixels.</param>
        /// <param name="scaleBiasTex">Scale and bias for sampling the source texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the destination quad.</param>
        /// <param name="mipLevelTex">Mip level of the source texture to sample.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        /// <param name="paddingInPixels">Padding in pixels to add in all directions to the source rect.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Create a 512 x 256 texture.
        /// int sourceWidth = 512;
        /// int sourceHeight = 256;
        /// TextureDesc texDesc = new TextureDesc(sourceWidth, sourceHeight, false, false);
        /// RTHandle source = renderGraph.CreateTexture(texDesc);
        /// // Copy the top right quadrant of the source texture's first mip level to the bottom left
        /// // quadrant of the current render target, without bilinear filtering, with 16 pixels of
        /// // padding.
        /// int paddingInPixelsOneDirection = 16;
        /// // Multiply by two for the total padding along an axis.
        /// int paddingInPixels = 2 * paddingInPixelsOneDirection;
        /// Vector4 topRight = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        /// Vector4 bottomLeft = new Vector4(0.5f, 0.5f, 0.0f, 0.0f);
        /// Vector2 subTextureSize = new Vector2(sourceWidth, sourceHeight);
        /// Blitter.BlitOctahedralWithPaddingMultiply(cmd, source, subTextureSize, topRight, bottomLeft, 0, false, paddingInPixels);
        /// ]]></code>
        /// </example>
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
        /// Adds in a <see cref="CommandBuffer"/> a command to copy a cube map texture onto a portion of the current render target
        /// using <a href="https://www.readkong.com/page/octahedron-environment-maps-6054207">octahedral mapping</a> for the destination.
        /// </summary>
        /// <remarks>
        /// The <c>scaleBiasRT</c> parameter controls the rectangle of pixels in the render target to write to by manipulating
        /// the destination quad coordinates. The X and Y coordinates store the scaling factor to apply to texture
        /// coordinates, while the Z and W coordinates store the coordinate offsets.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">The source cube texture to copy from.</param>
        /// <param name="scaleBiasRT">Scale and bias for the destination quad.</param>
        /// <param name="mipLevelTex">Mip level of the source texture to sample.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Copy the third mip level of a source cube map texture to the center rect
        /// // of a 3x3 grid on the current render target, like so:
        /// //    +-------+      + - + - + - +
        /// //  /       / |      |   |   |   |
        /// // +-------+  |      + - +---+ - +
        /// // |       |  |  --> |   | X |   |
        /// // |       |  +      + - +---+ - +
        /// // |       | /       |   |   |   |
        /// // +-------+         + - + - + - +
        /// Vector4 center3by3 = new Vector4(1.0f/3.0f, 1.0f/3.0f, 1.0f/3.0f, 1.0f/3.0f);
        /// Blitter.BlitCubeToOctahedral2DQuad(cmd, source, center3by3, 2);
        /// ]]></code>
        /// </example>
        public static void BlitCubeToOctahedral2DQuad(CommandBuffer cmd, Texture source, Vector4 scaleBiasRT, int mipLevelTex)
        {
            s_PropertyBlock.SetTexture(BlitShaderIDs._BlitCubeTexture, source);
            s_PropertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, new Vector4(1, 1, 0, 0));
            s_PropertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            DrawQuad(cmd, GetBlitMaterial(source.dimension), s_BlitShaderPassIndicesMap[14]);
        }

        /// <summary>
        /// Adds in a <see cref="CommandBuffer"/> a command to copy a cube map texture onto a portion of the current render target
        /// using <a href="https://www.readkong.com/page/octahedron-environment-maps-6054207">octahedral mapping</a> for the destination
        /// with extra padding on the destination rect.
        /// </summary>
        /// <remarks>
        /// The source cube map pixels are copied onto the destination rect with extra padding pixels taken from the source texture
        /// but using a specific octahedral mirror repeat mode. Both source rect pixels and padding pixels are blitted inside the destination rect.
        /// See <see cref="BlitOctahedralWithPadding"/> for a diagram of how padding is applied to the destination.
        ///
        /// The <c>scaleBiasRT</c> parameter controls the rectangle of pixels in the render target to write to by manipulating
        /// the destination quad coordinates. The X and Y coordinates store the scaling factor to apply to texture
        /// coordinates, while the Z and W coordinates store the coordinate offsets.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">The source cube texture to copy from.</param>
        /// <param name="textureSize">Source texture size in pixels.</param>
        /// <param name="scaleBiasRT">Scale and bias for the destination quad.</param>
        /// <param name="mipLevelTex">Mip level of the source texture to sample.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        /// <param name="paddingInPixels">Padding in pixels to add in all directions to the source rect.</param>
        /// <param name="decodeInstructions">The purpose of this parameter is to blit HDR-encoded values to a non HDR texture. Use values from API that produce HDR-encoded values, for example <see cref="ReflectionProbe.textureHDRDecodeValues"/>. If this parameter is null, HDR decoding is disabled.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Copy the third mip level of a source cube map texture to the center rect
        /// // of a 3x3 grid on the current render target, like so:
        /// //    +-------+      + - + - + - +
        /// //  /       / |      |   |   |   |
        /// // +-------+  |      + - +---+ - +
        /// // |       |  |  --> |   | X |   |
        /// // |       |  +      + - +---+ - +
        /// // |       | /       |   |   |   |
        /// // +-------+         + - + - + - +
        /// // Desired padding on the destination rect
        /// int paddingInPixelsOneDirection = 16;
        /// // Multiply by two for the total padding along an axis.
        /// int paddingInPixels = 2 * paddingInPixelsOneDirection;
        /// Vector4 center3by3 = new Vector4(1.0f/3.0f, 1.0f/3.0f, 1.0f/3.0f, 1.0f/3.0f);
        /// Vector2 subTextureSize = new Vector2(sourceWidth, sourceHeight);
        /// // HDR to non-HDR decoding is not necessary here so drop the
        /// // last parameter.
        /// Blitter.BlitCubeToOctahedral2DQuadWithPadding(cmd, source, subTextureSize, center3by3, 2, paddingInPixels);
        /// ]]></code>
        /// </example>
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
        /// Adds in a <see cref="CommandBuffer"/> a command to perform a single channel copy of a cube map texture onto a portion of the current render target
        /// using <a href="https://www.readkong.com/page/octahedron-environment-maps-6054207">octahedral mapping</a> for the destination.
        /// </summary>
        /// <remarks>
        /// The conversion to a single channel output depends on the source texture's format:
        /// <list type="table">
        /// <listheader><term>Texture Format</term><description>Output conversion</description></listheader>
        /// <item><term>RGB(A)</term><description>the RGB luminance is written to the destination in all channels.</description></item>
        /// <item><term>Red</term><description>the red value is written to the destination in all channels.</description></item>
        /// <item><term>Alpha</term><description>the alpha value is written to the destination in all channels.</description></item>
        /// </list>
        /// The <c>scaleBiasRT</c> parameter controls the rectangle of pixels in the render target to write to by manipulating
        /// the destination quad coordinates. The X and Y coordinates store the scaling factor to apply to texture
        /// coordinates, while the Z and W coordinates store the coordinate offsets.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">The source cube texture to copy from.</param>
        /// <param name="scaleBiasRT">Scale and bias for the destination quad.</param>
        /// <param name="mipLevelTex">Mip level of the source texture to sample.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Copy the third mip level of a source cube map texture to the center rect
        /// // of a 3x3 grid on the current render target, like so:
        /// //    +-------+      + - + - + - +
        /// //  /       / |      |   |   |   |
        /// // +-------+  |      + - +---+ - +
        /// // |       |  |  --> |   | X |   |
        /// // |       |  +      + - +---+ - +
        /// // |       | /       |   |   |   |
        /// // +-------+         + - + - + - +
        /// Vector4 center3by3 = new Vector4(1.0f/3.0f, 1.0f/3.0f, 1.0f/3.0f, 1.0f/3.0f);
        /// Blitter.BlitCubeToOctahedral2DQuadSingleChannel(cmd, source, center3by3, 2);
        /// ]]></code>
        /// </example>
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
        /// Adds in a <see cref="CommandBuffer"/> a command to perform a single channel copy off an XR compatible texture onto a
        /// portion of the current render target.
        /// </summary>
        /// <remarks>
        /// The conversion to a single channel output depends on the source texture's format:
        /// <list type="table">
        /// <listheader><term>Texture Format</term><description>Output conversion</description></listheader>
        /// <item><term>RGB(A)</term><description>the RGB luminance is written to the destination in all channels.</description></item>
        /// <item><term>Red</term><description>the red value is written to the destination in all channels.</description></item>
        /// <item><term>Alpha</term><description>the alpha value is written to the destination in all channels.</description></item>
        /// </list>
        ///
        /// The <c>scaleBiasTex</c> parameter controls the rectangle of pixels in the source texture to copy by manipulating
        /// the source texture coordinates. The X and Y coordinates store the scaling factor to apply to these texture
        /// coordinates, while the Z and W coordinates store the texture coordinate offsets.
        ///
        /// Similarly, the <c>scaleBiasRT</c> parameter controls the rectangle of pixels in the render target to write to by manipulating
        /// the destination quad coordinates. The X and Y coordinates store the scaling factor to apply to texture
        /// coordinates, while the Z and W coordinates store the coordinate offsets.
        /// </remarks>
        /// <param name="cmd">Command Buffer used for recording the action.</param>
        /// <param name="source">The source texture to copy from.</param>
        /// <param name="scaleBiasTex">Scale and bias for sampling the source texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the destination quad.</param>
        /// <param name="mipLevelTex">Mip level of the source texture to sample.</param>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Copy the top right quadrant of the source texture's first mip level to the bottom left
        /// // quadrant of the current render target, without bilinear filtering.
        /// Vector4 topRight = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        /// Vector4 bottomLeft = new Vector4(0.5f, 0.5f, 0.0f, 0.0f);
        /// Blitter.BlitQuadSingleChannel(cmd, source, topRight, bottomLeft, 0, false);
        /// ]]></code>
        /// </example>
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
