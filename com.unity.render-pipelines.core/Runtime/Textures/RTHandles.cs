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
        /// Allocate a new fixed sized RTHandle with the default RTHandle System.
        /// </summary>
        /// <param name="width">With of the RTHandle.</param>
        /// <param name="height">Heigh of the RTHandle.</param>
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
        /// <param name="useDynamicScale">Set to true to use hardware dynamic scaling.</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns></returns>
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
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
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
                memoryless,
                name
                );
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
        /// <param name="enableMSAA">Enable MSAA for this RTHandle.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">Set to true to use hardware dynamic scaling.</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns></returns>
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
            bool enableMSAA = false,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
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
                enableMSAA,
                bindTextureMS,
                useDynamicScale,
                memoryless,
                name
                );
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
        /// <param name="enableMSAA">Enable MSAA for this RTHandle.</param>
        /// <param name="bindTextureMS">Set to true if the texture needs to be bound as a multisampled texture in the shader.</param>
        /// <param name="useDynamicScale">Set to true to use hardware dynamic scaling.</param>
        /// <param name="memoryless">Use this property to set the render texture memoryless modes.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns></returns>
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
            bool enableMSAA = false,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
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
                enableMSAA,
                bindTextureMS,
                useDynamicScale,
                memoryless,
                name
                );
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
        /// <param name="scaledRTsupportsMSAA">Set to true if automatically scaled RTHandles should support MSAA</param>
        /// <param name="scaledRTMSAASamples">Number of MSAA samples for automatically scaled RTHandles.</param>
        public static void Initialize(
            int width,
            int height,
            bool scaledRTsupportsMSAA,
            MSAASamples scaledRTMSAASamples
            )
        {
            s_DefaultInstance.Initialize(
                width,
                height,
                scaledRTsupportsMSAA,
                scaledRTMSAASamples
                );
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
        /// <param name="msaaSamples">Number of MSAA samples for multisampled textures for subsequent rendering.</param>
        public static void SetReferenceSize(
            int width,
            int height,
            MSAASamples msaaSamples
            )
        {
            s_DefaultInstance.SetReferenceSize(
                width,
                height,
                msaaSamples
                );
        }
    }
}
