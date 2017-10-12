using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Controls
{
    public class Vector2ControlAttribute : Attribute, IControlAttribute
    {
        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new Vector2ControlView(() => (Vector2) propertyInfo.GetValue(node, null), (value) => propertyInfo.SetValue(node, value, null));
        }
    }

    public class Vector2ControlView : VisualElement
    {
        public Vector2ControlView(Func<Vector2> getter, Action<Vector2> setter)
        {
            Add(new IMGUIContainer(() => setter(EditorGUILayout.Vector2Field("", getter()))));
        }
    }
}
