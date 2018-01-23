using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public delegate Vector2Int ScaleFunc(Vector2Int size);

    public enum DepthBits
    {
        None = 0,
        Depth8 = 8,
        Depth16 = 16,
        Depth24 = 24
    }

    public enum MSAASamples
    {
        None = 1,
        MSAA2x = 2,
        MSAA4x = 4,
        MSAA8x = 8
    }

    public class RTHandle
    {
        public static int s_MaxWidth { get; private set; }
        public static int s_MaxHeight { get; private set; }

        static List<RTHandle> s_AutoSizedRTs;

        public RenderTexture rt { get; private set; }
        public RenderTargetIdentifier nameID;

        Vector2 scaleFactor = Vector2.one;
        ScaleFunc scaleFunc;

        static RTHandle()
        {
            s_AutoSizedRTs = new List<RTHandle>();
            s_MaxWidth = 1;
            s_MaxHeight = 1;
        }

        RTHandle(RenderTexture rt)
        {
            this.rt = rt;
            nameID = new RenderTargetIdentifier(rt);
        }

        public void Release()
        {
            s_AutoSizedRTs.Remove(this);
            CoreUtils.Destroy(rt);
            rt = null;
            nameID = BuiltinRenderTextureType.None;
        }

        public static void Release(RTHandle rth)
        {
            rth.Release();
        }

        public static void SetReferenceSize(int width, int height)
        {
            width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);

            if (width > s_MaxWidth || height > s_MaxHeight)
                Resize(width, height);
        }

        public static void ResetReferenceSize(int width, int height)
        {
            width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);

            if (width != s_MaxWidth || height != s_MaxHeight)
                Resize(width, height);
        }

        static void Resize(int width, int height)
        {
            s_MaxWidth = width;
            s_MaxHeight = height;
            var maxSize = new Vector2Int(s_MaxWidth, s_MaxHeight);

            foreach (var rth in s_AutoSizedRTs)
            {
                var rt = rth.rt;
                rt.Release();

                Vector2Int scaledSize;

                if (rth.scaleFunc != null)
                {
                    scaledSize = rth.scaleFunc(maxSize);
                }
                else
                {
                    scaledSize = new Vector2Int(
                        x: Mathf.RoundToInt(rth.scaleFactor.x * s_MaxWidth),
                        y: Mathf.RoundToInt(rth.scaleFactor.y * s_MaxHeight)
                    );
                }

                rt.width = Mathf.Max(scaledSize.x, 1);
                rt.height = Mathf.Max(scaledSize.y, 1);
                rt.Create();
            }
        }

        public static RTHandle Alloc(
                int width,
                int height,
                int slices                         = 1,
                DepthBits depthBufferBits          = DepthBits.None,
                RenderTextureFormat colorFormat    = RenderTextureFormat.Default,
                FilterMode filterMode              = FilterMode.Point,
                TextureWrapMode wrapMode           = TextureWrapMode.Repeat,
                TextureDimension dimension         = TextureDimension.Tex2D,
                bool sRGB                          = true,
                bool enableRandomWrite             = false,
                bool useMipMap                     = false,
                bool autoGenerateMips              = true,
                int anisoLevel                     = 1,
                float mipMapBias                   = 0f,
                MSAASamples msaaSamples            = MSAASamples.None,
                bool bindTextureMS                 = false,
                bool useDynamicScale               = false,
                VRTextureUsage vrUsage             = VRTextureUsage.None,
                RenderTextureMemoryless memoryless = RenderTextureMemoryless.None
            )
        {
            var rt = new RenderTexture(width, height, (int)depthBufferBits, colorFormat, sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear)
            {
                hideFlags = HideFlags.HideAndDontSave,
                volumeDepth = slices,
                filterMode = filterMode,
                wrapMode = wrapMode,
                dimension = dimension,
                enableRandomWrite = enableRandomWrite,
                useMipMap = useMipMap,
                autoGenerateMips = autoGenerateMips,
                anisoLevel = anisoLevel,
                mipMapBias = mipMapBias,
                antiAliasing = (int)msaaSamples,
                bindTextureMS = bindTextureMS,
                useDynamicScale = useDynamicScale,
                vrUsage = vrUsage,
                memorylessMode = memoryless
            };
            rt.Create();

            return new RTHandle(rt);
        }

        public static RTHandle Alloc(
                Vector2 scaleFactor,
                DepthBits depthBufferBits          = DepthBits.None,
                RenderTextureFormat colorFormat    = RenderTextureFormat.Default,
                FilterMode filterMode              = FilterMode.Point,
                TextureWrapMode wrapMode           = TextureWrapMode.Repeat,
                TextureDimension dimension         = TextureDimension.Tex2D,
                bool sRGB                          = true,
                bool enableRandomWrite             = false,
                bool useMipMap                     = false,
                bool autoGenerateMips              = true,
                int anisoLevel                     = 1,
                float mipMapBias                   = 0f,
                MSAASamples msaaSamples            = MSAASamples.None,
                bool bindTextureMS                 = false,
                bool useDynamicScale               = false,
                VRTextureUsage vrUsage             = VRTextureUsage.None,
                RenderTextureMemoryless memoryless = RenderTextureMemoryless.None
            )
        {
            int width = Mathf.Max(Mathf.RoundToInt(scaleFactor.x * s_MaxWidth), 1);
            int height = Mathf.Max(Mathf.RoundToInt(scaleFactor.y * s_MaxHeight), 1);

            var rth = Alloc(width,
                height,
                1,
                depthBufferBits,
                colorFormat,
                filterMode,
                wrapMode,
                dimension,
                sRGB,
                enableRandomWrite,
                useMipMap,
                autoGenerateMips,
                anisoLevel,
                mipMapBias,
                msaaSamples,
                bindTextureMS,
                useDynamicScale,
                vrUsage,
                memoryless
            );

            rth.scaleFactor = scaleFactor;
            s_AutoSizedRTs.Add(rth);
            return rth;
        }

        //
        // You can provide your own scaling function for advanced scaling schemes (e.g. scaling to
        // the next POT). The function takes a Vec2 as parameter that holds max width & height
        // values for the current manager context and returns a Vec2 of the final size in pixels.
        //
        // var rth = Alloc(
        //     size => new Vector2Int(size.x / 2, size.y),
        //     [...]
        // );
        //
        public static RTHandle Alloc(
                ScaleFunc scaleFunc,
                DepthBits depthBufferBits          = DepthBits.None,
                RenderTextureFormat colorFormat    = RenderTextureFormat.Default,
                FilterMode filterMode              = FilterMode.Point,
                TextureWrapMode wrapMode           = TextureWrapMode.Repeat,
                TextureDimension dimension         = TextureDimension.Tex2D,
                bool sRGB                          = true,
                bool enableRandomWrite             = false,
                bool useMipMap                     = false,
                bool autoGenerateMips              = true,
                int anisoLevel                     = 1,
                float mipMapBias                   = 0f,
                MSAASamples msaaSamples            = MSAASamples.None,
                bool bindTextureMS                 = false,
                bool useDynamicScale               = false,
                VRTextureUsage vrUsage             = VRTextureUsage.None,
                RenderTextureMemoryless memoryless = RenderTextureMemoryless.None
            )
        {
            var scaleFactor = scaleFunc(new Vector2Int(s_MaxWidth, s_MaxHeight));
            int width = Mathf.Max(scaleFactor.x, 1);
            int height = Mathf.Max(scaleFactor.y, 1);

            var rth = Alloc(width,
                height,
                1,
                depthBufferBits,
                colorFormat,
                filterMode,
                wrapMode,
                dimension,
                sRGB,
                enableRandomWrite,
                useMipMap,
                autoGenerateMips,
                anisoLevel,
                mipMapBias,
                msaaSamples,
                bindTextureMS,
                useDynamicScale,
                vrUsage,
                memoryless
            );

            rth.scaleFunc = scaleFunc;
            s_AutoSizedRTs.Add(rth);
            return rth;
        }

        public static implicit operator RenderTexture(RTHandle handle)
        {
            return handle.rt;
        }

        public static implicit operator RenderTargetIdentifier(RTHandle handle)
        {
            return handle.nameID;
        }
    }
}
