using System;
using UnityEngine.Experimental.Rendering;
using Unity.Mathematics;
using System.Runtime.InteropServices;

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
                public bool useMips;

                public bool isPow2 => Mathf.IsPowerOfTwo(resolution.x) && Mathf.IsPowerOfTwo(resolution.y);
                public bool isSquare => resolution.x == resolution.y;
            }

            public AtlasSettings atlas;
            public int maxAdditionalLights;        // UniversalRenderPipeline.maxVisibleAdditionalLights;
            public float cubeOctahedralSizeScale;  // Cube octahedral projection size scale.
            public bool useStructuredBuffer;       // RenderingUtils.useStructuredBuffer

            public static Settings GetDefault()
            {
                Settings s;
                s.atlas.resolution = new Vector2Int(1024, 1024);
                s.atlas.format = GraphicsFormat.R8G8B8A8_SRGB;
                s.atlas.useMips = false; // TODO: set to true, make sure they work proper first! Disable them for now...
                s.maxAdditionalLights = UniversalRenderPipeline.maxVisibleAdditionalLights;

                // (Scale * W * Scale * H) / (6 * WH) == (Scale^2 / 6)
                // 1: 1/6 = 16%, 2: 4/6 = 66%, 4: 16/6 == 266% of cube pixels
                // 100% cube pixels == sqrt(6) ~= 2.45f --> 2.5;
                s.cubeOctahedralSizeScale = s.atlas.useMips && s.atlas.isPow2 ? 2.0f : 2.5f;
                s.useStructuredBuffer = RenderingUtils.useStructuredBuffer;
                return s;
            }
        }

        private struct LightCookieMapping
        {
            public ushort visibleLightIndex; // Index into visible light (src)
            public ushort lightBufferIndex;  // Index into light shader data buffer (dst)
        }

        private readonly struct WorkSlice<T>
        {
            private readonly T[] m_Data;
            private readonly int m_Start;
            private readonly int m_Length;

            public WorkSlice(T[] src, int srcLen = -1) : this(src, 0, srcLen) {}

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
        }

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

        private struct ShaderBitArray
        {
            const int k_BitsPerElement = 32;
            const int k_ElementShift = 5;
            const int k_ElementMask = (1 << k_ElementShift) - 1;

            private float[] m_Data;

            public int elemLength => m_Data == null ? 0 : m_Data.Length;
            public int bitCapacity => elemLength * k_BitsPerElement;
            public float[] data => m_Data;

            public void Resize(int bitCount)
            {
                if (bitCapacity > bitCount)
                    return;

                int newElemCount = ((bitCount + (k_BitsPerElement - 1)) / k_BitsPerElement);
                if (newElemCount == m_Data?.Length)
                    return;

                var newData = new float[newElemCount];
                if (m_Data != null)
                {
                    for (int i = 0; i < m_Data.Length; i++)
                        newData[i] = m_Data[i];
                }
                m_Data = newData;
            }

            public void Clear()
            {
                for (int i = 0; i < m_Data.Length; i++)
                    m_Data[i] = 0;
            }

            private void GetElementIndexAndBitOffset(int index, out int elemIndex, out int bitOffset)
            {
                elemIndex = index >> k_ElementShift;
                bitOffset = index & k_ElementMask;
            }

            public bool this[int index]
            {
                get
                {
                    GetElementIndexAndBitOffset(index, out var elemIndex, out var bitOffset);

                    unsafe
                    {
                        fixed(float* floatData = m_Data)
                        {
                            uint* uintElem = (uint*)&floatData[elemIndex];
                            bool val = ((*uintElem) & (1u << bitOffset)) != 0u;
                            return val;
                        }
                    }
                }
                set
                {
                    GetElementIndexAndBitOffset(index, out var elemIndex, out var bitOffset);
                    unsafe
                    {
                        fixed(float* floatData = m_Data)
                        {
                            uint* uintElem = (uint*)&floatData[elemIndex];
                            if (value == true)
                                *uintElem = (*uintElem) | (1u << bitOffset);
                            else
                                *uintElem = (*uintElem) & ~(1u << bitOffset);
                        }
                    }
                }
            }

            public override string ToString()
            {
                unsafe
                {
                    Debug.Assert(bitCapacity < 4096, "Bit string too long! It was truncated!");
                    int len = Math.Min(bitCapacity, 4096);
                    byte* buf = stackalloc byte[len];
                    for (int i = 0; i < len; i++)
                    {
                        buf[i] = (byte)(this[i] ? '1' : '0');
                    }

                    return new string((sbyte*)buf, 0, len, System.Text.Encoding.UTF8);
                }
            }
        }

        /// Must match light data layout.
        private class LightCookieShaderData : IDisposable
        {
            int  m_Size = 0;
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

            public Matrix4x4[] worldToLights  => m_WorldToLightCpuData;
            public ShaderBitArray cookieEnableBits  => m_CookieEnableBitsCpuData;
            public Vector4[] atlasUVRects   => m_AtlasUVRectCpuData;
            public float[] lightTypes     => m_LightTypeCpuData;

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

            public void Apply(CommandBuffer cmd)
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
            }
        }

        Texture2DAtlas m_AdditionalLightsCookieAtlas;
        LightCookieShaderData m_AdditionalLightsCookieShaderData;
        WorkMemory m_WorkMem;

        // map[visibleLightIndex] = ShaderDataIndex
        int[] m_VisibleLightIndexToShaderDataIndex;

        readonly Settings m_Settings;

        // Unity defines directional light UVs over a unit box centered at light.
        // i.e. (0, 1) uv == (-0.5, 0.5) world area instead of the (0,1) world area.
        static readonly Matrix4x4 s_DirLightProj = Matrix4x4.Ortho(-0.5f, 0.5f, -0.5f, 0.5f, -0.5f, 0.5f);

        public LightCookieManager(ref Settings settings)
        {
            m_Settings = settings;
            m_WorkMem = new WorkMemory();
        }

        void InitAdditionalLights(int size)
        {
            if (m_Settings.atlas.useMips && m_Settings.atlas.isPow2)
            {
                // TODO: MipMaps still have sampling artifacts. FIX FIX

                // Supports mip padding for correct filtering at the edges.
                m_AdditionalLightsCookieAtlas = new PowerOfTwoTextureAtlas(
                    m_Settings.atlas.resolution.x,
                    4,
                    m_Settings.atlas.format,
                    FilterMode.Bilinear,
                    "Universal Light Cookie Pow2 Atlas",
                    true);
            }
            else
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
            }


            m_AdditionalLightsCookieShaderData = new LightCookieShaderData(size, m_Settings.useStructuredBuffer);
            const int mainLightCount = 1;
            m_VisibleLightIndexToShaderDataIndex = new int[m_Settings.maxAdditionalLights + mainLightCount];
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

        // by VisibleLight
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
                isAdditionalLightsAvailable = SetupAdditionalLights(cmd, ref lightData);

            // Main and additional lights are merged into one keyword to reduce variants.
            bool isLightCookieEnabled = isMainLightAvailable || isAdditionalLightsAvailable;
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightCookies, isLightCookieEnabled);
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

            return isMainLightCookieEnabled;
        }

        private LightCookieShaderFormat GetLightCookieShaderFormat(GraphicsFormat cookieFormat)
        {
            // RGB 0, A 1, R 2, (G 3), (B 4)
            switch (cookieFormat)
            {
                default:
                    return LightCookieShaderFormat.RGB;
                case (GraphicsFormat)54:    // A8_Unorm TODO: GraphicsFormat does not expose yet.
                case (GraphicsFormat)55:    // A16_Unorm TODO: GraphicsFormat does not expose yet.
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
                    return LightCookieShaderFormat.Red;
            }
        }

        private void GetLightUVScaleOffset(ref UniversalAdditionalLightData additionalLightData, ref Matrix4x4 uvTransform)
        {
            Vector2 uvScale  = Vector2.one / additionalLightData.lightCookieSize;
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
            int validUVRectCount = UpdateAdditionalLightsAtlas(cmd, ref lightData, ref validLights, m_WorkMem.uvRects);

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

            // Warn on dropped lights

            int maxLights = Math.Min(lightData.visibleLights.Length, validLightMappings.Length);
            for (int i = 0; i < maxLights; i++)
            {
                if (i == skipMainLightIndex)
                {
                    lightBufferOffset -= 1;
                    continue;
                }

                Light light = lightData.visibleLights[i].light;

                // Skip lights without a cookie texture
                if (light.cookie == null)
                    continue;

                // Only spot and point lights are supported.
                // Directional lights basically work,
                // but would require a lot of constants for the uv transform parameters
                // and there are very few use cases for multiple global cookies.
                var lightType = lightData.visibleLights[i].lightType;
                if (!(lightType == LightType.Spot ||
                      lightType == LightType.Point))
                {
                    Debug.LogWarning($"Additional {lightType.ToString()} light called '{light.name}' has a light cookie which will not be visible.", light);
                    continue;
                }

                // TODO: check if this is necessary
                // Skip vertex lights, no support
                if (light.renderMode == LightRenderMode.ForceVertex)
                {
                    Debug.LogWarning($"Additional {lightType.ToString()} light called '{light.name}' is a vertex light and its light cookie will not be visible.", light);
                    continue;
                }

                Assertions.Assert.IsTrue(i < ushort.MaxValue);

                LightCookieMapping lp;
                lp.visibleLightIndex = (ushort)i;
                lp.lightBufferIndex  = (ushort)(i + lightBufferOffset);

                validLightMappings[validLightCount++] = lp;
            }

            return validLightCount;
        }

        int UpdateAdditionalLightsAtlas(CommandBuffer cmd, ref LightData lightData, ref WorkSlice<LightCookieMapping> validLightMappings, Vector4[] textureAtlasUVRects)
        {
            bool atlasResetBefore = false;
            int uvRectCount = 0;
            for (int i = 0; i < validLightMappings.length; i++)
            {
                var lcm = validLightMappings[i];
                Light light = lightData.visibleLights[lcm.visibleLightIndex].light;
                Texture cookie = light.cookie;

                Vector4 uvScaleOffset = Vector4.zero;
                if (cookie.dimension == TextureDimension.Cube)
                {
                    Assertions.Assert.IsTrue(light.type == LightType.Point);
                    uvScaleOffset = FetchCube(cmd, cookie);
                }
                else
                {
                    Assertions.Assert.IsTrue(light.type == LightType.Spot || light.type == LightType.Directional, "Light type needs 2D texture!");
                    uvScaleOffset = Fetch2D(cmd, cookie);
                }

                bool isCached = uvScaleOffset != Vector4.zero;
                if (!isCached)
                {
                    if (atlasResetBefore)
                    {
                        // TODO: better messages
                        Debug.LogError("Universal Light Cookie Manager: Atlas full!");
                        return uvRectCount;
                    }

                    // Clear atlas allocs
                    m_AdditionalLightsCookieAtlas.ResetAllocator();

                    // Try to reinsert in priority order
                    i = 0;
                    uvRectCount = 0;
                    atlasResetBefore = true;
                    continue;
                }

                // Adjust atlas UVs for OpenGL
                if (!SystemInfo.graphicsUVStartsAtTop)
                    uvScaleOffset.w = 1.0f - uvScaleOffset.w - uvScaleOffset.y;

                textureAtlasUVRects[uvRectCount++] = uvScaleOffset;
            }

            return uvRectCount;
        }

        Vector4 Fetch2D(CommandBuffer cmd, Texture cookie)
        {
            Assertions.Assert.IsTrue(cookie != null);
            Assertions.Assert.IsTrue(cookie.dimension == TextureDimension.Tex2D);

            Vector4 uvScaleOffset = Vector4.zero;
            m_AdditionalLightsCookieAtlas.UpdateTexture(cmd, cookie, ref uvScaleOffset, cookie);
            if (m_Settings.atlas.useMips)
            {
                // Payload texture is inset
                uvScaleOffset = (m_AdditionalLightsCookieAtlas as PowerOfTwoTextureAtlas).GetPayloadScaleOffset(cookie, uvScaleOffset);
            }
            else
            {
                // Shrink by 0.5px to clamp sampling atlas neighbors (no padding)
                var size = new Vector2(cookie.width, cookie.height);
                var shrinkOffset = Vector2.one * 0.5f / size;
                var shrinkScale = (size - Vector2.one) / size;
                uvScaleOffset.z += uvScaleOffset.x * shrinkOffset.x;
                uvScaleOffset.w += uvScaleOffset.y * shrinkOffset.y;
                uvScaleOffset.x *= shrinkScale.x;
                uvScaleOffset.y *= shrinkScale.y;
            }

            return uvScaleOffset;
        }

        Vector4 FetchCube(CommandBuffer cmd, Texture cookie)
        {
            Assertions.Assert.IsTrue(cookie != null);
            Assertions.Assert.IsTrue(cookie.dimension == TextureDimension.Cube);

            Vector4 uvScaleOffset = Vector4.zero;

            // Check if texture is present
            bool isCached = m_AdditionalLightsCookieAtlas.IsCached(out uvScaleOffset, cookie);
            if (isCached)
            {
                // Update contents if required
                m_AdditionalLightsCookieAtlas.UpdateTexture(cmd, cookie, ref uvScaleOffset);

                return uvScaleOffset;
            }

            // Scale octahedral projection, so that cube -> oct2D pixel count match better.
            int octCookieSize = ComputeOctahedralCookieSize(cookie);

            // Allocate new
            bool isAllocated = m_AdditionalLightsCookieAtlas.AllocateTexture(cmd, ref uvScaleOffset, cookie, octCookieSize, octCookieSize);

            if (isAllocated)
                return uvScaleOffset;

            return Vector4.zero;
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

        void UploadAdditionalLights(CommandBuffer cmd, ref LightData lightData, ref WorkSlice<LightCookieMapping> validLightMappings, ref WorkSlice<Vector4> validUvRects)
        {
            Assertions.Assert.IsTrue(m_AdditionalLightsCookieAtlas != null);
            Assertions.Assert.IsTrue(m_AdditionalLightsCookieShaderData != null);

            cmd.SetGlobalTexture(ShaderProperty.additionalLightsCookieAtlasTexture, m_AdditionalLightsCookieAtlas.AtlasTexture);
            cmd.SetGlobalFloat(ShaderProperty.additionalLightsCookieAtlasTextureFormat, (float)GetLightCookieShaderFormat(m_AdditionalLightsCookieAtlas.AtlasTexture.rt.graphicsFormat));

            m_AdditionalLightsCookieShaderData.Resize(m_Settings.maxAdditionalLights);

            // Resize visible light data map if needed
            if (m_VisibleLightIndexToShaderDataIndex.Length < lightData.visibleLights.Length)
                m_VisibleLightIndexToShaderDataIndex = new int[lightData.visibleLights.Length];

            // Clear the light to data mapping
            int len = Math.Min(m_VisibleLightIndexToShaderDataIndex.Length, lightData.visibleLights.Length);
            for (int i = 0; i < len; i++)
                m_VisibleLightIndexToShaderDataIndex[i] = -1;

            var worldToLights = m_AdditionalLightsCookieShaderData.worldToLights;
            var cookieEnableBits = m_AdditionalLightsCookieShaderData.cookieEnableBits;
            var atlasUVRects = m_AdditionalLightsCookieShaderData.atlasUVRects;
            var lightTypes = m_AdditionalLightsCookieShaderData.lightTypes;

            // Set all rects to Invalid (Vector4.zero).
            Array.Clear(atlasUVRects, 0, atlasUVRects.Length);
            cookieEnableBits.Clear();

            // TODO: technically, we don't need to upload constants again if we knew the lights, atlas (rects) or visible order haven't changed.
            // TODO: but detecting that, might be as time consuming as just doing the work.

            // Fill shader data. Layout should match primary light data for additional lights.
            // Currently it's the same as visible lights, but main light(s) dropped.
            for (int i = 0; i < validUvRects.length; i++)
            {
                int visIndex = validLightMappings[i].visibleLightIndex;
                int bufIndex = validLightMappings[i].lightBufferIndex;

                // Update the mapping
                m_VisibleLightIndexToShaderDataIndex[visIndex] = bufIndex;

                // Update the (cpu) data
                lightTypes[bufIndex] = (int)lightData.visibleLights[visIndex].lightType;
                worldToLights[bufIndex] = lightData.visibleLights[visIndex].localToWorldMatrix.inverse;
                atlasUVRects[bufIndex] = validUvRects[i];
                cookieEnableBits[bufIndex] = true;

                // Spot projection
                if (lightData.visibleLights[visIndex].lightType == LightType.Spot)
                {
                    // VisibleLight.localToWorldMatrix only contains position & rotation.
                    // Multiply projection for spot light.
                    float spotAngle = lightData.visibleLights[visIndex].spotAngle;
                    float spotRange = lightData.visibleLights[visIndex].range;
                    var perp = Matrix4x4.Perspective(spotAngle, 1, 0.001f, spotRange);

                    // Cancel embedded camera view axis flip (https://docs.unity3d.com/2021.1/Documentation/ScriptReference/Matrix4x4.Perspective.html)
                    perp.SetColumn(2, perp.GetColumn(2) * -1);

                    // world -> light local -> light perspective
                    worldToLights[bufIndex] = perp * worldToLights[bufIndex];
                }
            }

            // Apply changes and upload to GPU
            m_AdditionalLightsCookieShaderData.Apply(cmd);
        }
    }
}
