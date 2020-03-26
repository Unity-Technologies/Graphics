using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Drawing.Views;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Drawing.Inspector
{
    class InspectorView : GraphSubWindow
    {
        // References
        readonly GraphData m_GraphData;
        readonly IList<Type> m_PropertyDrawerList = new List<Type>();
        private List<ISelectable> m_SelectionList = null;

        protected override string windowTitle => "Inspector";
        protected override string elementName => "InspectorView";
        protected override string styleName => "InspectorView";

        public InspectorView(GraphData graphData, GraphView graphView) : base(graphView)
        {
            m_GraphData = graphData;

            // Register property drawer types here
            RegisterPropertyDrawer(typeof(BoolPropertyDrawer));
            RegisterPropertyDrawer(typeof(EnumPropertyDrawer));
            RegisterPropertyDrawer(typeof(ShaderInputPropertyDrawer));
        }

#region PropertyDrawing
        void RegisterPropertyDrawer(Type propertyDrawerType)
        {
            // #TODO: Look into the right way to warn the user that there are errors they should probably be aware of

            if(typeof(IPropertyDrawer).IsAssignableFrom(propertyDrawerType) == false)
                throw new Exception("Attempted to register a property drawer that doesn't inherit from IPropertyDrawer!");

            var customAttribute = propertyDrawerType.GetCustomAttribute<SGPropertyDrawer>();
            if(customAttribute != null)
                m_PropertyDrawerList.Add(propertyDrawerType);
            else
                throw new Exception("Attempted to register a property drawer that isn't marked up with the SGPropertyDrawer attribute!");
        }

        bool IsPropertyTypeHandled(Type typeOfProperty, out Type propertyDrawerToUse)
        {
            propertyDrawerToUse = null;

            // Check to see if a property drawer has been registered that handles this type
            foreach (var propertyDrawerType in m_PropertyDrawerList)
            {
                var typeHandledByPropertyDrawer = propertyDrawerType.GetCustomAttribute<SGPropertyDrawer>();
                // Numeric types and boolean wrapper types like ToggleData handled here
                if (typeHandledByPropertyDrawer.propertyType == typeOfProperty)
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

#endregion

#region Selection
        public void UpdateSelection(List<ISelectable> selectedObjects)
        {
            m_SelectionList = selectedObjects;

            // Remove current properties
            for (int i = 0; i < m_ContentContainer.childCount; ++i)
            {
                var child = m_ContentContainer.Children().ElementAt(i);
                if (child is PropertySheet)
                {
                    m_ContentContainer.Remove(child);
                }
            }

            if(selectedObjects.Count == 0)
            {
                SetSelectionToGraph();
                return;
            }

            if(selectedObjects.Count > 1)
            {
                subTitle = $"{selectedObjects.Count} Objects.";
            }

            var propertySheet = new PropertySheet();
            try
            {
                foreach (var selectable in selectedObjects)
                {
                    object dataObject = null;
                    PropertyInfo[] properties;
                    if (selectable is IInspectable inspectable)
                    {
                        dataObject = inspectable.GetObjectToInspect();
                        if (dataObject == null)
                            throw new NullReferenceException("DataObject returned by Inspectable is null!");

                        properties = inspectable.GetPropertyInfo();
                        if (properties == null)
                            throw new NullReferenceException("PropertyInfos returned by Inspectable is null!");
                    }
                    else
                        continue;

                    foreach (var propertyInfo in properties)
                    {
                        var attribute = propertyInfo.GetCustomAttribute<Inspectable>();
                        if (attribute == null)
                            continue;

                        var propertyType = propertyInfo.PropertyType;

                        if (IsPropertyTypeHandled(propertyType, out var propertyDrawerTypeToUse))
                        {
                            var propertyDrawerInstance = (IPropertyDrawer)Activator.CreateInstance(propertyDrawerTypeToUse);
                            // Supply any required data to this particular kind of property drawer
                            inspectable.SupplyDataToPropertyDrawer(propertyDrawerInstance, () => this.UpdateSelection(m_SelectionList));
                            var propertyGUI = propertyDrawerInstance.DrawProperty(propertyInfo, dataObject, attribute);
                            propertySheet.Add(propertyGUI);
                        }
                    }
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

        void SetSelectionToGraph()
        {
            var graphEditorView = m_GraphView.GetFirstAncestorOfType<GraphEditorView>();
            if(graphEditorView == null)
                return;

            subTitle = $"{graphEditorView.assetName} (Graph)";

            // #TODO - Refactor, shouldn't this just be a property on the graph data object itself?
            var precisionField = new EnumField((Enum)m_GraphData.concretePrecision);
            precisionField.RegisterValueChangedCallback(evt =>
            {
                m_GraphData.owner.RegisterCompleteObjectUndo("Change Precision");
                if (m_GraphData.concretePrecision == (ConcretePrecision)evt.newValue)
                    return;

                m_GraphData.concretePrecision = (ConcretePrecision)evt.newValue;
                var nodeList = m_GraphView.Query<MaterialNodeView>().ToList();
                graphEditorView.colorManager.SetNodesDirty(nodeList);

                m_GraphData.ValidateGraph();
                graphEditorView.colorManager.UpdateNodeViews(nodeList);
                foreach (var node in m_GraphData.GetNodes<AbstractMaterialNode>())
                {
                    node.Dirty(ModificationScope.Graph);
                }
            });

            var sheet = new PropertySheet();
            sheet.Add(new PropertyRow(new Label("Precision")), (row) =>
            {
                row.Add(precisionField);
            });
            m_ContentContainer.Add(sheet);
        }
#endregion
    }
}
