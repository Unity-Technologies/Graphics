using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(MeshRenderer))]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [CanEditMultipleObjects]
    internal class Renderer2DMeshEditor : MeshRendererEditor
    {
        SerializedProperty m_MaskInteraction;
        SavedBool m_2DFoldout;
        class Styles
        {
            public static readonly GUIContent maskInteractionLabel = EditorGUIUtility.TrTextContent("Mask Interaction", "Renderer's interaction with a Sprite Mask");
        }


        public override void OnEnable()
        {
            base.OnEnable();
            m_MaskInteraction = serializedObject.FindProperty("m_MaskInteraction");
            m_2DFoldout = new SavedBool($"{target.GetType()}.MeshEditor2DFoldout", true);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            var rpAsset = UniversalRenderPipeline.asset;
            if (rpAsset != null && (rpAsset.scriptableRenderer is Renderer2D))
            {
                m_2DFoldout.value = EditorGUILayout.Foldout(m_2DFoldout.value, "2D");
                if (m_2DFoldout)
                {
                    EditorGUI.indentLevel++;
                    m_MaskInteraction.intValue = Convert.ToInt32(EditorGUILayout.EnumPopup(Styles.maskInteractionLabel, (SpriteMaskInteraction)m_MaskInteraction.intValue));
                    EditorGUI.indentLevel--;
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
