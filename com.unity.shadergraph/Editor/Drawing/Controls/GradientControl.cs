using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Experimental.UIElements;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class GradientControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public GradientControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new GradientControlView(m_Label, node, propertyInfo);
        }
    }

    [Serializable]
    public class GradientObject : ScriptableObject
    {
        public Gradient gradient = new Gradient();
    }

    public class GradientControlView : VisualElement, INodeModificationListener
    {
        GUIContent m_Label;

        AbstractMaterialNode m_Node;

        PropertyInfo m_PropertyInfo;

        string m_PrevWindow = "";

        [SerializeField]
        GradientObject m_GradientObject;

        [SerializeField]
        SerializedObject m_SerializedObject;

        [SerializeField]
        SerializedProperty m_SerializedProperty;

        IMGUIContainer m_Container;

        public GradientControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Label = new GUIContent(label ?? ObjectNames.NicifyVariableName(propertyInfo.Name));
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            if (propertyInfo.PropertyType != typeof(Gradient))
                throw new ArgumentException("Property must be of type Gradient.", "propertyInfo");
            m_GradientObject = ScriptableObject.CreateInstance<GradientObject>();
            m_GradientObject.gradient = new Gradient();
            m_SerializedObject = new SerializedObject(m_GradientObject);
            m_SerializedProperty = m_SerializedObject.FindProperty("gradient");
            m_Container = new IMGUIContainer(OnGUIHandler);
            Add(m_Container);
        }

        public void OnNodeModified(ModificationScope scope)
        {
            if (scope == ModificationScope.Graph)
                m_Container.Dirty(ChangeType.Repaint);
        }

        void OnGUIHandler()
        {
            m_SerializedObject.Update();
            var gradient = (Gradient)m_PropertyInfo.GetValue(m_Node, null);
            m_GradientObject.gradient.SetKeys(gradient.colorKeys, gradient.alphaKeys);


            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_SerializedProperty, m_Label, true, null);
                m_SerializedObject.ApplyModifiedProperties();
                if (changeCheckScope.changed)
                {
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                    m_PropertyInfo.SetValue(m_Node, m_GradientObject.gradient, null);
                }
            }

            var e = Event.current;

            if (EditorWindow.focusedWindow != null && m_PrevWindow != EditorWindow.focusedWindow.ToString() && EditorWindow.focusedWindow.ToString() != "(UnityEditor.GradientPicker)")
            {
                m_PrevWindow = EditorWindow.focusedWindow.ToString();
            }
        }
    }
}