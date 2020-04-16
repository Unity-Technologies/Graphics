﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Drawing.Views;
 using ICSharpCode.NRefactory.Ast;
 using UnityEditor.Experimental.GraphView;
 using UnityEditor.Graphing;
 using UnityEditor.ShaderGraph.Drawing;
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

        public int currentlyDisplayedPropertyCount { get; private set; } = 0;

        protected override string windowTitle => "Inspector";
        protected override string elementName => "InspectorView";
        protected override string styleName => "InspectorView";

        static IEnumerable<Type> GetPropertyDrawerTypes(Assembly assembly)
        {
            foreach(Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(SGPropertyDrawer), true).Length > 0)
                {
                    yield return type;
                }
            }
        }

        public InspectorView(GraphView graphView, Action updatePreviewDelegate) : base(graphView)
        {
            this.m_previewUpdateDelegate = updatePreviewDelegate;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                m_PropertyDrawerList.AddRange(GetPropertyDrawerTypes(assembly));
            }
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
            InspectorUtils.GatherInspectorContent(m_PropertyDrawerList, propertySheet, inspectable, this.TriggerInspectorAndPreviewUpdate, propertyDrawerToUse);
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
            List<Type> rawPropertyDrawerList,
            VisualElement propertySheet,
            IInspectable inspectable,
            Action propertyChangeCallback,
            IPropertyDrawer propertyDrawerToUse = null)
        {
            var registeredPropertyDrawerList = new List<Type>();

            foreach (var type in rawPropertyDrawerList)
            {
                RegisterPropertyDrawer(registeredPropertyDrawerList, type);
            }

            // #TODO: Inspector - Comment out when Matt lands stacks into master
            //RegisterPropertyDrawer(propertyDrawerList, typeof(TargetPropertyDrawer));

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

                if (IsPropertyTypeHandled(registeredPropertyDrawerList, propertyType, out var propertyDrawerTypeToUse))
                {
                    var propertyDrawerInstance = propertyDrawerToUse ??
                                                 (IPropertyDrawer) Activator.CreateInstance(propertyDrawerTypeToUse);
                    // Assign the inspector update delegate so any property drawer can trigger an inspector update if it needs it
                    propertyDrawerInstance.inspectorUpdateDelegate = propertyChangeCallback;
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
