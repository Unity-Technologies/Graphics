using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Rendering.Hybrid")]

namespace UnityEngine.Rendering.HighDefinition
{
    internal enum MaskSpacingMode
    {
        Density = 0,
        Resolution
    };

    [GenerateHLSL]
    internal enum MaskVolumeBlendMode
    {
        Normal = 0,
        Additive,
        Subtractive
    }

    // TODO: This description is from Probe Volume. Update to reflect the Mask Volume data storage.
    // Container structure for managing a mask volume's payload.
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
    // The data in the SH mask sample passed here is expected to already be normalized with kNormalizationConstants.
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
    internal struct MaskVolumePayload
    {
        public byte[] dataSHL0;
        // public byte[] dataSHL2;
        // public byte[] dataValidity;

        public static readonly MaskVolumePayload zero = new MaskVolumePayload
        {
            dataSHL0 = null,
            // dataSHL2 = null,
            // dataValidity = null
        };

        public static int GetDataSHL0Stride()
        {
            return 4;
        }

        /* public static int GetDataSHL1Stride()
        {
            return 4 * 3 - GetDataSHL0Stride();
        }

        public static int GetDataSHL2Stride()
        {
            return 9 * 3 - GetDataSHL0Stride();
        } */

        public static bool IsEmpty(ref MaskVolumePayload payload)
        {
            return payload.dataSHL0 == null || payload.dataSHL0.Length == 0;
        }

        public static int GetLength(ref MaskVolumePayload payload)
        {
            // No need to explicitly store mask length - dataValidity is one value per mask, so we can just query the length here.
            return payload.dataSHL0.Length / GetDataSHL0Stride();
        }

        public static void Allocate(ref MaskVolumePayload payload, int length)
        {
            payload.dataSHL0 = new byte[length * GetDataSHL0Stride()];
            // payload.dataSHL2 = new byte[length * GetDataSHL2Stride()];

            // TODO: Only allocate dataValidity if those payload slices are in use.
            // payload.dataValidity = new byte[length];
        }

        public static void Ensure(ref MaskVolumePayload payload, int length)
        {
            if (payload.dataSHL0 == null
                || payload.dataSHL0.Length != (length * GetDataSHL0Stride()))
            {
                Dispose(ref payload);
                Allocate(ref payload, length);
            }
        }

        public static void Dispose(ref MaskVolumePayload payload)
        {
            payload.dataSHL0 = null;
            // payload.dataSHL2 = null;
            // payload.dataValidity = null;
        }

        public static void Copy(ref MaskVolumePayload payloadSrc, ref MaskVolumePayload payloadDst)
        {
            Debug.Assert(GetLength(ref payloadSrc) == GetLength(ref payloadDst));

            Copy(ref payloadSrc, ref payloadDst, GetLength(ref payloadSrc));
        }

        public static void Copy(ref MaskVolumePayload payloadSrc, ref MaskVolumePayload payloadDst, int length)
        {
            Array.Copy(payloadSrc.dataSHL0, payloadDst.dataSHL0, length * GetDataSHL0Stride());
            // Array.Copy(payloadSrc.dataSHL2, payloadDst.dataSHL2, length * GetDataSHL2Stride());

            // Array.Copy(payloadSrc.dataValidity, payloadDst.dataValidity, length);
        }
        
        public static float FromUNormByte(byte value) => value / 255f;
        public static byte ToUNormByte(float value) => (byte)(Mathf.Clamp01(value) * 255f);

