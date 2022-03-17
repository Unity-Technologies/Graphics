using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        public int radianceReadIndex;
        public int buffersDataVersion;
        public int simulationFrameTick;
        
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

        public static ProbeVolumePropagationPipelineData Empty => new ProbeVolumePropagationPipelineData
        {
            buffersDataVersion = -1,
            simulationFrameTick = -1,
        };
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
        private ComputeShader _PropagationHitsShader = null;
        private ComputeShader _PropagationAxesShader = null;
        private ComputeShader _PropagationCombineShader = null;

        private NeighborAxisLookup[] _sortedNeighborAxisLookups;
        private ComputeBuffer _sortedNeighborAxisLookupsBuffer;
        private ProbeVolumeSimulationRequest[] _probeVolumeSimulationRequests;

        private const int MAX_SIMULATIONS_PER_FRAME = 128;
        private int _propagationSettingsHash;
        private int _probeVolumeSimulationRequestCount = 0;
        private int _probeVolumeSimulationFrameTick = 0;

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


        internal void Allocate(RenderPipelineResources resources)
        {
            Cleanup(); // To avoid double alloc.

            _PropagationClearRadianceShader = resources.shaders.probePropagationClearRadianceCS;
            _PropagationHitsShader = resources.shaders.probePropagationHitsCS;
            _PropagationAxesShader = resources.shaders.probePropagationAxesCS;
            _PropagationCombineShader = resources.shaders.probePropagationCombineCS;

            ProbeVolume.EnsureBuffer<NeighborAxisLookup>(ref _sortedNeighborAxisLookupsBuffer, _sortedNeighborAxisLookups.Length);

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


        internal void DispatchProbePropagation(CommandBuffer cmd, ProbeVolumeHandle probeVolume,
            ProbeDynamicGI giSettings, in ShaderVariablesGlobal shaderGlobals,
            RenderTargetIdentifier probeVolumeAtlasSHRTHandle, bool infiniteBounces,
            PropagationQuality propagationQuality, SphericalHarmonicsL2 ambientProbe)
        {
            var previousRadianceCacheInvalid = InitializePropagationBuffers(probeVolume);
            if (previousRadianceCacheInvalid || giSettings.clear.value || _clearAllActive)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProbeVolumeDynamicGIClear)))
                    DispatchClearPreviousRadianceCache(cmd, probeVolume);
            }
            
            if (probeVolume.HitNeighborAxisLength != 0)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProbeVolumeDynamicGIHits)))
                    DispatchPropagationHits(cmd, probeVolume, in giSettings, infiniteBounces, previousRadianceCacheInvalid);
            }
            
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProbeVolumeDynamicGIAxes)))
                DispatchPropagationAxes(cmd, probeVolume, in giSettings, previousRadianceCacheInvalid, propagationQuality, ambientProbe);
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProbeVolumeDynamicGICombine)))
                DispatchPropagationCombine(cmd, probeVolume, in giSettings, in shaderGlobals, probeVolumeAtlasSHRTHandle);

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
                    // trigger an update so original bake data gets set since Dynamic GI was disabled
                    probeVolume.IncrementDataVersion();
                }
            }
        }

        void DispatchClearPreviousRadianceCache(CommandBuffer cmd, ProbeVolumeHandle probeVolume)
        {
            var kernel = _PropagationClearRadianceShader.FindKernel("ClearPreviousRadianceCache");
            var shader = _PropagationClearRadianceShader;

            ref var propagationPipelineData = ref probeVolume.GetPropagationPipelineData();

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

        void DispatchPropagationHits(CommandBuffer cmd, ProbeVolumeHandle probeVolume, in ProbeDynamicGI giSettings, bool infiniteBounces, bool previousRadianceCacheInvalid)
        {
            var kernel = _PropagationHitsShader.FindKernel("AccumulateLightingDirectional");
            var shader = _PropagationHitsShader;

            ref var pipelineData = ref probeVolume.GetPipelineData();
            ref var propagationPipelineData = ref probeVolume.GetPropagationPipelineData();

            SetBasisKeywords(giSettings.basis.value, giSettings.basisPropagationOverride.value, shader);

            var obb = pipelineData.BoundingBox;
            var data = pipelineData.EngineData;
            cmd.SetComputeFloatParam(shader, "_ProbeVolumeDGIMaxNeighborDistance", data.maxNeighborDistance);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGIResolutionXY", (int)data.resolutionXY);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGIResolutionX", (int)data.resolutionX);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIResolutionInverse", data.resolutionInverse);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsRight", obb.right);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsUp", obb.up);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsExtents", new Vector3(obb.extentX, obb.extentY, obb.extentZ));
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsCenter", obb.center);

            cmd.SetComputeBufferParam(shader, kernel, "_ProbeVolumeNeighborHits", propagationPipelineData.neighborHits);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeNeighborHitCount", propagationPipelineData.neighborHits.count);
            cmd.SetComputeFloatParam(shader, "_IndirectScale", giSettings.indirectMultiplier.value);
            cmd.SetComputeFloatParam(shader, "_MaxAlbedo", giSettings.maxAlbedo.value);
            cmd.SetComputeFloatParam(shader, "_BakedEmissionMultiplier", giSettings.bakedEmissionMultiplier.value);
            cmd.SetComputeFloatParam(shader, "_RayBias", giSettings.bias.value);
            cmd.SetComputeFloatParam(shader, "_LeakMultiplier", giSettings.leakMultiplier.value);
            cmd.SetComputeFloatParam(shader, "_Sharpness", giSettings.sharpness.value);
            cmd.SetComputeVectorArrayParam(shader, "_RayAxis", s_NeighborAxis);

            cmd.SetComputeFloatParam(shader, "_RangeBehindCamera", giSettings.rangeBehindCamera.value);
            cmd.SetComputeFloatParam(shader, "_RangeInFrontOfCamera", giSettings.rangeInFrontOfCamera.value);

            cmd.SetComputeBufferParam(shader, kernel, "_PreviousRadianceCacheAxis", propagationPipelineData.GetReadRadianceCacheAxis());
            cmd.SetComputeIntParam(shader, "_RadianceCacheAxisCount", propagationPipelineData.radianceCacheAxis0.count);
            cmd.SetComputeBufferParam(shader, kernel, "_HitRadianceCacheAxis", propagationPipelineData.hitRadianceCache);
            cmd.SetComputeIntParam(shader, "_HitRadianceCacheAxisCount", probeVolume.HitNeighborAxisLength);

            // TODO: replace with real one
            cmd.SetComputeTextureParam(shader, kernel, "_HierarchicalVarianceScreenSpaceShadowsTexture", TextureXR.GetWhiteTexture());

            float infBounce = infiniteBounces ? giSettings.infiniteBounce.value : 0f;
            cmd.SetComputeFloatParam(shader, "_InfiniteBounce", infBounce);
            CoreUtils.SetKeyword(shader, "COMPUTE_INFINITE_BOUNCE", infBounce > 0);
            CoreUtils.SetKeyword(shader, "PREVIOUS_RADIANCE_CACHE_INVALID", previousRadianceCacheInvalid);

            int numHits = propagationPipelineData.neighborHits.count;
            int dispatchX = (numHits + 63) / 64;
            cmd.DispatchCompute(shader, kernel, dispatchX, 1, 1);
        }

        void DispatchPropagationAxes(CommandBuffer cmd, ProbeVolumeHandle probeVolume, in ProbeDynamicGI giSettings,
            bool previousRadianceCacheInvalid, PropagationQuality propagationQuality, SphericalHarmonicsL2 ambientProbe)
        {
            var kernel = _PropagationAxesShader.FindKernel("PropagateLight");
            var shader = _PropagationAxesShader;

            ref var pipelineData = ref probeVolume.GetPipelineData();
            ref var propagationPipelineData = ref probeVolume.GetPropagationPipelineData();

            SetBasisKeywords(giSettings.basis.value, giSettings.basisPropagationOverride.value, shader);

            var obb = pipelineData.BoundingBox;
            var data = pipelineData.EngineData;

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

            cmd.SetComputeFloatParam(shader, "_ProbeVolumeDGIMaxNeighborDistance", data.maxNeighborDistance);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGIResolutionXY", (int)data.resolutionXY);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGIResolutionX", (int)data.resolutionX);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGIResolutionY", (int)data.resolution.y);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGIResolutionZ", (int)data.resolution.z);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGILightLayers", unchecked((int)data.lightLayers));
            cmd.SetComputeIntParam(shader, "_ProbeVolumeDGIEngineDataIndex", pipelineData.EngineDataIndex);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIResolutionInverse", data.resolutionInverse);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsRight", obb.right);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsUp", obb.up);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsExtents", new Vector3(obb.extentX, obb.extentY, obb.extentZ));
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsCenter", obb.center);

            cmd.SetComputeBufferParam(shader, kernel, "_ProbeVolumeNeighbors", propagationPipelineData.neighbors);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeNeighborsCount", propagationPipelineData.neighbors.count);
            cmd.SetComputeIntParam(shader, "_ProbeVolumeProbeCount", propagationPipelineData.neighbors.count / s_NeighborAxis.Length);
            cmd.SetComputeFloatParam(shader, "_LeakMultiplier", giSettings.leakMultiplier.value);
            cmd.SetComputeFloatParam(shader, "_PropagationContribution", giSettings.propagationContribution.value);
            cmd.SetComputeFloatParam(shader, "_Sharpness", giSettings.sharpness.value);
            cmd.SetComputeVectorArrayParam(shader, "_RayAxis", s_NeighborAxis);

            cmd.SetComputeBufferParam(shader, kernel, "_HitRadianceCacheAxis", propagationPipelineData.hitRadianceCache);
            cmd.SetComputeIntParam(shader, "_HitRadianceCacheAxisCount", probeVolume.HitNeighborAxisLength);

            cmd.SetComputeFloatParam(shader, "_RangeBehindCamera", giSettings.rangeBehindCamera.value);
            cmd.SetComputeFloatParam(shader, "_RangeInFrontOfCamera", giSettings.rangeInFrontOfCamera.value);
            
            UpdateAmbientProbe(ambientProbe, giSettings.skyMultiplier.value);
            cmd.SetComputeVectorArrayParam(shader, "_AmbientProbe", s_AmbientProbe);

            cmd.SetComputeBufferParam(shader, kernel, "_PreviousRadianceCacheAxis", propagationPipelineData.GetReadRadianceCacheAxis());
            cmd.SetComputeBufferParam(shader, kernel, "_RadianceCacheAxis", propagationPipelineData.GetWriteRadianceCacheAxis());

            PrecomputeAxisCacheLookup(cmd, propagationAxisAmount, giSettings.basis.value, giSettings.sharpness.value,
                giSettings.basisPropagationOverride.value, giSettings.propagationSharpness.value);
            cmd.SetComputeBufferParam(shader, kernel, "_SortedNeighborAxisLookups", _sortedNeighborAxisLookupsBuffer);
            CoreUtils.SetKeyword(shader, "PREVIOUS_RADIANCE_CACHE_INVALID", previousRadianceCacheInvalid);

            cmd.SetComputeFloatParam(shader, "_PropagationSharpness", giSettings.propagationSharpness.value);
            cmd.SetComputeFloatParam(shader, "_Sharpness", giSettings.sharpness.value);

            int numHits = propagationPipelineData.neighbors.count;
            int dispatchX = (numHits + 63) / 64;
            cmd.DispatchCompute(shader, kernel, dispatchX, 1, 1);
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

            /*
            // Denormalize Quadratic term:
            c3.z /= 3f;
            c4.z /= 3f;
            c5.z /= 3f;

            // Denormalize DC term:
            c0.w += c3.z;
            c1.w += c4.z;
            c2.w += c5.z;
            */

            s_AmbientProbe[0] = c0;
            s_AmbientProbe[1] = c1;
            s_AmbientProbe[2] = c2;
            s_AmbientProbe[3] = c3;
            s_AmbientProbe[4] = c4;
            s_AmbientProbe[5] = c5;
            s_AmbientProbe[6] = c6;
        }

        void DispatchPropagationCombine(CommandBuffer cmd, ProbeVolumeHandle probeVolume, in ProbeDynamicGI giSettings, in ShaderVariablesGlobal shaderGlobals, RenderTargetIdentifier probeVolumeAtlasSHRTHandle)
        {
            int numProbes = probeVolume.parameters.resolutionX * probeVolume.parameters.resolutionY * probeVolume.parameters.resolutionZ;
            ProbeVolume.ProbeVolumeAtlasKey key = probeVolume.ComputeProbeVolumeAtlasKey();
            var kernel = _PropagationCombineShader.FindKernel("CombinePropagationAxis");
            var shader = _PropagationCombineShader;

            SetBasisKeywords(giSettings.basis.value, giSettings.basisPropagationOverride.value, shader);

            ref var pipelineData = ref probeVolume.GetPipelineData();
            var obb = pipelineData.BoundingBox;

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

            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCount, shaderGlobals._ProbeVolumeAtlasResolutionAndSliceCount);
            cmd.SetComputeVectorParam(shader, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCountInverse, shaderGlobals._ProbeVolumeAtlasResolutionAndSliceCountInverse);
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

            cmd.SetComputeFloatParam(shader, "_BakedLightingContribution", giSettings.bakeAmount.value);
            cmd.SetComputeFloatParam(shader, "_DynamicPropagationContribution", giSettings.dynamicAmount.value);
            cmd.SetComputeVectorArrayParam(shader, "_RayAxis", s_NeighborAxis);

            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsRight", obb.right);
            cmd.SetComputeVectorParam(shader, "_ProbeVolumeDGIBoundsUp", obb.up);
            
            cmd.SetComputeFloatParam(shader, "_PropagationSharpness", giSettings.propagationSharpness.value);
            cmd.SetComputeFloatParam(shader, "_Sharpness", giSettings.sharpness.value);

            int dispatchX = (numProbes + 63) / 64;
            cmd.DispatchCompute(shader, kernel, dispatchX, 1, 1);
        }

        internal bool CleanupPropagation(ProbeVolumeHandle probeVolume)
        {
            ref var propagationPipelineData = ref probeVolume.GetPropagationPipelineData();
            
            bool didDispose = ProbeVolume.CleanupBuffer(propagationPipelineData.neighborHits);
            didDispose |= ProbeVolume.CleanupBuffer(propagationPipelineData.neighbors);
            didDispose |= ProbeVolume.CleanupBuffer(propagationPipelineData.radianceCacheAxis0);
            didDispose |= ProbeVolume.CleanupBuffer(propagationPipelineData.radianceCacheAxis1);
            didDispose |= ProbeVolume.CleanupBuffer(propagationPipelineData.hitRadianceCache);
            
            propagationPipelineData.buffersDataVersion = -1;

            return didDispose;
        }

        private bool InitializePropagationBuffers(ProbeVolumeHandle probeVolume)
        {
            probeVolume.EnsureVolumeBuffers();

            var dataVersion = probeVolume.GetDataVersion();
            ref var propagationPipelineData = ref probeVolume.GetPropagationPipelineData();
            if (propagationPipelineData.buffersDataVersion != dataVersion)
            {
                var hitNeighborAxisLength = probeVolume.HitNeighborAxisLength;
                var hasHitNeighborAxes = hitNeighborAxisLength != 0;
                var hitNeighborAxisLengthOrOne = hasHitNeighborAxes ? hitNeighborAxisLength : 1;
                ProbeVolume.EnsureBuffer<PackedNeighborHit>(ref propagationPipelineData.neighborHits, hitNeighborAxisLengthOrOne);
                if (hasHitNeighborAxes)
                    probeVolume.SetHitNeighborAxis(propagationPipelineData.neighborHits);

                ProbeVolume.EnsureBuffer<NeighborAxis>(ref propagationPipelineData.neighbors, probeVolume.NeighborAxisLength);
                probeVolume.SetNeighborAxis(propagationPipelineData.neighbors);

                int numProbes = probeVolume.parameters.resolutionX * probeVolume.parameters.resolutionY * probeVolume.parameters.resolutionZ;
                int numAxis = numProbes * s_NeighborAxis.Length;

                ProbeVolume.EnsureBuffer<Vector3>(ref propagationPipelineData.hitRadianceCache, hitNeighborAxisLengthOrOne);
                if (ProbeVolume.EnsureBuffer<Vector3>(ref propagationPipelineData.radianceCacheAxis0, numAxis))
                {
                    ProbeVolume.EnsureBuffer<Vector3>(ref propagationPipelineData.radianceCacheAxis1, numAxis);
                    propagationPipelineData.radianceReadIndex = 0;
                }

                propagationPipelineData.buffersDataVersion = dataVersion;
                return true;
            }
            else
            {
                return false;
            }
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
                    CoreUnsafeUtils.QuickSort<NeighborAxisLookup>(s_NeighborAxis.Length, sortedAxisPtr);
                }
                
                // Renormalize so all weights still add up to 1 when using limited axis amount.
                var hitWeights = 0f;
                var propagationWeights = 0f;
                for (int sortedAxisIndex = 0; sortedAxisIndex < axisAmount; sortedAxisIndex++)
                {
                    hitWeights += _sortedNeighborAxisLookups[sortedAxisStart + sortedAxisIndex].hitWeight;
                    propagationWeights += _sortedNeighborAxisLookups[sortedAxisStart + sortedAxisIndex].propagationWeight;
                }
                for (int sortedAxisIndex = 0; sortedAxisIndex < axisAmount; sortedAxisIndex++)
                {
                    _sortedNeighborAxisLookups[sortedAxisStart + sortedAxisIndex].hitWeight /= hitWeights;
                    _sortedNeighborAxisLookups[sortedAxisStart + sortedAxisIndex].propagationWeight /= propagationWeights;
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
                    // trigger an update so original bake data gets set since Dynamic GI was disabled
                    probeVolume.IncrementDataVersion();
                }
            }
        }

        unsafe internal ProbeVolumeSimulationRequest[] SortSimulationRequests(int maxSimulationsPerFrameOverride, out int numSimulationRequests)
        {
            fixed (ProbeVolumeSimulationRequest* requestPtr = &_probeVolumeSimulationRequests[0])
            {
                CoreUnsafeUtils.QuickSort<ProbeVolumeSimulationRequest>(_probeVolumeSimulationRequestCount, requestPtr);
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
        }
    }

} // UnityEngine.Experimental.Rendering.HDPipeline
