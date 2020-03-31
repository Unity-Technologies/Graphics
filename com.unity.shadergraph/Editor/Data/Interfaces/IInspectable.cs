using System;
using System.Reflection;
using Drawing.Inspector;

namespace Data.Interfaces
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

        // This function should return the underlying data object that the inspector needs to represent the state of
        // The type of that object also determines what PropertyDrawer the inspector uses,
        // so make sure that whatever type you return here, there's a matching PropertyDrawer defined and registered with the InspectorView you're using.
        object GetObjectToInspect();

        //
        PropertyInfo[] GetPropertyInfo();

        // Used to provide any data needed by the property drawer from the inspectable
        // The inspectorUpdateDelegate is used to trigger an inspector update
        void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate);
    }
}
