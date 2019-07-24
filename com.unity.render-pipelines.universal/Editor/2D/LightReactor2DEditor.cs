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
            public static GUIContent shadowCasterGroup = EditorGUIUtility.TrTextContent("Shadow Caster Group", "Shadow Caster Group");
        }


        SerializedProperty m_ShadowCasterGroup;

        public void OnEnable()
        {
            ShadowCaster2DOnEnable();

            m_ShadowCasterGroup = serializedObject.FindProperty("m_ShadowCasterGroup");
        }

        public void OnSceneGUI()
        {
            ShadowCaster2DSceneGUI();
        }

        public override void OnInspectorGUI()
        {
            //EditorGUILayout.PropertyField(m_ShadowCasterGroup, Styles.shadowCasterGroup);

            ShadowCaster2DInspectorGUI<LightReactor2DShadowCasterShapeTool>();
        }
    }
}
