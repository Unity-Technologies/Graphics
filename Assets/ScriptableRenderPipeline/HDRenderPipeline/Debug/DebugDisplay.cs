using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public enum DebugLightingMode
    {
        None,
        DiffuseLighting,
        SpecularLighting,
        VisualizeCascade
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
        public static string kDebugLightingAlbedo = "Debug Lighting Albedo";
        public static string kFullScreenDebugMode = "Fullscreen Debug Mode";
        public static string kDisplaySkyReflectionDebug = "Display Sky Reflection";
        public static string kSkyReflectionMipmapDebug = "Sky Reflection Mipmap";
        public static string kTileDebug = "Tile Debug By Category";


        public float debugOverlayRatio = 0.33f;

        public MaterialDebugSettings materialDebugSettings = new MaterialDebugSettings();
        public LightingDebugSettings lightingDebugSettings = new LightingDebugSettings();
        public RenderingDebugSettings renderingDebugSettings = new RenderingDebugSettings();

        private static bool isDebugViewMaterialInit = false;
        public static GUIContent[] debugViewMaterialStrings = null;
        public static int[] debugViewMaterialValues = null;
        public static GUIContent[] debugViewEngineStrings = null;
        public static int[] debugViewEngineValues = null;
        public static GUIContent[] debugViewMaterialVaryingStrings = null;
        public static int[] debugViewMaterialVaryingValues = null;
        public static GUIContent[] debugViewMaterialPropertiesStrings = null;
        public static int[] debugViewMaterialPropertiesValues = null;
        public static GUIContent[] debugViewMaterialGBufferStrings = null;
        public static int[] debugViewMaterialGBufferValues = null;

        public DebugDisplaySettings()
        {
            BuildDebugRepresentation();
        }

        public int GetDebugMaterialIndex()
        {
            return materialDebugSettings.GetDebugMaterialIndex();
        }

        public DebugLightingMode GetDebugLightingMode()
        {
            return lightingDebugSettings.debugLightingMode;
        }

        public bool IsDebugDisplayEnabled()
        {
            return materialDebugSettings.IsDebugDisplayEnabled() || lightingDebugSettings.IsDebugDisplayEnabled();
        }

        public bool IsDebugMaterialDisplayEnabled()
        {
            return materialDebugSettings.IsDebugDisplayEnabled();
        }

        public void SetDebugViewMaterial(int value)
        {
            if (value != 0)
                lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            materialDebugSettings.SetDebugViewMaterial(value);
        }

        public void SetDebugViewEngine(int value)
        {
            if (value != 0)
                lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            materialDebugSettings.SetDebugViewEngine(value);
        }

        public void SetDebugViewVarying(Attributes.DebugViewVarying value)
        {
            if (value != 0)
                lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            materialDebugSettings.SetDebugViewVarying(value);
        }

        public void SetDebugViewProperties(Attributes.DebugViewProperties value)
        {
            if (value != 0)
                lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            materialDebugSettings.SetDebugViewProperties(value);
        }

        public void SetDebugViewGBuffer(int value)
        {
            if (value != 0)
                lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            materialDebugSettings.SetDebugViewGBuffer(value);
        }

        public void SetDebugLightingMode(DebugLightingMode value)
        {
            if(value != 0)
                materialDebugSettings.DisableMaterialDebug();
            lightingDebugSettings.debugLightingMode = value;
        }

        public void RegisterDebug()
        {
            DebugMenuManager.instance.AddDebugItem<float>("Display Stats", "Frame Rate", () => 1.0f / Time.smoothDeltaTime, null, DebugItemFlag.DynamicDisplay);
            DebugMenuManager.instance.AddDebugItem<float>("Display Stats", "Frame Time (ms)", () => Time.smoothDeltaTime * 1000.0f, null, DebugItemFlag.DynamicDisplay);

            DebugMenuManager.instance.AddDebugItem<int>("Material", "Material",() => materialDebugSettings.debugViewMaterial, (value) => SetDebugViewMaterial((int)value), DebugItemFlag.None, new DebugItemHandlerIntEnum(DebugDisplaySettings.debugViewMaterialStrings, DebugDisplaySettings.debugViewMaterialValues));
            DebugMenuManager.instance.AddDebugItem<int>("Material", "Engine",() => materialDebugSettings.debugViewEngine, (value) => SetDebugViewEngine((int)value), DebugItemFlag.None, new DebugItemHandlerIntEnum(DebugDisplaySettings.debugViewEngineStrings, DebugDisplaySettings.debugViewEngineValues));
            DebugMenuManager.instance.AddDebugItem<Attributes.DebugViewVarying>("Material", "Attributes",() => materialDebugSettings.debugViewVarying, (value) => SetDebugViewVarying((Attributes.DebugViewVarying)value));
            DebugMenuManager.instance.AddDebugItem<Attributes.DebugViewProperties>("Material", "Properties", () => materialDebugSettings.debugViewProperties, (value) => SetDebugViewProperties((Attributes.DebugViewProperties)value));
            DebugMenuManager.instance.AddDebugItem<int>("Material", "GBuffer",() => materialDebugSettings.debugViewGBuffer, (value) => SetDebugViewGBuffer((int)value), DebugItemFlag.None, new DebugItemHandlerIntEnum(DebugDisplaySettings.debugViewMaterialGBufferStrings, DebugDisplaySettings.debugViewMaterialGBufferValues));

            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, bool>(kEnableShadowDebug, () => lightingDebugSettings.enableShadows, (value) => lightingDebugSettings.enableShadows = (bool)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, ShadowMapDebugMode>(kShadowDebugMode, () => lightingDebugSettings.shadowDebugMode, (value) => lightingDebugSettings.shadowDebugMode = (ShadowMapDebugMode)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, bool>(kShadowSelectionDebug, () => lightingDebugSettings.shadowDebugUseSelection, (value) => lightingDebugSettings.shadowDebugUseSelection = (bool)value, DebugItemFlag.EditorOnly);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, uint>(kShadowMapIndexDebug, () => lightingDebugSettings.shadowMapIndex, (value) => lightingDebugSettings.shadowMapIndex = (uint)value, DebugItemFlag.None, new DebugItemHandlerShadowIndex(1));
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, uint>(kShadowAtlasIndexDebug, () => lightingDebugSettings.shadowAtlasIndex, (value) => lightingDebugSettings.shadowAtlasIndex = (uint)value, DebugItemFlag.None, new DebugItemHandlerShadowAtlasIndex(1));
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, float>(kShadowMinValueDebug, () => lightingDebugSettings.shadowMinValue, (value) => lightingDebugSettings.shadowMinValue = (float)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, float>(kShadowMaxValueDebug, () => lightingDebugSettings.shadowMaxValue, (value) => lightingDebugSettings.shadowMaxValue = (float)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, FullScreenDebugMode>(kFullScreenDebugMode, () => lightingDebugSettings.fullScreenDebugMode, (value) => lightingDebugSettings.fullScreenDebugMode = (FullScreenDebugMode)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, DebugLightingMode>(kLightingDebugMode, () => lightingDebugSettings.debugLightingMode, (value) => SetDebugLightingMode((DebugLightingMode)value));
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, bool>(kOverrideSmoothnessDebug, () => lightingDebugSettings.overrideSmoothness, (value) => lightingDebugSettings.overrideSmoothness = (bool)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, float>(kOverrideSmoothnessValueDebug, () => lightingDebugSettings.overrideSmoothnessValue, (value) => lightingDebugSettings.overrideSmoothnessValue = (float)value, DebugItemFlag.None, new DebugItemHandlerFloatMinMax(0.0f, 1.0f));
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, Color>(kDebugLightingAlbedo, () => lightingDebugSettings.debugLightingAlbedo, (value) => lightingDebugSettings.debugLightingAlbedo = (Color)value);
            DebugMenuManager.instance.AddDebugItem<bool>("Lighting", kDisplaySkyReflectionDebug, () => lightingDebugSettings.displaySkyReflection, (value) => lightingDebugSettings.displaySkyReflection = (bool)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, float>(kSkyReflectionMipmapDebug, () => lightingDebugSettings.skyReflectionMipmap, (value) => lightingDebugSettings.skyReflectionMipmap = (float)value, DebugItemFlag.None, new DebugItemHandlerFloatMinMax(0.0f, 1.0f));
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, TilePass.TileSettings.TileDebug>(kTileDebug,() => lightingDebugSettings.tileDebugByCategory, (value) => lightingDebugSettings.tileDebugByCategory = (TilePass.TileSettings.TileDebug)value);

            DebugMenuManager.instance.AddDebugItem<bool>("Rendering", "Display Opaque",() => renderingDebugSettings.displayOpaqueObjects, (value) => renderingDebugSettings.displayOpaqueObjects = (bool)value);
            DebugMenuManager.instance.AddDebugItem<bool>("Rendering", "Display Transparency",() => renderingDebugSettings.displayTransparentObjects, (value) => renderingDebugSettings.displayTransparentObjects = (bool)value);
            DebugMenuManager.instance.AddDebugItem<bool>("Rendering", "Enable Distortion",() => renderingDebugSettings.enableDistortion, (value) => renderingDebugSettings.enableDistortion = (bool)value);
            DebugMenuManager.instance.AddDebugItem<bool>("Rendering", "Enable Subsurface Scattering",() => renderingDebugSettings.enableSSSAndTransmission, (value) => renderingDebugSettings.enableSSSAndTransmission = (bool)value);
        }

        public void OnValidate()
        {
            lightingDebugSettings.OnValidate();
        }

        // className include the additional "/"
        void FillWithProperties(Type type, GUIContent[] debugViewMaterialStrings, int[] debugViewMaterialValues, string className, ref int index)
        {
            var attributes = type.GetCustomAttributes(true);
            // Get attribute to get the start number of the value for the enum
            var attr = attributes[0] as GenerateHLSL;

            if (!attr.needParamDebug)
            {
                return;
            }

            var fields = type.GetFields();

            var localIndex = 0;
            foreach (var field in fields)
            {
                var fieldName = field.Name;

                // Check if the display name have been override by the users
                if (Attribute.IsDefined(field, typeof(SurfaceDataAttributes)))
                {
                    var propertyAttr = (SurfaceDataAttributes[])field.GetCustomAttributes(typeof(SurfaceDataAttributes), false);
                    if (propertyAttr[0].displayName != "")
                    {
                        fieldName = propertyAttr[0].displayName;
                    }
                }

                fieldName = className + fieldName;

                debugViewMaterialStrings[index] = new GUIContent(fieldName);
                debugViewMaterialValues[index] = attr.paramDefinesStart + (int)localIndex;
                index++;
                localIndex++;
            }
        }

        void FillWithPropertiesEnum(Type type, GUIContent[] debugViewMaterialStrings, int[] debugViewMaterialValues, string prefix, ref int index)
        {
            var names = Enum.GetNames(type);

            var localIndex = 0;
            foreach (var value in Enum.GetValues(type))
            {
                var valueName = prefix + names[localIndex];

                debugViewMaterialStrings[index] = new GUIContent(valueName);
                debugViewMaterialValues[index] = (int)value;
                index++;
                localIndex++;
            }
        }

        public class MaterialItem
        {
            public String className;
            public Type surfaceDataType;
            public Type bsdfDataType;
        };

        void BuildDebugRepresentation()
        {
            if (!isDebugViewMaterialInit)
            {
                List<RenderPipelineMaterial> materialList = Utilities.GetRenderPipelineMaterialList();

                // TODO: Share this code to retrieve deferred material with HDRenderPipeline
                // Find first material that have non 0 Gbuffer count and assign it as deferredMaterial
                Type bsdfDataDeferredType = null;
                foreach (RenderPipelineMaterial material in materialList)
                {
                    if (material.GetMaterialGBufferCount() > 0)
                    {
                        bsdfDataDeferredType = material.GetType().GetNestedType("BSDFData");
                    }
                }

                // TODO: Handle the case of no Gbuffer material
                Debug.Assert(bsdfDataDeferredType != null);

                List<MaterialItem> materialItems = new List<MaterialItem>();

                int numSurfaceDataFields = 0;
                int numBSDFDataFields = 0;
                foreach (RenderPipelineMaterial material in materialList)
                {
                    MaterialItem item = new MaterialItem();

                    item.className = material.GetType().Name + "/";

                    item.surfaceDataType = material.GetType().GetNestedType("SurfaceData");
                    numSurfaceDataFields += item.surfaceDataType.GetFields().Length;

                    item.bsdfDataType = material.GetType().GetNestedType("BSDFData");
                    numBSDFDataFields += item.bsdfDataType.GetFields().Length;

                    materialItems.Add(item);
                }

                // Material properties debug
                var num =   typeof(Builtin.BuiltinData).GetFields().Length * materialList.Count // BuildtinData are duplicated for each material
                            + numSurfaceDataFields + 1; // +1 for None case

                debugViewMaterialStrings = new GUIContent[num];
                debugViewMaterialValues = new int[num];
                // Special case for None since it cannot be inferred from SurfaceData/BuiltinData
                debugViewMaterialStrings[0] = new GUIContent("None");
                debugViewMaterialValues[0] = 0;
                var index = 1;
                // 0 is a reserved number and should not be used (allow to track error)
                foreach (MaterialItem item in materialItems)
                {
                    // BuiltinData are duplicated for each material
                    FillWithProperties(typeof(Builtin.BuiltinData), debugViewMaterialStrings, debugViewMaterialValues, item.className, ref index);
                    FillWithProperties(item.surfaceDataType, debugViewMaterialStrings, debugViewMaterialValues, item.className, ref index);
                }

                // Engine properties debug
                num = numBSDFDataFields + 1; // +1 for None case
                debugViewEngineStrings = new GUIContent[num];
                debugViewEngineValues = new int[num];
                // 0 is a reserved number and should not be used (allow to track error)
                debugViewEngineStrings[0] = new GUIContent("None");
                debugViewEngineValues[0] = 0;
                index = 1;
                foreach (MaterialItem item in materialItems)
                {
                    FillWithProperties(item.bsdfDataType, debugViewEngineStrings, debugViewEngineValues, item.className, ref index);
                }

                // Attributes debug
                var varyingNames = Enum.GetNames(typeof(Attributes.DebugViewVarying));
                debugViewMaterialVaryingStrings = new GUIContent[varyingNames.Length];
                debugViewMaterialVaryingValues = new int[varyingNames.Length];
                index = 0;
                FillWithPropertiesEnum(typeof(Attributes.DebugViewVarying), debugViewMaterialVaryingStrings, debugViewMaterialVaryingValues, "", ref index);

                // Properties debug
                var propertiesNames = Enum.GetNames(typeof(Attributes.DebugViewProperties));
                debugViewMaterialPropertiesStrings = new GUIContent[propertiesNames.Length];
                debugViewMaterialPropertiesValues = new int[propertiesNames.Length];
                index = 0;
                FillWithPropertiesEnum(typeof(Attributes.DebugViewProperties), debugViewMaterialPropertiesStrings, debugViewMaterialPropertiesValues, "", ref index);

                // Gbuffer debug
                var gbufferNames = Enum.GetNames(typeof(Attributes.DebugViewGbuffer));
                debugViewMaterialGBufferStrings = new GUIContent[gbufferNames.Length + bsdfDataDeferredType.GetFields().Length];
                debugViewMaterialGBufferValues = new int[gbufferNames.Length + bsdfDataDeferredType.GetFields().Length];
                index = 0;
                FillWithPropertiesEnum(typeof(Attributes.DebugViewGbuffer), debugViewMaterialGBufferStrings, debugViewMaterialGBufferValues, "", ref index);
                FillWithProperties(typeof(Lit.BSDFData), debugViewMaterialGBufferStrings, debugViewMaterialGBufferValues, "", ref index);

                isDebugViewMaterialInit = true;
            }
        }
    }

    namespace Attributes
    {
        // 0 is reserved!
        [GenerateHLSL]
        public enum DebugViewVarying
        {
            None = 0,
            Texcoord0 = 1,
            Texcoord1,
            Texcoord2,
            Texcoord3,
            VertexTangentWS,
            VertexBitangentWS,
            VertexNormalWS,
            VertexColor,
            VertexColorAlpha,
            // caution if you add something here, it must start below
        };

        // Number must be contiguous
        [GenerateHLSL]
        public enum DebugViewGbuffer
        {
            None = 0,
            Depth = DebugViewVarying.VertexColorAlpha + 1,
            BakeDiffuseLightingWithAlbedoPlusEmissive,
        }

        // Number must be contiguous
        [GenerateHLSL]
        public enum DebugViewProperties
        {
            None = 0,
            Tessellation = DebugViewGbuffer.BakeDiffuseLightingWithAlbedoPlusEmissive + 1,
            PerPixelDisplacement,
            DepthOffset,
            Lightmap,
        }
    }

    [Serializable]
    public class MaterialDebugSettings
    {
        public int debugViewMaterial { get { return m_DebugViewMaterial; } }
        public int debugViewEngine { get { return m_DebugViewEngine; } }
        public Attributes.DebugViewVarying debugViewVarying { get { return m_DebugViewVarying; } }
        public Attributes.DebugViewProperties debugViewProperties { get { return m_DebugViewProperties; } }
        public int debugViewGBuffer { get { return m_DebugViewGBuffer; } }

        int                             m_DebugViewMaterial = 0; // No enum there because everything is generated from materials.
        int                             m_DebugViewEngine = 0;  // No enum there because everything is generated from BSDFData
        Attributes.DebugViewVarying     m_DebugViewVarying = Attributes.DebugViewVarying.None;
        Attributes.DebugViewProperties  m_DebugViewProperties = Attributes.DebugViewProperties.None;
        int                             m_DebugViewGBuffer = 0; // Can't use GBuffer enum here because the values are actually split between this enum and values from Lit.BSDFData

        public int GetDebugMaterialIndex()
        {
            // This value is used in the shader for the actual debug display.
            // There is only one uniform parameter for that so we just add all of them
            // They are all mutually exclusive so return the sum will return the right index.
            return m_DebugViewGBuffer + m_DebugViewMaterial + m_DebugViewEngine + (int)m_DebugViewVarying + (int)m_DebugViewProperties;
        }

        public void DisableMaterialDebug()
        {
            m_DebugViewMaterial = 0;
            m_DebugViewEngine = 0;
            m_DebugViewVarying = Attributes.DebugViewVarying.None;
            m_DebugViewProperties = Attributes.DebugViewProperties.None;
            m_DebugViewGBuffer = 0;
        }

        public void SetDebugViewMaterial(int value)
        {
            if (value != 0)
                DisableMaterialDebug();
            m_DebugViewMaterial = value;
        }

        public void SetDebugViewEngine(int value)
        {
            if (value != 0)
                DisableMaterialDebug();
            m_DebugViewEngine = value;
        }

        public void SetDebugViewVarying(Attributes.DebugViewVarying value)
        {
            if (value != 0)
                DisableMaterialDebug();
            m_DebugViewVarying = value;
        }
        public void SetDebugViewProperties(Attributes.DebugViewProperties value)
        {
            if (value != 0)
                DisableMaterialDebug();
            m_DebugViewProperties = value;
        }

        public void SetDebugViewGBuffer(int value)
        {
            if (value != 0)
                DisableMaterialDebug();
            m_DebugViewGBuffer = value;
        }


        public bool IsDebugDisplayEnabled()
        {
            return (m_DebugViewEngine != 0 || m_DebugViewMaterial != 0 || m_DebugViewVarying != Attributes.DebugViewVarying.None || m_DebugViewProperties != Attributes.DebugViewProperties.None || m_DebugViewGBuffer != 0);
        }
    }

    [Serializable]
    public class RenderingDebugSettings
    {
        public bool displayOpaqueObjects = true;
        public bool displayTransparentObjects = true;
        public bool enableDistortion = true;
        public bool enableSSSAndTransmission = true;
    }

    public enum ShadowMapDebugMode
    {
        None,
        VisualizeAtlas,
        VisualizeShadowMap
    }

    [GenerateHLSL]
    public enum FullScreenDebugMode
    {
        None,
        SSAO,
        SSAOBeforeFiltering,
        MotionVectors,
        NanTracker
    }

    [Serializable]
    public class LightingDebugSettings
    {
        public bool IsDebugDisplayEnabled()
        {
            return debugLightingMode != DebugLightingMode.None;
        }

        public DebugLightingMode    debugLightingMode = DebugLightingMode.None;
        public bool                 enableShadows = true;
        public ShadowMapDebugMode   shadowDebugMode = ShadowMapDebugMode.None;
        public bool                 shadowDebugUseSelection = false;
        public uint                 shadowMapIndex = 0;
        public uint                 shadowAtlasIndex = 0;
        public float                shadowMinValue = 0.0f;
        public float                shadowMaxValue = 1.0f;
        public FullScreenDebugMode  fullScreenDebugMode = FullScreenDebugMode.None;

        public bool                 overrideSmoothness = false;
        public float                overrideSmoothnessValue = 0.5f;
        public Color                debugLightingAlbedo = new Color(0.5f, 0.5f, 0.5f);

        public bool                 displaySkyReflection = false;
        public float                skyReflectionMipmap = 0.0f;

        public TilePass.TileSettings.TileDebug  tileDebugByCategory = TilePass.TileSettings.TileDebug.None;

        public void OnValidate()
        {
            overrideSmoothnessValue = Mathf.Clamp(overrideSmoothnessValue, 0.0f, 1.0f);
            skyReflectionMipmap = Mathf.Clamp(skyReflectionMipmap, 0.0f, 1.0f);
        }
    }
}
