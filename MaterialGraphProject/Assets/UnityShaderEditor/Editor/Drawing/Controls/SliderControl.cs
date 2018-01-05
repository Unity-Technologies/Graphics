using System;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SliderControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public SliderControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new SliderControlView(m_Label, node, propertyInfo);
        }
    }

    public class SliderControlView : VisualElement, INodeModificationListener
    {
        GUIContent m_Label;
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        IMGUIContainer m_Container;

        public SliderControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            if (propertyInfo.PropertyType != typeof(Vector3))
                throw new ArgumentException("Property must be of type Vector3.", "propertyInfo");
            m_Label = new GUIContent(label ?? ObjectNames.NicifyVariableName(propertyInfo.Name));
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
            var value = (Vector3)m_PropertyInfo.GetValue(m_Node, null);

            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                var slider = EditorGUILayout.Slider(value.x, value.y, value.z);
                GUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 32f;
                var minField = EditorGUILayout.FloatField("Min", value.y);
                var maxField = EditorGUILayout.FloatField("Max", value.z);
                GUILayout.EndHorizontal();

                if (changeCheckScope.changed)
                {
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                    m_PropertyInfo.SetValue(m_Node, new Vector3(slider, minField, maxField), null);
                }
            }
        }
    }
}
