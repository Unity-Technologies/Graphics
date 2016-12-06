using System;
using UnityEditor;

//using EditorGUIUtility=UnityEditor.EditorGUIUtility;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    [CustomEditor(typeof(HDRenderLoop))]
    public class HDRenderLoopInspector : Editor
    {
        private class Styles
        {
            public readonly GUIContent debugParameters = new GUIContent("Debug Parameters");
            public readonly GUIContent debugViewMaterial = new GUIContent("DebugView Material", "Display various properties of Materials.");

            public readonly GUIContent displayOpaqueObjects = new GUIContent("Display Opaque Objects", "Toggle opaque objects rendering on and off.");
            public readonly GUIContent displayTransparentObjects = new GUIContent("Display Transparent Objects", "Toggle transparent objects rendering on and off.");
            public readonly GUIContent enableTonemap = new GUIContent("Enable Tonemap");
            public readonly GUIContent exposure = new GUIContent("Exposure");

            public readonly GUIContent useForwardRenderingOnly = new GUIContent("Use Forward Rendering Only");
            public readonly GUIContent useDepthPrepass = new GUIContent("Use Depth Prepass");
            public readonly GUIContent useSinglePassLightLoop = new GUIContent("Use single Pass light loop");

            public bool isDebugViewMaterialInit = false;
            public GUIContent[] debugViewMaterialStrings = null;
            public int[] debugViewMaterialValues = null;

            public readonly GUIContent skyParameters = new GUIContent("Sky Parameters");
            public readonly GUIContent skyResolution = new GUIContent("Sky Resolution");
            public readonly GUIContent skyExposure = new GUIContent("Sky Exposure");
            public readonly GUIContent skyRotation = new GUIContent("Sky Rotation");
            public readonly GUIContent skyMultiplier = new GUIContent("Sky Multiplier");

            public readonly GUIContent shadowSettings = new GUIContent("Shadow Settings");

            public readonly GUIContent shadowsEnabled = new GUIContent("Enabled");
            public readonly GUIContent shadowsAtlasWidth = new GUIContent("Atlas width");
            public readonly GUIContent shadowsAtlasHeight = new GUIContent("Atlas height");

            public readonly GUIContent shadowsMaxShadowDistance = new GUIContent("Maximum shadow distance");
            public readonly GUIContent shadowsDirectionalLightCascadeCount = new GUIContent("Directional cascade count");
            public readonly GUIContent[] shadowsCascadeCounts = new GUIContent[] { new GUIContent("1"), new GUIContent("2"), new GUIContent("3"), new GUIContent("4") };
            public readonly int[] shadowsCascadeCountValues = new int[] { 1, 2, 3, 4 };
            public readonly GUIContent shadowsCascades = new GUIContent("Cascade values");

            public readonly GUIContent tileLightLoopSettings = new GUIContent("Tile Light Loop settings");
            public readonly string[] tileLightLoopDebugTileFlagStrings = new string[] { "Direct Light", "Reflection Light", "Area Light"};
            public readonly GUIContent directIndirectSinglePass = new GUIContent("Enable direct and indirect lighting in single pass", "Toggle");
            public readonly GUIContent bigTilePrepass = new GUIContent("Enable big tile prepass", "Toggle");
            public readonly GUIContent clustered = new GUIContent("Enable clustered", "Toggle");


            public readonly GUIContent textureSettings = new GUIContent("texture Settings");

            public readonly GUIContent spotCookieSize = new GUIContent("spotCookie Size");
            public readonly GUIContent pointCookieSize = new GUIContent("pointCookie Size");
            public readonly GUIContent reflectionCubemapSize = new GUIContent("reflectionCubemap Size");              
        }

        private static Styles s_Styles = null;

        private static Styles styles
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new Styles();
                return s_Styles;
            }
        }

        const float k_MaxExposure = 32.0f;

        string GetSubNameSpaceName(Type type)
        {
            return type.Namespace.Substring(type.Namespace.LastIndexOf((".")) + 1) + "/";
        }

        void FillWithProperties(Type type, GUIContent[] debugViewMaterialStrings, int[] debugViewMaterialValues, bool isBSDFData, string strSubNameSpace, ref int index)
        {
            var attributes = type.GetCustomAttributes(true);
            // Get attribute to get the start number of the value for the enum
            var attr = attributes[0] as GenerateHLSL;

            if (!attr.needParamDefines)
            {
                return ;
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

                fieldName = (isBSDFData ? "Engine/" : "") + strSubNameSpace + fieldName;

                debugViewMaterialStrings[index] = new GUIContent(fieldName);
                debugViewMaterialValues[index] = attr.paramDefinesStart + (int)localIndex;
                index++;
                localIndex++;
            }
        }

        void FillWithPropertiesEnum(Type type, GUIContent[] debugViewMaterialStrings, int[] debugViewMaterialValues, string prefix, bool isBSDFData, ref int index)
        {
            var names = Enum.GetNames(type);

            var localIndex = 0;
            foreach (var value in Enum.GetValues(type))
            {
                var valueName = (isBSDFData ? "Engine/" : "" + prefix) + names[localIndex];

                debugViewMaterialStrings[index] = new GUIContent(valueName);
                debugViewMaterialValues[index] = (int)value;
                index++;
                localIndex++;
            }
        }

        public override void OnInspectorGUI()
        {
            var renderLoop = target as HDRenderLoop;

            if (!renderLoop)
                return;

            var debugParameters = renderLoop.debugParameters;

            EditorGUILayout.LabelField(styles.debugParameters);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            if (!styles.isDebugViewMaterialInit)
            {
                var varyingNames = Enum.GetNames(typeof(Attributes.DebugViewVarying));
                var gbufferNames = Enum.GetNames(typeof(Attributes.DebugViewGbuffer));

                // +1 for the zero case
                var num = 1 + varyingNames.Length
                          + gbufferNames.Length
                          + typeof(Builtin.BuiltinData).GetFields().Length * 2 // BuildtinData are duplicated for each material
                          + typeof(Lit.SurfaceData).GetFields().Length
                          + typeof(Lit.BSDFData).GetFields().Length
                          + typeof(Unlit.SurfaceData).GetFields().Length
                          + typeof(Unlit.BSDFData).GetFields().Length;

                styles.debugViewMaterialStrings = new GUIContent[num];
                styles.debugViewMaterialValues = new int[num];

                var index = 0;

                // 0 is a reserved number
                styles.debugViewMaterialStrings[0] = new GUIContent("None");
                styles.debugViewMaterialValues[0] = 0;
                index++;

                FillWithPropertiesEnum(typeof(Attributes.DebugViewVarying), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, GetSubNameSpaceName(typeof(Attributes.DebugViewVarying)), false, ref index);
                FillWithProperties(typeof(Builtin.BuiltinData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, false, GetSubNameSpaceName(typeof(Lit.SurfaceData)), ref index);
                FillWithProperties(typeof(Lit.SurfaceData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, false, GetSubNameSpaceName(typeof(Lit.SurfaceData)), ref index);
                FillWithProperties(typeof(Builtin.BuiltinData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, false, GetSubNameSpaceName(typeof(Unlit.SurfaceData)), ref index);
                FillWithProperties(typeof(Unlit.SurfaceData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, false, GetSubNameSpaceName(typeof(Unlit.SurfaceData)), ref index);

                // Engine
                FillWithPropertiesEnum(typeof(Attributes.DebugViewGbuffer), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, "", true, ref index);
                FillWithProperties(typeof(Lit.BSDFData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, true, "", ref index);
                FillWithProperties(typeof(Unlit.BSDFData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, true, "", ref index);

                styles.isDebugViewMaterialInit = true;
            }

            debugParameters.debugViewMaterial = EditorGUILayout.IntPopup(styles.debugViewMaterial, (int)debugParameters.debugViewMaterial, styles.debugViewMaterialStrings, styles.debugViewMaterialValues);

            EditorGUILayout.Space();
            debugParameters.enableTonemap = EditorGUILayout.Toggle(styles.enableTonemap, debugParameters.enableTonemap);
            debugParameters.exposure = Mathf.Max(Mathf.Min(EditorGUILayout.FloatField(styles.exposure, debugParameters.exposure), k_MaxExposure), -k_MaxExposure);

            EditorGUILayout.Space();
            debugParameters.displayOpaqueObjects = EditorGUILayout.Toggle(styles.displayOpaqueObjects, debugParameters.displayOpaqueObjects);
            debugParameters.displayTransparentObjects = EditorGUILayout.Toggle(styles.displayTransparentObjects, debugParameters.displayTransparentObjects);
            debugParameters.useForwardRenderingOnly = EditorGUILayout.Toggle(styles.useForwardRenderingOnly, debugParameters.useForwardRenderingOnly);
            debugParameters.useDepthPrepass = EditorGUILayout.Toggle(styles.useDepthPrepass, debugParameters.useDepthPrepass);
            debugParameters.useSinglePassLightLoop = EditorGUILayout.Toggle(styles.useSinglePassLightLoop, debugParameters.useSinglePassLightLoop);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(renderLoop); // Repaint
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            var skyParameters = renderLoop.skyParameters;

            EditorGUILayout.LabelField(styles.skyParameters);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            skyParameters.skyHDRI = (Texture)EditorGUILayout.ObjectField("Cubemap", skyParameters.skyHDRI, typeof(Cubemap), false);

            skyParameters.skyResolution = (SkyResolution)EditorGUILayout.EnumPopup(styles.skyResolution, skyParameters.skyResolution);
            skyParameters.exposure = Mathf.Max(Mathf.Min(EditorGUILayout.FloatField(styles.skyExposure, skyParameters.exposure), 32), -32);
            skyParameters.multiplier = Mathf.Max(EditorGUILayout.FloatField(styles.skyMultiplier, skyParameters.multiplier), 0);
            skyParameters.rotation = Mathf.Max(Mathf.Min(EditorGUILayout.FloatField(styles.skyRotation, skyParameters.rotation), 360), -360);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(renderLoop); // Repaint
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            var shadowParameters = renderLoop.shadowSettings;

            EditorGUILayout.LabelField(styles.shadowSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            shadowParameters.enabled = EditorGUILayout.Toggle(styles.shadowsEnabled, shadowParameters.enabled);
            shadowParameters.shadowAtlasWidth = Mathf.Max(0, EditorGUILayout.IntField(styles.shadowsAtlasWidth, shadowParameters.shadowAtlasWidth));
            shadowParameters.shadowAtlasHeight = Mathf.Max(0, EditorGUILayout.IntField(styles.shadowsAtlasHeight, shadowParameters.shadowAtlasHeight));
            shadowParameters.maxShadowDistance = Mathf.Max(0, EditorGUILayout.FloatField(styles.shadowsMaxShadowDistance, shadowParameters.maxShadowDistance));
            shadowParameters.directionalLightCascadeCount = EditorGUILayout.IntPopup(styles.shadowsDirectionalLightCascadeCount, shadowParameters.directionalLightCascadeCount, styles.shadowsCascadeCounts, styles.shadowsCascadeCountValues);

            EditorGUI.indentLevel++;
            for (int i = 0; i < shadowParameters.directionalLightCascadeCount - 1; i++)
            {
                shadowParameters.directionalLightCascades[i] = Mathf.Max(0, EditorGUILayout.FloatField(shadowParameters.directionalLightCascades[i]));
            }
            EditorGUI.indentLevel--;
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(renderLoop); // Repaint
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            var textureParameters = renderLoop.textureSettings;

            EditorGUILayout.LabelField(styles.textureSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            textureParameters.spotCookieSize = Mathf.NextPowerOfTwo(Mathf.Clamp(EditorGUILayout.IntField(styles.spotCookieSize, textureParameters.spotCookieSize), 16, 1024));
            textureParameters.pointCookieSize = Mathf.NextPowerOfTwo(Mathf.Clamp(EditorGUILayout.IntField(styles.pointCookieSize, textureParameters.pointCookieSize), 16, 1024));
            textureParameters.reflectionCubemapSize = Mathf.NextPowerOfTwo(Mathf.Clamp(EditorGUILayout.IntField(styles.reflectionCubemapSize, textureParameters.reflectionCubemapSize), 64, 1024));

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(renderLoop); // Repaint
            }
            EditorGUI.indentLevel--;


            EditorGUILayout.Space();

            if (renderLoop.tilePassLightLoop != null)
            {
                EditorGUILayout.LabelField(styles.tileLightLoopSettings);
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                renderLoop.tilePassLightLoop.debugViewTilesFlags = EditorGUILayout.MaskField("DebugView Tiles", renderLoop.tilePassLightLoop.debugViewTilesFlags, styles.tileLightLoopDebugTileFlagStrings);
                renderLoop.tilePassLightLoop.enableDirectIndirectSinglePass = EditorGUILayout.Toggle(styles.directIndirectSinglePass, renderLoop.tilePassLightLoop.enableDirectIndirectSinglePass);
                renderLoop.tilePassLightLoop.enableBigTilePrepass = EditorGUILayout.Toggle(styles.bigTilePrepass, renderLoop.tilePassLightLoop.enableBigTilePrepass);
                renderLoop.tilePassLightLoop.enableClustered = EditorGUILayout.Toggle(styles.clustered, renderLoop.tilePassLightLoop.enableClustered);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(renderLoop); // Repaint

                    // If something is chanage on tilePassLightLoop we need to force a OnValidate() OnHDRenderLoop, else change Rebuild() will not be call
                    renderLoop.OnValidate();
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}
