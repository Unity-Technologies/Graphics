using System.Reflection;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(HDRenderPipelineAsset))]
    public sealed partial class HDRenderPipelineInspector : HDBaseEditor<HDRenderPipelineAsset>
    {
        SerializedProperty m_DefaultDiffuseMaterial;
        SerializedProperty m_DefaultShader;

        // TilePass settings
        SerializedProperty m_enableTileAndCluster;
        SerializedProperty m_enableSplitLightEvaluation;
        SerializedProperty m_enableComputeLightEvaluation;
        SerializedProperty m_enableComputeLightVariants;
        SerializedProperty m_enableComputeMaterialVariants;
        SerializedProperty m_enableClustered;
        SerializedProperty m_enableFptlForOpaqueWhenClustered;
        SerializedProperty m_enableBigTilePrepass;

        // Rendering Settings
        SerializedProperty m_RenderingUseForwardOnly;
        SerializedProperty m_RenderingUseDepthPrepass;
        SerializedProperty m_RenderingUseDepthPrepassAlphaTestOnly;

        // Subsurface Scattering Settings
        SerializedProperty m_SubsurfaceScatteringSettings;

        // Shadow Settings
        SerializedProperty m_ShadowAtlasWidth;
        SerializedProperty m_ShadowAtlasHeight;

        // Texture Settings
        SerializedProperty m_SpotCookieSize;
        SerializedProperty m_PointCookieSize;
        SerializedProperty m_ReflectionCubemapSize;

        void InitializeProperties()
        {
            m_DefaultDiffuseMaterial = properties.Find("m_DefaultDiffuseMaterial");
            m_DefaultShader = properties.Find("m_DefaultShader");

            // Tile settings
            m_enableTileAndCluster = properties.Find(x => x.tileSettings.enableTileAndCluster);
            m_enableComputeLightEvaluation = properties.Find(x => x.tileSettings.enableComputeLightEvaluation);
            m_enableComputeLightVariants = properties.Find(x => x.tileSettings.enableComputeLightVariants);
            m_enableComputeMaterialVariants = properties.Find(x => x.tileSettings.enableComputeMaterialVariants);
            m_enableClustered = properties.Find(x => x.tileSettings.enableClustered);
            m_enableFptlForOpaqueWhenClustered = properties.Find(x => x.tileSettings.enableFptlForOpaqueWhenClustered);
            m_enableBigTilePrepass = properties.Find(x => x.tileSettings.enableBigTilePrepass);

            // Shadow settings
            m_ShadowAtlasWidth = properties.Find(x => x.shadowInitParams.shadowAtlasWidth);
            m_ShadowAtlasHeight = properties.Find(x => x.shadowInitParams.shadowAtlasHeight);

            // Texture settings
            m_SpotCookieSize = properties.Find(x => x.textureSettings.spotCookieSize);
            m_PointCookieSize = properties.Find(x => x.textureSettings.pointCookieSize);
            m_ReflectionCubemapSize = properties.Find(x => x.textureSettings.reflectionCubemapSize);

            // Rendering settings
            m_RenderingUseForwardOnly = properties.Find(x => x.renderingSettings.useForwardRenderingOnly);
            m_RenderingUseDepthPrepass = properties.Find(x => x.renderingSettings.useDepthPrepassWithDeferredRendering);
            m_RenderingUseDepthPrepassAlphaTestOnly = properties.Find(x => x.renderingSettings.renderAlphaTestOnlyInDeferredPrepass);

            // Subsurface Scattering Settings
            m_SubsurfaceScatteringSettings = properties.Find(x => x.sssSettings);
        }

        static void HackSetDirty(RenderPipelineAsset asset)
        {
            EditorUtility.SetDirty(asset);
            var method = typeof(RenderPipelineAsset).GetMethod("OnValidate", BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
                method.Invoke(asset, new object[0]);
        }

        void TileSettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(s_Styles.tileLightLoopSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_enableTileAndCluster, s_Styles.enableTileAndCluster);
            if (m_enableTileAndCluster.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_enableBigTilePrepass, s_Styles.enableBigTilePrepass);
                EditorGUILayout.PropertyField(m_enableClustered, s_Styles.enableClustered);

                // Tag: SUPPORT_COMPUTE_CLUSTER_OPAQUE - Uncomment this if you want to do cluster opaque with compute shader (by default we support only fptl on opaque)
                // if (m_enableClustered.boolValue)
                if (m_enableClustered.boolValue && !m_enableComputeLightEvaluation.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_enableFptlForOpaqueWhenClustered, s_Styles.enableFptlForOpaqueWhenClustered);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.PropertyField(m_enableComputeLightEvaluation, s_Styles.enableComputeLightEvaluation);
                if (m_enableComputeLightEvaluation.boolValue)
                {
                    // Tag: SUPPORT_COMPUTE_CLUSTER_OPAQUE - Uncomment this if you want to do cluster opaque with compute shader (by default we support only fptl on opaque)
                    m_enableFptlForOpaqueWhenClustered.boolValue = true; // Force fptl to be always true if compute evaluation is enable

                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_enableComputeLightVariants, s_Styles.enableComputeLightVariants);
                    EditorGUILayout.PropertyField(m_enableComputeMaterialVariants, s_Styles.enableComputeMaterialVariants);
                    EditorGUI.indentLevel--;
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        void SssSettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.PropertyField(m_SubsurfaceScatteringSettings, s_Styles.sssSettings);
        }

        void SettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.LabelField(s_Styles.settingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            SssSettingsUI(renderContext);
            ShadowSettingsUI(renderContext);
            TextureSettingsUI(renderContext);
            RendereringSettingsUI(renderContext);
            TileSettingsUI(renderContext);

            EditorGUI.indentLevel--;
        }

        void ShadowSettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(s_Styles.shadowSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_ShadowAtlasWidth, s_Styles.shadowsAtlasWidth);
            EditorGUILayout.PropertyField(m_ShadowAtlasHeight, s_Styles.shadowsAtlasHeight);

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        void RendereringSettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_Styles.renderingSettingsLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(m_RenderingUseForwardOnly, s_Styles.useForwardRenderingOnly);
            if (!m_RenderingUseForwardOnly.boolValue) // If we are deferred
            {
                EditorGUILayout.PropertyField(m_RenderingUseDepthPrepass, s_Styles.useDepthPrepassWithDeferredRendering);
                if(m_RenderingUseDepthPrepass.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_RenderingUseDepthPrepassAlphaTestOnly, s_Styles.renderAlphaTestOnlyInDeferredPrepass);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;
        }

        void TextureSettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(s_Styles.textureSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_SpotCookieSize, s_Styles.spotCookieSize);
            EditorGUILayout.PropertyField(m_PointCookieSize, s_Styles.pointCookieSize);
            EditorGUILayout.PropertyField(m_ReflectionCubemapSize, s_Styles.reflectionCubemapSize);

            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(renderContext); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            InitializeProperties();
        }

        public override void OnInspectorGUI()
        {
            if (!m_Target || m_HDPipeline == null)
                return;

            CheckStyles();

            serializedObject.Update();

            EditorGUILayout.LabelField(s_Styles.defaults, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_DefaultDiffuseMaterial, s_Styles.defaultDiffuseMaterial);
            EditorGUILayout.PropertyField(m_DefaultShader, s_Styles.defaultShader);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            SettingsUI(m_Target);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
