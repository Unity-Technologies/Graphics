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

        // Subsurface Scattering Settings
        SerializedProperty m_SubsurfaceScatteringSettings;

        void InitializeProperties()
        {
            m_RenderPipelineResources = properties.Find("m_RenderPipelineResources");
            m_DefaultDiffuseMaterial = properties.Find("m_DefaultDiffuseMaterial");
            m_DefaultShader = properties.Find("m_DefaultShader");

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

        void SssSettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.PropertyField(m_SubsurfaceScatteringSettings, s_Styles.sssSettings);
        }

        void SettingsUI(HDRenderPipelineAsset renderContext)
        {
            EditorGUILayout.LabelField(s_Styles.settingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            SssSettingsUI(renderContext);

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
