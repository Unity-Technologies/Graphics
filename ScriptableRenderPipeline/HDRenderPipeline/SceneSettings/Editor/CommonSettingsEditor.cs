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
            public readonly GUIContent maxShadowDistance = new GUIContent("Maximum shadow distance");
            public readonly GUIContent nearPlaneOffset = new GUIContent("Shadow near plane offset");
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

        private SerializedProperty m_ShadowMaxDistance;
        private SerializedProperty m_ShadowNearPlaneOffset;

        void OnEnable()
        {
            m_ShadowMaxDistance = serializedObject.FindProperty("m_Settings.m_ShadowMaxDistance");
            m_ShadowNearPlaneOffset = serializedObject.FindProperty("m_Settings.m_ShadowNearPlaneOffset");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_ShadowMaxDistance, styles.maxShadowDistance);
            EditorGUILayout.PropertyField(m_ShadowNearPlaneOffset, styles.nearPlaneOffset);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
