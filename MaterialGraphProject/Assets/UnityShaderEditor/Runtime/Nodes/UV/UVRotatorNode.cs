using UnityEngine.Graphing;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.MaterialGraph
{
    [Title("UV/UV Rotator")]
    public class UVRotatorNode : Function2Input, IGeneratesFunction, IMayRequireMeshUV
    {
        private const string kUVSlotName = "UV";
        private const string kRotationSlotName = "Rotation";

        public UVRotatorNode()
        {
            name = "UVRotator";
        }

        protected override string GetFunctionName()
        {
            return "unity_uvrotator_" + precision;
        }

        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, kUVSlotName, kUVSlotName, SlotType.Input, SlotValueType.Vector4, Vector4.zero);
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, kRotationSlotName, kRotationSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, kUVSlotName, kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector4, Vector4.zero);
        }

        protected override string GetFunctionCallBody(string input1Value, string input2Value)
        {
            //get input UV slot
            var uvName = string.Format("{0}", UVChannel.uv0.GetUVName());
            bool isConnected = false;

            var uvSlot = FindInputSlot<MaterialSlot>(InputSlot1Id);
            if (uvSlot != null)
            {
                var edges = owner.GetEdges(uvSlot.slotReference).ToList();

                if (edges.Count > 0)
                {
                    var edge = edges[0];
                    var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
                    uvName = ShaderGenerator.AdaptNodeOutput(fromNode, edge.outputSlot.slotId, ConcreteSlotValueType.Vector4, true);
                    isConnected = true;
                }
            }


            if (isConnected)
                return GetFunctionName() + " (" + input1Value + ", " + input2Value + ")";
            else
                return GetFunctionName() + " (" + uvName + ", " + input2Value + ")";
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();

            outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            //center texture's pivot
            outputString.AddShaderChunk("arg1.xy -= 0.5;", false);

            //rotation matrix
            outputString.AddShaderChunk(precision + " s = sin(arg2);", false);
            outputString.AddShaderChunk(precision + " c = cos(arg2);", false);
            outputString.AddShaderChunk(precision + "2x2 rMatrix = float2x2(c, -s, s, c);", false);

            //center rotation matrix
            outputString.AddShaderChunk("rMatrix *= 0.5;", false);
            outputString.AddShaderChunk("rMatrix += 0.5;", false);
            outputString.AddShaderChunk("rMatrix = rMatrix*2 - 1;", false);

            //multiply the UVs by the rotation matrix
            outputString.AddShaderChunk("arg1.xy = mul(arg1.xy, rMatrix);", false);
            outputString.AddShaderChunk("arg1.xy += 0.5;", false);

            outputString.AddShaderChunk("return " + "arg1;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public bool RequiresMeshUV(UVChannel channel)
        {
            if (channel != UVChannel.uv0)
            {
                return false;
            }

            var uvSlot = FindInputSlot<MaterialSlot>(InputSlot1Id);
            if (uvSlot == null)
                return true;

            var edges = owner.GetEdges(uvSlot.slotReference).ToList();
            return edges.Count == 0;
        }
    }
}
