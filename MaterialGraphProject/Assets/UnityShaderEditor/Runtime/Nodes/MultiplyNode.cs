using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Multiply Node")]
    public class MultiplyNode : Function2Input, IGeneratesFunction
    {
        public MultiplyNode()
        {
            name = "MultiplyNode";
        }
        
        protected override string GetFunctionName()
        {
            return "unity_multiply_" + precision;
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            foreach (var precision in m_PrecisionNames)
            {
                outputString.AddShaderChunk("inline " + precision + outputDimension + " " + GetFunctionName() + " (" + precision + input1Dimension + " arg1, " + precision + input2Dimension + " arg2)", false);
                outputString.AddShaderChunk("{", false);
                outputString.Indent();
                outputString.AddShaderChunk("return arg1 * arg2;", false);
                outputString.Deindent();
                outputString.AddShaderChunk("}", false);
            }

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
