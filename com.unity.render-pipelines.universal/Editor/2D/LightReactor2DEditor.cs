using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.Rendering.Universal.Path2D;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomEditor(typeof(LightReactor2D))]
    [CanEditMultipleObjects]
    internal class LightReactor2DEditor : ShadowCaster2DEditor
    {
        [EditorTool("Edit Shadow Caster Shape", typeof(LightReactor2D))]
        class LightReactor2DShadowCasterShapeTool : ShadowCaster2DShapeTool { };

        private static class Styles
        {
            public static GUIContent shadowCasterGroup = EditorGUIUtility.TrTextContent("Shadow Caster Group", "Shadow casters in the same group will not shadow each other");
            public static GUIContent castsShadows = EditorGUIUtility.TrTextContent("Casts Shadows", "Specifies if this renderer will cast shadows");
            public static GUIContent receivesShadows = EditorGUIUtility.TrTextContent("Receive Shadows", "Specifies if this renderer will receive shadows");
        }


        SerializedProperty m_ShadowCasterGroup;
        SerializedProperty m_CastsShadows;
        SerializedProperty m_ReceivesShadows;

        public void OnEnable()
        {
            ShadowCaster2DOnEnable();

            m_ShadowCasterGroup = serializedObject.FindProperty("m_ShadowGroup");
            m_CastsShadows = serializedObject.FindProperty("m_CastsShadows");
            m_ReceivesShadows = serializedObject.FindProperty("m_ReceivesShadows");
        }

        public void OnSceneGUI()
        {
            ShadowCaster2DSceneGUI();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_ShadowCasterGroup, Styles.shadowCasterGroup);
            EditorGUILayout.PropertyField(m_CastsShadows, Styles.castsShadows);
            EditorGUILayout.PropertyField(m_ReceivesShadows, Styles.receivesShadows);
            serializedObject.ApplyModifiedProperties();

            ShadowCaster2DInspectorGUI<LightReactor2DShadowCasterShapeTool>();
        }
    }
}
