using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class RedirectNodeData : AbstractMaterialNode
    {
        public const int kInputSlotID = 0;
        public const int kOutputSlotID = 1;

        public RedirectNodeData()
        {
            name = "Redirect Node";
        }

        // Static version for testability
        public static RedirectNodeData Create(GraphData graph, ConcreteSlotValueType edgeType, Vector2 absolutePosition, SlotReference inputRef, SlotReference outputRef, GroupData group)
        {
            var nodeData = new RedirectNodeData();
            nodeData.AddSlots(edgeType);
            nodeData.SetPosition(absolutePosition);
            nodeData.group = group;

            // Hard-coded for single input-output. Changes would be needed for multi-input redirects
            var nodeInSlotRef = nodeData.GetSlotReference(RedirectNodeData.kInputSlotID);
            var nodeOutSlotRef = nodeData.GetSlotReference(RedirectNodeData.kOutputSlotID);

            graph.owner.RegisterCompleteObjectUndo("Add Redirect Node");
            graph.AddNode(nodeData);

            graph.Connect(outputRef, nodeInSlotRef);
            graph.Connect(nodeOutSlotRef, inputRef);

            return nodeData;
        }

        void AddSlots(ConcreteSlotValueType edgeType)
        {
            // Valuetype gets the type should be the type for input and output
            switch (edgeType)
            {
                case ConcreteSlotValueType.Boolean:
                    AddSlot(new BooleanMaterialSlot(kInputSlotID, "", "", SlotType.Input, false));
                    AddSlot(new BooleanMaterialSlot(kOutputSlotID, "", "", SlotType.Output, false));
                    break;
                case ConcreteSlotValueType.Vector1:
                    AddSlot(new DynamicVectorMaterialSlot(kInputSlotID, "", "", SlotType.Input, Vector4.zero));
                    AddSlot(new DynamicVectorMaterialSlot(kOutputSlotID, "", "", SlotType.Output, Vector4.zero));
                    break;
                case ConcreteSlotValueType.Vector2:
                    AddSlot(new DynamicVectorMaterialSlot(kInputSlotID, "", "", SlotType.Input, Vector4.zero));
                    AddSlot(new DynamicVectorMaterialSlot(kOutputSlotID, "", "", SlotType.Output, Vector4.zero));
                    break;
                case ConcreteSlotValueType.Vector3:
                    AddSlot(new DynamicVectorMaterialSlot(kInputSlotID, "", "", SlotType.Input, Vector4.zero));
                    AddSlot(new DynamicVectorMaterialSlot(kOutputSlotID, "", "", SlotType.Output, Vector4.zero));
                    break;
                case ConcreteSlotValueType.Vector4:
                    AddSlot(new DynamicVectorMaterialSlot(kInputSlotID, "", "", SlotType.Input, Vector4.zero));
                    AddSlot(new DynamicVectorMaterialSlot(kOutputSlotID, "", "", SlotType.Output, Vector4.zero));
                    break;
                case ConcreteSlotValueType.Matrix2:
                    AddSlot(new DynamicMatrixMaterialSlot(kInputSlotID, "", "", SlotType.Input));
                    AddSlot(new DynamicMatrixMaterialSlot(kOutputSlotID, "", "", SlotType.Output));
                    break;
                case ConcreteSlotValueType.Matrix3:
                    AddSlot(new DynamicMatrixMaterialSlot(kInputSlotID, "", "", SlotType.Input));
                    AddSlot(new DynamicMatrixMaterialSlot(kOutputSlotID, "", "", SlotType.Output));
                    break;
                case ConcreteSlotValueType.Matrix4:
                    AddSlot(new DynamicMatrixMaterialSlot(kInputSlotID, "", "", SlotType.Input));
                    AddSlot(new DynamicMatrixMaterialSlot(kOutputSlotID, "", "", SlotType.Output));
                    break;
                case ConcreteSlotValueType.Texture2D:
                    AddSlot(new Texture2DMaterialSlot(kInputSlotID, "", "", SlotType.Input));
                    AddSlot(new Texture2DMaterialSlot(kOutputSlotID, "", "", SlotType.Output));
                    break;
                case ConcreteSlotValueType.Texture2DArray:
                    AddSlot(new Texture2DArrayMaterialSlot(kInputSlotID, "", "", SlotType.Input));
                    AddSlot(new Texture2DArrayMaterialSlot(kOutputSlotID, "", "", SlotType.Output));
                    break;
                case ConcreteSlotValueType.Texture3D:
                    AddSlot(new Texture3DMaterialSlot(kInputSlotID, "", "", SlotType.Input));
                    AddSlot(new Texture3DMaterialSlot(kOutputSlotID, "", "", SlotType.Output));
                    break;
                case ConcreteSlotValueType.Cubemap:
                    AddSlot(new CubemapMaterialSlot(kInputSlotID, "", "", SlotType.Input));
                    AddSlot(new CubemapMaterialSlot(kOutputSlotID, "", "", SlotType.Output));
                    break;
                case ConcreteSlotValueType.SamplerState:
                    AddSlot(new SamplerStateMaterialSlot(kInputSlotID, "", "", SlotType.Input));
                    AddSlot(new SamplerStateMaterialSlot(kOutputSlotID, "", "", SlotType.Output));
                    break;
                case ConcreteSlotValueType.Gradient:
                    AddSlot(new GradientMaterialSlot(kInputSlotID, "", "", SlotType.Input));
                    AddSlot(new GradientMaterialSlot(kOutputSlotID, "", "", SlotType.Output));
                    break;
                case ConcreteSlotValueType.VirtualTexture:
                    AddSlot(new VirtualTextureMaterialSlot(kInputSlotID, "", "", SlotType.Input));
                    AddSlot(new VirtualTextureMaterialSlot(kOutputSlotID, "", "", SlotType.Output));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected internal override string GetOutputForSlot(SlotReference fromSocketRef, ConcreteSlotValueType valueType, GenerationMode generationMode)
        {
            var slotRef = NodeUtils.DepthFirstCollectRedirectNodeFromNode(this);
            var fromLeftNode = slotRef.node;
            if (fromLeftNode is RedirectNodeData)
            {
                return GetSlotValue(kInputSlotID, generationMode);
            }

            if (fromLeftNode != null)
            {
                return GenerationUtils.AdaptNodeOutput(fromLeftNode, slotRef.slotId, valueType);
            }
            return base.GetOutputForSlot(fromSocketRef, valueType, generationMode);
        }

        public void SetPosition(Vector2 pos)
        {
            var temp = drawState;
            Vector2 offset = new Vector2(-30, -12);
            temp.position = new Rect(pos + offset, Vector2.zero);
            drawState = temp;
        }

        public void GetOutputAndInputSlots(out SlotReference outputSlotRef, out List<SlotReference> inputSlotRefs)
        {
            var inputSlot = FindSlot<MaterialSlot>(kInputSlotID);
            var inEdges = owner.GetEdges(inputSlot.slotReference).ToList();
            outputSlotRef = inEdges.Any() ? inEdges.First().outputSlot : new SlotReference();

            var outputSlot = FindSlot<MaterialSlot>(kOutputSlotID);
            // Get the slot where this edge ends.
            var outEdges = owner.GetEdges(outputSlot.slotReference);
            inputSlotRefs = new List<SlotReference>(outEdges.Select(edge => edge.inputSlot));
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
