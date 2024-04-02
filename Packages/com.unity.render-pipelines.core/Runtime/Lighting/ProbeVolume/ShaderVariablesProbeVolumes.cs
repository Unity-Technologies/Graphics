namespace UnityEngine.Rendering
{
    [GenerateHLSL]
    class APVDefinitions
    {
        public static int probeIndexChunkSize = ProbeBrickIndex.kIndexChunkSize;
    }

    /// <summary>
    /// Defines the constant buffer register that will be used as binding point for the Adaptive Probe Volumes constant buffer.
    /// </summary>
    public enum APVConstantBufferRegister
    {
        /// <summary>
        /// Global register
        /// </summary>
        GlobalRegister = 6
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
        /// The uvw used to sample APV data are warped to try to have invalid probe not contributing to lighting.
        /// This only modifies the uvw used, but still sample a single time. It is effective when using rendering layers or in some situations (especially when occluding object contain probes inside) but ineffective in many other.
        /// </summary>
        ValidityBased = 1,
        /// <summary>
        /// The uvw used to sample APV data are warped to try to have invalid probe not contributing to lighting. Also, a geometric weight based on normal at sampling position and vector to probes is used.
        /// This only modifies the uvw used, but still sample a single time. It is effective in some situations (especially when occluding object contain probes inside) but ineffective in many other.
        /// </summary>
        ValidityAndNormalBased = 2,

    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true, constantRegister = (int)APVConstantBufferRegister.GlobalRegister)]
    internal unsafe struct ShaderVariablesProbeVolumes
    {
        public Vector4 _Offset_IndirectionEntryDim;
        public Vector4 _Weight_MinLoadedCellInEntries;
        public Vector4 _PoolDim_MinBrickSize;
        public Vector4 _RcpPoolDim_XY;
        public Vector4 _MinEntryPos_Noise;
        public Vector4 _IndicesDim_FrameIndex;
        public Vector4 _Biases_NormalizationClamp;
        public Vector4 _LeakReduction_SkyOcclusion;
        public Vector4 _MaxLoadedCellInEntries_Padding;
    }
}
