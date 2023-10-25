using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    internal enum RenderPassEvent2D
    {
        None = -1,
        BeforeRendering = 0,
        BeforeRenderingLayer = 100,
        BeforeRenderingShadows = 200,
        BeforeRenderingNormals = 300,
        BeforeRenderingLights = 400,
        BeforeRenderingSprites = 500,
        AfterRenderingLayer = 600,
        BeforeRenderingPostProcessing = 700,
        AfterRenderingPostProcessing = 800,
        AfterRendering = 900,
    }


#if USING_SCRIPTABLE_RENDERER_PASS_2D
    internal abstract class ScriptableRenderPass2D : ScriptableRenderPass
    {
        private RenderPassEvent2D m_RenderPassEvent2D = RenderPassEvent2D.None;
        private int m_RenderPassLayer2D = -1;

        internal RenderPassEvent2D renderPassEvent2D => m_RenderPassEvent2D;
        internal int renderPassLayer2D => m_RenderPassLayer2D;

        public ScriptableRenderPass2D(RenderPassEvent2D rpEvent, int rpLayer)
        {
            m_RenderPassEvent2D = rpEvent;
            m_RenderPassLayer2D = rpLayer;
        }
    }
#endif

    static internal class ScriptableRenderPass2DExtension
    {

        static internal void GetInjectionPoint2D(this ScriptableRenderPass renderPass, out RenderPassEvent2D rpEvent, out int rpLayer)
        {

#if USING_SCRIPTABLE_RENDERER_PASS_2D
            ScriptableRenderPass2D renderPass2D = renderPass as ScriptableRenderPass2D;

            if (renderPass2D == null || renderPass2D.renderPassEvent2D == RenderPassEvent2D.None)
#endif
            {
                rpLayer = int.MinValue;

                if (renderPass.renderPassEvent <= RenderPassEvent.BeforeRenderingTransparents)
                    rpEvent = RenderPassEvent2D.BeforeRendering;
                else if (renderPass.renderPassEvent <= RenderPassEvent.AfterRenderingTransparents)
                    rpEvent = RenderPassEvent2D.BeforeRenderingPostProcessing;
                else if (renderPass.renderPassEvent <= RenderPassEvent.AfterRenderingPostProcessing)
                    rpEvent = RenderPassEvent2D.AfterRenderingPostProcessing;
                else
                    rpEvent = RenderPassEvent2D.AfterRendering;
            }
#if USING_SCRIPTABLE_RENDERER_PASS_2D
            else
            {
                rpEvent = renderPass2D.renderPassEvent2D;
                rpLayer = renderPass2D.renderPassLayer2D;
            }
# endif
        }
    }
}
