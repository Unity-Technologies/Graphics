using System.Reflection;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(HDRenderPipelineAsset))]
    public sealed partial class HDRenderPipelineInspector : HDBaseEditor<HDRenderPipelineAsset>
    {
        SerializedProperty m_RenderPipelineResources;
        SerializedProperty m_DefaultDiffuseMaterial;
        SerializedProperty m_DefaultShader;

        // Global Frame Settings
        SerializedProperty m_GlobalFrameSettings;

        // LightLoop settings
        SerializedProperty m_enableTileAndCluster;
        SerializedProperty m_enableSplitLightEvaluation;
        SerializedProperty m_enableComputeLightEvaluation;
        SerializedProperty m_enableComputeLightVariants;
        SerializedProperty m_enableComputeMaterialVariants;
        SerializedProperty m_enableFptlForForwardOpaque;
        SerializedProperty m_enableBigTilePrepass;

        // Rendering Settings
        SerializedProperty m_RenderingUseForwardOnly;
        SerializedProperty m_RenderingUseDepthPrepass;
        SerializedProperty m_RenderingUseDepthPrepassAlphaTestOnly;
        SerializedProperty m_enableAsyncCompute;

        // Subsurface Scattering Settings
        SerializedProperty m_SubsurfaceScatteringSettings;

        void InitializeProperties()
        {
            m_RenderPipelineResources = properties.Find("m_RenderPipelineResources");
            m_DefaultDiffuseMaterial = properties.Find("m_DefaultDiffuseMaterial");
            m_DefaultShader = properties.Find("m_DefaultShader");

            // Global FrameSettings
            m_GlobalFrameSettings = properties.Find(x => x.globalFrameSettings);

            // LightLoop settings
            m_enableTileAndCluster = properties.Find(x => x.defaultFrameSettings.lightLoopSettings.enableTileAndCluster);
            m_enableComputeLightEvaluation = properties.Find(x => x.defaultFrameSettings.lightLoopSettings.enableComputeLightEvaluation);
            m_enableComputeLightVariants = properties.Find(x => x.defaultFrameSettings.lightLoopSettings.enableComputeLightVariants);
            m_enableComputeMaterialVariants = properties.Find(x => x.defaultFrameSettings.lightLoopSettings.enableComputeMaterialVariants);
            m_enableFptlForForwardOpaque = properties.Find(x => x.defaultFrameSettings.lightLoopSettings.enableFptlForForwardOpaque);
            m_enableBigTilePrepass = properties.Find(x => x.defaultFrameSettings.lightLoopSettings.enableBigTilePrepass);

            // Rendering Settings
            m_enableAsyncCompute = properties.Find(x => x.defaultFrameSettings.renderSettings.enableAsyncCompute);
            m_RenderingUseForwardOnly = properties.Find(x => x.defaultFrameSettings.renderSettings.enableForwardRenderingOnly);
            m_RenderingUseDepthPrepass = properties.Find(x => x.defaultFrameSettings.renderSettings.enableDepthPrepassWithDeferredRendering);
            m_RenderingUseDepthPrepassAlphaTestOnly = properties.Find(x => x.defaultFrameSettings.renderSettings.enableAlphaTestOnlyInDeferredPrepass);

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

        void LightLoopSettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_Styles.lightLoopSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_enableTileAndCluster, s_Styles.enableTileAndCluster);
            if (m_enableTileAndCluster.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_enableBigTilePrepass, s_Styles.enableBigTilePrepass);
                // Allow to disable cluster for forward opaque when in forward only (option have no effect when MSAA is enabled)
                // Deferred opaque are always tiled
                EditorGUILayout.PropertyField(m_enableFptlForForwardOpaque, s_Styles.enableFptlForForwardOpaque);
                EditorGUILayout.PropertyField(m_enableComputeLightEvaluation, s_Styles.enableComputeLightEvaluation);
                if (m_enableComputeLightEvaluation.boolValue)
                {
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

        void RendereringSettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_Styles.renderingSettingsLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(m_RenderingUseForwardOnly, s_Styles.useForwardRenderingOnly);
            if (!m_RenderingUseForwardOnly.boolValue) // If we are deferred
            {
                EditorGUILayout.PropertyField(m_RenderingUseDepthPrepass, s_Styles.useDepthPrepassWithDeferredRendering);
                if (m_RenderingUseDepthPrepass.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_RenderingUseDepthPrepassAlphaTestOnly, s_Styles.renderAlphaTestOnlyInDeferredPrepass);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.PropertyField(m_enableAsyncCompute, s_Styles.enableAsyncCompute);

            EditorGUI.indentLevel--;
        }

        void SettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.LabelField(s_Styles.settingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(m_GlobalFrameSettings, s_Styles.globalFrameSettings);

            RendereringSettingsUI(renderContext);
            LightLoopSettingsUI(renderContext);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_SubsurfaceScatteringSettings, s_Styles.sssSettings);

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
            EditorGUILayout.PropertyField(m_RenderPipelineResources, s_Styles.renderPipelineResources);
            EditorGUILayout.PropertyField(m_DefaultDiffuseMaterial, s_Styles.defaultDiffuseMaterial);
            EditorGUILayout.PropertyField(m_DefaultShader, s_Styles.defaultShader);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            SettingsUI(m_Target);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
