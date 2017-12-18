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
        // Global Render settings
        SerializedProperty m_supportDBuffer;
        SerializedProperty m_supportMSAA;
        // Global Shadow settings
        SerializedProperty m_ShadowAtlasWidth;
        SerializedProperty m_ShadowAtlasHeight;
        // Global LightLoop settings
        SerializedProperty m_SpotCookieSize;
        SerializedProperty m_PointCookieSize;
        SerializedProperty m_ReflectionCubemapSize;
        SerializedProperty m_ReflectionCacheCompressed;

        // FrameSettings
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
            // Global Render settings
            m_supportDBuffer = properties.Find(x => x.renderPipelineSettings.supportDBuffer);
            m_supportMSAA = properties.Find(x => x.renderPipelineSettings.supportMSAA);
            // Global Shadow settings
            m_ShadowAtlasWidth = properties.Find(x => x.renderPipelineSettings.shadowInitParams.shadowAtlasWidth);
            m_ShadowAtlasHeight = properties.Find(x => x.renderPipelineSettings.shadowInitParams.shadowAtlasHeight);
            // Global LightLoop settings
            m_SpotCookieSize = properties.Find(x => x.renderPipelineSettings.lightLoopSettings.spotCookieSize);
            m_PointCookieSize = properties.Find(x => x.renderPipelineSettings.lightLoopSettings.pointCookieSize);
            m_ReflectionCubemapSize = properties.Find(x => x.renderPipelineSettings.lightLoopSettings.reflectionCubemapSize);
            m_ReflectionCacheCompressed = properties.Find(x => x.renderPipelineSettings.lightLoopSettings.reflectionCacheCompressed);

            // FrameSettings
            // LightLoop settings
            m_enableTileAndCluster = properties.Find(x => x.serializedFrameSettings.lightLoopSettings.enableTileAndCluster);
            m_enableComputeLightEvaluation = properties.Find(x => x.serializedFrameSettings.lightLoopSettings.enableComputeLightEvaluation);
            m_enableComputeLightVariants = properties.Find(x => x.serializedFrameSettings.lightLoopSettings.enableComputeLightVariants);
            m_enableComputeMaterialVariants = properties.Find(x => x.serializedFrameSettings.lightLoopSettings.enableComputeMaterialVariants);
            m_enableFptlForForwardOpaque = properties.Find(x => x.serializedFrameSettings.lightLoopSettings.enableFptlForForwardOpaque);
            m_enableBigTilePrepass = properties.Find(x => x.serializedFrameSettings.lightLoopSettings.enableBigTilePrepass);
            // Rendering Settings
            m_enableAsyncCompute = properties.Find(x => x.serializedFrameSettings.enableAsyncCompute);
            m_RenderingUseForwardOnly = properties.Find(x => x.serializedFrameSettings.enableForwardRenderingOnly);
            m_RenderingUseDepthPrepass = properties.Find(x => x.serializedFrameSettings.enableDepthPrepassWithDeferredRendering);
            m_RenderingUseDepthPrepassAlphaTestOnly = properties.Find(x => x.serializedFrameSettings.enableAlphaTestOnlyInDeferredPrepass);

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

        void GlobalLightLoopSettingsUI(HDRenderPipelineAsset hdAsset)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_Styles.textureSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_SpotCookieSize, s_Styles.spotCookieSize);
            EditorGUILayout.PropertyField(m_PointCookieSize, s_Styles.pointCookieSize);
            EditorGUILayout.PropertyField(m_ReflectionCubemapSize, s_Styles.reflectionCubemapSize);
            // Commented out until we have proper realtime BC6H compression
            //EditorGUILayout.PropertyField(m_ReflectionCacheCompressed, s_Styles.reflectionCacheCompressed);
            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(hdAsset); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        void GlobalRenderSettingsUI(HDRenderPipelineAsset hdAsset)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_Styles.renderingSettingsLabel);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_supportDBuffer, s_Styles.supportDBuffer);
            EditorGUILayout.PropertyField(m_supportMSAA, s_Styles.supportMSAA);
            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(hdAsset); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        void GlobalShadowSettingsUI(HDRenderPipelineAsset hdAsset)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_Styles.shadowSettings);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_ShadowAtlasWidth, s_Styles.shadowsAtlasWidth);
            EditorGUILayout.PropertyField(m_ShadowAtlasHeight, s_Styles.shadowsAtlasHeight);
            if (EditorGUI.EndChangeCheck())
            {
                HackSetDirty(hdAsset); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        void LightLoopSettingsUI(HDRenderPipelineAsset hdAsset)
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
                HackSetDirty(hdAsset); // Repaint
            }
            EditorGUI.indentLevel--;
        }

        void RendereringSettingsUI(HDRenderPipelineAsset hdAsset)
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

        void SettingsUI(HDRenderPipelineAsset hdAsset)
        {
            EditorGUILayout.LabelField(s_Styles.settingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField(s_Styles.renderPipelineSettings, EditorStyles.boldLabel);

            GlobalRenderSettingsUI(hdAsset);
            GlobalShadowSettingsUI(hdAsset);
            GlobalLightLoopSettingsUI(hdAsset);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_Styles.defaultFrameSettings, EditorStyles.boldLabel);

            RendereringSettingsUI(hdAsset);
            LightLoopSettingsUI(hdAsset);

            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_SubsurfaceScatteringSettings, s_Styles.sssSettings);
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
