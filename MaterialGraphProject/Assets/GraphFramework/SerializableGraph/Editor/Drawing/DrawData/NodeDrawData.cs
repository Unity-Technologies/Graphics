using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public class NodeDrawData : GraphElementData
    {
        protected NodeDrawData()
        {}

        public INode node { get; private set; }

        protected List<GraphElementData> m_Children = new List<GraphElementData>();

        public override IEnumerable<GraphElementData> elements
        {
            get { return m_Children; }
        }

        //TODO: Kill this and the function below after talking with shanti
        [SerializeField]
        private int m_SerializationRandom;

        public void MarkDirtyHack()
        {
            m_SerializationRandom++;
        }

        public override void CommitChanges()
        {
            base.CommitChanges();
            var drawData = node.drawState;
            drawData.position = position;
            node.drawState = drawData;
        }

        protected virtual IEnumerable<GraphElementData> GetControlData()
        {
            return new ControlDrawData[0];
        }

        public virtual void Initialize(INode inNode)
        {
            node = inNode;
            capabilities |= Capabilities.Movable;

            if (node == null)
                return;

            name = inNode.name;

            foreach (var input in node.GetSlots<ISlot>())
            {
                var data = CreateInstance<AnchorDrawData>();
                data.Initialize(input);
                m_Children.Add(data);
            }

            var controlData = GetControlData();
            m_Children.AddRange(controlData);

            position = new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0);
            //position
        }
    }
}
