using System;
using System.Reflection;
using Data.Interfaces;
using Drawing.Inspector;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    interface IInspectable
    {
        // Implementors can override this in order to display their desired string when selected and viewed through the inscetor
        string inspectorTitle { get; }

        // This function should return the underlying data object that user wishes to expose to the Inspector
        object GetObjectToInspect();

        // This function should return the property information of whatever object has been marked up for metadata gathering by the inspector
        // This might be the same as the object returned by GetObjectToInspect(), it might not
        PropertyInfo[] GetPropertyInfo();

        // Used to provide any data needed by the property drawer from the inspectable
        // The inspectorUpdateDelegate is used to trigger an inspector update
        void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate);
    }
}
