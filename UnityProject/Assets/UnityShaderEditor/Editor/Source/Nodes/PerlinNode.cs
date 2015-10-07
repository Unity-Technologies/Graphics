using UnityEngine;

namespace UnityEditor.Graphs.Material
{
    [Title("Fractal/Perlin Node")]
    class PerlinNode : TextureNode, IGeneratesFunction, IGeneratesBodyCode
    {
        public override bool hasPreview { get { return true; } }

        [SerializeField]
        private int m_Iterations = 4;

        [SerializeField]
        private float m_Decay = 0.5f;

        [SerializeField]
        private float m_Frequency = 2.0f;

        public override void Init()
        {
            name = "Perlin";
            base.Init();
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            foreach (var precision in m_PrecisionNames)
            {
                outputString.AddShaderChunk("inline " + precision + "4 unity_perlin_" + precision + " ("
                    + "sampler2D textureID, "
                    + "int iterations, "
                    + precision + " decay, "
                    + precision + " frequency, "
                    + precision + "2 p"
                    + ")", false);
                outputString.AddShaderChunk("{", false);
                outputString.Indent();
                outputString.AddShaderChunk(precision + "4 sum = " + precision + "4(0, 0, 0, 0);", false);
                outputString.AddShaderChunk(precision + " amp = 0.5;", false);
                outputString.AddShaderChunk("for(int n = 0; n < iterations; n++)", false);
                outputString.AddShaderChunk("{", false);
                outputString.Indent();
                outputString.AddShaderChunk("sum += amp * tex2D (textureID, p);", false);
                outputString.AddShaderChunk("p *= frequency;", false);
                outputString.AddShaderChunk("amp *= decay;", false);
                outputString.Deindent();
                outputString.AddShaderChunk("}", false);
                outputString.AddShaderChunk("return sum;", false);
                outputString.Deindent();
                outputString.AddShaderChunk("}", false);
            }

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public override void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputSlot = FindOutputSlot(kOutputSlotName);
            if (outputSlot == null)
                return;

            var uvSlot = FindInputSlot(kUVSlotName);
            if (uvSlot == null)
                return;

            var uvName = "IN.meshUV0";
            if (uvSlot.edges.Count > 0)
            {
                var fromNode = uvSlot.edges[0].fromSlot.node as BaseMaterialNode;
                uvName = fromNode.GetOutputVariableNameForSlot(uvSlot.edges[0].fromSlot, generationMode);
            }

            string body = "unity_perlin_" + precision + "(" + GetPropertyName() + ", " + m_Iterations + ", " + m_Decay + ", " + m_Frequency + ", " + uvName + ".xy)";
            visitor.AddShaderChunk("float4 " + GetOutputVariableNameForSlot(outputSlot, generationMode) + " = " + body + ";", true);
        }

        static float Slider(string title, float value, float from, float to)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title);
            value = GUILayout.HorizontalSlider(value, from, to, GUILayout.Width(64));
            GUILayout.EndHorizontal();
            return value;
        }

        public override void NodeUI(GraphGUI host)
        {
            base.NodeUI(host);

            EditorGUI.BeginChangeCheck();
            m_Iterations = (int)Slider("Iterations", (float)m_Iterations, 1, 8);
            m_Decay = Slider("Decay", m_Decay, -1f, 1f);
            m_Frequency = Slider("Frequency", m_Frequency, 0f, 5f);
            if (EditorGUI.EndChangeCheck())
                RegeneratePreviewShaders();
        }
    }
}
