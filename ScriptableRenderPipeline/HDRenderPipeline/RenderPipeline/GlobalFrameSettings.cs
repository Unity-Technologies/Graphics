using System;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // GlobalFrameSettings represent settings that are immutable at runtime.
    // There is a dedicated RenderRenderPipelineSettings for each platform

    [Serializable]
    public struct GlobalRenderSettings
    {
        public bool supportShadowMask;

        public bool supportSSSAndTransmission;
        public bool supportDBuffer;

        public bool supportSSR;
        public bool supportSSAO;
        public bool supportMSAA;

        public GlobalRenderSettings()
        {
            supportShadowMask = true;

            supportSSSAndTransmission = true;
            supportDBuffer = false;

            supportSSR = true;
            supportSSAO = true;
            supportMSAA = false;
        }
    }

    [Serializable]
    public class GlobalFrameSettings : ScriptableObject
    {
        public GlobalRenderSettings     renderSettings;
        public GlobalLightLoopSettings  lightLoopSettings;
    }
}
