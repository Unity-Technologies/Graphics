using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Rendering;

using static UnityEngine.Rendering.HighDefinition.ProbePropagationBasis;

[assembly: InternalsVisibleTo("Unity.Entities.Hybrid.HybridComponents")]
[assembly: InternalsVisibleTo("Unity.Rendering.Hybrid")]

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    internal struct PackedNeighborHit
    {
        public uint indexValidity;
        public uint albedoDistance;
        public uint normalAxis;
        public uint emission;
        public uint mixedLighting;
    }

    [Serializable]
    internal struct NeighborAxis
    {
        public uint hitIndexValidity;

        public const uint Miss = 16777215; // 2 ^ 24 max value
    }

    internal struct ProbeVolumePropagationPipelineData
    {
        public ComputeBuffer neighborHits;
        public ComputeBuffer neighbors;
        public ComputeBuffer radianceCacheAxis0;
        public ComputeBuffer radianceCacheAxis1;
        public ComputeBuffer hitRadianceCache;
        public ComputeBuffer dirtyFlags0;
        public ComputeBuffer dirtyFlags1;
        public int radianceReadIndex;
        public int buffersDataVersion;
        public int simulationFrameTick;
        public ProbeVolumeDynamicGIRadianceEncoding radianceEncoding;

        public ComputeBuffer GetReadRadianceCacheAxis()
        {
            return (radianceReadIndex == 0) ? radianceCacheAxis0 : radianceCacheAxis1;
        }

        public ComputeBuffer GetWriteRadianceCacheAxis()
        {
            return (radianceReadIndex == 0) ? radianceCacheAxis1 : radianceCacheAxis0;
        }

        public ComputeBuffer GetDirtyFlags()
        {
            return (radianceReadIndex == 0) ? dirtyFlags0 : dirtyFlags1;
        }

        public ComputeBuffer GetNextDirtyFlags()
        {
            return (radianceReadIndex == 0) ? dirtyFlags1 : dirtyFlags0;
        }

        public void SwapRadianceCaches()
        {
            radianceReadIndex = (radianceReadIndex + 1) % 2;
        }

        public static ProbeVolumePropagationPipelineData Empty => new ProbeVolumePropagationPipelineData
        {
            buffersDataVersion = -1,
            simulationFrameTick = -1,
        };
    }

    internal struct ProbeVolumeDynamicGIDispatchData
    {
        public ProbeDynamicGI giSettings;
        public ShaderVariablesGlobal globalCB;
        public SphericalHarmonicsL2 ambientProbe;
        public bool infiniteBounces;
        public bool dirtyFlagsDisabled;
        public int propagationQuality;
        public ProbeVolumeDynamicGIMixedLightMode mixedLightMode;
        public ProbeVolumeDynamicGIRadianceEncoding radianceEncoding;
        public ProbeVolumesEncodingModes encodingMode;
    }

    public partial class ProbeVolumeDynamicGI
    {
        private static ProbeVolumeDynamicGI s_Instance = new ProbeVolumeDynamicGI();
        public static ProbeVolumeDynamicGI instance { get { return s_Instance; } }

        internal static float s_DiagonalDist;
        internal static float s_Diagonal;
        internal static float s_2DDiagonalDist;
        internal static float s_2DDiagonal;
        internal static Vector4[] s_NeighborAxis;
        static Vector4[] s_AmbientProbe;

        private ComputeShader _PropagationClearRadianceShader = null;
        private ComputeShader _PropagationInitializeShader = null;
        private ComputeShader _PropagationHitsShader = null;
        private ComputeShader _PropagationDirtyBlockList = null;
        private ComputeShader _PropagationResetDirtyFlags = null;
        private ComputeShader _PropagationAxesShader = null;
        private ComputeShader _PropagationCombineShader = null;

        private NeighborAxisLookup[] _sortedNeighborAxisLookups;
        private ComputeBuffer _sortedNeighborAxisLookupsBuffer;
        private ProbeVolumeSimulationRequest[] _probeVolumeSimulationRequests;

        private ComputeBuffer _dirtyBlockListBuffer;
        private ComputeBuffer _dirtyBlockIndirectBuffer;

        private const int MAX_SIMULATIONS_PER_FRAME = 128;
        private int _propagationSettingsHash;
        private int _probeVolumeSimulationRequestCount = 0;
        private int _probeVolumeSimulationFrameTick = 0;

        private Material _DebugDirtyFlagsMaterial = null;
        private Shader _DebugDirtyFlagsShader = null;
        private MaterialPropertyBlock _DebugDirtyFlagsMaterialPropertyBlock = null;

        public enum PropagationQuality
        {
            Low,
            Medium,
            High
        }

        [Serializable]
        public enum ProbeVolumeDynamicGIBasis
        {
            BasisSphericalGaussian = 0,
            BasisSphericalGaussianWindowed,
            BasisAmbientDiceSharp,
            BasisAmbientDiceSofter,
            BasisAmbientDiceSuperSoft,
            BasisAmbientDiceUltraSoft
        }

        [Serializable]
        public enum ProbeVolumeDynamicGIBasisPropagationOverride
        {
            None = 0,
            BasisSphericalGaussian,
            BasisAmbientDiceWrappedSofter,
            BasisAmbientDiceWrappedSuperSoft,
            BasisAmbientDiceWrappedUltraSoft
        }

        ProbeVolumeDynamicGI()
        {
            s_DiagonalDist = Mathf.Sqrt(3.0f);
            s_Diagonal = 1.0f / s_DiagonalDist;
            s_2DDiagonalDist = Mathf.Sqrt(2.0f);
            s_2DDiagonal = 1.0f / s_2DDiagonalDist;

            s_NeighborAxis = new Vector4[]
            {
                // middle slice
                new Vector4( 1,  0,  0, 1),
                new Vector4( s_2DDiagonal,  0,  s_2DDiagonal, s_2DDiagonalDist),
                new Vector4( s_2DDiagonal,  0, -s_2DDiagonal, s_2DDiagonalDist),
                new Vector4(-1,  0,  0, 1),
                new Vector4(-s_2DDiagonal,  0,  s_2DDiagonal, s_2DDiagonalDist),
                new Vector4(-s_2DDiagonal,  0, -s_2DDiagonal, s_2DDiagonalDist),
                new Vector4( 0,  0,  1, 1),
                new Vector4( 0,  0, -1, 1),

                // upper slice
                new Vector4( 0,  1,  0, 1),
                new Vector4( s_2DDiagonal,  s_2DDiagonal,  0, s_2DDiagonalDist),
                new Vector4( s_Diagonal,  s_Diagonal,  s_Diagonal, s_DiagonalDist),
                new Vector4( s_Diagonal,  s_Diagonal, -s_Diagonal, s_DiagonalDist),
                new Vector4(-s_2DDiagonal,  s_2DDiagonal,  0, s_2DDiagonalDist),
                new Vector4(-s_Diagonal,  s_Diagonal,  s_Diagonal, s_DiagonalDist),
                new Vector4(-s_Diagonal,  s_Diagonal, -s_Diagonal, s_DiagonalDist),
                new Vector4( 0,  s_2DDiagonal,  s_2DDiagonal, s_2DDiagonalDist),
                new Vector4( 0,  s_2DDiagonal, -s_2DDiagonal, s_2DDiagonalDist),

                // lower slice
                new Vector4( 0, -1,  0, 1),
                new Vector4( s_2DDiagonal, -s_2DDiagonal,  0, s_2DDiagonalDist),
                new Vector4( s_Diagonal, -s_Diagonal,  s_Diagonal, s_DiagonalDist),
                new Vector4( s_Diagonal, -s_Diagonal, -s_Diagonal, s_DiagonalDist),
                new Vector4(-s_2DDiagonal, -s_2DDiagonal,  0, s_2DDiagonalDist),
                new Vector4(-s_Diagonal, -s_Diagonal,  s_Diagonal, s_DiagonalDist),
                new Vector4(-s_Diagonal, -s_Diagonal, -s_Diagonal, s_DiagonalDist),
                new Vector4( 0, -s_2DDiagonal,  s_2DDiagonal, s_2DDiagonalDist),
                new Vector4( 0, -s_2DDiagonal, -s_2DDiagonal, s_2DDiagonalDist),
            };

            s_AmbientProbe = new Vector4[7];

            _sortedNeighborAxisLookups = new NeighborAxisLookup[s_NeighborAxis.Length * s_NeighborAxis.Length];
            _probeVolumeSimulationRequests = new ProbeVolumeSimulationRequest[MAX_SIMULATIONS_PER_FRAME];
        }

        private bool _clearAllActive = false;
        public void ClearAllActive(bool clearAll)
        {
            _clearAllActive = clearAll;
        }

        public struct ProbeVolumeDynamicGIStats
        {
            public int numSimulatedProbeVolumes;
            public int numSimulatedProbes;
            public int totalDynamicGIProbes;

            public void Reset()
            {
                numSimulatedProbeVolumes = 0;
                numSimulatedProbes = 0;
                totalDynamicGIProbes = 0;
            }

            internal void SimulationRequested(ProbeVolumeHandle probeVolume)
            {
                ++totalDynamicGIProbes;
            }

            internal void Simulated(ProbeVolumeHandle probeVolume)
            {
                ++numSimulatedProbeVolumes;
                numSimulatedProbes += probeVolume.parameters.resolutionX * probeVolume.parameters.resolutionY * probeVolume.parameters.resolutionZ;
            }
        }

        private ProbeVolumeDynamicGIStats _stats;
        public ProbeVolumeDynamicGIStats GetStats()
        {
            return _stats;
        }

        internal static void AllocateNeighbors(ref ProbeVolumePayload payload, int numHitAxis, int numAxis)
        {
            int totalProbes = numAxis / s_NeighborAxis.Length;
            int totalProbeNeighborCapacity = (1 << 19);
            Debug.Assert(totalProbes < totalProbeNeighborCapacity);

            payload.hitNeighborAxis= new PackedNeighborHit[numHitAxis];
            payload.neighborAxis = new NeighborAxis[numAxis];
        }


        internal static void EnsureNeighbors(ref ProbeVolumePayload payload, int hitNeighborAxis, int neighborAxis)
        {
            if (payload.hitNeighborAxis == null
                || payload.neighborAxis == null
                || payload.hitNeighborAxis.Length != hitNeighborAxis
                || payload.neighborAxis.Length != neighborAxis)
            {
                AllocateNeighbors(ref payload, hitNeighborAxis, neighborAxis);
            }
        }

        internal static void Copy(ref ProbeVolumePayload payloadSrc, ref ProbeVolumePayload payloadDst)
        {
            if (payloadSrc.hitNeighborAxis != null)
            {
                if (payloadDst.hitNeighborAxis == null || payloadDst.hitNeighborAxis.Length != payloadSrc.hitNeighborAxis.Length)
                {
                    payloadDst.hitNeighborAxis = new PackedNeighborHit[payloadSrc.hitNeighborAxis.Length];
                }

                Array.Copy(payloadSrc.hitNeighborAxis, payloadDst.hitNeighborAxis, payloadSrc.hitNeighborAxis.Length);
            }
            if (payloadSrc.neighborAxis != null)
            {
                if (payloadDst.neighborAxis == null || payloadDst.neighborAxis.Length != payloadSrc.neighborAxis.Length)
                {
                    payloadDst.neighborAxis = new NeighborAxis[payloadSrc.neighborAxis.Length];
                }

                Array.Copy(payloadSrc.neighborAxis, payloadDst.neighborAxis, payloadSrc.neighborAxis.Length);
            }
        }

        internal static void SetNeighborDataHit(ref ProbeVolumePayload payload, Vector3 albedo, Vector3 emission, Vector3 normal, float distance, float validity, int probeIndex, int axis, int hitIndex, float maxDensity)
        {
            ref var neighborDataHits = ref payload.hitNeighborAxis;
            if (hitIndex >= neighborDataHits.Length)
            {
                Debug.Assert(false, "Probe Volume Neighbor Indexing Code Error");
                return;
            }

            neighborDataHits[hitIndex] = new PackedNeighborHit
            {
                indexValidity = PackIndexAndValidity((uint) probeIndex, (uint) axis, validity),
                albedoDistance = PackAlbedoAndDistance(albedo, distance, maxDensity * s_DiagonalDist),
                normalAxis = PackNormalAndAxis(normal, axis),
                emission = PackEmission(emission)
            };
        }

        private static void SetNeighborData(ref ProbeVolumePayload payload, float validity, int probeIndex, int axis, uint hitIndexOrMiss)
        {
            ref var neighborData = ref payload.neighborAxis;
            var probeCount = neighborData.Length / s_NeighborAxis.Length;
            var axisIndex = axis * probeCount + probeIndex;
            if (axisIndex >= neighborData.Length)
            {
                Debug.Assert(false, "Probe Volume Neighbor Indexing Code Error");
                return;
            }

            neighborData[axisIndex] = new NeighborAxis
            {
                hitIndexValidity = PackIndexAndValidityOnly(hitIndexOrMiss, validity)
            };
        }

        internal static Vector4 PackProbeVolumeResolution(Vector3 resolution)
        {
            return new Vector4(
                resolution.x,
                resolution.y,
                resolution.z,
                resolution.x * resolution.y * resolution.z);
        }

        internal static Vector4 PackProbeVolumeBlockResolution(Vector3 resolution)
        {
            var blockCountX = ((uint)resolution.x + 3) / 4;
            var blockCountY = ((uint)resolution.y + 3) / 4;
            var blockCountZ = ((uint)resolution.z + 3) / 4;
            return new Vector4(
                blockCountX,
                blockCountY,
                blockCountZ,
                blockCountX * blockCountY * blockCountZ);
        }

        private static uint PackAlbedoAndDistance(Vector3 color, float distance, float maxDistance)
        {
            float albedoR = Mathf.Clamp01(color.x);
            float albedoG = Mathf.Clamp01(color.y);
            float albedoB = Mathf.Clamp01(color.z);

            float normalizedDistance = Mathf.Clamp01(distance / maxDistance);

            uint packedOutput = 0;

            packedOutput |= (uint)(albedoR * 255f) << 0;
            packedOutput |= (uint)(albedoG * 255f) << 8;
            packedOutput |= (uint)(albedoB * 255f) << 16;
            packedOutput |= (uint)(normalizedDistance * 255f) << 24;

            return packedOutput;
        }

        internal static uint PackEmission(Vector3 color)
        {
            var maxChannel = color.x > color.y ? color.x : color.y;
            maxChannel = maxChannel > color.z ? maxChannel : color.z;

            // This byte value in M will result in the color range [0, 1].
            const float multiplierToByteScale = 32f;

            byte m = (byte)Mathf.CeilToInt(maxChannel * multiplierToByteScale);
            color *= 255f * multiplierToByteScale / m;

            uint packedOutput = 0;

            packedOutput |= (uint)Mathf.Min(255f, color.x) << 0;
            packedOutput |= (uint)Mathf.Min(255f, color.y) << 8;
            packedOutput |= (uint)Mathf.Min(255f, color.z) << 16;
            packedOutput |= (uint)m << 24;

            return packedOutput;
        }

        // { probeIndex: 19 bits, validity: 8bit, axis: 5bit }
        private static uint PackIndexAndValidity(uint probeIndex, uint axisIndex, float validity)
        {
            uint output = 0;

            output |= axisIndex;
            output |= ((uint)(validity * 255f) & 255) << 5;
            output |= probeIndex << 13;

            return output;
        }

        // { probeIndex: 24 bits, validity: 8bit }
        private static uint PackIndexAndValidityOnly(uint probeIndex, float validity)
        {
            uint output = 0;

            output |= (uint)(validity * 255f) & 255;
            output |= probeIndex << 8;

            return output;
        }

        private static uint PackAxisDir(Vector4 axis)
        {
            uint axisType = (axis.w == 1.0f) ? 0u :
                axis.w < 1.5f ? 1u :
                2u;

            uint encodedX = axis.x < 0 ? 0u :
                axis.x == 0 ? 1u :
                2u;

            uint encodedY = axis.y < 0 ? 0u :
                axis.y == 0 ? 1u :
                2u;

            uint encodedZ = axis.z < 0 ? 0u :
                axis.z == 0 ? 1u :
                2u;

            uint output = 0;
            // Encode type of axis in bit [7:8]
            output |= (axisType << 6);
            // Encode axis signs in [5:6] [3:4] [1:2]
            output |= (encodedZ << 4);
            output |= (encodedY << 2);
            output |= (encodedX << 0);

            return output;
        }

        // Same as PackNormalOctQuadEncode and PackFloat2To888 in Packing.hlsl
        private static uint PackNormalAndAxis(Vector3 N, int axisIndex)
        {
            uint packedOutput = 0;
            var octN = LightUtils.PackNormalOctQuadEncode(N);

            octN *= 0.5f;
            octN += new Vector2(0.5f, 0.5f);

            uint i0 = (uint)(octN.x * 4095.5f);
            uint i1 = (uint)(octN.y * 4095.5f);

            packedOutput |= (i0 << 0);
            packedOutput |= (i1 << 12);

            var axis = s_NeighborAxis[axisIndex];
            packedOutput |= (PackAxisDir(axis) << 24);

            return packedOutput;
        }

        static readonly Matrix4x4 LOG_LUV_ENCODE_MAT = new Matrix4x4(
            new Vector4(0.2209f, 0.3390f, 0.4184f, 0f),
            new Vector4(0.1138f, 0.6780f, 0.7319f, 0f),
            new Vector4(0.0102f, 0.1130f, 0.2969f, 0f),
            Vector4.zero);

        static readonly Matrix4x4 LOG_LUV_DECODE_MAT = new Matrix4x4(
            new Vector4( 6.0014f, -2.7008f, -1.7996f, 0f),
            new Vector4(-1.3320f,  3.1029f, -5.7721f, 0f),
            new Vector4( 0.3008f, -1.0882f,  5.6268f, 0f),
            Vector4.zero);

        static Vector3 LogluvFromRgb(Vector3 vRGB)
        {
            Vector3 vResult;
            Vector3 Xp_Y_XYZp = LOG_LUV_ENCODE_MAT * vRGB;
            Xp_Y_XYZp = new Vector3(
                Mathf.Max(Xp_Y_XYZp.x, 1e-6f),
                Mathf.Max(Xp_Y_XYZp.y, 1e-6f),
                Mathf.Max(Xp_Y_XYZp.z, 1e-6f));
            vResult.x = Xp_Y_XYZp.x / Xp_Y_XYZp.z;
            vResult.y = Xp_Y_XYZp.y / Xp_Y_XYZp.z;
            // float Le = log2(Xp_Y_XYZp.y) * (2.0 / 255.0) + (127.0 / 255.0); // original super large range
            float Le = Mathf.Log(Xp_Y_XYZp.y, 2f) * (1f / (20f + 16.61f)) + (16.61f / (20f + 16.61f)); // map ~[1e-5, 1M] to [0, 1] range
            vResult.z = Le;
            return vResult;
        }

        static Vector3 RgbFromLogluv(Vector3 vLogLuv)
        {
            Vector3 Xp_Y_XYZp;
            // Xp_Y_XYZp.y = exp2(vLogLuv.z * 127.5 - 63.5); // original super large range
            Xp_Y_XYZp.y = Mathf.Pow(2f, vLogLuv.z * (20f + 16.61f) - 16.61f); // map [0, 1] to ~[1e-5, 1M] range
            Xp_Y_XYZp.z = Xp_Y_XYZp.y / vLogLuv.y;
            Xp_Y_XYZp.x = vLogLuv.x * Xp_Y_XYZp.z;
            Vector3 vRGB = LOG_LUV_DECODE_MAT * Xp_Y_XYZp;
            return new Vector3(
                Mathf.Max(vRGB.x, 0f),
                Mathf.Max(vRGB.y, 0f),
                Mathf.Max(vRGB.z, 0f));
        }

        static Vector3 LuvFromRgb(Vector3 vRGB)
        {
            Vector3 vResult; 
            Vector3 Xp_Y_XYZp = LOG_LUV_ENCODE_MAT * vRGB;
            Xp_Y_XYZp = new Vector3(
                Mathf.Max(Xp_Y_XYZp.x, 1e-6f),
                Mathf.Max(Xp_Y_XYZp.y, 1e-6f),
                Mathf.Max(Xp_Y_XYZp.z, 1e-6f));
            vResult.x = Xp_Y_XYZp.x / Xp_Y_XYZp.z;
            vResult.y = Xp_Y_XYZp.y / Xp_Y_XYZp.z;
            float Le = Xp_Y_XYZp.y; // Raw range
            vResult.z = Le;
            return vResult;
        }
        
        static Vector3 RgbFromLuv(Vector3 vLogLuv)
        {
            Vector3 Xp_Y_XYZp;
            Xp_Y_XYZp.y = vLogLuv.z; // Raw range
            Xp_Y_XYZp.z = Xp_Y_XYZp.y / vLogLuv.y;
            Xp_Y_XYZp.x = vLogLuv.x * Xp_Y_XYZp.z;
            Vector3 vRGB = LOG_LUV_DECODE_MAT * Xp_Y_XYZp;
            return new Vector3(
                Mathf.Max(vRGB.x, 0f),
                Mathf.Max(vRGB.y, 0f),
                Mathf.Max(vRGB.z, 0f));
        }
        
        static uint EncodeSimpleUHalfFloat(float x)
        {
            x = Mathf.Clamp(x, Mathf.Pow(2f, -15f), Mathf.Pow(2f, 16f));
        
            uint floatBits = UnsafeUtility.As<float, uint>(ref x);
        
            uint floatFraction = floatBits & ((1u << 23) - 1u);
            uint floatExponent = floatBits >> 23;
        
            uint halfFraction = floatFraction >> (23 - 11); // truncate.
        
            // float bias: -127
            // half bias: -15
            // diff bias: -112.
            uint halfExponent = (floatExponent < 112u) ? 0u : (floatExponent - 112u); 
            halfExponent = Math.Min(31u, halfExponent); // Clamp shouldnt be necessary.
            
            return (halfExponent << 11) | halfFraction;
        }
        
        static float DecodeSimpleUHalfFloat(uint halfBits)
        {
            uint halfExponent = halfBits >> 11;
            uint halfFraction = halfBits & ((1u << 11) - 1u);
        
            uint floatFraction = halfFraction << (23 - 11);
            uint floatExponent = halfExponent + 112u;
            
            uint floatBits = (floatExponent << 23) | floatFraction;
        
            return UnsafeUtility.As<uint, float>(ref floatBits);
        }
        
        static uint EncodeSimpleU10Float(float x)
        {
            x = Mathf.Clamp(x, Mathf.Pow(2f, -15f), Mathf.Pow(2f, 16f));
        
            uint floatBits = UnsafeUtility.As<float, uint>(ref x);
        
            uint floatFraction = floatBits & ((1u << 23) - 1u);
            uint floatExponent = floatBits >> 23;
        
            uint halfFraction = floatFraction >> (23 - 5); // truncate.
        
            // float bias: -127
            // half bias: -15
            // diff bias: -112.
            uint halfExponent = (floatExponent < 112u) ? 0u : (floatExponent - 112u); 
            halfExponent = Math.Min(31u, halfExponent); // Clamp shouldnt be necessary.
            
            return (halfExponent << 5) | halfFraction;
        }
        
        static float DecodeSimpleU10Float(uint halfBits)
        {
            uint halfExponent = halfBits >> 5;
            uint halfFraction = halfBits & ((1u << 5) - 1u);
        
            uint floatFraction = halfFraction << (23 - 5);
            uint floatExponent = halfExponent + 112u;
            
            uint floatBits = (floatExponent << 23) | floatFraction;
        
            return UnsafeUtility.As<uint, float>(ref floatBits);
        }
        
        static uint EncodeSimpleU11Float(float x)
        {
            x = Mathf.Clamp(x, Mathf.Pow(2f, -15f), Mathf.Pow(2f, 16f));
        
            uint floatBits = UnsafeUtility.As<float, uint>(ref x);
        
            uint floatFraction = floatBits & ((1u << 23) - 1u);
            uint floatExponent = floatBits >> 23;
        
            uint halfFraction = floatFraction >> (23 - 6); // truncate.
        
            // float bias: -127
            // half bias: -15
            // diff bias: -112.
            uint halfExponent = (floatExponent < 112u) ? 0u : (floatExponent - 112u); 
            halfExponent = Math.Min(31u, halfExponent); // Clamp shouldnt be necessary.
            
            return (halfExponent << 6) | halfFraction;
        }
        
        static float DecodeSimpleU11Float(uint halfBits)
        {
            uint halfExponent = halfBits >> 6;
            uint halfFraction = halfBits & ((1u << 6) - 1u);
        
            uint floatFraction = halfFraction << (23 - 6);
            uint floatExponent = halfExponent + 112u;
            
            uint floatBits = (floatExponent << 23) | floatFraction;
        
            return UnsafeUtility.As<uint, float>(ref floatBits);
        }
        
        static uint EncodeSimpleR11G11B10(Vector3 rgb)
        {
            uint r11 = EncodeSimpleU11Float(rgb.x);
            uint g11 = EncodeSimpleU11Float(rgb.y);
            uint b10 = EncodeSimpleU10Float(rgb.z);
        
            return (r11 << 21)
                | (g11 << 10)
                | b10;
        }
        
        internal static Vector3 DecodeSimpleR11G11B10(uint r11g11b10)
        {
            uint r11 = r11g11b10 >> 21;
            uint g11 = (r11g11b10 >> 10) & ((1u << 11) - 1u);
            uint b10 = r11g11b10 & ((1u << 10) - 1u);
        
            return new Vector3(
                DecodeSimpleU11Float(r11),
                DecodeSimpleU11Float(g11),
                DecodeSimpleU10Float(b10)
            );
        }

        internal static Vector3 DecodeLogLuv(uint value)
        {
            Vector3 logLuv;
            logLuv.x = ((value >> 0) & 255) / 255.0f;
            logLuv.y = ((value >> 8) & 255) / 255.0f;
            logLuv.z = ((value >> 16) & 65535) / 65535.0f;
            return RgbFromLogluv(logLuv);
        }

        internal static Vector3 DecodeHalfLuv(uint value)
        {
            Vector3 luv;
            luv.x = ((value >> 0) & 255) / 255.0f;
            luv.y = ((value >> 8) & 255) / 255.0f;
            luv.z = DecodeSimpleUHalfFloat(value >> 16);
            return RgbFromLuv(luv);
        }

        internal void Allocate(RenderPipelineResources resources)
        {
            Cleanup(); // To avoid double alloc.

            _PropagationClearRadianceShader = resources.shaders.probePropagationClearRadianceCS;
            _PropagationInitializeShader = resources.shaders.probePropagationInitializeCS;
            _PropagationHitsShader = resources.shaders.probePropagationHitsCS;
            _PropagationDirtyBlockList = resources.shaders.probePropagationDirtyBlockListCS;
            _PropagationResetDirtyFlags = resources.shaders.probePropagationResetDirtyFlagsCS;
            _PropagationAxesShader = resources.shaders.probePropagationAxesCS;
            _PropagationCombineShader = resources.shaders.probePropagationCombineCS;

            ProbeVolume.EnsureBuffer<NeighborAxisLookup>(ref _sortedNeighborAxisLookupsBuffer, _sortedNeighborAxisLookups.Length);

            _dirtyBlockListBuffer = new ComputeBuffer(32768, sizeof(int), ComputeBufferType.Append);
            _dirtyBlockIndirectBuffer = new ComputeBuffer(3, sizeof(int), ComputeBufferType.IndirectArguments);
            _dirtyBlockIndirectBuffer.SetData(new[] { 0, s_NeighborAxis.Length, 1 });

            _DebugDirtyFlagsShader = resources.shaders.probeVolumeDebugDirtyFlags;

#if UNITY_EDITOR
            _ProbeVolumeDebugNeighbors = resources.shaders.probeVolumeDebugNeighbors;
            dummyColor = RTHandles.Alloc(kDummyRTWidth, kDummyRTHeight, dimension: TextureDimension.Tex2D, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, name: "Dummy color");
#endif
        }


        internal void Cleanup()
        {
#if UNITY_EDITOR
            RTHandles.Release(dummyColor);
#endif
            _propagationSettingsHash = 0;
            ProbeVolume.CleanupBuffer(_sortedNeighborAxisLookupsBuffer);
        }

        private void SetBasisKeywords(ProbeVolumeDynamicGIBasis basis, ProbeVolumeDynamicGIBasisPropagationOverride basisPropagationOverride, ComputeShader shader)
        {
            CoreUtils.SetKeyword(shader, "BASIS_SPHERICAL_GAUSSIAN", false);
            CoreUtils.SetKeyword(shader, "BASIS_SPHERICAL_GAUSSIAN_WINDOWED", false);
            CoreUtils.SetKeyword(shader, "BASIS_AMBIENT_DICE_SHARP", false);
            CoreUtils.SetKeyword(shader, "BASIS_AMBIENT_DICE_SOFTER", false);
            CoreUtils.SetKeyword(shader, "BASIS_AMBIENT_DICE_SUPER_SOFT", false);
            CoreUtils.SetKeyword(shader, "BASIS_AMBIENT_DICE_ULTRA_SOFT", false);

            switch (basis)
            {
                case ProbeVolumeDynamicGIBasis.BasisSphericalGaussian:
                {
                    CoreUtils.SetKeyword(shader, "BASIS_SPHERICAL_GAUSSIAN", true);
                    break;
                }
                case ProbeVolumeDynamicGIBasis.BasisSphericalGaussianWindowed:
                {
                     CoreUtils.SetKeyword(shader, "BASIS_SPHERICAL_GAUSSIAN_WINDOWED", true);
                    break;
                }
                case ProbeVolumeDynamicGIBasis.BasisAmbientDiceSharp:
                {
                    CoreUtils.SetKeyword(shader, "BASIS_AMBIENT_DICE_SHARP", true);
                    break;
                }
                case ProbeVolumeDynamicGIBasis.BasisAmbientDiceSofter:
                {
                    CoreUtils.SetKeyword(shader, "BASIS_AMBIENT_DICE_SOFTER", true);
                    break;
                }
                case ProbeVolumeDynamicGIBasis.BasisAmbientDiceSuperSoft:
                {
                    CoreUtils.SetKeyword(shader, "BASIS_AMBIENT_DICE_SUPER_SOFT", true);
                    break;
                }
                case ProbeVolumeDynamicGIBasis.BasisAmbientDiceUltraSoft:
                {
                    CoreUtils.SetKeyword(shader, "BASIS_AMBIENT_DICE_ULTRA_SOFT", true);
                    break;
                }

                default:
                {
                    Debug.Assert(false);
                    break;
                }
            }

            CoreUtils.SetKeyword(shader, "BASIS_PROPAGATION_OVERRIDE_NONE", false);
            CoreUtils.SetKeyword(shader, "BASIS_PROPAGATION_OVERRIDE_SPHERICAL_GAUSSIAN", false);
            CoreUtils.SetKeyword(shader, "BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_SOFTER", false);
            CoreUtils.SetKeyword(shader, "BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_SUPER_SOFT", false);
            CoreUtils.SetKeyword(shader, "BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_ULTRA_SOFT", false);

            switch (basisPropagationOverride)
            {
                case ProbeVolumeDynamicGIBasisPropagationOverride.None:
                {
                    CoreUtils.SetKeyword(shader, "BASIS_PROPAGATION_OVERRIDE_NONE", true);
                    break;
                }

                case ProbeVolumeDynamicGIBasisPropagationOverride.BasisSphericalGaussian:
                {
                    CoreUtils.SetKeyword(shader, "BASIS_PROPAGATION_OVERRIDE_SPHERICAL_GAUSSIAN", true);
                    break;
                }

                case ProbeVolumeDynamicGIBasisPropagationOverride.BasisAmbientDiceWrappedSofter:
                {
                    CoreUtils.SetKeyword(shader, "BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_SOFTER", true);
                    break;
                }

                case ProbeVolumeDynamicGIBasisPropagationOverride.BasisAmbientDiceWrappedSuperSoft:
                {
                    CoreUtils.SetKeyword(shader, "BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_SUPER_SOFT", true);
                    break;
                }

                case ProbeVolumeDynamicGIBasisPropagationOverride.BasisAmbientDiceWrappedUltraSoft:
                {
                    CoreUtils.SetKeyword(shader, "BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_ULTRA_SOFT", true);
                    break;
                }

                default:
                {
                    Debug.Assert(false);
                    break;
                }
            }
        }

        internal static bool IsRadianceEncodedInUint(ProbeVolumeDynamicGIRadianceEncoding encoding) => encoding != ProbeVolumeDynamicGIRadianceEncoding.RGBFloat;

        static void SetRadianceEncodingKeywords(ComputeShader shader, ProbeVolumeDynamicGIRadianceEncoding encoding)
        {
            switch (encoding)
            {
                case ProbeVolumeDynamicGIRadianceEncoding.RGBFloat:
                {
                    CoreUtils.SetKeyword(shader, "RADIANCE_ENCODING_LOGLUV", false);
                    CoreUtils.SetKeyword(shader, "RADIANCE_ENCODING_HALFLUV", false);
                    CoreUtils.SetKeyword(shader, "RADIANCE_ENCODING_R11G11B10", false);
                    break;
                }

                case ProbeVolumeDynamicGIRadianceEncoding.LogLuv:
                {
                    CoreUtils.SetKeyword(shader, "RADIANCE_ENCODING_LOGLUV", true);
                    CoreUtils.SetKeyword(shader, "RADIANCE_ENCODING_HALFLUV", false);
                    CoreUtils.SetKeyword(shader, "RADIANCE_ENCODING_R11G11B10", false);
                    break;
                }

                case ProbeVolumeDynamicGIRadianceEncoding.HalfLuv:
                {
                    CoreUtils.SetKeyword(shader, "RADIANCE_ENCODING_LOGLUV", false);
                    CoreUtils.SetKeyword(shader, "RADIANCE_ENCODING_HALFLUV", true);
                    CoreUtils.SetKeyword(shader, "RADIANCE_ENCODING_R11G11B10", false);
                    break;
                }

                case ProbeVolumeDynamicGIRadianceEncoding.R11G11B10:
                {
                    CoreUtils.SetKeyword(shader, "RADIANCE_ENCODING_LOGLUV", false);
                    CoreUtils.SetKeyword(shader, "RADIANCE_ENCODING_HALFLUV", false);
                    CoreUtils.SetKeyword(shader, "RADIANCE_ENCODING_R11G11B10", true);
                    break;
                }

                default: Debug.Assert(false); break;
            }
        }

        internal void DispatchProbePropagation(CommandBuffer cmd, ProbeVolumeHandle probeVolume,
            RenderTargetIdentifier probeVolumeAtlasSHRTHandle, in ProbeVolumeDynamicGIDispatchData data)
        {
            ProbeVolume.EnsureVolumeBuffers(probeVolume, data.encodingMode);

            var previousRadianceCacheInvalid = InitializePropagationBuffers(probeVolume, data.radianceEncoding);
            var dirtyAll = false;
            if (previousRadianceCacheInvalid || data.giSettings.clear.value || _clearAllActive)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProbeVolumeDynamicGIInitialize)))
                    DispatchPropagationInitialize(cmd, probeVolume, in data);

                previousRadianceCacheInvalid = false;
                dirtyAll = true;
            }

            if (probeVolume.HitNeighborAxisLength != 0)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProbeVolumeDynamicGIHits)))
                {
                    DispatchPropagationHits(cmd, probeVolume, previousRadianceCacheInvalid, in data);
                }
            }

            if (!data.dirtyFlagsDisabled)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProbeVolumeDynamicGIDirtyBlockList)))
                    DispatchDirtyBlockList(cmd, probeVolume);
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProbeVolumeDynamicGIResetDirtyFlags)))
                    DispatchResetNextDirtyFlags(cmd, probeVolume, dirtyAll);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProbeVolumeDynamicGIAxes)))
                DispatchPropagationAxes(cmd, probeVolume, previousRadianceCacheInvalid, in data);
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProbeVolumeDynamicGICombine)))
                DispatchPropagationCombine(cmd, probeVolume, probeVolumeAtlasSHRTHandle, in data);

            _stats.Simulated(probeVolume);
            ref var propagationPipelineData = ref probeVolume.GetPropagationPipelineData();
            propagationPipelineData.SwapRadianceCaches();
            propagationPipelineData.simulationFrameTick = _probeVolumeSimulationFrameTick;
        }

        internal void ClearProbePropagation(ProbeVolumeHandle probeVolume)
        {
            if (probeVolume.AbleToSimulateDynamicGI())
            {
                if (CleanupPropagation(probeVolume))
                {
                    // Clear the atlas data so original bake data gets set since Dynamic GI was disabled
                    if (RenderPipelineManager.currentPipeline is HDRenderPipeline hdrp)
                        hdrp.ReleaseProbeVolumeFromAtlas(probeVolume);
                }
            }
        }

        void DispatchClearPreviousRadianceCache(CommandBuffer cmd, ProbeVolumeHandle probeVolume, ProbeVolumeDynamicGIRadianceEncoding radianceEncoding)
        {
            var kernel = _PropagationClearRadianceShader.FindKernel("ClearPreviousRadianceCache");
            var shader = _PropagationClearRadianceShader;

            ref var propagationPipelineData = ref probeVolume.GetPropagationPipelineData();

            SetRadianceEncodingKeywords(shader, radianceEncoding);

            cmd.SetComputeBufferParam(shader, kernel, "_RadianceCacheAxis0", propagationPipelineData.radianceCacheAxis0);
            cmd.SetComputeBufferParam(shader, kernel, "_RadianceCacheAxis1", propagationPipelineData.radianceCacheAxis1);
            cmd.SetComputeIntParam(shader, "_RadianceCacheAxisCount", propagationPipelineData.radianceCacheAxis0.count);

            var hitNeighborAxisLength = probeVolume.HitNeighborAxisLength;
            cmd.SetComputeBufferParam(shader, kernel, "_HitRadianceCacheAxis", propagationPipelineData.hitRadianceCache);
            cmd.SetComputeIntParam(shader, "_HitRadianceCacheAxisCount", hitNeighborAxisLength);

            int numHits = Mathf.Max(propagationPipelineData.radianceCacheAxis0.count, hitNeighborAxisLength);
            int dispatchX = (numHits + 63) / 64;
            cmd.DispatchCompute(shader, kernel, dispatchX, 1, 1);
        }

        void DispatchPropagationInitialize(CommandBuffer cmd, ProbeVolumeHandle probeVolume, in ProbeVolumeDynamicGIDispatchData data)
        {
            int numProbes = probeVolume.parameters.resolutionX * probeVolume.parameters.resolutionY * probeVolume.parameters.resolutionZ;
            ProbeVolume.ProbeVolumeAtlasKey key = probeVolume.ComputeProbeVolumeAtlasKey();
            var kernel = _PropagationInitializeShader.FindKernel("InitializePropagationAxis");
            var shader = _PropagationInitializeShader;

            SetRadianceEncodingKeywords(shader, data.radianceEncoding);
            SetBasisKeywords(data.giSettings.basis.value, data.giSettings.basisPropagationOverride.value, shader);

            ref var pipelineData = ref probeVolume.GetPipelineData();
            var obb = pipelineData.BoundingBox;

            var resolution = new Vector3(
                probeVolume.parameters.resolutionX,
                probeVolume.parameters.resolutionY,
                probeVolume.parameters.resolutionZ
            );
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeResolution", PackProbeVolumeResolution(resolution));
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeBlockResolution", PackProbeVolumeBlockResolution(resolution));

            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeResolutionInverse, new Vector3(
                1.0f / (float) probeVolume.parameters.resolutionX,
                1.0f / (float) probeVolume.parameters.resolutionY,
                1.0f / (float) probeVolume.parameters.resolutionZ
            ));

            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCount, data.globalCB._ProbeVolumeAtlasResolutionAndSliceCount);
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCountInverse, data.globalCB._ProbeVolumeAtlasResolutionAndSliceCountInverse);
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasSHRotateRight, key.rotation * new Vector3(1.0f, 0.0f, 0.0f));
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasSHRotateUp, key.rotation * new Vector3(0.0f, 1.0f, 0.0f));
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasSHRotateForward, key.rotation * new Vector3(0.0f, 0.0f, 1.0f));

            cmd.SetComputeIntParam(shader, HDShaderIDs._ProbeVolumeAtlasReadBufferCount, numProbes);

            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasScale, pipelineData.Scale);
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasBias, pipelineData.Bias);
            cmd.SetComputeBufferParam(shader, kernel, HDShaderIDs._ProbeVolumeAtlasReadSHL01Buffer, pipelineData.SHL01Buffer);
            cmd.SetComputeBufferParam(shader, kernel, HDShaderIDs._ProbeVolumeAtlasReadSHL2Buffer, pipelineData.SHL2Buffer);

            cmd.SetComputeBufferParam(shader, kernel, HDShaderIDs._ProbeVolumeAtlasReadValidityBuffer, pipelineData.ValidityBuffer);

            ref var propagationPipelineData = ref probeVolume.GetPropagationPipelineData();
            cmd.SetComputeBufferParam(shader, kernel, "_RadianceCacheAxis", propagationPipelineData.GetReadRadianceCacheAxis());
            cmd.SetComputeIntParam(shader, "_RadianceCacheAxisCount", propagationPipelineData.radianceCacheAxis0.count);

            cmd.SetComputeVectorArrayParam(shader, "_RayAxis", s_NeighborAxis);

            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsRight", obb.right);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsUp", obb.up);

            cmd.SetComputeFloatParam(shader, "_PropagationSharpness", data.giSettings.propagationSharpness.value);
            cmd.SetComputeFloatParam(shader, "_Sharpness", data.giSettings.sharpness.value);

            int dispatchX = (probeVolume.parameters.resolutionX + 3) / 4;
            int dispatchY = (probeVolume.parameters.resolutionY + 3) / 4;
            int dispatchZ = (probeVolume.parameters.resolutionZ + 3) / 4;
            cmd.DispatchCompute(shader, kernel, dispatchX, dispatchY, dispatchZ);
        }

        void DispatchPropagationHits(CommandBuffer cmd, ProbeVolumeHandle probeVolume,
            bool previousRadianceCacheInvalid, in ProbeVolumeDynamicGIDispatchData data)
        {
            var kernel = _PropagationHitsShader.FindKernel("AccumulateLightingDirectional");
            var shader = _PropagationHitsShader;
            
            ref var pipelineData = ref probeVolume.GetPipelineData();
            ref var propagationPipelineData = ref probeVolume.GetPropagationPipelineData();

            SetRadianceEncodingKeywords(shader, data.radianceEncoding);
            SetBasisKeywords(data.giSettings.basis.value, data.giSettings.basisPropagationOverride.value, shader);
            CoreUtils.SetKeyword(shader, "DIRTY_FLAGS_DISABLED", data.dirtyFlagsDisabled);

            var obb = pipelineData.BoundingBox;
            var engineData = pipelineData.EngineData;
            cmd.SetComputeFloatParam(shader, "_ProbeVolumeDGIMaxNeighborDistance", engineData.maxNeighborDistance);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeResolution", PackProbeVolumeResolution(engineData.resolution));
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeBlockResolution", PackProbeVolumeBlockResolution(engineData.resolution));
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIResolutionInverse", engineData.resolutionInverse);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsRight", obb.right);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsUp", obb.up);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsExtents", new Vector3(obb.extentX, obb.extentY, obb.extentZ));
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsCenter", obb.center);

            cmd.SetComputeBufferParam(shader, kernel, "_ProbeVolumeNeighborHits", propagationPipelineData.neighborHits);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeNeighborHitCount", propagationPipelineData.neighborHits.count);
            cmd.SetComputeFloatParam(shader, "_MaxAlbedo", data.giSettings.maxAlbedo.value);
            cmd.SetComputeFloatParam(shader, "_RayBias", data.giSettings.bias.value);
            cmd.SetComputeFloatParam(shader, "_LeakMitigation", data.giSettings.leakMitigation.value);
            cmd.SetComputeFloatParam(shader, "_Sharpness", data.giSettings.sharpness.value);
            cmd.SetComputeVectorArrayParam(shader, "_RayAxis", s_NeighborAxis);

            var mixedLightMode = data.mixedLightMode;
            float infBounce;

