using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition.LTC
{
    /// <summary>
    /// Disney Diffuse Implementation
    /// Source from 2012 Burley, B. "Physically-Based Shading at Disney" Section 5.3
    /// (https://disney-animation.s3.amazonaws.com/library/s2012_pbs_disney_brdf_notes_v2.pdf)
    /// </summary>
    struct BRDF_Disney : IBRDF
    {
        public double Eval(ref Vector3 _tsView, ref Vector3 _tsLight, float _alpha, out double _pdf)
        {
            if (_tsView.z <= 0)
            {
                _pdf = 0;
                return 0;
            }

            _alpha = Mathf.Max(0.002f, _alpha);

            double NdotL = Math.Max(0, _tsLight.z);
            double NdotV = Math.Max(0, _tsView.z);
            double LdotV = Math.Max(0, Vector3.Dot(_tsLight, _tsView));

            double perceptualRoughness = Math.Sqrt(_alpha);

            // (2 * LdotH * LdotH) = 1 + LdotV
            // real fd90 = 0.5 + 2 * LdotH * LdotH * perceptualRoughness;
            double fd90 = 0.5 + (perceptualRoughness + perceptualRoughness * LdotV);

            // Two schlick fresnel term
            double lightScatter = F_Schlick(1.0, fd90, NdotL);
            double viewScatter = F_Schlick(1.0, fd90, NdotV);

            // Normalize the BRDF for polar view angles of up to (Pi/4).
            // We use the worst case of (roughness = albedo = 1), and, for each view angle,
            // integrate (brdf * cos(theta_light)) over all light directions.
            // The resulting value is for (theta_view = 0), which is actually a little bit larger
            // than the value of the integral for (theta_view = Pi/4).
            // Hopefully, the compiler folds the constant together with (1/Pi).
            double res = lightScatter * viewScatter / Math.PI;
            res /= 1.03571;

            // Remember we must include the N.L term!
            res *= NdotL;

            // Cosine-weighted hemisphere sampling
            _pdf = NdotL / Math.PI;

            return res;
        }

        public void GetSamplingDirection(ref Vector3 _tsView, float _alpha, float _U1, float _U2, ref Vector3 _direction)
        {
            // Performs uniform sampling of the unit disk.
            // Ref: PBRT v3, p. 777.
            float r = Mathf.Sqrt(_U1);
            float phi = 2.0f * Mathf.PI * _U2;

            // Performs cosine-weighted sampling of the hemisphere.
            // Ref: PBRT v3, p. 780.
            _direction.x = r * Mathf.Cos(phi);
            _direction.y = r * Mathf.Sin(phi);
            _direction.z = Mathf.Sqrt(1 - _U1);      // Project the point from the disk onto the hemisphere.
        }

        double F_Schlick(double _F0, double _F90, double _cosTheta)
        {
            double x = 1.0 - _cosTheta;
            double x2 = x * x;
            double x5 = x * x2 * x2;
            return (_F90 - _F0) * x5 + _F0;                // sub mul mul mul sub mad
        }

        public LTCLightingModel GetLightingModel()
        {
            return LTCLightingModel.DisneyDiffuse;
        }
    }
}
