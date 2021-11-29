using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Defines the constant buffer register that will be used as binding point for the Probe Volumes constant buffer.
    /// </summary>
    public enum APVConstantBufferRegister
    {
        GlobalRegister = 5
    }

    [GenerateHLSL]
    /// <summary>
    /// Defines the method used to reduce leaking.
    /// </summary>
    public enum APVLeakReductionMode
    {
        /// <summary>
        /// Nothing special is done to prevent leaking. Cheapest option in terms of cost of sampling.
        /// </summary>
        None = 0,
        /// <summary>
        /// Validity based occlusion. It detects occlusion through invalid probes, meaning that occlusion is detected only when probes fall inside geometry. Cheaper than other options, but works only on some specific cases.
        /// </summary>
        ValidityBased = 1
    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true, constantRegister = (int)APVConstantBufferRegister.GlobalRegister)]
    internal unsafe struct ShaderVariablesProbeVolumes
    {
        public Vector3 _PoolDim;
        public float _ViewBias;

        public Vector3 _MinCellPosition;
        public float _PVSamplingNoise;

        public Vector3 _CellIndicesDim;
        public float _CellInMeters;

        public Vector4 _LeakReductionParams;

        public float _CellInMinBricks;
        public float _MinBrickSize;
        public int _IndexChunkSize;
        public float _NormalBias;

    }
}
