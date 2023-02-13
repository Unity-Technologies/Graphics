using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Contains spherical harmonic coefficients used for lighting representation in the format
    /// expected by <c>DOTS_INSTANCING_ON</c> shaders.
    ///
    /// The size of the struct is padded to a power of two so arrays of such structs can be efficiently
    /// indexed in shaders.
    /// </summary>
    /// <seealso cref="SphericalHarmonicsL2"/>
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct SHCoefficients : IEquatable<SHCoefficients>
    {
        /// <summary>
        /// Contains the SH coefficients that correspond to the <c>unity_SHAr</c> shader property.
        /// </summary>
        public Vector4 SHAr;
        /// <summary>
        /// Contains the SH coefficients that correspond to the <c>unity_SHAg</c> shader property.
        /// </summary>
        public Vector4 SHAg;
        /// <summary>
        /// Contains the SH coefficients that correspond to the <c>unity_SHAb</c> shader property.
        /// </summary>
        public Vector4 SHAb;
        /// <summary>
        /// Contains the SH coefficients that correspond to the <c>unity_SHBr</c> shader property.
        /// </summary>
        public Vector4 SHBr;
        /// <summary>
        /// Contains the SH coefficients that correspond to the <c>unity_SHBg</c> shader property.
        /// </summary>
        public Vector4 SHBg;
        /// <summary>
        /// Contains the SH coefficients that correspond to the <c>unity_SHBb</c> shader property.
        /// </summary>
        public Vector4 SHBb;
        /// <summary>
        /// Contains the SH coefficients that correspond to the <c>unity_SHC</c> shader property.
        /// </summary>
        public Vector4 SHC;
        /// <summary>
        /// Contains the baked shadowing data that corresponds to the <c>unity_ProbesOcclusion</c> shader property.
        /// </summary>
        public Vector4 ProbesOcclusion;

        /// <summary>
        /// Construct an instance of <c>SHCoefficients</c> that represents the same spherical
        /// harmonic coefficients as the parameter.
        /// </summary>
        /// <param name="sh">The spherical harmonic coefficients to initialize with.</param>
        public SHCoefficients(SphericalHarmonicsL2 sh)
        {
            SHAr = GetSHA(sh, 0);
            SHAg = GetSHA(sh, 1);
            SHAb = GetSHA(sh, 2);

            SHBr = GetSHB(sh, 0);
            SHBg = GetSHB(sh, 1);
            SHBb = GetSHB(sh, 2);

            SHC = GetSHC(sh);

            ProbesOcclusion = Vector4.one;
        }
        
        /// <summary>
        /// Construct an instance of <c>SHCoefficients</c> that represents the same spherical
        /// harmonic coefficients as the parameter.
        /// </summary>
        /// <param name="sh">The spherical harmonic coefficients to initialize with.</param>
        /// <param name="probesOcclusion">The baked shadowing data to include with this set of spherical harmonic coefficients.</param>
        public SHCoefficients(SphericalHarmonicsL2 sh, Vector4 probesOcclusion)
            : this(sh)
        {
            ProbesOcclusion = probesOcclusion;
        }

        static Vector4 GetSHA(SphericalHarmonicsL2 sh, int i)
        {
            return new Vector4(sh[i, 3], sh[i, 1], sh[i, 2], sh[i, 0] - sh[i, 6]);
        }

        static Vector4 GetSHB(SphericalHarmonicsL2 sh, int i)
        {
            return new Vector4(sh[i, 4], sh[i, 5], sh[i, 6] * 3f, sh[i, 7]);
        }

        static Vector4 GetSHC(SphericalHarmonicsL2 sh)
        {
            return new Vector4(sh[0, 8], sh[1, 8], sh[2, 8], 1);
        }

        /// <summary>
        /// Equals implementation.
        /// </summary>
        /// <param name="other">Other SHCoefficients instance to comapre this against.</param>
        /// <returns>True if contents are equal, False otherwise.</returns>
        public bool Equals(SHCoefficients other)
        {
            return SHAr.Equals(other.SHAr) && SHAg.Equals(other.SHAg) && SHAb.Equals(other.SHAb) && SHBr.Equals(other.SHBr) && SHBg.Equals(other.SHBg) && SHBb.Equals(other.SHBb) && SHC.Equals(other.SHC) && ProbesOcclusion.Equals(other.ProbesOcclusion);
        }

        /// <summary>
        /// Equals implementation.
        /// </summary>
        /// <param name="obj">Other object to compare this object against</param>
        /// <returns>True if contents are equal, False otherwise.</returns>
        public override bool Equals(object obj)
        {
            return obj is SHCoefficients other && Equals(other);
        }

        /// <summary>
        /// GetHashCode implementation.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(SHAr, SHAg, SHAb, SHBr, SHBg, SHBb, SHC, ProbesOcclusion);
        }

        /// <summary>
        /// Equality operator implementation.
        /// </summary>
        /// <param name="left">Left operand of comparison</param>
        /// <param name="right">Right operand of comparison</param>
        /// <returns>True if contents are equal, False otherwise.</returns>
        public static bool operator ==(SHCoefficients left, SHCoefficients right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Not equals operator implementation.
        /// </summary>
        /// <param name="left">Left operand of comparison</param>
        /// <param name="right">Right operand of comparison</param>
        /// <returns>True if contents are not equal, False otherwise.</returns>
        public static bool operator !=(SHCoefficients left, SHCoefficients right)
        {
            return !left.Equals(right);
        }
    }
	
    /// <summary>
    /// Contains default values for built-in properties that the user is expected to manually
    /// provide for <c>DOTS_INSTANCING_ON</c> shaders. The struct layout matches the
    /// <c>unity_DOTSInstanceGlobalValues</c> constant buffer the shader expects the default
    /// values in.
    /// </summary>
    [Obsolete("BatchRendererGroupGlobals and associated cbuffer are now set automatically by Unity. Setting it manually is no longer necessary or supported.")]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct BatchRendererGroupGlobals : IEquatable<BatchRendererGroupGlobals>
    {
        /// <summary>
        /// The string name of the constant buffer <c>DOTS_INSTANCING_ON</c> shaders use
        /// to read default values for the built-in properties contained in this struct.
        /// </summary>
        public const string kGlobalsPropertyName = "unity_DOTSInstanceGlobalValues";
        /// <summary>
        /// The unique identifier for <see cref="kGlobalsPropertyName"/>, retrieved using
        /// <see cref="Shader.PropertyToID"/>.
        /// </summary>
        /// <seealso cref="Shader.PropertyToID"/>
        public static readonly int kGlobalsPropertyId = Shader.PropertyToID(kGlobalsPropertyName);

        /// <summary>
        /// The default value to use for the <c>unity_ProbesOcclusion</c> built-in shader property.
        /// </summary>
        public Vector4 ProbesOcclusion;
        /// <summary>
        /// The default value to use for the <c>unity_SpecCube0_HDR</c> built-in shader property.
        /// </summary>
        public Vector4 SpecCube0_HDR;
        /// <summary>
        /// The default value to use for the <c>unity_SpecCube1_HDR</c> built-in shader property.
        /// </summary>
        public Vector4 SpecCube1_HDR;
        /// <summary>
        /// The default values to use for the built-in spherical harmonics shader properties.
        /// </summary>
        /// <seealso cref="SHCoefficients"/>
        public SHCoefficients SHCoefficients;

        /// <summary>
        /// Construct a struct with default values based on the currently active reflection probe
        /// and ambient lighting settings.
        /// </summary>
        public static BatchRendererGroupGlobals Default
        {
            get
            {
                var globals = new BatchRendererGroupGlobals();
                globals.ProbesOcclusion = Vector4.one;
                globals.SpecCube0_HDR = ReflectionProbe.defaultTextureHDRDecodeValues;
                globals.SpecCube1_HDR = globals.SpecCube0_HDR;
                globals.SHCoefficients = new SHCoefficients(RenderSettings.ambientProbe);
                return globals;
            }
        }

        /// <summary>
        /// Equals implementation.
        /// </summary>
        /// <param name="other">Other BatchRendererGroupGlobals instance to comapre this against.</param>
        /// <returns>True if contents are equal, False otherwise.</returns>
        public bool Equals(BatchRendererGroupGlobals other)
        {
            return ProbesOcclusion.Equals(other.ProbesOcclusion) && SpecCube0_HDR.Equals(other.SpecCube0_HDR) && SpecCube1_HDR.Equals(other.SpecCube1_HDR) && SHCoefficients.Equals(other.SHCoefficients);
        }

        /// <summary>
        /// Equals implementation.
        /// </summary>
        /// <param name="obj">Other object to comapre this against.</param>
        /// <returns>True if contents are equal, False otherwise.</returns>
        public override bool Equals(object obj)
        {
            return obj is BatchRendererGroupGlobals other && Equals(other);
        }

        /// <summary>
        /// GetHashCode implementation.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(ProbesOcclusion, SpecCube0_HDR, SpecCube1_HDR, SHCoefficients);
        }

        /// <summary>
        /// Equality operator implementation.
        /// </summary>
        /// <param name="left">Left operand of comparison</param>
        /// <param name="right">Right operand of comparison</param>
        /// <returns>True if contents are equal, False otherwise.</returns>
        public static bool operator ==(BatchRendererGroupGlobals left, BatchRendererGroupGlobals right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Not equals operator implementation.
        /// </summary>
        /// <param name="left">Left operand of comparison</param>
        /// <param name="right">Right operand of comparison</param>
        /// <returns>True if contents are not equal, False otherwise.</returns>
        public static bool operator !=(BatchRendererGroupGlobals left, BatchRendererGroupGlobals right)
        {
            return !left.Equals(right);
        }
    }

}
