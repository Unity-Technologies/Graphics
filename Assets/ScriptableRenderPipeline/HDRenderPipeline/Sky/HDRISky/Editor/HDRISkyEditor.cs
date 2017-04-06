using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HDRISkySettings))]
    public class HDRISkyParametersEditor
        : Editor
    {
        private class Styles
        {
            public readonly GUIContent skyHDRI = new GUIContent("HDRI", "Cubemap used to render the sky.");
            public readonly GUIContent skyResolution = new GUIContent("Resolution", "Resolution of the environment lighting generated from the sky.");
            public readonly GUIContent skyExposure = new GUIContent("Exposure", "Exposure of the sky in EV.");
            public readonly GUIContent skyRotation = new GUIContent("Rotation", "Rotation of the sky.");
            public readonly GUIContent skyMultiplier = new GUIContent("Multiplier", "Intensity multiplier for the sky.");
            public readonly GUIContent environmentUpdateMode = new GUIContent("Environment Update Mode", "Specify how the environment lighting should be updated.");
            public readonly GUIContent environmentUpdatePeriod = new GUIContent("Environment Update Period", "If environment update is set to realtime, period in seconds at which it is updated (0.0 means every frame).");
            public readonly GUIContent lightingOverride = new GUIContent("Lighting Override", "If a lighting override cubemap is provided, this cubemap will be used to compute lighting instead of the result from the visible sky.");
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
        private SerializedProperty m_SkyResolution;
        private SerializedProperty m_SkyExposure;
        private SerializedProperty m_SkyMultiplier;
        private SerializedProperty m_SkyRotation;
        private SerializedProperty m_EnvUpdateMode;
        private SerializedProperty m_EnvUpdatePeriod;
        private SerializedProperty m_LightingOverride;

        void OnEnable()
        {
            m_SkyHDRI = serializedObject.FindProperty("skyHDRI");
            m_SkyResolution = serializedObject.FindProperty("resolution");
            m_SkyExposure = serializedObject.FindProperty("exposure");
            m_SkyMultiplier = serializedObject.FindProperty("multiplier");
            m_SkyRotation = serializedObject.FindProperty("rotation");
            m_EnvUpdateMode = serializedObject.FindProperty("updateMode");
            m_EnvUpdatePeriod = serializedObject.FindProperty("updatePeriod");
            m_LightingOverride = serializedObject.FindProperty("lightingOverride");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_SkyHDRI, styles.skyHDRI);
            EditorGUILayout.PropertyField(m_SkyResolution, styles.skyResolution);
            EditorGUILayout.PropertyField(m_SkyExposure, styles.skyExposure);
            EditorGUILayout.PropertyField(m_SkyMultiplier, styles.skyMultiplier);
            EditorGUILayout.PropertyField(m_SkyRotation, styles.skyRotation);

            EditorGUILayout.PropertyField(m_EnvUpdateMode, styles.environmentUpdateMode);
            if (!m_EnvUpdateMode.hasMultipleDifferentValues && m_EnvUpdateMode.intValue == (int)EnvironementUpdateMode.Realtime)
            {
                EditorGUILayout.PropertyField(m_EnvUpdatePeriod, styles.environmentUpdatePeriod);
            }
            EditorGUILayout.PropertyField(m_LightingOverride, styles.lightingOverride);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