#if UNITY_EDITOR
            if (ProbeVolume.preparingMixedLights)
            {
                // We bake raw unscaled lighting values so we could adjust mixed lights contribution
                // with Indirect Scale at runtime in the same way as runtime lights.
                cmd.SetComputeFloatParam(shader, "_IndirectScale", 1f);
                cmd.SetComputeFloatParam(shader, "_BakedEmissionMultiplier", 0f);
                cmd.SetComputeFloatParam(shader, "_MixedLightingMultiplier", 0f);
                cmd.SetComputeIntParam(shader, "_MixedLightsAsRealtimeEnabled", 1);
                infBounce = 0f;

                cmd.SetComputeFloatParam(shader, "_RangeBehindCamera", float.MaxValue);
                cmd.SetComputeFloatParam(shader, "_RangeInFrontOfCamera", float.MaxValue);
            }
            else
#endif
            {
#if UNITY_EDITOR
                if (ProbeVolume.preparingForBake)
                {
                    mixedLightMode = ProbeVolumeDynamicGIMixedLightMode.ForceRealtime;
                    cmd.SetComputeFloatParam(shader, "_RangeBehindCamera", float.MaxValue);
                    cmd.SetComputeFloatParam(shader, "_RangeInFrontOfCamera", float.MaxValue);
                }
                else
#endif
                {
                    cmd.SetComputeFloatParam(shader, "_RangeBehindCamera", data.giSettings.rangeBehindCamera.value);
                    cmd.SetComputeFloatParam(shader, "_RangeInFrontOfCamera", data.giSettings.rangeInFrontOfCamera.value);
                }

                cmd.SetComputeFloatParam(shader, "_IndirectScale", mixedLightMode != ProbeVolumeDynamicGIMixedLightMode.MixedOnly ? data.giSettings.indirectMultiplier.value : 0f);
                cmd.SetComputeFloatParam(shader, "_BakedEmissionMultiplier", data.giSettings.bakedEmissionMultiplier.value);

                var forceRealtime = mixedLightMode == ProbeVolumeDynamicGIMixedLightMode.ForceRealtime;
                cmd.SetComputeFloatParam(shader, "_MixedLightingMultiplier", !forceRealtime ? data.giSettings.indirectMultiplier.value : 0f);
                cmd.SetComputeIntParam(shader, "_MixedLightsAsRealtimeEnabled", forceRealtime || !probeVolume.DynamicGIMixedLightsBaked() ? 1 : 0);

                infBounce = data.infiniteBounces ? data.giSettings.infiniteBounce.value : 0f;
            }

            cmd.SetComputeBufferParam(shader, kernel, "_ProbeVolumeDirtyFlags", propagationPipelineData.GetDirtyFlags());
            cmd.SetComputeBufferParam(shader, kernel, "_PreviousRadianceCacheAxis", propagationPipelineData.GetReadRadianceCacheAxis());
            cmd.SetComputeIntParam(shader, "_RadianceCacheAxisCount", propagationPipelineData.radianceCacheAxis0.count);
            cmd.SetComputeBufferParam(shader, kernel, "_HitRadianceCacheAxis", propagationPipelineData.hitRadianceCache);
            cmd.SetComputeIntParam(shader, "_HitRadianceCacheAxisCount", probeVolume.HitNeighborAxisLength);

            // TODO: replace with real one
            cmd.SetComputeTextureParam(shader, kernel, "_HierarchicalVarianceScreenSpaceShadowsTexture", TextureXR.GetWhiteTexture());

            cmd.SetComputeFloatParam(shader, "_InfiniteBounce", infBounce);
            CoreUtils.SetKeyword(shader, "COMPUTE_INFINITE_BOUNCE", infBounce > 0);
            CoreUtils.SetKeyword(shader, "PREVIOUS_RADIANCE_CACHE_INVALID", previousRadianceCacheInvalid);

            int numHits = propagationPipelineData.neighborHits.count;
            int dispatchX = (numHits + 63) / 64;
            cmd.DispatchCompute(shader, kernel, dispatchX, 1, 1);
        }

        void DispatchDirtyBlockList(CommandBuffer cmd, ProbeVolumeHandle probeVolume)
        {
            var kernel = _PropagationDirtyBlockList.FindKernel("DirtyBlockList");
            var shader = _PropagationDirtyBlockList;

            var data = probeVolume.GetPipelineData().EngineData;
            var propagationPipelineData = probeVolume.GetPropagationPipelineData();
            int blockResolutionX = ((int)data.resolution.x + 3) / 4;
            int blockResolutionY = ((int)data.resolution.y + 3) / 4;
            int blockResolutionZ = ((int)data.resolution.z + 3) / 4;
            int blockCount = blockResolutionX * blockResolutionY * blockResolutionZ;

            cmd.SetComputeBufferCounterValue(_dirtyBlockListBuffer, 0);
            cmd.SetComputeBufferParam(shader, kernel, "_ProbeVolumeDirtyBlocks", _dirtyBlockListBuffer);
            cmd.SetComputeBufferParam(shader, kernel, "_ProbeVolumeDirtyFlags", propagationPipelineData.GetDirtyFlags());
            cmd.SetComputeIntParam(shader, "_ProbeVolumeBlockCount", blockCount);

            int dispatch = (blockCount + 63) / 64;
            cmd.DispatchCompute(shader, kernel, dispatch, 1, 1);

            cmd.CopyCounterValue(_dirtyBlockListBuffer, _dirtyBlockIndirectBuffer, 0);
        }

        void DispatchResetNextDirtyFlags(CommandBuffer cmd, ProbeVolumeHandle probeVolume, bool dirtyAll)
        {
            var kernel = _PropagationResetDirtyFlags.FindKernel("ResetDirtyFlags");
            var shader = _PropagationResetDirtyFlags;

            var data = probeVolume.GetPipelineData().EngineData;
            var propagationPipelineData = probeVolume.GetPropagationPipelineData();

            cmd.SetComputeBufferParam(shader, kernel, "_ProbeVolumeDirtyFlags", propagationPipelineData.GetNextDirtyFlags());
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeResolution", PackProbeVolumeResolution(data.resolution));
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeBlockResolution", PackProbeVolumeBlockResolution(data.resolution));
            cmd.SetComputeIntParam(shader, "_DirtyAll", dirtyAll ? 1 : 0);

            int dispatchX = ((int)data.resolution.x + 3) / 4;
            int dispatchY = ((int)data.resolution.y + 3) / 4;
            int dispatchZ = ((int)data.resolution.z + 3) / 4;
            cmd.DispatchCompute(shader, kernel, dispatchX, dispatchY, dispatchZ);
        }

        void DispatchPropagationAxes(CommandBuffer cmd, ProbeVolumeHandle probeVolume,
            bool previousRadianceCacheInvalid, in ProbeVolumeDynamicGIDispatchData data)
        {
            var kernel = _PropagationAxesShader.FindKernel("PropagateLight");
            var shader = _PropagationAxesShader;

            ref var pipelineData = ref probeVolume.GetPipelineData();
            ref var propagationPipelineData = ref probeVolume.GetPropagationPipelineData();

            SetRadianceEncodingKeywords(shader, data.radianceEncoding);
            SetBasisKeywords(data.giSettings.basis.value, data.giSettings.basisPropagationOverride.value, shader);
            CoreUtils.SetKeyword(shader, "DIRTY_FLAGS_DISABLED", data.dirtyFlagsDisabled);

            var obb = pipelineData.BoundingBox;
            var engineData = pipelineData.EngineData;
            var propagationQuality = (PropagationQuality)data.propagationQuality;

            switch (data.giSettings.neighborVolumePropagationMode.value)
            {
                case ProbeDynamicGI.DynamicGINeighboringVolumePropagationMode.SampleNeighborsDirectionOnly:
                {
                    CoreUtils.SetKeyword(shader, "SAMPLE_NEIGHBORS_DIRECTION_ONLY", true);
                    CoreUtils.SetKeyword(shader, "SAMPLE_NEIGHBORS_POSITION_AND_DIRECTION", false);
                    break;
                }
                case ProbeDynamicGI.DynamicGINeighboringVolumePropagationMode.SampleNeighborsPositionAndDirection:
                {
                    CoreUtils.SetKeyword(shader, "SAMPLE_NEIGHBORS_DIRECTION_ONLY", false);
                    CoreUtils.SetKeyword(shader, "SAMPLE_NEIGHBORS_POSITION_AND_DIRECTION", true);
                    break;
                }
                default:
                {
                    CoreUtils.SetKeyword(shader, "SAMPLE_NEIGHBORS_DIRECTION_ONLY", false);
                    CoreUtils.SetKeyword(shader, "SAMPLE_NEIGHBORS_POSITION_AND_DIRECTION", false);
                    break;
                }
            }

#if UNITY_EDITOR
            if (ProbeVolume.preparingForBake)
            {
                propagationQuality = PropagationQuality.High;
                cmd.SetComputeFloatParam(shader, "_RangeBehindCamera", float.MaxValue);
                cmd.SetComputeFloatParam(shader, "_RangeInFrontOfCamera", float.MaxValue);
            }
            else
#endif
            {
                cmd.SetComputeFloatParam(shader, "_RangeBehindCamera", data.giSettings.rangeBehindCamera.value);
                cmd.SetComputeFloatParam(shader, "_RangeInFrontOfCamera", data.giSettings.rangeInFrontOfCamera.value);
            }

            int propagationAxisAmount;
            switch (propagationQuality)
            {
                case PropagationQuality.Low:
                {
                    CoreUtils.SetKeyword(shader, "PROPAGATION_AXIS_MOST", false);
                    CoreUtils.SetKeyword(shader, "PROPAGATION_AXIS_LEAST", true);
                    propagationAxisAmount = 10;
                    break;
                }
                case PropagationQuality.Medium:
                {
                    CoreUtils.SetKeyword(shader, "PROPAGATION_AXIS_MOST", true);
                    CoreUtils.SetKeyword(shader, "PROPAGATION_AXIS_LEAST", false);
                    propagationAxisAmount = 17;
                    break;
                }
                default:
                {
                    CoreUtils.SetKeyword(shader, "PROPAGATION_AXIS_MOST", false);
                    CoreUtils.SetKeyword(shader, "PROPAGATION_AXIS_LEAST", false);
                    propagationAxisAmount = 26;
                    break;
                }
            }

            cmd.SetComputeFloatParam(shader, "_ProbeVolumeDGIMaxNeighborDistance", engineData.maxNeighborDistance);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeResolution", PackProbeVolumeResolution(engineData.resolution));
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeBlockResolution", PackProbeVolumeBlockResolution(engineData.resolution));
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGILightLayers", unchecked((int)engineData.lightLayers));
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGIEngineDataIndex", pipelineData.EngineDataIndex);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIResolutionInverse", engineData.resolutionInverse);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsRight", obb.right);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsUp", obb.up);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsExtents", new Vector3(obb.extentX, obb.extentY, obb.extentZ));
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsCenter", obb.center);

            cmd.SetComputeBufferParam(shader, kernel, "_ProbeVolumeNeighbors", propagationPipelineData.neighbors);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeNeighborsCount", propagationPipelineData.neighbors.count);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeProbeCount", propagationPipelineData.neighbors.count / s_NeighborAxis.Length);
            cmd.SetComputeFloatParam(shader, "_LeakMitigation", data.giSettings.leakMitigation.value);
            cmd.SetComputeFloatParam(shader, "_PropagationContribution", data.giSettings.propagationContribution.value);
            cmd.SetComputeFloatParam(shader, "_Sharpness", data.giSettings.sharpness.value);
            cmd.SetComputeVectorArrayParam(shader, "_RayAxis", s_NeighborAxis);

            cmd.SetComputeBufferParam(shader, kernel, "_ProbeVolumeDirtyFlags", propagationPipelineData.GetDirtyFlags());
            cmd.SetComputeBufferParam(shader, kernel, "_ProbeVolumeNextDirtyFlags", propagationPipelineData.GetNextDirtyFlags());

            cmd.SetComputeBufferParam(shader, kernel, "_HitRadianceCacheAxis", propagationPipelineData.hitRadianceCache);
            cmd.SetComputeIntParam(shader, "_HitRadianceCacheAxisCount", probeVolume.HitNeighborAxisLength);

            UpdateAmbientProbe(data.ambientProbe, data.giSettings.skyMultiplier.value);
            cmd.SetComputeVectorArrayParam(shader, "_AmbientProbe", s_AmbientProbe);

            cmd.SetComputeBufferParam(shader, kernel, "_PreviousRadianceCacheAxis", propagationPipelineData.GetReadRadianceCacheAxis());
            cmd.SetComputeBufferParam(shader, kernel, "_RadianceCacheAxis", propagationPipelineData.GetWriteRadianceCacheAxis());

            PrecomputeAxisCacheLookup(cmd, propagationAxisAmount, data.giSettings.basis.value, data.giSettings.sharpness.value,
                data.giSettings.basisPropagationOverride.value, data.giSettings.propagationSharpness.value);
            cmd.SetComputeBufferParam(shader, kernel, "_SortedNeighborAxisLookups", _sortedNeighborAxisLookupsBuffer);
            CoreUtils.SetKeyword(shader, "PREVIOUS_RADIANCE_CACHE_INVALID", previousRadianceCacheInvalid);

            cmd.SetComputeFloatParam(shader, "_PropagationSharpness", data.giSettings.propagationSharpness.value);
            cmd.SetComputeFloatParam(shader, "_Sharpness", data.giSettings.sharpness.value);

            if (data.dirtyFlagsDisabled)
            {
                int dispatchX = ((int)engineData.resolution.x + 3) / 4;
                int dispatchY = ((int)engineData.resolution.y + 3) / 4;
                int dispatchZ = ((int)engineData.resolution.z + 3) / 4;
                cmd.DispatchCompute(shader, kernel, dispatchX * dispatchY * dispatchZ, s_NeighborAxis.Length, 1);
            }
            else
            {
                cmd.SetComputeBufferParam(shader, kernel, "_ProbeVolumeDirtyBlocks", _dirtyBlockListBuffer);
                cmd.DispatchCompute(shader, kernel, _dirtyBlockIndirectBuffer, 0);
            }
        }

        static void UpdateAmbientProbe(SphericalHarmonicsL2 ambientProbe, float multiplier)
        {
            // Pack sky probe in the way Probe Volume stores final SH to combine them easily.
            var c0 = new Vector4(ambientProbe[0, 0], ambientProbe[1, 0], ambientProbe[2, 0], ambientProbe[0, 3]) * multiplier;
            var c1 = new Vector4(ambientProbe[0, 1], ambientProbe[0, 2], ambientProbe[1, 3], ambientProbe[1, 1]) * multiplier;
            var c2 = new Vector4(ambientProbe[1, 2], ambientProbe[2, 3], ambientProbe[2, 1], ambientProbe[2, 2]) * multiplier;
            var c3 = new Vector4(ambientProbe[0, 4], ambientProbe[0, 5], ambientProbe[0, 6], ambientProbe[0, 7]) * multiplier;
            var c4 = new Vector4(ambientProbe[1, 4], ambientProbe[1, 5], ambientProbe[1, 6], ambientProbe[1, 7]) * multiplier;
            var c5 = new Vector4(ambientProbe[2, 4], ambientProbe[2, 5], ambientProbe[2, 6], ambientProbe[2, 7]) * multiplier;
            var c6 = new Vector4(ambientProbe[0, 8], ambientProbe[1, 8], ambientProbe[2, 8], 0f) * multiplier;

            s_AmbientProbe[0] = c0;
            s_AmbientProbe[1] = c1;
            s_AmbientProbe[2] = c2;
            s_AmbientProbe[3] = c3;
            s_AmbientProbe[4] = c4;
            s_AmbientProbe[5] = c5;
            s_AmbientProbe[6] = c6;
        }

        void DispatchPropagationCombine(CommandBuffer cmd, ProbeVolumeHandle probeVolume,
            RenderTargetIdentifier probeVolumeAtlasSHRTHandle, in ProbeVolumeDynamicGIDispatchData data)
        {
            int numProbes = probeVolume.parameters.resolutionX * probeVolume.parameters.resolutionY * probeVolume.parameters.resolutionZ;
            ProbeVolume.ProbeVolumeAtlasKey key = probeVolume.ComputeProbeVolumeAtlasKey();
            var kernel = _PropagationCombineShader.FindKernel("CombinePropagationAxis");
            var shader = _PropagationCombineShader;

            SetRadianceEncodingKeywords(shader, data.radianceEncoding);
            SetBasisKeywords(data.giSettings.basis.value, data.giSettings.basisPropagationOverride.value, shader);
            CoreUtils.SetKeyword(shader, "DIRTY_FLAGS_DISABLED", data.dirtyFlagsDisabled);

            ref var pipelineData = ref probeVolume.GetPipelineData();
            var obb = pipelineData.BoundingBox;

            var resolution = new Vector3(
                probeVolume.parameters.resolutionX,
                probeVolume.parameters.resolutionY,
                probeVolume.parameters.resolutionZ
            );
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeResolution", PackProbeVolumeResolution(resolution));
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeBlockResolution", PackProbeVolumeBlockResolution(resolution));
            
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeResolutionInverse, new Vector3(
                1.0f / (float) probeVolume.parameters.resolutionX,
                1.0f / (float) probeVolume.parameters.resolutionY,
                1.0f / (float) probeVolume.parameters.resolutionZ
            ));

            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCount, data.globalCB._ProbeVolumeAtlasResolutionAndSliceCount);
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCountInverse, data.globalCB._ProbeVolumeAtlasResolutionAndSliceCountInverse);
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasSHRotateRight, key.rotation * new Vector3(1.0f, 0.0f, 0.0f));
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasSHRotateUp, key.rotation * new Vector3(0.0f, 1.0f, 0.0f));
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasSHRotateForward, key.rotation * new Vector3(0.0f, 0.0f, 1.0f));

            cmd.SetComputeIntParam(shader, HDShaderIDs._ProbeVolumeAtlasReadBufferCount, numProbes);

            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasScale, pipelineData.Scale);
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasBias, pipelineData.Bias);
            cmd.SetComputeBufferParam(shader, kernel, HDShaderIDs._ProbeVolumeAtlasReadSHL01Buffer, pipelineData.SHL01Buffer);
            cmd.SetComputeBufferParam(shader, kernel, HDShaderIDs._ProbeVolumeAtlasReadSHL2Buffer, pipelineData.SHL2Buffer);

            cmd.SetComputeBufferParam(shader, kernel, HDShaderIDs._ProbeVolumeAtlasReadValidityBuffer, pipelineData.ValidityBuffer);
            cmd.SetComputeTextureParam(shader, kernel, HDShaderIDs._ProbeVolumeAtlasWriteTextureSH, probeVolumeAtlasSHRTHandle);

            ref var propagationPipelineData = ref probeVolume.GetPropagationPipelineData();
            cmd.SetComputeBufferParam(shader, kernel, "_RadianceCacheAxis", propagationPipelineData.GetWriteRadianceCacheAxis());
            cmd.SetComputeIntParam(shader, "_RadianceCacheAxisCount", propagationPipelineData.radianceCacheAxis0.count);
            cmd.SetComputeBufferParam(shader, kernel, "_ProbeVolumeDirtyFlags", propagationPipelineData.GetDirtyFlags());

            var dynamicAmount = data.giSettings.dynamicAmount.value;
