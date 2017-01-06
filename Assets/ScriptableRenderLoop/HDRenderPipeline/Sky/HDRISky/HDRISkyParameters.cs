using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [DisallowMultipleComponent]
    public class HDRISkyParameters
        : SkyParameters
    {
        public Cubemap skyHDRI;
    }
}
