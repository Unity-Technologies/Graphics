using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools.Graphics;

namespace Unity.Rendering.Universal.Tests
{
    [Flags]
    public enum RenderGraphContext
    {
        None = 0,
        RenderGraphActive = 1,
        RenderGraphInactive = 2
    }

    public class RenderGraphGlobalContext : IGlobalContextProvider
    {
        public int Context =>
            IsRenderGraphActive()
                ? (int)RenderGraphContext.RenderGraphActive
                : (int)RenderGraphContext.RenderGraphInactive;

        static bool IsRenderGraphActive()
        {
            return RenderGraphGraphicsAutomatedTests.enabled
                || (
                    !GraphicsSettings
                        .GetRenderPipelineSettings<RenderGraphSettings>()
                        ?.enableRenderCompatibilityMode ?? false
                );
        }

        public void ActivateContext(RenderGraphContext context)
        {
            if (context == RenderGraphContext.None)
                return;

            RenderGraphGraphicsAutomatedTests.enabled =
                context == RenderGraphContext.RenderGraphActive;
        }
    }
}
