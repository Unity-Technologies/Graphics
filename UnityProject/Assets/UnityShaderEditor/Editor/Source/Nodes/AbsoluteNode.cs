namespace UnityEditor.MaterialGraph
{
    [Title("Math/Absolute Node")]
    class AbsoluteNode : Function1Input, IGeneratesFunction
    {
        public override void Init()
        {
            name = "AbsoluteNode";
            base.Init();
        }

        protected override string GetFunctionName() {return "unity_absolute_" + precision; }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            foreach (var precision in m_PrecisionNames)
            {
                outputString.AddShaderChunk("inline " + precision + "4 unity_absolute_" + precision + " (" + precision + "4 arg1)", false);
                outputString.AddShaderChunk("{", false);
                outputString.Indent();
                outputString.AddShaderChunk("return abs(arg1);", false);
                outputString.Deindent();
                outputString.AddShaderChunk("}", false);
            }

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
