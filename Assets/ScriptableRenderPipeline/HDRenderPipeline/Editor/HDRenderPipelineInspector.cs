using System;
using System.Reflection;
using System.Linq.Expressions;
using UnityEditor;

//using EditorGUIUtility=UnityEditor.EditorGUIUtility;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(HDRenderPipeline))]
    public class HDRenderPipelineInspector : Editor
    {
        private class Styles
        {
            public readonly GUIContent settingsLabel = new GUIContent("Settings");

            // Rendering Settings
            public readonly GUIContent renderingSettingsLabel = new GUIContent("Rendering Settings");
            public readonly GUIContent useForwardRenderingOnly = new GUIContent("Use Forward Rendering Only");
            public readonly GUIContent useDepthPrepass = new GUIContent("Use Depth Prepass");

            // Texture Settings
            public readonly GUIContent textureSettings = new GUIContent("Texture Settings");
            public readonly GUIContent spotCookieSize = new GUIContent("Spot cookie size");
            public readonly GUIContent pointCookieSize = new GUIContent("Point cookie size");
            public readonly GUIContent reflectionCubemapSize = new GUIContent("Reflection cubemap size");

            public readonly GUIContent sssSettings = new GUIContent("Subsurface Scattering Settings");

            // Shadow Settings
            public readonly GUIContent shadowSettings = new GUIContent("Shadow Settings");
            public readonly GUIContent shadowsAtlasWidth = new GUIContent("Atlas width");
            public readonly GUIContent shadowsAtlasHeight = new GUIContent("Atlas height");

            // Tile pass Settings
            public readonly GUIContent tileLightLoopSettings = new GUIContent("Tile Light Loop Settings");
            public readonly string[] tileLightLoopDebugTileFlagStrings = new string[] { "Punctual Light", "Area Light", "Env Light"};
            public readonly GUIContent splitLightEvaluation = new GUIContent("Split light and reflection evaluation", "Toggle");
            public readonly GUIContent bigTilePrepass = new GUIContent("Enable big tile prepass", "Toggle");
            public readonly GUIContent clustered = new GUIContent("Enable clustered", "Toggle");
            public readonly GUIContent disableTileAndCluster = new GUIContent("Disable Tile/clustered", "Toggle");
            public readonly GUIContent disableDeferredShadingInCompute = new GUIContent("Disable deferred shading in compute", "Toggle");

            // Sky Settings
            public readonly GUIContent skyParams = new GUIContent("Sky Settings");

            // Global debug Settings
            public readonly GUIContent debugging = new GUIContent("Debugging");
            public readonly GUIContent debugOverlayRatio = new GUIContent("Overlay Ratio");

            // Material debug
            public readonly GUIContent materialDebugLabel = new GUIContent("Material Debug");
            public readonly GUIContent debugViewMaterial = new GUIContent("DebugView Material", "Display various properties of Materials.");
            public bool isDebugViewMaterialInit = false;
            public GUIContent[] debugViewMaterialStrings = null;
            public int[] debugViewMaterialValues = null;

            // Rendering Debug
            public readonly GUIContent renderingDebugSettings = new GUIContent("Rendering Debug");
            public readonly GUIContent displayOpaqueObjects = new GUIContent("Display Opaque Objects", "Toggle opaque objects rendering on and off.");
            public readonly GUIContent displayTransparentObjects = new GUIContent("Display Transparent Objects", "Toggle transparent objects rendering on and off.");
            public readonly GUIContent enableDistortion = new GUIContent("Enable Distortion");

            // Lighting Debug
            public readonly GUIContent lightingDebugSettings = new GUIContent("Lighting Debug");
            public readonly GUIContent shadowDebugEnable = new GUIContent("Enable Shadows");
            public readonly GUIContent shadowDebugVisualizationMode = new GUIContent("Shadow Debug Mode");
            public readonly GUIContent shadowDebugVisualizeShadowIndex = new GUIContent("Visualize Shadow Index");
            public readonly GUIContent lightingDebugMode = new GUIContent("Lighting Debug Mode");
            public readonly GUIContent lightingDebugOverrideSmoothness = new GUIContent("Override Smoothness");
            public readonly GUIContent lightingDebugOverrideSmoothnessValue = new GUIContent("Smoothness Value");
            public readonly GUIContent lightingDebugAlbedo = new GUIContent("Lighting Debug Albedo");
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

        // Global debug
        SerializedProperty m_ShowMaterialDebug = null;
        SerializedProperty m_ShowLightingDebug = null;
        SerializedProperty m_ShowRenderingDebug = null;
        SerializedProperty m_DebugOverlayRatio = null;

        // Rendering Debug
        SerializedProperty m_MaterialDebugMode = null;

        // Rendering Debug
        SerializedProperty m_DisplayOpaqueObjects = null;
        SerializedProperty m_DisplayTransparentObjects = null;
        SerializedProperty m_EnableDistortion = null;

        // Lighting debug
        SerializedProperty m_DebugShadowEnabled = null;
        SerializedProperty m_ShadowDebugMode = null;
        SerializedProperty m_ShadowDebugShadowMapIndex = null;
        SerializedProperty m_LightingDebugMode = null;
        SerializedProperty m_LightingDebugOverrideSmoothness = null;
        SerializedProperty m_LightingDebugOverrideSmoothnessValue = null;
        SerializedProperty m_LightingDebugAlbedo = null;

        // Rendering Settings
        SerializedProperty m_RenderingUseForwardOnly = null;
        SerializedProperty m_RenderingUseDepthPrepass = null;

        private void InitializeProperties()
        {
            // Global debug
            m_DebugOverlayRatio = FindProperty(x => x.globalDebugSettings.debugOverlayRatio);
            m_ShowLightingDebug = FindProperty(x => x.globalDebugSettings.displayLightingDebug);
            m_ShowRenderingDebug = FindProperty(x => x.globalDebugSettings.displayRenderingDebug);
            m_ShowMaterialDebug = FindProperty(x => x.globalDebugSettings.displayMaterialDebug);

            // Material debug
            m_MaterialDebugMode = FindProperty(x => x.globalDebugSettings.materialDebugSettings.debugViewMaterial);

            // Rendering debug
            m_DisplayOpaqueObjects = FindProperty(x => x.globalDebugSettings.renderingDebugSettings.displayOpaqueObjects);
            m_DisplayTransparentObjects = FindProperty(x => x.globalDebugSettings.renderingDebugSettings.displayTransparentObjects);
            m_EnableDistortion = FindProperty(x => x.globalDebugSettings.renderingDebugSettings.enableDistortion);

            // Lighting debug
            m_DebugShadowEnabled = FindProperty(x => x.globalDebugSettings.lightingDebugSettings.enableShadows);
            m_ShadowDebugMode = FindProperty(x => x.globalDebugSettings.lightingDebugSettings.shadowDebugMode);
            m_ShadowDebugShadowMapIndex = FindProperty(x => x.globalDebugSettings.lightingDebugSettings.shadowMapIndex);
            m_LightingDebugMode = FindProperty(x => x.globalDebugSettings.lightingDebugSettings.lightingDebugMode);
            m_LightingDebugOverrideSmoothness = FindProperty(x => x.globalDebugSettings.lightingDebugSettings.overrideSmoothness);
            m_LightingDebugOverrideSmoothnessValue = FindProperty(x => x.globalDebugSettings.lightingDebugSettings.overrideSmoothnessValue);
            m_LightingDebugAlbedo = FindProperty(x => x.globalDebugSettings.lightingDebugSettings.debugLightingAlbedo);

            // Rendering settings
            m_RenderingUseForwardOnly = FindProperty(x => x.renderingSettings.useForwardRenderingOnly);
            m_RenderingUseDepthPrepass = FindProperty(x => x.renderingSettings.useDepthPrepass);

        }

        SerializedProperty FindProperty<TValue>(Expression<Func<HDRenderPipeline, TValue>> expr)
        {
            var path = Utilities.GetFieldPath(expr);
            return serializedObject.FindProperty(path);
        }

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

        static void HackSetDirty(RenderPipelineAsset asset)
        {
            EditorUtility.SetDirty(asset);
            var method = typeof(RenderPipelineAsset).GetMethod("OnValidate", BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
                method.Invoke(asset, new object[0]);
        }

        private void DebuggingUI(HDRenderPipeline renderContext, HDRenderPipelineInstance renderpipelineInstance)
        {
            EditorGUILayout.LabelField(styles.debugging);

            // Global debug settings
            EditorGUI.indentLevel++;
            m_DebugOverlayRatio.floatValue = EditorGUILayout.Slider(styles.debugOverlayRatio, m_DebugOverlayRatio.floatValue, 0.1f, 1.0f);
            EditorGUILayout.Space();

            MaterialDebugSettingsUI(renderContext);
            RenderingDebugSettingsUI(renderContext);
            LightingDebugSettingsUI(renderContext, renderpipelineInstance);

            EditorGUILayout.Space();

            EditorGUI.indentLevel--;
        }


        private void MaterialDebugSettingsUI(HDRenderPipeline renderContext)
        {
            m_ShowMaterialDebug.boolValue = EditorGUILayout.Foldout(m_ShowMaterialDebug.boolValue, styles.materialDebugLabel);
            if (!m_ShowMaterialDebug.boolValue)
                return;

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

            EditorGUI.showMixedValue = m_MaterialDebugMode.hasMultipleDifferentValues;
            m_MaterialDebugMode.intValue = EditorGUILayout.IntPopup(styles.debugViewMaterial, m_MaterialDebugMode.intValue, styles.debugViewMaterialStrings, styles.debugViewMaterialValues);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        private void RenderingDebugSettingsUI(HDRenderPipeline renderContext)
        {
            m_ShowRenderingDebug.boolValue = EditorGUILayout.Foldout(m_ShowRenderingDebug.boolValue, styles.renderingDebugSettings);
            if (!m_ShowRenderingDebug.boolValue)
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_DisplayOpaqueObjects, styles.displayOpaqueObjects);
            EditorGUILayout.PropertyField(m_DisplayTransparentObjects, styles.displayTransparentObjects);
            EditorGUILayout.PropertyField(m_EnableDistortion, styles.enableDistortion);
            EditorGUI.indentLevel--;
        }

        private void SssSettingsUI(HDRenderPipeline pipe)
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(styles.sssSettings);
            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;
            pipe.localSssParameters = (SubsurfaceScatteringParameters) EditorGUILayout.ObjectField(new GUIContent("Subsurface Scattering Parameters"), pipe.localSssParameters, typeof(SubsurfaceScatteringParameters), false);
            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(pipe); // Repaint
            }
        }

        private void LightingDebugSettingsUI(HDRenderPipeline renderContext, HDRenderPipelineInstance renderpipelineInstance)
        {
            m_ShowLightingDebug.boolValue = EditorGUILayout.Foldout(m_ShowLightingDebug.boolValue, styles.lightingDebugSettings);
            if (!m_ShowLightingDebug.boolValue)
                return;

            EditorGUI.BeginChangeCheck();

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_DebugShadowEnabled, styles.shadowDebugEnable);
            EditorGUILayout.PropertyField(m_ShadowDebugMode, styles.shadowDebugVisualizationMode);
            if (!m_ShadowDebugMode.hasMultipleDifferentValues)
            {
                if ((ShadowDebugMode)m_ShadowDebugMode.intValue == ShadowDebugMode.VisualizeShadowMap)
                {
                    EditorGUILayout.IntSlider(m_ShadowDebugShadowMapIndex, 0, renderpipelineInstance.GetCurrentShadowCount() - 1, styles.shadowDebugVisualizeShadowIndex);
                }
            }
            EditorGUILayout.PropertyField(m_LightingDebugMode, styles.lightingDebugMode);
            if (!m_LightingDebugMode.hasMultipleDifferentValues)
            {
                if ((LightingDebugMode)m_LightingDebugMode.intValue != LightingDebugMode.None)
            {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_LightingDebugAlbedo, styles.lightingDebugAlbedo);
                    EditorGUILayout.PropertyField(m_LightingDebugOverrideSmoothness, styles.lightingDebugOverrideSmoothness);
                    if (!m_LightingDebugOverrideSmoothness.hasMultipleDifferentValues && m_LightingDebugOverrideSmoothness.boolValue == true)
                {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(m_LightingDebugOverrideSmoothnessValue, styles.lightingDebugOverrideSmoothnessValue);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(renderContext);
            }
        }

        private void SettingsUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.LabelField(styles.settingsLabel);
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();

            renderContext.lightLoopProducer = (LightLoopProducer)EditorGUILayout.ObjectField(new GUIContent("Light Loop"), renderContext.lightLoopProducer, typeof(LightLoopProducer), false);

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(renderContext); // Repaint
            }

            SssSettingsUI(renderContext);
            ShadowSettingsUI(renderContext);
            TextureSettingsUI(renderContext);
            RendereringSettingsUI(renderContext);
            //TilePassUI(renderContext);

            EditorGUI.indentLevel--;
        }

        private void ShadowSettingsUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.Space();
            var shadowSettings = renderContext.shadowSettings;

            EditorGUILayout.LabelField(styles.shadowSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            shadowSettings.shadowAtlasWidth = Mathf.Max(0, EditorGUILayout.IntField(styles.shadowsAtlasWidth, shadowSettings.shadowAtlasWidth));
            shadowSettings.shadowAtlasHeight = Mathf.Max(0, EditorGUILayout.IntField(styles.shadowsAtlasHeight, shadowSettings.shadowAtlasHeight));

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        private void RendereringSettingsUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(styles.renderingSettingsLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_RenderingUseDepthPrepass, styles.useDepthPrepass);
            EditorGUILayout.PropertyField(m_RenderingUseForwardOnly, styles.useForwardRenderingOnly);
            EditorGUI.indentLevel--;
        }

        private void TextureSettingsUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.Space();
            var textureSettings = renderContext.textureSettings;

            EditorGUILayout.LabelField(styles.textureSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            textureSettings.spotCookieSize = Mathf.NextPowerOfTwo(Mathf.Clamp(EditorGUILayout.IntField(styles.spotCookieSize, textureSettings.spotCookieSize), 16, 1024));
            textureSettings.pointCookieSize = Mathf.NextPowerOfTwo(Mathf.Clamp(EditorGUILayout.IntField(styles.pointCookieSize, textureSettings.pointCookieSize), 16, 1024));
            textureSettings.reflectionCubemapSize = Mathf.NextPowerOfTwo(Mathf.Clamp(EditorGUILayout.IntField(styles.reflectionCubemapSize, textureSettings.reflectionCubemapSize), 64, 1024));

            if (EditorGUI.EndChangeCheck())
            {
                renderContext.textureSettings = textureSettings;
                HackSetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        /*  private void TilePassUI(HDRenderPipeline renderContext)
        {
            EditorGUILayout.Space();

            // TODO: we should call a virtual method or something similar to setup the UI, inspector should not know about it
            var tilePass = renderContext.tileSettings;
            if (tilePass != null)
            {
                EditorGUILayout.LabelField(styles.tileLightLoopSettings);
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                tilePass.enableBigTilePrepass = EditorGUILayout.Toggle(styles.bigTilePrepass, tilePass.enableBigTilePrepass);
                tilePass.enableClustered = EditorGUILayout.Toggle(styles.clustered, tilePass.enableClustered);

                if (EditorGUI.EndChangeCheck())
                {
                   HackSetDirty(renderContext); // Repaint

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
                   HackSetDirty(renderContext); // Repaint
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }
                EditorGUI.indentLevel--;
            }
        }*/

        public void OnEnable()
        {
            InitializeProperties();
        }

        public override void OnInspectorGUI()
        {
            var renderContext = target as HDRenderPipeline;
            HDRenderPipelineInstance renderpipelineInstance = UnityEngine.Experimental.Rendering.RenderPipelineManager.currentPipeline as HDRenderPipelineInstance;

            if (!renderContext || renderpipelineInstance == null)
                return;

            serializedObject.Update();

            DebuggingUI(renderContext, renderpipelineInstance);
            SettingsUI(renderContext);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
