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
            public static GUIContent defaults = new GUIContent("Defaults");
            public static GUIContent defaultDiffuseMaterial = new GUIContent("Default Diffuse Material", "Material to use when creating objects");
            public static GUIContent defaultShader = new GUIContent("Default Shader", "Shader to use when creating materials");

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

            // Subsurface Scattering Settings
            public readonly GUIContent[] sssProfiles             = new GUIContent[SSSConstants.SSS_PROFILES_MAX] { new GUIContent("Profile #0"), new GUIContent("Profile #1"), new GUIContent("Profile #2"), new GUIContent("Profile #3"), new GUIContent("Profile #4"), new GUIContent("Profile #5"), new GUIContent("Profile #6"), new GUIContent("Profile #7") };
            public readonly GUIContent   sssNumProfiles          = new GUIContent("Number of profiles");

            // Tile pass Settings
            public readonly GUIContent tileLightLoopSettings = new GUIContent("Tile Light Loop Settings");
            public readonly string[] tileLightLoopDebugTileFlagStrings = new string[] { "Punctual Light", "Area Light", "Env Light"};
            public readonly GUIContent splitLightEvaluation = new GUIContent("Split light and reflection evaluation", "Toggle");
            public readonly GUIContent bigTilePrepass = new GUIContent("Enable big tile prepass", "Toggle");
            public readonly GUIContent clustered = new GUIContent("Enable clustered", "Toggle");
            public readonly GUIContent enableTileAndCluster = new GUIContent("Enable Tile/clustered", "Toggle");
            public readonly GUIContent enableComputeLightEvaluation = new GUIContent("Enable Compute Light Evaluation", "Toggle");

            // Sky Settings
            public readonly GUIContent skyParams = new GUIContent("Sky Settings");

            // Debug Display Settings
            public readonly GUIContent debugging = new GUIContent("Debugging");
            public readonly GUIContent debugOverlayRatio = new GUIContent("Overlay Ratio");

            // Material debug
            public readonly GUIContent materialDebugLabel = new GUIContent("Material Debug");
            public readonly GUIContent debugViewMaterial = new GUIContent("DebugView Material", "Display various properties of Materials.");
            public readonly GUIContent debugViewEngine = new GUIContent("DebugView Engine", "Display various properties of Materials.");
            public readonly GUIContent debugViewMaterialVarying = new GUIContent("DebugView Attributes", "Display varying input of Materials.");
            public readonly GUIContent debugViewMaterialGBuffer = new GUIContent("DebugView GBuffer", "Display GBuffer properties.");

            // Rendering Debug
            public readonly GUIContent renderingDebugSettings = new GUIContent("Rendering Debug");
            public readonly GUIContent displayOpaqueObjects = new GUIContent("Display Opaque Objects", "Toggle opaque objects rendering on and off.");
            public readonly GUIContent displayTransparentObjects = new GUIContent("Display Transparent Objects", "Toggle transparent objects rendering on and off.");
            public readonly GUIContent enableDistortion = new GUIContent("Enable Distortion");
            public readonly GUIContent enableSSS = new GUIContent("Enable Subsurface Scattering");

            // Lighting Debug
            public readonly GUIContent lightingDebugSettings = new GUIContent("Lighting Debug");
            public readonly GUIContent shadowDebugEnable = new GUIContent("Enable Shadows");
            public readonly GUIContent lightingVisualizationMode = new GUIContent("Lighting Debug Mode");
            public readonly GUIContent[] debugViewLightingStrings = { new GUIContent("None"), new GUIContent("Diffuse Lighting"), new GUIContent("Specular Lighting"), new GUIContent("Visualize Cascades") };
            public readonly int[] debugViewLightingValues = { (int)DebugLightingMode.None, (int)DebugLightingMode.DiffuseLighting, (int)DebugLightingMode.SpecularLighting, (int)DebugLightingMode.VisualizeCascade };
            public readonly GUIContent shadowDebugVisualizationMode = new GUIContent("Shadow Maps Debug Mode");
            public readonly GUIContent shadowDebugVisualizeShadowIndex = new GUIContent("Visualize Shadow Index");
            public readonly GUIContent lightingDebugOverrideSmoothness = new GUIContent("Override Smoothness");
            public readonly GUIContent lightingDebugOverrideSmoothnessValue = new GUIContent("Smoothness Value");
            public readonly GUIContent lightingDebugAlbedo = new GUIContent("Lighting Debug Albedo");
            public readonly GUIContent lightingDisplaySkyReflection = new GUIContent("Display Sky Reflection");
            public readonly GUIContent lightingDisplaySkyReflectionMipmap = new GUIContent("Reflection Mipmap");
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

        private SerializedProperty m_DefaultDiffuseMaterial;
        private SerializedProperty m_DefaultShader;

        // Display Debug
        SerializedProperty m_ShowMaterialDebug = null;
        SerializedProperty m_ShowLightingDebug = null;
        SerializedProperty m_ShowRenderingDebug = null;
        SerializedProperty m_DebugOverlayRatio = null;

        // Rendering Debug
        SerializedProperty m_DisplayOpaqueObjects = null;
        SerializedProperty m_DisplayTransparentObjects = null;
        SerializedProperty m_EnableDistortion = null;
        SerializedProperty m_EnableSSS = null;

        // Lighting debug
        SerializedProperty m_DebugShadowEnabled = null;
        SerializedProperty m_ShadowDebugMode = null;
        SerializedProperty m_ShadowDebugShadowMapIndex = null;
        SerializedProperty m_LightingDebugOverrideSmoothness = null;
        SerializedProperty m_LightingDebugOverrideSmoothnessValue = null;
        SerializedProperty m_LightingDebugAlbedo = null;
        SerializedProperty m_LightingDebugDisplaySkyReflection = null;
        SerializedProperty m_LightingDebugDisplaySkyReflectionMipmap = null;

        // Rendering Settings
        SerializedProperty m_RenderingUseForwardOnly = null;
        SerializedProperty m_RenderingUseDepthPrepass = null;

        // Subsurface Scattering Settings
        SerializedProperty m_Profiles = null;
        SerializedProperty m_NumProfiles = null;

        private void InitializeProperties()
        {
            m_DefaultDiffuseMaterial = serializedObject.FindProperty("m_DefaultDiffuseMaterial");
            m_DefaultShader = serializedObject.FindProperty("m_DefaultShader");

            // DebugDisplay debug
            m_DebugOverlayRatio = FindProperty(x => x.debugDisplaySettings.debugOverlayRatio);
            m_ShowLightingDebug = FindProperty(x => x.debugDisplaySettings.displayLightingDebug);
            m_ShowRenderingDebug = FindProperty(x => x.debugDisplaySettings.displayRenderingDebug);
            m_ShowMaterialDebug = FindProperty(x => x.debugDisplaySettings.displayMaterialDebug);

            // Rendering debug
            m_DisplayOpaqueObjects = FindProperty(x => x.debugDisplaySettings.renderingDebugSettings.displayOpaqueObjects);
            m_DisplayTransparentObjects = FindProperty(x => x.debugDisplaySettings.renderingDebugSettings.displayTransparentObjects);
            m_EnableDistortion = FindProperty(x => x.debugDisplaySettings.renderingDebugSettings.enableDistortion);
            m_EnableSSS = FindProperty(x => x.debugDisplaySettings.renderingDebugSettings.enableSSS);

            // Lighting debug
            m_DebugShadowEnabled = FindProperty(x => x.debugDisplaySettings.lightingDebugSettings.enableShadows);
            m_ShadowDebugMode = FindProperty(x => x.debugDisplaySettings.lightingDebugSettings.shadowDebugMode);
            m_ShadowDebugShadowMapIndex = FindProperty(x => x.debugDisplaySettings.lightingDebugSettings.shadowMapIndex);
            m_LightingDebugOverrideSmoothness = FindProperty(x => x.debugDisplaySettings.lightingDebugSettings.overrideSmoothness);
            m_LightingDebugOverrideSmoothnessValue = FindProperty(x => x.debugDisplaySettings.lightingDebugSettings.overrideSmoothnessValue);
            m_LightingDebugAlbedo = FindProperty(x => x.debugDisplaySettings.lightingDebugSettings.debugLightingAlbedo);
            m_LightingDebugDisplaySkyReflection = FindProperty(x => x.debugDisplaySettings.lightingDebugSettings.displaySkyReflection);
            m_LightingDebugDisplaySkyReflectionMipmap = FindProperty(x => x.debugDisplaySettings.lightingDebugSettings.skyReflectionMipmap);

            // Rendering settings
            m_RenderingUseForwardOnly = FindProperty(x => x.renderingSettings.useForwardRenderingOnly);
            m_RenderingUseDepthPrepass = FindProperty(x => x.renderingSettings.useDepthPrepass);

            // Subsurface Scattering Settings
            m_Profiles    = FindProperty(x => x.sssSettings.profiles);
            m_NumProfiles = m_Profiles.FindPropertyRelative("Array.size");
        }

        SerializedProperty FindProperty<TValue>(Expression<Func<HDRenderPipeline, TValue>> expr)
        {
            var path = Utilities.GetFieldPath(expr);
            return serializedObject.FindProperty(path);
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

            // Debug Display settings
            EditorGUI.indentLevel++;
            m_DebugOverlayRatio.floatValue = EditorGUILayout.Slider(styles.debugOverlayRatio, m_DebugOverlayRatio.floatValue, 0.1f, 1.0f);
            EditorGUILayout.Space();

            RenderingDebugSettingsUI(renderContext);
            MaterialDebugSettingsUI(renderContext);
            LightingDebugSettingsUI(renderContext, renderpipelineInstance);

            EditorGUILayout.Space();

            EditorGUI.indentLevel--;
        }

        private void MaterialDebugSettingsUI(HDRenderPipeline renderContext)
        {
            HDRenderPipeline hdPipe = target as HDRenderPipeline;

            m_ShowMaterialDebug.boolValue = EditorGUILayout.Foldout(m_ShowMaterialDebug.boolValue, styles.materialDebugLabel);
            if (!m_ShowMaterialDebug.boolValue)
                return;

            EditorGUI.indentLevel++;

            bool dirty = false;
            EditorGUI.BeginChangeCheck();
            int value = EditorGUILayout.IntPopup(styles.debugViewMaterial, hdPipe.debugDisplaySettings.materialDebugSettings.debugViewMaterial, DebugDisplaySettings.debugViewMaterialStrings, DebugDisplaySettings.debugViewMaterialValues);
            if (EditorGUI.EndChangeCheck())
            {
                hdPipe.debugDisplaySettings.SetDebugViewMaterial(value);
                dirty = true;
            }

            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.IntPopup(styles.debugViewEngine, hdPipe.debugDisplaySettings.materialDebugSettings.debugViewEngine, DebugDisplaySettings.debugViewEngineStrings, DebugDisplaySettings.debugViewEngineValues);
            if (EditorGUI.EndChangeCheck())
            {
                hdPipe.debugDisplaySettings.SetDebugViewEngine(value);
                dirty = true;
            }

            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.IntPopup(styles.debugViewMaterialVarying, (int)hdPipe.debugDisplaySettings.materialDebugSettings.debugViewVarying, DebugDisplaySettings.debugViewMaterialVaryingStrings, DebugDisplaySettings.debugViewMaterialVaryingValues);
            if (EditorGUI.EndChangeCheck())
            {
                hdPipe.debugDisplaySettings.SetDebugViewVarying((Attributes.DebugViewVarying)value);
                dirty = true;
            }

            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.IntPopup(styles.debugViewMaterialGBuffer, (int)hdPipe.debugDisplaySettings.materialDebugSettings.debugViewGBuffer, DebugDisplaySettings.debugViewMaterialGBufferStrings, DebugDisplaySettings.debugViewMaterialGBufferValues);
            if (EditorGUI.EndChangeCheck())
            {
                hdPipe.debugDisplaySettings.SetDebugViewGBuffer(value);
                dirty = true;
            }

            if(dirty)
                HackSetDirty(renderContext); // Repaint

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
            EditorGUILayout.PropertyField(m_EnableSSS, styles.enableSSS);
            EditorGUI.indentLevel--;
        }

        private void SssSettingsUI(HDRenderPipeline pipe)
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(styles.sssSettings);
            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_NumProfiles, styles.sssNumProfiles);

            for (int i = 0, n = Math.Min(m_Profiles.arraySize, SSSConstants.SSS_PROFILES_MAX); i < n; i++)
            {
                SerializedProperty profile = m_Profiles.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(profile, styles.sssProfiles[i]);
            }

            EditorGUI.indentLevel--;
        }

        private void LightingDebugSettingsUI(HDRenderPipeline renderContext, HDRenderPipelineInstance renderpipelineInstance)
        {
            m_ShowLightingDebug.boolValue = EditorGUILayout.Foldout(m_ShowLightingDebug.boolValue, styles.lightingDebugSettings);
            if (!m_ShowLightingDebug.boolValue)
                return;

            HDRenderPipeline hdPipe = target as HDRenderPipeline;

            bool dirty = false;
            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_DebugShadowEnabled, styles.shadowDebugEnable);
            EditorGUILayout.PropertyField(m_ShadowDebugMode, styles.shadowDebugVisualizationMode);
            if (!m_ShadowDebugMode.hasMultipleDifferentValues)
            {
                if ((ShadowMapDebugMode)m_ShadowDebugMode.intValue == ShadowMapDebugMode.VisualizeShadowMap)
                {
                    EditorGUILayout.IntSlider(m_ShadowDebugShadowMapIndex, 0, renderpipelineInstance.GetCurrentShadowCount() - 1, styles.shadowDebugVisualizeShadowIndex);
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                dirty = true;
            }

            EditorGUI.BeginChangeCheck();
            int value = EditorGUILayout.IntPopup(styles.lightingVisualizationMode, (int)hdPipe.debugDisplaySettings.lightingDebugSettings.debugLightingMode, styles.debugViewLightingStrings, styles.debugViewLightingValues);
            if (EditorGUI.EndChangeCheck())
            {
                hdPipe.debugDisplaySettings.SetDebugLightingMode((DebugLightingMode)value);
                dirty = true;
            }

            EditorGUI.BeginChangeCheck();
            if (hdPipe.debugDisplaySettings.GetDebugLightingMode() == DebugLightingMode.DiffuseLighting)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_LightingDebugAlbedo, styles.lightingDebugAlbedo);
                EditorGUI.indentLevel--;
            }

            if (hdPipe.debugDisplaySettings.GetDebugLightingMode() == DebugLightingMode.SpecularLighting)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_LightingDebugOverrideSmoothness, styles.lightingDebugOverrideSmoothness);
                if (!m_LightingDebugOverrideSmoothness.hasMultipleDifferentValues && m_LightingDebugOverrideSmoothness.boolValue == true)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_LightingDebugOverrideSmoothnessValue, styles.lightingDebugOverrideSmoothnessValue);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(m_LightingDebugDisplaySkyReflection, styles.lightingDisplaySkyReflection);
            if (!m_LightingDebugDisplaySkyReflection.hasMultipleDifferentValues && m_LightingDebugDisplaySkyReflection.boolValue == true)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_LightingDebugDisplaySkyReflectionMipmap, styles.lightingDisplaySkyReflectionMipmap);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                dirty = true;
            }

            if(dirty)
                HackSetDirty(renderContext);
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
                tilePass.enableTileAndCluster = EditorGUILayout.Toggle(styles.enableTileAndCluster, tilePass.enableTileAndCluster);
                tilePass.enableComputeLightEvaluation = EditorGUILayout.Toggle(styles.enableComputeLightEvaluation, tilePass.enableComputeLightEvaluation);

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

            EditorGUILayout.LabelField(Styles.defaults, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_DefaultDiffuseMaterial, Styles.defaultDiffuseMaterial);
            EditorGUILayout.PropertyField(m_DefaultShader, Styles.defaultShader);
            EditorGUI.indentLevel--;

            DebuggingUI(renderContext, renderpipelineInstance);
            SettingsUI(renderContext);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
