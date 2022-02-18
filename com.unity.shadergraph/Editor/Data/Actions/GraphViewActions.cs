using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class ConvertToPropertyAction : IGraphDataAction
    {
        void ConvertToProperty(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out ConvertToPropertyAction");
            AssertHelpers.IsNotNull(inlinePropertiesToConvert, "InlinePropertiesToConvert is null while carrying out ConvertToPropertyAction");
            graphData.owner.RegisterCompleteObjectUndo("Convert to Property");

            var defaultCategory = graphData.categories.FirstOrDefault();
            AssertHelpers.IsNotNull(defaultCategory, "Default Category is null while carrying out ConvertToPropertyAction");

            foreach (var converter in inlinePropertiesToConvert)
            {
                var convertedProperty = converter.AsShaderProperty();
                var node = converter as AbstractMaterialNode;

                graphData.AddGraphInput(convertedProperty);

                // Also insert this input into the default category
                if (defaultCategory != null)
                {
                    var addItemToCategoryAction = new AddItemToCategoryAction();
                    addItemToCategoryAction.categoryGuid = defaultCategory.categoryGuid;
                    addItemToCategoryAction.itemToAdd = convertedProperty;
                    graphData.owner.graphDataStore.Dispatch(addItemToCategoryAction);
                }

                // Add reference to converted property for use in responding to this action later
                convertedPropertyReferences.Add(convertedProperty);

                var propNode = new PropertyNode();
                propNode.drawState = node.drawState;
                propNode.group = node.group;
                graphData.AddNode(propNode);
                propNode.property = convertedProperty;

                var oldSlot = node.FindSlot<MaterialSlot>(converter.outputSlotId);
                var newSlot = propNode.FindSlot<MaterialSlot>(PropertyNode.OutputSlotId);

                foreach (var edge in graphData.GetEdges(oldSlot.slotReference))
                    graphData.Connect(newSlot.slotReference, edge.inputSlot);

                graphData.RemoveNode(node);
            }
        }

        public Action<GraphData> modifyGraphDataAction => ConvertToProperty;

        public IList<IPropertyFromNode> inlinePropertiesToConvert { get; set; } = new List<IPropertyFromNode>();

        public IList<AbstractShaderProperty> convertedPropertyReferences { get; set; } = new List<AbstractShaderProperty>();

        public Vector2 nodePsition { get; set; }
    }

    class ConvertToInlineAction : IGraphDataAction
    {
        void ConvertToInline(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out ConvertToInlineAction");
            AssertHelpers.IsNotNull(propertyNodesToConvert, "PropertyNodesToConvert is null while carrying out ConvertToInlineAction");
            graphData.owner.RegisterCompleteObjectUndo("Convert to Inline Node");

            foreach (var propertyNode in propertyNodesToConvert)
                graphData.ReplacePropertyNodeWithConcreteNode(propertyNode);
        }

        public Action<GraphData> modifyGraphDataAction => ConvertToInline;

        public IEnumerable<PropertyNode> propertyNodesToConvert { get; set; } = new List<PropertyNode>();
    }

    class DragGraphInputAction : IGraphDataAction
    {
        void DragGraphInput(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out DragGraphInputAction");
            AssertHelpers.IsNotNull(graphInputBeingDraggedIn, "GraphInputBeingDraggedIn is null while carrying out DragGraphInputAction");
            graphData.owner.RegisterCompleteObjectUndo("Drag Graph Input");

            switch (graphInputBeingDraggedIn)
            {
                case AbstractShaderProperty property:
                {
                    if (property is MultiJsonInternal.UnknownShaderPropertyType)
                        break;

                    // This could be from another graph, in which case we add a copy of the ShaderInput to this graph.
                    if (graphData.properties.FirstOrDefault(p => p == property) == null)
                    {
                        var copyShaderInputAction = new CopyShaderInputAction();
                        copyShaderInputAction.shaderInputToCopy = property;
                        graphData.owner.graphDataStore.Dispatch(copyShaderInputAction);
                        property = (AbstractShaderProperty)copyShaderInputAction.copiedShaderInput;
                    }

                    var node = new PropertyNode();
                    var drawState = node.drawState;
                    drawState.position = new Rect(nodePosition, drawState.position.size);
                    node.drawState = drawState;
                    graphData.AddNode(node);

                    // Setting the guid requires the graph to be set first.
                    node.property = property;
                    break;
                }
                case ShaderKeyword keyword:
                {
                    // This could be from another graph, in which case we add a copy of the ShaderInput to this graph.
                    if (graphData.keywords.FirstOrDefault(k => k == keyword) == null)
                    {
                        var copyShaderInputAction = new CopyShaderInputAction();
                        copyShaderInputAction.shaderInputToCopy = keyword;
                        graphData.owner.graphDataStore.Dispatch(copyShaderInputAction);
                        keyword = (ShaderKeyword)copyShaderInputAction.copiedShaderInput;
                    }

                    var node = new KeywordNode();
                    var drawState = node.drawState;
                    drawState.position = new Rect(nodePosition, drawState.position.size);
                    node.drawState = drawState;
                    graphData.AddNode(node);

                    // Setting the guid requires the graph to be set first.
                    node.keyword = keyword;
                    break;
                }
                case ShaderDropdown dropdown:
                {
                    if (graphData.IsInputAllowedInGraph(dropdown))
                    {
                        // This could be from another graph, in which case we add a copy of the ShaderInput to this graph.
                        if (graphData.dropdowns.FirstOrDefault(d => d == dropdown) == null)
                        {
                            var copyShaderInputAction = new CopyShaderInputAction();
                            copyShaderInputAction.shaderInputToCopy = dropdown;
                            graphData.owner.graphDataStore.Dispatch(copyShaderInputAction);
                            dropdown = (ShaderDropdown)copyShaderInputAction.copiedShaderInput;
                        }

                        var node = new DropdownNode();
                        var drawState = node.drawState;
                        drawState.position = new Rect(nodePosition, drawState.position.size);
                        node.drawState = drawState;
                        graphData.AddNode(node);

                        // Setting the guid requires the graph to be set first.
                        node.dropdown = dropdown;
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Action<GraphData> modifyGraphDataAction => DragGraphInput;

        public ShaderInput graphInputBeingDraggedIn { get; set; }

        public Vector2 nodePosition { get; set; }
    }
}
