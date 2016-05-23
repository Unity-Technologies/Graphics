using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Math/Add Node")]
    public class AddNode : Function2Input, IGeneratesFunction
    {
        public AddNode(AbstractMaterialGraph owner)
            : base(owner)
        {
            name = "AddNode";
        }

        protected override string GetFunctionName()
        {
            return "unity_add_" + precision;
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            foreach (var precision in m_PrecisionNames)
            {
                outputString.AddShaderChunk("inline " + precision + outputDimension + " unity_add_" + precision + " (" + precision + outputDimension +" arg1, " + precision + outputDimension + " arg2)", false);
                outputString.AddShaderChunk("{", false);
                outputString.Indent();
                outputString.AddShaderChunk("return arg1 + arg2;", false);
                outputString.Deindent();
                outputString.AddShaderChunk("}", false);
            }

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public override float GetNodeUIHeight(float width)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
