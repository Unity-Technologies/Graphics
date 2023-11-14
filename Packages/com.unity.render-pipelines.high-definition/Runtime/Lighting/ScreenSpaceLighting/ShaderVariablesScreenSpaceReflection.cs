namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesScreenSpaceReflection
    {
        public float _SsrThicknessScale;
        public float _SsrThicknessBias;
        public int _SsrStencilBit;
        public int _SsrIterLimit;

        public float _SsrRoughnessFadeEnd;
        public float _SsrRoughnessFadeRcpLength;
        public float _SsrRoughnessFadeEndTimesRcpLength;
        public float _SsrEdgeFadeRcpLength;

        public int _SsrDepthPyramidMaxMip;
        public int _SsrColorPyramidMaxMip;
        public int _SsrReflectsSky;
        public float _SsrAccumulationAmount;

        public float _SsrPBRSpeedRejection;
        public float _SsrPBRBias;
        public float _SsrPRBSpeedRejectionScalerFactor;
        public float _SsrPBRPad0;
    }
}
