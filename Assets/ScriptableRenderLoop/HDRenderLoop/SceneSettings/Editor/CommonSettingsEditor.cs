using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    [CustomEditor(typeof(CommonSettings))]
    public class CommonSettingsEditor
        : Editor
    {
        private class Styles
        {
            public readonly GUIContent none = new GUIContent("None");
            public readonly GUIContent skyRenderer = new GUIContent("Sky Renderer");
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

        private List<Type> m_SkyRendererTypes;
        private List<GUIContent> m_SkyRendererTypeNames = new List<GUIContent>();
        private List<int> m_SkyRendererTypeValues = new List<int>();

        void OnEnable()
        {
            m_SkyRendererTypes = Assembly.GetAssembly(typeof(SkyRenderer))
                                .GetTypes()
                                .Where(t => t.IsSubclassOf(typeof(SkyRenderer)) && !t.IsGenericType)
                                .ToList();

            // Prepare the list of available SkyRenderers for the IntPopup
            m_SkyRendererTypeNames.Clear();
            m_SkyRendererTypeValues.Clear();
            for(int i = 0 ; i < m_SkyRendererTypes.Count ; ++i)
            {
                string longName = m_SkyRendererTypes[i].ToString();
                char[] separators = {'.'};
                string[] tokens = longName.Split(separators);
                m_SkyRendererTypeNames.Add(new GUIContent(tokens[tokens.Length - 1]));
                m_SkyRendererTypeValues.Add(i);
            }

            // Add default null value.
            m_SkyRendererTypeNames.Add(styles.none);
            m_SkyRendererTypeValues.Add(m_SkyRendererTypeValues.Count);
            m_SkyRendererTypes.Add(null);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            CommonSettings settings = target as CommonSettings;
            // Retrieve the index of the current SkyRenderer
            int index = -1;
            for(int i = 0 ; i < m_SkyRendererTypeValues.Count ; ++i )
            {
                if(m_SkyRendererTypes[i] == settings.skyRendererType)
                {
                    index = i;
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUILayout.IntPopup(styles.skyRenderer, index, m_SkyRendererTypeNames.ToArray(), m_SkyRendererTypeValues.ToArray());
            if(EditorGUI.EndChangeCheck())
            {
                settings.skyRendererType = m_SkyRendererTypes[newValue];
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
