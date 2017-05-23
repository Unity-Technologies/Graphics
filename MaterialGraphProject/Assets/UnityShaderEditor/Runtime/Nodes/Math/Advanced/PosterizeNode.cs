namespace UnityEngine.MaterialGraph
{
    [Title("Math/Advanced/Posterize")]
    class PosterizeNode : Function2Input, IGeneratesFunction
    {
        public PosterizeNode()
        {
            name = "Posterize";
        }

        protected override string GetFunctionName() {return "unity_posterize_" + precision; }

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
