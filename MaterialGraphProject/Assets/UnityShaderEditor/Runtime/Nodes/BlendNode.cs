using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Art/Blend Node")]
    public class BlendNode : Function2Input, IGeneratesFunction
    {
        public BlendNode()
        {
            name = "BlendNode";
        }

        public enum Operation
        {
            Normal,
            Additive,
        }

        [SerializeField]
        private Operation m_Operation;
        [SerializeField]
        private float m_Blend = 0.5f;

        private static readonly string[] kOpNames =
        {
            "normal",
            "add"
        };

        public Operation operation
        {
            get { return m_Operation; }
            set { m_Operation = value; }
        }

        public float blend
        {
            get { return m_Blend; }
            set { m_Blend = value; }
        }

        protected override string GetFunctionName() { return "unity_blend_" + kOpNames[(int)m_Operation] + "_" + precision; }

        protected override string GetFunctionCallBody(string input1Value, string input2Value)
        {
            return GetFunctionName() + "(" + input1Value + ", " + input2Value + ", " + m_Blend + ")";
        }

        protected void AddOperationBody(ShaderGenerator visitor, string name, string body)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk("inline " + precision + outputDimension + " unity_blend_" + name + "_" + precision + " (" + precision + outputDimension + " arg1, " + precision + outputDimension + " arg2, " + precision + " blend)", false);
            outputString.AddShaderChunk("{", false); outputString.Indent();
            outputString.AddShaderChunk(body, false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            AddOperationBody(visitor, kOpNames[(int)Operation.Normal], "return lerp(arg1, arg2, blend);");
            AddOperationBody(visitor, kOpNames[(int)Operation.Additive], "return (arg1 + arg2) * blend;");
        }
    }
}
