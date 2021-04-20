using System;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEditor.Experimental;
using Unity.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Rendering.Hybrid")]
[assembly: InternalsVisibleTo("Unity.Entities.Hybrid.HybridComponents")]

namespace UnityEngine.Rendering.HighDefinition
{
    internal enum ProbeSpacingMode
    {
        Density = 0,
        Resolution
    };

    [GenerateHLSL]
    internal enum VolumeBlendMode
    {
        Normal = 0,
        Additive,
        Subtractive
    }

    // Container structure for managing a probe volume's payload.
    // Spherical Harmonics L2 data is stored across two flat float coefficients arrays, one for L0 and L1 terms, and one for L2 terms.
    // Storing these as two seperate arrays makes it easy for us to conditionally only upload SHL01 terms when the render pipeline is
    // configured to unly use an SphericalHarmonicsL1 atlas.
    // It will also enable us in the future to strip the SHL2 coefficients from the project at build time if only SHL1 is requested.
    // SH Coefficients are serialized in this order, regardless of their format.
    // SH1 will only serialize the first 12
    // SH2 will serialize all 27
    // This is not the order SphericalHarmonicsL2 natively stores these coefficients,
    // and it is also not the order that GPU EntityLighting.hlsl functions expect them in.
    // This order is optimized for minimizing the number of coefficients fetched on the GPU
    // when sampling various SH formats.
    // i.e: If the atlas is configured for SH2, but only SH0 is requested by a specific shader,
    // only the first three coefficients need to be fetched.
    // The atlasing code may make changes to the way this data is laid out in textures,
    // but having them laid out in polynomial order on disk makes writing the atlas transcodings easier.
    // Note: the coefficients in the L2 case are not fully normalized,
    // The data in the SH probe sample passed here is expected to already be normalized with kNormalizationConstants.
    // The complete normalization must be deferred until sample time on the GPU, since it should only be applied for SH2.
    // GPU code will be responsible for performing final normalization + swizzle into formats
    // that SampleSH9(), and SHEvalLinearL0L1() expect.
    // Note: the names for these coefficients is consistent with Unity's internal spherical harmonics use,
    // and are originally from: https://www.ppsloan.org/publications/StupidSH36.pdf
    /*
    {
        // Constant: (used by L0, L1, and L2)
        shAr.w, shAg.w, shAb.w,

        // Linear: (used by L1 and L2)
        shAr.x, shAr.y, shAr.z,
        shAg.x, shAg.y, shAg.z,
        shAb.x, shAb.y, shAb.z,

        // Quadratic: (used by L2)
        shBr.x, shBr.y, shBr.z, shBr.w,
        shBg.x, shBg.y, shBg.z, shBg.w,
        shBb.x, shBb.y, shBb.z, shBb.w,
        shCr.x, shCr.y, shCr.z
    }
    */
    [Serializable]
    internal struct ProbeVolumePayload
    {
        public float[] dataSHL01;
        public float[] dataSHL2;
        public float[] dataValidity;
        public float[] dataOctahedralDepth; // [depth, depthSquared] tuples.

        public static readonly ProbeVolumePayload zero = new ProbeVolumePayload
        {
            dataSHL01 = null,
            dataSHL2 = null,
            dataValidity = null,
            dataOctahedralDepth = null
        };

        public static int GetDataSHL01Stride()
        {
            return 4 * 3;
        }

        public static int GetDataSHL2Stride()
        {
            return 9 * 3 - GetDataSHL01Stride();
        }

        public static int GetDataOctahedralDepthStride()
        {
            return 8 * 8 * 2;
        }

        public static bool IsNull(ref ProbeVolumePayload payload)
        {
            return payload.dataSHL01 == null;
        }

        public static int GetLength(ref ProbeVolumePayload payload)
        {
            // No need to explicitly store probe length - dataValidity is one value per probe, so we can just query the length here.
            return payload.dataValidity.Length;
        }

        public static void Allocate(ref ProbeVolumePayload payload, int length)
        {
            payload.dataSHL01 = new float[length * GetDataSHL01Stride()];
            payload.dataSHL2 = new float[length * GetDataSHL2Stride()];


            // TODO: Only allocate dataValidity and dataOctahedralDepth if those payload slices are in use.
            payload.dataValidity = new float[length];

            payload.dataOctahedralDepth = null;
            if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth)
            {
                payload.dataOctahedralDepth = new float[length * GetDataOctahedralDepthStride()];
            }
        }

        public static void Ensure(ref ProbeVolumePayload payload, int length)
        {
            if (payload.dataSHL01 == null
                || payload.dataSHL01.Length != (length * GetDataSHL01Stride()))
            {
                ProbeVolumePayload.Dispose(ref payload);
                ProbeVolumePayload.Allocate(ref payload, length);
            }
        }

        public static void Dispose(ref ProbeVolumePayload payload)
        {
            payload.dataSHL01 = null;
            payload.dataSHL2 = null;
            payload.dataValidity = null;
            payload.dataOctahedralDepth = null;
        }

        public static void Copy(ref ProbeVolumePayload payloadSrc, ref ProbeVolumePayload payloadDst)
        {
            Debug.Assert(ProbeVolumePayload.GetLength(ref payloadSrc) == ProbeVolumePayload.GetLength(ref payloadDst));

            ProbeVolumePayload.Copy(ref payloadSrc, ref payloadDst, ProbeVolumePayload.GetLength(ref payloadSrc));
        }

        public static void Copy(ref ProbeVolumePayload payloadSrc, ref ProbeVolumePayload payloadDst, int length)
        {
            Array.Copy(payloadSrc.dataSHL01, payloadDst.dataSHL01, length * GetDataSHL01Stride());
            Array.Copy(payloadSrc.dataSHL2, payloadDst.dataSHL2, length * GetDataSHL2Stride());

            Array.Copy(payloadSrc.dataValidity, payloadDst.dataValidity, length);

            if (payloadSrc.dataOctahedralDepth != null && payloadDst.dataOctahedralDepth != null)
            {
                Array.Copy(payloadSrc.dataOctahedralDepth, payloadDst.dataOctahedralDepth, length * GetDataOctahedralDepthStride());
            }
        }

        public static void GetSphericalHarmonicsL1FromIndex(ref SphericalHarmonicsL1 sh, ref ProbeVolumePayload payload, int indexProbe)
        {
            int strideSHL01 = GetDataSHL01Stride();
            int indexDataBaseSHL01 = indexProbe * strideSHL01;
            int indexDataEndSHL01 = indexDataBaseSHL01 + strideSHL01;

            Debug.Assert(payload.dataSHL01 != null);
            Debug.Assert(payload.dataSHL01.Length >= indexDataEndSHL01);

            // Constant (DC terms):
            sh.shAr.w = payload.dataSHL01[indexDataBaseSHL01 + 0]; // shAr.w
            sh.shAg.w = payload.dataSHL01[indexDataBaseSHL01 + 1]; // shAg.w
            sh.shAb.w = payload.dataSHL01[indexDataBaseSHL01 + 2]; // shAb.w

            // Linear: (used by L1 and L2)
            // Swizzle the coefficients to be in { x, y, z } order.
            sh.shAr.x = payload.dataSHL01[indexDataBaseSHL01 + 3]; // shAr.x
            sh.shAr.y = payload.dataSHL01[indexDataBaseSHL01 + 4]; // shAr.y
            sh.shAr.z = payload.dataSHL01[indexDataBaseSHL01 + 5]; // shAr.z

            sh.shAg.x = payload.dataSHL01[indexDataBaseSHL01 + 6]; // shAg.x
            sh.shAg.y = payload.dataSHL01[indexDataBaseSHL01 + 7]; // shAg.y
            sh.shAg.z = payload.dataSHL01[indexDataBaseSHL01 + 8]; // shAg.z

            sh.shAb.x = payload.dataSHL01[indexDataBaseSHL01 + 9]; // shAb.x
            sh.shAb.y = payload.dataSHL01[indexDataBaseSHL01 + 10]; // shAb.y
            sh.shAb.z = payload.dataSHL01[indexDataBaseSHL01 + 11]; // shAb.z
        }

