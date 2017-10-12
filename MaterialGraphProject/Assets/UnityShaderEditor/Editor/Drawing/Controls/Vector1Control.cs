using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Controls
{
    public class Vector1ControlAttribute : Attribute, IControlAttribute
    {
        readonly string m_Label;

        public Vector1ControlAttribute(string label = "")
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new Vector1ControlView(
                () => (float) propertyInfo.GetValue(node, null),
                (value) => propertyInfo.SetValue(node, value, null),
                m_Label);
        }
    }

    public class Vector1ControlView : VisualElement
    {
        public Vector1ControlView(Func<float> getter, Action<float> setter, string label)
        {
            Add(new IMGUIContainer(() => setter(EditorGUILayout.FloatField(label, getter()))));
        }
    }
}