        public static void Resample(ref MaskVolumePayload oldPayload,
            int index0, float weight0,
            int index1, float weight1,
            int index2, float weight2,
            int index3, float weight3,
            int index4, float weight4,
            int index5, float weight5,
            int index6, float weight6,
            int index7, float weight7,
            ref MaskVolumePayload newPayload,
            int targetIndex)
        {
            var shl0Stride = GetDataSHL0Stride();
            index0 *= shl0Stride;
            index1 *= shl0Stride;
            index2 *= shl0Stride;
            index3 *= shl0Stride;
            index4 *= shl0Stride;
            index5 *= shl0Stride;
            index6 *= shl0Stride;
            index7 *= shl0Stride;
            targetIndex *= shl0Stride;
            for (int i = 0; i < shl0Stride; i++)
            {
                var value = oldPayload.dataSHL0[index0 + i] * weight0;
                value += oldPayload.dataSHL0[index1 + i] * weight1;
                value += oldPayload.dataSHL0[index2 + i] * weight2;
                value += oldPayload.dataSHL0[index3 + i] * weight3;
                value += oldPayload.dataSHL0[index4 + i] * weight4;
                value += oldPayload.dataSHL0[index5 + i] * weight5;
                value += oldPayload.dataSHL0[index6 + i] * weight6;
                value += oldPayload.dataSHL0[index7 + i] * weight7;
                newPayload.dataSHL0[targetIndex + i] = (byte)value;
            }
        }
        
        /*
        public static void GetSphericalHarmonicsL1FromIndex(ref SphericalHarmonicsL1 sh, ref MaskVolumePayload payload, int indexMask)
        {
            int strideSHL01 = GetDataSHL01Stride();
            int indexDataBaseSHL01 = indexMask * strideSHL01;
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

        public static void GetSphericalHarmonicsL2FromIndex(ref SphericalHarmonicsL2 sh, ref MaskVolumePayload payload, int indexMask)
        {
            int strideSHL01 = GetDataSHL01Stride();
            int indexDataBaseSHL01 = indexMask * strideSHL01;
            int indexDataEndSHL01 = indexDataBaseSHL01 + strideSHL01;

            Debug.Assert(payload.dataSHL01 != null);
            Debug.Assert(payload.dataSHL01.Length >= indexDataEndSHL01);

            int strideSHL2 = GetDataSHL2Stride();
            int indexDataBaseSHL2 = indexMask * strideSHL2;
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

        public static void SetSphericalHarmonicsL1FromIndex(ref MaskVolumePayload payload, SphericalHarmonicsL1 sh, int indexMask)
        {
            int strideSHL01 = GetDataSHL01Stride();
            int indexDataBaseSHL01 = indexMask * strideSHL01;
            int indexDataEndSHL01 = indexDataBaseSHL01 + strideSHL01;

            Debug.Assert(payload.dataSHL01 != null);
            Debug.Assert(payload.dataSHL01.Length >= indexDataEndSHL01);

            int strideSHL2 = GetDataSHL2Stride();
            int indexDataBaseSHL2 = indexMask * strideSHL2;
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

        public static void SetSphericalHarmonicsL2FromIndex(ref MaskVolumePayload payload, SphericalHarmonicsL2 sh, int indexMask)
        {
            int strideSHL01 = GetDataSHL01Stride();
            int indexDataBaseSHL01 = indexMask * strideSHL01;
            int indexDataEndSHL01 = indexDataBaseSHL01 + strideSHL01;

            Debug.Assert(payload.dataSHL01 != null);
            Debug.Assert(payload.dataSHL01.Length >= indexDataEndSHL01);

            int strideSHL2 = GetDataSHL2Stride();
            int indexDataBaseSHL2 = indexMask * strideSHL2;
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
        */
    }

#if UNITY_EDITOR
    // Rather than hashing all the inputs that define a Mask Volume's bake state into a 128-bit int (16-bytes),
    // we simply store the raw state values (56-bytes)
    // While this is 3.5x more memory, it's still fairly low, and avoids the runtime cost of string appending garbage creation.
    // It also means we can never ever have hash collision issues (due to precision loss in string construction, or from hashing),
    // which means we always detect changes correctly.
    [Serializable]
    internal struct MaskVolumeSettingsKey
    {
        public int id;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 size;
        public int resolutionX;
        public int resolutionY;
        public int resolutionZ;
        // public float backfaceTolerance;
        // public int dilationIterations;
    }
#endif
    
    [Serializable]
    internal struct MaskVolumeArtistParameters
    {
        public bool drawGizmos;
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

        public MaskSpacingMode maskSpacingMode;

        public float densityX;
        public float densityY;
        public float densityZ;

        public int resolutionX;
        public int resolutionY;
        public int resolutionZ;

