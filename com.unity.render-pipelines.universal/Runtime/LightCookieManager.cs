using System;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.Universal
{
    public class LightCookieManager : IDisposable
    {
        static class ShaderProperty
        {
            public static readonly int _MainLightTexture        = Shader.PropertyToID("_MainLightCookieTexture");
            public static readonly int _MainLightWorldToLight   = Shader.PropertyToID("_MainLightWorldToLight");
            public static readonly int _MainLightCookieUVScale  = Shader.PropertyToID("_MainLightCookieUVScale");
            public static readonly int _MainLightCookieUVOffset = Shader.PropertyToID("_MainLightCookieUVOffset");
            public static readonly int _MainLightCookieFormat   = Shader.PropertyToID("_MainLightCookieFormat");

            public static readonly int _AdditionalLightsCookieAtlasTexture      = Shader.PropertyToID("_AdditionalLightsCookieAtlasTexture");
            public static readonly int _AdditionalLightsCookieAtlasFormat       = Shader.PropertyToID("_AdditionalLightsCookieAtlasFormat");


            public static readonly int _AdditionalLightsCookieAtlasUVRectBuffer = Shader.PropertyToID("_AdditionalLightsCookieAtlasUVRectBuffer");
            public static readonly int _AdditionalLightsWorldToLightBuffer      = Shader.PropertyToID("_AdditionalLightsWorldToLightBuffer");    // TODO: really a light property
            public static readonly int _AdditionalLightsLightTypeBuffer         = Shader.PropertyToID("_AdditionalLightsLightTypeBuffer");        // TODO: really a light property
            public static readonly int _AdditionalLightsCookieUVScaleOffsetBuffer = Shader.PropertyToID("_AdditionalLightsCookieUVScaleOffsetBuffer"); // TODO: only for directional light
            public static readonly int _AdditionalLightsCookieUVWrapModeBuffer    = Shader.PropertyToID("_AdditionalLightsCookieUVWrapModeBuffer"); // TODO: only for directional light


            public static readonly int _AdditionalLightsCookieAtlasUVRects      = Shader.PropertyToID("_AdditionalLightsCookieAtlasUVRects");
            public static readonly int _AdditionalLightsWorldToLights           = Shader.PropertyToID("_AdditionalLightsWorldToLights"); // TODO: really a light property
            public static readonly int _AdditionalLightsLightTypes              = Shader.PropertyToID("_AdditionalLightsLightTypes");    // TODO: really a light property
            public static readonly int _AdditionalLightsCookieUVScaleOffsets = Shader.PropertyToID("_AdditionalLightsCookieUVScaleOffsets"); // TODO: only for directional light
            public static readonly int _AdditionalLightsCookieUVWrapModes    = Shader.PropertyToID("_AdditionalLightsCookieUVWrapModes"); // TODO: only for directional light
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
                // (Scale * WH) / (6 * WH)
                // 1: 1/6 = 16%, 2: 4/6 = 66%, 4: 16/6 == 266% of cube pixels
                // 100% cube pixels == sqrt(6) ~= 2.45f;
                s.cubeOctahedralSizeScale = s.atlas.isPow2 ? 2.0f : 2.45f;
                s.useStructuredBuffer = RenderingUtils.useStructuredBuffer;
                return s;
            }
        }

        private struct LightCookieData : System.IComparable<LightCookieData>
        {
            public ushort visibleLightIndex; // Index into visible light (src) (after sorting)
            public ushort lightBufferIndex; // Index into light shader data buffer (dst)
            public int priority;
            public float score;

            public int CompareTo(LightCookieData other)
            {
                if (priority > other.priority)
                    return -1;
                if (priority == other.priority)
                {
                    if (score > other.score)
                        return -1;
                    if (score == other.score)
                        return 0;
                }

                return 1;
            }
        }

        private class LightCookieShaderData : IDisposable
        {
            int m_Size = 0;
            bool m_useStructuredBuffer;

            Matrix4x4[] m_WorldToLightCpuData;
            Vector4[]   m_AtlasUVRectCpuData;
            float[]     m_LightTypeCpuData;
            Vector4[]   m_UVScaleOffsetCpuData;
            float[]     m_UVWrapModeCpuData;

            ComputeBuffer  m_WorldToLightBuffer;    // TODO: WorldToLight matrices should be general property of lights!!
            ComputeBuffer  m_AtlasUVRectBuffer;
            ComputeBuffer  m_LightTypeBuffer;
            ComputeBuffer  m_UVScaleOffsetBuffer;
            ComputeBuffer  m_UVWrapModeBuffer;

            public Matrix4x4[] worldToLights => m_WorldToLightCpuData;
            public Vector4[]   atlasUVRects  => m_AtlasUVRectCpuData;
            public float[]     lightTypes => m_LightTypeCpuData;
            public Vector4[]   uvScaleOffsets  => m_UVScaleOffsetCpuData;
            public float[]     uvWrapModes  => m_UVWrapModeCpuData;

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
                    m_UVScaleOffsetBuffer?.Dispose();
                    m_UVWrapModeBuffer?.Dispose();
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
                    m_UVScaleOffsetBuffer = new ComputeBuffer(size, Marshal.SizeOf<float4>());
                    m_UVWrapModeBuffer   = new ComputeBuffer(size, Marshal.SizeOf<float>());
                }

                m_WorldToLightCpuData  = new Matrix4x4[size];
                m_AtlasUVRectCpuData   = new Vector4[size];
                m_LightTypeCpuData     = new float[size];
                m_UVScaleOffsetCpuData = new Vector4[size];
                m_UVWrapModeCpuData    = new float[size];

                m_Size = size;
            }

            public void Apply(CommandBuffer cmd)
            {
                if (m_useStructuredBuffer)
                {
                    m_WorldToLightBuffer.SetData(m_WorldToLightCpuData);
                    m_AtlasUVRectBuffer.SetData(m_AtlasUVRectCpuData);
                    m_LightTypeBuffer.SetData(m_LightTypeCpuData);
                    m_UVScaleOffsetBuffer.SetData(m_UVScaleOffsetCpuData);
                    m_UVWrapModeBuffer.SetData(m_UVWrapModeCpuData);

                    cmd.SetGlobalBuffer(ShaderProperty._AdditionalLightsWorldToLightBuffer, m_WorldToLightBuffer);
                    cmd.SetGlobalBuffer(ShaderProperty._AdditionalLightsCookieAtlasUVRectBuffer, m_AtlasUVRectBuffer);
                    cmd.SetGlobalBuffer(ShaderProperty._AdditionalLightsLightTypeBuffer, m_LightTypeBuffer);
                    cmd.SetGlobalBuffer(ShaderProperty._AdditionalLightsCookieUVScaleOffsetBuffer, m_UVScaleOffsetBuffer);
                    cmd.SetGlobalBuffer(ShaderProperty._AdditionalLightsCookieUVWrapModeBuffer, m_UVWrapModeBuffer);
                }
                else
                {
                    cmd.SetGlobalMatrixArray(ShaderProperty._AdditionalLightsWorldToLights, m_WorldToLightCpuData);
                    cmd.SetGlobalVectorArray(ShaderProperty._AdditionalLightsCookieAtlasUVRects, m_AtlasUVRectCpuData);
                    cmd.SetGlobalFloatArray(ShaderProperty._AdditionalLightsLightTypes, m_LightTypeCpuData);
                    cmd.SetGlobalVectorArray(ShaderProperty._AdditionalLightsCookieUVScaleOffsets, m_UVScaleOffsetCpuData);
                    cmd.SetGlobalFloatArray(ShaderProperty._AdditionalLightsCookieUVWrapModes, m_UVWrapModeCpuData);
                }
            }
        }

        Texture2DAtlas        m_AdditionalLightsCookieAtlas;
        LightCookieShaderData m_AdditionalLightsCookieShaderData;
        readonly Settings     m_Settings;

        public LightCookieManager(ref Settings settings)
        {
            m_Settings = settings;
        }

        void InitAdditionalLights(int size)
        {
            if (m_Settings.atlas.useMips && m_Settings.atlas.isPow2)
            {
                // TOOD: MipMaps still have sampling artifacts. FIX fIX

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
        }

        bool isInitialized() => m_AdditionalLightsCookieAtlas != null && m_AdditionalLightsCookieShaderData != null;

        public void Dispose()
        {
            m_AdditionalLightsCookieAtlas?.Release();
            m_AdditionalLightsCookieShaderData?.Dispose();
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
                float cookieFormat     = ((cookieTexture as Texture2D)?.format == TextureFormat.Alpha8) ? 1.0f : 0.0f;

                var additionalLightData = mainLight.GetComponent<UniversalAdditionalLightData>();
                if (additionalLightData != null)
                    GetLightUVScaleOffset(ref additionalLightData, out cookieUVScale, out cookieUVOffset);

                cmd.SetGlobalTexture(ShaderProperty._MainLightTexture,       cookieTexture);
                cmd.SetGlobalMatrix(ShaderProperty._MainLightWorldToLight,   cookieMatrix);
                cmd.SetGlobalVector(ShaderProperty._MainLightCookieUVScale,  cookieUVScale);
                cmd.SetGlobalVector(ShaderProperty._MainLightCookieUVOffset, cookieUVOffset);
                cmd.SetGlobalFloat(ShaderProperty._MainLightCookieFormat,    cookieFormat);

                //DrawDebugFrustum(visibleMainLight.localToWorldMatrix);
            }

            return isMainLightCookieEnabled;
        }

        void GetLightUVScaleOffset(ref UniversalAdditionalLightData additionalLightData, out Vector2 uvScale, out Vector2 uvOffset)
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
            // TODO: better to use growing arrays instead of native arrays, List<T> at interface
            // TODO: how fast is temp alloc???
            var validLights = new NativeArray<LightCookieData>(lightData.additionalLightsCount , Allocator.Temp);
            int validLightCount = PrepareAndValidateAdditionalLights(ref lightData, ref validLights);

            // Early exit if no valid cookie lights
            if (validLightCount <= 0)
            {
                validLights.Dispose();
                return false;
            }

            // TODO: does this even make sense??? Lights are globally sorted by intensity. Kind of the same thing, as our sort is not strict cookie priority.
            // Sort by priority
            unsafe
            {
                CoreUnsafeUtils.QuickSort<LightCookieData>(validLightCount, validLights.GetUnsafePtr());
            }

            // Lazy init GPU resources
            if (validLightCount > 0 && !isInitialized())
                InitAdditionalLights(validLightCount);

            // Update Atlas
            var validSortedLights = validLights.GetSubArray(0, validLightCount);
            var uvRects = new NativeArray<Vector4>(validLightCount , Allocator.Temp);
            int validUVRectCount = UpdateAdditionalLightsAtlas(cmd, ref lightData, ref validSortedLights, ref uvRects);

            // Upload shader data
            var validUvRects = uvRects.GetSubArray(0, validUVRectCount);
            UploadAdditionalLights(cmd, ref lightData, ref validSortedLights, ref validUvRects);

            bool isAdditionalLightsEnabled = validUvRects.Length > 0;

            uvRects.Dispose();
            validLights.Dispose();

            return isAdditionalLightsEnabled;
        }

        int PrepareAndValidateAdditionalLights(ref LightData lightData, ref NativeArray<LightCookieData> validLights)
        {
            int skipMainLightIndex = lightData.mainLightIndex;
            int lightBufferOffset = 0;
            int validLightCount = 0;
            for (int i = 0; i < lightData.visibleLights.Length; i++)
            {
                if (i == skipMainLightIndex)
                {
                    lightBufferOffset = -1;
                    continue;
                }

                Light light = lightData.visibleLights[i].light;

                // Skip lights without a cookie texture
                if (light.cookie == null)
                    continue;

                // Skip vertex lights, no support
                if (light.renderMode == LightRenderMode.ForceVertex)
                    continue;

                //DrawDebugFrustum(lightData.visibleLights[i].localToWorldMatrix);

                Debug.Assert(i < ushort.MaxValue);

                LightCookieData lp;
                lp.visibleLightIndex = (ushort)i;
                lp.lightBufferIndex  = (ushort)(i + lightBufferOffset);
                lp.priority = 0;
                lp.score = 0;

                // Get user priority
                var additionalLightData = light.GetComponent<UniversalAdditionalLightData>();
                if (additionalLightData != null)
                    lp.priority = additionalLightData.priority;

                // TODO: could be computed globally and shared between systems!
                // Compute automatic importance score
                // Factors:
                // 1. Light screen area
                // 2. Light intensity
                // 4. TODO: better criteria?? spot > point?
                // TODO: Is screen rect accurate? If not then just use size
                Rect  lightScreenUVRect = lightData.visibleLights[i].screenRect;
                float lightScreenAreaUV = lightScreenUVRect.width * lightScreenUVRect.height;
                float lightIntensity    = light.intensity;
                lp.score                = lightScreenAreaUV * lightIntensity;

                validLights[validLightCount++] = lp;
            }

            return validLightCount;
        }

        int UpdateAdditionalLightsAtlas(CommandBuffer cmd, ref LightData lightData, ref NativeArray<LightCookieData> sortedLights, ref NativeArray<Vector4> textureAtlasUVRects)
        {
            // Test if a texture is in atlas
            // If yes
            //  --> add UV rect
            // If no
            //    --> add into atlas
            //      If no space
            //          --> clear atlas
            //          --> re-insert in priority order
            //          --> TODO: add partial eviction mechanism??
            //          If no space
            //              --> warn
            //          If space
            //              --> add UV rect
            //      If space
            //          --> add UV rect
            bool atlasResetBefore = false;
            int uvRectCount = 0;
            for (int i = 0; i < sortedLights.Length; i++)
            {
                var lcd = sortedLights[i];
                Light light = lightData.visibleLights[lcd.visibleLightIndex].light;
                Texture cookie = light.cookie;

                // TODO: blit point light into octahedraQuad or 2d slices.
                // TODO: blit format convert into A8 or into sRGB
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
                    // Update data
                    //if (m_CookieAtlas.NeedsUpdate(cookie, false))
                    //    m_CookieAtlas.BlitTexture(cmd, scaleBias, cookie, new Vector4(1, 1, 0, 0), blitMips: false);

                    if (atlasResetBefore)
                    {
                        // TODO: better messages
                        //Debug.LogError("Universal Light Cookie Manager: Atlas full!");
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

                textureAtlasUVRects[uvRectCount++] = new Vector4(uvScaleOffset.z, uvScaleOffset.w, uvScaleOffset.x, uvScaleOffset.y); // Flip ( scale, offset) into a rect i.e. ( offset, scale )
            }

            return uvRectCount;
        }

        Vector4 Fetch2D(CommandBuffer cmd, Texture cookie)
        {
            Debug.Assert(cookie != null);
            Debug.Assert(cookie.dimension == TextureDimension.Tex2D);

            Vector4 uvScaleOffset = Vector4.zero;
            m_AdditionalLightsCookieAtlas.AddTexture(cmd, ref uvScaleOffset, cookie);
            m_AdditionalLightsCookieAtlas.UpdateTexture(cmd, cookie, ref uvScaleOffset, cookie);
            if (m_Settings.atlas.useMips)
                uvScaleOffset = (m_AdditionalLightsCookieAtlas as PowerOfTwoTextureAtlas).GetPaddedScaleOffset(cookie, uvScaleOffset);

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

        public Vector4 FetchCube(CommandBuffer cmd, Texture cookie)
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

        void DrawDebugFrustum(Matrix4x4 m, float near = 1, float far = -1)
        {
            var src = new Vector4[]
            {
                new Vector4(-1, -1, near, 1),
                new Vector4(1, -1, near , 1),
                new Vector4(1, 1, near  , 1),
                new Vector4(-1, 1, near , 1),

                new Vector4(-1, -1, far , 1),
                new Vector4(1, -1, far  , 1),
                new Vector4(1, 1, far   , 1),
                new Vector4(-1, 1, far  , 1),
            };
            var res = new Vector4[8];
            for (int i = 0; i < src.Length; i++)
                res[i] = m * src[i];

            for (int i = 0; i < src.Length; i++)
                res[i] = res[i].w != 0 ? res[i] / res[i].w : res[i];

            Debug.DrawLine(res[0], res[1], Color.black);
            Debug.DrawLine(res[1], res[2], Color.black);
            Debug.DrawLine(res[2], res[3], Color.black);
            Debug.DrawLine(res[3], res[0], Color.black);

            Debug.DrawLine(res[4 + 0], res[4 + 1], Color.white);
            Debug.DrawLine(res[4 + 1], res[4 + 2], Color.white);
            Debug.DrawLine(res[4 + 2], res[4 + 3], Color.white);
            Debug.DrawLine(res[4 + 3], res[4 + 0], Color.white);

            Debug.DrawLine(res[0], res[4 + 0], Color.yellow);
            Debug.DrawLine(res[1], res[4 + 1], Color.yellow);
            Debug.DrawLine(res[2], res[4 + 2], Color.yellow);
            Debug.DrawLine(res[3], res[4 + 3], Color.yellow);

            var o = m * new Vector4(0, 0, 0, 1);
            var x = m * new Vector4(1, 0, 0, 1);
            var y = m * new Vector4(0, 1, 0, 1);
            var z = m * new Vector4(0, 0, 1, 1);
            o = o.w > 0 ? o / o.w : o;
            x = x.w > 0 ? x / x.w : x;
            y = y.w > 0 ? y / y.w : y;
            z = z.w > 0 ? z / z.w : z;
            Debug.DrawLine(o, x, Color.red);
            Debug.DrawLine(o, y, Color.green);
            Debug.DrawLine(o, z, Color.blue);
        }

        void UploadAdditionalLights(CommandBuffer cmd, ref LightData lightData, ref NativeArray<LightCookieData> validSortedLights, ref NativeArray<Vector4> validUvRects)
        {
            Debug.Assert(m_AdditionalLightsCookieAtlas != null);
            Debug.Assert(m_AdditionalLightsCookieShaderData != null);

            float cookieAtlasFormat = (GraphicsFormatUtility.GetTextureFormat(m_AdditionalLightsCookieAtlas.AtlasTexture.rt.graphicsFormat) == TextureFormat.Alpha8) ? 1.0f : 0.0f;
            cmd.SetGlobalTexture(ShaderProperty._AdditionalLightsCookieAtlasTexture, m_AdditionalLightsCookieAtlas.AtlasTexture);
            cmd.SetGlobalFloat(ShaderProperty._AdditionalLightsCookieAtlasFormat, cookieAtlasFormat);

            m_AdditionalLightsCookieShaderData.Resize(m_Settings.maxAdditionalLights);

            var worldToLights = m_AdditionalLightsCookieShaderData.worldToLights;
            var atlasUVRects = m_AdditionalLightsCookieShaderData.atlasUVRects;
            var lightTypes = m_AdditionalLightsCookieShaderData.lightTypes;
            var uvScaleOffsets = m_AdditionalLightsCookieShaderData.uvScaleOffsets;
            var uvWrapModes = m_AdditionalLightsCookieShaderData.uvWrapModes;

            // TODO: clear enable bits instead
            // Set all rects to Invalid (Vector4.zero).
            Array.Clear(atlasUVRects, 0, atlasUVRects.Length);

            // Fill shader data
            for (int i = 0; i < validUvRects.Length; i++)
            {
                int visIndex = validSortedLights[i].visibleLightIndex;
                int bufIndex = validSortedLights[i].lightBufferIndex;

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

                if (lightData.visibleLights[visIndex].lightType == LightType.Directional)
                {
                    Vector2 uvScale = Vector2.one;
                    Vector2 uvOffset = Vector2.zero;

                    // TODO: should get this data somewhere higher up
                    var light = lightData.visibleLights[visIndex].light;
                    var additionalLightData = light.GetComponent<UniversalAdditionalLightData>();
                    if (additionalLightData != null)
                        GetLightUVScaleOffset(ref additionalLightData, out uvScale, out uvOffset);

                    uvScaleOffsets[bufIndex] = new Vector4(uvScale.x, uvScale.y, uvOffset.x, uvOffset.y);
                    uvWrapModes[bufIndex] = (float)light?.cookie.wrapMode;
                }
            }

            // Apply changes and upload to GPU
            m_AdditionalLightsCookieShaderData.Apply(cmd);
        }
    }
}
