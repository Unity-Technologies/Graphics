namespace UnityEditor.MaterialGraph
{
    [Title("Math/Div Node")]
    class DivNode : Function2Input, IGeneratesFunction
    {
        public override void Init()
        {
            name = "DivNode";
            base.Init();
        }

        protected override string GetFunctionName() {return "unity_div_" + precision; }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            foreach (var precision in m_PrecisionNames)
            {
                outputString.AddShaderChunk("inline " + precision + "4 unity_div_" + precision + " (" + precision + "4 arg1, " + precision + "4 arg2)", false);
                outputString.AddShaderChunk("{", false);
                outputString.Indent();
                outputString.AddShaderChunk("return arg1 / arg2;", false);
                outputString.Deindent();
                outputString.AddShaderChunk("}", false);
            }

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
