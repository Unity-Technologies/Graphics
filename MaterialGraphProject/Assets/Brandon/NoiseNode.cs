namespace UnityEngine.MaterialGraph
{
    [Title("Input/Procedural/Noise")]
    public class NoiseNode : Function1Input, IGeneratesFunction
    {
        public NoiseNode()
        {
            name = "Noise";
        }

        protected override string GetFunctionName()
        {
            return "unity_noise_" + precision;
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

            outputString.AddShaderChunk("inline float unity_randomValue (float2 uv)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453);", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);


            outputString.AddShaderChunk("inline float unity_interpolate (float a, float b, float t)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("return (1.0-t)*a + (t*b);", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            outputString.AddShaderChunk("inline float unity_valueNoise (float2 uv)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("float2 i = floor(uv);", false);
            outputString.AddShaderChunk("float2 f = frac(uv);", false);
            outputString.AddShaderChunk("f = f * f * (3.0 - 2.0 * f);", false);

            outputString.AddShaderChunk("uv = abs(frac(uv) - 0.5);", false);
            outputString.AddShaderChunk("float2 c0 = i + float2(0.0, 0.0);", false);
            outputString.AddShaderChunk("float2 c1 = i + float2(1.0, 0.0);", false);
            outputString.AddShaderChunk("float2 c2 = i + float2(0.0, 1.0);", false);
            outputString.AddShaderChunk("float2 c3 = i + float2(1.0, 1.0);", false);
            outputString.AddShaderChunk("float r0 = unity_randomValue(c0);", false);
            outputString.AddShaderChunk("float r1 = unity_randomValue(c1);", false);
            outputString.AddShaderChunk("float r2 = unity_randomValue(c2);", false);
            outputString.AddShaderChunk("float r3 = unity_randomValue(c3);", false);

            outputString.AddShaderChunk("float bottomOfGrid = unity_interpolate(r0, r1, f.x);", false);
            outputString.AddShaderChunk("float topOfGrid = unity_interpolate(r2, r3, f.x);", false);
            outputString.AddShaderChunk("float t = unity_interpolate(bottomOfGrid, topOfGrid, f.y);", false);
            outputString.AddShaderChunk("return t;", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);


            outputString.AddShaderChunk(GetFunctionPrototype("uv"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("float t = 0.0;", false);
            outputString.AddShaderChunk("for(int i = 0; i < 3; i++)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("float freq = pow(2.0, float(i));", false);
            outputString.AddShaderChunk("float amp = pow(0.5, float(3-i));", false);
            outputString.AddShaderChunk("t += unity_valueNoise(float2(uv.x/freq, uv.y/freq))*amp;", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);
            outputString.AddShaderChunk("return t;", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
