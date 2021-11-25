//////////////////////////////////////////////////////////////////////////
// BRDF Interface
//////////////////////////////////////////////////////////////////////////
//
using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition.LTC
{
    /// <summary>
    /// BRDF Interface that you must implement in order to generate a new table
    /// </summary>
    internal interface IBRDF
    {
        /// <summary>
        /// Evaluation of the ***cosine-weighted*** BRDF
        /// </summary>
        /// <param name="_tsView">The vector pointing toward the camera</param>
        /// <param name="_tsLight">The vector pointing toward the light</param>
        /// <param name="_alpha">Surface roughness</param>
        /// <param name="_pdf">The Probability Density Function of sampling the light in that direction</param>
        /// <returns></returns>
        double Eval(ref Vector3 _tsView, ref Vector3 _tsLight, float _alpha, out double _pdf);

        /// <summary>
        /// Gets an importance-sampled light direction given a view vector and the surface roughness
        /// </summary>
        /// <param name="_tsView">The vector pointing toward the camera</param>
        /// <param name="_alpha">>Surface roughness</param>
        /// <param name="_U1">A random value in [0,1]</param>
        /// <param name="_U2">A 2nd random value in [0,1]</param>
        /// <param name="_direction">The generated direction</param>
        void GetSamplingDirection(ref Vector3 _tsView, float _alpha, float _U1, float _U2, ref Vector3 _direction);

        LTCLightingModel GetLightingModel();
    };
}