        public static void GetSphericalHarmonicsL2FromIndex(ref SphericalHarmonicsL2 sh, ref ProbeVolumePayload payload, int indexProbe)
        {
            int strideSHL01 = GetDataSHL01Stride();
            int indexDataBaseSHL01 = indexProbe * strideSHL01;
            int indexDataEndSHL01 = indexDataBaseSHL01 + strideSHL01;

            Debug.Assert(payload.dataSHL01 != null);
            Debug.Assert(payload.dataSHL01.Length >= indexDataEndSHL01);

            int strideSHL2 = GetDataSHL2Stride();
            int indexDataBaseSHL2 = indexProbe * strideSHL2;
            int indexDataEndSHL2 = indexDataBaseSHL2 + strideSHL2;

            Debug.Assert(payload.dataSHL2 != null);
            Debug.Assert(payload.dataSHL2.Length >= indexDataEndSHL2);

            // Constant (DC terms):
            sh[0, 0] = payload.dataSHL01[indexDataBaseSHL01 + 0]; // shAr.w
            sh[1, 0] = payload.dataSHL01[indexDataBaseSHL01 + 1]; // shAg.w
            sh[2, 0] = payload.dataSHL01[indexDataBaseSHL01 + 2]; // shAb.w

            // Linear: (used by L1 and L2)
            // Swizzle the coefficients to be in { x, y, z } order.
            sh[0, 3] = payload.dataSHL01[indexDataBaseSHL01 + 3]; // shAr.x
            sh[0, 1] = payload.dataSHL01[indexDataBaseSHL01 + 4]; // shAr.y
            sh[0, 2] = payload.dataSHL01[indexDataBaseSHL01 + 5]; // shAr.z

            sh[1, 3] = payload.dataSHL01[indexDataBaseSHL01 + 6]; // shAg.x
            sh[1, 1] = payload.dataSHL01[indexDataBaseSHL01 + 7]; // shAg.y
            sh[1, 2] = payload.dataSHL01[indexDataBaseSHL01 + 8]; // shAg.z

            sh[2, 3] = payload.dataSHL01[indexDataBaseSHL01 + 9]; // shAb.x
            sh[2, 1] = payload.dataSHL01[indexDataBaseSHL01 + 10]; // shAb.y
            sh[2, 2] = payload.dataSHL01[indexDataBaseSHL01 + 11]; // shAb.z

            // Quadratic: (used by L2)
            sh[0, 4] = payload.dataSHL2[indexDataBaseSHL2 + 0]; // shBr.x
            sh[0, 5] = payload.dataSHL2[indexDataBaseSHL2 + 1]; // shBr.y
            sh[0, 6] = payload.dataSHL2[indexDataBaseSHL2 + 2]; // shBr.z
            sh[0, 7] = payload.dataSHL2[indexDataBaseSHL2 + 3]; // shBr.w

            sh[1, 4] = payload.dataSHL2[indexDataBaseSHL2 + 4]; // shBg.x
            sh[1, 5] = payload.dataSHL2[indexDataBaseSHL2 + 5]; // shBg.y
            sh[1, 6] = payload.dataSHL2[indexDataBaseSHL2 + 6]; // shBg.z
            sh[1, 7] = payload.dataSHL2[indexDataBaseSHL2 + 7]; // shBg.w

            sh[2, 4] = payload.dataSHL2[indexDataBaseSHL2 + 8]; // shBb.x
            sh[2, 5] = payload.dataSHL2[indexDataBaseSHL2 + 9]; // shBb.y
            sh[2, 6] = payload.dataSHL2[indexDataBaseSHL2 + 10]; // shBb.z
            sh[2, 7] = payload.dataSHL2[indexDataBaseSHL2 + 11]; // shBb.w

            sh[0, 8] = payload.dataSHL2[indexDataBaseSHL2 + 12]; // shCr.x
            sh[1, 8] = payload.dataSHL2[indexDataBaseSHL2 + 13]; // shCr.y
            sh[2, 8] = payload.dataSHL2[indexDataBaseSHL2 + 14]; // shCr.z
        }

        public static void SetSphericalHarmonicsL1FromIndex(ref ProbeVolumePayload payload, SphericalHarmonicsL1 sh, int indexProbe)
        {
            int strideSHL01 = GetDataSHL01Stride();
            int indexDataBaseSHL01 = indexProbe * strideSHL01;
            int indexDataEndSHL01 = indexDataBaseSHL01 + strideSHL01;

            Debug.Assert(payload.dataSHL01 != null);
            Debug.Assert(payload.dataSHL01.Length >= indexDataEndSHL01);

            int strideSHL2 = GetDataSHL2Stride();
            int indexDataBaseSHL2 = indexProbe * strideSHL2;
            int indexDataEndSHL2 = indexDataBaseSHL2 + strideSHL2;

            Debug.Assert(payload.dataSHL2 != null);
            Debug.Assert(payload.dataSHL2.Length >= indexDataEndSHL2);

            // Constant (DC terms):
            payload.dataSHL01[indexDataBaseSHL01 + 0] = sh.shAr.w;
            payload.dataSHL01[indexDataBaseSHL01 + 1] = sh.shAg.w;
            payload.dataSHL01[indexDataBaseSHL01 + 2] = sh.shAb.w;

            // Linear: (used by L1 and L2)
            // Swizzle the coefficients to be in { x, y, z } order.
            payload.dataSHL01[indexDataBaseSHL01 + 3] = sh.shAr.x;
            payload.dataSHL01[indexDataBaseSHL01 + 4] = sh.shAr.y;
            payload.dataSHL01[indexDataBaseSHL01 + 5] = sh.shAr.z;

            payload.dataSHL01[indexDataBaseSHL01 + 6] = sh.shAg.x;
            payload.dataSHL01[indexDataBaseSHL01 + 7] = sh.shAg.y;
            payload.dataSHL01[indexDataBaseSHL01 + 8] = sh.shAg.z;

            payload.dataSHL01[indexDataBaseSHL01 + 9] = sh.shAb.x;
            payload.dataSHL01[indexDataBaseSHL01 + 10] = sh.shAb.y;
            payload.dataSHL01[indexDataBaseSHL01 + 11] = sh.shAb.z;

            // Quadratic: (used by L2)
            payload.dataSHL2[indexDataBaseSHL2 + 0] = 0.0f; // shBr.x
            payload.dataSHL2[indexDataBaseSHL2 + 1] = 0.0f; // shBr.y
            payload.dataSHL2[indexDataBaseSHL2 + 2] = 0.0f; // shBr.z
            payload.dataSHL2[indexDataBaseSHL2 + 3] = 0.0f; // shBr.w

            payload.dataSHL2[indexDataBaseSHL2 + 4] = 0.0f; // shBg.x
            payload.dataSHL2[indexDataBaseSHL2 + 5] = 0.0f; // shBg.y
            payload.dataSHL2[indexDataBaseSHL2 + 6] = 0.0f; // shBg.z
            payload.dataSHL2[indexDataBaseSHL2 + 7] = 0.0f; // shBg.w

            payload.dataSHL2[indexDataBaseSHL2 + 8] = 0.0f; // shBb.x
            payload.dataSHL2[indexDataBaseSHL2 + 9] = 0.0f; // shBb.y
            payload.dataSHL2[indexDataBaseSHL2 + 10] = 0.0f; // shBb.z
            payload.dataSHL2[indexDataBaseSHL2 + 11] = 0.0f; // shBb.w

            payload.dataSHL2[indexDataBaseSHL2 + 12] = 0.0f; // shCr.x
            payload.dataSHL2[indexDataBaseSHL2 + 13] = 0.0f; // shCr.y
            payload.dataSHL2[indexDataBaseSHL2 + 14] = 0.0f; // shCr.z
        }

