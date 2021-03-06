using System;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    // Interface that should be implemented by any property drawer for the inspector view
    interface IPropertyDrawer
    {
        Action<InspectorUpdateSource> inspectorUpdateDelegate { get; set; }

        VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute);
    }
}
