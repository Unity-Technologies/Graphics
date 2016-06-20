using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Lerp Node")]
    public class LerpNode : Function3Input, IGeneratesFunction
    {
        public LerpNode()
        {
            name = "LerpNode";
        }

        protected override string GetFunctionName() {return "unity_lerp_" + precision; }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            foreach (var precision in m_PrecisionNames)
            {
                outputString.AddShaderChunk(GetFunctionPrototype("first", "second", "s"), false);
                outputString.AddShaderChunk("{", false);
                outputString.Indent();
                outputString.AddShaderChunk("return lerp(first, second, s);", false);
                outputString.Deindent();
                outputString.AddShaderChunk("}", false);
            }

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