        public static void SetSphericalHarmonicsL2FromIndex(ref ProbeVolumePayload payload, SphericalHarmonicsL2 sh, int indexProbe)
        {
            int strideSHL01 = GetDataSHL01Stride();
            int indexDataBaseSHL01 = indexProbe * strideSHL01;
            int indexDataEndSHL01 = indexDataBaseSHL01 + strideSHL01;

            Debug.Assert(payload.dataSHL01 != null);
            Debug.Assert(payload.dataSHL01.Length >= indexDataEndSHL01);

            int strideSHL2 = GetDataSHL2Stride();
            int indexDataBaseSHL2 = indexProbe * strideSHL2;
            int indexDataEndSHL2 = indexDataBaseSHL2 + strideSHL2;

            Debug.Assert(payload.dataSHL2 != null);
            Debug.Assert(payload.dataSHL2.Length >= indexDataEndSHL2);

            // Constant (DC terms):
            payload.dataSHL01[indexDataBaseSHL01 + 0] = sh[0, 0]; // shAr.w
            payload.dataSHL01[indexDataBaseSHL01 + 1] = sh[1, 0]; // shAg.w
            payload.dataSHL01[indexDataBaseSHL01 + 2] = sh[2, 0]; // shAb.w

            // Linear: (used by L1 and L2)
            // Swizzle the coefficients to be in { x, y, z } order.
            payload.dataSHL01[indexDataBaseSHL01 + 3] = sh[0, 3]; // shAr.x
            payload.dataSHL01[indexDataBaseSHL01 + 4] = sh[0, 1]; // shAr.y
            payload.dataSHL01[indexDataBaseSHL01 + 5] = sh[0, 2]; // shAr.z

            payload.dataSHL01[indexDataBaseSHL01 + 6] = sh[1, 3]; // shAg.x
            payload.dataSHL01[indexDataBaseSHL01 + 7] = sh[1, 1]; // shAg.y
            payload.dataSHL01[indexDataBaseSHL01 + 8] = sh[1, 2]; // shAg.z

            payload.dataSHL01[indexDataBaseSHL01 + 9] = sh[2, 3]; // shAb.x
            payload.dataSHL01[indexDataBaseSHL01 + 10] = sh[2, 1]; // shAb.y
            payload.dataSHL01[indexDataBaseSHL01 + 11] = sh[2, 2]; // shAb.z

            // Quadratic: (used by L2)
            payload.dataSHL2[indexDataBaseSHL2 + 0] = sh[0, 4]; // shBr.x
            payload.dataSHL2[indexDataBaseSHL2 + 1] = sh[0, 5]; // shBr.y
            payload.dataSHL2[indexDataBaseSHL2 + 2] = sh[0, 6]; // shBr.z
            payload.dataSHL2[indexDataBaseSHL2 + 3] = sh[0, 7]; // shBr.w

            payload.dataSHL2[indexDataBaseSHL2 + 4] = sh[1, 4]; // shBg.x
            payload.dataSHL2[indexDataBaseSHL2 + 5] = sh[1, 5]; // shBg.y
            payload.dataSHL2[indexDataBaseSHL2 + 6] = sh[1, 6]; // shBg.z
            payload.dataSHL2[indexDataBaseSHL2 + 7] = sh[1, 7]; // shBg.w

            payload.dataSHL2[indexDataBaseSHL2 + 8] = sh[2, 4]; // shBb.x
            payload.dataSHL2[indexDataBaseSHL2 + 9] = sh[2, 5]; // shBb.y
            payload.dataSHL2[indexDataBaseSHL2 + 10] = sh[2, 6]; // shBb.z
            payload.dataSHL2[indexDataBaseSHL2 + 11] = sh[2, 7]; // shBb.w

            payload.dataSHL2[indexDataBaseSHL2 + 12] = sh[0, 8]; // shCr.x
            payload.dataSHL2[indexDataBaseSHL2 + 13] = sh[1, 8]; // shCr.y
            payload.dataSHL2[indexDataBaseSHL2 + 14] = sh[2, 8]; // shCr.z
        }
    }

    // Rather than hashing all the inputs that define a Probe Volume's bake state into a 128-bit int (16-bytes),
    // we simply store the raw state values (56-bytes)
    // While this is 3.5x more memory, it's still fairly low, and avoids the runtime cost of string appending garbage creation.
    // It also means we can never ever have hash collision issues (due to precision loss in string construction, or from hashing),
    // which means we always detect changes correctly.
    [Serializable]
    internal struct ProbeVolumeSettingsKey
    {
        public int id;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 size;
        public int resolutionX;
        public int resolutionY;
        public int resolutionZ;
        public float backfaceTolerance;
        public int dilationIterations;

        public static readonly ProbeVolumeSettingsKey zero = new ProbeVolumeSettingsKey()
        {
            id = 0,
            position = Vector3.zero,
            rotation = Quaternion.identity,
            size = Vector3.zero,
            resolutionX = 0,
            resolutionY = 0,
            resolutionZ = 0,
            backfaceTolerance = 0.0f,
            dilationIterations = 0
        };
    }

    [Serializable]
    internal struct ProbeVolumeArtistParameters
    {
        public bool drawProbes;
        public bool drawOctahedralDepthRays;
        public int drawOctahedralDepthRayIndexX;
        public int drawOctahedralDepthRayIndexY;
        public int drawOctahedralDepthRayIndexZ;
        public Color debugColor;
        public int payloadIndex;
        public Vector3 size;
        [SerializeField]
        private Vector3 m_PositiveFade;
        [SerializeField]
        private Vector3 m_NegativeFade;
        [SerializeField]
        private float m_UniformFade;
        public bool advancedFade;
        public float distanceFadeStart;
        public float distanceFadeEnd;

        public Vector3 scale;
        public Vector3 bias;
        public Vector4 octahedralDepthScaleBias;

        public ProbeSpacingMode probeSpacingMode;

        public float densityX;
        public float densityY;
        public float densityZ;

        public int resolutionX;
        public int resolutionY;
        public int resolutionZ;

        public VolumeBlendMode volumeBlendMode;
        public float weight;
        public float normalBiasWS;

        public float backfaceTolerance;
        public int dilationIterations;

        public LightLayerEnum lightLayers;

        public Vector3 positiveFade
        {
            get
            {
                return advancedFade ? m_PositiveFade : m_UniformFade * Vector3.one;
            }
            set
            {
                if (advancedFade)
                {
                    m_PositiveFade = value;
                }
                else
                {
                    m_UniformFade = value.x;
                }
            }
        }

        public Vector3 negativeFade
        {
            get
            {
                return advancedFade ? m_NegativeFade : m_UniformFade * Vector3.one;
            }
            set
            {
                if (advancedFade)
                {
                    m_NegativeFade = value;
                }
                else
                {
                    m_UniformFade = value.x;
                }
            }
        }

        public ProbeVolumeArtistParameters(Color debugColor)
        {
            this.debugColor = debugColor;
            this.drawProbes = false;
            this.drawOctahedralDepthRays = false;
            this.drawOctahedralDepthRayIndexX = 0;
            this.drawOctahedralDepthRayIndexY = 0;
            this.drawOctahedralDepthRayIndexZ = 0;
            this.payloadIndex = -1;
            this.size = Vector3.one;
            this.m_PositiveFade = Vector3.zero;
            this.m_NegativeFade = Vector3.zero;
            this.m_UniformFade = 0;
            this.advancedFade = false;
            this.distanceFadeStart = 10000.0f;
            this.distanceFadeEnd = 10000.0f;
            this.scale = Vector3.zero;
            this.bias = Vector3.zero;
            this.octahedralDepthScaleBias = Vector4.zero;
            this.probeSpacingMode = ProbeSpacingMode.Density;
            this.resolutionX = 4;
            this.resolutionY = 4;
            this.resolutionZ = 4;
            this.densityX = (float)this.resolutionX / this.size.x;
            this.densityY = (float)this.resolutionY / this.size.y;
            this.densityZ = (float)this.resolutionZ / this.size.z;
            this.volumeBlendMode = VolumeBlendMode.Normal;
            this.weight = 1;
            this.normalBiasWS = 0.0f;
            this.dilationIterations = 2;
            this.backfaceTolerance = 0.25f;
            this.lightLayers = LightLayerEnum.LightLayerDefault;
        }

        internal void Constrain()
        {
            this.distanceFadeStart = Mathf.Max(0, this.distanceFadeStart);
            this.distanceFadeEnd = Mathf.Max(this.distanceFadeStart, this.distanceFadeEnd);

            switch (this.probeSpacingMode)
            {
                case ProbeSpacingMode.Density:
                {
                    // Compute resolution from density and size.
                    this.densityX = Mathf.Max(1e-5f, this.densityX);
                    this.densityY = Mathf.Max(1e-5f, this.densityY);
                    this.densityZ = Mathf.Max(1e-5f, this.densityZ);

                    this.resolutionX = Mathf.Max(1, Mathf.RoundToInt(this.densityX * this.size.x));
                    this.resolutionY = Mathf.Max(1, Mathf.RoundToInt(this.densityY * this.size.y));
                    this.resolutionZ = Mathf.Max(1, Mathf.RoundToInt(this.densityZ * this.size.z));
                    break;
                }

                case ProbeSpacingMode.Resolution:
                {
                    // Compute density from resolution and size.
                    this.resolutionX = Mathf.Max(1, this.resolutionX);
                    this.resolutionY = Mathf.Max(1, this.resolutionY);
                    this.resolutionZ = Mathf.Max(1, this.resolutionZ);

                    this.densityX = (float)this.resolutionX / Mathf.Max(1e-5f, this.size.x);
                    this.densityY = (float)this.resolutionY / Mathf.Max(1e-5f, this.size.y);
                    this.densityZ = (float)this.resolutionZ / Mathf.Max(1e-5f, this.size.z);
                    break;
                }

                default:
                {
                    Debug.Assert(false, "Error: ProbeVolume: Encountered unsupported Probe Spacing Mode: " + this.probeSpacingMode);
                    break;
                }
            }
        }

        internal ProbeVolumeEngineData ConvertToEngineData()
        {
            ProbeVolumeEngineData data = new ProbeVolumeEngineData();

            data.weight = this.weight;
            data.normalBiasWS = this.normalBiasWS;

            data.debugColor.x = this.debugColor.r;
            data.debugColor.y = this.debugColor.g;
            data.debugColor.z = this.debugColor.b;

            // Clamp to avoid NaNs.
            Vector3 positiveFade = Vector3.Max(this.positiveFade, new Vector3(1e-5f, 1e-5f, 1e-5f));
            Vector3 negativeFade = Vector3.Max(this.negativeFade, new Vector3(1e-5f, 1e-5f, 1e-5f));

            data.rcpPosFaceFade.x = Mathf.Min(1.0f / positiveFade.x, float.MaxValue);
            data.rcpPosFaceFade.y = Mathf.Min(1.0f / positiveFade.y, float.MaxValue);
            data.rcpPosFaceFade.z = Mathf.Min(1.0f / positiveFade.z, float.MaxValue);

            data.rcpNegFaceFade.y = Mathf.Min(1.0f / negativeFade.y, float.MaxValue);
            data.rcpNegFaceFade.x = Mathf.Min(1.0f / negativeFade.x, float.MaxValue);
            data.rcpNegFaceFade.z = Mathf.Min(1.0f / negativeFade.z, float.MaxValue);

            data.volumeBlendMode = (int)this.volumeBlendMode;

            float distFadeLen = Mathf.Max(this.distanceFadeEnd - this.distanceFadeStart, 0.00001526f);
            data.rcpDistFadeLen = 1.0f / distFadeLen;
            data.endTimesRcpDistFadeLen = this.distanceFadeEnd * data.rcpDistFadeLen;

            data.scale = this.scale;
            data.bias = this.bias;
            data.octahedralDepthScaleBias = this.octahedralDepthScaleBias;

            data.resolution = new Vector3(this.resolutionX, this.resolutionY, this.resolutionZ);
            data.resolutionInverse = new Vector3(1.0f / (float)this.resolutionX, 1.0f / (float)this.resolutionY, 1.0f / (float)this.resolutionZ);

            data.lightLayers = (uint)this.lightLayers;

            return data;
        }

    } // class ProbeVolumeArtistParameters

    [ExecuteAlways]
    //[AddComponentMenu("Light/Experimental/Probe Volume")]
    internal class ProbeVolume : MonoBehaviour
    {
#if UNITY_EDITOR
        // Debugging code
        private Material m_DebugMaterial = null;
        private Mesh m_DebugMesh = null;
        private List<Matrix4x4[]> m_DebugProbeMatricesList;
        private List<Mesh> m_DebugProbePointMeshList;
#endif
        private ProbeVolumeSettingsKey bakeKey = new ProbeVolumeSettingsKey
        {
            id = 0,
            position = Vector3.zero,
            rotation = Quaternion.identity,
            size = Vector3.zero,
            resolutionX = 0,
            resolutionY = 0,
            resolutionZ = 0,
            backfaceTolerance = 0.0f,
            dilationIterations = 0
        };

        private bool dataUpdated = false;

#if UNITY_EDITOR
        private bool bakingEnabled = false;
        private bool dataNeedsDilation = false;
#endif

        [SerializeField] internal ProbeVolumeAsset probeVolumeAsset = null;
        [SerializeField] internal ProbeVolumeArtistParameters parameters = new ProbeVolumeArtistParameters(Color.white);

        private int GetID()
        {
            return GetInstanceID();
        }

        private int GetPayloadID()
        {
            return (probeVolumeAsset == null) ? 0 : probeVolumeAsset.GetInstanceID();
        }

        internal int GetBakeID()
        {
            return GetID();
        }

        internal int GetAtlasID()
        {
            // Use the payloadID, rather than the probe volume ID to uniquely identify data in the atlas.
            // This ensures that if 2 probe volume exist that point to the same data, that data will only be uploaded once.
            return GetPayloadID();
        }

        private void BakeKeyClear()
        {
            bakeKey = ProbeVolumeSettingsKey.zero;
        }

        internal bool GetDataIsUpdated()
        {
            return dataUpdated;
        }

        internal ProbeVolumePayload GetPayload()
        {
            dataUpdated = false;

            if (!probeVolumeAsset) { return ProbeVolumePayload.zero; }

            return probeVolumeAsset.payload;
        }

        bool CheckMigrationRequirement()
        {
            if (probeVolumeAsset == null) return false;
            if (probeVolumeAsset.Version == (int)ProbeVolumeAsset.AssetVersion.Current) return false;
            return true;
        }

        void Migrate()
        {
            // Must not be called at deserialization time if require other component
            while (CheckMigrationRequirement())
            {
                ApplyMigration();
            }
        }

        void ApplyMigration()
        {
            switch ((ProbeVolumeAsset.AssetVersion)probeVolumeAsset.Version)
            {
                case ProbeVolumeAsset.AssetVersion.First:
                    ApplyMigrationAddProbeVolumesAtlasEncodingModes();
                    break;

                case ProbeVolumeAsset.AssetVersion.AddProbeVolumesAtlasEncodingModes:
                    ApplyMigrationAddOctahedralDepthVarianceFromLightmapper();
                    break;
                default:
                    // No migration required.
                    break;
            }
        }

        void ApplyMigrationAddProbeVolumesAtlasEncodingModes()
        {
            Debug.Assert(probeVolumeAsset != null && probeVolumeAsset.Version == (int)ProbeVolumeAsset.AssetVersion.First);

            probeVolumeAsset.m_Version = (int)ProbeVolumeAsset.AssetVersion.AddProbeVolumesAtlasEncodingModes;

            int probeLength = probeVolumeAsset.dataSH.Length;

            ProbeVolumePayload.Allocate(ref probeVolumeAsset.payload, probeLength);

            for (int i = 0; i < probeLength; ++i)
            {
                ProbeVolumePayload.SetSphericalHarmonicsL1FromIndex(ref probeVolumeAsset.payload, probeVolumeAsset.dataSH[i], i);
            }

            probeVolumeAsset.dataSH = null;
            probeVolumeAsset.dataValidity = null;
            probeVolumeAsset.dataOctahedralDepth = null;
        }

        void ApplyMigrationAddOctahedralDepthVarianceFromLightmapper()
        {
            Debug.Assert(probeVolumeAsset != null && probeVolumeAsset.Version == (int)ProbeVolumeAsset.AssetVersion.AddProbeVolumesAtlasEncodingModes);

            probeVolumeAsset.m_Version = (int)ProbeVolumeAsset.AssetVersion.AddOctahedralDepthVarianceFromLightmapper;

            if (probeVolumeAsset.payload.dataOctahedralDepth == null) { return; }

            int probeLength = ProbeVolumePayload.GetLength(ref probeVolumeAsset.payload);
            var dataOctahedralDepthMigrated = new float[probeLength * ProbeVolumePayload.GetDataOctahedralDepthStride()];

            // Previously, the lightmapper only returned scalar mean depth values for octahedralDepth.
            // Now it returns float2(depthMean, depthMean^2) which can be used to reconstruct a variance estimate.
            int dataOctahedralDepthLengthPrevious = probeVolumeAsset.payload.dataOctahedralDepth.Length;
            for (int i = 0; i < dataOctahedralDepthLengthPrevious; ++i)
            {
                float depthMean = probeVolumeAsset.payload.dataOctahedralDepth[i];

                // For our migration, simply initialize our depthMeanSquared slots with depthMean * depthMean, which will reconstruct a zero variance estimate.
                // Really, the user will want to rebake to get a real variance estimate. This migration just ensures we do not error out.
                float depthMeanSquared = depthMean * depthMean;
                dataOctahedralDepthMigrated[i * 2 + 0] = depthMean;
                dataOctahedralDepthMigrated[i * 2 + 1] = depthMeanSquared;
            }
            probeVolumeAsset.payload.dataOctahedralDepth = dataOctahedralDepthMigrated;
        }

        protected void OnEnable()
        {
            Migrate();

            dataUpdated = false;
#if UNITY_EDITOR
            dataNeedsDilation = false;

            ForceBakingEnabled();
#endif

            ProbeVolumeManager.manager.RegisterVolume(this);

            // // Signal update
            // // TODO: Do we need to actually flag data as updated when the volume is enabled?
            // // This forces the data to be blitted into the atlas every time a volume is enabled.
            // // Instead, it seems like we should only flag it as updated (only blit) if:
            // // A) A new bake was completed
            // // B) A new probe volume asset was assigned
            // // C) The bake key was updated? i.e: resolution changed, but a bake has not been issued yet.
            // if (probeVolumeAsset)
            //     dataUpdated = true;

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            m_DebugMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            m_DebugMaterial = new Material(Shader.Find("HDRP/Lit"));
#endif
        }

        protected void OnDisable()
        {
            dataUpdated = false;
#if UNITY_EDITOR
            dataNeedsDilation = false;
#endif

            ProbeVolumeManager.manager.DeRegisterVolume(this);

#if UNITY_EDITOR
            // Make sure to tell the lightmapper to no longer attempt to bake the probe positions at this ID.
            // Debug.Log("ForceBakingDisabled() due to disabled probe volume " + this.gameObject.name);
            ForceBakingDisabled();

            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;
#endif
        }

        internal bool IsAssetCompatible()
        {
            return IsAssetCompatibleResolution();
        }

        internal bool IsAssetCompatibleResolution()
        {
            if (probeVolumeAsset)
            {
                return parameters.resolutionX == probeVolumeAsset.resolutionX &&
                       parameters.resolutionY == probeVolumeAsset.resolutionY &&
                       parameters.resolutionZ == probeVolumeAsset.resolutionZ;
            }
            return false;
        }

#if UNITY_EDITOR
        protected void Update()
        {
            OnValidate();
        }

        internal void ForceBakingEnabled()
        {
            BakeKeyClear();
            OnValidate();
        }

        internal void ForceBakingDisabled()
        {
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(GetBakeID(), null);
            bakingEnabled = false;
        }

        protected void OnValidate()
        {
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return;

            // custom-begin: Make lightmapper respect scene visibility toggle.
            // Probe Volumes will now not bake if they are hidden.
            if (UnityEditor.SceneVisibilityManager.instance.IsHidden(this.gameObject))
            {
                // Debug.Log("OnValidate: ForceBakingDisabled() due to hidden probe volume " + this.gameObject.name);
                ForceBakingDisabled();
                return;
            }
            // custom-end

            ProbeVolumeSettingsKey bakeKeyCurrent = ComputeProbeVolumeSettingsKeyFromProbeVolume(this);
            if (ProbeVolumeSettingsKeyEquals(ref bakeKey, ref bakeKeyCurrent) &&
                m_DebugProbeMatricesList != null)
            {
                return;
            }

            parameters.Constrain();

            bakeKey = bakeKeyCurrent;

            if (probeVolumeAsset)
            {
                if (!IsAssetCompatibleResolution())
                {
                    Debug.LogWarningFormat("The asset \"{0}\" assigned to Probe Volume \"{1}\" does not have matching data dimensions ({2}x{3}x{4} vs. {5}x{6}x{7}), please rebake.",
                        probeVolumeAsset.name, this.name,
                        probeVolumeAsset.resolutionX, probeVolumeAsset.resolutionY, probeVolumeAsset.resolutionZ,
                        parameters.resolutionX, parameters.resolutionY, parameters.resolutionZ);
                }

                dataUpdated = true;
            }

            SetupProbePositions();
        }

        internal void OnLightingDataCleared()
        {
            probeVolumeAsset = null;
            dataUpdated = true;
            BakeKeyClear();
        }

        internal void OnLightingDataAssetCleared()
        {
            if (probeVolumeAsset == null)
                return;

            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(probeVolumeAsset);
            if (assetPath == "")
                return;

            UnityEditor.AssetDatabase.DeleteAsset(assetPath);
            UnityEditor.AssetDatabase.Refresh();
            BakeKeyClear();
        }

        internal void OnProbesBakeCompleted()
        {
            if (this.gameObject == null || !this.gameObject.activeInHierarchy)
            {
                // Debug.Log("OnProbesBakeCompleted() ignored by probe volume " + this.gameObject.name + " because it was inactive in the heirarchy.");
                return;
            }

            if (!bakingEnabled)
            {
                // Baking was not setup for this probe volume.
                // This is caused by calls to ForceBakingDisabled()
                return;
            }

            // custom-begin: Make lightmapper respect scene visibility toggle.
            // Probe Volumes will now not bake if they are hidden.
            if (UnityEditor.SceneVisibilityManager.instance.IsHidden(this.gameObject))
            {
                // Debug.Log("OnProbesBakeCompleted() ignored by probe volume " + this.gameObject.name + " because it was hidden.");
                return;
            }
            // custom-end

            // if (probeVolumeAsset != null && probeVolumeAsset.instanceID != GetID())
            // {
            //     // Our probe volume references an asset that it does not own.
            //     // We should not update the data - the probe volume that owns that data is responsible for uploading it.
            //     // TODO: Is this a workflow we intend to support? Or should we error out here?
            //     // Should we disallow even wiring up probe volume assets that to probe volumes that do not own them?
            //     Debug.Log("Finished baking a probe volume who is not the owner of their asset. Skipping data assignment. Asset owner id is " + probeVolumeAsset.instanceID + " and probe volume id is " + GetID());
            //     // return;
            //     Debug.Log("Hack: Not actually going to skip it");
            // }

            int numProbes = parameters.resolutionX * parameters.resolutionY * parameters.resolutionZ;

            var sh = new NativeArray<SphericalHarmonicsL2>(numProbes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var validity = new NativeArray<float>(numProbes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            // TODO: Currently, we need to always allocate and pass this octahedralDepth array into GetAdditionalBakedProbes().
            // In the future, we should add an API call for GetAdditionalBakedProbes() without octahedralDepth required.
#if false
            var octahedralDepth = new NativeArray<Vector2>(numProbes * 8 * 8, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
#else
            var octahedralDepth = new NativeArray<float>(numProbes * 8 * 8, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
#endif

            if(UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(GetBakeID(), sh, validity, octahedralDepth))
            {
                // Debug.Log("GetAdditionalBakedProbes for probe volume: " + this.gameObject.name);

                if (probeVolumeAsset == null)
                {
                    probeVolumeAsset = ProbeVolumeAsset.CreateAsset(GetBakeID());
                    probeVolumeAsset.instanceID = GetID();
                }

                probeVolumeAsset.resolutionX = parameters.resolutionX;
                probeVolumeAsset.resolutionY = parameters.resolutionY;
                probeVolumeAsset.resolutionZ = parameters.resolutionZ;

                ProbeVolumePayload.Ensure(ref probeVolumeAsset.payload, numProbes);

                // Always serialize L0, L1 and L2 coefficients, even if atlas is configured to only store L1.
                // In the future we will strip the L2 coefficients from the project at build time if L2 is never used.
                for (int i = 0, iLen = sh.Length; i < iLen; ++i)
                {
                    ProbeVolumePayload.SetSphericalHarmonicsL2FromIndex(ref probeVolumeAsset.payload, sh[i], i);
                }

                validity.CopyTo(probeVolumeAsset.payload.dataValidity);

                if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth)
                {
                    for (int i = 0, iLen = octahedralDepth.Length; i < iLen; ++i)
                    {
#if false
                        probeVolumeAsset.payload.dataOctahedralDepth[i * 2 + 0] = octahedralDepth[i].x;
                        probeVolumeAsset.payload.dataOctahedralDepth[i * 2 + 1] = octahedralDepth[i].y;
#else
                        probeVolumeAsset.payload.dataOctahedralDepth[i * 2 + 0] = octahedralDepth[i];
                        probeVolumeAsset.payload.dataOctahedralDepth[i * 2 + 1] = octahedralDepth[i] * octahedralDepth[i]; // zero variance.
#endif
                    }
                }

                // if (UnityEditor.Lightmapping.giWorkflowMode != UnityEditor.Lightmapping.GIWorkflowMode.Iterative)
                    UnityEditor.EditorUtility.SetDirty(probeVolumeAsset);

                UnityEditor.AssetDatabase.Refresh();

                dataUpdated = true;
                dataNeedsDilation = true;
            }
            // else
            // {
            //     Debug.Log("GetAdditionalBakedProbes() returned false for probe volume " + this.gameObject.name);
            // }

            sh.Dispose();
            validity.Dispose();
            octahedralDepth.Dispose();
        }

        internal void OnBakeCompleted()
        {
            if (!probeVolumeAsset)
                return;

            if (!dataNeedsDilation)
                return;

            // Debug.Log("Dilating data for probe volume: " + this.gameObject.name);

            probeVolumeAsset.Dilate(parameters.backfaceTolerance, parameters.dilationIterations);
            UnityEditor.EditorUtility.SetDirty(probeVolumeAsset);

            dataUpdated = true;
            dataNeedsDilation = false;
        }

        private static ProbeVolumeSettingsKey ComputeProbeVolumeSettingsKeyFromProbeVolume(ProbeVolume probeVolume)
        {
            return new ProbeVolumeSettingsKey
            {
                id = probeVolume.GetBakeID(),
                position = probeVolume.transform.position,
                rotation = probeVolume.transform.rotation,
                size = probeVolume.parameters.size,
                resolutionX = probeVolume.parameters.resolutionX,
                resolutionY = probeVolume.parameters.resolutionY,
                resolutionZ = probeVolume.parameters.resolutionZ,
                backfaceTolerance = probeVolume.parameters.backfaceTolerance,
                dilationIterations = probeVolume.parameters.dilationIterations
            };
        }

        private static bool ProbeVolumeSettingsKeyEquals(ref ProbeVolumeSettingsKey a, ref ProbeVolumeSettingsKey b)
        {
            return (a.id == b.id)
                && (a.position == b.position)
                && (a.rotation == b.rotation)
                && (a.size == b.size)
                && (a.resolutionX == b.resolutionX)
                && (a.resolutionY == b.resolutionY)
                && (a.resolutionZ == b.resolutionZ)
                && (a.backfaceTolerance == b.backfaceTolerance)
                && (a.dilationIterations == b.dilationIterations);
        }

        private static bool ProbeVolumeSettingsKeyIsCleared(ref ProbeVolumeSettingsKey a)
        {
            return (a.id == ProbeVolumeSettingsKey.zero.id)
                && (a.position == ProbeVolumeSettingsKey.zero.position)
                && (a.rotation == ProbeVolumeSettingsKey.zero.rotation)
                && (a.size == ProbeVolumeSettingsKey.zero.size)
                && (a.resolutionX == ProbeVolumeSettingsKey.zero.resolutionX)
                && (a.resolutionY == ProbeVolumeSettingsKey.zero.resolutionY)
                && (a.resolutionZ == ProbeVolumeSettingsKey.zero.resolutionZ)
                && (a.backfaceTolerance == ProbeVolumeSettingsKey.zero.backfaceTolerance)
                && (a.dilationIterations == ProbeVolumeSettingsKey.zero.dilationIterations);
        }

        private void SetupProbePositions()
        {
            if (!this.gameObject.activeInHierarchy)
                return;

            float debugProbeSize = Gizmos.probeSize;

            int probeCount = parameters.resolutionX * parameters.resolutionY * parameters.resolutionZ;
            Vector3[] positions = new Vector3[probeCount];

            OrientedBBox obb = new OrientedBBox(Matrix4x4.TRS(this.transform.position, this.transform.rotation, parameters.size));

            Vector3 probeSteps = new Vector3(parameters.size.x / (float)parameters.resolutionX, parameters.size.y / (float)parameters.resolutionY, parameters.size.z / (float)parameters.resolutionZ);

            // TODO: Determine why we need to negate obb.forward but not other basis vectors in order to make positions start at the {left, lower, back} corner
            // and end at the {right, top, front} corner (which our atlasing code assumes).
            Vector3 probeStartPosition = obb.center
                - obb.right   * (parameters.size.x - probeSteps.x) * 0.5f
                - obb.up      * (parameters.size.y - probeSteps.y) * 0.5f
                + obb.forward * (parameters.size.z - probeSteps.z) * 0.5f;

            Quaternion rotation = Quaternion.identity;
            Vector3 scale = new Vector3(debugProbeSize, debugProbeSize, debugProbeSize);

            // Debugging objects start here
            int maxBatchSize = 1023;
            int probesInCurrentBatch = System.Math.Min(maxBatchSize, probeCount);
            int indexInCurrentBatch = 0;

            // Everything around cached matrices for the probe spheres
            m_DebugProbeMatricesList = new List<Matrix4x4[]>();
            Matrix4x4[] currentprobeMatrices = new Matrix4x4[probesInCurrentBatch];
            int[] indices = new int[probesInCurrentBatch];

            // Everything around point meshes for non-selected ProbeVolumes
            m_DebugProbePointMeshList = new List<Mesh>();
            int[] currentProbeDebugIndices = new int[probesInCurrentBatch];
            Vector3[] currentProbeDebugPositions = new Vector3[probesInCurrentBatch];

            int processedProbes = 0;

            for (int z = 0; z < parameters.resolutionZ; ++z)
            {
                for (int y = 0; y < parameters.resolutionY; ++y)
                {
                    for (int x = 0; x < parameters.resolutionX; ++x)
                    {
                        Vector3 position = probeStartPosition + (probeSteps.x * x * obb.right) + (probeSteps.y * y * obb.up) + (probeSteps.z * z * -obb.forward);
                        positions[processedProbes] = position;

                        currentProbeDebugIndices[indexInCurrentBatch] = indexInCurrentBatch;
                        currentProbeDebugPositions[indexInCurrentBatch] = position;

                        Matrix4x4 matrix = new Matrix4x4();
                        matrix.SetTRS(position, rotation, scale);
                        currentprobeMatrices[indexInCurrentBatch] = matrix;

                        indexInCurrentBatch++;
                        processedProbes++;

                        int probesLeft = probeCount - processedProbes;

                        if (indexInCurrentBatch >= 1023 || probesLeft == 0)
                        {
                            Mesh currentProbeDebugMesh = new Mesh();
                            currentProbeDebugMesh.SetVertices(currentProbeDebugPositions);
                            currentProbeDebugMesh.SetIndices(currentProbeDebugIndices, MeshTopology.Points, 0);

                            m_DebugProbePointMeshList.Add(currentProbeDebugMesh);
                            m_DebugProbeMatricesList.Add(currentprobeMatrices);

                            // More sets follow, reallocate
                            if (probesLeft > 0)
                            {
                                probesInCurrentBatch = System.Math.Min(maxBatchSize, probesLeft);

                                currentProbeDebugPositions = new Vector3[probesInCurrentBatch];
                                currentProbeDebugIndices = new int[probesInCurrentBatch];
                                currentprobeMatrices = new Matrix4x4[probesInCurrentBatch];

                                indexInCurrentBatch = 0;
                            }
                        }
                    }
                }
            }

            // Debug.Log("SetAdditionalBakedProbes for probe volume: " + this.gameObject.name);
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(GetBakeID(), positions);
            bakingEnabled = true;
        }

        private static bool ShouldDrawGizmos(ProbeVolume probeVolume)
        {
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return false;

            UnityEditor.SceneView sceneView = UnityEditor.SceneView.currentDrawingSceneView;

            if (sceneView == null)
                sceneView = UnityEditor.SceneView.lastActiveSceneView;

            if (sceneView != null && !sceneView.drawGizmos)
                return false;

            if (!probeVolume.enabled)
                return false;

            return probeVolume.parameters.drawProbes;
        }

        [UnityEditor.DrawGizmo(UnityEditor.GizmoType.NotInSelectionHierarchy)]
        private static void DrawProbes(ProbeVolume probeVolume, UnityEditor.GizmoType gizmoType)
        {
            if (!ShouldDrawGizmos(probeVolume))
                return;

            probeVolume.OnValidate();

            var pointMeshList = probeVolume.m_DebugProbePointMeshList;

            probeVolume.m_DebugMaterial.SetPass(8);
            foreach (Mesh debugMesh in pointMeshList)
                Graphics.DrawMeshNow(debugMesh, Matrix4x4.identity);
        }

        internal void DrawSelectedProbes()
        {
            DrawOctahedralDepthRays(this);

            if (!ShouldDrawGizmos(this))
                return;

            OnValidate();

            int layer = 0;

            Material material = m_DebugMaterial;

            if (!material)
                return;

            material.enableInstancing = true;

            Mesh mesh = m_DebugMesh;

            if (!mesh)
                return;

            int submeshIndex = 0;
            MaterialPropertyBlock properties = null;
            ShadowCastingMode castShadows = ShadowCastingMode.Off;
            bool receiveShadows = false;

            Camera emptyCamera = null;
            LightProbeUsage lightProbeUsage = LightProbeUsage.Off;
            LightProbeProxyVolume lightProbeProxyVolume = null;

            foreach (Matrix4x4[] matrices in m_DebugProbeMatricesList)
                Graphics.DrawMeshInstanced(mesh, submeshIndex, material, matrices, matrices.Length, properties, castShadows, receiveShadows, layer, emptyCamera, lightProbeUsage, lightProbeProxyVolume);
        }

        private static void DrawOctahedralDepthRays(ProbeVolume probeVolume)
        {
            if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode != ProbeVolumesBilateralFilteringModes.OctahedralDepth) { return; }
            if (!probeVolume.parameters.drawOctahedralDepthRays) { return; }
            if (probeVolume.probeVolumeAsset == null) { return; }
            if (probeVolume.probeVolumeAsset.payload.dataOctahedralDepth == null) { return; }

            Vector3 probePositionWS = ComputeProbePositionWS(
                probeVolume,
                probeVolume.parameters.drawOctahedralDepthRayIndexX,
                probeVolume.parameters.drawOctahedralDepthRayIndexY,
                probeVolume.parameters.drawOctahedralDepthRayIndexZ
            );

            int probeIndex1D = ComputeProbeIndex1DFrom3D(
                probeVolume,
                probeVolume.parameters.drawOctahedralDepthRayIndexX,
                probeVolume.parameters.drawOctahedralDepthRayIndexY,
                probeVolume.parameters.drawOctahedralDepthRayIndexZ
            );

            const int octahedralDepthResolution = 8;
            int octahedralDepthIndexBase = probeIndex1D * octahedralDepthResolution * octahedralDepthResolution;

            for (int y = 0; y < octahedralDepthResolution; ++y)
            {
                for (int x = 0; x < octahedralDepthResolution; ++x)
                {
                    int i = y * 8 + x + octahedralDepthIndexBase;

                    Vector3 rayDirectionWS = UnpackNormalOctQuadEncode(new Vector2(
                        ((float)x + 0.5f) / octahedralDepthResolution * 2.0f - 1.0f,
                        ((float)y + 0.5f) / octahedralDepthResolution * 2.0f - 1.0f
                    ));

                    float depthMean = probeVolume.probeVolumeAsset.payload.dataOctahedralDepth[i * 2 + 0];
                    float depthMeanSquared = probeVolume.probeVolumeAsset.payload.dataOctahedralDepth[i * 2 + 1];
                    float variance = Mathf.Max(1e-5f, depthMeanSquared - depthMean * depthMean);

                    Vector3 positionVarianceMinWS = rayDirectionWS * Mathf.Max(0.0f, (depthMean - variance)) + probePositionWS;
                    Vector3 positionVarianceMaxWS = rayDirectionWS * Mathf.Max(0.0f, (depthMean + variance)) + probePositionWS;
                    Debug.DrawLine(probePositionWS, positionVarianceMinWS, Color.red);
                    Debug.DrawLine(positionVarianceMinWS, positionVarianceMaxWS, Color.green);
                }
            }
        }

        private static Vector3 ComputeProbePositionWS(ProbeVolume probeVolume, int x, int y, int z)
        {
            Debug.Assert(probeVolume != null);
            Debug.Assert(x >= 0 && x < probeVolume.parameters.resolutionX);
            Debug.Assert(y >= 0 && y < probeVolume.parameters.resolutionY);
            Debug.Assert(z >= 0 && z < probeVolume.parameters.resolutionZ);

            Vector3 uvw = new Vector3(
                (x + 0.5f) / probeVolume.parameters.resolutionX,
                (y + 0.5f) / probeVolume.parameters.resolutionY,
                (z + 0.5f) / probeVolume.parameters.resolutionZ
            );

            Vector3 positionOS = new Vector3(
                (uvw.x - 0.5f) * probeVolume.parameters.size.x,
                (uvw.y - 0.5f) * probeVolume.parameters.size.y,
                (uvw.z - 0.5f) * probeVolume.parameters.size.z
            );
            Vector3 positionWS = (probeVolume.transform.rotation * positionOS) + probeVolume.transform.position;

            return positionWS;
        }

        // Expects a [-1, 1] range value.
        private static Vector3 UnpackNormalOctQuadEncode(Vector2 f)
        {
            Vector3 n = new Vector3(f.x, f.y, 1.0f - Mathf.Abs(f.x) - Mathf.Abs(f.y));
            float t = Mathf.Max(-n.z, 0.0f);

            n = new Vector3(
                n.x + (n.x > 0.0f ? -t : t),
                n.y + (n.y > 0.0f ? -t : t),
                n.z
            );

            return Vector3.Normalize(n);
        }

        internal static int ComputeProbeIndex1DFrom3D(ProbeVolume probeVolume, int x, int y, int z)
        {
            Debug.Assert(probeVolume != null);
            Debug.Assert(x >= 0 && x < probeVolume.parameters.resolutionX);
            Debug.Assert(y >= 0 && y < probeVolume.parameters.resolutionY);
            Debug.Assert(z >= 0 && z < probeVolume.parameters.resolutionZ);

            return z * probeVolume.parameters.resolutionY * probeVolume.parameters.resolutionX
                + y * probeVolume.parameters.resolutionX
                + x;
        }

        internal static Vector2Int ComputeProbeOctahedralDepthIndex2DFrom3D(ProbeVolume probeVolume, int x, int y, int z)
        {
            Debug.Assert(probeVolume != null);
            Debug.Assert(x >= 0 && x < probeVolume.parameters.resolutionX);
            Debug.Assert(y >= 0 && y < probeVolume.parameters.resolutionY);
            Debug.Assert(z >= 0 && z < probeVolume.parameters.resolutionZ);

            // Z slices are packed horizontally.
            return new Vector2Int(
                x + z * probeVolume.parameters.resolutionX,
                y
            );
        }

        internal static Vector4 ComputeProbeOctahedralDepthScaleBias2D(ProbeVolume probeVolume, int x, int y, int z)
        {
            Debug.Assert(probeVolume != null);
            Debug.Assert(x >= 0 && x < probeVolume.parameters.resolutionX);
            Debug.Assert(y >= 0 && y < probeVolume.parameters.resolutionY);
            Debug.Assert(z >= 0 && z < probeVolume.parameters.resolutionZ);

            Vector2Int probeOctahedralDepthIndex2D = ComputeProbeOctahedralDepthIndex2DFrom3D(probeVolume, x, y, z);

            // Z slices are packed horizontally.
            Vector2 probeOctahedralDepthScale2D = new Vector2(
                1.0f / (probeVolume.parameters.resolutionX * probeVolume.parameters.resolutionZ),
                1.0f / probeVolume.parameters.resolutionY
            );

            return new Vector4(probeOctahedralDepthScale2D.x, probeOctahedralDepthScale2D.y, probeOctahedralDepthIndex2D.x, probeOctahedralDepthIndex2D.y);
        }
#endif
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
