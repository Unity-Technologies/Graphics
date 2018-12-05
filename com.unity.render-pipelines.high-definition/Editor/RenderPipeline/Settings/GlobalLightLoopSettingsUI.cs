using UnityEditor.AnimatedValues;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<SerializedGlobalLightLoopSettings>;
    
    static partial class GlobalLightLoopSettingsUI
    {
        enum Expandable
        {
            Cookie = 1 << 0,
            Reflection = 1 << 1,
            Sky = 1 << 2,
            LightLoop = 1 << 3
        }

        readonly static ExpandedState<Expandable, GlobalLightLoopSettings> k_ExpandedState = new ExpandedState<Expandable, GlobalLightLoopSettings>(~(-1), "HDRP");
        
        static GlobalLightLoopSettingsUI()
        {
            Inspector = CED.Group(
                CED.FoldoutGroup(
                    k_CookiesHeaderContent,
                    Expandable.Cookie,
                    k_ExpandedState,
                    Drawer_SectionCookies
                    ),
                CED.FoldoutGroup(
                    k_ReflectionsHeaderContent,
                    Expandable.Reflection,
                    k_ExpandedState,
                    Drawer_SectionReflection
                    ),
                CED.FoldoutGroup(
                    k_SkyHeaderContent,
                    Expandable.Sky,
                    k_ExpandedState,
                    Drawer_SectionSky
                    ),
                CED.FoldoutGroup(
                    k_LightLoopHeaderContent,
                    Expandable.LightLoop,
                    k_ExpandedState,
                    Drawer_LightLoop
                    )
                );
        }
        
        public static readonly CED.IDrawer Inspector;
        
        static string HumanizeWeight(long weightInByte)
        {
            if (weightInByte < 500)
            {
                return weightInByte + " B";
            }
            else if (weightInByte < 500000L)
            {
                float res = weightInByte / 1000f;
                return res.ToString("n2") + " KB";
            }
            else if (weightInByte < 500000000L)
            {
                float res = weightInByte / 1000000f;
                return res.ToString("n2") + " MB";
            }
            else
            {
                float res = weightInByte / 1000000000f;
                return res.ToString("n2") + " GB";
            }
        }

        static void Drawer_SectionCookies(SerializedGlobalLightLoopSettings serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.cookieSize, k_CoockieSizeContent);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.cookieTexArraySize, k_CookieTextureArraySizeContent);
            if (EditorGUI.EndChangeCheck())
            {
                serialized.cookieTexArraySize.intValue = Mathf.Clamp(serialized.cookieTexArraySize.intValue, 1, TextureCache.k_MaxSupported);
            }
            long currentCache = TextureCache2D.GetApproxCacheSizeInByte(serialized.cookieTexArraySize.intValue, serialized.cookieSize.intValue, 1);
            if (currentCache > LightLoop.k_MaxCacheSize)
            {
                int reserved = TextureCache2D.GetMaxCacheSizeForWeightInByte(LightLoop.k_MaxCacheSize, serialized.cookieSize.intValue, 1);
                string message = string.Format(k_CacheErrorFormat, HumanizeWeight(currentCache), reserved);
                EditorGUILayout.HelpBox(message, MessageType.Error);
            }
            else
            {
                string message = string.Format(k_CacheInfoFormat, HumanizeWeight(currentCache));
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }
            EditorGUILayout.PropertyField(serialized.pointCookieSize, k_PointCoockieSizeContent);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.cubeCookieTexArraySize, k_PointCookieTextureArraySizeContent);
            if (EditorGUI.EndChangeCheck())
            {
                serialized.cubeCookieTexArraySize.intValue = Mathf.Clamp(serialized.cubeCookieTexArraySize.intValue, 1, TextureCache.k_MaxSupported);
            }
            currentCache = TextureCacheCubemap.GetApproxCacheSizeInByte(serialized.cubeCookieTexArraySize.intValue, serialized.pointCookieSize.intValue, 1);
            if (currentCache > LightLoop.k_MaxCacheSize)
            {
                int reserved = TextureCacheCubemap.GetMaxCacheSizeForWeightInByte(LightLoop.k_MaxCacheSize, serialized.pointCookieSize.intValue, 1);
                string message = string.Format(k_CacheErrorFormat, HumanizeWeight(currentCache), reserved);
                EditorGUILayout.HelpBox(message, MessageType.Error);
            }
            else
            {
                string message = string.Format(k_CacheInfoFormat, HumanizeWeight(currentCache));
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }
        }

        static void Drawer_SectionReflection(SerializedGlobalLightLoopSettings serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.reflectionCacheCompressed, k_CompressProbeCacheContent);
            EditorGUILayout.PropertyField(serialized.reflectionCubemapSize, k_CubemapSizeContent);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.reflectionProbeCacheSize, k_ProbeCacheSizeContent);
            if (EditorGUI.EndChangeCheck())
            {
                serialized.reflectionProbeCacheSize.intValue = Mathf.Clamp(serialized.reflectionProbeCacheSize.intValue, 1, TextureCache.k_MaxSupported);
            }
            long currentCache = ReflectionProbeCache.GetApproxCacheSizeInByte(serialized.reflectionProbeCacheSize.intValue, serialized.reflectionCubemapSize.intValue, serialized.supportFabricConvolution.boolValue ? 2 : 1);
            if (currentCache > LightLoop.k_MaxCacheSize)
            {
                int reserved = ReflectionProbeCache.GetMaxCacheSizeForWeightInByte(LightLoop.k_MaxCacheSize, serialized.reflectionCubemapSize.intValue, serialized.supportFabricConvolution.boolValue ? 2 : 1);
                string message = string.Format(k_CacheErrorFormat, HumanizeWeight(currentCache), reserved);
                EditorGUILayout.HelpBox(message, MessageType.Error);
            }
            else
            {
                string message = string.Format(k_CacheInfoFormat, HumanizeWeight(currentCache));
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serialized.planarReflectionCacheCompressed, k_CompressPlanarProbeCacheContent);
            EditorGUILayout.PropertyField(serialized.planarReflectionCubemapSize, k_PlanarTextureSizeContent);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.planarReflectionProbeCacheSize, k_PlanarProbeCacheSizeContent);
            if (EditorGUI.EndChangeCheck())
            {
                serialized.planarReflectionProbeCacheSize.intValue = Mathf.Clamp(serialized.planarReflectionProbeCacheSize.intValue, 1, TextureCache.k_MaxSupported);
            }
            currentCache = PlanarReflectionProbeCache.GetApproxCacheSizeInByte(serialized.planarReflectionProbeCacheSize.intValue, serialized.planarReflectionCubemapSize.intValue, 1);
            if (currentCache > LightLoop.k_MaxCacheSize)
            {
                int reserved = PlanarReflectionProbeCache.GetMaxCacheSizeForWeightInByte(LightLoop.k_MaxCacheSize, serialized.planarReflectionCubemapSize.intValue, 1);
                string message = string.Format(k_CacheErrorFormat, HumanizeWeight(currentCache), reserved);
                EditorGUILayout.HelpBox(message, MessageType.Error);
            }
            else
            {
                string message = string.Format(k_CacheInfoFormat, HumanizeWeight(currentCache));
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }

            EditorGUILayout.PropertyField(serialized.supportFabricConvolution, k_SupportFabricBSDFConvolutionContent);
        }

        static void Drawer_SectionSky(SerializedGlobalLightLoopSettings serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.skyReflectionSize, k_SkyReflectionSizeContent);
            EditorGUILayout.PropertyField(serialized.skyLightingOverrideLayerMask, k_SkyLightingOverrideMaskContent);
            if (serialized.skyLightingOverrideLayerMask.intValue == -1)
            {
                EditorGUILayout.HelpBox(k_SkyLightingHelpBoxContent, MessageType.Warning);
            }
        }

        static void Drawer_LightLoop(SerializedGlobalLightLoopSettings serialized, Editor o)
        {
            EditorGUILayout.DelayedIntField(serialized.maxDirectionalLightsOnScreen, k_MaxDirectionalContent);
            EditorGUILayout.DelayedIntField(serialized.maxPunctualLightsOnScreen, k_MaxPonctualContent);
            EditorGUILayout.DelayedIntField(serialized.maxAreaLightsOnScreen, k_MaxAreaContent);
            EditorGUILayout.DelayedIntField(serialized.maxEnvLightsOnScreen, k_MaxEnvContent);
            EditorGUILayout.DelayedIntField(serialized.maxDecalsOnScreen, k_MaxDecalContent);
            
            serialized.maxDirectionalLightsOnScreen.intValue = Mathf.Clamp(serialized.maxDirectionalLightsOnScreen.intValue, 1, LightLoop.k_MaxDirectionalLightsOnScreen);
            serialized.maxPunctualLightsOnScreen.intValue = Mathf.Clamp(serialized.maxPunctualLightsOnScreen.intValue, 1, LightLoop.k_MaxPunctualLightsOnScreen);
            serialized.maxAreaLightsOnScreen.intValue = Mathf.Clamp(serialized.maxAreaLightsOnScreen.intValue, 1, LightLoop.k_MaxAreaLightsOnScreen);
            serialized.maxEnvLightsOnScreen.intValue = Mathf.Clamp(serialized.maxEnvLightsOnScreen.intValue, 1, LightLoop.k_MaxEnvLightsOnScreen);
            serialized.maxDecalsOnScreen.intValue = Mathf.Clamp(serialized.maxDecalsOnScreen.intValue, 1, LightLoop.k_MaxDecalsOnScreen);
        }
    }
}
