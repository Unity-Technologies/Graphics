namespace UnityEngine.MaterialGraph
{
    [Title("Procedural/Pulse")]
    public class PulseNode : Function3Input, IGeneratesFunction
    {
        public PulseNode()
        {
            name = "Pulse";
        }

        protected override string GetFunctionName()
        {
            return "unity_pulsenode_" + precision;
        }

        protected override string GetInputSlot1Name()
        {
            return "Value";
        }

        protected override string GetInputSlot2Name()
        {
            return "MinValue";
        }

        protected override string GetInputSlot3Name()
        {
            return "MaxValue";
        }

        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector1, Vector2.zero);
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector1, Vector2.zero);
        }

        protected override MaterialSlot GetInputSlot3()
        {
            return new MaterialSlot(InputSlot3Id, GetInputSlot3Name(), kInputSlot3ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector1, Vector2.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, UnityEngine.Graphing.SlotType.Output, SlotValueType.Vector1, Vector2.zero);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("value", "minValue", "maxValue"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("return step( minValue, value ) - step( maxValue, value );", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
