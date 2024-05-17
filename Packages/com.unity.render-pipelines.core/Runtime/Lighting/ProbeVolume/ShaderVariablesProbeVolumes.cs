using System;
using Unity.Mathematics;

namespace UnityEngine.Rendering
{
    [GenerateHLSL]
    class APVDefinitions
    {
        public static int probeIndexChunkSize = ProbeBrickIndex.kIndexChunkSize;
        public const float probeValidityThreshold = 0.05f;

        public static int probeMaxRegionCount = 4;
        public static Color32[] layerMaskColors = new Color32[] {
            new Color32(230, 159, 0, 255),
            new Color32(0, 158, 115, 255),
            new Color32(0, 114, 178, 255),
            new Color32(204, 121, 167, 255),
        };

        public static Color debugEmptyColor = new Color(0.388f, 0.812f, 0.804f, 1.0f);
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
        /// This samples APV a single time so it's a cheap option but will only work in the simplest cases.
        /// </summary>
        Performance = 1,
        /// <summary>
        /// This option samples APV between 1 and 3 times to provide the smoothest result without introducing artifacts.
        /// This is as expensive as Performance mode in the simplest cases, and is better and more expensive in the most complex cases.
        /// </summary>
        Quality = 2,

        /// <summary>
        /// Obsolete, kept for migration.
        /// </summary>
        [Obsolete("Performance")]
        ValidityBased = Performance,
        /// <summary>
        /// Obsolete, kept for migration.
        /// </summary>
        [Obsolete("Quality")]
        ValidityAndNormalBased = Quality,
    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true, constantRegister = (int)APVConstantBufferRegister.GlobalRegister)]
    internal unsafe struct ShaderVariablesProbeVolumes
    {
        public Vector4 _Offset_LayerCount;
        public Vector4 _MinLoadedCellInEntries_IndirectionEntryDim;
        public Vector4 _MaxLoadedCellInEntries_RcpIndirectionEntryDim;
        public Vector4 _PoolDim_MinBrickSize;
        public Vector4 _RcpPoolDim_XY;
        public Vector4 _MinEntryPos_Noise;
        public uint4 _EntryCount_X_XY_LeakReduction;
        public Vector4 _Biases_NormalizationClamp;
        public Vector4 _FrameIndex_Weights;
        public uint4 _ProbeVolumeLayerMask;
    }
}
