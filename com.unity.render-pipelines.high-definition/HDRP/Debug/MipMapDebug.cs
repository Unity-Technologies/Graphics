using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public enum DebugMipMapMode
    {
        None,
        MipRatio,
        MipCount,
        MipCountReduction,
        StreamingMipBudget,
        StreamingMip
    }

    [Serializable]
    public class MipMapDebugSettings
    {
        public DebugMipMapMode debugMipMapMode = DebugMipMapMode.None;

        public bool IsDebugDisplayEnabled()
        {
            return debugMipMapMode != DebugMipMapMode.None;
        }

        public void OnValidate()
        {
        }
    }
}
