using UnityEngine;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProceduralSkySettings))]
    public class ProceduralSkySettingsEditor
        : SkySettingsEditor
    {
        private class Styles
        {
            public readonly GUIContent sunSize = new GUIContent("Sun Size");
            public readonly GUIContent sunSizeConvergence = new GUIContent("Sun Size Convergence");
            public readonly GUIContent atmosphereThickness = new GUIContent("Atmosphere Thickness");
            public readonly GUIContent skyTint = new GUIContent("SkyTint");
            public readonly GUIContent groundColor = new GUIContent("Ground Color");
            public readonly GUIContent enableSunDisk = new GUIContent("Enable Sun Disk");
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

        private SerializedProperty m_SunSize;
        private SerializedProperty m_SunSizeConvergence;
        private SerializedProperty m_AtmosphericThickness;
        private SerializedProperty m_SkyTint;
        private SerializedProperty m_GroundColor;
        private SerializedProperty m_EnableSunDisk;

        protected override void InitializeProperties()
        {
            base.InitializeProperties();

            m_SunSize = serializedObject.FindProperty("sunSize");
            m_SunSizeConvergence = serializedObject.FindProperty("sunSizeConvergence");
            m_AtmosphericThickness = serializedObject.FindProperty("atmosphereThickness");
            m_SkyTint = serializedObject.FindProperty("skyTint");
            m_GroundColor = serializedObject.FindProperty("groundColor");
            m_EnableSunDisk = serializedObject.FindProperty("enableSunDisk");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_EnableSunDisk, styles.enableSunDisk);
            EditorGUILayout.PropertyField(m_SunSize, styles.sunSize);
            EditorGUILayout.PropertyField(m_SunSizeConvergence, styles.sunSizeConvergence);
            EditorGUILayout.PropertyField(m_AtmosphericThickness, styles.atmosphereThickness);
            EditorGUILayout.PropertyField(m_SkyTint, styles.skyTint);
            EditorGUILayout.PropertyField(m_GroundColor, styles.groundColor);

            EditorGUILayout.Space();

            base.CommonSkySettingsGUI();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
