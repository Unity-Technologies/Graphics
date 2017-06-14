using System;
using System.Reflection;
using System.Linq.Expressions;
using UnityEditor;

//using EditorGUIUtility=UnityEditor.EditorGUIUtility;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(HDRenderPipelineAsset))]
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
            public readonly GUIContent[] sssProfiles    = new GUIContent[SssConstants.SSS_N_PROFILES - 1] { new GUIContent("Profile #1"), new GUIContent("Profile #2"), new GUIContent("Profile #3"), new GUIContent("Profile #4"), new GUIContent("Profile #5"),
                                                                                                            new GUIContent("Profile #6"), new GUIContent("Profile #7")/*, new GUIContent("Profile #8"), new GUIContent("Profile #9"), new GUIContent("Profile #10"),
                                                                                                            new GUIContent("Profile #11"), new GUIContent("Profile #12"), new GUIContent("Profile #13"), new GUIContent("Profile #14"), new GUIContent("Profile #15")*/ };
            public readonly GUIContent   sssNumProfiles = new GUIContent("Number of profiles");

            // Tile pass Settings
            public readonly GUIContent tileLightLoopSettings = new GUIContent("Tile Light Loop Settings");
            public readonly GUIContent enableTileAndCluster = new GUIContent("Enable tile/clustered", "Toggle");
            public readonly GUIContent enableSplitLightEvaluation = new GUIContent("Split light and reflection evaluation", "Toggle");
            public readonly GUIContent enableComputeLightEvaluation = new GUIContent("Enable Compute Light Evaluation", "Toggle");
            public readonly GUIContent enableComputeLightVariants = new GUIContent("Enable Compute Light Variants", "Toggle");
            public readonly GUIContent enableComputeMaterialVariants = new GUIContent("Enable Compute Material Variants", "Toggle");
            public readonly GUIContent enableClustered = new GUIContent("Enable clustered", "Toggle");
            public readonly GUIContent enableFptlForOpaqueWhenClustered = new GUIContent("Enable Fptl For Opaque When Clustered", "Toggle");
            public readonly GUIContent enableBigTilePrepass = new GUIContent("Enable big tile prepass", "Toggle");
            public readonly GUIContent tileDebugByCategory = new GUIContent("Enable Debug By Category", "Toggle");

            // Sky Settings
            public readonly GUIContent skyParams = new GUIContent("Sky Settings");
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

        // TilePass settings
        SerializedProperty m_enableTileAndCluster;
        SerializedProperty m_enableSplitLightEvaluation;
        SerializedProperty m_enableComputeLightEvaluation;
        SerializedProperty m_enableComputeLightVariants;
        SerializedProperty m_enableComputeMaterialVariants;
        SerializedProperty m_enableClustered;
        SerializedProperty m_enableFptlForOpaqueWhenClustered;
        SerializedProperty m_enableBigTilePrepass;
        SerializedProperty m_tileDebugByCategory;

        // Rendering Settings
        SerializedProperty m_RenderingUseForwardOnly = null;
        SerializedProperty m_RenderingUseDepthPrepass = null;

        // Subsurface Scattering Settings
        // Old SSS Model >>>
        SerializedProperty m_UseDisneySSS = null;
        // <<< Old SSS Model
        SerializedProperty m_Profiles = null;
        SerializedProperty m_NumProfiles = null;

        // Shadow Settings
        SerializedProperty m_ShadowAtlasWidth = null;
        SerializedProperty m_ShadowAtlasHeight = null;

        // Texture Settings
        SerializedProperty m_SpotCookieSize = null;
        SerializedProperty m_PointCookieSize = null;
        SerializedProperty m_ReflectionCubemapSize = null;

        private void InitializeProperties()
        {
            m_DefaultDiffuseMaterial = serializedObject.FindProperty("m_DefaultDiffuseMaterial");
            m_DefaultShader = serializedObject.FindProperty("m_DefaultShader");

            // Following way of getting property allow to handle change of properties name with serializations

            // Tile settings
            m_enableTileAndCluster = FindProperty(x => x.tileSettings.enableTileAndCluster);
            m_enableSplitLightEvaluation = FindProperty(x => x.tileSettings.enableSplitLightEvaluation);
            m_enableComputeLightEvaluation = FindProperty(x => x.tileSettings.enableComputeLightEvaluation);
            m_enableComputeLightVariants = FindProperty(x => x.tileSettings.enableComputeLightVariants);
            m_enableComputeMaterialVariants = FindProperty(x => x.tileSettings.enableComputeMaterialVariants);
            m_enableClustered = FindProperty(x => x.tileSettings.enableClustered);
            m_enableFptlForOpaqueWhenClustered = FindProperty(x => x.tileSettings.enableFptlForOpaqueWhenClustered);
            m_enableBigTilePrepass = FindProperty(x => x.tileSettings.enableBigTilePrepass);
            m_tileDebugByCategory = FindProperty(x => x.tileSettings.tileDebugByCategory);

            // Shadow settings
            m_ShadowAtlasWidth = FindProperty(x => x.shadowInitParams.shadowAtlasWidth);
            m_ShadowAtlasHeight = FindProperty(x => x.shadowInitParams.shadowAtlasHeight);

            // Texture settings
            m_SpotCookieSize = FindProperty(x => x.textureSettings.spotCookieSize);
            m_PointCookieSize = FindProperty(x => x.textureSettings.pointCookieSize);
            m_ReflectionCubemapSize = FindProperty(x => x.textureSettings.reflectionCubemapSize);

            // Rendering settings
            m_RenderingUseForwardOnly = FindProperty(x => x.renderingSettings.useForwardRenderingOnly);
            m_RenderingUseDepthPrepass = FindProperty(x => x.renderingSettings.useDepthPrepass);

            // Subsurface Scattering Settings
            // Old SSS Model >>>
            m_UseDisneySSS = FindProperty(x => x.sssSettings.useDisneySSS);
            // <<< Old SSS Model
            m_Profiles    = FindProperty(x => x.sssSettings.profiles);
            m_NumProfiles = m_Profiles.FindPropertyRelative("Array.size");
        }

        SerializedProperty FindProperty<TValue>(Expression<Func<HDRenderPipelineAsset, TValue>> expr)
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

        private void TileSettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(styles.tileLightLoopSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_enableTileAndCluster, styles.enableTileAndCluster);
            EditorGUILayout.PropertyField(m_enableSplitLightEvaluation, styles.enableSplitLightEvaluation);
            EditorGUILayout.PropertyField(m_enableComputeLightEvaluation, styles.enableComputeLightEvaluation);
            EditorGUILayout.PropertyField(m_enableComputeLightVariants, styles.enableComputeLightVariants);
            EditorGUILayout.PropertyField(m_enableComputeMaterialVariants, styles.enableComputeMaterialVariants);
            EditorGUILayout.PropertyField(m_enableClustered, styles.enableClustered);
            EditorGUILayout.PropertyField(m_enableFptlForOpaqueWhenClustered, styles.enableFptlForOpaqueWhenClustered);
            EditorGUILayout.PropertyField(m_enableBigTilePrepass, styles.enableBigTilePrepass);
            EditorGUILayout.PropertyField(m_tileDebugByCategory, styles.tileDebugByCategory);

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        private void SssSettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(styles.sssSettings);
            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();

            // Old SSS Model >>>
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_UseDisneySSS);
            if (EditorGUI.EndChangeCheck())
            {
                HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
                hdPipeline.CreateSssMaterials(m_UseDisneySSS.boolValue);
            }
            // <<< Old SSS Model
            EditorGUILayout.PropertyField(m_NumProfiles, styles.sssNumProfiles);

            for (int i = 0, n = m_Profiles.arraySize; i < n; i++)
            {
                SerializedProperty profile = m_Profiles.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(profile, styles.sssProfiles[i]);
            }

            EditorGUI.indentLevel--;
        }

        private void SettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.LabelField(styles.settingsLabel);
            EditorGUI.indentLevel++;

            SssSettingsUI(renderContext);
            ShadowSettingsUI(renderContext);
            TextureSettingsUI(renderContext);
            RendereringSettingsUI(renderContext);
            TileSettingsUI(renderContext);

            EditorGUI.indentLevel--;
        }

        private void ShadowSettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(styles.shadowSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_ShadowAtlasWidth, styles.shadowsAtlasWidth);
            EditorGUILayout.PropertyField(m_ShadowAtlasHeight, styles.shadowsAtlasHeight);

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        private void RendereringSettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(styles.renderingSettingsLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_RenderingUseDepthPrepass, styles.useDepthPrepass);
            EditorGUILayout.PropertyField(m_RenderingUseForwardOnly, styles.useForwardRenderingOnly);
            EditorGUI.indentLevel--;
        }

        private void TextureSettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(styles.textureSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_SpotCookieSize, styles.spotCookieSize);
            EditorGUILayout.PropertyField(m_PointCookieSize, styles.pointCookieSize);
            EditorGUILayout.PropertyField(m_ReflectionCubemapSize, styles.reflectionCubemapSize);

            if (EditorGUI.EndChangeCheck())
            {
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
            var renderContext = target as HDRenderPipelineAsset;
            HDRenderPipeline renderpipeline = UnityEngine.Experimental.Rendering.RenderPipelineManager.currentPipeline as HDRenderPipeline;

            if (!renderContext || renderpipeline == null)
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
