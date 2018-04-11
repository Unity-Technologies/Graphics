using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public delegate Vector2Int ScaleFunc(Vector2Int size);

    public partial class RTHandleSystem
    {
        internal enum RTCategory
        {
            Regular = 0,
            MSAA = 1,
            Count
        }

        // Parameters for auto-scaled Render Textures
        bool             m_ScaledRTSupportsMSAA = false;
        MSAASamples      m_ScaledRTCurrentMSAASamples = MSAASamples.None;
        List<RTHandle>   m_AutoSizedRTs;
        RTCategory       m_ScaledRTCurrentCategory = RTCategory.Regular;

        int[] m_MaxWidths = new int[(int)RTCategory.Count];
        int[] m_MaxHeights = new int[(int)RTCategory.Count];

        int maxWidthRegular { get { return GetMaxWidth(RTCategory.Regular); } }
        int maxHeightRegular { get { return GetMaxHeight(RTCategory.Regular); } }

        int maxWidthMSAA { get { return GetMaxWidth(RTCategory.MSAA); } }
        int maxHeightMSAA { get { return GetMaxHeight(RTCategory.MSAA); } }

        public int maxWidth { get { return GetMaxWidth(m_ScaledRTCurrentCategory); } }
        public int maxHeight { get { return GetMaxHeight(m_ScaledRTCurrentCategory); } }

        internal RTHandleSystem()
        {
            m_AutoSizedRTs = new List<RTHandle>();
            for (int i = 0; i < (int)RTCategory.Count; ++i)
            {
                m_MaxWidths[i] = 1;
                m_MaxHeights[i] = 1;
            }
        }

        // Call this once to set the initial size and allow msaa targets or not.
        public void Initialize(int width, int height, bool scaledRTsupportsMSAA, MSAASamples scaledRTMSAASamples)
        {
            Debug.Assert(m_AutoSizedRTs.Count == 0, "RTHandle.Initialize should only be called once before allocating any Render Texture.");

            for (int i = 0; i < (int)RTCategory.Count; ++i)
            {
                m_MaxWidths[i] = width;
                m_MaxHeights[i] = height;
            }

            m_ScaledRTSupportsMSAA = scaledRTsupportsMSAA;
            m_ScaledRTCurrentMSAASamples = scaledRTMSAASamples;
        }

        public void Release(RTHandle rth)
        {
            if(rth != null)
            {
                Assert.AreEqual(this, rth.m_Owner);
                rth.Release();
            }
        }

        public void SetReferenceSize(int width, int height, bool msaa, MSAASamples msaaSamples)
        {
            // Technically, the enum could be passed as argument directly but let's not pollute public API with unnecessary complexity for now.
            RTCategory category = msaa ? RTCategory.MSAA : RTCategory.Regular;

            width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);

            bool msaaSamplesChanged = msaa && (msaaSamples != m_ScaledRTCurrentMSAASamples);
            if (width > GetMaxWidth(category) || height > GetMaxHeight(category) || msaaSamplesChanged)
                Resize(width, height, category, msaaSamples);
        }

        public void ResetReferenceSize(int width, int height, bool msaa, MSAASamples msaaSamples)
        {
            // Technically, the enum could be passed as argument directly but let's not pollute public API with unnecessary complexity for now.
            RTCategory category = msaa ? RTCategory.MSAA : RTCategory.Regular;

            width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);

            bool msaaSamplesChanged = msaa && (msaaSamples != m_ScaledRTCurrentMSAASamples);
            if (width != GetMaxWidth(category) || height != GetMaxHeight(category) || msaaSamplesChanged)
                Resize(width, height, category, msaaSamples);
        }

        int GetMaxWidth(RTCategory category) { return m_MaxWidths[(int)category]; }
        int GetMaxHeight(RTCategory category) { return m_MaxHeights[(int)category]; }

        void Resize(int width, int height, RTCategory category, MSAASamples msaaSamples)
        {
            m_MaxWidths[(int)category] = width;
            m_MaxHeights[(int)category] = height;
            m_ScaledRTCurrentMSAASamples = msaaSamples;

            var maxSize = new Vector2Int(width, height);
            m_ScaledRTCurrentCategory = category;

            foreach (var rth in m_AutoSizedRTs)
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
                        rt.antiAliasing = (int)m_ScaledRTCurrentMSAASamples;

                    rt.name = CoreUtils.GetRenderTargetAutoName(rt.width, rt.height, rt.format, rth.m_Name, mips: rt.useMipMap, enableMSAA : category == RTCategory.MSAA, msaaSamples: m_ScaledRTCurrentMSAASamples);
                    rt.Create();
                }
            }
        }


        // This method wraps around regular RenderTexture creation.
        // There is no specific logic applied to RenderTextures created this way.
        public RTHandle Alloc(
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
            var newRT = new RTHandle(this);
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
        public RTHandle Alloc(
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
            bool allocForMSAA = m_ScaledRTSupportsMSAA ? enableMSAA : false;
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
        public RTHandle Alloc(
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
            bool allocForMSAA = m_ScaledRTSupportsMSAA ? enableMSAA : false;
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
        RTHandle AllocAutoSizedRenderTexture(
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

            bool allocForMSAA = m_ScaledRTSupportsMSAA ? enableMSAA : false;
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

            int msaaSamples = allocForMSAA ? (int)m_ScaledRTCurrentMSAASamples : 1;
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
                name = CoreUtils.GetRenderTargetAutoName(width, height, colorFormat, name, mips : useMipMap, enableMSAA: allocForMSAA, msaaSamples : m_ScaledRTCurrentMSAASamples)
            };
            rt.Create();

            var rth = new RTHandle(this);
            rth.SetRenderTexture(rt, category);
            rth.m_EnableMSAA = enableMSAA;
            rth.m_EnableRandomWrite = enableRandomWrite;
            rth.useScaling = true;
            rth.m_Name = name;
            m_AutoSizedRTs.Add(rth);
            return rth;
        }

        public string DumpRTInfo()
        {
            string result = "";
            for (int i = 0; i < m_AutoSizedRTs.Count; ++i)
            {
                RenderTexture rt = m_AutoSizedRTs[i].rt;
                result = string.Format("{0}\nRT ({1})\t Format: {2} W: {3} H {4}\n", result, i, rt.format, rt.width, rt.height );
            }

            return result;
        }
    }
}
