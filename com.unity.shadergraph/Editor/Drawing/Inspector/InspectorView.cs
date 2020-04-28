﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Drawing.Views;
 using ICSharpCode.NRefactory.Ast;
 using UnityEditor;
 using UnityEditor.Experimental.GraphView;
 using UnityEditor.Graphing;
 using UnityEditor.ShaderGraph.Drawing;
 using UnityEngine;
 using UnityEngine.UIElements;

 namespace Drawing.Inspector
{
    class InspectorView : GraphSubWindow
    {
        // References
        readonly List<Type> m_PropertyDrawerList = new List<Type>();

        // There's persistent data that is stored in the graph settings property drawer that we need to hold onto between interactions
        private IPropertyDrawer m_graphSettingsPropertyDrawer = new GraphDataPropertyDrawer();

        private Action m_previewUpdateDelegate;
        protected override string windowTitle => "Inspector";
        protected override string elementName => "InspectorView";
        protected override string styleName => "InspectorView";

        void RegisterPropertyDrawer(Type propertyDrawerType)
        {
            if(typeof(IPropertyDrawer).IsAssignableFrom(propertyDrawerType) == false)
                Debug.Log("Attempted to register a property drawer that doesn't inherit from IPropertyDrawer!");

            var customAttribute = propertyDrawerType.GetCustomAttribute<SGPropertyDrawer>();
            if(customAttribute != null)
                m_PropertyDrawerList.Add(propertyDrawerType);
            else
                Debug.Log("Attempted to register a property drawer that isn't marked up with the SGPropertyDrawer attribute!");
        }

        public InspectorView(GraphView graphView, Action updatePreviewDelegate) : base(graphView)
        {
            this.m_previewUpdateDelegate = updatePreviewDelegate;

            var unregisteredPropertyDrawerTypes = TypeCache.GetTypesDerivedFrom<IPropertyDrawer>().ToList();

            foreach (var type in unregisteredPropertyDrawerTypes)
            {
                RegisterPropertyDrawer(type);
            }
        }

#region Selection
        public void Update()
        {
            // Remove current properties
            for (int i = 0; i < m_ContentContainer.childCount; ++i)
            {
                var child = m_ContentContainer.Children().ElementAt(i);
                m_ContentContainer.Remove(child);
            }

            if(selection.Count == 0)
            {
                ShowGraphSettings(m_ContentContainer);
            }
            else if(selection.Count == 1)
            {
                var inspectable = selection.First() as IInspectable;
                subTitle = $"{inspectable?.inspectorTitle}.";
            }
            else if(selection.Count > 1)
            {
                subTitle = $"{selection.Count} Objects.";
            }

            try
            {
                foreach (var selectable in selection)
                {
                    DrawSelection(selectable, m_ContentContainer);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            m_ContentContainer.MarkDirtyRepaint();
        }

        private void DrawSelection(ISelectable selectable, VisualElement outputVisualElement)
        {
            if(selectable is IInspectable inspectable)
            {
                DrawInspectable(outputVisualElement, inspectable);
            }
        }

        private void DrawInspectable(
            VisualElement outputVisualElement,
            IInspectable inspectable,
            IPropertyDrawer propertyDrawerToUse = null)
        {
            InspectorUtils.GatherInspectorContent(m_PropertyDrawerList, outputVisualElement, inspectable, this.TriggerInspectorAndPreviewUpdate, propertyDrawerToUse);
        }

        void TriggerInspectorAndPreviewUpdate()
        {
            this.m_previewUpdateDelegate();
            this.Update();
        }

        // This should be implemented by any inspector class that wants to define its own GraphSettings
        // which for SG, is a representation of the settings in GraphData
        protected virtual void ShowGraphSettings(VisualElement contentContainer)
        {
            var graphEditorView = m_GraphView.GetFirstAncestorOfType<GraphEditorView>();
            if(graphEditorView == null)
                return;

            subTitle = $"{graphEditorView.assetName} (Graph)";

            DrawInspectable(contentContainer, (IInspectable)graphView, m_graphSettingsPropertyDrawer);
        }
#endregion
    }


    public static class InspectorUtils
    {
        internal static void GatherInspectorContent(
            List<Type> propertyDrawerList,
            VisualElement outputVisualElement,
            IInspectable inspectable,
            Action propertyChangeCallback,
            IPropertyDrawer propertyDrawerToUse = null)
        {
            var dataObject = inspectable.GetObjectToInspect();
            if (dataObject == null)
                throw new NullReferenceException("DataObject returned by Inspectable is null!");

            var properties = inspectable.GetPropertyInfo();
            if (properties == null)
                throw new NullReferenceException("PropertyInfos returned by Inspectable is null!");

            foreach (var propertyInfo in properties)
            {
                var attribute = propertyInfo.GetCustomAttribute<InspectableAttribute>();
                if (attribute == null)
                    continue;

                var propertyType = propertyInfo.PropertyType;

                if (IsPropertyTypeHandled(propertyDrawerList, propertyType, out var propertyDrawerTypeToUse))
                {
                    var propertyDrawerInstance = propertyDrawerToUse ??
                                                 (IPropertyDrawer) Activator.CreateInstance(propertyDrawerTypeToUse);
                    // Assign the inspector update delegate so any property drawer can trigger an inspector update if it needs it
                    propertyDrawerInstance.inspectorUpdateDelegate = propertyChangeCallback;
                    // Supply any required data to this particular kind of property drawer
                    inspectable.SupplyDataToPropertyDrawer(propertyDrawerInstance, propertyChangeCallback);
                    var propertyGUI = propertyDrawerInstance.DrawProperty(propertyInfo, dataObject, attribute);
                    outputVisualElement.Add(propertyGUI);
                }
            }
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
