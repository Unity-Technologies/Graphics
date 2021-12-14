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

    [GenerateHLSL(needAccessors = false, generateCBuffer = true, constantRegister = (int)APVConstantBufferRegister.GlobalRegister)]
    internal unsafe struct ShaderVariablesProbeVolumes
    {
        public Vector4 _PoolDim_CellInMeters;
        public Vector4 _MinCellPos_Noise;
        public Vector4 _IndicesDim_IndexChunkSize;
        public Vector4 _Biases_CellInMinBrick_MinBrickSize;
    }
}
