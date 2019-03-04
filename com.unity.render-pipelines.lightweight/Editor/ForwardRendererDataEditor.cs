using UnityEngine;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering.LWRP
{
    [CustomEditor(typeof(ForwardRendererData), true)]
    public class ForwardRendererDataEditor : ScriptableRendererDataEditor
    {
        private class Styles
        {
            //Title
            public static GUIContent rendererTitle = new GUIContent("Forward Renderer");
            public static GUIContent opaqueMask = new GUIContent("Default Layer Mask", "Null.");
        }
        
        private SerializedProperty m_OpaqueLayerMask;
        private SerializedProperty m_TransparentLayerMask;
        
        private void OnEnable()
        {
            m_OpaqueLayerMask = serializedObject.FindProperty("m_OpaqueLayerMask");
            m_TransparentLayerMask = serializedObject.FindProperty("m_TransparentLayerMask");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField(Styles.rendererTitle, EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_OpaqueLayerMask, Styles.opaqueMask);
            if (EditorGUI.EndChangeCheck())
                m_TransparentLayerMask.intValue = m_OpaqueLayerMask.intValue;

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
    }
}
