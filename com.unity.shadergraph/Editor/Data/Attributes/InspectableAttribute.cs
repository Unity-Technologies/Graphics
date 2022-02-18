using System;

namespace UnityEditor.ShaderGraph.Drawing
{
    [AttributeUsage(AttributeTargets.Property)]
    public class InspectableAttribute : Attribute
    {
        // String value to use in the Property name TextLabel
        public string labelName { get; private set; }

        // The default value of this property
        public object defaultValue { get; private set; }

        // String value to supply if you wish to use a custom style when drawing this property
        public string customStyleName { get; private set; }

        public InspectableAttribute(string labelName, object defaultValue, string customStyleName = "")
        {
            this.labelName = labelName;
            this.defaultValue = defaultValue;
            this.customStyleName = customStyleName;
        }
    }
}
