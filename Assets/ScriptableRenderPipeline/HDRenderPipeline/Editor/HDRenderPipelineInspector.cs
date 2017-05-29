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

            SettingsUI(renderContext);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
