using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(CommonSettings))]
    [CanEditMultipleObjects]
    public class CommonSettingsEditor
        : Editor
    {
        private class Styles
        {
            public readonly GUIContent none = new GUIContent("None");
            public readonly GUIContent sky = new GUIContent("Sky");
            public readonly GUIContent skyRenderer = new GUIContent("Sky Renderer");

            public readonly GUIContent shadows = new GUIContent("Shadows");
            public readonly GUIContent maxShadowDistance = new GUIContent("Maximum shadow distance");
            public readonly GUIContent shadowsDirectionalLightCascadeCount = new GUIContent("Directional cascade count");
            public readonly GUIContent[] shadowsCascadeCounts = new GUIContent[] { new GUIContent("1"), new GUIContent("2"), new GUIContent("3"), new GUIContent("4") };
            public readonly int[] shadowsCascadeCountValues = new int[] { 1, 2, 3, 4 };
            public readonly GUIContent shadowsCascades = new GUIContent("Cascade values");
            public readonly GUIContent[] shadowSplits = new GUIContent[] { new GUIContent("Split 0"), new GUIContent("Split 1"), new GUIContent("Split 2") };

            public readonly GUIContent sssCategory                = new GUIContent("Subsurface scattering");
            public readonly GUIContent sssProfileFilter1Variance  = new GUIContent("SSS profile filter #1 variance", "Determines the shape of the 1st Gaussian filter. Increases the strength of the blur of the corresponding color channel.");
            public readonly GUIContent sssProfileFilter2Variance  = new GUIContent("SSS profile filter #2 variance", "Determines the shape of the 2nd Gaussian filter. Increases the strength of the blur of the corresponding color channel.");
            public readonly GUIContent sssProfileFilterLerpWeight = new GUIContent("SSS profile filter interpolation", "Controls linear interpolation between the two Gaussian filters.");
            public readonly GUIContent sssBilateralScale          = new GUIContent("SSS bilateral filtering scale", "Larger values make the filter more tolerant to depth differences.");
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

        // Sky renderer
        List<Type> m_SkyRendererTypes = new List<Type>();
        private List<GUIContent> m_SkyRendererTypeNames = new List<GUIContent>();
        private List<string> m_SkyRendererFullTypeNames = new List<string>();
        private List<int> m_SkyRendererTypeValues = new List<int>();

        private bool multipleEditing { get { return targets.Length > 1; } }

        private SerializedProperty m_SkyRenderer;

        private SerializedProperty m_ShadowMaxDistance;
        private SerializedProperty m_ShadowCascadeCount;
        private SerializedProperty[] m_ShadowCascadeSplits = new SerializedProperty[3];

        // Subsurface scattering
        private SerializedProperty m_SssProfileFilter1Variance;
        private SerializedProperty m_SssProfileFilter2Variance;
        private SerializedProperty m_SssProfileFilterLerpWeight;
        private SerializedProperty m_SssBilateralScale;

        void OnEnable()
        {
            m_SkyRenderer = serializedObject.FindProperty("m_SkyRendererTypeName");

            m_ShadowMaxDistance = serializedObject.FindProperty("m_ShadowMaxDistance");
            m_ShadowCascadeCount = serializedObject.FindProperty("m_ShadowCascadeCount");
            for (int i = 0; i < 3; ++i)
                m_ShadowCascadeSplits[i] = serializedObject.FindProperty(string.Format("m_ShadowCascadeSplit{0}", i));

            m_SkyRendererTypes = Assembly.GetAssembly(typeof(SkyRenderer))
                                            .GetTypes()
                                            .Where(t => t.IsSubclassOf(typeof(SkyRenderer)) && !t.IsGenericType)
                                            .ToList();

            // Prepare the list of available SkyRenderers for the IntPopup
            m_SkyRendererTypeNames.Clear();
            m_SkyRendererFullTypeNames.Clear();
            m_SkyRendererTypeValues.Clear();
            for (int i = 0; i < m_SkyRendererTypes.Count; ++i)
            {
                string longName = m_SkyRendererTypes[i].ToString();
                m_SkyRendererFullTypeNames.Add(longName);
                char[] separators = {'.'};
                string[] tokens = longName.Split(separators);
                m_SkyRendererTypeNames.Add(new GUIContent(tokens[tokens.Length - 1]));
                m_SkyRendererTypeValues.Add(i);
            }

            // Add default null value.
            m_SkyRendererTypeNames.Add(styles.none);
            m_SkyRendererFullTypeNames.Add("");
            m_SkyRendererTypeValues.Add(m_SkyRendererTypeValues.Count);
            m_SkyRendererTypes.Add(null);

            m_SssProfileFilter1Variance  = serializedObject.FindProperty("m_SssProfileFilter1Variance");
            m_SssProfileFilter2Variance  = serializedObject.FindProperty("m_SssProfileFilter2Variance");
            m_SssProfileFilterLerpWeight = serializedObject.FindProperty("m_SssProfileFilterLerpWeight");
            m_SssBilateralScale          = serializedObject.FindProperty("m_SssBilateralScale");
        }

        void OnSkyInspectorGUI()
        {
            EditorGUILayout.LabelField(styles.sky);
            EditorGUI.indentLevel++;

            // Retrieve the index of the current SkyRenderer. Won't be used in case of multiple editing with different values
            int index = -1;
            for (int i = 0; i < m_SkyRendererTypeNames.Count; ++i)
            {
                if (m_SkyRendererFullTypeNames[i] == m_SkyRenderer.stringValue)
                {
                    index = i;
                    break;
                }
            }

            EditorGUI.showMixedValue = m_SkyRenderer.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUILayout.IntPopup(styles.skyRenderer, index, m_SkyRendererTypeNames.ToArray(), m_SkyRendererTypeValues.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                m_SkyRenderer.stringValue = m_SkyRendererFullTypeNames[newValue];
            }
            EditorGUI.showMixedValue = false;

            EditorGUI.indentLevel--;
        }

        void OnShadowInspectorGUI()
        {
            EditorGUILayout.LabelField(styles.shadows);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_ShadowMaxDistance, styles.maxShadowDistance);

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = m_ShadowCascadeCount.hasMultipleDifferentValues;
            int newCascadeCount = EditorGUILayout.IntPopup(styles.shadowsDirectionalLightCascadeCount, m_ShadowCascadeCount.intValue, styles.shadowsCascadeCounts, styles.shadowsCascadeCountValues);
            if(EditorGUI.EndChangeCheck())
            {
                m_ShadowCascadeCount.intValue = newCascadeCount;
            }

            // Compute max cascade count.
            int maxCascadeCount = 0;
            for (int i = 0; i < targets.Length; ++i)
            {
                CommonSettings settings = targets[i] as CommonSettings;
                maxCascadeCount = Math.Max(maxCascadeCount, settings.shadowCascadeCount);
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < maxCascadeCount - 1; i++)
            {
                EditorGUILayout.PropertyField(m_ShadowCascadeSplits[i], styles.shadowSplits[i]);
            }
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }

        void OnSubsurfaceInspectorGUI()
        {
            EditorGUILayout.LabelField(styles.sssCategory);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_SssProfileFilter1Variance,  styles.sssProfileFilter1Variance);
            EditorGUILayout.PropertyField(m_SssProfileFilter2Variance,  styles.sssProfileFilter2Variance);
            EditorGUILayout.PropertyField(m_SssProfileFilterLerpWeight, styles.sssProfileFilterLerpWeight);
            EditorGUILayout.PropertyField(m_SssBilateralScale,          styles.sssBilateralScale);
            EditorGUI.indentLevel--;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            OnSkyInspectorGUI();
            OnShadowInspectorGUI();
            OnSubsurfaceInspectorGUI();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
