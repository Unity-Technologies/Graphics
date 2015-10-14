using System;
using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Fractal/Kaleidoscope Node")]
    class KaleidoscopeNode : BaseMaterialNode, IGeneratesFunction, IGeneratesBodyCode
    {
        [SerializeField]
        private int m_Iterations = 6;

        private const string kPointInputName = "Point";
        private const string kConstant1InputName = "Constant1";
        private const string kConstant2InputName = "Constant2";
        private const string kOutputSlotName = "Output";

        public override bool hasPreview { get { return true; } }

        public override void Init()
        {
            name = "KaleidoscopeNode";
            base.Init();
            AddSlot(new Slot(SlotType.InputSlot, kPointInputName));
            AddSlot(new Slot(SlotType.InputSlot, kConstant1InputName));
            AddSlot(new Slot(SlotType.InputSlot, kConstant2InputName));
            AddSlot(new Slot(SlotType.OutputSlot, kOutputSlotName));
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            foreach (var precision in m_PrecisionNames)
            {
                outputString.AddShaderChunk("inline " + precision + "4 unity_kaleidoscope_" + precision + " (" + precision + "2 p, " + precision + "4 c1, " + precision + "4 c2, int iterations)", false);
                outputString.AddShaderChunk("{", false);
                outputString.Indent();
                outputString.AddShaderChunk("c1 = c1 * 2 - 1;", false);
                outputString.AddShaderChunk("c2 = c2 * 2 - 1;", false);
                outputString.AddShaderChunk("for (int n = 0; n < iterations; n++)", false);
                outputString.AddShaderChunk("{", false);
                outputString.Indent();
                outputString.AddShaderChunk("p = c1.xy + p * c1.zw + " + precision + "2(dot(p, c2.xy), dot(p, c2.zw));", false);
                outputString.AddShaderChunk("if(p.x < p.y) p.xy = -p.yx;", false);
                outputString.Deindent();
                outputString.AddShaderChunk("}", false);
                outputString.AddShaderChunk("return " + precision + "4(p, 1, 1);", false);
                outputString.Deindent();
                outputString.AddShaderChunk("}", false);
            }

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputSlot = FindOutputSlot(kOutputSlotName);
            var pointInput = FindInputSlot(kPointInputName);
            var constant1Input = FindInputSlot(kConstant1InputName);
            var constant2Input = FindInputSlot(kConstant2InputName);

            if (outputSlot == null || pointInput == null || constant1Input == null || constant2Input == null)
            {
                Debug.LogError("Invalid slot configuration on node: " + name);
                return;
            }

            string pointInputValue = GetSlotValue(pointInput, generationMode);
            string constant1InputValue = GetSlotValue(constant1Input, generationMode);
            string constant2InputValue = GetSlotValue(constant2Input, generationMode);

            string outName = GetOutputVariableNameForSlot(outputSlot, generationMode);
            visitor.AddShaderChunk(precision + "4 " + outName + " = unity_kaleidoscope_" + precision + "(" + pointInputValue + ".xy, " + constant1InputValue + ", " + constant2InputValue + ", " + m_Iterations + ");", false);
        }

        static float Slider(string title, float value, float from, float to)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title);
            value = GUILayout.HorizontalSlider(value, from, to, GUILayout.Width(64));
            GUILayout.EndHorizontal();
            return value;
        }

        public override void NodeUI(Graphs.GraphGUI host)
        {
            base.NodeUI(host);

            EditorGUI.BeginChangeCheck();
            m_Iterations = (int)Slider("Iterations", (float)m_Iterations, 1, 50);
            if (EditorGUI.EndChangeCheck())
                RegeneratePreviewShaders();
        }
    }
}
