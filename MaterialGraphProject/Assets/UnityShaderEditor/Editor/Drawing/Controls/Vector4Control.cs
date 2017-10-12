using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Controls
{
    public class Vector4ControlAttribute : Attribute, IControlAttribute
    {
        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new Vector4ControlView(() => (Vector4) propertyInfo.GetValue(node, null), (value) => propertyInfo.SetValue(node, value, null));
        }
    }

    public class Vector4ControlView : VisualElement
    {
        public Vector4ControlView(Func<Vector4> getter, Action<Vector4> setter)
        {
            Add(new IMGUIContainer(() => setter(EditorGUILayout.Vector4Field("", getter()))));
        }
    }
}