#if UNITY_EDITOR
            // When preparing mixed lights we set Indirect Scale to 1.0 for hit pass to get raw values in there.
            // So here we multiply output by correct Indirect Scale from settings to preview how it would look
            // during final propagation when Indirect Scale is applied to mixed lights as well as realtime lights.
            if (ProbeVolume.preparingMixedLights)
                dynamicAmount *= data.giSettings.indirectMultiplier.value;
#endif
            cmd.SetComputeFloatParam(shader, "_DynamicPropagationContribution", dynamicAmount);

            cmd.SetComputeFloatParam(shader, "_BakedLightingContribution", data.giSettings.fallbackAmount.value);
            cmd.SetComputeVectorArrayParam(shader, "_RayAxis", s_NeighborAxis);

            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsRight", obb.right);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsUp", obb.up);

            cmd.SetComputeFloatParam(shader, "_PropagationSharpness", data.giSettings.propagationSharpness.value);
            cmd.SetComputeFloatParam(shader, "_Sharpness", data.giSettings.sharpness.value);

            int dispatchX = (probeVolume.parameters.resolutionX + 3) / 4;
            int dispatchY = (probeVolume.parameters.resolutionY + 3) / 4;
            int dispatchZ = (probeVolume.parameters.resolutionZ + 3) / 4;
            cmd.DispatchCompute(shader, kernel, dispatchX, dispatchY, dispatchZ);
        }

        internal void DispatchPropagationOutputDynamicSH(
            CommandBuffer cmd,
            Vector3Int size,
            ProbeVolumePipelineData pipelineData,
            ProbeVolumePropagationPipelineData propagationPipelineData,
            RTHandle probeVolumeAtlasSHRTHandle)
        {
            int numProbes = size.x * size.y * size.z;
            var kernel = _PropagationCombineShader.FindKernel("CombinePropagationAxis");
            var shader = _PropagationCombineShader;

            var obb = pipelineData.BoundingBox;

            CoreUtils.SetKeyword(cmd, "PROBE_VOLUMES_ENCODING_SPHERICAL_HARMONICS_L1", false);
            CoreUtils.SetKeyword(cmd, "PROBE_VOLUMES_ENCODING_SPHERICAL_HARMONICS_L2", true);
            CoreUtils.SetKeyword(shader, "DIRTY_FLAGS_DISABLED", true);

            cmd.SetComputeVectorParam(shader, "_ProbeVolumeResolution", PackProbeVolumeResolution(size));
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeBlockResolution", PackProbeVolumeBlockResolution(size));
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeResolutionInverse, new Vector3(
                1.0f / size.x,
                1.0f / size.y,
                1.0f / size.z
            ));

            var sliceCount = HDRenderPipeline.GetDepthSliceCountFromEncodingMode(ProbeVolumesEncodingModes.SphericalHarmonicsL2);
            var resolutionAndSliceCount = new Vector4(
                size.x,
                size.y,
                size.z,
                sliceCount
            );
            var resolutionAndSliceCountInverse = new Vector4(
                1.0f / size.x,
                1.0f / size.y,
                1.0f / size.z,
                1.0f / sliceCount
            );
            
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCount, resolutionAndSliceCount);
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCountInverse, resolutionAndSliceCountInverse);
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasSHRotateRight, Vector3.right);
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasSHRotateUp, Vector3.up);
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasSHRotateForward, Vector3.forward);

            cmd.SetComputeIntParam(shader, HDShaderIDs._ProbeVolumeAtlasReadBufferCount, numProbes);

            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasScale, Vector3.one);
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasBias, Vector3.zero);
            cmd.SetComputeBufferParam(shader, kernel, HDShaderIDs._ProbeVolumeAtlasReadSHL01Buffer, pipelineData.SHL01Buffer);
            cmd.SetComputeBufferParam(shader, kernel, HDShaderIDs._ProbeVolumeAtlasReadSHL2Buffer, pipelineData.SHL2Buffer);

            cmd.SetComputeBufferParam(shader, kernel, HDShaderIDs._ProbeVolumeAtlasReadValidityBuffer, pipelineData.ValidityBuffer);
            cmd.SetComputeTextureParam(shader, kernel, HDShaderIDs._ProbeVolumeAtlasWriteTextureSH, probeVolumeAtlasSHRTHandle);

            cmd.SetComputeBufferParam(shader, kernel, "_RadianceCacheAxis", propagationPipelineData.GetWriteRadianceCacheAxis());
            cmd.SetComputeIntParam(shader, "_RadianceCacheAxisCount", propagationPipelineData.radianceCacheAxis0.count);

            cmd.SetComputeFloatParam(shader, "_DynamicPropagationContribution", 1f);
            cmd.SetComputeFloatParam(shader, "_BakedLightingContribution", 0f);
            cmd.SetComputeVectorArrayParam(shader, "_RayAxis", s_NeighborAxis);

            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsRight", obb.right);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsUp", obb.up);

            cmd.SetComputeFloatParam(shader, "_PropagationSharpness", 2f);
            cmd.SetComputeFloatParam(shader, "_Sharpness", 6f);

            int dispatchX = (size.x + 3) / 4;
            int dispatchY = (size.y + 3) / 4;
            int dispatchZ = (size.z + 3) / 4;
            cmd.DispatchCompute(shader, kernel, dispatchX, dispatchY, dispatchZ);
        }

        Material GetDebugDirtyProbeMaterial()
        {
            if (_DebugDirtyFlagsMaterial == null && _DebugDirtyFlagsShader != null)
            {
                _DebugDirtyFlagsMaterial = new Material(_DebugDirtyFlagsShader);
            }

            return _DebugDirtyFlagsMaterial;
        }

        MaterialPropertyBlock GetDebugDirtyProbeMaterialPropertyBlock()
        {
            if (_DebugDirtyFlagsMaterialPropertyBlock == null)
            {
                _DebugDirtyFlagsMaterialPropertyBlock = new MaterialPropertyBlock();
            }

            return _DebugDirtyFlagsMaterialPropertyBlock;
        }

        internal void DebugDrawDirtyFlags(CommandBuffer cmd, ProbeVolumeHandle probeVolume)
        {
            var dirtyFlags = probeVolume.GetPropagationPipelineData().GetDirtyFlags();
            if (dirtyFlags == null || !dirtyFlags.IsValid())
                return;

            var material = GetDebugDirtyProbeMaterial();
            if (material == null)
                return;
                
            var materialPropertyBlock = GetDebugDirtyProbeMaterialPropertyBlock();

            var parameters = probeVolume.parameters;

            var probeIndex3DToPositionWSMatrix = VolumeUtils.ComputeProbeIndex3DToPositionWSMatrix(
                probeVolume.position,
                probeVolume.rotation,
                parameters.size,
                parameters.resolutionX,
                parameters.resolutionY,
                parameters.resolutionZ);

            var resolution = new Vector3(parameters.resolutionX, parameters.resolutionY, parameters.resolutionZ);
            var probeRadiusWS = (parameters.size.x / resolution.x + parameters.size.y / resolution.y + parameters.size.z / resolution.z) * (1f / 3f * 1f / 32f);

            materialPropertyBlock.SetVector("_ProbeVolumeResolution", PackProbeVolumeResolution(resolution));
            materialPropertyBlock.SetVector("_ProbeVolumeBlockResolution", PackProbeVolumeBlockResolution(resolution));
            materialPropertyBlock.SetMatrix("_ProbeIndex3DToPositionWSMatrix", probeIndex3DToPositionWSMatrix);
            materialPropertyBlock.SetFloat("_ProbeVolumeProbeDisplayRadiusWS", probeRadiusWS);
            materialPropertyBlock.SetBuffer("_ProbeVolumeDirtyFlags", dirtyFlags);

            var probeCount = VolumeUtils.ComputeProbeCount(parameters.resolutionX, parameters.resolutionY, parameters.resolutionZ);

            cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3 * 2 * probeCount, 1, materialPropertyBlock);
        }

        internal static bool CleanupPropagation(ProbeVolumeHandle probeVolume)
        {
            ref var propagationPipelineData = ref probeVolume.GetPropagationPipelineData();

            bool didDispose = ProbeVolume.CleanupBuffer(propagationPipelineData.neighborHits);
            didDispose |= ProbeVolume.CleanupBuffer(propagationPipelineData.neighbors);
            didDispose |= ProbeVolume.CleanupBuffer(propagationPipelineData.radianceCacheAxis0);
            didDispose |= ProbeVolume.CleanupBuffer(propagationPipelineData.radianceCacheAxis1);
            didDispose |= ProbeVolume.CleanupBuffer(propagationPipelineData.hitRadianceCache);
            didDispose |= ProbeVolume.CleanupBuffer(propagationPipelineData.dirtyFlags0);
            didDispose |= ProbeVolume.CleanupBuffer(propagationPipelineData.dirtyFlags1);

            propagationPipelineData.buffersDataVersion = -1;
            propagationPipelineData.simulationFrameTick = -1;

            return didDispose;
        }

        static bool InitializePropagationInputBuffers(ProbeVolumeHandle probeVolume)
        {
            var dataVersion = probeVolume.GetDataVersion();
            ref var propagationPipelineData = ref probeVolume.GetPropagationPipelineData();

            var hitNeighborAxisLength = probeVolume.HitNeighborAxisLength;
            var hasHitNeighborAxes = hitNeighborAxisLength != 0;
            var hitNeighborAxisLengthOrOne = hasHitNeighborAxes ? hitNeighborAxisLength : 1;
            
            if (propagationPipelineData.buffersDataVersion != dataVersion)
            {
                ProbeVolume.EnsureBuffer<PackedNeighborHit>(ref propagationPipelineData.neighborHits, hitNeighborAxisLengthOrOne);
                if (hasHitNeighborAxes)
                    probeVolume.SetHitNeighborAxis(propagationPipelineData.neighborHits);

                ProbeVolume.EnsureBuffer<NeighborAxis>(ref propagationPipelineData.neighbors, probeVolume.NeighborAxisLength);
                probeVolume.SetNeighborAxis(propagationPipelineData.neighbors);

                var groupCountX = (probeVolume.parameters.resolutionX + 3) / 4;
                var groupCountY = (probeVolume.parameters.resolutionY + 3) / 4;
                var groupCountZ = (probeVolume.parameters.resolutionZ + 3) / 4;
                var groupCount = groupCountX * groupCountY * groupCountZ;

                var packedDirtyFlagsLength = groupCount << 1;
                ProbeVolume.EnsureBuffer<int>(ref propagationPipelineData.dirtyFlags0, packedDirtyFlagsLength);
                ProbeVolume.EnsureBuffer<int>(ref propagationPipelineData.dirtyFlags1, packedDirtyFlagsLength);
                
                propagationPipelineData.buffersDataVersion = dataVersion;
                return true;
            }
            else
            {
                return false;
            }
        }

        static bool InitializePropagationBuffers(ProbeVolumeHandle probeVolume, ProbeVolumeDynamicGIRadianceEncoding radianceEncoding)
        {
            var changed = InitializePropagationInputBuffers(probeVolume);

            ref var propagationPipelineData = ref probeVolume.GetPropagationPipelineData();
            var hitNeighborAxisLength = probeVolume.HitNeighborAxisLength;
            var hasHitNeighborAxes = hitNeighborAxisLength != 0;
            var hitNeighborAxisLengthOrOne = hasHitNeighborAxes ? hitNeighborAxisLength : 1;

            var groupCountX = (probeVolume.parameters.resolutionX + 3) / 4;
            var groupCountY = (probeVolume.parameters.resolutionY + 3) / 4;
            var groupCountZ = (probeVolume.parameters.resolutionZ + 3) / 4;
            var groupCount = groupCountX * groupCountY * groupCountZ;
            int numAxis = groupCount * 64 * s_NeighborAxis.Length;
            
            if (propagationPipelineData.radianceEncoding != radianceEncoding)
            {
                ProbeVolume.CleanupBuffer(propagationPipelineData.hitRadianceCache);
                ProbeVolume.CleanupBuffer(propagationPipelineData.radianceCacheAxis0);
                ProbeVolume.CleanupBuffer(propagationPipelineData.radianceCacheAxis1);
                propagationPipelineData.radianceEncoding = radianceEncoding;
            }

            changed |= IsRadianceEncodedInUint(propagationPipelineData.radianceEncoding)
                ? EnsurePropagationBuffers<uint>(ref propagationPipelineData, hitNeighborAxisLengthOrOne, numAxis)
                : EnsurePropagationBuffers<Vector3>(ref propagationPipelineData, hitNeighborAxisLengthOrOne, numAxis);

            return changed;
        }

        static bool EnsurePropagationBuffers<T>(ref ProbeVolumePropagationPipelineData propagationPipelineData,
            int hitNeighborAxisLengthOrOne, int numAxis)
        {
            var changed = ProbeVolume.EnsureBuffer<T>(ref propagationPipelineData.hitRadianceCache, hitNeighborAxisLengthOrOne);
            if (ProbeVolume.EnsureBuffer<T>(ref propagationPipelineData.radianceCacheAxis0, numAxis))
            {
                ProbeVolume.EnsureBuffer<T>(ref propagationPipelineData.radianceCacheAxis1, numAxis);
                propagationPipelineData.radianceReadIndex = 0;
                changed = true;
            }
            return changed;
        }

        internal static float GetMaxNeighborDistance(in ProbeVolumeArtistParameters parameters)
        {
            float minDensity = Mathf.Min(parameters.densityX, parameters.densityY);
            minDensity = Mathf.Min(minDensity, parameters.densityZ);
            return 1.0f / minDensity;
        }

        void PrecomputeAxisCacheLookup(CommandBuffer cmd, int axisAmount, ProbeVolumeDynamicGIBasis basis, float sharpness,
            ProbeVolumeDynamicGIBasisPropagationOverride basisPropagationOverride, float propagationSharpness)
        {
            var settingsHash = 13;
            settingsHash = settingsHash * 23 + basis.GetHashCode();
            settingsHash = settingsHash * 23 + sharpness.GetHashCode();
            settingsHash = settingsHash * 23 + basisPropagationOverride.GetHashCode();
            settingsHash = settingsHash * 23 + propagationSharpness.GetHashCode();
            settingsHash = settingsHash * 23 + axisAmount.GetHashCode();

            if (settingsHash != _propagationSettingsHash)
            {
                BasisFunction basisFunction = basis switch
                {
                    ProbeVolumeDynamicGIBasis.BasisSphericalGaussianWindowed => BasisSGClampedCosineWindowEvaluate,
                    ProbeVolumeDynamicGIBasis.BasisAmbientDiceSharp => BasisAmbientDiceSharpEvaluate,
                    ProbeVolumeDynamicGIBasis.BasisAmbientDiceSofter => BasisAmbientDiceSofterEvaluate,
                    ProbeVolumeDynamicGIBasis.BasisAmbientDiceSuperSoft => BasisAmbientDiceSuperSoftEvaluate,
                    ProbeVolumeDynamicGIBasis.BasisAmbientDiceUltraSoft => BasisAmbientDiceUltraSoftEvaluate,
                    _ => BasisSGEvaluate
                };

                BasisFunction basisPropagationFunction = basisPropagationOverride switch
                {
                    ProbeVolumeDynamicGIBasisPropagationOverride.BasisSphericalGaussian => BasisMissSGEvaluate,
                    ProbeVolumeDynamicGIBasisPropagationOverride.BasisAmbientDiceWrappedSofter => BasisAmbientDiceWrappedSofterEvaluate,
                    ProbeVolumeDynamicGIBasisPropagationOverride.BasisAmbientDiceWrappedSuperSoft => BasisAmbientDiceWrappedSuperSoftEvaluate,
                    ProbeVolumeDynamicGIBasisPropagationOverride.BasisAmbientDiceWrappedUltraSoft => BasisAmbientDiceWrappedUltraSoftEvaluate,

                    // No override, use the setting of the hit basis for miss propagation.
                    _ => basis switch
                    {
                        // For miss propagation with SG, we do not use different amplitudes per axis -
                        // since it is more of a radial blur filter than a storage basis.
                        // We want to blur all axes the same amount.
                        // TODO: Discuss why. We still use different amplitudes per axis in some Ambient Dice options.
                        ProbeVolumeDynamicGIBasis.BasisSphericalGaussian => BasisMissSGEvaluate,
                        ProbeVolumeDynamicGIBasis.BasisSphericalGaussianWindowed => BasisMissSGClampedCosineWindowEvaluate,

                        // For any Ambient Dice option just use the same exact basis as hits.
                        _ => basisFunction
                    }
                };

                PrecomputeAxisCacheLookup(axisAmount, basisFunction, sharpness, basisPropagationFunction, propagationSharpness);

                cmd.SetComputeBufferData(_sortedNeighborAxisLookupsBuffer, _sortedNeighborAxisLookups);

                _propagationSettingsHash = settingsHash;
            }
        }

        unsafe void PrecomputeAxisCacheLookup(int axisAmount, BasisFunction basisFunction, float sharpness,
            BasisFunction basisPropagationFunction, float propagationSharpness)
        {
            for (int axisIndex = 0; axisIndex < s_NeighborAxis.Length; ++axisIndex)
            {
                var axis = s_NeighborAxis[axisIndex];
                var sortedAxisStart = axisIndex * s_NeighborAxis.Length;

                for (int neighborIndex = 0; neighborIndex < s_NeighborAxis.Length; ++neighborIndex)
                {
                    var neighborDirection = s_NeighborAxis[neighborIndex];

                    var hitWeight = basisFunction(axis, neighborDirection, sharpness);

                    // For propagation instead of evaluated axis we use neighbor direction as the basis mean.
                    // It's not the same because bases can have lower amplitude for corners and diagonals in some modes.
                    var propagationWeight = basisPropagationFunction(neighborDirection, axis, propagationSharpness);

                    _sortedNeighborAxisLookups[sortedAxisStart + neighborIndex] = new NeighborAxisLookup(neighborIndex, hitWeight, propagationWeight, neighborDirection);
                }

                fixed (NeighborAxisLookup* sortedAxisPtr = &_sortedNeighborAxisLookups[sortedAxisStart])
                {
                    CoreUnsafeUtils.QuickSort<NeighborAxisLookup, NeighborAxisLookup, NeighborAxisLookup.NeighborAxisLookupKeyGetter>(s_NeighborAxis.Length, sortedAxisPtr);
                }

                // Renormalize so all weights still add up to what they would have added up to had we not truncated any of the axes.
                // Careful: this is not 1.0 on all axes - some are intentionally more or less represented due to the way the basis is constructed.
                var hitWeights = 0f;
                var hitWeightsGoal = 0f;
                var propagationWeights = 0f;
                var propagationWeightsGoal = 0f;
                for (int sortedAxisIndex = 0; sortedAxisIndex < s_NeighborAxis.Length; sortedAxisIndex++)
                {
                    if (sortedAxisIndex < axisAmount)
                    {
                        hitWeights += _sortedNeighborAxisLookups[sortedAxisStart + sortedAxisIndex].hitWeight;
                        propagationWeights += _sortedNeighborAxisLookups[sortedAxisStart + sortedAxisIndex].propagationWeight;
                    }

                    hitWeightsGoal += _sortedNeighborAxisLookups[sortedAxisStart + sortedAxisIndex].hitWeight;
                    propagationWeightsGoal += _sortedNeighborAxisLookups[sortedAxisStart + sortedAxisIndex].propagationWeight;
                }
                float hitWeightsNormalization = hitWeightsGoal / hitWeights;
                float propagationWeightsNormalization = propagationWeightsGoal / propagationWeights;
                for (int sortedAxisIndex = 0; sortedAxisIndex < axisAmount; sortedAxisIndex++)
                {
                    _sortedNeighborAxisLookups[sortedAxisStart + sortedAxisIndex].hitWeight *= hitWeightsNormalization;
                    _sortedNeighborAxisLookups[sortedAxisStart + sortedAxisIndex].propagationWeight *= propagationWeightsNormalization;
                }
            }
        }

        delegate float BasisFunction(Vector3 mean, Vector3 direction, float sgSharpness);

        static float BasisSGEvaluate(Vector3 mean, Vector3 direction, float sgSharpness)
        {
            var amplitude = ComputeSGAmplitudeFromSharpnessAndAxisBasis26Fit(sgSharpness, mean);
            return SGEvaluateFromDirection(amplitude, sgSharpness, mean, direction);
        }

        static float BasisMissSGEvaluate(Vector3 mean, Vector3 direction, float sgSharpness)
        {
            var amplitude = ComputeSGAmplitudeFromSharpnessBasis26Fit(sgSharpness);
            return SGEvaluateFromDirection(amplitude, sgSharpness, mean, direction);
        }

        static float BasisSGClampedCosineWindowEvaluate(Vector3 mean, Vector3 direction, float sgSharpness)
        {
            var amplitude = ComputeSGClampedCosineWindowAmplitudeFromSharpnessAndAxisBasis26Fit(sgSharpness, mean);
            return SGClampedCosineWindowEvaluateFromDirection(amplitude, sgSharpness, mean, direction);
        }

        static float BasisMissSGClampedCosineWindowEvaluate(Vector3 mean, Vector3 direction, float sgSharpness)
        {
            var amplitude = ComputeSGClampedCosineWindowAmplitudeFromSharpnessBasis26Fit(sgSharpness);
            return SGClampedCosineWindowEvaluateFromDirection(amplitude, sgSharpness, mean, direction);
        }

        static float BasisAmbientDiceSharpEvaluate(Vector3 mean, Vector3 direction, float sgSharpness)
        {
            ComputeAmbientDiceSharpAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(out var amplitude, out var sharpness, mean);
            return AmbientDiceEvaluateFromDirection(amplitude, sharpness, mean, direction);
        }

        static float BasisAmbientDiceSofterEvaluate(Vector3 mean, Vector3 direction, float sgSharpness)
        {
            ComputeAmbientDiceSofterAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(out var amplitude, out var sharpness, mean);
            return AmbientDiceEvaluateFromDirection(amplitude, sharpness, mean, direction);
        }

        static float BasisAmbientDiceSuperSoftEvaluate(Vector3 mean, Vector3 direction, float sgSharpness)
        {
            ComputeAmbientDiceSuperSoftAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(out var amplitude, out var sharpness, mean);
            return AmbientDiceEvaluateFromDirection(amplitude, sharpness, mean, direction);
        }

        static float BasisAmbientDiceUltraSoftEvaluate(Vector3 mean, Vector3 direction, float sgSharpness)
        {
            ComputeAmbientDiceUltraSoftAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(out var amplitude, out var sharpness, mean);
            return AmbientDiceEvaluateFromDirection(amplitude, sharpness, mean, direction);
        }

        static float BasisAmbientDiceWrappedSofterEvaluate(Vector3 mean, Vector3 direction, float sgSharpness)
        {
            ComputeAmbientDiceSofterAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(out var amplitude, out var sharpness, mean);
            return AmbientDiceWrappedEvaluateFromDirection(amplitude, sharpness, mean, direction);
        }

        static float BasisAmbientDiceWrappedSuperSoftEvaluate(Vector3 mean, Vector3 direction, float sgSharpness)
        {
            ComputeAmbientDiceSuperSoftAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(out var amplitude, out var sharpness, mean);
            return AmbientDiceWrappedEvaluateFromDirection(amplitude, sharpness, mean, direction);
        }

        static float BasisAmbientDiceWrappedUltraSoftEvaluate(Vector3 mean, Vector3 direction, float sgSharpness)
        {
            ComputeAmbientDiceUltraSoftAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(out var amplitude, out var sharpness, mean);
            return AmbientDiceWrappedEvaluateFromDirection(amplitude, sharpness, mean, direction);
        }

        internal void ResetSimulationRequests()
        {
            _stats.Reset();
            _probeVolumeSimulationRequestCount = 0;
            ++_probeVolumeSimulationFrameTick;
        }

        internal void AddSimulationRequest(List<ProbeVolumeHandle> volumes, int probeVolumeIndex)
        {
            var probeVolume = volumes[probeVolumeIndex];
            if (probeVolume.AbleToSimulateDynamicGI() && _probeVolumeSimulationRequestCount < _probeVolumeSimulationRequests.Length)
            {
                _stats.SimulationRequested(probeVolume);
                var lastSimulatedFrame = probeVolume.GetPropagationPipelineData().simulationFrameTick;
                _probeVolumeSimulationRequests[_probeVolumeSimulationRequestCount] = new ProbeVolumeSimulationRequest
                {
                    probeVolumeIndex = probeVolumeIndex,
                    simulationFrameDelta = Mathf.Abs(lastSimulatedFrame - _probeVolumeSimulationFrameTick)
                };
                ++_probeVolumeSimulationRequestCount;
            }
            else
            {
                if (CleanupPropagation(probeVolume))
                {
                    // Clear the atlas data so original bake data gets set since Dynamic GI was disabled
                    if (RenderPipelineManager.currentPipeline is HDRenderPipeline hdrp)
                        hdrp.ReleaseProbeVolumeFromAtlas(probeVolume);
                }
            }
        }

        unsafe internal ProbeVolumeSimulationRequest[] SortSimulationRequests(int maxSimulationsPerFrameOverride, out int numSimulationRequests)
        {
            fixed (ProbeVolumeSimulationRequest* requestPtr = &_probeVolumeSimulationRequests[0])
            {
                CoreUnsafeUtils.QuickSort<ProbeVolumeSimulationRequest, ProbeVolumeSimulationRequest, ProbeVolumeSimulationRequest.ProbeVolumeSimulationRequestKeyGetter>(_probeVolumeSimulationRequestCount, requestPtr);
            }

            int maxSimulationsPerFrame;
            if (maxSimulationsPerFrameOverride >= 0 && maxSimulationsPerFrameOverride < MAX_SIMULATIONS_PER_FRAME)
                maxSimulationsPerFrame = maxSimulationsPerFrameOverride;
            else
                maxSimulationsPerFrame = MAX_SIMULATIONS_PER_FRAME;

            numSimulationRequests = Mathf.Min(_probeVolumeSimulationRequestCount, maxSimulationsPerFrame);

            return _probeVolumeSimulationRequests;
        }

        internal struct ProbeVolumeSimulationRequest : IComparable<ProbeVolumeSimulationRequest>
        {
            public int probeVolumeIndex;
            public int simulationFrameDelta;

            public int CompareTo(ProbeVolumeSimulationRequest other)
            {
                return other.simulationFrameDelta - simulationFrameDelta;
            }

            public struct ProbeVolumeSimulationRequestKeyGetter : CoreUnsafeUtils.IKeyGetter<ProbeVolumeSimulationRequest, ProbeVolumeSimulationRequest>
            {
                public ProbeVolumeSimulationRequest Get(ref ProbeVolumeSimulationRequest v) { return v; }
            }

        }

        struct NeighborAxisLookup : IComparable<NeighborAxisLookup>
        {
            public Vector3 neighborDirection;
            public float hitWeight;
            public float propagationWeight;
            public int index;

            public NeighborAxisLookup(int index, float hitWeight, float propagationWeight, Vector3 neighborDirection)
            {
                this.index = index;
                this.hitWeight = hitWeight;
                this.propagationWeight = propagationWeight;
                this.neighborDirection = neighborDirection;
            }

            public int CompareTo(NeighborAxisLookup other)
            {
                float diff = propagationWeight - other.propagationWeight;
                return diff < 0 ? 1 : diff > 0 ? -1 : 0;
            }

            public struct NeighborAxisLookupKeyGetter : CoreUnsafeUtils.IKeyGetter<NeighborAxisLookup, NeighborAxisLookup>
            {
                public NeighborAxisLookup Get(ref NeighborAxisLookup v) { return v; }
            }
        }
    }

} // UnityEngine.Experimental.Rendering.HDPipeline
