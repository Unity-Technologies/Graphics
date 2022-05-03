using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Defines the constant buffer register that will be used as binding point for the Probe Volumes constant buffer.
    /// </summary>
    public enum APVConstantBufferRegister
    {
        /// <summary>
        /// Global register
        /// </summary>
        GlobalRegister = 5
    }

    /// <summary>
    /// Defines the method used to reduce leaking.
    /// </summary>
    [GenerateHLSL]
    public enum APVLeakReductionMode
    {
        /// <summary>
        /// Nothing is done to prevent leaking. Cheapest option in terms of cost of sampling.
        /// </summary>
        None = 0,
        /// <summary>
        /// The uvw used to sample APV data are warped to try to have invalid probe not contributing to lighting. Also, a geometric weight based on normal at sampling position and vector to probes is used.
        /// This only modifies the uvw used, but still sample a single time. It is effective in some situations (especially when occluding object contain probes inside) but ineffective in many other.
        /// </summary>
        ValidityAndNormalBased = 1,

    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true, constantRegister = (int)APVConstantBufferRegister.GlobalRegister)]
    internal unsafe struct ShaderVariablesProbeVolumes
    {
        public Vector4 _PoolDim_CellInMeters;
        public Vector4 _MinCellPos_Noise;
        public Vector4 _IndicesDim_IndexChunkSize;
        public Vector4 _Biases_CellInMinBrick_MinBrickSize;
        public Vector4 _LeakReductionParams;
        public Vector4 _Weight_MinLoadedCell;
        public Vector4 _MaxLoadedCell_FrameIndex;
        public Vector4 _NormalizationClamp_Padding12;
    }
}
