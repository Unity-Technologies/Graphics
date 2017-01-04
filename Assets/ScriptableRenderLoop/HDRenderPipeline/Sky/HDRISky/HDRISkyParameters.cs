using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    [DisallowMultipleComponent]
    public class HDRISkyParameters
        : SkyParameters
    {
        public Cubemap skyHDRI;
    }
}
