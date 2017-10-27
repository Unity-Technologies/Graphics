using UnityEngine;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BlacksmithSkySettings))]
    public class BlacksmithSkySettingsEditor : Editor
    {
        private float heightFogHeight = 0.0f;

        private class Styles
        {
            public readonly GUIContent skyHDRI = new GUIContent("HDRI");
            public readonly GUIContent skyResolution = new GUIContent("Resolution");
            public readonly GUIContent skyExposure = new GUIContent("Exposure");
            public readonly GUIContent skyRotation = new GUIContent("Rotation");
            public readonly GUIContent skyMultiplier = new GUIContent("Multiplier");
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
        private SerializedProperty m_LightingOverride;

        private SerializedProperty m_updateMode;
        private SerializedProperty m_updatePeriod;

        private SerializedProperty m_worldScaleExponent;
        private SerializedProperty m_maxSkyDistance;

        private SerializedProperty m_worldMieColorIntensity;
        private SerializedProperty m_worldMieColorRamp;
        private SerializedProperty m_worldMieDensity;
        private SerializedProperty m_worldMieNearScatterPush;
        private SerializedProperty m_worldMiePhaseAnisotropy;

        private SerializedProperty m_worldRayleighColorIntensity;
        private SerializedProperty m_worldRayleighColorRamp;
        private SerializedProperty m_worldRayleighDensity;
        private SerializedProperty m_worldRayleighNearScatterPush;

        private SerializedProperty m_heightSeaLevel;
        private SerializedProperty m_heightDistance;
        private SerializedProperty m_heightMieDensity;
        private SerializedProperty m_heightMieNearScatterPush;
        private SerializedProperty m_heightRayleighColor;
        private SerializedProperty m_heightRayleighDensity;
        private SerializedProperty m_heightRayleighIntensity;
        private SerializedProperty m_heightRayleighNearScatterPush;

        void OnEnable()
        {
            m_SkyHDRI = serializedObject.FindProperty("skyHDRI");
            m_SkyResolution = serializedObject.FindProperty("resolution");
            m_SkyExposure = serializedObject.FindProperty("exposure");
            m_SkyMultiplier = serializedObject.FindProperty("multiplier");
            m_SkyRotation = serializedObject.FindProperty("rotation");
            m_updateMode = serializedObject.FindProperty("updateMode");
            m_updatePeriod = serializedObject.FindProperty("updatePeriod");
            m_LightingOverride = serializedObject.FindProperty("lightingOverride");

            m_maxSkyDistance = serializedObject.FindProperty("maxSkyDistance");
            m_worldScaleExponent = serializedObject.FindProperty("worldScaleExponent");

            m_worldMieColorIntensity = serializedObject.FindProperty("worldMieColorIntensity");
            m_worldMieColorRamp = serializedObject.FindProperty("worldMieColorRamp");
            m_worldMieDensity = serializedObject.FindProperty("worldMieDensity");
            m_worldMieNearScatterPush = serializedObject.FindProperty("worldMieNearScatterPush");
            m_worldMiePhaseAnisotropy = serializedObject.FindProperty("worldMiePhaseAnisotropy");

            m_worldRayleighColorIntensity = serializedObject.FindProperty("worldRayleighColorIntensity");
            m_worldRayleighColorRamp = serializedObject.FindProperty("worldRayleighColorRamp");
            m_worldRayleighDensity = serializedObject.FindProperty("worldRayleighDensity");
            m_worldRayleighNearScatterPush = serializedObject.FindProperty("worldRayleighNearScatterPush");

            m_heightSeaLevel                = serializedObject.FindProperty("heightSeaLevel");
            m_heightDistance                = serializedObject.FindProperty("heightDistance");
            m_heightMieDensity              = serializedObject.FindProperty("heightMieDensity");
            m_heightMieNearScatterPush      = serializedObject.FindProperty("heightMieNearScatterPush");
            m_heightRayleighColor           = serializedObject.FindProperty("heightRayleighColor");
            m_heightRayleighDensity         = serializedObject.FindProperty("heightRayleighDensity");
            m_heightRayleighIntensity       = serializedObject.FindProperty("heightRayleighIntensity");
            m_heightRayleighNearScatterPush = serializedObject.FindProperty("heightRayleighNearScatterPush");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField(new GUIContent("Skydome"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_SkyHDRI, styles.skyHDRI);
            EditorGUILayout.PropertyField(m_SkyResolution, styles.skyResolution);
            EditorGUILayout.PropertyField(m_SkyExposure, styles.skyExposure);
            EditorGUILayout.PropertyField(m_SkyMultiplier, styles.skyMultiplier);
            EditorGUILayout.PropertyField(m_SkyRotation, styles.skyRotation);
            EditorGUILayout.PropertyField(m_updateMode);
            EditorGUILayout.PropertyField(m_updatePeriod);
            EditorGUILayout.PropertyField(m_LightingOverride, styles.lightingOverride);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(new GUIContent("Atmosphere"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_worldMieDensity, new GUIContent("Density"));
            m_worldRayleighDensity.floatValue = m_worldMieDensity.floatValue;
            EditorGUILayout.PropertyField(m_worldScaleExponent, new GUIContent("Global scale"));
            EditorGUILayout.PropertyField(m_maxSkyDistance, new GUIContent("Sky distance"));
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_worldMieColorRamp);
            EditorGUILayout.PropertyField(m_worldMieColorIntensity);
            EditorGUILayout.PropertyField(m_worldMieNearScatterPush);
            EditorGUILayout.PropertyField(m_worldMiePhaseAnisotropy);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_worldRayleighColorRamp);
            EditorGUILayout.PropertyField(m_worldRayleighColorIntensity);
            //EditorGUILayout.PropertyField(m_worldRayleighDensity);
            EditorGUILayout.PropertyField(m_worldRayleighNearScatterPush);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(new GUIContent("Height fog"), EditorStyles.boldLabel);

            heightFogHeight = EditorGUILayout.FloatField(new GUIContent("Height"), heightFogHeight);
            EditorGUILayout.PropertyField(m_heightDistance, new GUIContent("Falloff"));

            m_heightSeaLevel.floatValue = heightFogHeight + m_heightDistance.floatValue * 2.0f - 2.0f;
            EditorGUILayout.PropertyField(m_heightMieDensity, new GUIContent("Density"));
            m_heightRayleighDensity.floatValue = m_heightMieDensity.floatValue;

            EditorGUILayout.PropertyField(m_heightMieNearScatterPush);
            EditorGUILayout.PropertyField(m_heightRayleighColor);
            //EditorGUILayout.PropertyField(m_heightRayleighDensity);
            EditorGUILayout.PropertyField(m_heightRayleighIntensity);
            EditorGUILayout.PropertyField(m_heightRayleighNearScatterPush);

            //base.DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
