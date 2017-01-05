using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    public class BaseLightLoop
    {
        protected Light m_CurrentSunLight = null;

        // TODO: We should rather put the texture settings in LightLoop, but how do we serialize it ?
        public virtual void Build(TextureSettings textureSettings) {}

        public virtual void Cleanup() {}

        public virtual bool NeedResize() { return false;  }

        public virtual void AllocResolutionDependentBuffers(int width, int height) { }

        public virtual void ReleaseResolutionDependentBuffers() {}

        public virtual void NewFrame() {}

        public virtual void PrepareLightsForGPU(ShadowSettings shadowSettings, CullResults cullResults, Camera camera, ref ShadowOutput shadowOutput) { }
        
        // TODO: this should not be part of the interface but for now make something working
        public virtual void BuildGPULightLists(Camera camera, ScriptableRenderContext loop, RenderTargetIdentifier cameraDepthBufferRT) { }

        public virtual void PushGlobalParams(Camera camera, ScriptableRenderContext loop) {}

        public virtual void RenderDeferredLighting(HDRenderPipeline.HDCamera hdCamera, ScriptableRenderContext renderContext, RenderTargetIdentifier cameraColorBufferRT) {}

        public virtual void RenderForward(Camera camera, ScriptableRenderContext renderContext, bool renderOpaque) {}

        public Light GetCurrentSunLight() { return m_CurrentSunLight;  }
    }
}
