namespace UnityEngine.MaterialGraph
{
    [Title("Procedural/Pulse")]
    public class PulseNode : Function2Input, IGeneratesFunction
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
            return "X Value";
        }

        protected override string GetInputSlot2Name()
        {
            return "X Min and Max";
        }

        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector1, Vector2.zero);
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector2, Vector2.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, UnityEngine.Graphing.SlotType.Output, SlotValueType.Vector1, Vector2.zero);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("xValue", "xMinAndMax"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("return step( xMinAndMax.x, xValue ) - step( xMinAndMax.y, xValue );", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
