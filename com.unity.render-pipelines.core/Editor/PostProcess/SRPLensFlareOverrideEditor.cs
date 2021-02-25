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
        SerializedProperty m_MaxAttenuationDistance;
        SerializedProperty m_DistanceAttenuationCurve;
        SerializedProperty m_ScaleByDistanceCurve;
        SerializedProperty m_AttenuationByLightShape;
        SerializedProperty m_RadialScreenAttenuationCurve;
        SerializedProperty m_UseOcclusion;
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
            m_MaxAttenuationDistance = entryPoint.Find(x => x.maxAttenuationDistance);
            m_DistanceAttenuationCurve = entryPoint.Find(x => x.distanceAttenuationCurve);
            m_AttenuationByLightShape = entryPoint.Find(x => x.attenuationByLightShape);
            m_ScaleByDistanceCurve = entryPoint.Find(x => x.scaleByDistanceCurve);
            m_RadialScreenAttenuationCurve = entryPoint.Find(x => x.radialScreenAttenuationCurve);
            m_UseOcclusion = entryPoint.Find(x => x.useOcclusion);
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
            SRPLensFlareOverride lensFlareDat = m_Intensity.serializedObject.targetObject as SRPLensFlareOverride;
            bool attachedToLight = false;
            if (lensFlareDat != null &&
                lensFlareDat.GetComponent<Light>() != null)
            {
                attachedToLight = true;
            }

            EditorGUI.BeginChangeCheck();
            ++EditorGUI.indentLevel;
            EditorGUILayout.BeginFoldoutHeaderGroup(false, "      General", EditorStyles.boldLabel);
            {
                EditorGUILayout.PropertyField(m_LensFlareData, Styles.lensFlareData);
                EditorGUILayout.PropertyField(m_Intensity, Styles.intensity);
                if (attachedToLight)
                    EditorGUILayout.PropertyField(m_AttenuationByLightShape, Styles.attenuationByLightShape);
                EditorGUILayout.PropertyField(m_MaxAttenuationDistance, Styles.maxAttenuationDistance);
                EditorGUILayout.PropertyField(m_DistanceAttenuationCurve, Styles.distanceAttenuationCurve);
                EditorGUILayout.PropertyField(m_ScaleByDistanceCurve, Styles.scaleByDistanceCurve);
                EditorGUILayout.PropertyField(m_RadialScreenAttenuationCurve, Styles.radialScreenAttenuationCurve);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.BeginHorizontal();
            bool oldValue = m_UseOcclusion.boolValue;
            bool curValue;
            Rect rect = EditorGUILayout.GetControlRect();
            rect.x -= 26.0f;
            rect.width = 26.0f;
            if ((curValue = EditorGUI.Toggle(rect, oldValue)) != oldValue)
            {
                m_UseOcclusion.boolValue = curValue;
            }
            rect.x += 43.0f;
            rect.width = 92.0f;
            EditorGUI.BeginFoldoutHeaderGroup(rect, false, "Occlusion", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            if (m_UseOcclusion.boolValue)
            {
                EditorGUILayout.PropertyField(m_OcclusionRadius, Styles.occlusionRadius);
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(m_SamplesCount, Styles.sampleCount);
                --EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(m_OcclusionOffset, Styles.occlusionOffset);
            }
            --EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(m_AllowOffScreen, Styles.allowOffScreen);
            if (EditorGUI.EndChangeCheck())
            {
                m_LensFlareData.serializedObject.ApplyModifiedProperties();
            }
        }

        sealed class Styles
        {
            static public readonly GUIContent lensFlareData = new GUIContent("Lens Flare Data", "Lens flare asset used on this component.");
            static public readonly GUIContent intensity = new GUIContent("Intensity", "Intensity.");
            static public readonly GUIContent maxAttenuationDistance = new GUIContent("Max Attenuation Distance", "Distance used to scale the Distance Attenuation Curve.");
            static public readonly GUIContent distanceAttenuationCurve = new GUIContent("Distance Attenuation Curve", "Attenuation by distance, scaled by max distance.");
            static public readonly GUIContent scaleByDistanceCurve = new GUIContent("Distance Scale Curve", ".");
            static public readonly GUIContent attenuationByLightShape = new GUIContent("Attenuation By Light Shape", "If component attached to a light, attenuation the lens flare per light type.");
            static public readonly GUIContent radialScreenAttenuationCurve = new GUIContent("Screen Attenuation Curve", "Attenuation used radially, which allow for instance to enable flare only on the edge of the screen.");
            static public readonly GUIContent occlusionRadius = new GUIContent("Occlusion Radius", "Radius around the light used to occlude the flare (value in world space).");
            static public readonly GUIContent sampleCount = new GUIContent("Sample Count", "Random sample count used inside the disk with 'occlusion radius'. Higher sample counts will give a smoother attenuation when being occluded.");
            static public readonly GUIContent occlusionOffset = new GUIContent("Occlusion Offset", "Occlusion Offset allows us to offset the plane for where the disc of occlusion is placed in world space (which will make it appear smaller or larger on the debug view as it is moving relative to the camera).\nThis is useful in order to sample occlusion outside a light bulb if a flare was placed inside.");
            static public readonly GUIContent allowOffScreen = new GUIContent("Allow Off Screen", "If allowOffScreen is true then If the lens flare is outside the screen we still emit the flare on screen.");
        }
    }
}
