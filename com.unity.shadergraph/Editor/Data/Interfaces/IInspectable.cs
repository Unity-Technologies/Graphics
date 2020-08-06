using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    interface IInspectable
    {
        // Implementors can override this in order to display their desired string when selected and viewed through the inspector
        string inspectorTitle { get; }

        // This function should return the underlying data object that user wishes to expose to the Inspector
        // For simple data properties like Integers/Floats this is the object that contains the properties
        // For complex types like GraphData this is the GraphData itself, its up to the PropertyDrawer to define what it needs
        object GetObjectToInspect();

        // Used to provide any data needed by the property drawer from the inspectable
        // The inspectorUpdateDelegate is used to trigger an inspector update
        void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate);
    }
}
