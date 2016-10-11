using System;
using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEditor.MaterialGraph;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.Graphing.Drawing
{
    [Serializable]
	[CustomDataView(typeof(MaterialGraphNode))]
	public class ColorNodeData : MaterialNodeData
    {
        class ColorNodeContolData : NodeControlData
        {
            public override void OnGUIHandler()
            {
                EditorGUILayout.ColorField("test", Color.blue);
            }
        }

        protected override IEnumerable<NodeControlData> GetControlData()
        {
            return new List<NodeControlData> { CreateInstance<ColorNodeContolData>() };
        }
    }

    [Serializable]
	[CustomDataView(typeof(MaterialGraphNode))]
	public class MaterialNodeData : GraphElementData
    {
        public INode node { get; private set; }

        protected List<GraphElementData> m_Children = new List<GraphElementData>();

        public override IEnumerable<GraphElementData> elements
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

            AddPreview(inNode);

            m_Children.AddRange(GetControlData().OfType<GraphElementData>());


            //position = new Rect(node.drawState.position.x, node.drawState.position.y, 100, 200);
            //position
        }

        private void AddPreview(INode inNode)
        {
            var materialNode = inNode as AbstractMaterialNode;
            if (materialNode == null || !materialNode.hasPreview)
                return;

            var previewData = CreateInstance<NodePreviewData>();
            previewData.Initialize(materialNode);
            m_Children.Add(previewData);
        }

        protected virtual IEnumerable<NodeControlData> GetControlData()
        {
            return new NodeControlData[0];
        }
    }
}
