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

        public RedirectNodeData() : base()
        {
            name = "Redirect Node";

            //Set the default state to collapsed
            DrawState temp = drawState;
            temp.expanded = false;
            drawState = temp;
        }

        public virtual void AddPortPair(int index = -1)
        {
            AddSlot(new DynamicValueMaterialSlot(m_inSlotID, m_inSlotName, m_inSlotName, SlotType.Input, Matrix4x4.zero));
            AddSlot(new DynamicValueMaterialSlot(m_outSlotID, m_outSlotName, m_outSlotName, SlotType.Output, Matrix4x4.zero));
        }

        public void OnDelete()
        {
            if (owner.isUndoingOrRedoing == true)
                return;

            // @SamH: hard-coded single case
            var node_inSlotRef = GetSlotReference(0);
            var node_outSlotRef = GetSlotReference(1);
            
            foreach (var inEdge in owner.GetRemovedEdges(node_inSlotRef))
            {
                foreach (var outEdge in owner.GetRemovedEdges(node_outSlotRef))
                    owner.Connect(inEdge.outputSlot, outEdge.inputSlot);
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Redirect", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Redirect(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
            //[Slot(2, Binding.None)] DynamicDimensionVector Add)
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
