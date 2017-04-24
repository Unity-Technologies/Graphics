using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class GlobalDebugSettings
    {
        public float debugOverlayRatio = 0.33f;
        public bool displayMaterialDebug = false;
        public bool displayRenderingDebug = false;
        public bool displayLightingDebug = false;

        public MaterialDebugSettings materialDebugSettings = new MaterialDebugSettings();
        public LightingDebugSettings lightingDebugSettings = new LightingDebugSettings();
        public RenderingDebugSettings renderingDebugSettings = new RenderingDebugSettings();

        public void RegisterDebug()
        {
            DebugMenuManager.instance.AddDebugItem<LightingDebugMenu, bool>("Enable Shadows", () => lightingDebugSettings.enableShadows, (value) => lightingDebugSettings.enableShadows = (bool)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugMenu, int>("Shadow Map Index", () => lightingDebugSettings.shadowMapIndex, (value) => lightingDebugSettings.shadowMapIndex = (uint)value);
            DebugMenuManager.instance.AddDebugItem<bool>("Lighting", "Display Sky Reflection", () => lightingDebugSettings.displaySkyReflection, (value) => lightingDebugSettings.displaySkyReflection = (bool)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugMenu, float>("Sky Reflection Mipmap", () => lightingDebugSettings.skyReflectionMipmap, (value) => lightingDebugSettings.skyReflectionMipmap = (float)value, new DebugItemDrawFloatMinMax(0.0f, 1.0f));

            DebugMenuManager.instance.AddDebugItem<bool>("Rendering", "Display Opaque",() => renderingDebugSettings.displayOpaqueObjects, (value) => renderingDebugSettings.displayOpaqueObjects = (bool)value);
            DebugMenuManager.instance.AddDebugItem<bool>("Rendering", "Display Transparency",() => renderingDebugSettings.displayTransparentObjects, (value) => renderingDebugSettings.displayTransparentObjects = (bool)value);
            DebugMenuManager.instance.AddDebugItem<bool>("Rendering", "Enable Distortion",() => renderingDebugSettings.enableDistortion, (value) => renderingDebugSettings.enableDistortion = (bool)value);
            DebugMenuManager.instance.AddDebugItem<bool>("Rendering", "Enable Subsurface Scattering",() => renderingDebugSettings.enableSSS, (value) => renderingDebugSettings.enableSSS = (bool)value);
        }

        public void OnValidate()
        {
            lightingDebugSettings.OnValidate();
        }
    }


    [Serializable]
    public class MaterialDebugSettings
    {
        public int debugViewMaterial = 0;
    }

    [Serializable]
    public class RenderingDebugSettings
    {
        public bool displayOpaqueObjects = true;
        public bool displayTransparentObjects = true;
        public bool enableDistortion = true;
        public bool enableSSS = true;
    }

    public enum ShadowMapDebugMode
    {
        None,
        VisualizeAtlas,
        VisualizeShadowMap
    }

    [GenerateHLSL]
    public enum LightingDebugMode
    {
        None,
        DiffuseLighting,
        SpecularLighting,
        VisualizeCascade
    }

    [Serializable]
    public class LightingDebugSettings
    {
        public bool                 enableShadows = true;
        public ShadowMapDebugMode   shadowDebugMode = ShadowMapDebugMode.None;
        public uint                 shadowMapIndex = 0;

        public LightingDebugMode    lightingDebugMode = LightingDebugMode.None;
        public bool                 overrideSmoothness = false;
        public float                overrideSmoothnessValue = 0.5f;
        public Color                debugLightingAlbedo = new Color(0.5f, 0.5f, 0.5f);

        public bool                 displaySkyReflection = false;
        public float                skyReflectionMipmap = 0.0f;

        public void OnValidate()
        {
            overrideSmoothnessValue = Mathf.Clamp(overrideSmoothnessValue, 0.0f, 1.0f);
            skyReflectionMipmap = Mathf.Clamp(skyReflectionMipmap, 0.0f, 1.0f);
        }
    }
}
