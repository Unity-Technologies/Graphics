namespace UnityEngine.MaterialGraph
{
    [Title("Procedural/Gradient Ramp Node")]
    public class GradientRampNode : Function2Input, IGeneratesFunction
    {
        public GradientRampNode()
        {
            name = "GradientRampNode";
        }

        protected override string GetFunctionName()
        {
            return "unitygGradientramp_" + precision;
        }

        protected override string GetInputSlot1Name()
        {
            return "UV";
        }

        protected override string GetInputSlot2Name()
        {
            return "Stripe Count";
        }

        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector2, Vector4.zero);
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector1, Vector4.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, UnityEngine.Graphing.SlotType.Output, SlotValueType.Vector1, Vector2.zero);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("uv", "stripeCount"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("float widthOfEachStripe = 1.0 / stripeCount;", false);
            outputString.AddShaderChunk("float t = fmod(floor(uv.x / widthOfEachStripe), stripeCount);", false);
            outputString.AddShaderChunk("return lerp(0.0, 1.0, t / (stripeCount - 1.0));", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
