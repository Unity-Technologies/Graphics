using UnityEngine;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HDRISkySettings))]
    public class HDRISkySettingsEditor
        : SkySettingsEditor
    {
        private class Styles
        {
            public readonly GUIContent skyHDRI = new GUIContent("HDRI", "Cubemap used to render the sky.");
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

        private SerializedProperty m_SkyHDRI;

        protected override void InitializeProperties()
        {
            base.InitializeProperties();

            m_SkyHDRI = serializedObject.FindProperty("skyHDRI");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_SkyHDRI, styles.skyHDRI);

            EditorGUILayout.Space();

            base.CommonSkySettingsGUI();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
