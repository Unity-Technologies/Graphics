using System;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // RenderPipelineSettings define settings that can't be change during runtime. It is equivalent to the GraphicsSettings of Unity (Tiers + shader variant removal).
    // This allow to allocate resource or not for a given feature.
    // FrameSettings control within a frame what is enable or not(enableShadow, enableStereo, enableDistortion...).
    // HDRenderPipelineAsset reference the current RenderPipelineSettings use, there is one per supported platform(Currently this feature is not implemented and only one GlobalFrameSettings is available).
    // A Camera with HDAdditionalData have one FrameSettings that configure how it will render.For example a camera use for reflection will disable distortion and postprocess.
    // Additionally on a Camera there is another FrameSettings call ActiveFrameSettings that is created on the fly based on FrameSettings and allow modification for debugging purpose at runtime without being serialized on disk.
    // The ActiveFrameSettings is register in the debug windows at the creation of the camera.
    // A Camera with HDAdditionalData have a RenderPath that define if it use a "Default" FrameSettings, a preset of FrameSettings or a custom one.
    // HDRenderPipelineAsset contain a "Default" FrameSettings that can be reference by any camera with RenderPath.Defaut or when the camera don't have HDAdditionalData like the camera of the Editor.
    // It also contain a DefaultActiveFrameSettings

    // RenderPipelineSettings represent settings that are immutable at runtime.
    // There is a dedicated RenderPipelineSettings for each platform
    [Serializable]
    public class RenderPipelineSettings
    {
        // Lighting
        public bool supportShadowMask = true;
        public bool supportSSR = true;
        public bool supportSSAO = true;
        public bool supportSubsurfaceScattering = true;

        // Engine
        public bool supportDBuffer = false;
        public bool supportMSAA = false;

        public GlobalLightLoopSettings  lightLoopSettings = new GlobalLightLoopSettings();
        public ShadowInitParameters     shadowInitParams = new ShadowInitParameters();
    }
}
