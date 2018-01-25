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
        enum RTCategory
        {
            Regular = 0,
            MSAA = 1,
            Count
        }

        // Static management.
        public static int s_MaxWidth { get { return s_MaxWidths[(int)RTCategory.Regular]; } }
        public static int s_MaxHeight { get { return s_MaxHeights[(int)RTCategory.Regular]; } }

        public static int s_MaxWidthMSAAA { get { return s_MaxWidths[(int)RTCategory.MSAA]; } }
        public static int s_MaxHeightMSAA { get { return s_MaxHeights[(int)RTCategory.MSAA]; } }

        private static int GetMaxWith(RTCategory category) { return s_MaxWidths[(int)category]; }
        private static int GetMaxHeight(RTCategory category) { return s_MaxHeights[(int)category]; }

        static List<RTHandle>   s_AutoSizedRTs;
        static RTCategory       s_CurrentCategory = RTCategory.Regular;

        static int[] s_MaxWidths = new int[(int)RTCategory.Count];
        static int[] s_MaxHeights = new int[(int)RTCategory.Count];

        public static int maxWidth { get { return GetMaxWith(s_CurrentCategory); } }
        public static int maxHeight { get { return GetMaxHeight(s_CurrentCategory); } }

        static RTHandle()
        {
            s_AutoSizedRTs = new List<RTHandle>();
            for (int i = 0; i < (int)RTCategory.Count; ++i)
            {
                s_MaxWidths[i] = 1;
                s_MaxHeights[i] = 1;
            }
        }

        public static void Release(RTHandle rth)
        {
            if(rth != null)
                rth.Release();
        }

        public static void SetReferenceSize(int width, int height, bool msaa)
        {
            // Technically, the enum could be passed as argument directly but let's not pollute public API with unnecessary complexity for now.
            RTCategory category = msaa ? RTCategory.MSAA : RTCategory.Regular;

            width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);

            if (width > GetMaxWith(category) || height > GetMaxHeight(category))
                Resize(width, height, category);
        }

        public static void ResetReferenceSize(int width, int height, bool msaa)
        {
            // Technically, the enum could be passed as argument directly but let's not pollute public API with unnecessary complexity for now.
            RTCategory category = msaa ? RTCategory.MSAA : RTCategory.Regular;

            width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);

            if (width != GetMaxWith(category) || height != GetMaxHeight(category))
                Resize(width, height, category);
        }

        static void Resize(int width, int height, RTCategory category)
        {
            s_MaxWidths[(int)category] = width;
            s_MaxHeights[(int)category] = height;

            var maxSize = new Vector2Int(width, height);
            s_CurrentCategory = category;

            foreach (var rth in s_AutoSizedRTs)
            {
                var rt = rth.m_RTs[(int)category];

                // This can happen if you create a RTH for MSAA. By default we only create the MSAA version of the target.
                // Missing version will be created when needed in the getter.
                if (rt != null)
                {
                    rt.Release();

                    Vector2Int scaledSize = rth.GetScaledSize(maxSize);

                    rt.width = Mathf.Max(scaledSize.x, 1);
                    rt.height = Mathf.Max(scaledSize.y, 1);
                    rt.Create();
                }

                rth.SetCurrentCategory(category);
            }
        }

        // This method wraps around regular RenderTexture creation.
        // There is no specific logic applied to RenderTextures created this way.
        public static RTHandle Alloc(
                int width,
                int height,
                int slices = 1,
                DepthBits depthBufferBits = DepthBits.None,
                RenderTextureFormat colorFormat = RenderTextureFormat.Default,
                FilterMode filterMode = FilterMode.Point,
                TextureWrapMode wrapMode = TextureWrapMode.Repeat,
                TextureDimension dimension = TextureDimension.Tex2D,
                bool sRGB = true,
                bool enableRandomWrite = false,
                bool useMipMap = false,
                bool autoGenerateMips = true,
                int anisoLevel = 1,
                float mipMapBias = 0f,
                MSAASamples msaaSamples = MSAASamples.None,
                bool bindTextureMS = false,
                bool useDynamicScale = false,
                VRTextureUsage vrUsage = VRTextureUsage.None,
                RenderTextureMemoryless memoryless = RenderTextureMemoryless.None
            )
        {
            RTCategory category = msaaSamples != MSAASamples.None ? RTCategory.MSAA : RTCategory.Regular;

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

            var newRT = new RTHandle(rt, category);
            newRT.useScaling = false;
            return newRT;
        }


        // Next two methods are used to allocate RenderTexture that depend on the frame settings (resolution and msaa for now)
        // RenderTextures allocated this way are meant to be defined by a scale of camera resolution (full/half/quarter resolution for example).
        // The idea is that internally the system will scale up the size of all render texture so that it amortizes with time and not reallocate when a smaller size is required (which is what happens with TemporaryRTs).
        // Since MSAA cannot be changed on the fly for a given RenderTexture, a separate instance will be created if the user requires it. This instance will be the one used after the next call of SetReferenceSize if MSAA is required.
        public static RTHandle Alloc(
                Vector2 scaleFactor,
                DepthBits depthBufferBits = DepthBits.None,
                RenderTextureFormat colorFormat = RenderTextureFormat.Default,
                FilterMode filterMode = FilterMode.Point,
                TextureWrapMode wrapMode = TextureWrapMode.Repeat,
                TextureDimension dimension = TextureDimension.Tex2D,
                bool sRGB = true,
                bool enableRandomWrite = false,
                bool useMipMap = false,
                bool autoGenerateMips = true,
                int anisoLevel = 1,
                float mipMapBias = 0f,
                MSAASamples msaaSamples = MSAASamples.None,
                bool bindTextureMS = false,
                bool useDynamicScale = false,
                VRTextureUsage vrUsage = VRTextureUsage.None,
                RenderTextureMemoryless memoryless = RenderTextureMemoryless.None
            )
        {
            RTCategory category = msaaSamples != MSAASamples.None ? RTCategory.MSAA : RTCategory.Regular;

            int width = Mathf.Max(Mathf.RoundToInt(scaleFactor.x * GetMaxWith(category)), 1);
            int height = Mathf.Max(Mathf.RoundToInt(scaleFactor.y * GetMaxHeight(category)), 1);

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
            rth.useScaling = true;
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
                DepthBits depthBufferBits = DepthBits.None,
                RenderTextureFormat colorFormat = RenderTextureFormat.Default,
                FilterMode filterMode = FilterMode.Point,
                TextureWrapMode wrapMode = TextureWrapMode.Repeat,
                TextureDimension dimension = TextureDimension.Tex2D,
                bool sRGB = true,
                bool enableRandomWrite = false,
                bool useMipMap = false,
                bool autoGenerateMips = true,
                int anisoLevel = 1,
                float mipMapBias = 0f,
                MSAASamples msaaSamples = MSAASamples.None,
                bool bindTextureMS = false,
                bool useDynamicScale = false,
                VRTextureUsage vrUsage = VRTextureUsage.None,
                RenderTextureMemoryless memoryless = RenderTextureMemoryless.None
            )
        {
            RTCategory category = msaaSamples != MSAASamples.None ? RTCategory.MSAA : RTCategory.Regular;

            var scaleFactor = scaleFunc(new Vector2Int(GetMaxWith(category), GetMaxHeight(category)));
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
            rth.useScaling = true;
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

        public static string DumpRTInfo()
        {
            string result = "";
            for (int i = 0; i < s_AutoSizedRTs.Count; ++i)
            {
                RenderTexture rt = s_AutoSizedRTs[i].rt;
                result = string.Format("{0}\nRT ({1})\t Format: {2} W: {3} H {4}\n", result, i, rt.format, rt.width, rt.height );
            }

            return result;
        }

        // Instance data
        RTCategory m_CurrentCategory = RTCategory.Regular;
        RenderTexture[] m_RTs = new RenderTexture[2];
        RenderTargetIdentifier[] m_NameIDs = new RenderTargetIdentifier[2];

        Vector2 scaleFactor = Vector2.one;
        ScaleFunc scaleFunc;

        public bool useScaling { get; private set; }

        public RenderTexture rt
        {
            get
            {
                CreateIfNeeded(m_CurrentCategory);
                return m_RTs[(int)m_CurrentCategory];
            }
        }

        public RenderTargetIdentifier nameID
        {
            get
            {
                CreateIfNeeded(m_CurrentCategory);
                return m_NameIDs[(int)m_CurrentCategory];
            }
        }

        RTHandle(RenderTexture rt, RTCategory category)
        {
            this.m_RTs[(int)category] = rt;
            this.m_NameIDs[(int)category] = new RenderTargetIdentifier(rt);

            SetCurrentCategory(category);
        }

        void CreateIfNeeded(RTCategory category)
        {
            // If a RT was first created for MSAA then the regular one might be null, in this case we create it.
            // That's why we never test the MSAA version: It should always be there if RT was declared correctly.
            if(category == RTCategory.Regular && m_RTs[(int)RTCategory.Regular] == null)
            {
                RenderTexture refRT = m_RTs[(int)RTCategory.MSAA];
                Debug.Assert(refRT != null);
                Vector2Int scaledSize = GetScaledSize(new Vector2Int(s_MaxWidth, s_MaxHeight));

                RenderTexture newRT = new RenderTexture(scaledSize.x, scaledSize.y, refRT.depth, refRT.format, refRT.sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    volumeDepth = refRT.volumeDepth,
                    filterMode = refRT.filterMode,
                    wrapMode = refRT.wrapMode,
                    dimension = refRT.dimension,
                    enableRandomWrite = refRT.enableRandomWrite,
                    useMipMap = refRT.useMipMap,
                    autoGenerateMips = refRT.autoGenerateMips,
                    anisoLevel = refRT.anisoLevel,
                    mipMapBias = refRT.mipMapBias,
                    antiAliasing = 1, // No MSAA for the regular version of the texture.
                    bindTextureMS = refRT.bindTextureMS,
                    useDynamicScale = refRT.useDynamicScale,
                    vrUsage = refRT.vrUsage,
                    memorylessMode = refRT.memorylessMode
                };
                newRT.Create();

                m_RTs[(int)RTCategory.Regular] = newRT;
                m_NameIDs[(int)RTCategory.Regular] = new RenderTargetIdentifier(newRT);
            }
        }

        void SetCurrentCategory(RTCategory category)
        {
            m_CurrentCategory = category;
        }

        public void Release()
        {
            s_AutoSizedRTs.Remove(this);
            for (int i = 0; i < (int)RTCategory.Count; ++i)
            {
                CoreUtils.Destroy(m_RTs[i]);
                m_NameIDs[i] = BuiltinRenderTextureType.None;
                m_RTs[i] = null;
            }
        }

        public Vector2Int GetScaledSize(Vector2Int refSize)
        {
            if (scaleFunc != null)
            {
                return scaleFunc(refSize);
            }
            else
            {
                return new Vector2Int(
                    x: Mathf.RoundToInt(scaleFactor.x * refSize.x),
                    y: Mathf.RoundToInt(scaleFactor.y * refSize.y)
                );
            }
        }
    }
}