        public MaskVolumeBlendMode blendMode;
        public float weight;
        public float normalBiasWS;

        // public float backfaceTolerance;
        // public int dilationIterations;

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

        public MaskVolumeArtistParameters(Color debugColor)
        {
            this.debugColor = debugColor;
            this.drawGizmos = false;
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
            this.maskSpacingMode = MaskSpacingMode.Density;
            this.resolutionX = 4;
            this.resolutionY = 4;
            this.resolutionZ = 4;
            this.densityX = (float)this.resolutionX / this.size.x;
            this.densityY = (float)this.resolutionY / this.size.y;
            this.densityZ = (float)this.resolutionZ / this.size.z;
            this.blendMode = MaskVolumeBlendMode.Normal;
            this.weight = 1;
            this.normalBiasWS = 0.0f;
            // this.dilationIterations = 2;
            // this.backfaceTolerance = 0.25f;
            this.lightLayers = LightLayerEnum.LightLayerDefault;
        }

        internal void Constrain()
        {
            this.distanceFadeStart = Mathf.Max(0, this.distanceFadeStart);
            this.distanceFadeEnd = Mathf.Max(this.distanceFadeStart, this.distanceFadeEnd);

            switch (this.maskSpacingMode)
            {
                case MaskSpacingMode.Density:
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

                case MaskSpacingMode.Resolution:
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
                    Debug.Assert(false, "Error: MaskVolume: Encountered unsupported Mask Spacing Mode: " + this.maskSpacingMode);
                    break;
                }
            }
        }
    } // class MaskVolumeArtistParameters

    [ExecuteAlways]
    [AddComponentMenu("Rendering/Mask Volume")]
    internal class MaskVolume : MonoBehaviour
    {
        static List<MaskVolume> s_Volumes = null;

        internal static List<MaskVolume> GetVolumes()
        {
            if (s_Volumes == null)
                s_Volumes = new List<MaskVolume>();
            return s_Volumes;
        }
        
        static void RegisterVolume(MaskVolume volume)
        {
            var volumes = GetVolumes();
            if (volumes.Contains(volume))
                return;

            volumes.Add(volume);
        }
        
        static void DeRegisterVolume(MaskVolume volume)
        {
            var volumes = GetVolumes();
            var volumeIndex = volumes.IndexOf(volume);
            if (volumeIndex == -1)
                return;

            volumes.RemoveAt(volumeIndex);
            ReleaseFromAtlas(volume);
        }

        static void ReleaseFromAtlas(MaskVolume volume)
        {
            if (RenderPipelineManager.currentPipeline is HDRenderPipeline hdrp)
                hdrp.ReleaseMaskVolumeFromAtlas(volume);
        }
        
#if UNITY_EDITOR
        // Debugging code
        private Material m_DebugMaterial = null;
        // private Mesh m_DebugMesh = null;
        // private List<Matrix4x4[]> m_DebugMaskMatricesList;
        private List<Mesh> m_DebugMaskPointMeshList;
        
        private MaskVolumeSettingsKey bakeKey = new MaskVolumeSettingsKey
        {
            id = 0,
            position = Vector3.zero,
            rotation = Quaternion.identity,
            size = Vector3.zero,
            resolutionX = 0,
            resolutionY = 0,
            resolutionZ = 0,
            // backfaceTolerance = 0.0f,
            // dilationIterations = 0
        };
#endif
        internal bool dataUpdated = false;

        [SerializeField] internal MaskVolumeAsset maskVolumeAsset = null;
        [SerializeField] internal MaskVolumeArtistParameters parameters = new MaskVolumeArtistParameters(Color.white);

        internal int GetID()
        {
            return GetInstanceID();
        }

        internal MaskVolumeEngineData ConvertToEngineData()
        {
            MaskVolumeEngineData data = new MaskVolumeEngineData();

            data.weight = parameters.weight;
            data.normalBiasWS = parameters.normalBiasWS;

            data.debugColor.x = parameters.debugColor.r;
            data.debugColor.y = parameters.debugColor.g;
            data.debugColor.z = parameters.debugColor.b;

            // Clamp to avoid NaNs.
            Vector3 positiveFade = Vector3.Max(parameters.positiveFade, new Vector3(1e-5f, 1e-5f, 1e-5f));
            Vector3 negativeFade = Vector3.Max(parameters.negativeFade, new Vector3(1e-5f, 1e-5f, 1e-5f));

            data.rcpPosFaceFade.x = Mathf.Min(1.0f / positiveFade.x, float.MaxValue);
            data.rcpPosFaceFade.y = Mathf.Min(1.0f / positiveFade.y, float.MaxValue);
            data.rcpPosFaceFade.z = Mathf.Min(1.0f / positiveFade.z, float.MaxValue);

            data.rcpNegFaceFade.y = Mathf.Min(1.0f / negativeFade.y, float.MaxValue);
            data.rcpNegFaceFade.x = Mathf.Min(1.0f / negativeFade.x, float.MaxValue);
            data.rcpNegFaceFade.z = Mathf.Min(1.0f / negativeFade.z, float.MaxValue);

            data.blendMode = (int)parameters.blendMode;

            float distFadeLen = Mathf.Max(parameters.distanceFadeEnd - parameters.distanceFadeStart, 0.00001526f);
            data.rcpDistFadeLen = 1.0f / distFadeLen;
            data.endTimesRcpDistFadeLen = parameters.distanceFadeEnd * data.rcpDistFadeLen;

            data.scale = parameters.scale;
            data.bias = parameters.bias;

            data.resolution = new Vector3(maskVolumeAsset.resolutionX, maskVolumeAsset.resolutionY, maskVolumeAsset.resolutionZ);
            data.resolutionInverse = new Vector3(1.0f / maskVolumeAsset.resolutionX, 1.0f / maskVolumeAsset.resolutionY, 1.0f / maskVolumeAsset.resolutionZ);

            data.lightLayers = (uint)parameters.lightLayers;

            return data;
        }
        
        internal MaskVolumePayload GetPayload()
        {
            dataUpdated = false;

            if (!maskVolumeAsset) { return MaskVolumePayload.zero; }

            return maskVolumeAsset.payload;
        }

        bool CheckMigrationRequirement()
        {
            return maskVolumeAsset != null && maskVolumeAsset.Version != (int)MaskVolumeAsset.AssetVersion.Current;
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
            switch ((MaskVolumeAsset.AssetVersion)maskVolumeAsset.Version)
            {
                case MaskVolumeAsset.AssetVersion.First:
                    // No migration required.
                    break;
            }
        }

        protected void OnEnable()
        {
            // Migrate();

            RegisterVolume(this);

            // Signal update
            if (maskVolumeAsset)
                dataUpdated = true;

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            // m_DebugMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            m_DebugMaterial = new Material(Shader.Find("HDRP/Lit"));
#endif
        }

        protected void OnDisable()
        {
            DeRegisterVolume(this);
        }

        internal bool IsAssetCompatible()
        {
            return maskVolumeAsset;
        }

        internal bool IsAssetMatchingResolution()
        {
            if (maskVolumeAsset)
            {
                return parameters.resolutionX == maskVolumeAsset.resolutionX &&
                       parameters.resolutionY == maskVolumeAsset.resolutionY &&
                       parameters.resolutionZ == maskVolumeAsset.resolutionZ;
            }
            return false;
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            parameters.Constrain();
        }
        
        protected void UpdateDebugMeshes()
        {
            MaskVolumeSettingsKey bakeKeyCurrent = ComputeMaskVolumeSettingsKeyFromMaskVolume(this);
            if (MaskVolumeSettingsKeyEquals(ref bakeKey, ref bakeKeyCurrent) &&
                m_DebugMaskPointMeshList != null) { return; }

            bakeKey = bakeKeyCurrent;

            if (maskVolumeAsset)
            {
                dataUpdated = true;
            }

            SetupMaskPositions();
        }

        internal void CreateAsset()
        {
            maskVolumeAsset = MaskVolumeAsset.CreateAsset(GetID());

            maskVolumeAsset.instanceID = GetID();
            maskVolumeAsset.resolutionX = parameters.resolutionX;
            maskVolumeAsset.resolutionY = parameters.resolutionY;
            maskVolumeAsset.resolutionZ = parameters.resolutionZ;

            int numMasks = parameters.resolutionX * parameters.resolutionY * parameters.resolutionZ;
            MaskVolumePayload.Allocate(ref maskVolumeAsset.payload, numMasks);

            UnityEditor.EditorUtility.SetDirty(maskVolumeAsset);

            dataUpdated = true;
        }

        internal void ResampleAsset()
        {
            MaskVolumePayload oldPayload = maskVolumeAsset.payload;
            int oldResolutionX = maskVolumeAsset.resolutionX;
            int oldResolutionY = maskVolumeAsset.resolutionY;
            int oldResolutionZ = maskVolumeAsset.resolutionZ;
            
            int numMasks = parameters.resolutionX * parameters.resolutionY * parameters.resolutionZ;
            MaskVolumePayload newPayload = default;
            MaskVolumePayload.Allocate(ref newPayload, numMasks);

            if (oldResolutionX > 0 && oldResolutionY > 0 && oldResolutionZ > 0 && !MaskVolumePayload.IsEmpty(ref oldPayload))
            {
                for (int z = 0; z < parameters.resolutionZ; z++)
                {
                    CalculateResamplingWeights(oldResolutionZ, parameters.resolutionZ, z, out int oldZLow, out int oldZHigh, out float oldZLowWeight, out float oldZHighWeight);

                    for (int y = 0; y < parameters.resolutionY; y++)
                    {
                        CalculateResamplingWeights(oldResolutionY, parameters.resolutionY, y, out int oldYLow, out int oldYHigh, out float oldYLowWeight, out float oldYHighWeight);

                        for (int x = 0; x < parameters.resolutionX; x++)
                        {
                            CalculateResamplingWeights(oldResolutionX, parameters.resolutionX, x, out int oldXLow, out int oldXHigh, out float oldXLowWeight, out float oldXHighWeight);

                            MaskVolumePayload.Resample(ref oldPayload,
                                PayloadIndex(oldResolutionX, oldResolutionY, oldXLow, oldYLow, oldZLow), oldXLowWeight * oldYLowWeight * oldZLowWeight,
                                PayloadIndex(oldResolutionX, oldResolutionY, oldXHigh, oldYLow, oldZLow), oldXHighWeight * oldYLowWeight * oldZLowWeight,
                                PayloadIndex(oldResolutionX, oldResolutionY, oldXLow, oldYHigh, oldZLow), oldXLowWeight * oldYHighWeight * oldZLowWeight,
                                PayloadIndex(oldResolutionX, oldResolutionY, oldXHigh, oldYHigh, oldZLow), oldXHighWeight * oldYHighWeight * oldZLowWeight,
                                PayloadIndex(oldResolutionX, oldResolutionY, oldXLow, oldYLow, oldZHigh), oldXLowWeight * oldYLowWeight * oldZHighWeight,
                                PayloadIndex(oldResolutionX, oldResolutionY, oldXHigh, oldYLow, oldZHigh), oldXHighWeight * oldYLowWeight * oldZHighWeight,
                                PayloadIndex(oldResolutionX, oldResolutionY, oldXLow, oldYHigh, oldZHigh), oldXLowWeight * oldYHighWeight * oldZHighWeight,
                                PayloadIndex(oldResolutionX, oldResolutionY, oldXHigh, oldYHigh, oldZHigh), oldXHighWeight * oldYHighWeight * oldZHighWeight,
                                ref newPayload,
                                PayloadIndex(parameters.resolutionX, parameters.resolutionY, x, y, z));
                        }
                    }
                }

                ReleaseFromAtlas(this);
            }

            maskVolumeAsset.instanceID = GetID();
            maskVolumeAsset.resolutionX = parameters.resolutionX;
            maskVolumeAsset.resolutionY = parameters.resolutionY;
            maskVolumeAsset.resolutionZ = parameters.resolutionZ;
           
            maskVolumeAsset.payload = newPayload;
            MaskVolumePayload.Dispose(ref oldPayload);
            
            UnityEditor.EditorUtility.SetDirty(maskVolumeAsset);

            dataUpdated = true;
        }

        static void CalculateResamplingWeights(int oldResolution, int newResolution, int targetTexel, out int oldLowTexel, out int oldHighTexel, out float oldLowWeight, out float oldHighWeight)
        {
            float sampleCoord = (targetTexel + 0.5f) / newResolution * oldResolution - 0.5f;

            if (oldResolution > 1)
            {
                oldLowTexel = Mathf.Clamp(Mathf.FloorToInt(sampleCoord), 0, oldResolution - 2);
                oldHighTexel = oldLowTexel + 1;
            }
            else
            {
                oldLowTexel = 0;
                oldHighTexel = 0;
            }

            oldHighWeight = sampleCoord - oldLowTexel;
            oldLowWeight = 1f - oldHighWeight;
            
            if (oldHighTexel == oldResolution)
                Debug.LogError(oldHighTexel);
        }

        static int PayloadIndex(int resolutionX, int resolutionY, int x, int y, int z)
        {
            return (z * resolutionY + y) * resolutionX + x;
        }

        private static MaskVolumeSettingsKey ComputeMaskVolumeSettingsKeyFromMaskVolume(MaskVolume maskVolume)
        {
            return new MaskVolumeSettingsKey
            {
                id = maskVolume.GetID(),
                position = maskVolume.transform.position,
                rotation = maskVolume.transform.rotation,
                size = maskVolume.parameters.size,
                resolutionX = maskVolume.parameters.resolutionX,
                resolutionY = maskVolume.parameters.resolutionY,
                resolutionZ = maskVolume.parameters.resolutionZ,
                // backfaceTolerance = maskVolume.parameters.backfaceTolerance,
                // dilationIterations = maskVolume.parameters.dilationIterations
            };
        }

        private static bool MaskVolumeSettingsKeyEquals(ref MaskVolumeSettingsKey a, ref MaskVolumeSettingsKey b)
        {
            return (a.id == b.id)
                && (a.position == b.position)
                && (a.rotation == b.rotation)
                && (a.size == b.size)
                && (a.resolutionX == b.resolutionX)
                && (a.resolutionY == b.resolutionY)
                && (a.resolutionZ == b.resolutionZ);
                // && (a.backfaceTolerance == b.backfaceTolerance)
                // && (a.dilationIterations == b.dilationIterations);
        }

        private void SetupMaskPositions()
        {
            if (!this.gameObject.activeInHierarchy)
                return;

            float debugMaskSize = Gizmos.probeSize;

            int maskCount = parameters.resolutionX * parameters.resolutionY * parameters.resolutionZ;
            Vector3[] positions = new Vector3[maskCount];

            OrientedBBox obb = new OrientedBBox(Matrix4x4.TRS(this.transform.position, this.transform.rotation, parameters.size));

            Vector3 maskSteps = new Vector3(parameters.size.x / (float)parameters.resolutionX, parameters.size.y / (float)parameters.resolutionY, parameters.size.z / (float)parameters.resolutionZ);

            // TODO: Determine why we need to negate obb.forward but not other basis vectors in order to make positions start at the {left, lower, back} corner
            // and end at the {right, top, front} corner (which our atlasing code assumes).
            Vector3 maskStartPosition = obb.center
                - obb.right   * (parameters.size.x - maskSteps.x) * 0.5f
                - obb.up      * (parameters.size.y - maskSteps.y) * 0.5f
                + obb.forward * (parameters.size.z - maskSteps.z) * 0.5f;

            Quaternion rotation = Quaternion.identity;
            Vector3 scale = new Vector3(debugMaskSize, debugMaskSize, debugMaskSize);

            // Debugging objects start here
            int maxBatchSize = 1023;
            int masksInCurrentBatch = System.Math.Min(maxBatchSize, maskCount);
            int indexInCurrentBatch = 0;

            // Everything around cached matrices for the mask spheres
            // m_DebugMaskMatricesList = new List<Matrix4x4[]>();
            // Matrix4x4[] currentmaskMatrices = new Matrix4x4[masksInCurrentBatch];
            // int[] indices = new int[masksInCurrentBatch];

            // Everything around point meshes for non-selected MaskVolumes
            m_DebugMaskPointMeshList = new List<Mesh>();
            int[] currentMaskDebugIndices = new int[masksInCurrentBatch];
            Vector3[] currentMaskDebugPositions = new Vector3[masksInCurrentBatch];

            int processedMasks = 0;

            for (int z = 0; z < parameters.resolutionZ; ++z)
            {
                for (int y = 0; y < parameters.resolutionY; ++y)
                {
                    for (int x = 0; x < parameters.resolutionX; ++x)
                    {
                        Vector3 position = maskStartPosition + (maskSteps.x * x * obb.right) + (maskSteps.y * y * obb.up) + (maskSteps.z * z * -obb.forward);
                        positions[processedMasks] = position;

                        currentMaskDebugIndices[indexInCurrentBatch] = indexInCurrentBatch;
                        currentMaskDebugPositions[indexInCurrentBatch] = position;

                        Matrix4x4 matrix = new Matrix4x4();
                        matrix.SetTRS(position, rotation, scale);
                        // currentmaskMatrices[indexInCurrentBatch] = matrix;

                        indexInCurrentBatch++;
                        processedMasks++;

                        int masksLeft = maskCount - processedMasks;

                        if (indexInCurrentBatch >= 1023 || masksLeft == 0)
                        {
                            Mesh currentMaskDebugMesh = new Mesh();
                            currentMaskDebugMesh.SetVertices(currentMaskDebugPositions);
                            currentMaskDebugMesh.SetIndices(currentMaskDebugIndices, MeshTopology.Points, 0);

                            m_DebugMaskPointMeshList.Add(currentMaskDebugMesh);
                            // m_DebugMaskMatricesList.Add(currentmaskMatrices);

                            // More sets follow, reallocate
                            if (masksLeft > 0)
                            {
                                masksInCurrentBatch = System.Math.Min(maxBatchSize, masksLeft);

                                currentMaskDebugPositions = new Vector3[masksInCurrentBatch];
                                currentMaskDebugIndices = new int[masksInCurrentBatch];
                                // currentmaskMatrices = new Matrix4x4[masksInCurrentBatch];

                                indexInCurrentBatch = 0;
                            }
                        }
                    }
                }
            }
        }

        private static bool ShouldDrawGizmos(MaskVolume maskVolume)
        {
            UnityEditor.SceneView sceneView = UnityEditor.SceneView.currentDrawingSceneView;

            if (sceneView == null)
                sceneView = UnityEditor.SceneView.lastActiveSceneView;

            if (sceneView != null && !sceneView.drawGizmos)
                return false;

            if (!maskVolume.enabled)
                return false;

            return maskVolume.parameters.drawGizmos;
        }

        [UnityEditor.DrawGizmo(UnityEditor.GizmoType.InSelectionHierarchy | UnityEditor.GizmoType.NotInSelectionHierarchy)]
        private static void DrawMasks(MaskVolume maskVolume, UnityEditor.GizmoType gizmoType)
        {
            if (!ShouldDrawGizmos(maskVolume))
                return;

            maskVolume.UpdateDebugMeshes();

            var pointMeshList = maskVolume.m_DebugMaskPointMeshList;

            maskVolume.m_DebugMaterial.SetPass(8);
            foreach (Mesh debugMesh in pointMeshList)
                Graphics.DrawMeshNow(debugMesh, Matrix4x4.identity);
        }

        /* internal void DrawSelectedMasks()
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
            LightProbeUsage lightMaskUsage = LightProbeUsage.Off;
            LightProbeProxyVolume lightMaskProxyVolume = null;

            foreach (Matrix4x4[] matrices in m_DebugMaskMatricesList)
                Graphics.DrawMeshInstanced(mesh, submeshIndex, material, matrices, matrices.Length, properties, castShadows, receiveShadows, layer, emptyCamera, lightMaskUsage, lightMaskProxyVolume);
        } */
#endif
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
