using UnityEngine;

namespace UnityEditor.Graphs.Material
{
    [Title("Math/Multiply Node")]
    class MultiplyNode : FunctionMultiInput, IGeneratesFunction
    {
        public override void Init()
        {
            name = "MultiplyNode";
            base.Init();
        }

        protected override string GetFunctionName() { return "unity_multiply_" + precision; }

        public override Vector4 GetNewSlotDefaultValue()
        {
            return Vector4.one;
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            foreach (var precision in m_PrecisionNames)
            {
                outputString.AddShaderChunk("inline " + precision + "4 unity_multiply_" + precision + " (" + precision + "4 arg1, " + precision + "4 arg2)", false);
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
