using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Controls
{
    public class ColorControlAttribute : Attribute, IControlAttribute
    {
        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new ColorControlView(() => (Color) propertyInfo.GetValue(node, null), (value) => propertyInfo.SetValue(node, value, null));
        }
    }

    public class ColorControlView : VisualElement
    {
        readonly Func<Color> m_Getter;
        readonly Action<Color> m_Setter;

        public ColorControlView(Func<Color> getter, Action<Color> setter)
        {
            m_Getter = getter;
            m_Setter = setter;
            Add(new IMGUIContainer(OnGUIHandler));
        }

        void OnGUIHandler()
        {
            var value = m_Getter();
            var newValue = EditorGUILayout.ColorField("", value);
            if (newValue != value)
                m_Setter(newValue);
        }
    }
}
