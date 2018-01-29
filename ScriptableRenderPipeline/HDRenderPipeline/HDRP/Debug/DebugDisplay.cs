using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public enum FullScreenDebugMode
    {
        None,

        // Lighting
        MinLightingFullScreenDebug,
        SSAO,
        DeferredShadows,
        PreRefractionColorPyramid,
        DepthPyramid,
        FinalColorPyramid,
        MaxLightingFullScreenDebug,

        // Rendering
        MinRenderingFullScreenDebug,
        MotionVectors,
        NanTracker,
        MaxRenderingFullScreenDebug
    }

    public class DebugDisplaySettings
    {
        public static string kEnableShadowDebug = "Enable Shadows";
        public static string kShadowDebugMode = "Shadow Debug Mode";
        public static string kShadowSelectionDebug = "Use Selection";
        public static string kShadowMapIndexDebug = "Shadow Map Index";
        public static string kShadowAtlasIndexDebug = "Shadow Atlas Index";
        public static string kShadowMinValueDebug = "Shadow Range Min Value";
        public static string kShadowMaxValueDebug = "Shadow Range Max Value";
        public static string kLightingDebugMode = "Lighting Debug Mode";
        public static string kOverrideSmoothnessDebug = "Override Smoothness";
        public static string kOverrideSmoothnessValueDebug = "Override Smoothness Value"; 
        public static string kDebugEnvironmentProxyDepthScale = "Debug Environment Proxy Depth Scale";
        public static string kDebugLightingAlbedo = "Debug Lighting Albedo";
        public static string kFullScreenDebugMode = "Fullscreen Debug Mode";
        public static string kFullScreenDebugMip = "Fullscreen Debug Mip";
        public static string kDisplaySkyReflectionDebug = "Display Sky Reflection";
        public static string kSkyReflectionMipmapDebug = "Sky Reflection Mipmap";
        public static string kTileClusterCategoryDebug = "Tile/Cluster Debug By Category";
        public static string kTileClusterDebug = "Tile/Cluster Debug";
        public static string kMipMapDebugMode = "Mip Map Debug Mode";


        public float debugOverlayRatio = 0.33f;
        public FullScreenDebugMode  fullScreenDebugMode = FullScreenDebugMode.None;
        public float fullscreenDebugMip = 0;

        public MaterialDebugSettings materialDebugSettings = new MaterialDebugSettings();
        public LightingDebugSettings lightingDebugSettings = new LightingDebugSettings();
        public MipMapDebugSettings mipMapDebugSettings = new MipMapDebugSettings();
        public ColorPickerDebugSettings colorPickerDebugSettings = new ColorPickerDebugSettings();

        public static GUIContent[] lightingFullScreenDebugStrings = null;
        public static int[] lightingFullScreenDebugValues = null;
        public static GUIContent[] renderingFullScreenDebugStrings = null;
        public static int[] renderingFullScreenDebugValues = null;

        public DebugDisplaySettings()
        {
            FillFullScreenDebugEnum(ref lightingFullScreenDebugStrings, ref lightingFullScreenDebugValues, FullScreenDebugMode.MinLightingFullScreenDebug, FullScreenDebugMode.MaxLightingFullScreenDebug);
            FillFullScreenDebugEnum(ref renderingFullScreenDebugStrings, ref renderingFullScreenDebugValues, FullScreenDebugMode.MinRenderingFullScreenDebug, FullScreenDebugMode.MaxRenderingFullScreenDebug);
        }

        public int GetDebugMaterialIndex()
        {
            return materialDebugSettings.GetDebugMaterialIndex();
        }

        public DebugLightingMode GetDebugLightingMode()
        {
            return lightingDebugSettings.debugLightingMode;
        }

        public DebugMipMapMode GetDebugMipMapMode()
        {
            return mipMapDebugSettings.debugMipMapMode;
        }

        public bool IsDebugDisplayEnabled()
        {
            return materialDebugSettings.IsDebugDisplayEnabled() || lightingDebugSettings.IsDebugDisplayEnabled() || mipMapDebugSettings.IsDebugDisplayEnabled();
        }

        public bool IsDebugMaterialDisplayEnabled()
        {
            return materialDebugSettings.IsDebugDisplayEnabled();
        }
        public bool IsDebugMipMapDisplayEnabled()
        {
            return mipMapDebugSettings.IsDebugDisplayEnabled();
        }

        private void DisableNonMaterialDebugSettings()
        {
            lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
        }

        public void SetDebugViewMaterial(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewMaterial(value);
        }

        public void SetDebugViewEngine(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewEngine(value);
        }

        public void SetDebugViewVarying(Attributes.DebugViewVarying value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewVarying(value);
        }

        public void SetDebugViewProperties(Attributes.DebugViewProperties value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewProperties(value);
        }

        public void SetDebugViewGBuffer(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewGBuffer(value);
        }

        public void SetDebugLightingMode(DebugLightingMode value)
        {
            if (value != 0)
            {
                materialDebugSettings.DisableMaterialDebug();
                mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
            }
            lightingDebugSettings.debugLightingMode = value;
        }

        public void SetMipMapMode(DebugMipMapMode value)
        {
            if (value != 0)
            {
                materialDebugSettings.DisableMaterialDebug();
                lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            }
            mipMapDebugSettings.debugMipMapMode = value;
        }

        public void UpdateMaterials()
        {
            //if (mipMapDebugSettings.debugMipMapMode != 0)
            //    Texture.SetStreamingTextureMaterialDebugProperties();
        }

        public void RegisterDebug()
        {
            DebugMenuManager.instance.AddDebugItem<float>("Display Stats", "Frame Rate", () => 1.0f / Time.smoothDeltaTime, null, DebugItemFlag.DynamicDisplay);
            DebugMenuManager.instance.AddDebugItem<float>("Display Stats", "Frame Time (ms)", () => Time.smoothDeltaTime * 1000.0f, null, DebugItemFlag.DynamicDisplay);

            DebugMenuManager.instance.AddDebugItem<int>("Material", "Material",() => materialDebugSettings.debugViewMaterial, (value) => SetDebugViewMaterial((int)value), DebugItemFlag.None, new DebugItemHandlerIntEnum(MaterialDebugSettings.debugViewMaterialStrings, MaterialDebugSettings.debugViewMaterialValues));
            DebugMenuManager.instance.AddDebugItem<int>("Material", "Engine",() => materialDebugSettings.debugViewEngine, (value) => SetDebugViewEngine((int)value), DebugItemFlag.None, new DebugItemHandlerIntEnum(MaterialDebugSettings.debugViewEngineStrings, MaterialDebugSettings.debugViewEngineValues));
            DebugMenuManager.instance.AddDebugItem<Attributes.DebugViewVarying>("Material", "Attributes",() => materialDebugSettings.debugViewVarying, (value) => SetDebugViewVarying((Attributes.DebugViewVarying)value));
            DebugMenuManager.instance.AddDebugItem<Attributes.DebugViewProperties>("Material", "Properties", () => materialDebugSettings.debugViewProperties, (value) => SetDebugViewProperties((Attributes.DebugViewProperties)value));
            DebugMenuManager.instance.AddDebugItem<int>("Material", "GBuffer",() => materialDebugSettings.debugViewGBuffer, (value) => SetDebugViewGBuffer((int)value), DebugItemFlag.None, new DebugItemHandlerIntEnum(MaterialDebugSettings.debugViewMaterialGBufferStrings, MaterialDebugSettings.debugViewMaterialGBufferValues));

            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, ShadowMapDebugMode>(kShadowDebugMode, () => lightingDebugSettings.shadowDebugMode, (value) => lightingDebugSettings.shadowDebugMode = (ShadowMapDebugMode)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, bool>(kShadowSelectionDebug, () => lightingDebugSettings.shadowDebugUseSelection, (value) => lightingDebugSettings.shadowDebugUseSelection = (bool)value, DebugItemFlag.EditorOnly);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, uint>(kShadowMapIndexDebug, () => lightingDebugSettings.shadowMapIndex, (value) => lightingDebugSettings.shadowMapIndex = (uint)value, DebugItemFlag.None, new DebugItemHandlerShadowIndex(1));
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, uint>(kShadowAtlasIndexDebug, () => lightingDebugSettings.shadowAtlasIndex, (value) => lightingDebugSettings.shadowAtlasIndex = (uint)value, DebugItemFlag.None, new DebugItemHandlerShadowAtlasIndex(1));
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, float>(kShadowMinValueDebug, () => lightingDebugSettings.shadowMinValue, (value) => lightingDebugSettings.shadowMinValue = (float)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, float>(kShadowMaxValueDebug, () => lightingDebugSettings.shadowMaxValue, (value) => lightingDebugSettings.shadowMaxValue = (float)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, int>(kFullScreenDebugMode, () => (int)fullScreenDebugMode, (value) => fullScreenDebugMode = (FullScreenDebugMode)value, DebugItemFlag.None, new DebugItemHandlerIntEnum(DebugDisplaySettings.lightingFullScreenDebugStrings, DebugDisplaySettings.lightingFullScreenDebugValues));
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, float>(kFullScreenDebugMip, () => fullscreenDebugMip, value => fullscreenDebugMip = (float)value, DebugItemFlag.None, new DebugItemHandlerFloatMinMax(0f, 1f));
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, DebugLightingMode>(kLightingDebugMode, () => lightingDebugSettings.debugLightingMode, (value) => SetDebugLightingMode((DebugLightingMode)value));
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, bool>(kOverrideSmoothnessDebug, () => lightingDebugSettings.overrideSmoothness, (value) => lightingDebugSettings.overrideSmoothness = (bool)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, float>(kOverrideSmoothnessValueDebug, () => lightingDebugSettings.overrideSmoothnessValue, (value) => lightingDebugSettings.overrideSmoothnessValue = (float)value, DebugItemFlag.None, new DebugItemHandlerFloatMinMax(0.0f, 1.0f));
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, Color>(kDebugLightingAlbedo, () => lightingDebugSettings.debugLightingAlbedo, (value) => lightingDebugSettings.debugLightingAlbedo = (Color)value);
            DebugMenuManager.instance.AddDebugItem<bool>("Lighting", kDisplaySkyReflectionDebug, () => lightingDebugSettings.displaySkyReflection, (value) => lightingDebugSettings.displaySkyReflection = (bool)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, float>(kSkyReflectionMipmapDebug, () => lightingDebugSettings.skyReflectionMipmap, (value) => lightingDebugSettings.skyReflectionMipmap = (float)value, DebugItemFlag.None, new DebugItemHandlerFloatMinMax(0.0f, 1.0f));
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, LightLoop.TileClusterDebug>(kTileClusterDebug,() => lightingDebugSettings.tileClusterDebug, (value) => lightingDebugSettings.tileClusterDebug = (LightLoop.TileClusterDebug)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, LightLoop.TileClusterCategoryDebug>(kTileClusterCategoryDebug,() => lightingDebugSettings.tileClusterDebugByCategory, (value) => lightingDebugSettings.tileClusterDebugByCategory = (LightLoop.TileClusterCategoryDebug)value);

            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, float>(kDebugEnvironmentProxyDepthScale, () => lightingDebugSettings.environmentProxyDepthScale, value => lightingDebugSettings.environmentProxyDepthScale = (float)value, DebugItemFlag.None, new DebugItemHandlerFloatMinMax(0.1f, 50f));

            DebugMenuManager.instance.AddDebugItem<int>("Rendering", kFullScreenDebugMode, () => (int)fullScreenDebugMode, (value) => fullScreenDebugMode = (FullScreenDebugMode)value, DebugItemFlag.None, new DebugItemHandlerIntEnum(DebugDisplaySettings.renderingFullScreenDebugStrings, DebugDisplaySettings.renderingFullScreenDebugValues));
            DebugMenuManager.instance.AddDebugItem<DebugMipMapMode>("Rendering", "MipMaps", () => mipMapDebugSettings.debugMipMapMode, (value) => SetMipMapMode((DebugMipMapMode)value));

            DebugMenuManager.instance.AddDebugItem<ColorPickerDebugMode>("Rendering", ColorPickerDebugSettings.kColorPickerDebugMode, () => (int)colorPickerDebugSettings.colorPickerMode, (value) => colorPickerDebugSettings.colorPickerMode = (ColorPickerDebugMode)value);
            DebugMenuManager.instance.AddDebugItem<float>("Rendering", ColorPickerDebugSettings.kColorPickerThreshold0Debug, () => colorPickerDebugSettings.colorThreshold0, (value) => colorPickerDebugSettings.colorThreshold0 = (float)value);
            DebugMenuManager.instance.AddDebugItem<float>("Rendering", ColorPickerDebugSettings.kColorPickerThreshold1Debug, () => colorPickerDebugSettings.colorThreshold1, (value) => colorPickerDebugSettings.colorThreshold1 = (float)value);
            DebugMenuManager.instance.AddDebugItem<float>("Rendering", ColorPickerDebugSettings.kColorPickerThreshold2Debug, () => colorPickerDebugSettings.colorThreshold2, (value) => colorPickerDebugSettings.colorThreshold2 = (float)value);
            DebugMenuManager.instance.AddDebugItem<float>("Rendering", ColorPickerDebugSettings.kColorPickerThreshold3Debug, () => colorPickerDebugSettings.colorThreshold3, (value) => colorPickerDebugSettings.colorThreshold3 = (float)value);
            DebugMenuManager.instance.AddDebugItem<Color>("Rendering", ColorPickerDebugSettings.kColorPickerFontColor, () => colorPickerDebugSettings.fontColor, (value) => colorPickerDebugSettings.fontColor = (Color)value);

        }

        public void OnValidate()
        {
            lightingDebugSettings.OnValidate();
        }

        void FillFullScreenDebugEnum(ref GUIContent[] strings, ref int[] values, FullScreenDebugMode min, FullScreenDebugMode max)
        {
            int count = max - min - 1;
            strings = new GUIContent[count + 1];
            values = new int[count + 1];
            strings[0] = new GUIContent(FullScreenDebugMode.None.ToString());
            values[0] = (int)FullScreenDebugMode.None;
            int index = 1;
            for (int i = (int)min + 1; i < (int)max; ++i)
            {
                strings[index] = new GUIContent(((FullScreenDebugMode)i).ToString());
                values[index] = i;
                index++;
            }
        }
    }
}
