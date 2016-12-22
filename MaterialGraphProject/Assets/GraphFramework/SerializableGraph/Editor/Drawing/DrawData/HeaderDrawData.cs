using System;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public class HeaderDrawData : GraphElementPresenter
    {
        protected HeaderDrawData()
        {}

        private INode node;

        [SerializeField] private bool m_Expanded;

        public string title
        {
            get { return node.name; }
        }

        public bool expanded
        {
            get { return m_Expanded; }
            set
            {
                var state = node.drawState;
                state.expanded = value;
                node.drawState = state;
                m_Expanded = value;
            }
        }

        public void Initialize(INode inNode)
        {
            node = inNode;
            name = inNode.name + " Header";
            m_Expanded = node.drawState.expanded;
        }
    }
}
