using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition.LTC
{
    /// <summary>
    /// Diffuse lobe used for the cotton wool of the Fabric shader
    /// </summary>
    struct BRDF_FabricLambert : IBRDF
    {
        public double Eval(ref Vector3 _tsView, ref Vector3 _tsLight, float _alpha, out double _pdf)
        {
            // Light Sample under the surface
            if (_tsView.z <= 0)
            {
                _pdf = 0;
                return 0;
            }
            double NdotL = Math.Max(0, _tsLight.z);
            _pdf = NdotL / Math.PI;
            return Mathf.Lerp(1.0f, 0.5f, Mathf.Max(0.002f, _alpha)) * _pdf;
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
            _direction.z = Mathf.Sqrt(1 - _U1);    // Project the point from the disk onto the hemisphere.
        }

        public LTCLightingModel GetLightingModel()
        {
            return LTCLightingModel.FabricLambert;
        }
    }
}
