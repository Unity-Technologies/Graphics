﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Data.Interfaces;
using Drawing.Views;
using UnityEditor.Experimental.GraphView;
 using UnityEditor.ShaderGraph.Drawing;
 using UnityEngine.UIElements;

 namespace Drawing.Inspector
{
    class InspectorView : GraphSubWindow
    {
        // References
        readonly IList<Type> m_PropertyDrawerList = new List<Type>();

        // There's persistent data that is stored in the graph settings property drawer that we need to hold onto between interactions
        private IPropertyDrawer m_graphSettingsPropertyDrawer = new GraphDataPropertyDrawer();

        private Action m_previewUpdateDelegate;

        public int currentlyDisplayedPropertyCount { get; private set; } = 0;

        protected override string windowTitle => "Inspector";
        protected override string elementName => "InspectorView";
        protected override string styleName => "InspectorView";

        public InspectorView(GraphView graphView, Action updatePreviewDelegate) : base(graphView)
        {
            this.m_previewUpdateDelegate = updatePreviewDelegate;
        }

#region Selection
        public void Update()
        {
            currentlyDisplayedPropertyCount = selection.Count;

            // Remove current properties
            for (int i = 0; i < m_ContentContainer.childCount; ++i)
            {
                var child = m_ContentContainer.Children().ElementAt(i);
                m_ContentContainer.Remove(child);
            }

            var propertySheet = new PropertySheet();
            if(selection.Count == 0)
            {
                ShowGraphSettings(propertySheet);
            }

            if(selection.Count > 1)
            {
                subTitle = $"{selection.Count} Objects.";
            }

            try
            {
                foreach (var selectable in selection)
                {
                    DrawSelection(selectable, propertySheet);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            m_ContentContainer.Add(propertySheet);
            m_ContentContainer.MarkDirtyRepaint();
        }

        private void DrawSelection(ISelectable selectable, PropertySheet propertySheet)
        {
            if(selectable is IInspectable inspectable)
            {
                DrawInspectable(propertySheet, inspectable);
            }
        }

        private void DrawInspectable(PropertySheet propertySheet, IInspectable inspectable, IPropertyDrawer propertyDrawerToUse = null)
        {
            InspectorUtils.GatherInspectorContent(propertySheet, inspectable, this.TriggerInspectorAndPreviewUpdate, propertyDrawerToUse);
        }

        void TriggerInspectorAndPreviewUpdate()
        {
            this.m_previewUpdateDelegate();
            this.Update();
        }

        // This should be implemented by any inspector class that wants to define its own GraphSettings
        // which for SG, is a representation of the settings in GraphData
        protected virtual void ShowGraphSettings(PropertySheet propertySheet)
        {
            var graphEditorView = m_GraphView.GetFirstAncestorOfType<GraphEditorView>();
            if(graphEditorView == null)
                return;

            subTitle = $"{graphEditorView.assetName} (Graph)";

            DrawInspectable(propertySheet, (IInspectable)graphView, m_graphSettingsPropertyDrawer);
        }
#endregion
    }


    public static class InspectorUtils
    {
        internal static void GatherInspectorContent(
            VisualElement propertySheet,
            IInspectable inspectable,
            Action propertyChangeCallback,
            IPropertyDrawer propertyDrawerToUse = null)
        {
            var propertyDrawerList = new List<Type>();

            // Register property drawer types here
            RegisterPropertyDrawer(propertyDrawerList, typeof(BoolPropertyDrawer));
            RegisterPropertyDrawer(propertyDrawerList, typeof(ToggleDataPropertyDrawer));
            RegisterPropertyDrawer(propertyDrawerList, typeof(EnumPropertyDrawer));
            RegisterPropertyDrawer(propertyDrawerList, typeof(TextPropertyDrawer));
            RegisterPropertyDrawer(propertyDrawerList, typeof(Vector2PropertyDrawer));
            RegisterPropertyDrawer(propertyDrawerList, typeof(Vector3PropertyDrawer));
            RegisterPropertyDrawer(propertyDrawerList, typeof(Vector4PropertyDrawer));
            RegisterPropertyDrawer(propertyDrawerList, typeof(MatrixPropertyDrawer));
            RegisterPropertyDrawer(propertyDrawerList, typeof(ColorPropertyDrawer));
            RegisterPropertyDrawer(propertyDrawerList, typeof(GradientPropertyDrawer));
            RegisterPropertyDrawer(propertyDrawerList, typeof(Texture2DPropertyDrawer));
            RegisterPropertyDrawer(propertyDrawerList, typeof(Texture2DArrayPropertyDrawer));
            RegisterPropertyDrawer(propertyDrawerList, typeof(Texture3DPropertyDrawer));
            RegisterPropertyDrawer(propertyDrawerList, typeof(CubemapPropertyDrawer));
            RegisterPropertyDrawer(propertyDrawerList, typeof(ShaderInputPropertyDrawer));
            RegisterPropertyDrawer(propertyDrawerList, typeof(GraphDataPropertyDrawer));
            RegisterPropertyDrawer(propertyDrawerList, typeof(TargetPropertyDrawer));

            var dataObject = inspectable.GetObjectToInspect();
            if (dataObject == null)
                throw new NullReferenceException("DataObject returned by Inspectable is null!");

            var properties = inspectable.GetPropertyInfo();
            if (properties == null)
                throw new NullReferenceException("PropertyInfos returned by Inspectable is null!");

            foreach (var propertyInfo in properties)
            {
                var attribute = propertyInfo.GetCustomAttribute<Inspectable>();
                if (attribute == null)
                    continue;

                var propertyType = propertyInfo.PropertyType;

                if(IsPropertyTypeHandled(propertyDrawerList, propertyType, out var propertyDrawerTypeToUse))
                {
                    var propertyDrawerInstance = propertyDrawerToUse ?? (IPropertyDrawer) Activator.CreateInstance(propertyDrawerTypeToUse);
                    // Supply any required data to this particular kind of property drawer
                    inspectable.SupplyDataToPropertyDrawer(propertyDrawerInstance, propertyChangeCallback);
                    var propertyGUI = propertyDrawerInstance.DrawProperty(propertyInfo, dataObject, attribute);
                    propertySheet.Add(propertyGUI);
                }
            }
        }

        private static void RegisterPropertyDrawer(List<Type> propertyDrawerList, Type propertyDrawerType)
        {
            if(typeof(IPropertyDrawer).IsAssignableFrom(propertyDrawerType) == false)
                throw new Exception("Attempted to register a property drawer that doesn't inherit from IPropertyDrawer!");

            var customAttribute = propertyDrawerType.GetCustomAttribute<SGPropertyDrawer>();
            if(customAttribute != null)
                propertyDrawerList.Add(propertyDrawerType);
            else
                throw new Exception("Attempted to register a property drawer that isn't marked up with the SGPropertyDrawer attribute!");
        }

        private static bool IsPropertyTypeHandled(List<Type> propertyDrawerList, Type typeOfProperty,
            out Type propertyDrawerToUse)
        {
            propertyDrawerToUse = null;

            // Check to see if a property drawer has been registered that handles this type
            foreach (var propertyDrawerType in propertyDrawerList)
            {
                var typeHandledByPropertyDrawer = propertyDrawerType.GetCustomAttribute<SGPropertyDrawer>();
                // Numeric types and boolean wrapper types like ToggleData handled here
                if (typeHandledByPropertyDrawer.propertyType == typeOfProperty)
                {
                    propertyDrawerToUse = propertyDrawerType;
                    return true;
                }
                // Generics and Enumerable types are handled here
                else if (typeHandledByPropertyDrawer.propertyType.IsAssignableFrom(typeOfProperty))
                {
                    propertyDrawerToUse = propertyDrawerType;
                    return true;
                }
                // Enums are weird and need to be handled explicitly as done below as their runtime type isn't the same as System.Enum
                else if (typeHandledByPropertyDrawer.propertyType == typeOfProperty.BaseType)
                {
                    propertyDrawerToUse = propertyDrawerType;
                    return true;
                }
            }

            return false;
        }
    }
}
