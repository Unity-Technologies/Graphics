using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition.LTC
{
    /// <summary>
    /// Disney Diffuse Implementation
    /// Source from 2012 Burley, B. "Physically-Based Shading at Disney" Section 5.3
    /// (https://disney-animation.s3.amazonaws.com/library/s2012_pbs_disney_brdf_notes_v2.pdf)
    /// </summary>
    struct BRDF_KajiyaKayDiffuse : IBRDF
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

            // Cosine-weighted hemisphere sampling
            _pdf = NdotL / Math.PI;

            return NdotL / (Math.PI * Math.PI);
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

        public LTCLightingModel GetLightingModel()
        {
            return LTCLightingModel.KajiyaKayDiffuse;
        }
    }
}
