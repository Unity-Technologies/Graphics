using System;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace UnityEngine.Rendering.Universal
{
    public class LightCookieManager : IDisposable
    {
        static class ShaderProperty
        {
            public static readonly int _MainLightTexture        = Shader.PropertyToID("_MainLightCookieTexture");
            public static readonly int _MainLightWorldToLight   = Shader.PropertyToID("_MainLightWorldToLight");
            public static readonly int _MainLightCookieUVScale  = Shader.PropertyToID("_MainLightCookieUVScale");
            public static readonly int _MainLightCookieFormat   = Shader.PropertyToID("_MainLightCookieFormat");

            public static readonly int _AdditionalLightCookieAtlasTexture = Shader.PropertyToID("_AdditionalLightCookieAtlasTexture");
            public static readonly int _AdditionalLightWorldToLight       = Shader.PropertyToID("_AdditionalLightWorldToLight");
            public static readonly int _AdditionalLightCookieAtlasUVRect  = Shader.PropertyToID("_AdditionalLightCookieAtlasUVRect");
        }
        public struct Settings
        {
            public struct AtlasSettings
            {
                public Vector2Int resolution;
                public GraphicsFormat format;

                public bool isPow2 => Mathf.IsPowerOfTwo(resolution.x) && Mathf.IsPowerOfTwo(resolution.x);
            }

            public AtlasSettings atlas;
            public bool useStructuredBuffer;

            public static Settings GetDefault()
            {
                Settings s;
                s.atlas.resolution    = new Vector2Int(1024, 1024);
                s.atlas.format        = GraphicsFormat.R8G8B8A8_SRGB; // TODO: optimize
                s.useStructuredBuffer = false;
                return s;
            }
        }

        private struct LightCookieData : System.IComparable<LightCookieData>
        {
            public int visibleLightIndex;
            public int priority;
            public int score;

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
            // TODO: WorldToLight matrices should be general property of lights!!
            ComputeBuffer  m_WorldToLightBuffer;
            ComputeBuffer  m_AtlasUVRectBuffer;

            NativeArray<float4x4> m_WorldToLightArray;
            NativeArray<float4>   m_AtlasUVRectArray;

            private int m_Size = 0;
            bool m_useStructuredBuffer;

            public ComputeBuffer worldToLightBuffer => m_WorldToLightBuffer;
            public ComputeBuffer atlasUVRectBuffer  => m_AtlasUVRectBuffer;
            public NativeArray<float4x4> worldToLightArray => m_WorldToLightArray;
            public NativeArray<float4>   atlasUVRectArray  => m_AtlasUVRectArray;


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
                }
                else
                {
                    m_WorldToLightArray.Dispose();
                    m_AtlasUVRectArray.Dispose();
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
                    m_WorldToLightBuffer = new ComputeBuffer(size, Marshal.SizeOf<float4x4>());
                    m_AtlasUVRectBuffer  = new ComputeBuffer(size, Marshal.SizeOf<float4>());
                }
                else
                {
                    m_WorldToLightArray = new NativeArray<float4x4>(size, Allocator.Persistent);
                    m_AtlasUVRectArray  = new NativeArray<float4>(size, Allocator.Persistent);
                }

                m_Size = size;
            }
        }

        Texture2DAtlas        m_AdditionalLightCookieAtlas;
        LightCookieShaderData m_AdditionalLightCookieShaderData;
        Settings              m_Settings;

        public LightCookieManager(in Settings settings)
        {
            m_Settings = settings;
        }

        public void Dispose()
        {
            m_AdditionalLightCookieAtlas?.Release();
            m_AdditionalLightCookieShaderData?.Dispose();
        }

        public void Setup(ScriptableRenderContext ctx, CommandBuffer cmd, in LightData lightData)
        {
            using var profScope = new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.LightCookies));

            // Main light, 1 directional, bound directly
            bool isMainLightAvailable = lightData.mainLightIndex >= 0;
            if (isMainLightAvailable)
                SetupMainLight(cmd, lightData.visibleLights[lightData.mainLightIndex]);
            else
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightCookie, false);

            // Additional lights, N spot and point lights in atlas
            bool isAdditionalLightsAvailable = lightData.additionalLightsCount > 0;
            if (isAdditionalLightsAvailable)
                SetupAdditionalLights(cmd, lightData);
            else
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightCookies, false);
        }

        void SetupMainLight(CommandBuffer cmd, in VisibleLight visibleMainLight)
        {
            var mainLight                 = visibleMainLight.light;
            var cookieTexture             = mainLight.cookie;
            bool isMainLightCookieEnabled = cookieTexture != null;

            if (isMainLightCookieEnabled)
            {
                Matrix4x4 cookieMatrix = visibleMainLight.localToWorldMatrix.inverse;
                Vector2 cookieUVScale  = Vector2.one;
                float cookieFormat     = ((cookieTexture as Texture2D)?.format == TextureFormat.Alpha8) ? 1.0f : 0.0f;

                // TODO: verify against HDRP if scale should actually be invScale
                var additionalLightData = mainLight.GetComponent<UniversalAdditionalLightData>();
                if (additionalLightData != null)
                    cookieUVScale = additionalLightData.lightCookieSize;

                cmd.SetGlobalTexture(ShaderProperty._MainLightTexture,       cookieTexture);
                cmd.SetGlobalMatrix(ShaderProperty._MainLightWorldToLight,  cookieMatrix);
                cmd.SetGlobalVector(ShaderProperty._MainLightCookieUVScale, cookieUVScale);
                cmd.SetGlobalFloat(ShaderProperty._MainLightCookieFormat,  cookieFormat);
            }

            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightCookie, isMainLightCookieEnabled);
        }

        void SetupAdditionalLights(CommandBuffer cmd, in LightData lightData)
        {
            // TODO: how fast is temp alloc???
            var sortedLights = new NativeArray<LightCookieData>(lightData.additionalLightsCount , Allocator.Temp);
            int validLightCount = PrepareSortedAdditionalLights(lightData, ref sortedLights);

            // Lazy init GPU resources
            if (validLightCount > 0 && m_AdditionalLightCookieAtlas == null)
                InitAdditionalLights(validLightCount);

            var validSortedLights = sortedLights.GetSubArray(0, validLightCount);
            var uvRects = new NativeArray<Rect>(validLightCount , Allocator.Temp);
            int validUVRectCount = UpdateAdditionalLightAtlas(cmd, lightData, validSortedLights, ref uvRects);

            var validUvRects = uvRects.GetSubArray(0, validUVRectCount);
            BindAdditionalLights(cmd, lightData, validUvRects);

            bool isAdditionalLightsEnabled = validUvRects.Length > 0;
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightCookies, isAdditionalLightsEnabled);


            uvRects.Dispose();
            sortedLights.Dispose();
        }

        void InitAdditionalLights(int size)
        {
            // TODO: correct atlas type?
            m_AdditionalLightCookieAtlas = new Texture2DAtlas(
                m_Settings.atlas.resolution.x,
                m_Settings.atlas.resolution.y,
                m_Settings.atlas.format,
                FilterMode.Bilinear,    // TODO: option?
                m_Settings.atlas.isPow2,    // TODO: necessary?
                "Universal Light Cookie Atlas",
                false); // TODO: support mips, use Pow2Atlas
        }

        int PrepareSortedAdditionalLights(in LightData lightData, ref NativeArray<LightCookieData> sortedLights)
        {
            int skipIndex = lightData.mainLightIndex;
            int validLightCount = 0;
            for (int i = 0; i < sortedLights.Length; i++)
            {
                // Skip main light
                if (i == skipIndex)
                    continue;

                Light light = lightData.visibleLights[i].light;

                // Skip lights without a cookie texture
                if (light.cookie == null)
                    continue;

                // TODO: support vertex lights?
                if (light.renderMode == LightRenderMode.ForceVertex)
                    continue;

                LightCookieData lp;
                lp.visibleLightIndex = i;    // Index into light data after sorting
                lp.priority = 0;
                lp.score = 0;

                // Get user priority
                var additionalLightData = lightData.visibleLights[i].light.GetComponent<UniversalAdditionalLightData>();
                if (additionalLightData != null)
                    lp.priority = additionalLightData.priority;

                // TODO: could be computed globally and shared between systems!
                // Compute automatic importance score
                // Factors:
                // 1. Light screen area
                // 2. Light intensity
                // 4. TODO: better criteria?? spot > point?
                Rect  lightScreenRect = lightData.visibleLights[i].screenRect;
                float lightScreenArea = lightScreenRect.width * lightScreenRect.height;
                float lightIntensity  = Mathf.Min(light.intensity * 10.0f, 0.1f);
                lp.score = (int)(lightScreenArea * lightIntensity + 0.5f);

                sortedLights[validLightCount++] = lp;
            }

            unsafe
            {
                CoreUnsafeUtils.QuickSort<LightCookieData>(validLightCount, sortedLights.GetUnsafePtr());
            }

            return validLightCount;
        }

        int UpdateAdditionalLightAtlas(CommandBuffer cmd, in LightData lightData, in NativeArray<LightCookieData> sortedLights, ref NativeArray<Rect> textureAtlasUVRects)
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
                var l = sortedLights[i];
                Light light = lightData.visibleLights[l.visibleLightIndex].light;

                Vector4 uvScaleOffset = Vector4.zero;
                bool isCached = m_AdditionalLightCookieAtlas.AddTexture(cmd, ref uvScaleOffset, light.cookie);
                if (!isCached)
                {
                    if (atlasResetBefore)
                    {
                        // TODO: better messages
                        Debug.LogError("Universal Light Cookie Manager: Atlas full!");
                        return uvRectCount;
                    }

                    // Clear atlas allocs
                    m_AdditionalLightCookieAtlas.ResetAllocator();

                    // Try to reinsert in priority order
                    i = 0;
                    uvRectCount = 0;
                    atlasResetBefore = true;
                    continue;
                }

                textureAtlasUVRects[uvRectCount++] = new Rect(uvScaleOffset.z, uvScaleOffset.w, uvScaleOffset.x, uvScaleOffset.y);
            }

            return uvRectCount;
        }

        void BindAdditionalLights(CommandBuffer cmd, in LightData lightData, in NativeArray<Rect> validUvRects)
        {
            cmd.SetGlobalTexture(ShaderProperty._AdditionalLightCookieAtlasTexture, m_AdditionalLightCookieAtlas?.AtlasTexture);
            m_AdditionalLightCookieShaderData?.Resize(validUvRects.Length);

            /*if (m_Settings.useStructuredBuffer)
            {
                {
                    var buf = m_AdditionalLightCookieShaderData.worldToLightBuffer;
                    buf.SetData(m_AdditionalLightCookieShaderData.worldToLightArray);
                    cmd.SetGlobalBuffer(m_AdditionalLightCookieShaderData_SSBO, buf);
                }

                {
                    var buf = m_AdditionalLightCookieShaderData.atlasUVRectBuffer;
                    buf.SetData(m_AdditionalLightCookieShaderData.atlasUVRectArray);
                    cmd.SetGlobalBuffer(m_AdditionalLightCookieShaderData_SSBO, buf);
                }
            }
            else
            {
                cmd.SetGlobalMatrixArray(ShaderProperty._AdditionalLightWorldToLight, m_AdditionalLightCookieShaderData.worldToLightArray);
                cmd.SetGlobalVectorArray(ShaderProperty._AdditionalLightCookieAtlasUVRect, m_AdditionalLightCookieShaderData.atlasUVRectArray);

            }*/
        }
    }
}
