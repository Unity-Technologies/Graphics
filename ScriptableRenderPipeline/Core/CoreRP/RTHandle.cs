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

        private static int GetMaxWidth(RTCategory category) { return s_MaxWidths[(int)category]; }
        private static int GetMaxHeight(RTCategory category) { return s_MaxHeights[(int)category]; }


        // Parameters for auto-scaled Render Textures
        static bool             s_ScaledRTSupportsMSAA = false;
        static MSAASamples      s_ScaledRTCurrentMSAASamples = MSAASamples.None;
        static List<RTHandle>   s_AutoSizedRTs;
        static RTCategory       s_ScaledRTCurrentCategory = RTCategory.Regular;

        static int[] s_MaxWidths = new int[(int)RTCategory.Count];
        static int[] s_MaxHeights = new int[(int)RTCategory.Count];

        public static int maxWidth { get { return GetMaxWidth(s_ScaledRTCurrentCategory); } }
        public static int maxHeight { get { return GetMaxHeight(s_ScaledRTCurrentCategory); } }

        static RTHandle()
        {
            s_AutoSizedRTs = new List<RTHandle>();
            for (int i = 0; i < (int)RTCategory.Count; ++i)
            {
                s_MaxWidths[i] = 1;
                s_MaxHeights[i] = 1;
            }
        }

        // Call this once to set the initial size and allow msaa targets or not.
        public static void Initialize(int width, int height, bool scaledRTsupportsMSAA, MSAASamples scaledRTMSAASamples)
        {
            Debug.Assert(s_AutoSizedRTs.Count == 0, "RTHandle.Initialize should only be called once before allocating any Render Texture.");

            for (int i = 0; i < (int)RTCategory.Count; ++i)
            {
                s_MaxWidths[i] = width;
                s_MaxHeights[i] = height;
            }

            s_ScaledRTSupportsMSAA = scaledRTsupportsMSAA;
            s_ScaledRTCurrentMSAASamples = scaledRTMSAASamples;
        }

        public static void Release(RTHandle rth)
        {
            if(rth != null)
                rth.Release();
        }

        public static void SetReferenceSize(int width, int height, bool msaa, MSAASamples msaaSamples)
        {
            // Technically, the enum could be passed as argument directly but let's not pollute public API with unnecessary complexity for now.
            RTCategory category = msaa ? RTCategory.MSAA : RTCategory.Regular;

            width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);

            bool msaaSamplesChanged = msaa && (msaaSamples != s_ScaledRTCurrentMSAASamples);
            if (width > GetMaxWidth(category) || height > GetMaxHeight(category) || msaaSamplesChanged)
                Resize(width, height, category, msaaSamples);
        }

        public static void ResetReferenceSize(int width, int height, bool msaa, MSAASamples msaaSamples)
        {
            // Technically, the enum could be passed as argument directly but let's not pollute public API with unnecessary complexity for now.
            RTCategory category = msaa ? RTCategory.MSAA : RTCategory.Regular;

            width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);

            bool msaaSamplesChanged = msaa && (msaaSamples != s_ScaledRTCurrentMSAASamples);
            if (width != GetMaxWidth(category) || height != GetMaxHeight(category) || msaaSamplesChanged)
                Resize(width, height, category, msaaSamples);
        }

        static void Resize(int width, int height, RTCategory category, MSAASamples msaaSamples)
        {
            s_MaxWidths[(int)category] = width;
            s_MaxHeights[(int)category] = height;
            s_ScaledRTCurrentMSAASamples = msaaSamples;

            var maxSize = new Vector2Int(width, height);
            s_ScaledRTCurrentCategory = category;

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

                    if (category == RTCategory.MSAA)
                        rt.antiAliasing = (int)s_ScaledRTCurrentMSAASamples;

                    rt.name = CoreUtils.GetRenderTargetAutoName(rt.width, rt.height, rt.format, rth.m_Name, mips: rt.useMipMap, enableMSAA : category == RTCategory.MSAA, msaaSamples: s_ScaledRTCurrentMSAASamples);
                    rt.Create();
                }
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
                RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
                string name = ""
            )
        {
            bool enableMSAA = msaaSamples != MSAASamples.None;
            if (!enableMSAA && bindTextureMS == true)
            {
                Debug.LogWarning("RTHandle allocated without MSAA but with bindMS set to true, forcing bindMS to false.");
                bindTextureMS = false;
            }

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
                memorylessMode = memoryless,
                name = CoreUtils.GetRenderTargetAutoName(width, height, colorFormat, name, mips: useMipMap, enableMSAA: enableMSAA, msaaSamples: msaaSamples)
            };
            rt.Create();

            RTCategory category = enableMSAA ? RTCategory.MSAA : RTCategory.Regular;
            var newRT = new RTHandle();
            newRT.SetRenderTexture(rt, category);
            newRT.useScaling = false;
            newRT.m_EnableRandomWrite = enableRandomWrite;
            newRT.m_EnableMSAA = enableMSAA;
            newRT.m_Name = name;
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
                bool enableMSAA = false,
                bool bindTextureMS = false,
                bool useDynamicScale = false,
                VRTextureUsage vrUsage = VRTextureUsage.None,
                RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
                string name = ""
            )
        {
            bool allocForMSAA = s_ScaledRTSupportsMSAA ? enableMSAA : false;
            RTCategory category = allocForMSAA ? RTCategory.MSAA : RTCategory.Regular;

            int width = Mathf.Max(Mathf.RoundToInt(scaleFactor.x * GetMaxWidth(category)), 1);
            int height = Mathf.Max(Mathf.RoundToInt(scaleFactor.y * GetMaxHeight(category)), 1);

            var rth = AllocAutoSizedRenderTexture(width,
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
                enableMSAA,
                bindTextureMS,
                useDynamicScale,
                vrUsage,
                memoryless,
                name
            );

            rth.scaleFactor = scaleFactor;
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
                bool enableMSAA = false,
                bool bindTextureMS = false,
                bool useDynamicScale = false,
                VRTextureUsage vrUsage = VRTextureUsage.None,
                RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
                string name = ""
            )
        {
            bool allocForMSAA = s_ScaledRTSupportsMSAA ? enableMSAA : false;
            RTCategory category = allocForMSAA ? RTCategory.MSAA : RTCategory.Regular;

            var scaleFactor = scaleFunc(new Vector2Int(GetMaxWidth(category), GetMaxHeight(category)));
            int width = Mathf.Max(scaleFactor.x, 1);
            int height = Mathf.Max(scaleFactor.y, 1);

            var rth = AllocAutoSizedRenderTexture(width,
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
                enableMSAA,
                bindTextureMS,
                useDynamicScale,
                vrUsage,
                memoryless,
                name
            );

            rth.scaleFunc = scaleFunc;
            return rth;
        }

        // Internal function
        static RTHandle AllocAutoSizedRenderTexture(
                int width,
                int height,
                int slices,
                DepthBits depthBufferBits,
                RenderTextureFormat colorFormat,
                FilterMode filterMode,
                TextureWrapMode wrapMode,
                TextureDimension dimension,
                bool sRGB,
                bool enableRandomWrite,
                bool useMipMap,
                bool autoGenerateMips,
                int anisoLevel,
                float mipMapBias,
                bool enableMSAA,
                bool bindTextureMS,
                bool useDynamicScale,
                VRTextureUsage vrUsage,
                RenderTextureMemoryless memoryless,
                string name
            )
        {
            // Here user made a mistake in setting up msaa/bindMS, hence the warning
            if (!enableMSAA && bindTextureMS == true)
            {
                Debug.LogWarning("RTHandle allocated without MSAA but with bindMS set to true, forcing bindMS to false.");
                bindTextureMS = false;
            }

            bool allocForMSAA = s_ScaledRTSupportsMSAA ? enableMSAA : false;
            // Here we purposefully disable MSAA so we just force the bindMS param to false.
            if (!allocForMSAA)
            {
                bindTextureMS = false;
            }

            // MSAA Does not support random read/write.
            bool UAV = enableRandomWrite;
            if (allocForMSAA && (UAV == true))
            {
                Debug.LogWarning("RTHandle that is MSAA-enabled cannot allocate MSAA RT with 'enableRandomWrite = true'.");
                UAV = false;
            }

            int msaaSamples = allocForMSAA ? (int)s_ScaledRTCurrentMSAASamples : 1;
            RTCategory category = allocForMSAA ? RTCategory.MSAA : RTCategory.Regular;

            var rt = new RenderTexture(width, height, (int)depthBufferBits, colorFormat, sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear)
            {
                hideFlags = HideFlags.HideAndDontSave,
                volumeDepth = slices,
                filterMode = filterMode,
                wrapMode = wrapMode,
                dimension = dimension,
                enableRandomWrite = UAV,
                useMipMap = useMipMap,
                autoGenerateMips = autoGenerateMips,
                anisoLevel = anisoLevel,
                mipMapBias = mipMapBias,
                antiAliasing = msaaSamples,
                bindTextureMS = bindTextureMS,
                useDynamicScale = useDynamicScale,
                vrUsage = vrUsage,
                memorylessMode = memoryless,
                name = CoreUtils.GetRenderTargetAutoName(width, height, colorFormat, name, mips : useMipMap, enableMSAA: allocForMSAA, msaaSamples : s_ScaledRTCurrentMSAASamples)
            };
            rt.Create();

            RTHandle rth = new RTHandle();
            rth.SetRenderTexture(rt, category);
            rth.m_EnableMSAA = enableMSAA;
            rth.m_EnableRandomWrite = enableRandomWrite;
            rth.useScaling = true;
            rth.m_Name = name;
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
        RenderTexture[]             m_RTs = new RenderTexture[2];
        RenderTargetIdentifier[]    m_NameIDs = new RenderTargetIdentifier[2];
        bool                        m_EnableMSAA = false;
        bool                        m_EnableRandomWrite = false;
        string                      m_Name;

        Vector2 scaleFactor = Vector2.one;
        ScaleFunc scaleFunc;

        public bool useScaling { get; private set; }

        public RenderTexture rt
        {
            get
            {
                if(!useScaling)
                {
                    return m_EnableMSAA ? m_RTs[(int)RTCategory.MSAA] : m_RTs[(int)RTCategory.Regular];
                }
                else
                {
                    RTCategory category = (m_EnableMSAA && s_ScaledRTCurrentCategory == RTCategory.MSAA) ? RTCategory.MSAA : RTCategory.Regular;
                    CreateIfNeeded(category);
                    return m_RTs[(int)category];
                }
            }
        }

        public RenderTargetIdentifier nameID
        {
            get
            {
                if (!useScaling)
                {
                    return m_EnableMSAA ? m_NameIDs[(int)RTCategory.MSAA] : m_RTs[(int)RTCategory.Regular];
                }
                else
                {
                    RTCategory category = (m_EnableMSAA && s_ScaledRTCurrentCategory == RTCategory.MSAA) ? RTCategory.MSAA : RTCategory.Regular;
                    CreateIfNeeded(category);
                    return m_NameIDs[(int)category];
                }
            }
        }

        // Keep constructor private
        RTHandle()
        {
        }

        void SetRenderTexture(RenderTexture rt, RTCategory category)
        {
            m_RTs[(int)category] = rt;
            m_NameIDs[(int)category] = new RenderTargetIdentifier(rt);
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
                    enableRandomWrite = m_EnableRandomWrite, // We cannot take the info from the msaa rt since we force it to 1
                    useMipMap = refRT.useMipMap,
                    autoGenerateMips = refRT.autoGenerateMips,
                    anisoLevel = refRT.anisoLevel,
                    mipMapBias = refRT.mipMapBias,
                    antiAliasing = 1, // No MSAA for the regular version of the texture.
                    bindTextureMS = false, // Somehow, this can be true even if antiAliasing == 1. Leads to Unity-internal binding errors.
                    useDynamicScale = refRT.useDynamicScale,
                    vrUsage = refRT.vrUsage,
                    memorylessMode = refRT.memorylessMode,
                    name = CoreUtils.GetRenderTargetAutoName(refRT.width, refRT.height, refRT.format, m_Name, mips : refRT.useMipMap)
                };
                newRT.Create();

                m_RTs[(int)RTCategory.Regular] = newRT;
                m_NameIDs[(int)RTCategory.Regular] = new RenderTargetIdentifier(newRT);
            }
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
