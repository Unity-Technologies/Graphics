using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph
{
    sealed class ContextView : StackNode
    {
        ContextData m_ContextData;

        // Currently we only need one Port per context
        // As the Contexts are hardcoded we know their directions
        Port m_Port;

        // These are required by MaterialNodeView.Init
        // To avoid having to go through GraphEditorView to create Nodes
        // We pass these into the constructor instead
        GraphView m_GraphView;
        EdgeConnectorListener m_Listener;
        PreviewManager m_PreviewManager;

        // When dealing with more Contexts, `name` should be serialized in the ContextData
        // Right now we dont do this so we dont overcommit to serializing unknowns
        public ContextView(string name, ContextData contextData, 
            GraphView graphView, EdgeConnectorListener listener, PreviewManager previewManager)
        {
            // Set data
            m_ContextData = contextData;
            m_GraphView = graphView;
            m_Listener = listener;
            m_PreviewManager = previewManager;

            // Header
            var headerLabel = new Label() { name = "headerLabel" };
            headerLabel.text = name;
            headerContainer.Add(headerLabel);

            // Add Blocks
            for(int i = 0; i < contextData.blocks.Count; i++)
            {
                var block = contextData.blocks[i];
                AddBlock(block, i);
            }
        }

        public ContextData contextData => m_ContextData;
        public Port port => m_Port;

        public void AddBlock(BlockNode blockData, int index)
        {
            var nodeView = new MaterialNodeView { userData = blockData };
            nodeView.Initialize(blockData, m_PreviewManager, m_Listener, m_GraphView);
            nodeView.MarkDirtyRepaint();

            if(index == -1)
            {
                AddElement(nodeView);
            }
            else 
            {
                InsertElement(index, nodeView);
            }
        }

        public void AddPort(Direction direction)
        {
            var capacity = direction == Direction.Input ? Port.Capacity.Single : Port.Capacity.Multi;
            var container = direction == Direction.Input ? inputContainer : outputContainer;
            m_Port = Port.Create<Edge>(Orientation.Vertical, direction, capacity, null);
            m_Port.portName = "";

            // Vertical ports have no representation in Model
            // Therefore we need to disable interaction
            m_Port.pickingMode = PickingMode.Ignore;

            container.Add(m_Port);
        }

        public void InsertElements(int insertIndex, IEnumerable<GraphElement> elements)
        {
            var blockDatas = elements.Select(x => x.userData as BlockNode).ToArray();
            for(int i = 0; i < blockDatas.Length; i++)
            {
                contextData.blocks.Remove(blockDatas[i]);
            }

            contextData.blocks.InsertRange(insertIndex, blockDatas);
        }

        protected override bool AcceptsElement(GraphElement element, ref int proposedIndex, int maxIndex)
        {
            return element.userData is BlockNode;
        }
    }
}
