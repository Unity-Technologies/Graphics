using System;
using System.Reflection;
using Data.Interfaces;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEngine.UIElements;

namespace Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(CustomFunctionNode))]
    public class CustomFunctionNodePropertyDrawer : IPropertyDrawer
    {
        VisualElement CreateGUI(CustomFunctionNode node, InspectableAttribute attribute,
            out VisualElement propertyVisualElement)
        {
            var propertySheet = new PropertySheet(PropertyDrawerUtils.CreateLabel(attribute.labelName));
            propertySheet.Add(new ReorderableSlotListView(node, SlotType.Input));
            propertySheet.Add(new ReorderableSlotListView(node, SlotType.Output));
            propertySheet.Add(new HlslFunctionView(node));
            propertyVisualElement = propertySheet;
            return propertySheet;
        }

        public Action inspectorUpdateDelegate { get; set; }

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
