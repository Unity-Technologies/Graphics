using System;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class Inspectable : Attribute
    {
        public string _labelName;
        public object _defaultValue;
        public Inspectable(string labelName, object defaultValue)
        {
            this._labelName = labelName;
            this._defaultValue = defaultValue;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SGPropertyDrawer : Attribute
    {
        public Type _propertyType;
        public SGPropertyDrawer(Type _propertyType)
        {
            this._propertyType = _propertyType;
        }
    }


    interface IInspectable
    {
        string displayName { get; }
        PropertySheet GetInspectorContent();
    }
}
