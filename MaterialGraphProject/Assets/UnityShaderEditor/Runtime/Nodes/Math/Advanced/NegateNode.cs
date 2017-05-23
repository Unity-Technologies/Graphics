namespace UnityEngine.MaterialGraph
{
    [Title("Math/Advanced/Negate")]
    public class NegateNode : Function1Input, IGeneratesFunction
    {
        public NegateNode()
        {
            name = "Negate";
        }

        protected override string GetFunctionName()
        {
            return "unity_negate_" + precision;
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("arg1"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return -1 * arg1;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
