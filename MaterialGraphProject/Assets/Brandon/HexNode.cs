namespace UnityEngine.MaterialGraph
{
    [Title("Procedural/Hex Node")]
    public class HexNode : Function2Input, IGeneratesFunction
    {
        public HexNode()
        {
            name = "HexNode";
        }

        protected override string GetFunctionName()
        {
            return "unity_hex_" + precision;
        }

        protected override string GetInputSlot1Name()
        {
            return "UV";
        }

        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector2, Vector2.zero);
        }

        protected override string GetInputSlot2Name()
        {
            return "Thickness";
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector1, Vector2.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, UnityEngine.Graphing.SlotType.Output, SlotValueType.Vector1, Vector2.zero);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("uv", "thickness"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("uv.y += fmod(floor(uv.x), 2.0) * 0.5;", false);
            outputString.AddShaderChunk("uv = abs(frac(uv) - 0.5);", false);
            outputString.AddShaderChunk("return step(thickness, abs(max(uv.x * 1.5 + uv.y, uv.y * 2.0) - 1.0));", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
