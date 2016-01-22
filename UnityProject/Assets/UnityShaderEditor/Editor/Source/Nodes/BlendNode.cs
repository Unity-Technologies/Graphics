using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Art/Blend Node")]
    class BlendNode : Function2Input, IGeneratesFunction
    {
        public override void OnCreate()
        {
            name = "BlendNode";
            base.OnCreate();
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

        private static readonly string[] kOpNames = {
            "normal",
            "add"
        };

        protected override string GetFunctionName() { return "unity_blend_" + kOpNames[(int)m_Operation] + "_" + precision; }
        protected override string GetFunctionCallBody(string input1Value, string input2Value)
        {
            return GetFunctionName() + "(" + input1Value + ", " + input2Value + ", " + m_Blend + ")";
        }

        protected void AddOperationBody(ShaderGenerator visitor, string name, string body, string precision)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk("inline " + precision + outputDimension +" unity_blend_" + name + "_" + precision + " (" + precision + outputDimension + " arg1, " + precision + outputDimension + " arg2, " + precision + " blend)", false);
            outputString.AddShaderChunk("{", false); outputString.Indent();
            outputString.AddShaderChunk(body, false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
        public override float GetNodeUIHeight(float width)
        {
            return 2.0f * EditorGUIUtility.singleLineHeight;
        }

        public override bool NodeUI(Rect drawArea)
        {
            base.NodeUI(drawArea);

            EditorGUI.BeginChangeCheck();
            m_Blend = GUI.HorizontalSlider(new Rect(drawArea.x, drawArea.y, drawArea.width, EditorGUIUtility.singleLineHeight), m_Blend, 0f, 1f);
            m_Operation = (Operation) EditorGUI.EnumPopup(new Rect(drawArea.x, drawArea.y + EditorGUIUtility.singleLineHeight, drawArea.width, EditorGUIUtility.singleLineHeight), m_Operation);
            if (EditorGUI.EndChangeCheck())
            {
                pixelGraph.RevalidateGraph();
                return true;
            }
            return false;
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var precision in m_PrecisionNames)
            {
                AddOperationBody(visitor, kOpNames[(int)Operation.Normal], "return lerp(arg1, arg2, blend);", precision);
                AddOperationBody(visitor, kOpNames[(int)Operation.Additive], "return (arg1 + arg2) * blend;", precision);
            }
        }
    }
}
