using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    internal class LightCookieManager : IDisposable
    {
        static class ShaderProperty
        {
            public static readonly int mainLightTexture = Shader.PropertyToID("_MainLightCookieTexture");
            public static readonly int mainLightWorldToLight = Shader.PropertyToID("_MainLightWorldToLight");
            public static readonly int mainLightCookieTextureFormat = Shader.PropertyToID("_MainLightCookieTextureFormat");

            public static readonly int additionalLightsCookieAtlasTexture = Shader.PropertyToID("_AdditionalLightsCookieAtlasTexture");
            public static readonly int additionalLightsCookieAtlasTextureFormat = Shader.PropertyToID("_AdditionalLightsCookieAtlasTextureFormat");

            public static readonly int additionalLightsCookieEnableBits = Shader.PropertyToID("_AdditionalLightsCookieEnableBits");

            public static readonly int additionalLightsCookieAtlasUVRectBuffer = Shader.PropertyToID("_AdditionalLightsCookieAtlasUVRectBuffer");
            public static readonly int additionalLightsCookieAtlasUVRects = Shader.PropertyToID("_AdditionalLightsCookieAtlasUVRects");

            // TODO: these should be generic light property
            public static readonly int additionalLightsWorldToLightBuffer = Shader.PropertyToID("_AdditionalLightsWorldToLightBuffer");
            public static readonly int additionalLightsLightTypeBuffer = Shader.PropertyToID("_AdditionalLightsLightTypeBuffer");

            public static readonly int additionalLightsWorldToLights = Shader.PropertyToID("_AdditionalLightsWorldToLights");
            public static readonly int additionalLightsLightTypes = Shader.PropertyToID("_AdditionalLightsLightTypes");
        }

        private enum LightCookieShaderFormat
        {
            None = -1,

            RGB = 0,
            Alpha = 1,
            Red = 2
        }

        public struct Settings
        {
            public struct AtlasSettings
            {
                public Vector2Int resolution;
                public GraphicsFormat format;

                public bool isPow2 => Mathf.IsPowerOfTwo(resolution.x) && Mathf.IsPowerOfTwo(resolution.y);
                public bool isSquare => resolution.x == resolution.y;
            }

            public AtlasSettings atlas;
            public int maxAdditionalLights;        // UniversalRenderPipeline.maxVisibleAdditionalLights;
            public float cubeOctahedralSizeScale;  // Cube octahedral projection size scale.
            public bool useStructuredBuffer;       // RenderingUtils.useStructuredBuffer

            public static Settings Create()
            {
                Settings s;
                s.atlas.resolution = new Vector2Int(1024, 1024);
                s.atlas.format = GraphicsFormat.R8G8B8A8_SRGB;
                s.maxAdditionalLights = UniversalRenderPipeline.maxVisibleAdditionalLights;

                // (Scale * W * Scale * H) / (6 * WH) == (Scale^2 / 6)
                // 1: 1/6 = 16%, 2: 4/6 = 66%, 4: 16/6 == 266% of cube pixels
                // 100% cube pixels == sqrt(6) ~= 2.45f --> 2.5;
                s.cubeOctahedralSizeScale = 2.5f;
                s.useStructuredBuffer = RenderingUtils.useStructuredBuffer;
                return s;
            }
        }

        private struct LightCookieMapping
        {
            public ushort visibleLightIndex; // Index into visible light (src)
            public ushort lightBufferIndex;  // Index into light shader data buffer (dst)
            public Light light; // Cached built-in light for the visibleLightIndex. Avoids multiple copies on all the gets from native array.

            public static Func<LightCookieMapping, LightCookieMapping, int> s_CompareByCookieSize = (LightCookieMapping a, LightCookieMapping b) =>
            {
                var alc = a.light.cookie;
                var blc = b.light.cookie;
                int a2 = alc.width * alc.height;
                int b2 = blc.width * blc.height;
                int d = b2 - a2;
                if (d == 0)
                {
                    // Sort by texture ID if "undecided" to batch fetches to the same cookie texture.
                    int ai = alc.GetInstanceID();
                    int bi = blc.GetInstanceID();
                    return ai - bi;
                }
                return d;
            };

            public static Func<LightCookieMapping, LightCookieMapping, int> s_CompareByBufferIndex = (LightCookieMapping a, LightCookieMapping b) =>
            {
                return a.lightBufferIndex - b.lightBufferIndex;
            };
        }

        private readonly struct WorkSlice<T>
        {
            private readonly T[] m_Data;
            private readonly int m_Start;
            private readonly int m_Length;

            public WorkSlice(T[] src, int srcLen = -1) : this(src, 0, srcLen) { }

            public WorkSlice(T[] src, int srcStart, int srcLen = -1)
            {
                m_Data = src;
                m_Start = srcStart;
                m_Length = (srcLen < 0) ? src.Length : Math.Min(srcLen, src.Length);
                Assertions.Assert.IsTrue(m_Start + m_Length <= capacity);
            }

            public T this[int index]
            {
                get => m_Data[m_Start + index];
                set => m_Data[m_Start + index] = value;
            }

            public int length => m_Length;
            public int capacity => m_Data.Length;

            public void Sort(Func<T, T, int> compare)
            {
                if (m_Length > 1)
                    Sorting.QuickSort(m_Data, m_Start, m_Start + m_Length - 1, compare);
            }
        }

        // Persistent work/temp memory of [] data.
        private class WorkMemory
        {
            public LightCookieMapping[] lightMappings;
            public Vector4[] uvRects;

            public void Resize(int size)
            {
                if (size <= lightMappings?.Length)
                    return;

                // Avoid allocs on every tiny size change.
                size = Math.Max(size, ((size + 15) / 16) * 16);

                lightMappings = new LightCookieMapping[size];
                uvRects = new Vector4[size];
            }
        }



        /// Must match light data layout.
        private class LightCookieShaderData : IDisposable
        {
            int m_Size = 0;
            bool m_UseStructuredBuffer;

            // Shader data CPU arrays, used to upload the data to GPU
            Matrix4x4[] m_WorldToLightCpuData;
            Vector4[] m_AtlasUVRectCpuData;
            float[] m_LightTypeCpuData;
            ShaderBitArray m_CookieEnableBitsCpuData;

            // Compute buffer counterparts for the CPU data
            ComputeBuffer m_WorldToLightBuffer;    // TODO: WorldToLight matrices should be general property of lights!!
            ComputeBuffer m_AtlasUVRectBuffer;
            ComputeBuffer m_LightTypeBuffer;

            public Matrix4x4[] worldToLights => m_WorldToLightCpuData;
            public ShaderBitArray cookieEnableBits => m_CookieEnableBitsCpuData;
            public Vector4[] atlasUVRects => m_AtlasUVRectCpuData;
            public float[] lightTypes => m_LightTypeCpuData;

            public bool isUploaded { get; set; }

            public LightCookieShaderData(int size, bool useStructuredBuffer)
            {
                m_UseStructuredBuffer = useStructuredBuffer;
                Resize(size);
            }

            public void Dispose()
            {
                if (m_UseStructuredBuffer)
                {
                    m_WorldToLightBuffer?.Dispose();
                    m_AtlasUVRectBuffer?.Dispose();
                    m_LightTypeBuffer?.Dispose();
                }
            }

            public void Resize(int size)
            {
                if (size <= m_Size)
                    return;

                if (m_Size > 0)
                    Dispose();

                m_WorldToLightCpuData = new Matrix4x4[size];
                m_AtlasUVRectCpuData = new Vector4[size];
                m_LightTypeCpuData = new float[size];
                m_CookieEnableBitsCpuData.Resize(size);

                if (m_UseStructuredBuffer)
                {
                    m_WorldToLightBuffer = new ComputeBuffer(size, Marshal.SizeOf<Matrix4x4>());
                    m_AtlasUVRectBuffer = new ComputeBuffer(size, Marshal.SizeOf<Vector4>());
                    m_LightTypeBuffer = new ComputeBuffer(size, Marshal.SizeOf<float>());
                }

                m_Size = size;
            }

            public void Upload(CommandBuffer cmd)
            {
                if (m_UseStructuredBuffer)
                {
                    m_WorldToLightBuffer.SetData(m_WorldToLightCpuData);
                    m_AtlasUVRectBuffer.SetData(m_AtlasUVRectCpuData);
                    m_LightTypeBuffer.SetData(m_LightTypeCpuData);

                    cmd.SetGlobalBuffer(ShaderProperty.additionalLightsWorldToLightBuffer, m_WorldToLightBuffer);
                    cmd.SetGlobalBuffer(ShaderProperty.additionalLightsCookieAtlasUVRectBuffer, m_AtlasUVRectBuffer);
                    cmd.SetGlobalBuffer(ShaderProperty.additionalLightsLightTypeBuffer, m_LightTypeBuffer);
                }
                else
                {
                    cmd.SetGlobalMatrixArray(ShaderProperty.additionalLightsWorldToLights, m_WorldToLightCpuData);
                    cmd.SetGlobalVectorArray(ShaderProperty.additionalLightsCookieAtlasUVRects, m_AtlasUVRectCpuData);
                    cmd.SetGlobalFloatArray(ShaderProperty.additionalLightsLightTypes, m_LightTypeCpuData);
                }

                cmd.SetGlobalFloatArray(ShaderProperty.additionalLightsCookieEnableBits, m_CookieEnableBitsCpuData.data);
                isUploaded = true;
            }

            public void Clear(CommandBuffer cmd)
            {
                if (isUploaded)
                {
                    // Set all lights to disabled/invalid state
                    m_CookieEnableBitsCpuData.Clear();
                    cmd.SetGlobalFloatArray(ShaderProperty.additionalLightsCookieEnableBits, m_CookieEnableBitsCpuData.data);
                    isUploaded = false;
                }
            }
        }

        // Unity defines directional light UVs over a unit box centered at light.
        // i.e. (0, 1) uv == (-0.5, 0.5) world area instead of the (0,1) world area.
        static readonly Matrix4x4 s_DirLightProj = Matrix4x4.Ortho(-0.5f, 0.5f, -0.5f, 0.5f, -0.5f, 0.5f);

        Texture2DAtlas m_AdditionalLightsCookieAtlas;
        LightCookieShaderData m_AdditionalLightsCookieShaderData;

        readonly Settings m_Settings;
        WorkMemory m_WorkMem;

        // Mapping: map[visibleLightIndex] = ShaderDataIndex
        // Mostly used by deferred rendering.
        int[] m_VisibleLightIndexToShaderDataIndex;

        // Parameters for rescaling cookies to fit into the atlas.
        const int k_MaxCookieSizeDivisor = 16;
        int m_CookieSizeDivisor = 1;
        uint m_PrevCookieRequestPixelCount = 0xFFFFFFFF;

        // TODO: replace with a proper error system
        // Frame "timestamp" of last warning to throttle warn messages.
        int m_PrevWarnFrame = -1;

        internal bool IsKeywordLightCookieEnabled { get; private set; }

        public LightCookieManager(ref Settings settings)
        {
            m_Settings = settings;
            m_WorkMem = new WorkMemory();
        }

        void InitAdditionalLights(int size)
        {
            // No mip padding support.
            m_AdditionalLightsCookieAtlas = new Texture2DAtlas(
                m_Settings.atlas.resolution.x,
                m_Settings.atlas.resolution.y,
                m_Settings.atlas.format,
                FilterMode.Bilinear,
                false,
                "Universal Light Cookie Atlas",
                false); // to support mips, use Pow2Atlas


            m_AdditionalLightsCookieShaderData = new LightCookieShaderData(size, m_Settings.useStructuredBuffer);
            const int mainLightCount = 1;
            m_VisibleLightIndexToShaderDataIndex = new int[m_Settings.maxAdditionalLights + mainLightCount];

            m_CookieSizeDivisor = 1;
            m_PrevCookieRequestPixelCount = 0xFFFFFFFF;
        }

        public bool isInitialized() => m_AdditionalLightsCookieAtlas != null && m_AdditionalLightsCookieShaderData != null;

        /// <summary>
        /// Release LightCookieManager resources.
        /// </summary>
        public void Dispose()
        {
            m_AdditionalLightsCookieAtlas?.Release();
            m_AdditionalLightsCookieShaderData?.Dispose();
        }

        // -1 on invalid/disabled cookie.
        public int GetLightCookieShaderDataIndex(int visibleLightIndex)
        {
            if (!isInitialized())
                return -1;

            return m_VisibleLightIndexToShaderDataIndex[visibleLightIndex];
        }

        public void Setup(ScriptableRenderContext ctx, CommandBuffer cmd, ref LightData lightData)
        {
            using var profScope = new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.LightCookies));

            // Main light, 1 directional, bound directly
            bool isMainLightAvailable = lightData.mainLightIndex >= 0;
            if (isMainLightAvailable)
            {
                var mainLight = lightData.visibleLights[lightData.mainLightIndex];
                isMainLightAvailable = SetupMainLight(cmd, ref mainLight);
            }

            // Additional lights, N spot and point lights in atlas
            bool isAdditionalLightsAvailable = lightData.additionalLightsCount > 0;
            if (isAdditionalLightsAvailable)
            {
                isAdditionalLightsAvailable = SetupAdditionalLights(cmd, ref lightData);
            }

            // Ensure cookies are disabled if no cookies are available.
            if (!isAdditionalLightsAvailable)
            {
                // ..on the CPU (for deferred)
                if (m_VisibleLightIndexToShaderDataIndex != null &&
                    m_AdditionalLightsCookieShaderData.isUploaded)
                {
                    int len = m_VisibleLightIndexToShaderDataIndex.Length;
                    for (int i = 0; i < len; i++)
                        m_VisibleLightIndexToShaderDataIndex[i] = -1;
                }

                // ..on the GPU
                m_AdditionalLightsCookieShaderData?.Clear(cmd);
            }

            // Main and additional lights are merged into one keyword to reduce variants.
            IsKeywordLightCookieEnabled = isMainLightAvailable || isAdditionalLightsAvailable;
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightCookies, IsKeywordLightCookieEnabled);
        }

        bool SetupMainLight(CommandBuffer cmd, ref VisibleLight visibleMainLight)
        {
            var mainLight = visibleMainLight.light;
            var cookieTexture = mainLight.cookie;
            bool isMainLightCookieEnabled = cookieTexture != null;

            if (isMainLightCookieEnabled)
            {
                Matrix4x4 cookieUVTransform = Matrix4x4.identity;
                float cookieFormat = (float)GetLightCookieShaderFormat(cookieTexture.graphicsFormat);

                if (mainLight.TryGetComponent(out UniversalAdditionalLightData additionalLightData))
                    GetLightUVScaleOffset(ref additionalLightData, ref cookieUVTransform);

                Matrix4x4 cookieMatrix = s_DirLightProj * cookieUVTransform *
                    visibleMainLight.localToWorldMatrix.inverse;

                cmd.SetGlobalTexture(ShaderProperty.mainLightTexture, cookieTexture);
                cmd.SetGlobalMatrix(ShaderProperty.mainLightWorldToLight, cookieMatrix);
                cmd.SetGlobalFloat(ShaderProperty.mainLightCookieTextureFormat, cookieFormat);
            }
            else
            {
                // Make sure we erase stale data in case the main light is disabled but cookie system is enabled (for additional lights).
                cmd.SetGlobalTexture(ShaderProperty.mainLightTexture, Texture2D.whiteTexture);
                cmd.SetGlobalMatrix(ShaderProperty.mainLightWorldToLight, Matrix4x4.identity);
                cmd.SetGlobalFloat(ShaderProperty.mainLightCookieTextureFormat, (float)LightCookieShaderFormat.None);
            }

            return isMainLightCookieEnabled;
        }

        private LightCookieShaderFormat GetLightCookieShaderFormat(GraphicsFormat cookieFormat)
        {
            // TODO: convert this to use GraphicsFormatUtility
            switch (cookieFormat)
            {
                default:
                    return LightCookieShaderFormat.RGB;
                // A8, A16 GraphicsFormat does not expose yet.
                case (GraphicsFormat)54:
                case (GraphicsFormat)55:
                    return LightCookieShaderFormat.Alpha;
                case GraphicsFormat.R8_SRGB:
                case GraphicsFormat.R8_UNorm:
                case GraphicsFormat.R8_UInt:
                case GraphicsFormat.R8_SNorm:
                case GraphicsFormat.R8_SInt:
                case GraphicsFormat.R16_UNorm:
                case GraphicsFormat.R16_UInt:
                case GraphicsFormat.R16_SNorm:
                case GraphicsFormat.R16_SInt:
                case GraphicsFormat.R16_SFloat:
                case GraphicsFormat.R32_UInt:
                case GraphicsFormat.R32_SInt:
                case GraphicsFormat.R32_SFloat:
                case GraphicsFormat.R_BC4_SNorm:
                case GraphicsFormat.R_BC4_UNorm:
                case GraphicsFormat.R_EAC_SNorm:
                case GraphicsFormat.R_EAC_UNorm:
                    return LightCookieShaderFormat.Red;
            }
        }

        private void GetLightUVScaleOffset(ref UniversalAdditionalLightData additionalLightData, ref Matrix4x4 uvTransform)
        {
            Vector2 uvScale = Vector2.one / additionalLightData.lightCookieSize;
            Vector2 uvOffset = additionalLightData.lightCookieOffset;

            if (Mathf.Abs(uvScale.x) < half.MinValue)
                uvScale.x = Mathf.Sign(uvScale.x) * half.MinValue;
            if (Mathf.Abs(uvScale.y) < half.MinValue)
                uvScale.y = Mathf.Sign(uvScale.y) * half.MinValue;

            uvTransform = Matrix4x4.Scale(new Vector3(uvScale.x, uvScale.y, 1));
            uvTransform.SetColumn(3, new Vector4(-uvOffset.x * uvScale.x, -uvOffset.y * uvScale.y, 0, 1));
        }

        bool SetupAdditionalLights(CommandBuffer cmd, ref LightData lightData)
        {
            int maxLightCount = Math.Min(m_Settings.maxAdditionalLights, lightData.visibleLights.Length);
            m_WorkMem.Resize(maxLightCount);

            int validLightCount = FilterAndValidateAdditionalLights(ref lightData, m_WorkMem.lightMappings);

            // Early exit if no valid cookie lights
            if (validLightCount <= 0)
                return false;

            // Lazy init GPU resources
            if (!isInitialized())
                InitAdditionalLights(validLightCount);

            // Update Atlas
            var validLights = new WorkSlice<LightCookieMapping>(m_WorkMem.lightMappings, validLightCount);
            int validUVRectCount = UpdateAdditionalLightsAtlas(cmd, ref validLights, m_WorkMem.uvRects);

            // Upload shader data
            var validUvRects = new WorkSlice<Vector4>(m_WorkMem.uvRects, validUVRectCount);
            UploadAdditionalLights(cmd, ref lightData, ref validLights, ref validUvRects);

            bool isAdditionalLightsEnabled = validUvRects.length > 0;
            return isAdditionalLightsEnabled;
        }

        int FilterAndValidateAdditionalLights(ref LightData lightData, LightCookieMapping[] validLightMappings)
        {
            int skipMainLightIndex = lightData.mainLightIndex;
            int lightBufferOffset = 0;
            int validLightCount = 0;

            int visibleLightCount = lightData.visibleLights.Length;
            for (int i = 0; i < visibleLightCount; i++)
            {
                if (i == skipMainLightIndex)
                {
                    lightBufferOffset -= 1;
                    continue;
                }

                ref var visLight = ref lightData.visibleLights.UnsafeElementAtMutable(i);
                Light light = visLight.light;

                // Skip lights without a cookie texture
                if (light.cookie == null)
                    continue;

                // Only spot and point lights are supported.
                // Directional lights are not currently supported,
                // they have very few use cases for multiple global cookies.
                // Warn on dropped lights
                var lightType = visLight.lightType;
                if (!(lightType == LightType.Spot ||
                      lightType == LightType.Point))
                {
                    Debug.LogWarning($"Additional {lightType.ToString()} light called '{light.name}' has a light cookie which will not be visible.", light);
                    continue;
                }

                Assertions.Assert.IsTrue(i < ushort.MaxValue);

                LightCookieMapping lp;
                lp.visibleLightIndex = (ushort)i;
                lp.lightBufferIndex = (ushort)(i + lightBufferOffset);
                lp.light = light;

                if (lp.lightBufferIndex >= validLightMappings.Length || validLightCount + 1 >= validLightMappings.Length)
                {
                    // TODO: Better error system
                    if (visibleLightCount > m_Settings.maxAdditionalLights &&
                        Time.frameCount - m_PrevWarnFrame > 60 * 60 * 30) // warn throttling: ~60 FPS * 60 secs * 30 mins
                    {
                        m_PrevWarnFrame = Time.frameCount;
                        Debug.LogWarning($"Max light cookies ({validLightMappings.Length.ToString()}) reached. Some visible lights ({(visibleLightCount - i - 1).ToString()}) might skip light cookie rendering.");
                    }

                    // Always break, buffer full.
                    break;
                }

                validLightMappings[validLightCount++] = lp;
            }

            return validLightCount;
        }

        int UpdateAdditionalLightsAtlas(CommandBuffer cmd, ref WorkSlice<LightCookieMapping> validLightMappings, Vector4[] textureAtlasUVRects)
        {
            // Sort in-place by cookie size for better atlas allocation efficiency (and deduplication)
            validLightMappings.Sort(LightCookieMapping.s_CompareByCookieSize);

            uint cookieRequestPixelCount = ComputeCookieRequestPixelCount(ref validLightMappings);
            var atlasSize = m_AdditionalLightsCookieAtlas.AtlasTexture.referenceSize;
            float requestAtlasRatio = cookieRequestPixelCount / (float)(atlasSize.x * atlasSize.y);
            int cookieSizeDivisorApprox = ApproximateCookieSizeDivisor(requestAtlasRatio);

            // Try to recover resolution and scale the cookies back up.
            // If the cookies "should fit" and
            // If we have less requested pixels than the last time we found the correct divisor (a guard against retrying every frame).
            if (cookieSizeDivisorApprox < m_CookieSizeDivisor &&
                cookieRequestPixelCount < m_PrevCookieRequestPixelCount)
            {
                m_AdditionalLightsCookieAtlas.ResetAllocator();
                m_CookieSizeDivisor = cookieSizeDivisorApprox;
            }


            // Get cached atlas uv rectangles.
            // If there's new cookies, first try to add at current scaling level.
            // (This can result in suboptimal packing & scaling (additions aren't sorted), but reduces rebuilds.)
            // If it doesn't fit, scale down and rebuild the atlas until it fits.
            int uvRectCount = 0;
            while (uvRectCount <= 0)
            {
                uvRectCount = FetchUVRects(cmd, ref validLightMappings, textureAtlasUVRects, m_CookieSizeDivisor);

                if (uvRectCount <= 0)
                {
                    // Uv rect fetching failed, reset and try again.
                    m_AdditionalLightsCookieAtlas.ResetAllocator();

                    // Reduce cookie size to approximate value try to rebuild the atlas.
                    m_CookieSizeDivisor = Mathf.Max(m_CookieSizeDivisor + 1, cookieSizeDivisorApprox);
                    m_PrevCookieRequestPixelCount = cookieRequestPixelCount;
                }
            }

            return uvRectCount;
        }

        int FetchUVRects(CommandBuffer cmd, ref WorkSlice<LightCookieMapping> validLightMappings, Vector4[] textureAtlasUVRects, int cookieSizeDivisor)
        {
            int uvRectCount = 0;
            for (int i = 0; i < validLightMappings.length; i++)
            {
                var lcm = validLightMappings[i];

                Light light = lcm.light;
                Texture cookie = light.cookie;

                // NOTE: Currently we blit directly on addition (on atlas fetch cache miss).
                //   This can be costly if there are many resize rebuilds (in case "out-of-space", which shouldn't be a common case).
                //   If rebuilds become a problem, we could try to just allocate and blit only when we have a fully valid allocation.
                //   It would also make sense to do atlas operations only for unique textures and then reuse the results for similar cookies.
                Vector4 uvScaleOffset = Vector4.zero;
                if (cookie.dimension == TextureDimension.Cube)
                {
                    Assertions.Assert.IsTrue(light.type == LightType.Point);
                    uvScaleOffset = FetchCube(cmd, cookie, cookieSizeDivisor);
                }
                else
                {
                    Assertions.Assert.IsTrue(light.type == LightType.Spot || light.type == LightType.Directional, "Light type needs 2D texture!");
                    uvScaleOffset = Fetch2D(cmd, cookie, cookieSizeDivisor);
                }

                bool isCached = uvScaleOffset != Vector4.zero;
                if (!isCached)
                {
                    if (cookieSizeDivisor > k_MaxCookieSizeDivisor)
                    {
                        Debug.LogWarning($"Light cookies atlas is extremely full! Some of the light cookies were discarded. Increase light cookie atlas space or reduce the amount of unique light cookies.");
                        // Complete fail, return what we have.
                        return uvRectCount;
                    }

                    // Failed to get uv rect for each cookie, fail and try again.
                    return 0;
                }

                // Adjust atlas UVs for OpenGL
                if (!SystemInfo.graphicsUVStartsAtTop)
                    uvScaleOffset.w = 1.0f - uvScaleOffset.w - uvScaleOffset.y;

                textureAtlasUVRects[uvRectCount++] = uvScaleOffset;
            }

            return uvRectCount;
        }

        uint ComputeCookieRequestPixelCount(ref WorkSlice<LightCookieMapping> validLightMappings)
        {
            uint requestPixelCount = 0;
            int prevCookieID = 0;
            for (int i = 0; i < validLightMappings.length; i++)
            {
                var lcm = validLightMappings[i];
                Texture cookie = lcm.light.cookie;
                int cookieID = cookie.GetInstanceID();

                // Consider only unique textures as atlas request pixels
                // NOTE: relies on same cookies being sorted together
                // (we need sorting for good atlas packing anyway)
                if (cookieID == prevCookieID)
                {
                    continue;
                }
                prevCookieID = cookieID;

                int pixelCookieCount = cookie.width * cookie.height;
                requestPixelCount += (uint)pixelCookieCount;
            }

            return requestPixelCount;
        }

        int ApproximateCookieSizeDivisor(float requestAtlasRatio)
        {
            // (Edge / N)^2 == 1/N^2 of area.
            // Ratio/N^2 == 1, sqrt(Ratio) == N, for "1:1" ratio.
            return (int)Mathf.Max(Mathf.Ceil(Mathf.Sqrt(requestAtlasRatio)), 1);
        }

        Vector4 Fetch2D(CommandBuffer cmd, Texture cookie, int cookieSizeDivisor = 1)
        {
            Assertions.Assert.IsTrue(cookie != null);
            Assertions.Assert.IsTrue(cookie.dimension == TextureDimension.Tex2D);

            Vector4 uvScaleOffset = Vector4.zero;

            var scaledWidth = Mathf.Max(cookie.width / cookieSizeDivisor, 4);
            var scaledHeight = Mathf.Max(cookie.height / cookieSizeDivisor, 4);
            Vector2 scaledCookieSize = new Vector2(scaledWidth, scaledHeight);

            bool isCached = m_AdditionalLightsCookieAtlas.IsCached(out uvScaleOffset, cookie);
            if (isCached)
            {
                // Update contents IF required
                m_AdditionalLightsCookieAtlas.UpdateTexture(cmd, cookie, ref uvScaleOffset);
            }
            else
            {
                m_AdditionalLightsCookieAtlas.AllocateTexture(cmd, ref uvScaleOffset, cookie, scaledWidth, scaledHeight);
            }

            AdjustUVRect(ref uvScaleOffset, cookie, ref scaledCookieSize);
            return uvScaleOffset;
        }

        Vector4 FetchCube(CommandBuffer cmd, Texture cookie, int cookieSizeDivisor = 1)
        {
            Assertions.Assert.IsTrue(cookie != null);
            Assertions.Assert.IsTrue(cookie.dimension == TextureDimension.Cube);

            Vector4 uvScaleOffset = Vector4.zero;

            // Scale octahedral projection, so that cube -> oct2D pixel count match better.
            int scaledOctCookieSize = Mathf.Max(ComputeOctahedralCookieSize(cookie) / cookieSizeDivisor, 4);

            bool isCached = m_AdditionalLightsCookieAtlas.IsCached(out uvScaleOffset, cookie);
            if (isCached)
            {
                // Update contents IF required
                m_AdditionalLightsCookieAtlas.UpdateTexture(cmd, cookie, ref uvScaleOffset);
            }
            else
            {
                m_AdditionalLightsCookieAtlas.AllocateTexture(cmd, ref uvScaleOffset, cookie, scaledOctCookieSize, scaledOctCookieSize);
            }

            // Cookie size in the atlas might not match CookieTexture size.
            // UVRect adjustment must be done with size in atlas.
            var scaledCookieSize = Vector2.one * scaledOctCookieSize;
            AdjustUVRect(ref uvScaleOffset, cookie, ref scaledCookieSize);
            return uvScaleOffset;
        }

        int ComputeOctahedralCookieSize(Texture cookie)
        {
            // Map 6*WxH pixels into 2W*2H pixels, so 4/6 ratio or 66% of cube pixels.
            int octCookieSize = Math.Max(cookie.width, cookie.height);
            if (m_Settings.atlas.isPow2)
                octCookieSize = octCookieSize * Mathf.NextPowerOfTwo((int)m_Settings.cubeOctahedralSizeScale);
            else
                octCookieSize = (int)(octCookieSize * m_Settings.cubeOctahedralSizeScale + 0.5f);
            return octCookieSize;
        }

        private void AdjustUVRect(ref Vector4 uvScaleOffset, Texture cookie, ref Vector2 cookieSize)
        {
            if (uvScaleOffset != Vector4.zero)
            {
                // Shrink by 0.5px to clamp the bilinear sampling to exclude atlas neighbors (no padding)
                ShrinkUVRect(ref uvScaleOffset, 0.5f, ref cookieSize);
            }
        }

        private void ShrinkUVRect(ref Vector4 uvScaleOffset, float amountPixels, ref Vector2 cookieSize)
        {
            var shrinkOffset = Vector2.one * amountPixels / cookieSize;
            var shrinkScale = (cookieSize - Vector2.one * (amountPixels * 2)) / cookieSize;
            uvScaleOffset.z += uvScaleOffset.x * shrinkOffset.x;
            uvScaleOffset.w += uvScaleOffset.y * shrinkOffset.y;
            uvScaleOffset.x *= shrinkScale.x;
            uvScaleOffset.y *= shrinkScale.y;
        }

        void UploadAdditionalLights(CommandBuffer cmd, ref LightData lightData, ref WorkSlice<LightCookieMapping> validLightMappings, ref WorkSlice<Vector4> validUvRects)
        {
            Assertions.Assert.IsTrue(m_AdditionalLightsCookieAtlas != null);
            Assertions.Assert.IsTrue(m_AdditionalLightsCookieShaderData != null);

            cmd.SetGlobalTexture(ShaderProperty.additionalLightsCookieAtlasTexture, m_AdditionalLightsCookieAtlas.AtlasTexture);
            cmd.SetGlobalFloat(ShaderProperty.additionalLightsCookieAtlasTextureFormat, (float)GetLightCookieShaderFormat(m_AdditionalLightsCookieAtlas.AtlasTexture.rt.graphicsFormat));

            // Resize and clear visible light to shader data mapping
            if (m_VisibleLightIndexToShaderDataIndex.Length < lightData.visibleLights.Length)
                m_VisibleLightIndexToShaderDataIndex = new int[lightData.visibleLights.Length];

            // Clear
            int len = Math.Min(m_VisibleLightIndexToShaderDataIndex.Length, lightData.visibleLights.Length);
            for (int i = 0; i < len; i++)
                m_VisibleLightIndexToShaderDataIndex[i] = -1;

            // Resize or init shader data.
            m_AdditionalLightsCookieShaderData.Resize(m_Settings.maxAdditionalLights);

            var worldToLights = m_AdditionalLightsCookieShaderData.worldToLights;
            var cookieEnableBits = m_AdditionalLightsCookieShaderData.cookieEnableBits;
            var atlasUVRects = m_AdditionalLightsCookieShaderData.atlasUVRects;
            var lightTypes = m_AdditionalLightsCookieShaderData.lightTypes;

            // Set all rects to "Invalid" zero area (Vector4.zero), just in case they're accessed.
            Array.Clear(atlasUVRects, 0, atlasUVRects.Length);
            // Set all cookies disabled
            cookieEnableBits.Clear();

            // NOTE: technically, we don't need to upload constants again if we knew the lights, atlas (rects) or visible order haven't changed.
            // But detecting that, might be as time consuming as just doing the work.

            // Fill shader data. Layout should match primary light data for additional lights.
            // Currently it's the same as visible lights, but main light(s) dropped.
            for (int i = 0; i < validUvRects.length; i++)
            {
                int visIndex = validLightMappings[i].visibleLightIndex;
                int bufIndex = validLightMappings[i].lightBufferIndex;

                // Update the mapping
                m_VisibleLightIndexToShaderDataIndex[visIndex] = bufIndex;

                ref var visLight = ref lightData.visibleLights.UnsafeElementAtMutable(visIndex);

                // Update the (cpu) data
                lightTypes[bufIndex] = (int)visLight.lightType;
                worldToLights[bufIndex] = visLight.localToWorldMatrix.inverse;
                atlasUVRects[bufIndex] = validUvRects[i];
                cookieEnableBits[bufIndex] = true;

                // Spot projection
                if (visLight.lightType == LightType.Spot)
                {
                    // VisibleLight.localToWorldMatrix only contains position & rotation.
                    // Multiply projection for spot light.
                    float spotAngle = visLight.spotAngle;
                    float spotRange = visLight.range;
                    var perp = Matrix4x4.Perspective(spotAngle, 1, 0.001f, spotRange);

                    // Cancel embedded camera view axis flip (https://docs.unity3d.com/2021.1/Documentation/ScriptReference/Matrix4x4.Perspective.html)
                    perp.SetColumn(2, perp.GetColumn(2) * -1);

                    // world -> light local -> light perspective
                    worldToLights[bufIndex] = perp * worldToLights[bufIndex];
                }
            }

            // Apply changes and upload to GPU
            m_AdditionalLightsCookieShaderData.Upload(cmd);
        }
    }
}
