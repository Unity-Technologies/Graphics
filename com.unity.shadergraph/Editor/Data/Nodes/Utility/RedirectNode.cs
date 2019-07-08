using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Redirect")]
    class RedirectNodeData : AbstractMaterialNode
    {
        public struct PortPair
        {
            public Port input;
            public Port output;
        }

        //Dictionary of index -> port pairs?
        Dictionary<int, PortPair> m_portPairs;
        protected int nextFreeIndex = 0;

        public RedirectNodeData() : base()
        {
            name = "Redirect Node";
            m_portPairs = new Dictionary<int, PortPair>();
        }

        //Create and/or destroy input pair
        public virtual void AddPortPair(Port _input, Port _output, int index = -1)
        {
            PortPair newPair = new PortPair() { input = _input, output = _output };

            if (index == -1)
            {
                FindNextFreeIndex();
                m_portPairs.Add(nextFreeIndex++, newPair);
            }
            else
            {
                RemovePortPair(index); // Purposeful overwrite
                m_portPairs.Add(index, newPair);
            }
        }

        public void RemovePortPair(int index)
        {
            // Handle existing connections
            if (m_portPairs.ContainsKey(index))
            {
                //Remove Ports
            }

            if (nextFreeIndex > index)
                nextFreeIndex = index;
        }

        bool FindNextFreeIndex()
        {
            while(true)
            {
                if (!m_portPairs.ContainsKey(nextFreeIndex))
                    return true;
                else
                    ++nextFreeIndex;
            }
        }

        //public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        //{
        //    if (evt.target == this)
        //    {
        //
        //    }
        //}

        private void SplitEdge(Edge edge, int index)
        {

        }

        /*
         In order to add new logic to expand/collapse behaviors please override the property:
         public virtual bool expanded {... }
         
         And the function:
         public void RefreshExpandedState(){...}
         
         which are both declared on the Node.cs parent class
         */
    }
}
