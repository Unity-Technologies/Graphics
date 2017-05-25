namespace UnityEngine.MaterialGraph
{
    [Title("Math/Range/RandomRange")]
    public class RandomRangeNode : Function3Input, IGeneratesFunction
    {
        public RandomRangeNode()
        {
            name = "RandomRange";
        }

        protected override string GetFunctionName()
        {
            return "unity_randomrange_" + precision;
        }

        protected override string GetInputSlot1Name()
        {
            return "Seed";
        }

        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector2, Vector2.zero);
        }

        protected override string GetInputSlot2Name()
        {
            return "Min";
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector1, Vector2.zero);
        }

        protected override string GetInputSlot3Name()
        {
            return "Max";
        }

        protected override MaterialSlot GetInputSlot3()
        {
            return new MaterialSlot(InputSlot3Id, GetInputSlot3Name(), kInputSlot3ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector1, Vector3.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, UnityEngine.Graphing.SlotType.Output, SlotValueType.Vector1, Vector2.zero);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("seed", "min", "max"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("float randomno =  frac(sin(dot(seed, float2(12.9898, 78.233)))*43758.5453);", false);
            outputString.AddShaderChunk("return floor(randomno * (max - min + 1)) + min;", false);
            
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
