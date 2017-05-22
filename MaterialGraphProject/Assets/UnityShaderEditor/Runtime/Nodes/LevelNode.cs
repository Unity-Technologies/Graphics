namespace UnityEngine.MaterialGraph
{
    [Title("Art/Level Node")]
    public class LevelNode : Function1Input, IGeneratesFunction
    {
        [SerializeField]
        private float m_InputMin = 0.0f;
        [SerializeField]
        private float m_InputMax = 1.0f;
        [SerializeField]
        private float m_InputGamma = 1.0f;
        [SerializeField]
        private float m_OutputMin = 0.0f;
        [SerializeField]
        private float m_OutputMax = 1.0f;

        public float inputMin
        {
            get { return m_InputMin; }
            set { m_InputMin = value; }
        }

        public float inputMax
        {
            get { return m_InputMax; }
            set { m_InputMax = value; }
        }

        public float inputGamma
        {
            get { return m_InputGamma; }
            set { m_InputGamma = value; }
        }

        public float outputMin
        {
            get { return m_OutputMin; }
            set { m_OutputMin = value; }
        }

        public float outputMax
        {
            get { return m_OutputMax; }
            set { m_OutputMax = value; }
        }

        public LevelNode()
        {
            name = "LevelNode";
        }

        protected override string GetFunctionName()
        {
            return "unity_level_" + precision;
        }

        protected override string GetFunctionCallBody(string inputValue)
        {
            float inputInvGamma = 1.0f / m_InputGamma;
            return GetFunctionName() + "(" + inputValue + ", " + m_InputMin + ", " + m_InputMax + ", " + inputInvGamma + ", " + m_OutputMin + ", " + m_OutputMax + ")";
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk("inline " + precision + outputDimension + " unity_level_" + precision + " (" + precision + outputDimension + " arg1, "
                                        + precision + " inputMin, "
                                        + precision + " inputMax, "
                                        + precision + " inputInvGamma, "
                                        + precision + " outputMin, "
                                        + precision + " outputMax)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk(precision + inputDimension + " colorMinClamped = max(arg1 - inputMin, 0.0);", false);
            outputString.AddShaderChunk(precision + inputDimension + " colorMinMaxClamped = min(colorMinClamped / (inputMax - inputMin), 1.0);", false);
            outputString.AddShaderChunk("return lerp(outputMin, outputMax, pow(colorMinMaxClamped, inputInvGamma));", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
