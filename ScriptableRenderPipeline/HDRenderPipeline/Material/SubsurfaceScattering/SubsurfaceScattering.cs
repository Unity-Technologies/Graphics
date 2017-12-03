using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class SubsurfaceScatteringManager
    {
        public const int k_MaxGbuffer = 2;

        public int sssbufferCount { get; set; }

        RenderTargetIdentifier[] m_ColorMRTs;
        RenderTargetIdentifier[] m_RTIDs = new RenderTargetIdentifier[k_MaxGbuffer];


       // public RenderTargetIdentifier SSSBuffer[2];

       // public SetSSSBuffer();
    }
}
