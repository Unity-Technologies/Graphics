using System;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    [AttributeUsage(AttributeTargets.Property)]
    public class Inspectable : Attribute
    {
        public string labelName { get; private set; }

        public object defaultValue { get; private set; }

        public Inspectable(string labelName, object defaultValue)
        {
            this.labelName = labelName;
            this.defaultValue = defaultValue;
        }

    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SGPropertyDrawer : Attribute
    {
        public Type propertyType { get; private set; }

        public SGPropertyDrawer(Type propertyType)
        {
            this.propertyType = propertyType;
        }
    }


    interface IInspectable
    {
        string displayName { get; }
        object GetUnderlyingObject();
    }
}
