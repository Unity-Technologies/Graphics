using System;
using System.Reflection;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEngine.UIElements;

namespace Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(SubGraphOutputNode))]
    public class SubGraphOutputNodePropertyDrawer
    {
        VisualElement CreateGUI(SubGraphOutputNode node, InspectableAttribute attribute,
            out VisualElement propertyVisualElement)
        {
            var propertySheet = new PropertySheet(PropertyDrawerUtils.CreateLabel(attribute.labelName));
            propertySheet.Add(new ReorderableSlotListView(node, SlotType.Input));
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
