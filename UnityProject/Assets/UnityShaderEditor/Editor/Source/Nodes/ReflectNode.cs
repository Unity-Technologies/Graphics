using System;

namespace UnityEditor.Graphs.Material
{
    [Title("Math/Reflect Node")]
    class ReflectNode : Function2Input, IGeneratesFunction
    {
        public override void Init()
        {
            name = "ReflectNode";
            base.Init();
        }

        protected override string GetInputSlot1Name() {return "Normal"; }
        protected override string GetInputSlot2Name() {return "Direction"; }
        protected override string GetOutputSlotName() {return "Reflection"; }

        protected override string GetFunctionName() {return "unity_reflect_" + precision; }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            foreach (var precision in m_PrecisionNames)
            {
                outputString.AddShaderChunk("inline " + precision + "4 unity_reflect_" + precision + " (" + precision + "4 normal, " + precision + "4 direction)", false);
                outputString.AddShaderChunk("{", false);
                outputString.Indent();
                outputString.AddShaderChunk(precision + "3 vn = normalize(normal.xyz);", false);
                outputString.AddShaderChunk(precision + "3 vd = normalize(direction.xyz);", false);
                outputString.AddShaderChunk("return half4 (2 * dot(vn, vd) * vn - vd, 1.0);", false);
                outputString.Deindent();
                outputString.AddShaderChunk("}", false);
            }

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
