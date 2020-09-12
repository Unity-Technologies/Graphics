using System;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine.UIElements;

namespace Data.Interfaces
{
    // Interface that should be implemented by any property drawer for the inspector view
    public interface IPropertyDrawer
    {
        Action inspectorUpdateDelegate { get; set; }

        VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute);
    }
}
