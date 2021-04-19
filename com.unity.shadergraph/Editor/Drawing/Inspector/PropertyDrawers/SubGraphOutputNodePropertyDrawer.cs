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
    public class SubGraphOutputNodePropertyDrawer : IPropertyDrawer
    {
        VisualElement CreateGUI(SubGraphOutputNode node, InspectableAttribute attribute,
            out VisualElement propertyVisualElement)
        {
            var propertySheet = new PropertySheet(PropertyDrawerUtils.CreateLabel($"{node.name} Node", 0, FontStyle.Bold));
            var inputListView = new ReorderableSlotListView(node, SlotType.Input, false);
            inputListView.OnAddCallback += list => inspectorUpdateDelegate();
            inputListView.OnRemoveCallback += list => inspectorUpdateDelegate();
            inputListView.OnListRecreatedCallback += () => inspectorUpdateDelegate();
            inputListView.AllowedTypeCallback = SlotValueHelper.AllowedAsSubgraphOutput;
            propertySheet.Add(inputListView);
            propertyVisualElement = propertySheet;
            return propertySheet;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject,
            InspectableAttribute attribute)
        {
            return this.CreateGUI(
                (SubGraphOutputNode) actualObject,
                attribute,
                out var propertyVisualElement);
        }
    }
}
