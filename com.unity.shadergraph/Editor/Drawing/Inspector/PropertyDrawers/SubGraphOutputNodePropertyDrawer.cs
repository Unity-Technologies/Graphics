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
    [SGPropertyDrawer(typeof(SubGraphOutputNode))]
    class SubGraphOutputNodePropertyDrawer : IPropertyDrawer, IGetNodePropertyDrawerPropertyData
    {
        Action m_setNodesAsDirtyCallback;
        Action m_updateNodeViewsCallback;

        public void GetPropertyData(Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            m_setNodesAsDirtyCallback = setNodesAsDirtyCallback;
            m_updateNodeViewsCallback = updateNodeViewsCallback;
        }

        VisualElement CreateGUI(SubGraphOutputNode node, InspectableAttribute attribute,
            out VisualElement propertyVisualElement)
        {
            var propertySheet = new PropertySheet(PropertyDrawerUtils.CreateLabel($"{node.name} Node", 0, FontStyle.Bold));

            PropertyDrawerUtils.AddDefaultNodeProperties(propertySheet, node, m_setNodesAsDirtyCallback, m_updateNodeViewsCallback);

            var inputListView = new ReorderableSlotListView(node, SlotType.Input, false);
            inputListView.OnAddCallback += list => inspectorUpdateDelegate(InspectorUpdateSource.PropertyInspection);
            inputListView.OnRemoveCallback += list => inspectorUpdateDelegate(InspectorUpdateSource.PropertyInspection);
            inputListView.OnListRecreatedCallback += () => inspectorUpdateDelegate(InspectorUpdateSource.PropertyInspection);
            inputListView.AllowedTypeCallback = SlotValueHelper.AllowedAsSubgraphOutput;
            propertySheet.Add(inputListView);
            propertyVisualElement = propertySheet;
            return propertySheet;
        }

        public Action<InspectorUpdateSource> inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject,
            InspectableAttribute attribute)
        {
            return this.CreateGUI(
                (SubGraphOutputNode)actualObject,
                attribute,
                out var propertyVisualElement);
        }
    }
}
