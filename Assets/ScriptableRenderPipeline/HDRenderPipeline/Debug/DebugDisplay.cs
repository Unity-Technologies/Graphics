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
            DebugMenuManager.instance.AddDebugItem<float>("Display Stats", "Frame Rate", () => 1.0f / Time.deltaTime, null, true);
            DebugMenuManager.instance.AddDebugItem<float>("Display Stats", "Frame Time", () => Time.deltaTime * 1000.0f, null, true);

            DebugMenuManager.instance.AddDebugItem<int>("Material", "Material",() => materialDebugSettings.debugViewMaterial, (value) => SetDebugViewMaterial((int)value), false, new DebugItemHandlerIntEnum(DebugDisplaySettings.debugViewMaterialStrings, DebugDisplaySettings.debugViewMaterialValues));
            DebugMenuManager.instance.AddDebugItem<int>("Material", "Engine",() => materialDebugSettings.debugViewEngine, (value) => SetDebugViewEngine((int)value), false, new DebugItemHandlerIntEnum(DebugDisplaySettings.debugViewEngineStrings, DebugDisplaySettings.debugViewEngineValues));
            DebugMenuManager.instance.AddDebugItem<Attributes.DebugViewVarying>("Material", "Attributes",() => materialDebugSettings.debugViewVarying, (value) => SetDebugViewVarying((Attributes.DebugViewVarying)value));
            DebugMenuManager.instance.AddDebugItem<int>("Material", "GBuffer",() => materialDebugSettings.debugViewGBuffer, (value) => SetDebugViewGBuffer((int)value), false, new DebugItemHandlerIntEnum(DebugDisplaySettings.debugViewMaterialGBufferStrings, DebugDisplaySettings.debugViewMaterialGBufferValues));

            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, bool>("Enable Shadows", () => lightingDebugSettings.enableShadows, (value) => lightingDebugSettings.enableShadows = (bool)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, ShadowMapDebugMode>("Shadow Debug Mode", () => lightingDebugSettings.shadowDebugMode, (value) => lightingDebugSettings.shadowDebugMode = (ShadowMapDebugMode)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, uint>("Shadow Map Index", () => lightingDebugSettings.shadowMapIndex, (value) => lightingDebugSettings.shadowMapIndex = (uint)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, DebugLightingMode>("Lighting Debug Mode", () => lightingDebugSettings.debugLightingMode, (value) => SetDebugLightingMode((DebugLightingMode)value));
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, bool>("Override Smoothness", () => lightingDebugSettings.overrideSmoothness, (value) => lightingDebugSettings.overrideSmoothness = (bool)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, float>("Override Smoothness Value", () => lightingDebugSettings.overrideSmoothnessValue, (value) => lightingDebugSettings.overrideSmoothnessValue = (float)value, false, new DebugItemHandlerFloatMinMax(0.0f, 1.0f));
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, Color>("Debug Lighting Albedo", () => lightingDebugSettings.debugLightingAlbedo, (value) => lightingDebugSettings.debugLightingAlbedo = (Color)value);
            DebugMenuManager.instance.AddDebugItem<bool>("Lighting", "Display Sky Reflection", () => lightingDebugSettings.displaySkyReflection, (value) => lightingDebugSettings.displaySkyReflection = (bool)value);
            DebugMenuManager.instance.AddDebugItem<LightingDebugPanel, float>("Sky Reflection Mipmap", () => lightingDebugSettings.skyReflectionMipmap, (value) => lightingDebugSettings.skyReflectionMipmap = (float)value, false, new DebugItemHandlerFloatMinMax(0.0f, 1.0f));

            DebugMenuManager.instance.AddDebugItem<bool>("Rendering", "Display Opaque",() => renderingDebugSettings.displayOpaqueObjects, (value) => renderingDebugSettings.displayOpaqueObjects = (bool)value);
            DebugMenuManager.instance.AddDebugItem<bool>("Rendering", "Display Transparency",() => renderingDebugSettings.displayTransparentObjects, (value) => renderingDebugSettings.displayTransparentObjects = (bool)value);
            DebugMenuManager.instance.AddDebugItem<bool>("Rendering", "Enable Distortion",() => renderingDebugSettings.enableDistortion, (value) => renderingDebugSettings.enableDistortion = (bool)value);
            DebugMenuManager.instance.AddDebugItem<bool>("Rendering", "Enable Subsurface Scattering",() => renderingDebugSettings.enableSSS, (value) => renderingDebugSettings.enableSSS = (bool)value);
        }

        public void OnValidate()
        {
            lightingDebugSettings.OnValidate();
        }

        void FillWithProperties(Type type, GUIContent[] debugViewMaterialStrings, int[] debugViewMaterialValues, string strSubNameSpace, ref int index)
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

                fieldName = strSubNameSpace + fieldName;

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

        string GetSubNameSpaceName(Type type)
        {
            return type.Namespace.Substring(type.Namespace.LastIndexOf((".")) + 1) + "/";
        }

        void BuildDebugRepresentation()
        {
            if (!isDebugViewMaterialInit)
            {
                var varyingNames = Enum.GetNames(typeof(Attributes.DebugViewVarying));
                debugViewMaterialVaryingStrings = new GUIContent[varyingNames.Length];
                debugViewMaterialVaryingValues = new int[varyingNames.Length];
                var gbufferNames = Enum.GetNames(typeof(Attributes.DebugViewGbuffer));
                debugViewMaterialGBufferStrings = new GUIContent[gbufferNames.Length + typeof(Lit.BSDFData).GetFields().Length];
                debugViewMaterialGBufferValues = new int[gbufferNames.Length + typeof(Lit.BSDFData).GetFields().Length];

                var num = typeof(Builtin.BuiltinData).GetFields().Length * 2 // BuildtinData are duplicated for each material
                    + typeof(Lit.SurfaceData).GetFields().Length
                    + typeof(Unlit.SurfaceData).GetFields().Length
                    + 1; // None

                debugViewMaterialStrings = new GUIContent[num];
                debugViewMaterialValues = new int[num];

                num = typeof(Lit.BSDFData).GetFields().Length
                    + typeof(Unlit.BSDFData).GetFields().Length
                    + 1; // None

                debugViewEngineStrings = new GUIContent[num];
                debugViewEngineValues = new int[num];


                // Special case for None since it cannot be inferred from SurfaceDAta/BuiltinData
                debugViewMaterialStrings[0] = new GUIContent("None");
                debugViewMaterialValues[0] = 0;
                var index = 1;
                // 0 is a reserved number and should not be used (allow to track error)
                FillWithProperties(typeof(Builtin.BuiltinData), debugViewMaterialStrings, debugViewMaterialValues, GetSubNameSpaceName(typeof(Lit.SurfaceData)), ref index);
                FillWithProperties(typeof(Lit.SurfaceData), debugViewMaterialStrings, debugViewMaterialValues, GetSubNameSpaceName(typeof(Lit.SurfaceData)), ref index);
                FillWithProperties(typeof(Builtin.BuiltinData), debugViewMaterialStrings, debugViewMaterialValues, GetSubNameSpaceName(typeof(Unlit.SurfaceData)), ref index);
                FillWithProperties(typeof(Unlit.SurfaceData), debugViewMaterialStrings, debugViewMaterialValues, GetSubNameSpaceName(typeof(Unlit.SurfaceData)), ref index);

                // Engine
                debugViewEngineStrings[0] = new GUIContent("None");
                debugViewEngineValues[0] = 0;
                index = 1;
                FillWithProperties(typeof(Lit.BSDFData), debugViewEngineStrings, debugViewEngineValues, GetSubNameSpaceName(typeof(Lit.BSDFData)), ref index);
                FillWithProperties(typeof(Unlit.BSDFData), debugViewEngineStrings, debugViewEngineValues, GetSubNameSpaceName(typeof(Unlit.BSDFData)), ref index);

                index = 0;
                FillWithPropertiesEnum(typeof(Attributes.DebugViewVarying), debugViewMaterialVaryingStrings, debugViewMaterialVaryingValues, "", ref index);
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
    }

    [Serializable]
    public class MaterialDebugSettings
    {
        public int debugViewMaterial { get { return m_DebugViewMaterial; } }
        public int debugViewEngine { get { return m_DebugViewEngine; } }
        public Attributes.DebugViewVarying debugViewVarying { get { return m_DebugViewVarying; } }
        public int debugViewGBuffer { get { return m_DebugViewGBuffer; } }

        int                             m_DebugViewMaterial = 0; // No enum there because everything is generated from materials.
        int                             m_DebugViewEngine = 0;  // No enum there because everything is generated from BSDFData
        Attributes.DebugViewVarying     m_DebugViewVarying = Attributes.DebugViewVarying.None;
        int                             m_DebugViewGBuffer = 0; // Can't use GBuffer enum here because the values are actually split between this enum and values from Lit.BSDFData

        public int GetDebugMaterialIndex()
        {
            // This value is used in the shader for the actual debug display.
            // There is only one uniform parameter for that so we just add all of them
            // They are all mutually exclusive so return the sum will return the right index.
            return m_DebugViewGBuffer + m_DebugViewMaterial + m_DebugViewEngine + (int)m_DebugViewVarying;
        }

        public void DisableMaterialDebug()
        {
            m_DebugViewMaterial = 0;
            m_DebugViewEngine = 0;
            m_DebugViewVarying = Attributes.DebugViewVarying.None;
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

        public void SetDebugViewGBuffer(int value)
        {
            if (value != 0)
                DisableMaterialDebug();
            m_DebugViewGBuffer = value;
        }


        public bool IsDebugDisplayEnabled()
        {
            return (m_DebugViewEngine != 0 || m_DebugViewMaterial != 0 || m_DebugViewVarying != Attributes.DebugViewVarying.None || m_DebugViewGBuffer != 0);
        }
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
        public uint                 shadowMapIndex = 0;

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
