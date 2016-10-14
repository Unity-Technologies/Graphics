using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.MaterialGraph;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.Graphing.Drawing
{
    class ColorNodeContolData : NodeControlData
    {
        public override void OnGUIHandler()
        {
            var cNode = node as UnityEngine.MaterialGraph.ColorNode;
            if (cNode == null)
                return;
            cNode.color =  EditorGUILayout.ColorField("Color", cNode.color);
        }
    }

    [Serializable]
	public class ColorNodeData : MaterialNodeData
    {
        protected override IEnumerable<GraphElementData> GetControlData()
        {
            var instance = CreateInstance<ColorNodeContolData>();
            instance.Initialize(node);
            return new List<GraphElementData> { instance };
        }
    }

    [Serializable]
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


        public override void CommitChanges()
        {
            base.CommitChanges();
            var drawData = node.drawState;
            drawData.position = position;
            node.drawState = drawData;
        }


        //TODO: Kill this and the function below after talking with shanti
        [SerializeField]
        private int m_SerializationRandom;

        public void MarkDirtyHack()
        {
            m_SerializationRandom++;
        }

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

            var controlData = GetControlData();
            m_Children.AddRange(controlData);


            position = new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0);
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

        protected virtual IEnumerable<GraphElementData> GetControlData()
        {
            return new NodeControlData[0];
        }
    }
}
