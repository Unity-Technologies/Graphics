using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Rendering
{
    [CustomEditor(typeof(SRPLensFlareOverride))]
    public class SRPLensFlareOverrideEditor : Editor
    {
        SerializedProperty m_LensFlareData;
        SerializedProperty m_Intensity;
        SerializedProperty m_DistanceAttenuationCurve;
        SerializedProperty m_AttenuationByLightShape;
        SerializedProperty m_RadialScreenAttenuationCurve;
        SerializedProperty m_OcclusionRadius;
        SerializedProperty m_SamplesCount;
        SerializedProperty m_OcclusionOffset;
        SerializedProperty m_AllowOffScreen;

        /// <summary>
        /// Prepare the code for the UI
        /// </summary>
        public void OnEnable()
        {
            PropertyFetcher<SRPLensFlareOverride> entryPoint = new PropertyFetcher<SRPLensFlareOverride>(serializedObject);
            m_LensFlareData = entryPoint.Find(x => x.lensFlareData);
            m_Intensity = entryPoint.Find(x => x.intensity);
            m_DistanceAttenuationCurve = entryPoint.Find(x => x.distanceAttenuationCurve);
            m_AttenuationByLightShape = entryPoint.Find(x => x.attenuationByLightShape);
            m_RadialScreenAttenuationCurve = entryPoint.Find(x => x.radialScreenAttenuationCurve);
            m_OcclusionRadius = entryPoint.Find(x => x.occlusionRadius);
            m_SamplesCount = entryPoint.Find(x => x.sampleCount);
            m_OcclusionOffset = entryPoint.Find(x => x.occlusionOffset);
            m_AllowOffScreen = entryPoint.Find(x => x.allowOffScreen);
        }

        /// <summary>
        /// Implement this function to make a custom inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            ++EditorGUI.indentLevel;
            EditorGUILayout.BeginFoldoutHeaderGroup(true, "    General", EditorStyles.boldLabel);
            {
                EditorGUILayout.PropertyField(m_LensFlareData);
                EditorGUILayout.PropertyField(m_Intensity);
                EditorGUILayout.PropertyField(m_DistanceAttenuationCurve);
                EditorGUILayout.PropertyField(m_AttenuationByLightShape);
                EditorGUILayout.PropertyField(m_RadialScreenAttenuationCurve);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.BeginFoldoutHeaderGroup(false, "    Occlusion", EditorStyles.boldLabel);
            {
                EditorGUILayout.PropertyField(m_OcclusionRadius);   // Occlusion Fade Radius
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(m_SamplesCount);      // 
                --EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(m_OcclusionOffset);
                EditorGUILayout.PropertyField(m_AllowOffScreen);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            --EditorGUI.indentLevel;
            if (EditorGUI.EndChangeCheck())
            {
                m_LensFlareData.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
