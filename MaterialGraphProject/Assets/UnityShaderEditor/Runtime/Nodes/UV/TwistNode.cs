using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("UV/Twist")]
    public class TwistNode : Function2Input, IGeneratesFunction
    {
        public TwistNode()
        {
            name = "Twist";
        }

        protected override string GetFunctionName()
        {
            return "unity_twist_" + precision;
        }

        protected override string GetInputSlot1Name()
        {
            return "UV";
        }

        protected override string GetInputSlot2Name()
        {
            return "Twist";
        }

        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, SlotType.Input, SlotValueType.Vector2, Vector2.zero);
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, SlotType.Input, SlotValueType.Vector1, Vector2.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector2, Vector2.zero);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("uv", "twist"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("float angle = twist * length(uv);", false);
            outputString.AddShaderChunk("float x = cos(angle) * uv.x - sin(angle) * uv.y;", false);
            outputString.AddShaderChunk("float y = sin(angle) * uv.x + cos(angle) * uv.y;", false);
            outputString.AddShaderChunk("return float2(x, y);", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
