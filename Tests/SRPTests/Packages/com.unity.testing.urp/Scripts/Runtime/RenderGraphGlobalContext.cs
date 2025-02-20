using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools.Graphics;

namespace Unity.Rendering.Universal.Tests
{
    public enum RenderGraphContext
    {
        RenderGraphActive = 1,
        RenderGraphInactive = 0
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
    }
}
