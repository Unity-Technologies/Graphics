using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultControlAttribute : Attribute, IControlAttribute
    {
        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (MultiFloatControlView.validTypes.Contains(propertyInfo.PropertyType))
                return new MultiFloatControlView(null, "X", "Y", "Z", "W", node, propertyInfo);
            return null;
        }
    }
}
