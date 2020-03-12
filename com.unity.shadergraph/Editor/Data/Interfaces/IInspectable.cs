using System;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    [AttributeUsage(AttributeTargets.Property)]
    public class Inspectable : Attribute
    {
        // String value to use in the Property name TextLabel
        public string labelName { get; private set; }

        // The default value of this property
        public object defaultValue { get; private set; }

        // String value to supply if you wish to use a custom style when drawing this property
        public string customStyleName { get; private set; }

        public Inspectable(string labelName, object defaultValue, string customStyleName = "")
        {
            this.labelName = labelName;
            this.defaultValue = defaultValue;
            this.customStyleName = customStyleName;
        }
    }

    interface IInspectable
    {
        string displayName { get; }
        object GetUnderlyingObject();
    }
}
