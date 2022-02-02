using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition.LTC
{
    /// <summary>
    /// Ward implementation of the BRDF interface
    // Formulas come from -> Walter, B. 2005 "Notes on the Ward BRDF" (https://pdfs.semanticscholar.org/330e/59117d7da6c794750730a15f9a178391b9fe.pdf)
    // The BRDF though, is the one most proeminently used by the AxF materials and is based on the Geisler-Moroder variation of Ward (http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.169.9908&rep=rep1&type=pdf)
    /// </summary>
    struct BRDF_Ward : IBRDF
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
            double NdotH = Math.Max(1e-8, H.z);
            double LdotH = Math.Max(1e-8, Vector3.Dot(_tsLight, H));

            // D (basically a Beckmann distribution + an additional divider for albedo bounding)
            double m2 = _alpha * _alpha;
            double cosb2 = NdotH * NdotH;
            double D = Math.Exp(-(1 - cosb2) / (m2 * cosb2))       // exp( -tan(a)² / m² )
                / (Math.PI * m2 * cosb2 * cosb2);                   // / (PI * m² * cos(a)^4)
            D /= 4.0 * LdotH * LdotH;                               // Moroder

            // fr = F(H) * D(H)
            double res = D;

            // Remember we must include the N.L term!
            res *= NdotL;

            // From Walter, eq. 24 we know that pdf(H) = D(H) * (N.H)
            _pdf = Math.Abs(D * NdotH);

            return res;
        }

        public void GetSamplingDirection(ref Vector3 _tsView, float _alpha, float _U1, float _U2, ref Vector3 _direction)
        {
            // Ward NDF sampling (eqs. 6 & 7 from above paper)
            float tanTheta = _alpha * Mathf.Sqrt(-Mathf.Log(Mathf.Max(1e-6f, _U1)));
            float phi = _U2 * 2.0f * Mathf.PI;

            float cosTheta = 1.0f / Mathf.Sqrt(1 + tanTheta * tanTheta);
            float sinTheta = Mathf.Sqrt(1 - cosTheta * cosTheta);
            Vector3 H = new Vector3(sinTheta * Mathf.Cos(phi), sinTheta * Mathf.Sin(phi), cosTheta);
            _direction = 2.0f * Vector3.Dot(H, _tsView) * H - _tsView;      // Mirror view direction
        }

        public LTCLightingModel GetLightingModel()
        {
            return LTCLightingModel.Ward;
        }
    }
}
