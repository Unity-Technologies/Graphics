using System;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEditor.Experimental;
using Unity.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

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
    }

    [Serializable]
    internal struct NeighborAxis
    {
        public uint hitIndexValidity;

        public const uint Miss = 16777215; // 2 ^ 24 max value
    }

    internal struct ProbePropagationBuffers
    {
        public ComputeBuffer neighborHits;
        public ComputeBuffer neighbors;
        public ComputeBuffer radianceCacheAxis0;
        public ComputeBuffer radianceCacheAxis1;
        public ComputeBuffer hitRadianceCache;
        public int radianceReadIndex;

        public ComputeBuffer GetReadRadianceCacheAxis()
        {
            return (radianceReadIndex == 0) ? radianceCacheAxis0 : radianceCacheAxis1;
        }

        public ComputeBuffer GetWriteRadianceCacheAxis()
        {
            return (radianceReadIndex == 0) ? radianceCacheAxis1 : radianceCacheAxis0;
        }

        public void SwapRadianceCaches()
        {
            radianceReadIndex = (radianceReadIndex + 1) % 2;
        }
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

        private ComputeShader _PropagationClearRadianceShader = null;
        private ComputeShader _PropagationHitsShader = null;
        private ComputeShader _PropagationAxesShader = null;
        private ComputeShader _PropagationCombineShader = null;

        private Vector4[] _sortedAxisLookups;
        private ComputeBuffer _sortedAxisLookupsV2;
        private ProbeVolumeSimulationRequest[] _probeVolumeSimulationRequests;

        private const int MAX_SIMULATIONS_PER_FRAME = 128;
        private float _sortedAxisSharpness = -1;
        private int _probeVolumeSimulationRequestCount = 0;
        private int _probeVolumeSimulationFrameTick = 0;

        public enum PropagationAxisAmount
        {
            All = 0,
            Most,
            Least
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

            int sortedAxisLookupLength = (s_NeighborAxis.Length * s_NeighborAxis.Length);
            _sortedAxisLookups = new Vector4[sortedAxisLookupLength];
            ProbeVolume.EnsureBuffer<NeighbourAxisLookup>(ref _sortedAxisLookupsV2, sortedAxisLookupLength);

            _probeVolumeSimulationRequests = new ProbeVolumeSimulationRequest[MAX_SIMULATIONS_PER_FRAME];
        }

        private bool _clearAllActive = false;
        public void ClearAllActive(bool clearAll)
        {
            _clearAllActive = clearAll;
        }

        private bool _overrideInfiniteBounce = false;
        private float _overrideInfiniteBounceValue;
        public void OverrideInfiniteBounce(bool setOverride, float value = 0)
        {
            _overrideInfiniteBounce = setOverride;
            _overrideInfiniteBounceValue = value;
        }

        private int _maxSimulationsPerFrame = MAX_SIMULATIONS_PER_FRAME;
        public void OverrideMaxSimulationsPerFrame(bool setOverride, int maxSimulations)
        {
            if (setOverride)
            {
                _maxSimulationsPerFrame = maxSimulations;
            }
            else
            {
                _maxSimulationsPerFrame = MAX_SIMULATIONS_PER_FRAME;
            }
        }

        private PropagationAxisAmount _propagationAxisAmount = PropagationAxisAmount.All;
        public void OverridePropagationAxisAmount(bool setOverride, PropagationAxisAmount amount)
        {
            if (setOverride)
            {
                _propagationAxisAmount = amount;
            }
            else
            {
                _propagationAxisAmount = PropagationAxisAmount.All;
            }
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

        internal static void AllocateNeighbors(ref ProbeVolumePayload payload, int numMissedAxis, int numHitAxis, int numAxis)
        {
            int totalAxis = numMissedAxis + numHitAxis;
            int totalProbes = totalAxis / s_NeighborAxis.Length;
            int totalProbeNeighborCapacity = (1 << 19);
            Debug.Assert(totalProbes < totalProbeNeighborCapacity);

            payload.hitNeighborAxis= new PackedNeighborHit[numHitAxis];
            payload.neighborAxis = new NeighborAxis[numAxis];
        }


        internal static void EnsureNeighbors(ref ProbeVolumePayload payload, int missedNeighborAxis, int hitNeighborAxis, int neighborAxis)
        {
            if (payload.hitNeighborAxis == null
                || payload.neighborAxis == null
                || payload.hitNeighborAxis.Length != hitNeighborAxis
                || payload.neighborAxis.Length != neighborAxis)
            {
                AllocateNeighbors(ref payload, missedNeighborAxis, hitNeighborAxis, neighborAxis);
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

        private static uint PackAlbedoAndDistance(Vector3 color, float distance, float maxDistance)
        {
            float albedoR = Mathf.Clamp01(color.x);
            float albedoG = Mathf.Clamp01(color.y);
            float albedoB = Mathf.Clamp01(color.z);

            float normalizedDistance = Mathf.Clamp01(distance / maxDistance);

            uint packedOutput = 0;

            packedOutput |= ((uint)(albedoR * 255.5f) << 0);
            packedOutput |= ((uint)(albedoG * 255.5f) << 8);
            packedOutput |= ((uint)(albedoB * 255.5f) << 16);
            packedOutput |= ((uint)(normalizedDistance * 255.5f) << 24);

            return packedOutput;
        }

        private static uint PackEmission(Vector3 color)
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
            output |= (((uint)(validity * 255.5f) & 31) << 5);
            output |= (probeIndex << 13);

            return output;
        }

        // { probeIndex: 24 bits, validity: 8bit }
        private static uint PackIndexAndValidityOnly(uint probeIndex, float validity)
        {
            uint output = 0;

            output |= (((uint)(validity * 255.5f) & 255) << 8);
            output |= (probeIndex << 8);

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


        internal void Allocate(RenderPipelineResources resources)
        {
            Cleanup(); // To avoid double alloc.

            _PropagationClearRadianceShader = resources.shaders.probePropagationClearRadianceCS;
            _PropagationHitsShader = resources.shaders.probePropagationHitsCS;
            _PropagationAxesShader = resources.shaders.probePropagationAxesCS;
            _PropagationCombineShader = resources.shaders.probePropagationCombineCS;

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
            ProbeVolume.CleanupBuffer(_sortedAxisLookupsV2);
        }


        internal void DispatchProbePropagation(CommandBuffer cmd, ProbeVolumeHandle probeVolume, ProbeDynamicGI giSettings, in ShaderVariablesGlobal shaderGlobals, RenderTargetIdentifier probeVolumeAtlasSHRTHandle)
        {
            var previousRadianceCacheInvalid = InitializePropagationBuffers(probeVolume);

            DispatchClearPreviousRadianceCache(cmd, probeVolume, previousRadianceCacheInvalid || giSettings.clear.value || _clearAllActive);

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProbeVolumeDynamicGIHits)))
                DispatchPropagationHits(cmd, probeVolume, in giSettings, previousRadianceCacheInvalid);
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProbeVolumeDynamicGIAxes)))
                DispatchPropagationAxes(cmd, probeVolume, in giSettings, previousRadianceCacheInvalid);
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProbeVolumeDynamicGICombine)))
                DispatchPropagationCombine(cmd, probeVolume, in giSettings, in shaderGlobals, probeVolumeAtlasSHRTHandle);

            _stats.Simulated(probeVolume);
            probeVolume.propagationBuffers.SwapRadianceCaches();
            probeVolume.SetLastSimulatedFrame(_probeVolumeSimulationFrameTick);
        }

        internal void ClearProbePropagation(ProbeVolumeHandle probeVolume)
        {
            if (probeVolume.AbleToSimulateDynamicGI())
            {
                if (CleanupPropagation(probeVolume))
                {
                    // trigger an update so original bake data gets set since Dynamic GI was disabled
                    probeVolume.SetDataUpdated(true);
                }
            }
        }

        void DispatchClearPreviousRadianceCache(CommandBuffer cmd, ProbeVolumeHandle probeVolume, bool previousRadianceCacheInvalid)
        {
            if (previousRadianceCacheInvalid)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProbeVolumeDynamicGIClear)))
                {
                    var kernel = _PropagationClearRadianceShader.FindKernel("ClearPreviousRadianceCache");
                    var shader = _PropagationClearRadianceShader;

                    cmd.SetComputeBufferParam(shader, kernel, "_RadianceCacheAxis0", probeVolume.propagationBuffers.radianceCacheAxis0);
                    cmd.SetComputeBufferParam(shader, kernel, "_RadianceCacheAxis1", probeVolume.propagationBuffers.radianceCacheAxis1);
                    cmd.SetComputeIntParam(shader, "_RadianceCacheAxisCount", probeVolume.propagationBuffers.radianceCacheAxis0.count);
                    cmd.SetComputeBufferParam(shader, kernel, "_HitRadianceCacheAxis", probeVolume.propagationBuffers.hitRadianceCache);
                    cmd.SetComputeIntParam(shader, "_HitRadianceCacheAxisCount", probeVolume.propagationBuffers.hitRadianceCache.count);

                    int numHits = Mathf.Max(probeVolume.propagationBuffers.radianceCacheAxis0.count, probeVolume.propagationBuffers.hitRadianceCache.count);
                    int dispatchX = (numHits + 63) / 64;
                    cmd.DispatchCompute(shader, kernel, dispatchX, 1, 1);
                }
            }
        }

        void DispatchPropagationHits(CommandBuffer cmd, ProbeVolumeHandle probeVolume, in ProbeDynamicGI giSettings, bool previousRadianceCacheInvalid)
        {
            var kernel = _PropagationHitsShader.FindKernel("AccumulateLightingDirectional");
            var shader = _PropagationHitsShader;

            var obb = probeVolume.GetProbeVolumeEngineDataBoundingBox();
            var data = probeVolume.GetProbeVolumeEngineData();
            cmd.SetComputeFloatParam(shader, "_ProbeVolumeDGIMaxNeighborDistance", data.maxNeighborDistance);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGIResolutionXY", (int)data.resolutionXY);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGIResolutionX", (int)data.resolutionX);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIResolutionInverse", data.resolutionInverse);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsRight", obb.right);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsUp", obb.up);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsExtents", new Vector3(obb.extentX, obb.extentY, obb.extentZ));
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsCenter", obb.center);

            cmd.SetComputeBufferParam(shader, kernel, "_ProbeVolumeNeighborHits", probeVolume.propagationBuffers.neighborHits);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeNeighborHitCount", probeVolume.propagationBuffers.neighborHits.count);
            cmd.SetComputeFloatParam(shader, "_IndirectScale", giSettings.indirectMultiplier.value);
            cmd.SetComputeFloatParam(shader, "_BakedEmissionMultiplier", giSettings.bakedEmissionMultiplier.value);
            cmd.SetComputeFloatParam(shader, "_RayBias", giSettings.bias.value);
            cmd.SetComputeFloatParam(shader, "_LeakMultiplier", giSettings.leakMultiplier.value);
            cmd.SetComputeFloatParam(shader, "_DirectContribution", giSettings.directContribution.value);
            cmd.SetComputeFloatParam(shader, "_InfiniteBounceSharpness", giSettings.infiniteBounceSharpness.value);
            cmd.SetComputeVectorArrayParam(shader, "_RayAxis", s_NeighborAxis);

            cmd.SetComputeFloatParam(shader, "_RangeBehindCamera", giSettings.rangeBehindCamera.value);
            cmd.SetComputeFloatParam(shader, "_RangeInFrontOfCamera", giSettings.rangeInFrontOfCamera.value);

            cmd.SetComputeBufferParam(shader, kernel, "_PreviousRadianceCacheAxis", probeVolume.propagationBuffers.GetReadRadianceCacheAxis());
            cmd.SetComputeIntParam(shader, "_RadianceCacheAxisCount", probeVolume.propagationBuffers.radianceCacheAxis0.count);
            cmd.SetComputeBufferParam(shader, kernel, "_HitRadianceCacheAxis", probeVolume.propagationBuffers.hitRadianceCache);
            cmd.SetComputeIntParam(shader, "_HitRadianceCacheAxisCount", probeVolume.propagationBuffers.hitRadianceCache.count);

            float infBounce = giSettings.infiniteBounce.value;
            if (_overrideInfiniteBounce)
            {
                infBounce = _overrideInfiniteBounceValue;
            }

            cmd.SetComputeFloatParam(shader, "_InfiniteBounce", infBounce);
            CoreUtils.SetKeyword(shader, "COMPUTE_INFINITE_BOUNCE", infBounce > 0);
            CoreUtils.SetKeyword(shader, "PREVIOUS_RADIANCE_CACHE_INVALID", previousRadianceCacheInvalid);

            int numHits = probeVolume.propagationBuffers.neighborHits.count;
            int dispatchX = (numHits + 63) / 64;
            cmd.DispatchCompute(shader, kernel, dispatchX, 1, 1);
        }

        void DispatchPropagationAxes(CommandBuffer cmd, ProbeVolumeHandle probeVolume, in ProbeDynamicGI giSettings, bool previousRadianceCacheInvalid)
        {
            var kernel = _PropagationAxesShader.FindKernel("PropagateLight");
            var shader = _PropagationAxesShader;

            var obb = probeVolume.GetProbeVolumeEngineDataBoundingBox();
            var data = probeVolume.GetProbeVolumeEngineData();

            switch (giSettings.neighborVolumePropagationMode.value)
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

            switch (_propagationAxisAmount)
            {
                case PropagationAxisAmount.All:
                    CoreUtils.SetKeyword(shader, "PROPAGATION_AXIS_MOST", false);
                    CoreUtils.SetKeyword(shader, "PROPAGATION_AXIS_LEAST", false);
                    break;
                case PropagationAxisAmount.Most:
                    CoreUtils.SetKeyword(shader, "PROPAGATION_AXIS_MOST", true);
                    CoreUtils.SetKeyword(shader, "PROPAGATION_AXIS_LEAST", false);
                    break;
                case PropagationAxisAmount.Least:
                    CoreUtils.SetKeyword(shader, "PROPAGATION_AXIS_MOST", false);
                    CoreUtils.SetKeyword(shader, "PROPAGATION_AXIS_LEAST", true);
                    break;
            }

            cmd.SetComputeFloatParam(shader, "_ProbeVolumeDGIMaxNeighborDistance", data.maxNeighborDistance);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGIResolutionXY", (int)data.resolutionXY);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGIResolutionX", (int)data.resolutionX);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGIResolutionY", (int)data.resolution.y);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGIResolutionZ", (int)data.resolution.z);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGILightLayers", unchecked((int)data.lightLayers));
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGIEngineDataIndex", probeVolume.GetProbeVolumeEngineDataIndex());
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIResolutionInverse", data.resolutionInverse);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsRight", obb.right);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsUp", obb.up);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsExtents", new Vector3(obb.extentX, obb.extentY, obb.extentZ));
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsCenter", obb.center);

            cmd.SetComputeBufferParam(shader, kernel, "_ProbeVolumeNeighbors", probeVolume.propagationBuffers.neighbors);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeNeighborsCount", probeVolume.propagationBuffers.neighbors.count);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeProbeCount", probeVolume.propagationBuffers.neighbors.count / s_NeighborAxis.Length);
            cmd.SetComputeFloatParam(shader, "_LeakMultiplier", giSettings.leakMultiplier.value);
            cmd.SetComputeFloatParam(shader, "_PropagationContribution", giSettings.propagationContribution.value);
            cmd.SetComputeFloatParam(shader, "_PropagationSharpness", giSettings.propagationSharpness.value);
            cmd.SetComputeVectorArrayParam(shader, "_RayAxis", s_NeighborAxis);

            cmd.SetComputeBufferParam(shader, kernel, "_HitRadianceCacheAxis", probeVolume.propagationBuffers.hitRadianceCache);
            cmd.SetComputeIntParam(shader, "_HitRadianceCacheAxisCount", probeVolume.propagationBuffers.hitRadianceCache.count);

            cmd.SetComputeFloatParam(shader, "_RangeBehindCamera", giSettings.rangeBehindCamera.value);
            cmd.SetComputeFloatParam(shader, "_RangeInFrontOfCamera", giSettings.rangeInFrontOfCamera.value);

            cmd.SetComputeBufferParam(shader, kernel, "_PreviousRadianceCacheAxis", probeVolume.propagationBuffers.GetReadRadianceCacheAxis());
            cmd.SetComputeBufferParam(shader, kernel, "_RadianceCacheAxis", probeVolume.propagationBuffers.GetWriteRadianceCacheAxis());

            PrecomputeAxisCacheLookup(giSettings.propagationSharpness.value);
            cmd.SetComputeVectorArrayParam(shader, "_SortedNeighborAxis", _sortedAxisLookups);
            cmd.SetComputeBufferParam(shader, kernel, "_SortedNeighborAxisV2", _sortedAxisLookupsV2);
            cmd.SetComputeIntParam(shader, "_SortedNeighborAxisV2Count", _sortedAxisLookupsV2.count);
            CoreUtils.SetKeyword(shader, "PREVIOUS_RADIANCE_CACHE_INVALID", previousRadianceCacheInvalid);

            int numHits = probeVolume.propagationBuffers.neighbors.count;
            int dispatchX = (numHits + 63) / 64;
            cmd.DispatchCompute(shader, kernel, dispatchX, 1, 1);
        }

        void DispatchPropagationCombine(CommandBuffer cmd, ProbeVolumeHandle probeVolume, in ProbeDynamicGI giSettings, in ShaderVariablesGlobal shaderGlobals, RenderTargetIdentifier probeVolumeAtlasSHRTHandle)
        {
            int numProbes = probeVolume.parameters.resolutionX * probeVolume.parameters.resolutionY * probeVolume.parameters.resolutionZ;
            ProbeVolume.ProbeVolumeAtlasKey key = probeVolume.ComputeProbeVolumeAtlasKey();
            var kernel = _PropagationCombineShader.FindKernel("CombinePropagationAxis");
            var shader = _PropagationCombineShader;

            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeResolution, new Vector3(
                probeVolume.parameters.resolutionX,
                probeVolume.parameters.resolutionY,
                probeVolume.parameters.resolutionZ
            ));
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeResolutionInverse, new Vector3(
                1.0f / (float) probeVolume.parameters.resolutionX,
                1.0f / (float) probeVolume.parameters.resolutionY,
                1.0f / (float) probeVolume.parameters.resolutionZ
            ));

            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasScale, probeVolume.parameters.scale);
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasBias, probeVolume.parameters.bias);
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCount, shaderGlobals._ProbeVolumeAtlasResolutionAndSliceCount);
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCountInverse, shaderGlobals._ProbeVolumeAtlasResolutionAndSliceCountInverse);
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasSHRotateRight, key.rotation * new Vector3(1.0f, 0.0f, 0.0f));
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasSHRotateUp, key.rotation * new Vector3(0.0f, 1.0f, 0.0f));
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasSHRotateForward, key.rotation * new Vector3(0.0f, 0.0f, 1.0f));

            var dynamicRotation = probeVolume.rotation;
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasDynamicSHRotateRight, dynamicRotation * new Vector3(1.0f, 0.0f, 0.0f));
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasDynamicSHRotateUp, dynamicRotation * new Vector3(0.0f, 1.0f, 0.0f));
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasDynamicSHRotateForward, dynamicRotation * new Vector3(0.0f, 0.0f, 1.0f));

            cmd.SetComputeIntParam(shader, HDShaderIDs._ProbeVolumeAtlasReadBufferCount, numProbes);

            var volumeBuffers = probeVolume.GetVolumeBuffers();
            cmd.SetComputeBufferParam(shader, kernel, HDShaderIDs._ProbeVolumeAtlasReadSHL01Buffer, volumeBuffers.SHL01Buffer);
            cmd.SetComputeBufferParam(shader, kernel, HDShaderIDs._ProbeVolumeAtlasReadSHL2Buffer, volumeBuffers.SHL2Buffer);

            cmd.SetComputeBufferParam(shader, kernel, HDShaderIDs._ProbeVolumeAtlasReadValidityBuffer, volumeBuffers.ValidityBuffer);
            cmd.SetComputeTextureParam(shader, kernel, HDShaderIDs._ProbeVolumeAtlasWriteTextureSH, probeVolumeAtlasSHRTHandle);

            cmd.SetComputeBufferParam(shader, kernel, "_RadianceCacheAxis", probeVolume.propagationBuffers.GetWriteRadianceCacheAxis());
            cmd.SetComputeIntParam(shader, "_RadianceCacheAxisCount", probeVolume.propagationBuffers.radianceCacheAxis0.count);

            cmd.SetComputeFloatParam(shader, "_BakedLightingContribution", giSettings.bakeAmount.value);
            cmd.SetComputeFloatParam(shader, "_DynamicPropagationContribution", giSettings.dynamicAmount.value);
            cmd.SetComputeVectorArrayParam(shader, "_RayAxis", s_NeighborAxis);

            cmd.SetComputeFloatParam(shader, "_PropagationSharpness", giSettings.propagationSharpness.value);

            switch (giSettings.shFromSGMode.value)
            {
                case ProbeDynamicGI.SHFromSGMode.SamplePeakAndProject:
                {
                    CoreUtils.SetKeyword(shader, "SH_FROM_SG_PBR_FIT", false);
                    CoreUtils.SetKeyword(shader, "SH_FROM_SG_PBR_FIT_WITH_COSINE_WINDOW", false);
                    break;
                }
                case ProbeDynamicGI.SHFromSGMode.SHFromSGFit:
                {
                    CoreUtils.SetKeyword(shader, "SH_FROM_SG_PBR_FIT", true);
                    CoreUtils.SetKeyword(shader, "SH_FROM_SG_PBR_FIT_WITH_COSINE_WINDOW", false);
                    break;
                }
                case ProbeDynamicGI.SHFromSGMode.SHFromSGFitWithCosineWindow:
                {
                    CoreUtils.SetKeyword(shader, "SH_FROM_SG_PBR_FIT", false);
                    CoreUtils.SetKeyword(shader, "SH_FROM_SG_PBR_FIT_WITH_COSINE_WINDOW", true);
                    break;
                }
                default: break;
            }

            int dispatchX = (numProbes + 63) / 64;
            cmd.DispatchCompute(shader, kernel, dispatchX, 1, 1);
        }

        internal bool CleanupPropagation(ProbeVolumeHandle probeVolume)
        {
            bool didDispose = ProbeVolume.CleanupBuffer(probeVolume.propagationBuffers.neighborHits);
            didDispose |= ProbeVolume.CleanupBuffer(probeVolume.propagationBuffers.neighbors);
            didDispose |= ProbeVolume.CleanupBuffer(probeVolume.propagationBuffers.radianceCacheAxis0);
            didDispose |= ProbeVolume.CleanupBuffer(probeVolume.propagationBuffers.radianceCacheAxis1);
            didDispose |= ProbeVolume.CleanupBuffer(probeVolume.propagationBuffers.hitRadianceCache);

            return didDispose;
        }

        private bool InitializePropagationBuffers(ProbeVolumeHandle probeVolume)
        {
            probeVolume.EnsureVolumeBuffers();
            if (ProbeVolume.EnsureBuffer<PackedNeighborHit>(ref probeVolume.propagationBuffers.neighborHits, probeVolume.HitNeighborAxisLength))
            {
                probeVolume.SetHitNeighborAxis(probeVolume.propagationBuffers.neighborHits);
            }

            if (ProbeVolume.EnsureBuffer<NeighborAxis>(ref probeVolume.propagationBuffers.neighbors, probeVolume.NeighborAxisLength))
            {
                probeVolume.SetNeighborAxis(probeVolume.propagationBuffers.neighbors);
            }

            int numProbes = probeVolume.parameters.resolutionX * probeVolume.parameters.resolutionY * probeVolume.parameters.resolutionZ;
            int numAxis = numProbes * s_NeighborAxis.Length;

            bool previousRadianceCacheInvalid = ProbeVolume.EnsureBuffer<Vector3>(ref probeVolume.propagationBuffers.hitRadianceCache, probeVolume.HitNeighborAxisLength);
            if (ProbeVolume.EnsureBuffer<Vector3>(ref probeVolume.propagationBuffers.radianceCacheAxis0, numAxis))
            {
                ProbeVolume.EnsureBuffer<Vector3>(ref probeVolume.propagationBuffers.radianceCacheAxis1, numAxis);
                probeVolume.propagationBuffers.radianceReadIndex = 0;
                previousRadianceCacheInvalid = true;
            }

            return previousRadianceCacheInvalid;
        }

        internal static float GetMaxNeighborDistance(in ProbeVolumeArtistParameters parameters)
        {
            float minDensity = Mathf.Min(parameters.densityX, parameters.densityY);
            minDensity = Mathf.Min(minDensity, parameters.densityZ);
            return 1.0f / minDensity;
        }

        // http://research.microsoft.com/en-us/um/people/johnsny/papers/sg.pdf
        float SGEvaluateFromDirection(float sgAmplitude, float sgSharpness, Vector3 sgMean, Vector3 direction)
        {
            // MADD optimized form of: a.amplitude * exp(a.sharpness * (dot(a.mean, direction) - 1.0));
            return sgAmplitude * Mathf.Exp(Vector3.Dot(sgMean, direction) * sgSharpness - sgSharpness);
        }

        unsafe void PrecomputeAxisCacheLookup(float sgSharpness)
        {
            if (!Mathf.Approximately(_sortedAxisSharpness, sgSharpness))
            {
                for (int axisIndex = 0; axisIndex < s_NeighborAxis.Length; ++axisIndex)
                {
                    var axis = s_NeighborAxis[axisIndex];
                    var sortedAxisStart = axisIndex * s_NeighborAxis.Length;
                    for (int neighborIndex = 0; neighborIndex < s_NeighborAxis.Length; ++neighborIndex)
                    {
                        var neighborDirection = s_NeighborAxis[neighborIndex];
                        var sgWeight = SGEvaluateFromDirection(1, sgSharpness, neighborDirection, axis);
                        sgWeight /= neighborDirection.w * neighborDirection.w;
                        _sortedAxisLookups[sortedAxisStart + neighborIndex] = new Vector4(sgWeight, neighborIndex, 0, 0);

                        // TODO-BD: Set _sortedAxisLookupsV2
                        // s_NeighborAxis - maps to _RayAxis
                        // neighborDirection var is already here
                    }

                    fixed (Vector4* sortedAxisPtr = &_sortedAxisLookups[sortedAxisStart])
                    {
                        CoreUnsafeUtils.QuickSort<AxisVector4>(s_NeighborAxis.Length, sortedAxisPtr);
                    }

                    // TODO-BD: Set _sortedAxisLookupsV2 quicksort
                }

                _sortedAxisSharpness = sgSharpness;
            }
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
                var lastSimulatedFrame = probeVolume.GetLastSimulatedFrame();
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
                    // trigger an update so original bake data gets set since Dynamic GI was disabled
                    probeVolume.SetDataUpdated(true);
                }
            }
        }

        unsafe internal ProbeVolumeSimulationRequest[] SortSimulationRequests(out int numSimulationRequests)
        {
            fixed (ProbeVolumeSimulationRequest* requestPtr = &_probeVolumeSimulationRequests[0])
            {
                CoreUnsafeUtils.QuickSort<ProbeVolumeSimulationRequest>(_probeVolumeSimulationRequestCount, requestPtr);
            }

            numSimulationRequests = Mathf.Min(_probeVolumeSimulationRequestCount, _maxSimulationsPerFrame);

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
        }

        struct AxisVector4 : IComparable<AxisVector4>
        {
            public Vector4 value;

            public AxisVector4(Vector4 newValue)
            {
                value = newValue;
            }

            public int CompareTo(AxisVector4 other)
            {
                float diff = value.x - other.value.x;
                return diff < 0 ? 1 : diff > 0 ? -1 : 0;
            }
        }

        [Serializable] // TODO-BD
        internal struct NeighbourAxisLookup : IComparable<NeighbourAxisLookup>
        {
            public int index;
            public float sgWeight;
            public Vector3 neighbourDirection;

            public NeighbourAxisLookup(int index, float sgWeight, Vector3 neighbourDirection)
            {
                this.index = index;
                this.sgWeight = sgWeight;
                this.neighbourDirection = neighbourDirection;
            }

            public int CompareTo(NeighbourAxisLookup other)
            {
                float diff = sgWeight - other.sgWeight;
                return diff < 0 ? 1 : diff > 0 ? -1 : 0;
            }
        };

    }

} // UnityEngine.Experimental.Rendering.HDPipeline
