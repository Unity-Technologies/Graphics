using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(ScreenSpaceAmbientOcclusion.Settings), true)]
    internal class ScreenSpaceAmbientOcclusionEditor : PropertyDrawer
    {
        // Serialized Properties
        private SerializedProperty m_Quality;
        private List<SerializedObject> m_Properties = new List<SerializedObject>();

        // Constants
        private const string k_Message = "Quality settings are controlled in the Universal Render Pipeline Asset but they "
                                         +"can also be overridden by volumes in your scenes.";
        private static GUIContent[] s_QualityOptions =
        {
            EditorGUIUtility.TrTextContent("Low"),
            EditorGUIUtility.TrTextContent("Medium"),
            EditorGUIUtility.TrTextContent("High"),
        };
        private static int[] s_QualityValues = { 0, 1, 2};

        // Structs
        internal struct Styles
        {
            public static GUIContent ScreenSpaceAmbientOcclusion = EditorGUIUtility.TrTextContent("Screen Space Ambient Occlusion", "Screen Space Ambient Occlusion.");
            public static GUIContent LowQuality = EditorGUIUtility.TrTextContent("Low", "The low settings.");
            public static GUIContent MediumQuality = EditorGUIUtility.TrTextContent("Medium", "The medium settings.");
            public static GUIContent HighQuality = EditorGUIUtility.TrTextContent("High", "The high settings.");
            public static GUIContent Quality = EditorGUIUtility.TrTextContent("Quality", "The quality setting to use. The settings are controlled in the Universal Render Pipeline Asset.");
            public static GUIContent DefaultQuality = EditorGUIUtility.TrTextContent("Default Quality", "The quality setting to use. The settings are controlled in the Universal Render Pipeline Asset.");
            public static GUIContent Downsample = EditorGUIUtility.TrTextContent("Downsample", "With this option enabled, Unity downsamples the SSAO effect texture to improve performance. Each dimension of the texture is reduced by a factor of 2.");
            //public static GUIContent DepthSource = EditorGUIUtility.TrTextContent("Depth Source", "");
            public static GUIContent NormalSamples = EditorGUIUtility.TrTextContent("Normal Samples", "The options in this field define the number of depth texture samples that Unity takes when computing the normals from the Depth texture.");
            public static GUIContent Intensity = EditorGUIUtility.TrTextContent("Intensity", "The degree of darkness that Ambient Occlusion adds.");
            public static GUIContent Radius = EditorGUIUtility.TrTextContent("Radius", "The radius around a given point, where Unity calculates and applies the effect.");
            public static GUIContent SampleCount = EditorGUIUtility.TrTextContent("Sample Count", "The number of samples that Unity takes when calculating the obscurance value. Higher values have high performance impact.");
            public static GUIContent BlurPasses = EditorGUIUtility.TrTextContent("Blur Passes", "The number of render passes for blurring the SSAO effect texture.");
        }

        private void Init(SerializedProperty property)
        {
            m_Properties.Clear();
            m_Quality = property.FindPropertyRelative("Quality");
            m_Properties.Add(property.serializedObject);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();

            if (!m_Properties.Contains(property.serializedObject))
            {
                Init(property);
            }

            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, Styles.Quality, m_Quality);

            EditorGUI.BeginChangeCheck();
            int value = EditorGUI.IntPopup(controlRect, Styles.DefaultQuality, m_Quality.intValue, s_QualityOptions, s_QualityValues);
            if (EditorGUI.EndChangeCheck())
            {
                m_Quality.intValue = value;
            }
            EditorGUI.EndProperty();

            EditorGUILayout.Space(5f);
            EditorGUILayout.HelpBox(k_Message, MessageType.Info);
        }
    }
}
