using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphing;

using Edge = UnityEditor.Experimental.GraphView.Edge;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Redirect")]
    class RedirectNodeData : CodeFunctionNode
    {
        public struct PortPair
        {
            public Port input;
            public Port output;
        }

        const int m_inSlotID = 0;
        const int m_outSlotID = 1;
        const string m_inSlotName = "In";
        const string m_outSlotName = "Out";

        const int m_tempSlotID = 2;
        const string m_tempSlotName = "Add";

        //Dictionary of index -> port pairs?
        Dictionary<int, PortPair> m_portPairs;
        protected int nextFreeIndex = 0;

        public RedirectNodeData() : base()
        {
            name = "Redirect Node";
            m_portPairs = new Dictionary<int, PortPair>();

            //Set the default state to collapsed
            DrawState temp = drawState;
            temp.expanded = false;
            drawState = temp;

            AddSlot(new DynamicValueMaterialSlot(m_tempSlotID, m_tempSlotName, m_tempSlotName, SlotType.Input, Matrix4x4.zero));
        }

        public virtual void AddPortPair(int index = -1)
        {
            AddSlot(new DynamicValueMaterialSlot(m_inSlotID, m_inSlotName, m_inSlotName, SlotType.Input, Matrix4x4.zero));
            AddSlot(new DynamicValueMaterialSlot(m_outSlotID, m_outSlotName, m_outSlotName, SlotType.Output, Matrix4x4.zero));
        }

        public void Disconnect()
        {
            // @SamH: Hacky, hard-coded single case
            var node_inSlotRef = GetSlotReference(0);
            var node_outSlotRef = GetSlotReference(1);
            
            var inEdges = owner.GetEdges(node_outSlotRef);

            foreach (var inEdge in inEdges)
            {
                if(inEdge.outputSlot.nodeGuid == guid)
                {
                    var outEdges = this.owner.GetEdges(node_outSlotRef);
                    foreach (var outEdge in outEdges)
                    {
                        if (outEdge.outputSlot.nodeGuid == guid)
                        {
                            owner.Connect(inEdge.inputSlot, outEdge.inputSlot);
                        }
                    }
                }
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

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Redirect", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Redirect(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out,
            [Slot(2, Binding.None)] DynamicDimensionVector Add)
        {
            return
                @"
{
    Out = In;
}
";
        }
    }
}
