using System;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Default instance of a RTHandleSystem
    /// </summary>
    public static class RTHandles
    {
        static RTHandleSystem s_DefaultInstance = new RTHandleSystem();

        /// <summary>
        /// Maximum allocated width of the default RTHandle System
        /// </summary>
        public static int maxWidth { get { return s_DefaultInstance.GetMaxWidth(); } }
        /// <summary>
        /// Maximum allocated height of the default RTHandle System
        /// </summary>
        public static int maxHeight { get { return s_DefaultInstance.GetMaxHeight(); } }
        /// <summary>
        /// Current properties of the default RTHandle System
        /// </summary>
        public static RTHandleProperties rtHandleProperties { get { return s_DefaultInstance.rtHandleProperties; } }

        /// <summary>
        /// Calculate the dimensions (in pixels) of the RTHandles given the scale factor.
        /// </summary>
        /// <param name="scaleFactor">The scale factor to use when calculating the dimensions. The base unscaled size used, is the sizes passed to the last ResetReferenceSize call.</param>
        /// <returns>The calculated dimensions.</returns>
        public static Vector2Int CalculateDimensions(Vector2 scaleFactor)
        {
            return s_DefaultInstance.CalculateDimensions(scaleFactor);
        }

        /// <summary>
        /// Calculate the dimensions (in pixels) of the RTHandles given the scale function. The base unscaled size used, is the sizes passed to the last ResetReferenceSize call.
        /// </summary>
        /// <param name="scaleFunc">The scale function to use when calculating the dimensions.</param>
        /// <returns>The calculated dimensions.</returns>
        public static Vector2Int CalculateDimensions(ScaleFunc scaleFunc)
        {
            return s_DefaultInstance.CalculateDimensions(scaleFunc);
        }

        /// <summary>
        /// Allocate a new fixed sized RTHandle with the default RTHandle System.
        /// </summary>
        /// <param name="width">With of the RTHandle.</param>
        /// <param name="height">height of the RTHandle.</param>
        /// <param name="slices">Number of slices of the RTHandle.</param>
        /// <param name="depthBufferBits">Bit depths of a depth buffer.</param>
        /// <param name="colorFormat">GraphicsFormat of a color buffer.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="dimension">Texture dimension of the RTHandle.</param>
        /// <param name="enableRandomWrite">Set to true to enable UAV random read writes on the texture.</param>
        /// <param name="useMipMap">Set to true if the texture should have mipmaps.</param>
        /// <param name="autoGenerateMips">Set to true to automatically generate mipmaps.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="msaaSamples">Number of MSAA samples for the RTHandle.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="useDynamicScaleExplicit">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="vrUsage">Special treatment of the VR eye texture used in stereoscopic rendering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public static RTHandle Alloc(
            int width,
            int height,
            int slices = 1,
            DepthBits depthBufferBits = DepthBits.None,
            GraphicsFormat colorFormat = GraphicsFormat.R8G8B8A8_SRGB,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            bool useDynamicScaleExplicit = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            string name = ""
        )
        {
            return s_DefaultInstance.Alloc(
                width,
                height,
                slices,
                depthBufferBits,
                colorFormat,
                filterMode,
                wrapMode,
                dimension,
                enableRandomWrite,
                useMipMap,
                autoGenerateMips,
                isShadowMap,
                anisoLevel,
                mipMapBias,
                msaaSamples,
                bindTextureMS,
                useDynamicScale,
                useDynamicScaleExplicit,
                memoryless,
                vrUsage,
                name
            );
        }

        /// <summary>
        /// Allocate a new fixed sized RTHandle with the default RTHandle System.
        /// </summary>
        /// <param name="width">With of the RTHandle.</param>
        /// <param name="height">height of the RTHandle.</param>
        /// <param name="format">GraphicsFormat of a color or depth stencil buffer.</param>
        /// <param name="slices">Number of slices of the RTHandle.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="dimension">Texture dimension of the RTHandle.</param>
        /// <param name="enableRandomWrite">Set to true to enable UAV random read writes on the texture.</param>
        /// <param name="useMipMap">Set to true if the texture should have mipmaps.</param>
        /// <param name="autoGenerateMips">Set to true to automatically generate mipmaps.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="msaaSamples">Number of MSAA samples for the RTHandle.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="useDynamicScaleExplicit">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="vrUsage">Special treatment of the VR eye texture used in stereoscopic rendering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public static RTHandle Alloc(
            int width,
            int height,
            GraphicsFormat format,
            int slices = 1,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            bool useDynamicScaleExplicit = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            string name = ""
        )
        {
            return s_DefaultInstance.Alloc(
                width,
                height,
                format,
                slices,
                filterMode,
                wrapMode,
                dimension,
                enableRandomWrite,
                useMipMap,
                autoGenerateMips,
                isShadowMap,
                anisoLevel,
                mipMapBias,
                msaaSamples,
                bindTextureMS,
                useDynamicScale,
                useDynamicScaleExplicit,
                memoryless,
                vrUsage,
                name
            );
        }

        /// <summary>
        /// Allocate a new fixed sized RTHandle with the default RTHandle System.
        /// </summary>
        /// <param name="width">With of the RTHandle.</param>
        /// <param name="height">height of the RTHandle.</param>
        /// <param name="wrapModeU">U coordinate wrapping mode of the RTHandle.</param>
        /// <param name="wrapModeV">V coordinate wrapping mode of the RTHandle.</param>
        /// <param name="wrapModeW">W coordinate wrapping mode of the RTHandle.</param>
        /// <param name="slices">Number of slices of the RTHandle.</param>
        /// <param name="depthBufferBits">Bit depths of a depth buffer.</param>
        /// <param name="colorFormat">GraphicsFormat of a color buffer.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="dimension">Texture dimension of the RTHandle.</param>
        /// <param name="enableRandomWrite">Set to true to enable UAV random read writes on the texture.</param>
        /// <param name="useMipMap">Set to true if the texture should have mipmaps.</param>
        /// <param name="autoGenerateMips">Set to true to automatically generate mipmaps.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="msaaSamples">Number of MSAA samples for the RTHandle.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="useDynamicScaleExplicit">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="vrUsage">Special treatment of the VR eye texture used in stereoscopic rendering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public static RTHandle Alloc(
            int width,
            int height,
            TextureWrapMode wrapModeU,
            TextureWrapMode wrapModeV,
            TextureWrapMode wrapModeW = TextureWrapMode.Repeat,
            int slices = 1,
            DepthBits depthBufferBits = DepthBits.None,
            GraphicsFormat colorFormat = GraphicsFormat.R8G8B8A8_SRGB,
            FilterMode filterMode = FilterMode.Point,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            bool useDynamicScaleExplicit = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            string name = ""
        )
        {
            return s_DefaultInstance.Alloc(
                width,
                height,
                wrapModeU,
                wrapModeV,
                wrapModeW,
                slices,
                depthBufferBits,
                colorFormat,
                filterMode,
                dimension,
                enableRandomWrite,
                useMipMap,
                autoGenerateMips,
                isShadowMap,
                anisoLevel,
                mipMapBias,
                msaaSamples,
                bindTextureMS,
                useDynamicScale,
                useDynamicScaleExplicit,
                memoryless,
                vrUsage,
                name
            );
        }

        /// <summary>
        /// Allocate a new fixed sized RTHandle with the default RTHandle System.
        /// </summary>
        /// <param name="width">With of the RTHandle.</param>
        /// <param name="height">Height of the RTHandle.</param>
        /// <param name="info">Struct containing details of allocation</param>
        /// <returns>A new RTHandle.</returns>
        public static RTHandle Alloc(int width, int height, RTHandleAllocInfo info)
        {
            return s_DefaultInstance.Alloc(width, height, info);
        }

        /// <summary>
        /// Allocate a new fixed sized RTHandle with the default RTHandle System.
        /// </summary>
        /// <param name="descriptor">RenderTexture descriptor of the RTHandle.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public static RTHandle Alloc(
            in RenderTextureDescriptor descriptor,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0,
            string name = ""
        )
        {
            return s_DefaultInstance.Alloc(descriptor.width, descriptor.height,
                GetRTHandleAllocInfo(descriptor, filterMode, wrapMode, anisoLevel, mipMapBias, name));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal GraphicsFormat GetFormat(GraphicsFormat colorFormat, GraphicsFormat depthStencilFormat)
        {
            return (depthStencilFormat==GraphicsFormat.None) ? colorFormat : depthStencilFormat;
        }

        // Internal RtDesc to RtAllocInfo conversion utility function.
        // Keep internal for future changes.
        //
        // NOTE: It has no default values to avoid param ambiguity of the flat RtHandleAlloc API.
        // NOTE: There isn't 100% field to field mapping, so some fields might need manual adjustment afterwards.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RTHandleAllocInfo GetRTHandleAllocInfo(in RenderTextureDescriptor desc, FilterMode filterMode, TextureWrapMode wrapMode, int anisoLevel, float mipMapBias, string name)
        {
            return new RTHandleAllocInfo(name)
            {
                slices = desc.volumeDepth,
                format = RTHandles.GetFormat(desc.graphicsFormat, desc.depthStencilFormat),
                filterMode = filterMode,
                wrapModeU = wrapMode,
                wrapModeV = wrapMode,
                wrapModeW = wrapMode,
                dimension = desc.dimension,
                enableRandomWrite = desc.enableRandomWrite,
                useMipMap = desc.useMipMap,
                autoGenerateMips = desc.autoGenerateMips,
                isShadowMap = desc.shadowSamplingMode != ShadowSamplingMode.None,
                anisoLevel = anisoLevel,
                mipMapBias = mipMapBias,
                msaaSamples = (MSAASamples)desc.msaaSamples,
                bindTextureMS = desc.bindMS,
                useDynamicScale = desc.useDynamicScale,
                useDynamicScaleExplicit = desc.useDynamicScaleExplicit,
                memoryless = desc.memoryless,
                vrUsage = desc.vrUsage,
                enableShadingRate = desc.enableShadingRate,
            };
        }

        /// <summary>
        /// Allocate a new automatically sized RTHandle for the default RTHandle System.
        /// </summary>
        /// <param name="scaleFactor">Constant scale for the RTHandle size computation.</param>
        /// <param name="slices">Number of slices of the RTHandle.</param>
        /// <param name="depthBufferBits">Bit depths of a depth buffer.</param>
        /// <param name="colorFormat">GraphicsFormat of a color buffer.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="dimension">Texture dimension of the RTHandle.</param>
        /// <param name="enableRandomWrite">Set to true to enable UAV random read writes on the texture.</param>
        /// <param name="useMipMap">Set to true if the texture should have mipmaps.</param>
        /// <param name="autoGenerateMips">Set to true to automatically generate mipmaps.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="msaaSamples">Number of MSAA samples.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="useDynamicScaleExplicit">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="vrUsage">Special treatment of the VR eye texture used in stereoscopic rendering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public static RTHandle Alloc(
            Vector2 scaleFactor,
            int slices = 1,
            DepthBits depthBufferBits = DepthBits.None,
            GraphicsFormat colorFormat = GraphicsFormat.R8G8B8A8_SRGB,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            bool useDynamicScaleExplicit = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            string name = ""
        )
        {
            return s_DefaultInstance.Alloc(
                scaleFactor,
                slices,
                depthBufferBits,
                colorFormat,
                filterMode,
                wrapMode,
                dimension,
                enableRandomWrite,
                useMipMap,
                autoGenerateMips,
                isShadowMap,
                anisoLevel,
                mipMapBias,
                msaaSamples,
                bindTextureMS,
                useDynamicScale,
                useDynamicScaleExplicit,
                memoryless,
                vrUsage,
                name
            );
        }

        /// <summary>
        /// Allocate a new automatically sized RTHandle for the default RTHandle System.
        /// </summary>
        /// <param name="scaleFactor">Constant scale for the RTHandle size computation.</param>
        /// <param name="format">GraphicsFormat of a color or depth stencil buffer.</param>
        /// <param name="slices">Number of slices of the RTHandle.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="dimension">Texture dimension of the RTHandle.</param>
        /// <param name="enableRandomWrite">Set to true to enable UAV random read writes on the texture.</param>
        /// <param name="useMipMap">Set to true if the texture should have mipmaps.</param>
        /// <param name="autoGenerateMips">Set to true to automatically generate mipmaps.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="msaaSamples">Number of MSAA samples.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="useDynamicScaleExplicit">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="vrUsage">Special treatment of the VR eye texture used in stereoscopic rendering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public static RTHandle Alloc(
            Vector2 scaleFactor,
            GraphicsFormat format,
            int slices = 1,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            bool useDynamicScaleExplicit = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            string name = ""
        )
        {
            return s_DefaultInstance.Alloc(
                scaleFactor,
                format,
                slices,
                filterMode,
                wrapMode,
                dimension,
                enableRandomWrite,
                useMipMap,
                autoGenerateMips,
                isShadowMap,
                anisoLevel,
                mipMapBias,
                msaaSamples,
                bindTextureMS,
                useDynamicScale,
                useDynamicScaleExplicit,
                memoryless,
                vrUsage,
                name
            );
        }

        /// <summary>
        /// Allocate a new automatically sized RTHandle for the default RTHandle System.
        /// </summary>
        /// <param name="scaleFactor">Constant scale for the RTHandle size computation.</param>
        /// <param name="descriptor">RenderTexture descriptor of the RTHandle.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public static RTHandle Alloc(
            Vector2 scaleFactor,
            in RenderTextureDescriptor descriptor,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0,
            string name = ""
        )
        {
            return s_DefaultInstance.Alloc(scaleFactor,
                GetRTHandleAllocInfo(descriptor, filterMode, wrapMode, anisoLevel, mipMapBias, name));
        }

        /// <summary>
        /// Allocate a new automatically sized RTHandle for the default RTHandle System.
        /// </summary>
        /// <param name="scaleFactor">Constant scale for the RTHandle size computation.</param>
        /// <param name="info">Struct containing details of allocation</param>
        /// <returns>A new RTHandle.</returns>
        public static RTHandle Alloc(Vector2 scaleFactor, RTHandleAllocInfo info)
        {
            return s_DefaultInstance.Alloc(scaleFactor, info);
        }

        /// <summary>
        /// Allocate a new automatically sized RTHandle for the default RTHandle System.
        /// </summary>
        /// <param name="scaleFunc">Function used for the RTHandle size computation.</param>
        /// <param name="slices">Number of slices of the RTHandle.</param>
        /// <param name="depthBufferBits">Bit depths of a depth buffer.</param>
        /// <param name="colorFormat">GraphicsFormat of a color buffer.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="dimension">Texture dimension of the RTHandle.</param>
        /// <param name="enableRandomWrite">Set to true to enable UAV random read writes on the texture.</param>
        /// <param name="useMipMap">Set to true if the texture should have mipmaps.</param>
        /// <param name="autoGenerateMips">Set to true to automatically generate mipmaps.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="msaaSamples">Number of MSAA samples.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="useDynamicScaleExplicit">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="vrUsage">Special treatment of the VR eye texture used in stereoscopic rendering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public static RTHandle Alloc(
            ScaleFunc scaleFunc,
            int slices = 1,
            DepthBits depthBufferBits = DepthBits.None,
            GraphicsFormat colorFormat = GraphicsFormat.R8G8B8A8_SRGB,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            bool useDynamicScaleExplicit = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            string name = ""
        )
        {
            return s_DefaultInstance.Alloc(
                scaleFunc,
                slices,
                depthBufferBits,
                colorFormat,
                filterMode,
                wrapMode,
                dimension,
                enableRandomWrite,
                useMipMap,
                autoGenerateMips,
                isShadowMap,
                anisoLevel,
                mipMapBias,
                msaaSamples,
                bindTextureMS,
                useDynamicScale,
                useDynamicScaleExplicit,
                memoryless,
                vrUsage,
                name
            );
        }

        /// <summary>
        /// Allocate a new automatically sized RTHandle for the default RTHandle System.
        /// </summary>
        /// <param name="scaleFunc">Function used for the RTHandle size computation.</param>
        /// <param name="format">GraphicsFormat of a color or depth stencil buffer.</param>
        /// <param name="slices">Number of slices of the RTHandle.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="dimension">Texture dimension of the RTHandle.</param>
        /// <param name="enableRandomWrite">Set to true to enable UAV random read writes on the texture.</param>
        /// <param name="useMipMap">Set to true if the texture should have mipmaps.</param>
        /// <param name="autoGenerateMips">Set to true to automatically generate mipmaps.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="msaaSamples">Number of MSAA samples.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="useDynamicScaleExplicit">[See Dynamic Resolution documentation](https://docs.unity3d.com/Manual/DynamicResolution.html)</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="vrUsage">Special treatment of the VR eye texture used in stereoscopic rendering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public static RTHandle Alloc(
            ScaleFunc scaleFunc,
            GraphicsFormat format,
            int slices = 1,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            bool useDynamicScaleExplicit = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            string name = ""
        )
        {
            return s_DefaultInstance.Alloc(
                scaleFunc,
                format,
                slices,
                filterMode,
                wrapMode,
                dimension,
                enableRandomWrite,
                useMipMap,
                autoGenerateMips,
                isShadowMap,
                anisoLevel,
                mipMapBias,
                msaaSamples,
                bindTextureMS,
                useDynamicScale,
                useDynamicScaleExplicit,
                memoryless,
                vrUsage,
                name
            );
        }

        /// <summary>
        /// Allocate a new automatically sized RTHandle for the default RTHandle System.
        /// </summary>
        /// <param name="scaleFunc">Function used for the RTHandle size computation.</param>
        /// <param name="descriptor">RenderTexture descriptor of the RTHandle.</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>A new RTHandle.</returns>
        public static RTHandle Alloc(
            ScaleFunc scaleFunc,
            in RenderTextureDescriptor descriptor,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0,
            string name = ""
        )
        {
            Assert.IsFalse(descriptor.graphicsFormat != GraphicsFormat.None && descriptor.depthStencilFormat != GraphicsFormat.None,
                "The RenderTextureDescriptor used to create RTHandle " + name + " contains both graphicsFormat and depthStencilFormat which is not allowed.");

            return s_DefaultInstance.Alloc(scaleFunc,
                GetRTHandleAllocInfo(descriptor, filterMode, wrapMode, anisoLevel, mipMapBias, name));
        }

        /// <summary>
        /// Allocate a new automatically sized RTHandle for the default RTHandle System.
        /// </summary>
        /// <param name="scaleFunc">Function used for the RTHandle size computation.</param>
        /// <param name="info">Struct containing details of allocation</param>
        /// <returns>A new RTHandle.</returns>
        public static RTHandle Alloc(ScaleFunc scaleFunc, RTHandleAllocInfo info)
        {
            return s_DefaultInstance.Alloc(scaleFunc, info);
        }

        /// <summary>
        /// Allocate a RTHandle from a regular Texture for the default RTHandle system.
        /// </summary>
        /// <param name="tex">Input texture</param>
        /// <returns>A new RTHandle referencing the input texture.</returns>
        public static RTHandle Alloc(Texture tex)
        {
            return s_DefaultInstance.Alloc(tex);
        }

        /// <summary>
        /// Allocate a RTHandle from a regular RenderTexture for the default RTHandle system.
        /// </summary>
        /// <param name="tex">Input texture</param>
        /// <param name="transferOwnership">To transfer ownership of the RenderTexture to the default RTHandles system, false by default</param>
        /// <returns>A new RTHandle referencing the input texture.</returns>
        public static RTHandle Alloc(RenderTexture tex, bool transferOwnership = false)
        {
            return s_DefaultInstance.Alloc(tex, transferOwnership);
        }

        /// <summary>
        /// Allocate a RTHandle from a regular render target identifier for the default RTHandle system.
        /// </summary>
        /// <param name="tex">Input render target identifier.</param>
        /// <returns>A new RTHandle referencing the input render target identifier.</returns>
        public static RTHandle Alloc(RenderTargetIdentifier tex)
        {
            return s_DefaultInstance.Alloc(tex);
        }

        /// <summary>
        /// Allocate a RTHandle from a regular render target identifier for the default RTHandle system.
        /// </summary>
        /// <param name="tex">Input render target identifier.</param>
        /// <param name="name">Name of the render target.</param>
        /// <returns>A new RTHandle referencing the input render target identifier.</returns>
        public static RTHandle Alloc(RenderTargetIdentifier tex, string name)
        {
            return s_DefaultInstance.Alloc(tex, name);
        }

        private static RTHandle Alloc(RTHandle tex)
        {
            Debug.LogError("Allocation a RTHandle from another one is forbidden.");
            return null;
        }

        /// <summary>
        /// Initialize the default RTHandle system.
        /// </summary>
        /// <param name="width">Initial reference rendering width.</param>
        /// <param name="height">Initial reference rendering height.</param>
        public static void Initialize(int width, int height)
        {
            s_DefaultInstance.Initialize(width, height);
        }

        /// <summary>
        /// Initialize the default RTHandle system.
        /// </summary>
        /// <param name="width">Initial reference rendering width.</param>
        /// <param name="height">Initial reference rendering height.</param>
        /// <param name="useLegacyDynamicResControl">Use legacy hardware DynamicResolution control in the default RTHandle system.</param>
        [Obsolete("useLegacyDynamicResControl is deprecated. Please use SetHardwareDynamicResolutionState() instead. #from(2023.3)")]
        public static void Initialize(int width, int height, bool useLegacyDynamicResControl = false)
        {
            s_DefaultInstance.Initialize(width, height, useLegacyDynamicResControl);
        }

        /// <summary>
        /// Release memory of a RTHandle from the default RTHandle System
        /// </summary>
        /// <param name="rth">RTHandle that should be released.</param>
        public static void Release(RTHandle rth)
        {
            s_DefaultInstance.Release(rth);
        }

        /// <summary>
        /// Enable or disable hardware dynamic resolution for the default RTHandle System
        /// </summary>
        /// <param name="hwDynamicResRequested">State of hardware dynamic resolution.</param>
        public static void SetHardwareDynamicResolutionState(bool hwDynamicResRequested)
        {
            s_DefaultInstance.SetHardwareDynamicResolutionState(hwDynamicResRequested);
        }

        /// <summary>
        /// Sets the reference rendering size for subsequent rendering for the default RTHandle System
        /// </summary>
        /// <param name="width">Reference rendering width for subsequent rendering.</param>
        /// <param name="height">Reference rendering height for subsequent rendering.</param>
        public static void SetReferenceSize(int width, int height)
        {
            s_DefaultInstance.SetReferenceSize(width, height);
        }

        /// <summary>
        /// Reset the reference size of the system and reallocate all textures.
        /// </summary>
        /// <param name="width">New width.</param>
        /// <param name="height">New height.</param>
        public static void ResetReferenceSize(int width, int height)
        {
            s_DefaultInstance.ResetReferenceSize(width, height);
        }

        /// <summary>
        /// Returns the ratio against the current target's max resolution
        /// </summary>
        /// <param name="width">width to utilize</param>
        /// <param name="height">height to utilize</param>
        /// <returns> retruns the width,height / maxTargetSize.xy ratio. </returns>
        public static Vector2 CalculateRatioAgainstMaxSize(int width, int height)
        {
            return s_DefaultInstance.CalculateRatioAgainstMaxSize(new Vector2Int(width, height));
        }
    }
}
