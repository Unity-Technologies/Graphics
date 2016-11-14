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

        public bool expanded = true;

        protected List<GraphElementData> m_Children = new List<GraphElementData>();

        public IEnumerable<GraphElementData> elements
        {
            get { return m_Children; }
        }

        public virtual void OnModified(ModificationScope scope)
        {
            expanded = node.drawState.expanded;
        }

        public void CommitChanges()
        {
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
            expanded = node.drawState.expanded;

            var m_HeaderData = CreateInstance<HeaderDrawData>();
            m_HeaderData.Initialize(inNode);
            m_Children.Add(m_HeaderData);

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
