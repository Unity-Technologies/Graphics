using System;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public class HeaderDrawData : NodeDrawData
    {
        protected HeaderDrawData()
        {}

        private INode node;

        public string title
        {
            get { return node.name; }
        }

        public bool expanded
        {
            get { return node.drawState.expanded; }
            set
            {
                var state = node.drawState;
                state.expanded = value;
                node.drawState = state;
            }
        }

        public void Initialize(INode inNode)
        {
            node = inNode;
            name = inNode.name + " Header";
        }
    }
}