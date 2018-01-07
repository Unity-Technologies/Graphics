using System.Linq;
using System.Reflection;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<FrameSettingsUI, SerializedFrameSettings>;
    using _ = CoreEditorUtils;

    [CustomEditor(typeof(HDRenderPipelineAsset))]
    public sealed partial class HDRenderPipelineInspector : HDBaseEditor<HDRenderPipelineAsset>
    {
        static readonly CED.IDrawer[] k_FrameSettings = new[]
        {
            FrameSettingsUI.SectionRenderingPasses,
            FrameSettingsUI.SectionRenderingSettings,
            CED.Select(
                (s, d, o) => s.lightLoopSettingsUI,
                (s, d, o) => d.lightLoopSettings,
                LightLoopSettingsUI.SectionLightLoopSettings),
            FrameSettingsUI.SectionXRSettings
        };

        SerializedProperty m_RenderPipelineResources;

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
        // Commented out until we have proper realtime BC6H compression
        //SerializedProperty m_ReflectionCacheCompressed;
        SerializedProperty m_SkyReflectionSize;
        SerializedProperty m_SkyLightingOverrideLayerMask;

        // Diffusion profile Settings
        SerializedProperty m_DiffusionProfileSettings;

        SerializedFrameSettings serializedFrameSettings = null;
        FrameSettingsUI m_FrameSettingsUI = new FrameSettingsUI();

        void InitializeProperties()
        {
            m_RenderPipelineResources = properties.Find("m_RenderPipelineResources");

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
            // Commented out until we have proper realtime BC6H compression
            //m_ReflectionCacheCompressed = properties.Find(x => x.globalFrameSettings.lightLoopSettings.reflectionCacheCompressed);
            m_SkyReflectionSize = properties.Find(x => x.renderPipelineSettings.lightLoopSettings.skyReflectionSize);
            m_SkyLightingOverrideLayerMask = properties.Find(x => x.renderPipelineSettings.lightLoopSettings.skyLightingOverrideLayerMask);

            // Diffusion profile Settings
            m_DiffusionProfileSettings = properties.Find(x => x.diffusionProfileSettings);

            serializedFrameSettings = new SerializedFrameSettings(properties.Find(x => x.serializedFrameSettings));

            m_FrameSettingsUI.Reset(serializedFrameSettings, Repaint);
        }

        void GlobalLightLoopSettingsUI(HDRenderPipelineAsset hdAsset)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_Styles.textureSettings);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_SpotCookieSize, s_Styles.spotCookieSize);
            EditorGUILayout.PropertyField(m_PointCookieSize, s_Styles.pointCookieSize);
            EditorGUILayout.PropertyField(m_ReflectionCubemapSize, s_Styles.reflectionCubemapSize);
            // Commented out until we have proper realtime BC6H compression
            //EditorGUILayout.PropertyField(m_ReflectionCacheCompressed, s_Styles.reflectionCacheCompressed);
            EditorGUILayout.PropertyField(m_SkyReflectionSize, s_Styles.skyReflectionSize);
            EditorGUILayout.PropertyField(m_SkyLightingOverrideLayerMask, s_Styles.skyLightingOverride);
            EditorGUI.indentLevel--;
        }

        void GlobalRenderSettingsUI(HDRenderPipelineAsset hdAsset)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_Styles.renderingSettingsLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_supportDBuffer, s_Styles.supportDBuffer);
            EditorGUILayout.PropertyField(m_supportMSAA, s_Styles.supportMSAA);
            EditorGUI.indentLevel--;
        }

        void GlobalShadowSettingsUI(HDRenderPipelineAsset hdAsset)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_Styles.shadowSettings);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_ShadowAtlasWidth, s_Styles.shadowsAtlasWidth);
            EditorGUILayout.PropertyField(m_ShadowAtlasHeight, s_Styles.shadowsAtlasHeight);
            EditorGUI.indentLevel--;
        }

        void SettingsUI(HDRenderPipelineAsset hdAsset)
        {
            EditorGUILayout.LabelField(s_Styles.renderPipelineSettings, EditorStyles.boldLabel);
            GlobalRenderSettingsUI(hdAsset);
            GlobalShadowSettingsUI(hdAsset);
            GlobalLightLoopSettingsUI(hdAsset);

            EditorGUILayout.Space();
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
            m_FrameSettingsUI.Update();

            EditorGUILayout.PropertyField(m_RenderPipelineResources, s_Styles.renderPipelineResources);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_DiffusionProfileSettings, s_Styles.diffusionProfileSettings);
            EditorGUILayout.Space();

            SettingsUI(m_Target);

            EditorGUILayout.LabelField(s_Styles.defaultFrameSettings, EditorStyles.boldLabel);
            k_FrameSettings.Draw(m_FrameSettingsUI, serializedFrameSettings, this);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
