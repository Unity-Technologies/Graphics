using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace UnityEngine.Rendering.RenderGraphModule.Util
{
    /// <summary>
    /// Helper functions used for RenderGraph.
    /// </summary>
    public static partial class RenderGraphUtils
    {
        static MaterialPropertyBlock s_PropertyBlock = new MaterialPropertyBlock();

        /// <summary>
        /// Checks if the shader features required by the MSAA version of the copy pass is supported on current platform.
        /// </summary>
        /// <returns>Returns true if the shader features required by the copy pass is supported for MSAA, otherwise will it return false.</returns>
        public static bool CanAddCopyPassMSAA()
        {
            if (!IsFramebufferFetchEmulationMSAASupportedOnCurrentPlatform())
                return false;

            return Blitter.CanCopyMSAA();
        }

        /// <summary>
        /// Checks if the shader features required by the MSAA version of the copy pass is supported on current platform.
        /// </summary>
        /// <param name="sourceDesc">The texture description of the that will be copied from.</param>
        /// <returns>Returns true if the shader features required by the copy pass is supported for MSAA, otherwise will it return false.</returns>
        public static bool CanAddCopyPassMSAA(in TextureDesc sourceDesc)
        {
            if (!IsFramebufferFetchEmulationMSAASupportedOnCurrentPlatform())
                return false;

            return Blitter.CanCopyMSAA(sourceDesc.bindTextureMS);
        }

        /// <summary>
        /// Checks if the shader features required by the MSAA version of the copy pass is supported on current platform.
        /// </summary>
        /// <param name="bindTextureMS">The texture description of the that will be copied from.</param>
        /// <returns>Returns true if the shader features required by the copy pass is supported for MSAA, otherwise will it return false.</returns>
        public static bool CanAddCopyPassMSAA(bool bindTextureMS)
        {
            if (!IsFramebufferFetchEmulationMSAASupportedOnCurrentPlatform())
                return false;

            return Blitter.CanCopyMSAA(bindTextureMS);
        }

        internal static bool IsFramebufferFetchEmulationSupportedOnCurrentPlatform()
        {
#if PLATFORM_WEBGL
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
                return false;
#endif
            return true;
        }

        internal static bool IsFramebufferFetchEmulationMSAASupportedOnCurrentPlatform()
        {
            // TODO: Temporarily disable this utility pending a more efficient solution for supporting or disabling framebuffer fetch emulation on PS4/PS5.
            return (SystemInfo.graphicsDeviceType != GraphicsDeviceType.PlayStation4
                 && SystemInfo.graphicsDeviceType != GraphicsDeviceType.PlayStation5 && SystemInfo.graphicsDeviceType != GraphicsDeviceType.PlayStation5NGGC);
        }

        /// <summary>
        /// Determines whether framebuffer fetch is supported on the current platform for the given texture.
        /// This includes checking both general support for framebuffer fetch emulation and specific support
        /// for multisampled (MSAA) textures.
        /// </summary>
        /// <param name="graph">The RenderGraph adding this pass to.</param>
        /// <param name="tex">The texture handle to validate for framebuffer fetch compatibility.</param>
        /// <returns>
        /// Returns true if framebuffer fetch is supported on the current platform for the given texture;
        /// otherwise, returns false.
        /// </returns>
        public static bool IsFramebufferFetchSupportedOnCurrentPlatform(this RenderGraph graph, in TextureHandle tex)
        {
            if (!IsFramebufferFetchEmulationSupportedOnCurrentPlatform())
                return false;

            if (!IsFramebufferFetchEmulationMSAASupportedOnCurrentPlatform())
            {
                var sourceInfo = graph.GetRenderTargetInfo(tex);
                if (sourceInfo.msaaSamples > 1)
                    return sourceInfo.bindMS;
            }
            return true;
        }

        /// <summary>
        /// Checks whether the copy pass can be used between the given source and destination textures within the RenderGraph.
        /// </summary>
        /// <param name="graph">The RenderGraph adding this pass to.</param>
        /// <param name="source">The texture the data is copied from.</param>
        /// <param name="destination">The texture the data is copied to. This has to be different from souce.</param>
        /// <returns>True if the copy pass can be used between the given source and destination textures, false otherwise.</returns>
        public static bool CanAddCopyPass(this RenderGraph graph, TextureHandle source, TextureHandle destination)
        {
            if (!source.IsValid() || !destination.IsValid())
                return false;

            if (!graph.nativeRenderPassesEnabled)
                return false;

            if (!IsFramebufferFetchEmulationSupportedOnCurrentPlatform())
                return false;

            var sourceInfo = graph.GetRenderTargetInfo(source);
            var destinationInfo = graph.GetRenderTargetInfo(destination);

            if (sourceInfo.msaaSamples != destinationInfo.msaaSamples)
                return false;

            if (sourceInfo.width != destinationInfo.width ||
                sourceInfo.height != destinationInfo.height)
                return false;

            if (sourceInfo.volumeDepth != destinationInfo.volumeDepth)
                return false;

            // Note: Needs shader model ps_4.1 to support SV_SampleIndex which means the copy pass isn't supported for MSAA on some platforms.
            //       We can check this by checking the amout of shader passes the copy shader has.
            //       It would have 1 if the MSAA pass is not able to be used for target and 2 otherwise.
            //       https://docs.unity3d.com/2017.4/Documentation/Manual/SL-ShaderCompileTargets.html
            //       https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-to-get-sample-position
            if ((int)sourceInfo.msaaSamples > 1 && !CanAddCopyPassMSAA(sourceInfo.bindMS))
                return false;

            return true;
        }

        class CopyPassData
        {
            public bool isMSAA;
            public bool force2DForXR;
        }

        /// <summary>
        /// Adds a pass to copy data from a source texture to a destination texture and returns the builder.
        /// The data in the texture is copied pixel by pixel. The copy function can only do 1:1 copies it will not allow scaling the data or
        /// doing texture filtering. Furthermore it requires the source and destination surfaces to be the same size in pixels and have the same number of MSAA samples and array slices.
        /// If the textures are multi sampled, individual samples will be copied.
        ///
        /// Copy is intentionally limited in functionally so it can be implemented using frame buffer fetch for optimal performance on tile based GPUs. If you are looking for a more generic
        /// function please use the AddBlitPass function. To verify whether the copy pass is supported for the intended operation, use the CanAddCopyPass function.
        ///
        /// When XR is active, array textures containing both eyes will be automatically copied.
        /// 
        /// </summary>
        /// <param name="graph">The RenderGraph adding this pass to.</param>
        /// <param name="source">The texture the data is copied from.</param>
        /// <param name="destination">The texture the data is copied to. This has to be different from souce.</param>
        /// <param name="returnBuilder">The builder instance of the added copy pass.</param>
        /// <param name="passName">A name to use for debugging and error logging. This name will be shown in the rendergraph debugger. </param>
        /// <param name="file">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <param name="line">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <returns>The builder instance of the added copy pass.</returns>
        public static IBaseRenderGraphBuilder AddCopyPass(
            this RenderGraph graph,
            TextureHandle source,
            TextureHandle destination,
            string passName = "Copy Pass Utility",
            bool returnBuilder = false
#if !CORE_PACKAGE_DOCTOOLS
            , [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
#endif
        {
            if (!graph.nativeRenderPassesEnabled)
                throw new ArgumentException("CopyPass only supported for native render pass. Please use the blit functions instead for non native render pass platforms.");

            var sourceInfo = graph.GetRenderTargetInfo(source);
            var destinationInfo = graph.GetRenderTargetInfo(destination);

            if (sourceInfo.msaaSamples != destinationInfo.msaaSamples)
                throw new ArgumentException("MSAA samples from source and destination texture doesn't match.");

            if (sourceInfo.width != destinationInfo.width ||
                sourceInfo.height != destinationInfo.height)
                throw new ArgumentException("Dimensions for source and destination texture doesn't match.");

            if (sourceInfo.volumeDepth != destinationInfo.volumeDepth)
                throw new ArgumentException("Slice count for source and destination texture doesn't match.");

            var isMSAA = (int)sourceInfo.msaaSamples > 1;

            // Note: Needs shader model ps_4.1 to support SV_SampleIndex which means the copy pass isn't supported for MSAA on some platforms.
            //       We can check this by checking the amout of shader passes the copy shader has.
            //       It would have 1 if the MSAA pass is not able to be used for target and 2 otherwise.
            //       https://docs.unity3d.com/2017.4/Documentation/Manual/SL-ShaderCompileTargets.html
            //       https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-to-get-sample-position
            if (isMSAA && !CanAddCopyPassMSAA(sourceInfo.bindMS))
                throw new ArgumentException("Target does not support MSAA for AddCopyPass. Please use the blit alternative or use non MSAA textures.");

            var builder = graph.AddRasterRenderPass<CopyPassData>(passName, out var passData, file, line);

            try
            {
                bool isXRArrayTextureActive = TextureXR.useTexArray;
                bool isArrayTexture = sourceInfo.volumeDepth > 1;

                passData.isMSAA = isMSAA;
                passData.force2DForXR = isXRArrayTextureActive && (!isArrayTexture);

                builder.SetInputAttachment(source, 0, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => CopyRenderFunc(data, context));

                if (passData.force2DForXR)
                    builder.AllowGlobalStateModification(true);// So we can set the keywords
            }
            catch
            {
                builder.Dispose();
                throw;
            }

            if (returnBuilder)
                return builder;

            builder.Dispose();
            return null;
        }

        /// <summary>
        /// Adds a pass to copy data from a source texture to a destination texture. The data in the texture is copied pixel by pixel. The copy function can only do 1:1 copies it will not allow scaling the data or
        /// doing texture filtering. Furthermore it requires the source and destination surfaces to be the same size in pixels and have the same number of MSAA samples. If the textures are multi sampled
        /// individual samples will be copied.
        ///
        /// Copy is intentionally limited in functionally so it can be implemented using frame buffer fetch for optimal performance on tile based GPUs. If you are looking for a more generic
        /// function please use the AddBlitPass function. Blit will automatically decide (based on the arguments) whether to use normal rendering or to instead call copy internally.
        /// To verify whether the copy pass is supported for the intended operation, use the CanAddCopyPass function.
        ///
        /// The source/destination mip and slice arguments are ignored and were never used by this function therefore it is better to call the AddCopyPass overload without them. This function
        /// is here for backwards compatibility with existing code.
        ///
        /// When XR is active, array textures containing both eyes will be automatically copied.
        /// 
        /// </summary>
        /// <param name="graph">The RenderGraph adding this pass to.</param>
        /// <param name="source">The texture the data is copied from.</param>
        /// <param name="destination">The texture the data is copied to. This has to be different from souce.</param>
        /// <param name="sourceSlice">This argument was never used. Please use the overload without this argument instead. If you want to work with mips or array slices you can use blit or write your own frame buffer fetch based implementation.</param>
        /// <param name="destinationSlice">This argument was never used. Please use the overload without this argument instead. If you want to work with mips or array slices you can use blit or write your own frame buffer fetch based implementation.</param>
        /// <param name="sourceMip">This argument was never used. Please use the overload without this argument instead. If you want to work with mips or array slices you can use blit or write your own frame buffer fetch based implementation.</param>
        /// <param name="destinationMip">This argument was never used. Please use the overload without this argument instead. If you want to work with mips or array slices you can use blit or write your own frame buffer fetch based implementation.</param>
        /// <param name="passName">A name to use for debugging and error logging. This name will be shown in the rendergraph debugger. </param>
        /// <param name="file">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <param name="line">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        public static void AddCopyPass(
            this RenderGraph graph,
            TextureHandle source,
            TextureHandle destination,
            int sourceSlice,
            int destinationSlice = 0,
            int sourceMip = 0,
            int destinationMip = 0,
            string passName = "Copy Pass Utility"
#if !CORE_PACKAGE_DOCTOOLS
            , [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
#endif
        {
            AddCopyPass(graph, source, destination, passName, false, file, line);
        }

        static void CopyRenderFunc(CopyPassData data, RasterGraphContext rgContext)
        {
            Blitter.CopyTexture(rgContext.cmd, data.isMSAA, data.force2DForXR);
        }

        /// <summary>
        /// Try to auto detect XR textures.
        /// </summary>
        /// <param name="sourceDesc"></param>
        /// <param name="destDesc"></param>
        /// <param name="sourceSlice"></param>
        /// <param name="destinationSlice"></param>
        /// <param name="numSlices"></param>
        /// <param name="numMips"></param>
        internal static bool IsTextureXR(ref RenderTargetInfo destDesc, int sourceSlice, int destinationSlice, int numSlices, int numMips)
        {
            if (TextureXR.useTexArray &&
                  destDesc.volumeDepth > 1 &&
                  destDesc.volumeDepth == TextureXR.slices &&
                  sourceSlice == 0 &&
                  destinationSlice == 0 &&
                  numSlices == TextureXR.slices &&
                  numMips == 1)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Filtermode for the simple blit.
        /// </summary>
        public enum BlitFilterMode
        {
            /// <summary>
            /// Clamp to the nearest pixel when selecting which pixel to blit from.
            /// </summary>
            ClampNearest,
            /// <summary>
            /// Use bileanear filtering when selecting which pixels to blit from.
            /// </summary>
            ClampBilinear
        }

        class BlitPassData
        {
            public TextureHandle source;
            public TextureHandle destination;
            public Vector2 scale;
            public Vector2 offset;
            public int sourceSlice;
            public int destinationSlice;
            public int numSlices;
            public int sourceMip;
            public int destinationMip;
            public int numMips;
            public BlitFilterMode filterMode;
            public bool isXR;

        }

        /// <summary>
        /// Add a render graph pass to blit an area of the source texture into the destination texture. Blitting is a high-level way to transfer texture data from a source to a destination texture.
        /// It may scale and texture-filter the transferred data as well as doing data transformations on it (e.g. R8Unorm to float).
        ///
        /// This function does not have special handling for MSAA textures. This means that when the source is sampled this will be a resolved value (standard Unity behavior when sampling an MSAA render texture)
        /// and when the destination is MSAA all written samples will contain the same values (e.g. as you would expect when rendering a full screen quad to an msaa buffer). If you need special MSAA
        /// handling or custom resolving please use the overload that takes a Material and implement the appropriate behavior in the shader.
        ///
        /// This function works transparently with regular textures and XR textures (which may depending on the situation be 2D array textures). In the case of an XR array texture
        /// the operation will be repeated for each slice in the texture if numSlices is set to -1.
        ///
        /// </summary>
        /// <param name="graph">The RenderGraph adding this pass to.</param>
        /// <param name="source">The texture the data is copied from.</param>
        /// <param name="destination">The texture the data is copied to.</param>
        /// <param name="scale">The scale that is applied to the texture coordinates used to sample the source texture.</param>
        /// <param name="offset">The offset that is added to the texture coordinates used to sample the source texture.</param>
        /// <param name="sourceSlice"> The first slice to copy from if the texture is an 3D or array texture. Must be zero for regular textures.</param>
        /// <param name="destinationSlice"> The first slice to copy to if the texture is an 3D or array texture. Must be zero for regular textures.</param>
        /// <param name="numSlices"> The number of slices to copy. -1 to copy all slices until the end of the texture. Arguments that copy invalid slices to be copied will lead to an error.</param>
        /// <param name="sourceMip"> The first mipmap level to copy from. Must be zero for non-mipmapped textures. Must be a valid index for mipmapped textures.</param>
        /// <param name="destinationMip"> The first mipmap level to copy to. Must be zero for non-mipmapped textures. Must be a valid index for mipmapped textures.</param>
        /// <param name="numMips"> The number of mipmaps to copy. -1 to copy all mipmaps. Arguments that copy invalid mips to be copied will lead to an error.</param>
        /// <param name="filterMode">The filtering used when blitting from source to destination.</param>
        /// <param name="passName">A name to use for debugging and error logging. This name will be shown in the rendergraph debugger. </param>
        /// <param name="returnBuilder">A boolean indicating whether to return the builder instance for the blit pass.</param>
        /// <param name="file">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <param name="line">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <returns>A new instance of IBaseRenderGraphBuilder used to setup the new Render Pass, returned only if <paramref name="returnBuilder"/> is set to <c>true</c>or <c>null</c> if <paramref name="returnBuilder"/> is <c>false</c>.</returns>
        public static IBaseRenderGraphBuilder AddBlitPass(this RenderGraph graph,
            TextureHandle source,
            TextureHandle destination,
            Vector2 scale,
            Vector2 offset,
            int sourceSlice = 0,
            int destinationSlice = 0,
            int numSlices = -1,
            int sourceMip = 0,
            int destinationMip = 0,
            int numMips = 1,
            BlitFilterMode filterMode = BlitFilterMode.ClampBilinear,
            string passName = "Blit Pass Utility",
            bool returnBuilder = false
#if !CORE_PACKAGE_DOCTOOLS
                , [CallerFilePath] string file = "",
                [CallerLineNumber] int line = 0)
#endif
        {
            if (!source.IsValid())
            {
                throw new ArgumentException($"BlitPass: {passName} source needs to be a valid texture handle.");
            }
            var sourceDesc = graph.GetTextureDesc(source);

            if (!destination.IsValid())
            {
                throw new ArgumentException($"BlitPass: {passName} destination needs to be a valid texture handle.");
            }
            var destinationDesc = graph.GetRenderTargetInfo(destination);

            int sourceMaxWidth = math.max(math.max(sourceDesc.width, sourceDesc.height), sourceDesc.slices);
            int sourceTotalMipChainLevels = (int)math.log2(sourceMaxWidth) + 1;

            int destinationMaxWidth = math.max(math.max(destinationDesc.width, destinationDesc.height), destinationDesc.volumeDepth);
            int destinationTotalMipChainLevels = (int)math.log2(destinationMaxWidth) + 1;

            if (numSlices == -1) numSlices = sourceDesc.slices - sourceSlice;
            if (numSlices > sourceDesc.slices - sourceSlice
                || numSlices > destinationDesc.volumeDepth - destinationSlice)
            {
                throw new ArgumentException($"BlitPass: {passName} attempts to blit too many slices. The pass will be skipped.");
            }
            if (numMips == -1) numMips = sourceTotalMipChainLevels - sourceMip;
            if (numMips > sourceTotalMipChainLevels - sourceMip
                || numMips > destinationTotalMipChainLevels - destinationMip)
            {
                throw new ArgumentException($"BlitPass: {passName} attempts to blit too many mips. The pass will be skipped.");
            }

            var canUseCopyPass = CanAddCopyPass(graph, source, destination)
                                 && scale == Vector2.one && offset == Vector2.zero && numSlices == 1 && numMips == 1;

            if (canUseCopyPass)
            {
                return AddCopyPass(graph, source, destination, passName, returnBuilder, file, line);
            }

            var builder = graph.AddUnsafePass<BlitPassData>(passName, out var passData, file, line);
            try
            {
                passData.isXR = IsTextureXR(ref destinationDesc, sourceSlice, destinationSlice, numSlices, numMips);
                passData.source = source;
                passData.destination = destination;
                passData.scale = scale;
                passData.offset = offset;
                passData.sourceSlice = sourceSlice;
                passData.destinationSlice = destinationSlice;
                passData.numSlices = numSlices;
                passData.sourceMip = sourceMip;
                passData.destinationMip = destinationMip;
                passData.numMips = numMips;
                passData.filterMode = filterMode;
                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(destination, AccessFlags.Write);
                builder.SetRenderFunc((BlitPassData data, UnsafeGraphContext context) => BlitRenderFunc(data, context));
            }
            catch
            {
                builder.Dispose();
                throw;
            }

            if (returnBuilder)
                return builder;

            builder.Dispose();
            return null;
        }

        static Vector4 s_BlitScaleBias = new Vector4();
        static void BlitRenderFunc(BlitPassData data, UnsafeGraphContext context)
        {
            s_BlitScaleBias.x = data.scale.x;
            s_BlitScaleBias.y = data.scale.y;
            s_BlitScaleBias.z = data.offset.x;
            s_BlitScaleBias.w = data.offset.y;

            CommandBuffer unsafeCmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
            if (data.isXR)
            {
                // This is the magic that makes XR work for blit. We set the rendertargets passing -1 for the slices. This means it will bind all (both eyes) slices. 
                // The engine will then also automatically duplicate our draws and the vertex and pixel shader (through macros) will ensure those draws end up in the right eye.
                context.cmd.SetRenderTarget(data.destination, 0, CubemapFace.Unknown, -1);
                Blitter.BlitTexture(unsafeCmd, data.source, s_BlitScaleBias, data.sourceMip, data.filterMode == BlitFilterMode.ClampBilinear);
            }
            else
            {
                for (int currSlice = 0; currSlice < data.numSlices; currSlice++)
                {
                    for (int currMip = 0; currMip < data.numMips; currMip++)
                    {
                        context.cmd.SetRenderTarget(data.destination, data.destinationMip + currMip, CubemapFace.Unknown, data.destinationSlice + currSlice);
                        Blitter.BlitTexture(unsafeCmd, data.source, s_BlitScaleBias, data.sourceMip + currMip, data.sourceSlice + currSlice, data.filterMode == BlitFilterMode.ClampBilinear);
                    }
                }
            }
        }

        /// <summary>
        /// Enums to select what geometry used for the blit.
        /// </summary>
        public enum FullScreenGeometryType
        {
            /// <summary>
            /// Draw a quad mesh built of two triangles. The texture coordinates of the mesh will cover the 0-1 texture space. This is the most compatible if you have an existing Unity Graphics.Blit shader.
            /// This geometry allows you to use a simples vertex shader but has more rendering overhead on the CPU as a mesh and vertex buffers need to be bound to the pipeline.
            /// </summary>
            Mesh,
            /// <summary>
            /// A single triangle will be scheduled. The vertex shader will need to generate  correct vertex data in the vertex shader for the tree vertices to cover the full screen.
            /// To get the vertices in the vertex shader include "com.unity.render-pipelines.core\ShaderLibrary\Common.hlsl" and call the GetFullScreenTriangleTexCoord/GetFullScreenTriangleVertexPosition
            /// </summary>
            ProceduralTriangle,
            /// <summary>
            /// A four vertices forming two triangles will be scheduled. The vertex shader will need to generate correct vertex data in the vertex shader for the four vertices to cover the full screen.
            /// While more intuitive this may be slower as the quad occupancy will be lower alongside the diagonal line where the two triangles meet.
            /// To get the vertices in the vertex shader include "com.unity.render-pipelines.core\ShaderLibrary\Common.hlsl" and call the GetQuadTexCoord/GetQuadVertexPosition
            /// </summary>
            ProceduralQuad,
        }

        /// <summary>
        /// This struct specifies all the arugments to the blit-with-material function. As there are many parameters with some of them only rarely used moving them to a struct
        /// makes it easier to use the function.
        ///
        /// Use one of the constructor overloads for common use cases.
        ///
        /// By default most constructors will copy all array texture slices. This ensures XR textures are handled "automatically" without additional consideration.
        /// 
        /// The shader properties defined in the struct or constructors is used for most common usecases but they are not required to be used in the shader.
        /// By using the <c>MaterialPropertyBlock</c> can you add your shader properties with custom values.
        /// </summary>
        public struct BlitMaterialParameters
        {
            private static readonly int blitTextureProperty = Shader.PropertyToID("_BlitTexture");
            private static readonly int blitSliceProperty = Shader.PropertyToID("_BlitTexArraySlice");
            private static readonly int blitMipProperty = Shader.PropertyToID("_BlitMipLevel");
            private static readonly int blitScaleBias = Shader.PropertyToID("_BlitScaleBias");

            /// <summary>
            /// Simple constructor that sets only the most common parameters to blit. The other parameters will be set to sensible default values.
            /// 
            /// </summary>
            /// <param name="source">The texture the data is copied from.</param>
            /// <param name="destination">The texture the data is copied to.</param>
            /// <param name="material">Material used for blitting.</param>
            /// <param name="shaderPass">The shader pass index to use for the material.</param>
            public BlitMaterialParameters(TextureHandle source, TextureHandle destination, Material material, int shaderPass)
                : this(source, destination, Vector2.one, Vector2.zero, material, shaderPass) { }

            /// <summary>
            /// Simple constructor that sets only the most common parameters to blit. The other parameters will be set to sensible default values.
            /// </summary>
            /// <param name="source">The texture the data is copied from.</param>
            /// <param name="destination">The texture the data is copied to.</param>
            /// <param name="scale">Scale for sampling the input texture.</param>
            /// <param name="offset">Offset also known as bias for sampling the input texture</param>
            /// <param name="material">Material used for blitting.</param>
            /// <param name="shaderPass">The shader pass index to use for the material.</param>
            public BlitMaterialParameters(TextureHandle source, TextureHandle destination, Vector2 scale, Vector2 offset, Material material, int shaderPass)
            {
                this.source = source;
                this.destination = destination;
                this.scale = scale;
                this.offset = offset;
                sourceSlice = -1;
                destinationSlice = 0;
                numSlices = -1;
                sourceMip = -1;
                destinationMip = 0;
                numMips = 1;
                this.material = material;
                this.shaderPass = shaderPass;
                propertyBlock = null;
                sourceTexturePropertyID = blitTextureProperty;
                sourceSlicePropertyID = blitSliceProperty;
                sourceMipPropertyID = blitMipProperty;
                scaleBiasPropertyID = blitScaleBias;
                geometry = FullScreenGeometryType.ProceduralTriangle;
            }

            /// <summary>
            /// Constructor to set the source and destination mip and slices as well as material property and IDs to interact with it.
            /// </summary>
            /// <param name="source">The texture the data is copied from.</param>
            /// <param name="destination">The texture the data is copied to.</param>
            /// <param name="material">Material used for blitting.</param>
            /// <param name="shaderPass">The shader pass index to use for the material.</param>
            /// <param name="mpb">Material property block to use to render the blit. This property should contain all data the shader needs.</param>
            /// <param name="destinationSlice"> The first slice to copy to if the texture is an 3D or array texture. Must be zero for regular textures.</param>
            /// <param name="destinationMip"> The first mipmap level to copy to. Must be zero for non-mipmapped textures. Must be a valid index for mipmapped textures.</param>
            /// <param name="numSlices"> The number of slices to copy. -1 to copy all slices until the end of the texture. Arguments that copy invalid slices to be copied will lead to an error.</param>
            /// <param name="numMips"> The number of mipmaps to copy. -1 to copy all mipmaps. Arguments that copy invalid mips to be copied will lead to an error.</param>
            /// <param name="sourceSlice"> The first slice to copy from if the texture is an 3D or array texture. Must be zero for regular textures. Default is set to -1 to ignore source slices and set it to 0 without looping for each destination slice</param>
            /// <param name="sourceMip"> The first mipmap level to copy from. Must be zero for non-mipmapped textures. Must be a valid index for mipmapped textures. Defaults to -1 to ignore source mips and set it to 0 without looping for each destination mip.</param>
            /// <param name="geometry">Geometry used for blitting the source texture.</param>
            /// <param name="sourceTexturePropertyID">
            /// The texture property to set with the source texture. If -1 the default "_BlitTexture" texture property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// If propertyBlock is null the texture will be applied directly to the material.
            /// </param>
            /// <param name="sourceSlicePropertyID">
            /// The scalar property to set with the source slice index. If -1 the default "_BlitTexArraySlice" property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// If more than one slice is rendered using the blit function (numSlices>1) several full screen quads will be rendered for each slice with different sourceSlicePropertyID values set.
            /// </param>
            /// <param name="sourceMipPropertyID">
            /// The scalar property to set with the source mip index. If -1 the default "_BlitMipLevel" property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// If more than one mip is rendered using the blit function (numMips>1) several full screen quads will be rendered for each slice with different sourceMipPropertyID values set.
            /// </param>
            public BlitMaterialParameters(TextureHandle source, TextureHandle destination, Material material, int shaderPass,
                MaterialPropertyBlock mpb,
                int destinationSlice,
                int destinationMip,
                int numSlices = -1,
                int numMips = 1,
                int sourceSlice = -1,
                int sourceMip = -1,
                FullScreenGeometryType geometry = FullScreenGeometryType.Mesh,
                int sourceTexturePropertyID = -1,
                int sourceSlicePropertyID = -1,
                int sourceMipPropertyID = -1)
                : this(source, destination, Vector2.one, Vector2.zero, material, shaderPass,
                      mpb,
                      destinationSlice, destinationMip,
                      numSlices, numMips,
                      sourceSlice, sourceMip,
                      geometry,
                      sourceTexturePropertyID, sourceSlicePropertyID, sourceMipPropertyID) { }

            /// <summary>
            /// Constructor to set the source and destination mip and slices as well as material property and IDs to interact with it.
            /// </summary>
            /// <param name="source">The texture the data is copied from.</param>
            /// <param name="destination">The texture the data is copied to.</param>
            /// <param name="scale">Scale for sampling the input texture.</param>
            /// <param name="offset">Offset also known as bias for sampling the input texture</param>
            /// <param name="material">Material used for blitting.</param>
            /// <param name="shaderPass">The shader pass index to use for the material.</param>
            /// <param name="mpb">Material property block to use to render the blit. This property should contain all data the shader needs.</param>
            /// <param name="destinationSlice"> The first slice to copy to if the texture is an 3D or array texture. Must be zero for regular textures.</param>
            /// <param name="destinationMip"> The first mipmap level to copy to. Must be zero for non-mipmapped textures. Must be a valid index for mipmapped textures.</param>
            /// <param name="numSlices"> The number of slices to copy. -1 to copy all slices until the end of the texture. Arguments that copy invalid slices to be copied will lead to an error. If you are using an XR-array texture make sure you set this to -1 or the number of slices in the array our your XR texture will not be correctly copied.</param>
            /// <param name="numMips"> The number of mipmaps to copy. -1 to copy all mipmaps. Arguments that copy invalid mips to be copied will lead to an error.</param>
            /// <param name="sourceSlice"> The first slice to copy from if the texture is an 3D or array texture. Must be zero for regular textures. Default is set to -1 to ignore source slices and set it to 0 without looping for each destination slice</param>
            /// <param name="sourceMip"> The first mipmap level to copy from. Must be zero for non-mipmapped textures. Must be a valid index for mipmapped textures. Defaults to -1 to ignore source mips and set it to 0 without looping for each destination mip.</param>
            /// <param name="geometry">Geometry used for blitting the source texture.</param>
            /// <param name="sourceTexturePropertyID">
            /// The texture property to set with the source texture. If -1 the default "_BlitTexture" texture property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// If propertyBlock is null the texture will be applied directly to the material.
            /// </param>
            /// <param name="sourceSlicePropertyID">
            /// The scalar property to set with the source slice index. If -1 the default "_BlitTexArraySlice" property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// If more than one slice is rendered using the blit function (numSlices>1) several full screen quads will be rendered for each slice with different sourceSlicePropertyID values set.
            /// </param>
            /// <param name="sourceMipPropertyID">
            /// The scalar property to set with the source mip index. If -1 the default "_BlitMipLevel" property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// If more than one mip is rendered using the blit function (numMips>1) several full screen quads will be rendered for each slice with different sourceMipPropertyID values set.
            /// </param>
            /// <param name="scaleBiasPropertyID">
            /// The scalar property to set with the scale and bias known as offset. If -1 the default "_BlitScaleBias" property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// </param>
            public BlitMaterialParameters(TextureHandle source, TextureHandle destination, Vector2 scale, Vector2 offset, Material material, int shaderPass,
                MaterialPropertyBlock mpb,
                int destinationSlice,
                int destinationMip,
                int numSlices = -1,
                int numMips = 1,
                int sourceSlice = -1,
                int sourceMip = -1,
                FullScreenGeometryType geometry = FullScreenGeometryType.Mesh,
                int sourceTexturePropertyID = -1,
                int sourceSlicePropertyID = -1,
                int sourceMipPropertyID = -1,
                int scaleBiasPropertyID = -1) : this(source, destination, scale, offset, material, shaderPass)
            {
                this.propertyBlock = mpb;
                this.sourceSlice = sourceSlice;
                this.destinationSlice = destinationSlice;
                this.numSlices = numSlices;
                this.sourceMip = sourceMip;
                this.destinationMip = destinationMip;
                this.numMips = numMips;
                if (sourceTexturePropertyID != -1)
                    this.sourceTexturePropertyID = sourceTexturePropertyID;
                if (sourceSlicePropertyID != -1)
                    this.sourceSlicePropertyID = sourceSlicePropertyID;
                if (sourceMipPropertyID != -1)
                    this.sourceMipPropertyID = sourceMipPropertyID;
                if (scaleBiasPropertyID != -1)
                    this.scaleBiasPropertyID = scaleBiasPropertyID;
                this.geometry = geometry;
            }

            /// <summary>
            /// Constructor to set textures, material, shader pass and material property block.
            /// </summary>
            /// <param name="source">The texture the data is copied from.</param>
            /// <param name="destination">The texture the data is copied to.</param>
            /// <param name="material">Material used for blitting.</param>
            /// <param name="shaderPass">The shader pass index to use for the material.</param>
            /// <param name="mpb">Material property block to use to render the blit. This property should contain all data the shader needs.</param>
            /// <param name="geometry">Geometry used for blitting the source texture.</param>
            /// <param name="sourceTexturePropertyID">
            /// The texture property to set with the source texture. If -1 the default "_BlitTexture" texture property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// If propertyBlock is null the texture will be applied directly to the material.
            /// </param>
            /// <param name="sourceSlicePropertyID">
            /// The scalar property to set with the source slice index. If -1 the default "_BlitTexArraySlice" property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// If more than one slice is rendered using the blit function (numSlices>1) several full screen quads will be rendered for each slice with different sourceSlicePropertyID values set.
            /// </param>
            /// <param name="sourceMipPropertyID">
            /// The scalar property to set with the source mip index. If -1 the default "_BlitMipLevel" property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// If more than one mip is rendered using the blit function (numMips>1) several full screen quads will be rendered for each slice with different sourceMipPropertyID values set.
            /// </param>
            public BlitMaterialParameters(TextureHandle source, TextureHandle destination, Material material, int shaderPass,
                MaterialPropertyBlock mpb,
                FullScreenGeometryType geometry = FullScreenGeometryType.Mesh,
                int sourceTexturePropertyID = -1,
                int sourceSlicePropertyID = -1,
                int sourceMipPropertyID = -1)
                : this(source, destination,
                      Vector2.one, Vector2.zero,
                      material, shaderPass,
                      mpb, geometry,
                      sourceTexturePropertyID, sourceSlicePropertyID, sourceMipPropertyID) { }

            /// <summary>
            /// Constructor to set textures, material, shader pass and material property block.
            /// </summary>
            /// <param name="source">The texture the data is copied from.</param>
            /// <param name="destination">The texture the data is copied to.</param>
            /// <param name="scale">Scale for sampling the input texture.</param>
            /// <param name="offset">Offset also known as bias for sampling the input texture</param>
            /// <param name="material">Material used for blitting.</param>
            /// <param name="shaderPass">The shader pass index to use for the material.</param>
            /// <param name="mpb">Material property block to use to render the blit. This property should contain all data the shader needs.</param>
            /// <param name="geometry">Geometry used for blitting the source texture.</param>
            /// <param name="sourceTexturePropertyID">
            /// The texture property to set with the source texture. If -1 the default "_BlitTexture" texture property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// If propertyBlock is null the texture will be applied directly to the material.
            /// </param>
            /// <param name="sourceSlicePropertyID">
            /// The scalar property to set with the source slice index. If -1 the default "_BlitSlice" property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// If more than one slice is rendered using the blit function (numSlices>1) several full screen quads will be rendered for each slice with different sourceSlicePropertyID values set.
            /// </param>
            /// <param name="sourceMipPropertyID">
            /// The scalar property to set with the source mip index. If -1 the default "_BlitMipLevel" property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// If more than one mip is rendered using the blit function (numMips>1) several full screen quads will be rendered for each slice with different sourceMipPropertyID values set.
            /// </param>
            /// <param name="scaleBiasPropertyID">
            /// The scalar property to set with the scale and bias known as offset. If -1 the default "_BlitScaleBias" property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// </param>
            public BlitMaterialParameters(TextureHandle source, TextureHandle destination, Vector2 scale, Vector2 offset, Material material, int shaderPass,
                MaterialPropertyBlock mpb,
                FullScreenGeometryType geometry = FullScreenGeometryType.Mesh,
                int sourceTexturePropertyID = -1,
                int sourceSlicePropertyID = -1,
                int sourceMipPropertyID = -1,
                int scaleBiasPropertyID = -1) : this(source, destination, scale, offset, material, shaderPass)
            {
                this.propertyBlock = mpb;
                if (sourceTexturePropertyID != -1)
                    this.sourceTexturePropertyID = sourceTexturePropertyID;
                if (sourceSlicePropertyID != -1)
                    this.sourceSlicePropertyID = sourceSlicePropertyID;
                if (sourceMipPropertyID != -1)
                    this.sourceMipPropertyID = sourceMipPropertyID;
                if (scaleBiasPropertyID != -1)
                    this.scaleBiasPropertyID = scaleBiasPropertyID;
                this.geometry = geometry;
            }

            /// <summary>
            /// The source texture. This texture will be set on the specified material property block property with
            /// the name specified sourceTexturePropertyID. If the property block is null, a temp property block will
            /// be allocated by the blit function.
            /// </summary>
            public TextureHandle source;

            /// <summary>
            /// The texture to blit into. This subresources (mips,slices) of this texture  texture will be set-up as a render attachment based on the destination argumments.
            /// </summary>
            public TextureHandle destination;

            /// <summary>
            /// The scale used for the blit operation.
            /// </summary>
            public Vector2 scale;

            /// <summary>
            /// The offset of the blit destination.
            /// </summary>
            public Vector2 offset;

            /// <summary>
            /// The first slice of the source texture to blit from. -1 to ignore source slices. This will not set any values to the sourceSlicePropertyID texture parameters.
            /// If not -1, the sourceSlicePropertyID will be set between sourceSlice and sourceSlice+numSlices for each slice that is blit.
            /// </summary>
            public int sourceSlice;

            /// <summary>
            /// The first slice of the destination texture to blit into.
            /// </summary>
            public int destinationSlice;

            /// <summary>
            /// The number of slices to blit. -1 to blit all slices until the end of the texture starting from destinationSlice. Arguments that copy invalid slices (e.g. out of range or zero) will lead to an error.
            /// </summary>
            public int numSlices;

            /// <summary>
            /// The first source mipmap to blit from. -1 to ignore source mips. This will not set any values to the sourceMipPropertyID texture parameters.
            /// If not -1, the sourceMipPropertyID will be set between sourceMip and sourceMip+numMips for each mip that is blit.
            /// </summary>
            public int sourceMip;

            /// <summary>
            /// The first destination mipmap to blit into.
            /// </summary>
            public int destinationMip;

            /// <summary>
            /// The number of mipmaps to blit. -1 to blit all mipmaps until the end of the texture starting from destinationMip. Arguments that copy invalid slices (e.g. out of range or zero) will lead to an error.
            /// </summary>
            public int numMips;

            /// <summary>
            /// The material to use, cannot be null. The blit functions will not modify this material in any way.
            /// </summary>
            public Material material;

            /// <summary>
            /// The shader pass index to use.
            /// </summary>
            public int shaderPass;

            /// <summary>
            /// The material propery block to use, can be null. The blit functions will modify the sourceTexturePropertyID, sourceSliceProperty, and sourceMipPropertyID of this material poperty block as part of the blit.
            /// Calling propertyBlock's SetTexture(...) function used by BlitMaterialParameters should be avoid since it will cause untracked textures when using RenderGraph. This can cause unexpected behaviours.
            /// </summary>
            public MaterialPropertyBlock propertyBlock;

            /// <summary>
            /// The texture property to set with the source texture. If -1 the default "_BlitTexture" texture property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// If propertyBlock is null the texture will be applied directly to the material.
            /// </summary>
            public int sourceTexturePropertyID;

            /// <summary>
            /// The scalar property to set with the source slice index. If -1 the default "_BlitTexArraySlice" property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// If more than one slice is rendered using the blit function (numSlices>1) several full screen quads will be rendered for each slice with different sourceSlicePropertyID values set.
            /// If sourceSlice is -1, no values will be set on the property.
            /// </summary>
            public int sourceSlicePropertyID;

            /// <summary>
            /// The scalar property to set with the source mip index. If -1 the default "_BlitMipLevel" property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// If more than one mip is rendered using the blit function (numMips>1), several full screen quads will be rendered for each slice with different sourceMipPropertyID values set.
            /// If sourceMip is -1, no values will be set on the property.
            /// </summary>
            public int sourceMipPropertyID;

            /// <summary>
            /// The scalar property to set with the scale and bias also known as offset from the source to distination. If -1 the default "_BlitScaleBias" property will be used. Note: Use Shader.PropertyToID to convert a string property name to an ID.
            /// </summary>
            public int scaleBiasPropertyID;

            /// <summary>
            /// The type of full-screen geometry to use when rendering the blit material. See FullScreenGeometryType for details.
            /// </summary>
            public FullScreenGeometryType geometry;
        }

        class BlitMaterialPassData
        {
            public int sourceTexturePropertyID;
            public TextureHandle source;
            public TextureHandle destination;
            public Vector2 scale;
            public Vector2 offset;
            public Material material;
            public int shaderPass;
            public MaterialPropertyBlock propertyBlock;
            public int sourceSlice;
            public int destinationSlice;
            public int numSlices;
            public int sourceMip;
            public int destinationMip;
            public int numMips;
            public FullScreenGeometryType geometry;
            public int sourceSlicePropertyID;
            public int sourceMipPropertyID;
            public int scaleBiasPropertyID;
            public bool isXR;
        }

        /// <summary>
        /// Add a render graph pass to blit an area of the source texture into the destination texture and return the builder if requested.
        /// Blitting is a high-level way to transfer texture data from a source to a destination texture.
        /// In this overload the data may be transformed by an arbitrary material.
        ///
        /// This function works transparently with regular textures and XR textures (which may depending on the situation be 2D array textures) if numSlices is set to -1 and the slice property works correctly.
        ///
        /// This is a helper function to schedule a simple pass calling a single blit. If you want to call a number of blits in a row (e.g. to a slice-by-slice or mip-by-mip blit) it's generally faster
        /// to simple schedule a single pass and then do schedule blits directly on the command buffer.
        ///
        /// This function schedules a pass for execution on the rendergraph execution timeline. It's important to ensure the passed material and material property blocks correctly account for this behavior in
        /// particular the following code will likely not behave as intented:
        /// material.SetFloat("Visibility", 0.5);
        /// renderGraph.AddBlitPass(... material ...);
        /// material.SetFloat("Visibility", 0.8);
        /// renderGraph.AddBlitPass(... material ...);
        ///
        /// This will result in both passes using the float value "Visibility" as when the graph is executed the value in the material is assigned 0.8. The correct way to handle such use cases is either using two separate
        /// materials or using two separate material property blocks. E.g. :
        ///
        /// propertyBlock1.SetFloat("Visibility", 0.5);
        /// renderGraph.AddBlitPass(... material, propertyBlock1, ...);
        /// propertyBlock2.SetFloat("Visibility", 0.8);
        /// renderGraph.AddBlitPass(... material, propertyBlock2, ...);
        ///
        /// Notes on using this function:
        /// - If you need special handling of MSAA buffers this can be implemented using the bindMS flag on the source texture and per-sample pixel shader invocation on the destination texture (e.g. using SV_SampleIndex).
        /// - MaterialPropertyBlocks used for this function should not contain any textures added by MaterialPropertyBlock.SetTexture(...) as it will cause untracked textures when using RenderGraph causing uninstended behaviour.
        /// 
        /// </summary>
        /// <param name="graph">The RenderGraph adding this pass to.</param>
        /// <param name="blitParameters">Parameters used for rendering.</param>
		/// <param name="passName">A name to use for debugging and error logging. This name will be shown in the rendergraph debugger. </param>
        /// <param name="returnBuilder">A boolean indicating whether to return the builder instance for the blit pass.</param>
        /// <param name="file">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <param name="line">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <returns>A new instance of IBaseRenderGraphBuilder used to setup the new Render Pass, returned only if <paramref name="returnBuilder"/> is set to <c>true</c>or <c>null</c> if <paramref name="returnBuilder"/> is <c>false</c>.</returns>
        public static IBaseRenderGraphBuilder AddBlitPass(this RenderGraph graph,
            BlitMaterialParameters blitParameters,
            string passName = "Blit Pass Utility w. Material",
            bool returnBuilder = false
#if !CORE_PACKAGE_DOCTOOLS
                , [CallerFilePath] string file = "",
                [CallerLineNumber] int line = 0)
#endif
        {
            if (!blitParameters.destination.IsValid())
            {
                throw new ArgumentException($"BlitPass: {passName} destination needs to be a valid texture handle.");
            }

            var destinationDesc = graph.GetRenderTargetInfo(blitParameters.destination);

            // Fill in unspecified parameters automatically based on the texture descriptor
            int destinationMaxWidth = math.max(math.max(destinationDesc.width, destinationDesc.height), destinationDesc.volumeDepth);
            int destinationTotalMipChainLevels = (int)math.log2(destinationMaxWidth) + 1;
            if (blitParameters.numSlices == -1)
            {
                blitParameters.numSlices = destinationDesc.volumeDepth - blitParameters.destinationSlice;
            }

            if (blitParameters.numMips == -1)
            {
                blitParameters.numMips = destinationTotalMipChainLevels - blitParameters.destinationMip;
            }

            // Validate against the source if available
            if (blitParameters.source.IsValid())
            {
                var sourceDesc = graph.GetTextureDesc(blitParameters.source);
                int sourceMaxWidth = math.max(math.max(sourceDesc.width, sourceDesc.height), sourceDesc.slices);
                int sourceTotalMipChainLevels = (int)math.log2(sourceMaxWidth) + 1;

                if (blitParameters.sourceSlice != -1 && blitParameters.numSlices > sourceDesc.slices - blitParameters.sourceSlice)
                {
                    throw new ArgumentException($"BlitPass: {passName} attempts to blit too many slices. There are not enough slices in the source array. The pass will be skipped.");
                }

                if (blitParameters.sourceMip != -1 && blitParameters.numMips > sourceTotalMipChainLevels - blitParameters.sourceMip)
                {
                    throw new ArgumentException($"BlitPass: {passName} attempts to blit too many mips. There are not enough mips in the source texture. The pass will be skipped.");
                }
            }

            // Validate against destination
            if (blitParameters.numSlices > destinationDesc.volumeDepth - blitParameters.destinationSlice)
            {
                throw new ArgumentException($"BlitPass: {passName} attempts to blit too many slices. There are not enough slices in the destination array. The pass will be skipped.");
            }

            if (blitParameters.numMips > destinationTotalMipChainLevels - blitParameters.destinationMip)
            {
                throw new ArgumentException($"BlitPass: {passName} attempts to blit too many mips. There are not enough mips in the destination texture. The pass will be skipped.");
            }

            if (blitParameters.material == null)
            {
                throw new ArgumentException($"BlitPass: {passName} attempts to use a null material.");
            }

            var builder = graph.AddUnsafePass<BlitMaterialPassData>(passName, out var passData, file, line);
            try
            {
                passData.sourceTexturePropertyID = blitParameters.sourceTexturePropertyID;
                passData.source = blitParameters.source;
                passData.destination = blitParameters.destination;
                passData.scale = blitParameters.scale;
                passData.offset = blitParameters.offset;
                passData.material = blitParameters.material;
                passData.shaderPass = blitParameters.shaderPass;
                passData.propertyBlock = blitParameters.propertyBlock;
                passData.sourceSlice = blitParameters.sourceSlice;
                passData.destinationSlice = blitParameters.destinationSlice;
                passData.numSlices = blitParameters.numSlices;
                passData.sourceMip = blitParameters.sourceMip;
                passData.destinationMip = blitParameters.destinationMip;
                passData.numMips = blitParameters.numMips;
                passData.geometry = blitParameters.geometry;
                passData.sourceSlicePropertyID = blitParameters.sourceSlicePropertyID;
                passData.sourceMipPropertyID = blitParameters.sourceMipPropertyID;
                passData.scaleBiasPropertyID = blitParameters.scaleBiasPropertyID;

                passData.isXR = IsTextureXR(ref destinationDesc, (passData.sourceSlice == -1) ? 0 : passData.sourceSlice, passData.destinationSlice, passData.numSlices, passData.numMips);
                if (blitParameters.source.IsValid())
                {
                    builder.UseTexture(blitParameters.source);
                }
                builder.UseTexture(blitParameters.destination, AccessFlags.Write);
                builder.SetRenderFunc((BlitMaterialPassData data, UnsafeGraphContext context) => BlitMaterialRenderFunc(data, context));
            }
            catch
            {
                builder.Dispose();
                throw;
            }

            if (returnBuilder)
                return builder;

            builder.Dispose();
            return null;
        }

        static void BlitMaterialRenderFunc(BlitMaterialPassData data, UnsafeGraphContext context)
        {
            s_BlitScaleBias.x = data.scale.x;
            s_BlitScaleBias.y = data.scale.y;
            s_BlitScaleBias.z = data.offset.x;
            s_BlitScaleBias.w = data.offset.y;

            CommandBuffer unsafeCmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

            if (data.propertyBlock == null) data.propertyBlock = s_PropertyBlock;

            if (data.source.IsValid())
            {
                data.propertyBlock.SetTexture(data.sourceTexturePropertyID, data.source);
            }

            data.propertyBlock.SetVector(data.scaleBiasPropertyID, s_BlitScaleBias);

            if (data.isXR)
            {
                // This is the magic that makes XR work for blit. We set the rendertargets passing -1 for the slices. This means it will bind all (both eyes) slices. 
                // The engine will then also automatically duplicate our draws and the vertex and pixel shader (through macros) will ensure those draws end up in the right eye.

                if (data.sourceSlice != -1)
                    data.propertyBlock.SetInt(data.sourceSlicePropertyID, 0);
                if (data.sourceMip != -1)
                    data.propertyBlock.SetInt(data.sourceMipPropertyID, data.sourceMip);

                context.cmd.SetRenderTarget(data.destination, 0, CubemapFace.Unknown, -1);
                switch (data.geometry)
                {
                    case FullScreenGeometryType.Mesh:
                        Blitter.DrawQuadMesh(unsafeCmd, data.material, data.shaderPass, data.propertyBlock);
                        break;
                    case FullScreenGeometryType.ProceduralQuad:
                        Blitter.DrawQuad(unsafeCmd, data.material, data.shaderPass, data.propertyBlock);
                        break;
                    case FullScreenGeometryType.ProceduralTriangle:
                        Blitter.DrawTriangle(unsafeCmd, data.material, data.shaderPass, data.propertyBlock);
                        break;
                }
            }
            else
            {
                for (int currSlice = 0; currSlice < data.numSlices; currSlice++)
                {
                    for (int currMip = 0; currMip < data.numMips; currMip++)
                    {
                        if (data.sourceSlice != -1)
                            data.propertyBlock.SetInt(data.sourceSlicePropertyID, data.sourceSlice + currSlice);
                        if (data.sourceMip != -1)
                            data.propertyBlock.SetInt(data.sourceMipPropertyID, data.sourceMip + currMip);

                        context.cmd.SetRenderTarget(data.destination, data.destinationMip + currMip, CubemapFace.Unknown, data.destinationSlice + currSlice);
                        switch (data.geometry)
                        {
                            case FullScreenGeometryType.Mesh:
                                Blitter.DrawQuadMesh(unsafeCmd, data.material, data.shaderPass, data.propertyBlock);
                                break;
                            case FullScreenGeometryType.ProceduralQuad:
                                Blitter.DrawQuad(unsafeCmd, data.material, data.shaderPass, data.propertyBlock);
                                break;
                            case FullScreenGeometryType.ProceduralTriangle:
                                Blitter.DrawTriangle(unsafeCmd, data.material, data.shaderPass, data.propertyBlock);
                                break;
                        }
                    }
                }
            }
        }
    }
}
