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

    interface IInspectable
    {
        string displayName { get; }
        PropertySheet GetInspectorContent();
    }
}
