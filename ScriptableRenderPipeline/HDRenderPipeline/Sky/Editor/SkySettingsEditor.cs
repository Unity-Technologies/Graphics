using System.Collections;
using UnityEngine;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public abstract class SkySettingsEditor
        : Editor
    {
        static protected class SkySettingsStyles
        {
            public static readonly GUIContent skyResolution = new GUIContent("Resolution", "Resolution of the environment lighting generated from the sky.");
            public static readonly GUIContent skyExposure = new GUIContent("Exposure", "Exposure of the sky in EV.");
            public static readonly GUIContent skyRotation = new GUIContent("Rotation", "Rotation of the sky.");
            public static readonly GUIContent skyMultiplier = new GUIContent("Multiplier", "Intensity multiplier for the sky.");
            public static readonly GUIContent environmentUpdateMode = new GUIContent("Environment Update Mode", "Specify how the environment lighting should be updated.");
            public static readonly GUIContent environmentUpdatePeriod = new GUIContent("Environment Update Period", "If environment update is set to realtime, period in seconds at which it is updated (0.0 means every frame).");
            public static readonly GUIContent lightingOverride = new GUIContent("Lighting Override", "If a lighting override cubemap is provided, this cubemap will be used to compute lighting instead of the result from the visible sky.");
        }

        private SerializedProperty m_SkyResolution;
        private SerializedProperty m_SkyExposure;
        private SerializedProperty m_SkyMultiplier;
        private SerializedProperty m_SkyRotation;
        private SerializedProperty m_EnvUpdateMode;
        private SerializedProperty m_EnvUpdatePeriod;
        private SerializedProperty m_LightingOverride;

        private AtmosphericScatteringEditor m_AtmosphericScatteringEditor = new AtmosphericScatteringEditor();

        void OnEnable()
        {
            InitializeProperties();
        }

        virtual protected void InitializeProperties()
        {
            m_SkyResolution = serializedObject.FindProperty("resolution");
            m_SkyExposure = serializedObject.FindProperty("exposure");
            m_SkyMultiplier = serializedObject.FindProperty("multiplier");
            m_SkyRotation = serializedObject.FindProperty("rotation");
            m_EnvUpdateMode = serializedObject.FindProperty("updateMode");
            m_EnvUpdatePeriod = serializedObject.FindProperty("updatePeriod");
            m_LightingOverride = serializedObject.FindProperty("lightingOverride");

            SerializedProperty atmosphericScattering = serializedObject.FindProperty("atmosphericScatteringSettings");
            m_AtmosphericScatteringEditor.OnEnable(atmosphericScattering);
        }

        protected void CommonSkySettingsGUI()
        {
            EditorGUILayout.PropertyField(m_SkyResolution, SkySettingsStyles.skyResolution);
            EditorGUILayout.PropertyField(m_SkyExposure, SkySettingsStyles.skyExposure);
            EditorGUILayout.PropertyField(m_SkyMultiplier, SkySettingsStyles.skyMultiplier);
            EditorGUILayout.PropertyField(m_SkyRotation, SkySettingsStyles.skyRotation);

            EditorGUILayout.PropertyField(m_EnvUpdateMode, SkySettingsStyles.environmentUpdateMode);
            if (!m_EnvUpdateMode.hasMultipleDifferentValues && m_EnvUpdateMode.intValue == (int)EnvironementUpdateMode.Realtime)
            {
                EditorGUILayout.PropertyField(m_EnvUpdatePeriod, SkySettingsStyles.environmentUpdatePeriod);
            }
            EditorGUILayout.PropertyField(m_LightingOverride, SkySettingsStyles.lightingOverride);

            EditorGUILayout.Space();

            m_AtmosphericScatteringEditor.OnGUI();
        }
    }
}