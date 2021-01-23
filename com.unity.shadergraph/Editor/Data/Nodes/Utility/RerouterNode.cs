using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class RerouterNode : AbstractMaterialNode
    {
        public const int kInputSlotID = 0;
        public const int kOutputSlotID = 1;

        internal override bool ExposeToSearcher => false;
        public override bool hasPreview => true;

        public RerouterNode()
        {
            name = "Reroute Node";
        }



        private SlotReference From;
        private List<SlotReference> To;
        int outWidth = 4;
        public static RerouterNode Create(SlotReference from, List<SlotReference> to, BlockNode.CustomBlockType type)
        {
            var node = new RerouterNode();
            node.From = from;
            node.To = to;
            node.outWidth = (int)type;
            node.AddSlots(type);
            return node;
        }

        internal void ApplyReroute(GraphData graph)
        {
            graph.AddNode(this);
            var nodeInSlotRef = GetSlotReference(RedirectNodeData.kInputSlotID);
            var nodeOutSlotRef = GetSlotReference(RedirectNodeData.kOutputSlotID);

            foreach (var to in To)
            {                
                graph.Connect(From, nodeInSlotRef);
                graph.Connect(nodeOutSlotRef, to);
            }
        }

        void AddSlots(BlockNode.CustomBlockType type)
        {
            switch (type)
            {
                case BlockNode.CustomBlockType.Float:
                    AddSlot(new Vector1MaterialSlot(kOutputSlotID, "", "", SlotType.Output, 0));
                    AddSlot(new Vector1MaterialSlot(kInputSlotID, "", "", SlotType.Input, 0));
                    break;
                case BlockNode.CustomBlockType.Vector2:
                    AddSlot(new Vector2MaterialSlot(kOutputSlotID, "", "", SlotType.Output, Vector4.zero));
                    AddSlot(new Vector2MaterialSlot(kInputSlotID, "", "", SlotType.Input, Vector4.zero));
                    break;
                case BlockNode.CustomBlockType.Vector3:
                    AddSlot(new Vector3MaterialSlot(kOutputSlotID, "", "", SlotType.Output, Vector4.zero));
                    AddSlot(new Vector3MaterialSlot(kInputSlotID, "", "", SlotType.Input, Vector4.zero));
                    break;
                case BlockNode.CustomBlockType.Vector4:                    
                    AddSlot(new Vector4MaterialSlot(kOutputSlotID, "", "", SlotType.Output, Vector4.zero));
                    AddSlot(new Vector4MaterialSlot(kInputSlotID, "", "", SlotType.Input, Vector4.zero));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected internal override string GetOutputForSlot(SlotReference fromSocketRef, ConcreteSlotValueType valueType, GenerationMode generationMode)
        {
            if (generationMode != GenerationMode.Preview)
                throw new Exception("this should not be possible in.");

            var width = 0;
            switch (valueType)
            {                
                case ConcreteSlotValueType.Vector1: width = 1; break;
                case ConcreteSlotValueType.Vector2: width = 2; break;
                case ConcreteSlotValueType.Vector3: width = 3; break;
                default: width = 4; break;
            }


            var result = GetVariableNameForSlot(kInputSlotID);
            result = CustomInterpolatorUtils.ConvertVector(result, outWidth, width);

            return result;
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return From.node.GetOutputForSlot(From, FindSlot<MaterialSlot>(0).concreteValueType, GenerationMode.Preview);
        }



        public override void ValidateNode()
        {
            base.ValidateNode();

            bool noInputs = false;
            bool noOutputs = false;
            var slots = new List<MaterialSlot>();

            GetInputSlots(slots);
            foreach (var inSlot in slots)
            {
                var edges = owner.GetEdges(inSlot.slotReference).ToList();
                noInputs = !edges.Any();
            }

            slots.Clear();
            GetOutputSlots(slots);
            foreach (var outSlot in slots)
            {
                var edges = owner.GetEdges(outSlot.slotReference).ToList();
                noOutputs = !edges.Any();
            }

            if (noInputs && !noOutputs)
            {
                owner.AddValidationError(objectId, "Node has no inputs and default value will be 0.", ShaderCompilerMessageSeverity.Warning);
            }
        }
    }
}
