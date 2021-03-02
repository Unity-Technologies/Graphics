using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public class LightCookieManager
    {
        static class ShaderProperty
        {
            public static readonly int _MainLightTexture          = Shader.PropertyToID("_MainLightCookieTexture");
            public static readonly int _MainLightWorldToLight     = Shader.PropertyToID("_MainLightWorldToLight");
            public static readonly int _MainLightCookieUVScale    = Shader.PropertyToID("_MainLightCookieUVScale");
            public static readonly int _MainLightCookieFormat     = Shader.PropertyToID("_MainLightCookieFormat");
        }
        public struct LightCookieSettings
        {
            public struct AtlasSettings
            {
                public Vector2Int resolution;
                public GraphicsFormat format;

                public bool isPow2 => Mathf.IsPowerOfTwo(resolution.x) && Mathf.IsPowerOfTwo(resolution.x);
            }

            public AtlasSettings atlas;

            public static LightCookieSettings GetDefault()
            {
                LightCookieSettings s;
                s.atlas.resolution = new Vector2Int(1024, 1024);
                s.atlas.format     = GraphicsFormat.R8G8B8A8_SRGB; // TODO: optimize
                return s;
            }
        }

        Texture2DAtlas m_AdditionalCookieLightAtlas;
        LightCookieSettings m_Settings;

        public LightCookieManager(in LightCookieSettings settings)
        {
            // TODO: correct atlas type?
            m_AdditionalCookieLightAtlas = new Texture2DAtlas(
                settings.atlas.resolution.x,
                settings.atlas.resolution.y,
                settings.atlas.format,
                FilterMode.Bilinear,    // TODO: option?
                settings.atlas.isPow2,    // TODO: necessary?
                "Universal Light Cookie Atlas",
                false); // TODO: support mips

            m_Settings = settings;
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
            //SortByPriority();
            //UpdateAtlas();
            //Bind();
        }
    }
}
