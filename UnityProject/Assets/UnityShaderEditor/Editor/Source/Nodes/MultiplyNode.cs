using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Math/Multiply Node")]
    internal class MultiplyNode : Function2Input, IGeneratesFunction
    {
        public override void OnCreate()
        {
            name = "MultiplyNode";
            base.OnCreate();
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
                outputString.AddShaderChunk("inline " + precision + outputDimension + " unity_multiply_" + precision + " (" + precision + input1Dimension + " arg1, " + precision + input2Dimension + " arg2)", false);
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
