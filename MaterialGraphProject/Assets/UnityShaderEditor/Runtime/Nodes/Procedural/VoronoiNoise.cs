namespace UnityEngine.MaterialGraph
{
    [Title("Procedural/Voronoi Noise")]
    public class VoronoiNoiseNode : Function1Input, IGeneratesFunction
    {
        public VoronoiNoiseNode()
        {
            name = "VoronoiNoise";
        }

        protected override string GetFunctionName()
        {
            return "unity_voronoinoise_" + precision;
        }

        protected override string GetInputSlotName()
        {
            return "UV";
        }

        protected override MaterialSlot GetInputSlot()
        {
            return new MaterialSlot(InputSlotId, GetInputSlotName(), kInputSlotShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector2, Vector2.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, UnityEngine.Graphing.SlotType.Output, SlotValueType.Vector1, Vector2.zero);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();

            outputString.AddShaderChunk("inline float unity_voronoi_noise_randomVector (float2 uv)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);", false);
            outputString.AddShaderChunk("return frac(sin(mul(uv, m)) * 46839.32);", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            outputString.AddShaderChunk(GetFunctionPrototype("uv"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("float2 g = floor(uv);", false);
            outputString.AddShaderChunk("float2 f = frac(uv);", false);
            outputString.AddShaderChunk("float t = 8.0;", false);
            outputString.AddShaderChunk("float3 res = float3(8.0, 0.0, 0.0);", false);


            outputString.AddShaderChunk("for(int y=-1; y<=1; y++)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("for(int x=-1; x<=1; x++)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("float2 lattice = float2(x,y);", false);
            outputString.AddShaderChunk("float2 offset = unity_voronoi_noise_randomVector(lattice + g);", false);
            outputString.AddShaderChunk("float d = distance(lattice + offset, f);", false);
            outputString.AddShaderChunk("if(d < res.x)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("res = float3(d, offset.x, offset.y);", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            outputString.AddShaderChunk("return res.x;", false);


            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
