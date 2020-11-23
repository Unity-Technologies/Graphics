using System;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEditor.Experimental;
using Unity.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Rendering.Hybrid")]

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
        public float[] dataOctahedralDepth;

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
                payload.dataOctahedralDepth = new float[length * 8 * 8];
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
                Array.Copy(payloadSrc.dataOctahedralDepth, payloadDst.dataOctahedralDepth, length * 8 * 8);
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
    }

    [Serializable]
    internal struct ProbeVolumeArtistParameters
    {
        public bool drawProbes;
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

        internal bool dataUpdated = false;

        [SerializeField] internal ProbeVolumeAsset probeVolumeAsset = null;
        [SerializeField] internal ProbeVolumeArtistParameters parameters = new ProbeVolumeArtistParameters(Color.white);

        internal int GetID()
        {
            return GetInstanceID();
        }

        private void BakeKeyClear()
        {
            bakeKey = new ProbeVolumeSettingsKey
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

        protected void OnEnable()
        {
            Migrate();

#if UNITY_EDITOR
            OnValidate();
#endif

            ProbeVolumeManager.manager.RegisterVolume(this);

            // Signal update
            if (probeVolumeAsset)
                dataUpdated = true;

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            m_DebugMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            m_DebugMaterial = new Material(Shader.Find("HDRP/Lit"));
#endif
        }

        protected void OnDisable()
        {
            ProbeVolumeManager.manager.DeRegisterVolume(this);
#if UNITY_EDITOR
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
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(GetID(), null);
        }

        protected void OnValidate()
        {
            if (ShaderConfig.s_EnableProbeVolumes == 0)
                return;

            ProbeVolumeSettingsKey bakeKeyCurrent = ComputeProbeVolumeSettingsKeyFromProbeVolume(this);
            if (ProbeVolumeSettingsKeyEquals(ref bakeKey, ref bakeKeyCurrent) &&
                m_DebugProbeMatricesList != null) { return; }

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
                return;

            int numProbes = parameters.resolutionX * parameters.resolutionY * parameters.resolutionZ;

            var sh = new NativeArray<SphericalHarmonicsL2>(numProbes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var validity = new NativeArray<float>(numProbes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            // TODO: Currently, we need to always allocate and pass this octahedralDepth array into GetAdditionalBakedProbes().
            // In the future, we should add an API call for GetAdditionalBakedProbes() without octahedralDepth required.
            var octahedralDepth = new NativeArray<float>(numProbes * 8 * 8, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            if (UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(GetID(), sh, validity, octahedralDepth))
            {
                if (!probeVolumeAsset || GetID() != probeVolumeAsset.instanceID)
                    probeVolumeAsset = ProbeVolumeAsset.CreateAsset(GetID());

                probeVolumeAsset.instanceID = GetID();
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
                    octahedralDepth.CopyTo(probeVolumeAsset.payload.dataOctahedralDepth);
                }

                if (UnityEditor.Lightmapping.giWorkflowMode != UnityEditor.Lightmapping.GIWorkflowMode.Iterative)
                    UnityEditor.EditorUtility.SetDirty(probeVolumeAsset);

                UnityEditor.AssetDatabase.Refresh();

                dataUpdated = true;
            }

            sh.Dispose();
            validity.Dispose();
            octahedralDepth.Dispose();
        }

        internal void OnBakeCompleted()
        {
            if (!probeVolumeAsset)
                return;

            probeVolumeAsset.Dilate(parameters.backfaceTolerance, parameters.dilationIterations);
            dataUpdated = true;
        }

        private static ProbeVolumeSettingsKey ComputeProbeVolumeSettingsKeyFromProbeVolume(ProbeVolume probeVolume)
        {
            return new ProbeVolumeSettingsKey
            {
                id = probeVolume.GetID(),
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

            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(GetID(), positions);
        }

        private static bool ShouldDrawGizmos(ProbeVolume probeVolume)
        {
            if (ShaderConfig.s_EnableProbeVolumes == 0)
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

#endif
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
