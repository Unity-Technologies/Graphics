using UnityEngine;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering.LWRP
{
    [CustomEditor(typeof(ForwardRendererData), true)]
    public class ForwardRendererDataEditor : ScriptableRendererDataEditor
    {
        private class Styles
        {
            public static readonly GUIContent RendererTitle = new GUIContent("Forward Renderer", "Custom Forward Renderer for LWRP.");
            public static readonly GUIContent OpaqueMask = new GUIContent("Default Layer Mask", "Controls which layers to globally include in the Custom Forward Renderer.");
        }
        
        SerializedProperty m_OpaqueLayerMask;
        SerializedProperty m_TransparentLayerMask;
        
        private void OnEnable()
        {
            m_OpaqueLayerMask = serializedObject.FindProperty("m_OpaqueLayerMask");
            m_TransparentLayerMask = serializedObject.FindProperty("m_TransparentLayerMask");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.RendererTitle, EditorStyles.boldLabel); // Title
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_OpaqueLayerMask, Styles.OpaqueMask);
            if (EditorGUI.EndChangeCheck())  // We copy the opaque mask to the transparent mask, later we might expose both
                m_TransparentLayerMask.intValue = m_OpaqueLayerMask.intValue;
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
            
            base.OnInspectorGUI(); // Draw the base UI, contains ScriptableRenderFeatures list
        }
    }
}
