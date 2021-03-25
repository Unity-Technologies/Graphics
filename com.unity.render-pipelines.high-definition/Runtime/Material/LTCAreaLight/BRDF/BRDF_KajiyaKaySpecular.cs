using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition.LTC
{
    /// <summary>
    /// Disney Diffuse Implementation
    /// Source from 2012 Burley, B. "Physically-Based Shading at Disney" Section 5.3
    /// (https://disney-animation.s3.amazonaws.com/library/s2012_pbs_disney_brdf_notes_v2.pdf)
    /// </summary>
    struct BRDF_KajiyaKaySpecular : IBRDF
    {
        public double Eval(ref Vector3 _tsView, ref Vector3 _tsLight, float _alpha, out double _pdf)
        {
            if (_tsView.z <= 0)
            {
                _pdf = 0;
                return 0;
            }

            _alpha = Mathf.Max(0.002f, _alpha);
            double perceptualRoughness = Math.Sqrt(_alpha);

            Vector3 T = Vector3.right;
            Vector3 N = Vector3.forward;

            double NdotV = Math.Max(0, _tsView.z);
            double NdotL = Math.Max(0, _tsLight.z);

            double LdotV = Math.Max(0, Vector3.Dot(_tsLight, _tsView));
            Vector3 H = Vector3.Normalize(_tsLight + _tsView);
            double LdotH = Math.Max(0, Vector3.Dot(_tsLight, H));

            Vector3 t1 = ShiftTangent(T, N, 0.0f);
            Vector3 t2 = ShiftTangent(T, N, 0.0f);

            double specularExponent = RoughnessToBlinnPhongSpecularExponent(_alpha);

            // Balancing energy between lobes, as well as between diffuse and specular is left to artists.
            double hairSpec1 = D_KajiyaKay(t1, H, specularExponent);
            double hairSpec2 = D_KajiyaKay(t2, H, specularExponent);

            double fd90 = 0.5 + (perceptualRoughness + perceptualRoughness * LdotV);
            double F = F_Schlick(1.0, fd90, LdotH);

            // G = NdotL * NdotV.
            double res = 0.25 * F * (hairSpec1 + hairSpec2) * NdotL * Math.Min(Math.Max(NdotV * double.MaxValue, 0.0), 1.0);

            // Uniform Sampling
            _pdf = 0.5 / Math.PI;

            return res;
        }

        // Uniform Sampling
        public void GetSamplingDirection(ref Vector3 _tsView, float _alpha, float _U1, float _U2, ref Vector3 _direction)
        {
            float phi = 2.0f * Mathf.PI * _U1;
            float cosTheta = 1.0f - _U2;
            float sinTheta = Mathf.Sqrt(1 - cosTheta * cosTheta);
            _direction = new Vector3(sinTheta * Mathf.Cos(phi), sinTheta * Mathf.Sin(phi), cosTheta);
        }

        double RoughnessToBlinnPhongSpecularExponent(double roughness)
        {
            // 1e-4 and 3e3 are the lowest/highest values that do not cause numerical instabilities with the fitting tool
            return Math.Min(Math.Max(2.0 / (roughness * roughness) - 2.0, 1e-4), 3e3);
        }

        double F_Schlick(double _F0, double _F90, double _cosTheta)
        {
            double x = 1.0f - _cosTheta;
            double x2 = x * x;
            double x5 = x * x2 * x2;
            return (_F90 - _F0) * x5 + _F0;                // sub mul mul mul sub mad
        }

        //http://web.engr.oregonstate.edu/~mjb/cs519/Projects/Papers/HairRendering.pdf
        Vector3 ShiftTangent(Vector3 T, Vector3 N, float shift)
        {
            return Vector3.Normalize(T + N * shift);
        }

        double PositivePow(double value, double power)
        {
            return Math.Pow(Math.Max(Math.Abs(value), 1.192092896e-07), power);
        }

        double D_KajiyaKay(Vector3 T, Vector3 H, double specularExponent)
        {
            float TdotH = Vector3.Dot(T, H);
            float sinTHSq = Mathf.Clamp(1.0f - TdotH * TdotH, 0.0f, 1.0f);

            float dirAttn = Mathf.Clamp(TdotH + 1.0f, 0.0f, 1.0f); // Evgenii: this seems like a hack? Do we really need this?

            // Note: Kajiya-Kay is not energy conserving.
            // We attempt at least some energy conservation by approximately normalizing Blinn-Phong NDF.
            // We use the formulation with the NdotL.
            // See http://www.thetenthplanet.de/archives/255.
            double n = specularExponent;
            double norm = (n + 2) / (2.0 * Math.PI);

            return dirAttn * norm * PositivePow(sinTHSq, 0.5 * n);
        }

        public LTCLightingModel GetLightingModel()
        {
            return LTCLightingModel.KajiyaKaySpecular;
        }
    }
}
