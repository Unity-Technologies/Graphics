using System;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // GlobalFrameSettings represent settings that are immutable at runtime.
    // There is a dedicated RenderRenderPipelineSettings for each platform
    [Serializable]
    public class GlobalFrameSettings : ScriptableObject
    {
        [Serializable]
        public class GlobalLightingSettings
        {
            public bool supportShadowMask = true;
            public bool supportSSR = true;
            public bool supportSSAO = true;
            public bool supportSSSAndTransmission = true;
        }

        [Serializable]
        public class GlobalRenderSettings
        {
            public bool supportDBuffer = false;
            public bool supportMSAA = false;
        }

        public GlobalLightingSettings   lightingSettings = new GlobalLightingSettings();
        public GlobalRenderSettings     renderSettings = new GlobalRenderSettings();
        public GlobalLightLoopSettings  lightLoopSettings = new GlobalLightLoopSettings();
        public ShadowInitParameters     shadowInitParams = new ShadowInitParameters();
    }
}
