using System;
using UnityEditor;

//using EditorGUIUtility=UnityEditor.EditorGUIUtility;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(HDRenderPipeline))]
    public class HDRenderPipelineInspector : Editor
    {
        private class Styles
        {
            public readonly GUIContent debugParameters = new GUIContent("Debug Parameters");
            public readonly GUIContent debugViewMaterial = new GUIContent("DebugView Material", "Display various properties of Materials.");

            public readonly GUIContent displayOpaqueObjects = new GUIContent("Display Opaque Objects", "Toggle opaque objects rendering on and off.");
            public readonly GUIContent displayTransparentObjects = new GUIContent("Display Transparent Objects", "Toggle transparent objects rendering on and off.");

            public readonly GUIContent useForwardRenderingOnly = new GUIContent("Use Forward Rendering Only");
            public readonly GUIContent useDepthPrepass = new GUIContent("Use Depth Prepass");
            public readonly GUIContent useDistortion = new GUIContent("Use Distortion");        

            public bool isDebugViewMaterialInit = false;
            public GUIContent[] debugViewMaterialStrings = null;
            public int[] debugViewMaterialValues = null;

            public readonly GUIContent shadowSettings = new GUIContent("Shadow Settings");

            public readonly GUIContent shadowsEnabled = new GUIContent("Enabled");
            public readonly GUIContent shadowsAtlasWidth = new GUIContent("Atlas width");
            public readonly GUIContent shadowsAtlasHeight = new GUIContent("Atlas height");

            public readonly GUIContent tileLightLoopSettings = new GUIContent("Tile Light Loop Settings");
            public readonly string[] tileLightLoopDebugTileFlagStrings = new string[] { "Punctual Light", "Area Light", "Env Light"};
            public readonly GUIContent splitLightEvaluation = new GUIContent("Split light and reflection evaluation", "Toggle");
            public readonly GUIContent bigTilePrepass = new GUIContent("Enable big tile prepass", "Toggle");
            public readonly GUIContent clustered = new GUIContent("Enable clustered", "Toggle");
            public readonly GUIContent disableTileAndCluster = new GUIContent("Disable Tile/clustered", "Toggle");
            public readonly GUIContent disableDeferredShadingInCompute = new GUIContent("Disable deferred shading in compute", "Toggle");



            public readonly GUIContent textureSettings = new GUIContent("Texture Settings");

            public readonly GUIContent spotCookieSize = new GUIContent("Spot cookie size");
            public readonly GUIContent pointCookieSize = new GUIContent("Point cookie size");
            public readonly GUIContent reflectionCubemapSize = new GUIContent("Reflection cubemap size");              
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

        private void DebugParametersUI(HDRenderPipeline renderContext)
        {
            var debugParameters = renderContext.debugParameters;

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
            debugParameters.displayOpaqueObjects = EditorGUILayout.Toggle(styles.displayOpaqueObjects, debugParameters.displayOpaqueObjects);
            debugParameters.displayTransparentObjects = EditorGUILayout.Toggle(styles.displayTransparentObjects, debugParameters.displayTransparentObjects);
            debugParameters.useForwardRenderingOnly = EditorGUILayout.Toggle(styles.useForwardRenderingOnly, debugParameters.useForwardRenderingOnly);
            debugParameters.useDepthPrepass = EditorGUILayout.Toggle(styles.useDepthPrepass, debugParameters.useDepthPrepass);
            debugParameters.useDistortion = EditorGUILayout.Toggle(styles.useDistortion, debugParameters.useDistortion);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        private void ShadowParametersUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.Space();
            var shadowParameters = renderContext.shadowSettings;

            EditorGUILayout.LabelField(styles.shadowSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            shadowParameters.enabled = EditorGUILayout.Toggle(styles.shadowsEnabled, shadowParameters.enabled);
            shadowParameters.shadowAtlasWidth = Mathf.Max(0, EditorGUILayout.IntField(styles.shadowsAtlasWidth, shadowParameters.shadowAtlasWidth));
            shadowParameters.shadowAtlasHeight = Mathf.Max(0, EditorGUILayout.IntField(styles.shadowsAtlasHeight, shadowParameters.shadowAtlasHeight));

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        private void TextureParametersUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.Space();
            var textureParameters = renderContext.textureSettings;

            EditorGUILayout.LabelField(styles.textureSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            textureParameters.spotCookieSize = Mathf.NextPowerOfTwo(Mathf.Clamp(EditorGUILayout.IntField(styles.spotCookieSize, textureParameters.spotCookieSize), 16, 1024));
            textureParameters.pointCookieSize = Mathf.NextPowerOfTwo(Mathf.Clamp(EditorGUILayout.IntField(styles.pointCookieSize, textureParameters.pointCookieSize), 16, 1024));
            textureParameters.reflectionCubemapSize = Mathf.NextPowerOfTwo(Mathf.Clamp(EditorGUILayout.IntField(styles.reflectionCubemapSize, textureParameters.reflectionCubemapSize), 64, 1024));

            if (EditorGUI.EndChangeCheck())
            {
                renderContext.textureSettings = textureParameters;
                EditorUtility.SetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        private void TilePassUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.Space();

            // TODO: we should call a virtual method or something similar to setup the UI, inspector should not know about it
            TilePass.LightLoop tilePass = renderContext.lightLoop as TilePass.LightLoop;
            if (tilePass != null)
            {
                EditorGUILayout.LabelField(styles.tileLightLoopSettings);
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                tilePass.enableBigTilePrepass = EditorGUILayout.Toggle(styles.bigTilePrepass, tilePass.enableBigTilePrepass);
                tilePass.enableClustered = EditorGUILayout.Toggle(styles.clustered, tilePass.enableClustered);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(renderContext); // Repaint

                    // SetAssetDirty will tell renderloop to rebuild
                    renderContext.DestroyCreatedInstances();
                }

                EditorGUI.BeginChangeCheck();

                tilePass.debugViewTilesFlags = EditorGUILayout.MaskField("DebugView Tiles", tilePass.debugViewTilesFlags, styles.tileLightLoopDebugTileFlagStrings);
                tilePass.enableSplitLightEvaluation = EditorGUILayout.Toggle(styles.splitLightEvaluation, tilePass.enableSplitLightEvaluation);
                tilePass.disableTileAndCluster = EditorGUILayout.Toggle(styles.disableTileAndCluster, tilePass.disableTileAndCluster);
                tilePass.disableDeferredShadingInCompute = EditorGUILayout.Toggle(styles.disableDeferredShadingInCompute, tilePass.disableDeferredShadingInCompute);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(renderContext); // Repaint
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }
                EditorGUI.indentLevel--;
            }
        }

        public override void OnInspectorGUI()
        {
            var renderContext = target as HDRenderPipeline;

            if (!renderContext)
                return;

            DebugParametersUI(renderContext);
            ShadowParametersUI(renderContext);
            TextureParametersUI(renderContext);
            TilePassUI(renderContext);
        }
    }
}
