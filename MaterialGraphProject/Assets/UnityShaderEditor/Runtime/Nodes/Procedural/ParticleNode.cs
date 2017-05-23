namespace UnityEngine.MaterialGraph
{
    [Title("Procedural/Particle")]
    public class ParticleNode : Function2Input, IGeneratesFunction
    {
        public ParticleNode()
        {
            name = "Particle";
        }

        protected override string GetFunctionName()
        {
            return "unity_particle_" + precision;
        }

        protected override string GetInputSlot1Name()
        {
            return "UV";
        }

        protected override string GetInputSlot2Name()
        {
            return "ScaleFactor";
        }

        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector2, Vector2.zero);
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
            outputString.AddShaderChunk(GetFunctionPrototype("uv", "scaleFactor"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("uv = uv * 2.0 - 1.0;", false);
            outputString.AddShaderChunk("return abs(1.0/length(uv * scaleFactor));", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
