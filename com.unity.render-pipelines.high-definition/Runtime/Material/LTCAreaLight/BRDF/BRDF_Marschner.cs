using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition.LTC
{
    struct BRDF_Marschner : IBRDF
    {
        public double Eval(ref Vector3 _tsView, ref Vector3 _tsLight, float _alpha, out double _pdf)
        {
            // Uniform sampled over a sphere.
            _pdf = 1f / (4f * Math.PI);

            return 0f;
        }

        public void GetSamplingDirection(ref Vector3 _tsView, float _alpha, float _U1, float _U2, ref Vector3 _direction)
        {
            _direction = Vector3.up;
        }

        public LTCLightingModel GetLightingModel()
        {
            return LTCLightingModel.Marschner;
        }
    }
}
