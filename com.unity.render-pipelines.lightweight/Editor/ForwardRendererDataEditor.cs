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
            //LayerMasks
            public static GUIContent layerMasks = new GUIContent("Default Layer Masks", "Null.");
            public static GUIContent opaqueMask = new GUIContent("Opaque", "Null.");
            public static GUIContent transparentMask = new GUIContent("Transparent", "Null.");
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
            
            EditorGUILayout.LabelField(Styles.layerMasks);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_OpaqueLayerMask, Styles.opaqueMask);
            EditorGUILayout.PropertyField(m_TransparentLayerMask, Styles.transparentMask);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            
            base.OnInspectorGUI();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
