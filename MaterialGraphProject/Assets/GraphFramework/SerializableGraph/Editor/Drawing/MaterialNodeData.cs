using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.MaterialGraph;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.Graphing.Drawing
{

    [Serializable]
    public class MaterialNodeData : GraphElementData
    {
        public INode node { get; private set; }

        protected List<GraphElementData> m_Children = new List<GraphElementData>();

        public IEnumerable<GraphElementData> elements
        {
            get { return m_Children; }
        }

        protected MaterialNodeData()
        {}

        public void Initialize(INode inNode)
        {
            node = inNode;
            capabilities |= Capabilities.Movable;

            if (node == null)
                return;

            name = inNode.name;

            foreach (var input in node.GetSlots<ISlot>())
            {
                var data = CreateInstance<MaterialNodeAnchorData>();
                data.Initialize(input);
                m_Children.Add(data);
            }

            var materialNode = inNode as AbstractMaterialNode;
            if (materialNode == null || !materialNode.hasPreview)
                return;
            
            var previewData = CreateInstance<NodePreviewData>();
            previewData.Initialize(materialNode);
            m_Children.Add(previewData);


            //position = new Rect(node.drawState.position.x, node.drawState.position.y, 100, 200);
            //position
        }

    }
}
