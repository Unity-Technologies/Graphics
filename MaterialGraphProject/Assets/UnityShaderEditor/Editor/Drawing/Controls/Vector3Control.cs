using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Controls
{
    public class Vector3ControlAttribute : Attribute, IControlAttribute
    {
        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new Vector3ControlView(() => (Vector3) propertyInfo.GetValue(node, null), (value) => propertyInfo.SetValue(node, value, null));
        }
    }

    public class Vector3ControlView : VisualElement
    {
        public Vector3ControlView(Func<Vector3> getter, Action<Vector3> setter)
        {
            Add(new IMGUIContainer(() => setter(EditorGUILayout.Vector3Field("", getter()))));
        }
    }
}
