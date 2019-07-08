using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Redirect")]
    class RedirectNode : AbstractMaterialNode
    {
        private struct PortPair
        {
            public Port input;
            public Port output;
        }

        //Dictionary of index -> port pairs?
        Dictionary<int, PortPair> inOutPortPairs;
        protected int nextFreeIndex = 0;

        public RedirectNode() : base()
        {
            name = "Redirect Node";
        }

        //Create and/or destroy input pair
        public virtual void AddInputPair(Type valueType, int index = -1)
        {
            PortPair newPair = new PortPair();

            //newPair.input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, valueType);
            //newPair.output = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, valueType);

            if (index == -1)
            {
                FindNextFreeIndex();
                inOutPortPairs.Add(nextFreeIndex++, newPair);
            }
            else
            {
                RemovePortPair(index); // Purposeful overwrite
                inOutPortPairs.Add(index, newPair);
            }
        }

        public void RemovePortPair(int index)
        {
            // Handle existing connections
            if (inOutPortPairs.ContainsKey(index))
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
                if (!inOutPortPairs.ContainsKey(nextFreeIndex))
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
