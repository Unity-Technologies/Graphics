namespace UnityEngine.MaterialGraph
{
    [Title("Math/Advanced/Reciprocal Node")]
    public class ReciprocalNode : Function1Input, IGeneratesFunction
    {
        public ReciprocalNode()
        {
            name = "ReciprocalNode";
        }

        protected override string GetFunctionName()
        {
            return "unity_reciprocal_" + precision;
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("arg1"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return 1.0/arg1;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
