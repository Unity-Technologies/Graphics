using System;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.Universal
{
    public class LightCookieManager : IDisposable
    {
        static class ShaderProperty
        {
            public static readonly int _MainLightTexture          = Shader.PropertyToID("_MainLightCookieTexture");
            public static readonly int _MainLightWorldToLight     = Shader.PropertyToID("_MainLightWorldToLight");
            public static readonly int _MainLightCookieUVScale    = Shader.PropertyToID("_MainLightCookieUVScale");
            public static readonly int _MainLightCookieFormat     = Shader.PropertyToID("_MainLightCookieFormat");
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

            public static Settings GetDefault()
            {
                Settings s;
                s.atlas.resolution = new Vector2Int(1024, 1024);
                s.atlas.format     = GraphicsFormat.R8G8B8A8_SRGB; // TODO: optimize
                return s;
            }
        }

        private struct LightPriority : System.IComparable<LightPriority>
        {
            public int visibleLightIndex;
            public int priority;
            public int score;

            public int CompareTo(LightPriority other)
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

        Texture2DAtlas m_AdditionalCookieLightAtlas;
        Settings m_Settings;

        public LightCookieManager(in Settings settings)
        {
            // TODO: correct atlas type?
            m_AdditionalCookieLightAtlas = new Texture2DAtlas(
                settings.atlas.resolution.x,
                settings.atlas.resolution.y,
                settings.atlas.format,
                FilterMode.Bilinear,    // TODO: option?
                settings.atlas.isPow2,    // TODO: necessary?
                "Universal Light Cookie Atlas",
                false); // TODO: support mips, use Pow2Atlas

            m_Settings = settings;
        }

        public void Dispose()
        {
            m_AdditionalCookieLightAtlas.Release();
        }

        public void Setup(ScriptableRenderContext ctx, CommandBuffer cmd, in LightData lightData)
        {
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
            var sortedLights = new NativeArray<LightPriority>(lightData.additionalLightsCount , Allocator.Temp);
            SortVisibleLightsByPriority(lightData, ref sortedLights);

            var textureAtlasUVRects = new NativeArray<Rect>(lightData.additionalLightsCount , Allocator.Temp);
            UpdateAtlas(cmd, lightData, sortedLights, ref textureAtlasUVRects);

            //Bind();

            textureAtlasUVRects.Dispose();
            sortedLights.Dispose();
        }

        void SortVisibleLightsByPriority(in LightData lightData, ref NativeArray<LightPriority> sortedLights)
        {
            var skipIndex = lightData.mainLightIndex;
            int lightIndex = 0;
            for (int i = 0; i < sortedLights.Length; i++)
            {
                // Skip main light
                if (i == skipIndex)
                    continue;

                LightPriority lp;
                lp.visibleLightIndex = i;    // Index into light data after sorting
                lp.priority = 0;
                lp.score = 0;

                // Get user priority
                var additionalLightData = lightData.visibleLights[i].light.GetComponent<UniversalAdditionalLightData>();
                if (additionalLightData != null)
                    lp.priority = additionalLightData.priority;

                // Compute importance score
                // Factors:
                // 1. Light screen area
                // 2. Light intensity
                // 3. Cookies only for pixel lights
                // 4. TODO: better criteria?? spot > point?
                Rect  lightScreenRect = lightData.visibleLights[i].screenRect;
                float lightScreenArea = lightScreenRect.width * lightScreenRect.height;
                float lightIntensity  = lightData.visibleLights[i].light.intensity;
                float pixelLight      = lightData.visibleLights[i].light.renderMode == LightRenderMode.ForceVertex ? 0 : 1;
                lp.score = (int)(lightScreenArea * lightIntensity * pixelLight + 0.5f);

                sortedLights[lightIndex++] = lp;
            }

            unsafe
            {
                CoreUnsafeUtils.QuickSort<LightPriority>(sortedLights.Length, sortedLights.GetUnsafePtr());
            }
        }

        void UpdateAtlas(CommandBuffer cmd, in LightData lightData, in NativeArray<LightPriority> sortedLights, ref NativeArray<Rect> textureAtlasUVRects)
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
        }
    }
}
