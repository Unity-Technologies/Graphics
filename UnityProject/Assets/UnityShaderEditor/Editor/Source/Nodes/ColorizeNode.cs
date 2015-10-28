using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Art/Colorize Node")]
    class ColorizeNode : Function1Input, IGeneratesFunction
    {
        [SerializeField]
        private Color m_Color = Color.blue;
        [SerializeField]
        private float m_Colorization = 0.0f;
        [SerializeField]
        private float m_Brightness = 1.0f;
        [SerializeField]
        private float m_Contrast = 1.0f;

        public override void OnCreate()
        {
            name = "ColorizeNode";
            base.OnCreate();
        }

        protected override string GetFunctionName() {return ""; }

        protected override string GetFunctionCallBody(string inputValue)
        {
            return "unity_colorize_" + precision + "(" + inputValue + ", " + precision + "4(" + m_Color.r + ", " + m_Color.g + ", " + m_Color.b + ", " + m_Color.a + "), " + m_Colorization + ", " + m_Brightness + ", " + m_Contrast + ")";
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();

            foreach (var thePrecisision in m_PrecisionNames)
            {
                outputString.AddShaderChunk("inline " + thePrecisision + "4 unity_colorize_" + thePrecisision + " (" + thePrecisision + "4 arg1, " + thePrecisision + "4 color, " + thePrecisision + " amount, " + thePrecisision + " brightness, " + thePrecisision + " contrast)", false);
                outputString.AddShaderChunk("{", false);
                outputString.Indent();
                outputString.AddShaderChunk(thePrecisision + "4 x = lerp(arg1, arg1 * color, amount);", false);
                outputString.AddShaderChunk("x *= brightness;", false);
                outputString.AddShaderChunk("x = pow(x, contrast);", false);
                outputString.AddShaderChunk("return x;", false);
                outputString.Deindent();
                outputString.AddShaderChunk("}", false);
            }

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
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
            m_Color = EditorGUILayout.ColorField("Tint", m_Color);
            m_Colorization = Slider("Colorization", m_Colorization, 0f, 1f);
            m_Brightness = Slider("Brightness", m_Brightness, 0f, 2f);
            m_Contrast = Slider("Contrast", m_Contrast, 0.3f, 4f);
            if (EditorGUI.EndChangeCheck())
                RegeneratePreviewShaders();
        }*/
    }
}
