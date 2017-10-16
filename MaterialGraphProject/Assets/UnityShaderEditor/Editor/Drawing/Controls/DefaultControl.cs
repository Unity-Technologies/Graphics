using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.MaterialGraph;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultControlAttribute : Attribute, IControlAttribute
    {
        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (propertyInfo.PropertyType == typeof(Color))
                return new ColorControlView(null, node, propertyInfo);
            if (typeof(Enum).IsAssignableFrom(propertyInfo.PropertyType))
                return new EnumControlView(null, node, propertyInfo);
            if (propertyInfo.PropertyType == typeof(Gradient))
                return new GradientControlView(null, node, propertyInfo);
            if (MultiFloatControlView.validTypes.Contains(propertyInfo.PropertyType))
                return new MultiFloatControlView(null, "X", "Y", "Z", "W", node, propertyInfo);
            if (typeof(Object).IsAssignableFrom(propertyInfo.PropertyType))
                return new ObjectControlView(null, node, propertyInfo);
            return null;
        }
    }
}
