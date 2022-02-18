using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition.LTC
{
    /// <summary>
    /// "Charlie" Sheen Implementation
    /// Source from Sony Pictures Imageworks by Estevez and Kulla, "Production Friendly Microfacet Sheen BRDF" (http://blog.selfshadow.com/publications/s2017-shading-course/imageworks/s2017_pbs_imageworks_sheen.pdf)
    /// Details: https://knarkowicz.wordpress.com/2018/01/04/cloth-shading/
    /// </summary>
    struct BRDF_Charlie : IBRDF
    {
        public double Eval(ref Vector3 _tsView, ref Vector3 _tsLight, float _alpha, out double _pdf)
        {
            if (_tsView.z <= 0)
            {
                _pdf = 0;
                return 0;
            }

            _alpha = Mathf.Max(0.002f, _alpha);

            Vector3 H = Vector3.Normalize(_tsView + _tsLight);
            double NdotL = _tsLight.z;
            double NdotV = _tsView.z;
            double NdotH = H.z;

            // D
            double D = CharlieD(_alpha, NdotH);

            // Ashikmin masking/shadowing
            //            double  G = V_Ashikhmin( NdotV, NdotL );
            double G = V_Charlie(NdotV, NdotL, _alpha);

            // fr = F(H) * G(V, L) * D(H)
            // Note that the usual 1 / (4 * (N.L) * (N.V)) part of the Cook-Torrance micro-facet model is actually contained in the G visibility term in our case (as reported by Ashkmin in "Distribution-based BRDFs" eq. 2)
            double res = D * G * NdotL;    // We also include the (N.L) term here

            // We're using uniform distribution
            _pdf = 0.5 / Math.PI;

            return res;
        }

        // Paper recommend plain uniform sampling of upper hemisphere instead of importance sampling for Charlie
        public void GetSamplingDirection(ref Vector3 _tsView, float _alpha, float _U1, float _U2, ref Vector3 _direction)
        {
            float phi = 2.0f * Mathf.PI * _U1;
            float cosTheta = 1.0f - _U2;
            float sinTheta = Mathf.Sqrt(1 - cosTheta * cosTheta);
            _direction = new Vector3(sinTheta * Mathf.Cos(phi), sinTheta * Mathf.Sin(phi), cosTheta);
        }

        double CharlieD(float _roughness, double _NdotH)
        {
            double invR = 1.0 / _roughness;
            double cos2h = _NdotH * _NdotH;
            double sin2h = 1.0f - cos2h;
            double res = (2.0 + invR) * Math.Pow(sin2h, invR * 0.5) / (2.0 * Math.PI);
            return res;
        }

        double V_Ashikhmin(double _NdotV, double _NdotL)
        {
            return 1.0 / (4.0 * (_NdotL + _NdotV - _NdotL * _NdotV));
        }

        // Note: This version doesn't include the softening of the paper: Production Friendly Microfacet Sheen BRDF
        double V_Charlie(double _NdotV, double _NdotL, double _roughness)
        {
            double lambdaV = _NdotV < 0.5 ? Math.Exp(CharlieL(_NdotV, _roughness)) : Math.Exp(2.0 * CharlieL(0.5, _roughness) - CharlieL(1.0 - _NdotV, _roughness));
            double lambdaL = _NdotL < 0.5 ? Math.Exp(CharlieL(_NdotL, _roughness)) : Math.Exp(2.0 * CharlieL(0.5, _roughness) - CharlieL(1.0 - _NdotL, _roughness));

            return 1.0 / ((1.0 + lambdaV + lambdaL) * (4.0 * _NdotV * _NdotL));
        }

        double CharlieL(double x, double _roughness)
        {
            float r = Mathf.Clamp01((float)_roughness);
            r = 1.0f - r * r;

            float a = Mathf.Lerp(25.3245f, 21.5473f, r);
            float b = Mathf.Lerp(3.32435f, 3.82987f, r);
            float c = Mathf.Lerp(0.16801f, 0.19823f, r);
            float d = Mathf.Lerp(-1.27393f, -1.97760f, r);
            float e = Mathf.Lerp(-4.85967f, -4.32054f, r);

            double res = a / (1.0 + b * Math.Pow(Math.Max(0, x), c)) + d * x + e;
            return res;
        }

        public LTCLightingModel GetLightingModel()
        {
            return LTCLightingModel.Charlie;
        }
    }
}
