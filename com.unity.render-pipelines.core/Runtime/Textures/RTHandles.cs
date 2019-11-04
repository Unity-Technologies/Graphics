using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    public static class RTHandles
    {
        static RTHandleSystem s_DefaultInstance = new RTHandleSystem();

        public static int maxWidth { get { return s_DefaultInstance.GetMaxWidth(); } }
        public static int maxHeight { get { return s_DefaultInstance.GetMaxHeight(); } }
        public static RTHandleProperties rtHandleProperties { get { return s_DefaultInstance.rtHandleProperties; } }
        public static RTHandleSystem defaultRTHandleSystem { get { return s_DefaultInstance; } }

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

        public static RTHandle Alloc(Texture tex)
        {
            return s_DefaultInstance.Alloc(tex);
        }

        public static RTHandle Alloc(RTHandle tex)
        {
            Debug.LogError("Allocation a RTHandle from another one is forbidden.");
            return null;
        }


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

        public static void Release(RTHandle rth)
        {
            s_DefaultInstance.Release(rth);
        }

        public static void SetHardwareDynamicResolutionState(bool hwDynamicResRequested)
        {
            s_DefaultInstance.SetHardwareDynamicResolutionState(hwDynamicResRequested);
        }

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
