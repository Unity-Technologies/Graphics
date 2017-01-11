using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ShadowFilteringFixedSizePCF
    {
        string GetKeyword()
        {
            return "SHADOWFILTERING_FIXED_SIZE_PCF";
        }
    };
}
