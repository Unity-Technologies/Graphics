using System;
using System.Reflection;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEngine.UIElements;
using UnityEngine;

namespace  UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(CustomFunctionNode))]
    class CustomFunctionNodePropertyDrawer : IPropertyDrawer, IGetNodePropertyDrawerPropertyData
    {
        Action m_setNodesAsDirtyCallback;
        Action m_updateNodeViewsCallback;

        void IGetNodePropertyDrawerPropertyData.GetPropertyData(Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            m_setNodesAsDirtyCallback = setNodesAsDirtyCallback;
            m_updateNodeViewsCallback = updateNodeViewsCallback;
        }

        VisualElement CreateGUI(CustomFunctionNode node, InspectableAttribute attribute,
            out VisualElement propertyVisualElement)
        {
            var propertySheet = new PropertySheet(PropertyDrawerUtils.CreateLabel($"{node.name} Node", 0, FontStyle.Bold));

            PropertyDrawerUtils.AddDefaultNodeProperties(propertySheet, node, m_setNodesAsDirtyCallback, m_updateNodeViewsCallback);

            var inputListView = new ReorderableSlotListView(node, SlotType.Input, true);
            inputListView.OnAddCallback += list => inspectorUpdateDelegate(InspectorUpdateSource.PropertyInspection);
            inputListView.OnRemoveCallback += list => inspectorUpdateDelegate(InspectorUpdateSource.PropertyInspection);
            inputListView.OnListRecreatedCallback += () => inspectorUpdateDelegate(InspectorUpdateSource.PropertyInspection);
            propertySheet.Add(inputListView);

            var outputListView = new ReorderableSlotListView(node, SlotType.Output, true);
            outputListView.OnAddCallback += list => inspectorUpdateDelegate(InspectorUpdateSource.PropertyInspection);
            outputListView.OnRemoveCallback += list => inspectorUpdateDelegate(InspectorUpdateSource.PropertyInspection);
            outputListView.OnListRecreatedCallback += () => inspectorUpdateDelegate(InspectorUpdateSource.PropertyInspection);
            propertySheet.Add(outputListView);

            propertySheet.Add(new HlslFunctionView(node));
            propertyVisualElement = null;
            return propertySheet;
        }

        public Action<InspectorUpdateSource> inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject,
            InspectableAttribute attribute)
        {
            return this.CreateGUI(
                (CustomFunctionNode) actualObject,
                attribute,
                out var propertyVisualElement);
        }
    }
}
