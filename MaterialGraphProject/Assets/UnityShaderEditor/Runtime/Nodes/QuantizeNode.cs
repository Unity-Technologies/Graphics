namespace UnityEngine.MaterialGraph
{
    [Title("Math/Quantize Node")]
    class QuantizeNode : Function2Input, IGeneratesFunction
    {
        public QuantizeNode()
        {
            name = "QuantizeNode";
        }

        protected override string GetFunctionName() {return "unity_quantize_" + precision; }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("input", "stepsize"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return floor(input / stepsize) * stepsize;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
