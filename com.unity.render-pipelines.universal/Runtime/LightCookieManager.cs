using System;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.Universal
{
    internal class LightCookieManager : IDisposable
    {
        static class ShaderProperty
        {
            public static readonly int _MainLightTexture        = Shader.PropertyToID("_MainLightCookieTexture");
            public static readonly int _MainLightWorldToLight   = Shader.PropertyToID("_MainLightWorldToLight");
            public static readonly int _MainLightCookieUVScale  = Shader.PropertyToID("_MainLightCookieUVScale");
            public static readonly int _MainLightCookieUVOffset = Shader.PropertyToID("_MainLightCookieUVOffset");
            public static readonly int _MainLightCookieFormat   = Shader.PropertyToID("_MainLightCookieFormat");

            public static readonly int _AdditionalLightsCookieAtlasTexture = Shader.PropertyToID("_AdditionalLightsCookieAtlasTexture");
            public static readonly int _AdditionalLightsCookieAtlasFormat  = Shader.PropertyToID("_AdditionalLightsCookieAtlasFormat");

            public static readonly int _AdditionalLightsCookieAtlasUVRectBuffer   = Shader.PropertyToID("_AdditionalLightsCookieAtlasUVRectBuffer");
            public static readonly int _AdditionalLightsWorldToLightBuffer        = Shader.PropertyToID("_AdditionalLightsWorldToLightBuffer");    // TODO: really a light property
            public static readonly int _AdditionalLightsLightTypeBuffer           = Shader.PropertyToID("_AdditionalLightsLightTypeBuffer");        // TODO: really a light property

            public static readonly int _AdditionalLightsCookieAtlasUVRects   = Shader.PropertyToID("_AdditionalLightsCookieAtlasUVRects");
            public static readonly int _AdditionalLightsWorldToLights        = Shader.PropertyToID("_AdditionalLightsWorldToLights"); // TODO: really a light property
            public static readonly int _AdditionalLightsLightTypes           = Shader.PropertyToID("_AdditionalLightsLightTypes");    // TODO: really a light property
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
            public int   maxAdditionalLights;        // UniversalRenderPipeline.maxVisibleAdditionalLights;
            public float cubeOctahedralSizeScale;    // Cube octahedral projection size scale.
            public bool  useStructuredBuffer;        // RenderingUtils.useStructuredBuffer

            public static Settings GetDefault()
            {
                Settings s;
                s.atlas.resolution    = new Vector2Int(1024, 1024);
                s.atlas.format        = GraphicsFormat.R8G8B8A8_SRGB; // TODO: optimize
                s.atlas.useMips       = false; // TODO: set to true, make sure they work proper first! Disable them for now...
                s.maxAdditionalLights = UniversalRenderPipeline.maxVisibleAdditionalLights;
                // (Scale * W * Scale * H) / (6 * WH) == (Scale^2 / 6)
                // 1: 1/6 = 16%, 2: 4/6 = 66%, 4: 16/6 == 266% of cube pixels
                // 100% cube pixels == sqrt(6) ~= 2.45f --> 2.5;
                s.cubeOctahedralSizeScale = s.atlas.useMips && s.atlas.isPow2 ? 2.0f : 2.5f;
                s.useStructuredBuffer     = RenderingUtils.useStructuredBuffer;
                return s;
            }
        }

        private struct LightCookieMapping
        {
            public ushort visibleLightIndex; // Index into visible light (src)
            public ushort lightBufferIndex;  // Index into light shader data buffer (dst)
        }

        private struct WorkSlice<T>
        {
            private T[] m_Data;
            private int m_Start;
            private int m_Length;

            public WorkSlice(T[] src, int srcLen = -1)
            {
                m_Data = src;
                m_Start = 0;
                m_Length = (srcLen < 0) ? src.Length : Math.Min(srcLen, src.Length);
            }

            public WorkSlice(T[] src, int srcStart, int srcLen = -1)
            {
                m_Data = src;
                m_Start = srcStart;
                m_Length = (srcLen < 0) ? src.Length : Math.Min(srcLen, src.Length);
                Debug.Assert(m_Start + m_Length <= capacity, "Slice out of bounds!");
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
                if (size < lightMappings?.Length)
                    return;

                // Avoid allocs on every tiny size change.
                size = Math.Max((int)(size * 1.25), 16);

                lightMappings = new LightCookieMapping[size];
                uvRects = new Vector4[size];
            }
        }

        private class LightCookieShaderData : IDisposable
        {
            int  m_Size = 0;
            bool m_useStructuredBuffer;

            // Shader data CPU arrays, used to upload the data to GPU
            Matrix4x4[] m_WorldToLightCpuData;
            Vector4[]   m_AtlasUVRectCpuData;
            float[]     m_LightTypeCpuData;

            // Compute buffer counterparts for the CPU data
            ComputeBuffer  m_WorldToLightBuffer;    // TODO: WorldToLight matrices should be general property of lights!!
            ComputeBuffer  m_AtlasUVRectBuffer;
            ComputeBuffer  m_LightTypeBuffer;

            public Matrix4x4[] worldToLights  => m_WorldToLightCpuData;
            public Vector4[]   atlasUVRects   => m_AtlasUVRectCpuData;
            public float[]     lightTypes     => m_LightTypeCpuData;

            public LightCookieShaderData(int size, bool useStructuredBuffer)
            {
                m_useStructuredBuffer = useStructuredBuffer;
                Resize(size);
            }

            public void Dispose()
            {
                if (m_useStructuredBuffer)
                {
                    m_WorldToLightBuffer?.Dispose();
                    m_AtlasUVRectBuffer?.Dispose();
                    m_LightTypeBuffer?.Dispose();
                }
            }

            public void Resize(int size)
            {
                if (size < m_Size)
                    return;

                if (m_Size > 0)
                    Dispose();

                if (m_useStructuredBuffer)
                {
                    m_WorldToLightBuffer = new ComputeBuffer(size, Marshal.SizeOf<Matrix4x4>());
                    m_AtlasUVRectBuffer  = new ComputeBuffer(size, Marshal.SizeOf<Vector4>());
                    m_LightTypeBuffer    = new ComputeBuffer(size, Marshal.SizeOf<float>());
                }

                m_WorldToLightCpuData  = new Matrix4x4[size];
                m_AtlasUVRectCpuData   = new Vector4[size];
                m_LightTypeCpuData     = new float[size];

                m_Size = size;
            }

            public void Apply(CommandBuffer cmd)
            {
                if (m_useStructuredBuffer)
                {
                    m_WorldToLightBuffer.SetData(m_WorldToLightCpuData);
                    m_AtlasUVRectBuffer.SetData(m_AtlasUVRectCpuData);
                    m_LightTypeBuffer.SetData(m_LightTypeCpuData);

                    cmd.SetGlobalBuffer(ShaderProperty._AdditionalLightsWorldToLightBuffer, m_WorldToLightBuffer);
                    cmd.SetGlobalBuffer(ShaderProperty._AdditionalLightsCookieAtlasUVRectBuffer, m_AtlasUVRectBuffer);
                    cmd.SetGlobalBuffer(ShaderProperty._AdditionalLightsLightTypeBuffer, m_LightTypeBuffer);
                }
                else
                {
                    cmd.SetGlobalMatrixArray(ShaderProperty._AdditionalLightsWorldToLights, m_WorldToLightCpuData);
                    cmd.SetGlobalVectorArray(ShaderProperty._AdditionalLightsCookieAtlasUVRects, m_AtlasUVRectCpuData);
                    cmd.SetGlobalFloatArray(ShaderProperty._AdditionalLightsLightTypes, m_LightTypeCpuData);
                }
            }
        }

        Texture2DAtlas        m_AdditionalLightsCookieAtlas;
        LightCookieShaderData m_AdditionalLightsCookieShaderData;
        WorkMemory            m_WorkMem;

        // map[visibleLightIndex] = ShaderDataIndex
        int[] m_VisibleLightIndexToShaderDataIndex;

        readonly Settings     m_Settings;

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

            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightCookie, isMainLightAvailable);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightCookies, isAdditionalLightsAvailable);
        }

        bool SetupMainLight(CommandBuffer cmd, ref VisibleLight visibleMainLight)
        {
            var mainLight                 = visibleMainLight.light;
            var cookieTexture             = mainLight.cookie;
            bool isMainLightCookieEnabled = cookieTexture != null;

            if (isMainLightCookieEnabled)
            {
                Matrix4x4 cookieMatrix = visibleMainLight.localToWorldMatrix.inverse;
                Vector2 cookieUVScale  = Vector2.one;
                Vector2 cookieUVOffset = Vector2.zero;
                float cookieFormat     = (float)GetLightCookieShaderFormat(cookieTexture.graphicsFormat);

                var additionalLightData = mainLight.GetComponent<UniversalAdditionalLightData>();
                if (additionalLightData != null)
                    GetLightUVScaleOffset(ref additionalLightData, out cookieUVScale, out cookieUVOffset);

                cmd.SetGlobalTexture(ShaderProperty._MainLightTexture,       cookieTexture);
                cmd.SetGlobalMatrix(ShaderProperty._MainLightWorldToLight,   cookieMatrix);
                cmd.SetGlobalVector(ShaderProperty._MainLightCookieUVScale,  cookieUVScale);
                cmd.SetGlobalVector(ShaderProperty._MainLightCookieUVOffset, cookieUVOffset);
                cmd.SetGlobalFloat(ShaderProperty._MainLightCookieFormat,    cookieFormat);
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

        private void GetLightUVScaleOffset(ref UniversalAdditionalLightData additionalLightData, out Vector2 uvScale, out Vector2 uvOffset)
        {
            uvScale  = Vector2.one / additionalLightData.lightCookieSize;
            uvOffset = additionalLightData.lightCookieOffset;

            if (Mathf.Abs(uvScale.x) < half.MinValue)
                uvScale.x = Mathf.Sign(uvScale.x) * half.MinValue;
            if (Mathf.Abs(uvScale.y) < half.MinValue)
                uvScale.y = Mathf.Sign(uvScale.y) * half.MinValue;
        }

        bool SetupAdditionalLights(CommandBuffer cmd, ref LightData lightData)
        {
            m_WorkMem.Resize(lightData.additionalLightsCount);
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

        int FilterAndValidateAdditionalLights(ref LightData lightData, LightCookieMapping[] validLights)
        {
            int skipMainLightIndex = lightData.mainLightIndex;
            int lightBufferOffset = 0;
            int validLightCount = 0;
            for (int i = 0; i < lightData.visibleLights.Length; i++)
            {
                if (i == skipMainLightIndex)
                {
                    lightBufferOffset -= 1;
                    continue;
                }

                // TODO: visibleLights[].light is a search, should cache these somewhere
                Light light = lightData.visibleLights[i].light;

                // Skip lights without a cookie texture
                if (light.cookie == null)
                    continue;

                // Only spot and point lights are supported.
                // Directional lights basically work,
                // but would require a lot of constants for the uv transform parameters
                // and there are very few use cases multiple global cookies.
                var lightType = lightData.visibleLights[i].lightType;
                if (!(lightType == LightType.Spot ||
                      lightType == LightType.Point))
                {
                    Debug.LogWarning($"Additional {lightType.ToString()} light called '{light.name}' has a light cookie which will not be visible.");
                    continue;
                }

                // Skip vertex lights, no support
                if (light.renderMode == LightRenderMode.ForceVertex)
                {
                    Debug.LogWarning($"Additional {lightType.ToString()} light called '{light.name}' is a vertex light and its light cookie will not be visible.");
                    continue;
                }

                Debug.Assert(i < ushort.MaxValue);

                LightCookieMapping lp;
                lp.visibleLightIndex = (ushort)i;
                lp.lightBufferIndex  = (ushort)(i + lightBufferOffset);

                validLights[validLightCount++] = lp;
            }

            return validLightCount;
        }

        int UpdateAdditionalLightsAtlas(CommandBuffer cmd, ref LightData lightData, ref WorkSlice<LightCookieMapping> sortedLights, Vector4[] textureAtlasUVRects)
        {
            // Test if a texture is in atlas
            // If yes
            //  --> add UV rect
            // If no
            //    --> add into atlas
            // If no space
            //     --> clear atlas
            //     --> re-insert in priority order
            //     --> TODO: add partial eviction mechanism??
            //     If space
            //         --> add UV rect
            //     If no space
            //         --> warn
            //         --> exit
            //         --> TODO: remaining textures might fit into the atlas, add support

            bool atlasResetBefore = false;
            int uvRectCount = 0;
            for (int i = 0; i < sortedLights.length; i++)
            {
                var lcd = sortedLights[i];
                Light light = lightData.visibleLights[lcd.visibleLightIndex].light;
                Texture cookie = light.cookie;

                Vector4 uvScaleOffset = Vector4.zero;
                if (cookie.dimension == TextureDimension.Cube)
                {
                    Debug.Assert(light.type == LightType.Point);
                    uvScaleOffset = FetchCube(cmd, cookie);
                }
                else
                {
                    Debug.Assert(light.type == LightType.Spot || light.type == LightType.Directional, "Light type needs 2D texture!");
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

                textureAtlasUVRects[uvRectCount++] = new Vector4(uvScaleOffset.z, uvScaleOffset.w, uvScaleOffset.x, uvScaleOffset.y); // Flip ( scale, offset) into a rect i.e. ( offset, scale )
            }

            return uvRectCount;
        }

        Vector4 Fetch2D(CommandBuffer cmd, Texture cookie)
        {
            Debug.Assert(cookie != null);
            Debug.Assert(cookie.dimension == TextureDimension.Tex2D);

            Vector4 uvScaleOffset = Vector4.zero;
            m_AdditionalLightsCookieAtlas.UpdateTexture(cmd, cookie, ref uvScaleOffset, cookie);
            if (m_Settings.atlas.useMips)
                uvScaleOffset = (m_AdditionalLightsCookieAtlas as PowerOfTwoTextureAtlas).GetPaddedScaleOffset(cookie, uvScaleOffset);
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
            Debug.Assert(cookie != null);
            Debug.Assert(cookie.dimension == TextureDimension.Cube);

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

        void UploadAdditionalLights(CommandBuffer cmd, ref LightData lightData, ref WorkSlice<LightCookieMapping> validSortedLights, ref WorkSlice<Vector4> validUvRects)
        {
            Debug.Assert(m_AdditionalLightsCookieAtlas != null);
            Debug.Assert(m_AdditionalLightsCookieShaderData != null);

            cmd.SetGlobalTexture(ShaderProperty._AdditionalLightsCookieAtlasTexture, m_AdditionalLightsCookieAtlas.AtlasTexture);
            cmd.SetGlobalFloat(ShaderProperty._AdditionalLightsCookieAtlasFormat, (float)GetLightCookieShaderFormat(m_AdditionalLightsCookieAtlas.AtlasTexture.rt.graphicsFormat));

            m_AdditionalLightsCookieShaderData.Resize(m_Settings.maxAdditionalLights);

            // Resize visible light data map if needed
            if (m_VisibleLightIndexToShaderDataIndex.Length < lightData.visibleLights.Length)
                m_VisibleLightIndexToShaderDataIndex = new int[lightData.visibleLights.Length];

            // Clear the light to data mapping
            int len = Math.Min(m_VisibleLightIndexToShaderDataIndex.Length, lightData.visibleLights.Length);
            for (int i = 0; i < len; i++)
                m_VisibleLightIndexToShaderDataIndex[i] = -1;

            var worldToLights = m_AdditionalLightsCookieShaderData.worldToLights;
            var atlasUVRects = m_AdditionalLightsCookieShaderData.atlasUVRects;
            var lightTypes = m_AdditionalLightsCookieShaderData.lightTypes;

            // TODO: clear enable bits instead
            // Set all rects to Invalid (Vector4.zero).
            Array.Clear(atlasUVRects, 0, atlasUVRects.Length);

            // TODO: for deferred, process and bind only the necessary

            // Fill shader data. Layout should match primary light data for additional lights.
            // Currently it's the same as visible lights, but main light(s) dropped.
            for (int i = 0; i < validUvRects.length; i++)
            {
                int visIndex = validSortedLights[i].visibleLightIndex;
                int bufIndex = validSortedLights[i].lightBufferIndex;

                // Update the mapping
                m_VisibleLightIndexToShaderDataIndex[visIndex] = bufIndex;

                // Update the (cpu) data
                lightTypes[bufIndex]    = (int)lightData.visibleLights[visIndex].lightType;
                worldToLights[bufIndex] = lightData.visibleLights[visIndex].localToWorldMatrix.inverse;
                atlasUVRects[bufIndex]  = validUvRects[i];

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
