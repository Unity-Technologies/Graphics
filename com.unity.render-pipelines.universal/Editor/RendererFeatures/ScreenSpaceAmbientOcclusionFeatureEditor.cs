using System.Linq;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Experimental.Rendering.Universal;
using System.Collections.Generic;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(ScreenSpaceAmbientOcclusionFeature.Settings), true)]
    internal class ScreenSpaceAmbientOcclusionFeatureEditor : PropertyDrawer
    {
        internal class Styles
        {
            public static GUIContent UseVolumes = new GUIContent("Use Volumes", "Enable this option to use the settings from a volume override.");
            public static GUIContent DownSample = new GUIContent("Downsample", "With this option enabled, Unity downsamples the SSAO effect texture to improve performance. Each dimension of the texture is reduced by a factor of 2.");
            //public static GUIContent DepthSource = new GUIContent("Depth Source", "");
            public static GUIContent NormalQuality = new GUIContent("Normal Quality", "The options in this field define the number of depth texture samples that Unity takes when computing the normals. Low: 1 sample, Medium: 5 samples, High: 9 samples.");
            public static GUIContent Intensity = new GUIContent("Intensity", "The degree of darkness that Ambient Occlusion adds.");
            public static GUIContent Radius = new GUIContent("Radius", "The radius around a given point, where Unity calculates and applies the effect.");
            public static GUIContent SampleCount = new GUIContent("Sample Count", "The number of samples that Unity takes when calculating the obscurance value. Higher values have high performance impact.");
            public static GUIContent BlurPassesCount = new GUIContent("Blur Passes", "The number of render passes for blurring the SSAO effect texture.");
        }

        // Serialized Properties
        private SerializedProperty m_UseVolumes;
        private SerializedProperty m_Downsample;
        //private SerializedProperty m_DepthSource;
        private SerializedProperty m_NormalQuality;
        private SerializedProperty m_Intensity;
        private SerializedProperty m_Radius;
        private SerializedProperty m_SampleCount;
        private SerializedProperty m_BlurPassesCount;
        private List<SerializedObject> m_properties = new List<SerializedObject>();

        private void Init(SerializedProperty property)
        {
            m_UseVolumes = property.FindPropertyRelative("UseVolumes");
            m_Downsample = property.FindPropertyRelative("Downsample");
            //m_DepthSource = property.FindPropertyRelative("DepthSource");
            m_NormalQuality = property.FindPropertyRelative("NormalQuality");
            m_Intensity = property.FindPropertyRelative("Intensity");
            m_Radius = property.FindPropertyRelative("Radius");
            m_SampleCount = property.FindPropertyRelative("SampleCount");
            m_BlurPassesCount = property.FindPropertyRelative("BlurPassesCount");
            m_properties.Add(property.serializedObject);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();

            if (!m_properties.Contains(property.serializedObject))
            {
                Init(property);
            }

            EditorGUILayout.PropertyField(m_UseVolumes, Styles.UseVolumes);
            if (m_UseVolumes.boolValue)
            {
                EditorGUILayout.HelpBox( "Settings will be taken from SSAO volumes. Make sure you have a Volume in your scene with a Screen Space Ambient Occlusion override.", MessageType.Info);
                EditorGUILayout.Space(4f);
            }
            else
            {
                EditorGUILayout.PropertyField(m_Downsample, Styles.DownSample);
                //EditorGUILayout.PropertyField(m_DepthSource, Styles.depthSource);
                //if (m_DepthSource == DepthSource.Depth)
                {
                    EditorGUILayout.PropertyField(m_NormalQuality, Styles.NormalQuality);
                }

                EditorGUILayout.Slider(m_Intensity, 0f, 10f, Styles.Intensity);
                EditorGUILayout.Slider(m_Radius, 0f, 10f, Styles.Radius);
                EditorGUILayout.IntSlider(m_SampleCount, 0, 12, Styles.SampleCount);
                EditorGUILayout.IntSlider(m_BlurPassesCount, 0, 12, Styles.BlurPassesCount);
            }

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
