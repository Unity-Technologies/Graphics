using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition.LTC
{
    /// <summary>
    /// Cook-Torrance implementation of the BRDF interface
    /// </summary>
    struct BRDF_CookTorrance : IBRDF
    {
        public double Eval(ref Vector3 _tsView, ref Vector3 _tsLight, float _alpha, out double _pdf)
        {
            if (_tsView.z <= 0)
            {
                _pdf = 0;
                return 0;
            }

            _alpha = Mathf.Max(0.002f, _alpha);

            Vector3 H = (_tsView + _tsLight).normalized;
            double NdotL = Math.Max(1e-8, _tsLight.z);
            double NdotV = Math.Max(1e-8, _tsView.z);
            double NdotH = H.z;
            double LdotH = Math.Max(1e-8, Vector3.Dot(_tsLight, H));

            // D
            double cosb2 = NdotH * NdotH;
            double m2 = _alpha * _alpha;
            double D = Math.Exp((cosb2 - 1.0) / (cosb2 * m2))            // exp( -tan(a)² / m² )
                / Math.Max(1e-12, Math.PI * m2 * cosb2 * cosb2);          // / (PI * m² * cos(a)^4)

            // masking/shadowing
            double G = Math.Min(1, 2.0 * NdotH * Math.Min(NdotV, NdotL) / LdotH);

            // fr = F(H) * G(V, L) * D(H) / (4 * (N.L) * (N.V))
            double res = D * G / (4.0 * NdotV);        // Full specular mico-facet model is F * D * G / (4 * NdotL * NdotV) but since we're fitting with the NdotL included, it gets nicely canceled out!

            // pdf = D(H) * (N.H) / (4 * (L.H))
            _pdf = Math.Abs(D * NdotH / (4.0 * LdotH));

            return res;
        }

        public void GetSamplingDirection(ref Vector3 _tsView, float _alpha, float _U1, float _U2, ref Vector3 _direction)
        {
            float phi = 2.0f * Mathf.PI * _U1;
            float cosTheta = 1.0f / Mathf.Sqrt(1 - _alpha * _alpha * Mathf.Log(Mathf.Max(1e-6f, _U2)));
            float sinTheta = Mathf.Sqrt(1 - cosTheta * cosTheta);
            Vector3 H = new Vector3(sinTheta * Mathf.Cos(phi), sinTheta * Mathf.Sin(phi), cosTheta);
            _direction = 2.0f * Vector3.Dot(H, _tsView) * H - _tsView;      // Mirror view direction
        }

        public LTCLightingModel GetLightingModel()
        {
            return LTCLightingModel.CookTorrance;
        }
    }
}
