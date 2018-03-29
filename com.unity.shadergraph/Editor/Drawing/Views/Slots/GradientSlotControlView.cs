using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class GradientSlotControlView : VisualElement, INodeModificationListener
    {
        GradientInputMaterialSlot m_Slot;

        [SerializeField]
        GradientObject m_GradientObject;

        [SerializeField]
        SerializedObject m_SerializedObject;

        [SerializeField]
        SerializedProperty m_SerializedProperty;

        IMGUIContainer m_Container;

        public GradientSlotControlView(GradientInputMaterialSlot slot)
        {
            m_Slot = slot;
            m_GradientObject = ScriptableObject.CreateInstance<GradientObject>();
            m_GradientObject.gradient = new Gradient();
            m_SerializedObject = new SerializedObject(m_GradientObject);
            m_SerializedProperty = m_SerializedObject.FindProperty("gradient");
            m_Container = new IMGUIContainer(OnGUIHandler);
            Add(m_Container);
        }

        public void OnNodeModified(ModificationScope scope)
        {
            if (scope == ModificationScope.Node)
                m_Container.Dirty(ChangeType.Repaint);
        }

        void OnGUIHandler()
        {
            m_SerializedObject.Update();
            m_GradientObject.gradient.SetKeys(m_Slot.value.colorKeys, m_Slot.value.alphaKeys);
            m_GradientObject.gradient.mode = m_Slot.value.mode;

            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_SerializedProperty, new GUIContent(""), true, null);
                m_SerializedObject.ApplyModifiedProperties();
                if (changeCheckScope.changed)
                {
                    m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change Gradient");
                    m_Slot.value = m_GradientObject.gradient;
                    m_Slot.owner.Dirty(ModificationScope.Node);
                    m_Container.Dirty(ChangeType.Repaint);
                }
            }
        }
    }
}