using System;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    [AttributeUsage(AttributeTargets.Property)]
    public class Inspectable : Attribute
    {
        public string LabelName { get; set; }

        public object DefaultValue { get; set; }

        public Inspectable(string labelName, object defaultValue)
        {
            this.LabelName = labelName;
            this.DefaultValue = defaultValue;
        }

    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SGPropertyDrawer : Attribute
    {
        public Type PropertyType { get; set; }

        public SGPropertyDrawer(Type propertyType)
        {
            this.PropertyType = propertyType;
        }
    }


    interface IInspectable
    {
        string displayName { get; }
        PropertySheet GetInspectorContent();
    }
}
