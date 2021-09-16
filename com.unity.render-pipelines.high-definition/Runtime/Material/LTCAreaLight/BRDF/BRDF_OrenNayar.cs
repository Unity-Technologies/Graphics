using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition.LTC
{
    /// <summary>
    /// Oren-Nayar Implementation
    /// Source from 1994 Oren, M. Nayar, S. K. "Generalization of Lambert's Reflectance Model"
    /// </summary>
    struct BRDF_OrenNayar : IBRDF
    {
        public double Eval(ref Vector3 _tsView, ref Vector3 _tsLight, float _alpha, out double _pdf)
        {
            if (_tsView.z <= 0)
            {
                _pdf = 0;
                return 0;
            }

            float sigma = Mathf.Max(0.002f, 0.5f * Mathf.PI * _alpha);    // Standard deviation is a [0,PI/2] angle

            double NdotL = Math.Max(0, _tsLight.z);
            double NdotV = Math.Max(0, _tsView.z);

            double gamma = (_tsView.x * _tsLight.x + _tsView.y * _tsLight.y)
                / Math.Max(1e-20, Math.Sqrt(1.0 - NdotV * NdotV) * Math.Sqrt(1.0 - NdotL * NdotL));

            double rough_sq = sigma * sigma;
            double A = 1.0 - 0.5 * (rough_sq / (rough_sq + 0.57));   // You can replace 0.33 by 0.57 to simulate the missing inter-reflection term, as specified in footnote of page 22 of the 1992 paper
            double B = 0.45 * (rough_sq / (rough_sq + 0.09));

            // Original formulation
            //            float angle_vn = acos( NdotV );
            //            float angle_ln = acos( NdotL );
            //            float alpha = max( angle_vn, angle_ln );
            //            float beta  = min( angle_vn, angle_ln );
            //            float C = sin(alpha) * tan(beta);

            // Optimized formulation (without tangents, arccos or sines)
            double cos_alpha = NdotV < NdotL ? NdotV : NdotL;
            double cos_beta = NdotV < NdotL ? NdotL : NdotV;
            double sin_alpha = Math.Sqrt(1.0 - cos_alpha * cos_alpha);
            double sin_beta = Math.Sqrt(1.0 - cos_beta * cos_beta);
            double C = sin_alpha * sin_beta / Math.Max(1e-20, cos_beta);

            double res = A + B * Math.Max(0.0, gamma) * C;
            res /= Math.PI;

            // Remember we must include the N.L term!
            res *= NdotL;

            // Cosine-weighted hemisphere sampling
            _pdf = NdotL / Math.PI;

            return res;
        }

        // Here we use a simple cosine-weighted hemisphere sampling
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

        public LTCLightingModel GetLightingModel()
        {
            return LTCLightingModel.OrenNayar;
        }
    }
}
