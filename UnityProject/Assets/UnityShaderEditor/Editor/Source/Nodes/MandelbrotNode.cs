using System;
using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Fractal/Mandelbrot Node")]
    class MandelbrotNode : BaseMaterialNode, IGeneratesFunction, IGeneratesBodyCode
    {
        [SerializeField]
        private int m_Iterations = 6;

        private const string kPointInputName = "Point";
        private const string kConstantInputName = "Constant";
        private const string kOutputSlotName = "Output";

        public override bool hasPreview { get { return true; } }

        public override void OnCreate()
        {
            name = "MandelbrotNode";
            base.OnCreate();
            //AddSlot(new Slot(SlotType.InputSlot, kPointInputName));
            //AddSlot(new Slot(SlotType.InputSlot, kConstantInputName));
            //AddSlot(new Slot(SlotType.OutputSlot, kOutputSlotName));
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            foreach (var precision in m_PrecisionNames)
            {
                outputString.AddShaderChunk("inline " + precision + "4 unity_mandelbrot_" + precision + " (" + precision + "2 p, " + precision + "2 c, float limit, float scale, int iterations)", false);
                outputString.AddShaderChunk("{", false);
                outputString.Indent();
                outputString.AddShaderChunk(precision + " zr = p.x * 4 - 2, zi = p.y * 4 - 2, dzr = 1, dzi = 0, r = 0, len2;", false);
                outputString.AddShaderChunk("for (int n = 0; n < iterations; n++)", false);
                outputString.AddShaderChunk("{", false);
                outputString.Indent();
                outputString.AddShaderChunk(precision + " tmp1 = 2 * zr * zi + c.y; zr = zr * zr - zi * zi + c.x; zi = tmp1;", false);
                outputString.AddShaderChunk(precision + " tmp2 = 2 * (dzr * zi + dzi * zr); dzr = 2 * (dzr * zr - dzi * zi) + 1; dzi = tmp2;", false);
                outputString.AddShaderChunk("len2 = zr * zr + zi * zi;", false);
                outputString.AddShaderChunk("if (len2 > 1000000 * limit) { r = n; break; }", false);
                outputString.Deindent();
                outputString.AddShaderChunk("}", false);
                outputString.AddShaderChunk("return scale * sqrt(len2 / (dzr * dzr + dzi * dzi)) * log(len2);", false);
                outputString.Deindent();
                outputString.AddShaderChunk("}", false);
            }

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var validSlots = ListPool<Slot>.Get();
            GetValidInputSlots(validSlots);

            var outputSlot = outputSlots.FirstOrDefault(x => x.name == kOutputSlotName);
            var pointInput = validSlots.FirstOrDefault(x => x.name == kPointInputName);
            var constantInput = validSlots.FirstOrDefault(x => x.name == kConstantInputName);

            ListPool<Slot>.Release(validSlots);

            if (outputSlot == null || pointInput == null || constantInput == null)
            {
                Debug.LogError("Invalid slot configuration on node: " + name);
                return;
            }

            //TODO: This will break if there is NO input connected, use default in that case
            var pointProvider = pointInput.edges[0].fromSlot.node as BaseMaterialNode;
            var pointName = pointProvider.GetOutputVariableNameForSlot(pointInput.edges[0].fromSlot, generationMode);

            var constantProvider = constantInput.edges[0].fromSlot.node as BaseMaterialNode;
            var constantName = constantProvider.GetOutputVariableNameForSlot(constantInput.edges[0].fromSlot, generationMode);

            string outName = GetOutputVariableNameForSlot(outputSlot, generationMode);
            visitor.AddShaderChunk(precision + "4 " + outName + " = unity_mandelbrot_" + precision + "(" + pointName + ".xy, " + constantName + ".xy, " + constantName + ".z, " + constantName + ".w, " + m_Iterations + ");", false);
        }

        static float Slider(string title, float value, float from, float to)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title);
            value = GUILayout.HorizontalSlider(value, from, to, GUILayout.Width(64));
            GUILayout.EndHorizontal();
            return value;
        }

        /*public override void NodeUI()
        {
            base.NodeUI();

            EditorGUI.BeginChangeCheck();
            m_Iterations = (int)Slider("Iterations", (float)m_Iterations, 1, 50);
            if (EditorGUI.EndChangeCheck())
                RegeneratePreviewShaders();
        }*/
    }
}
