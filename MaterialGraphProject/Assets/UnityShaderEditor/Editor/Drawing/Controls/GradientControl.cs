using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Controls
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

    public class GradientControlView : VisualElement
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
            Add(new IMGUIContainer(OnGUIHandler));
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
                    m_PropertyInfo.SetValue(m_Node, m_GradientObject.gradient, null);
            }

            var e = Event.current;

            if (EditorWindow.focusedWindow != null && m_PrevWindow != EditorWindow.focusedWindow.ToString() && EditorWindow.focusedWindow.ToString() != "(UnityEditor.GradientPicker)")
            {
                m_PropertyInfo.SetValue(m_Node, m_GradientObject.gradient, null);
                m_PrevWindow = EditorWindow.focusedWindow.ToString();
                Debug.Log("Update Gradient Shader");
            }
        }
    }
}
