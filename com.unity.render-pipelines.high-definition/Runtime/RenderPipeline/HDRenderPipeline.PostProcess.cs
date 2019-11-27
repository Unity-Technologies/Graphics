using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        void RenderPostProcess()
        {
            PostProcessParameters parameters = PreparePostProcess(cullResults, hdCamera);

            // We render AfterPostProcess objects first into a separate buffer that will be composited in the final post process pass
            RenderAfterPostProcess( parameters
                                    , GetAfterPostProcessOffScreenBuffer()
                                    , m_SharedRTManager.GetDepthStencilBuffer()
                                    , RendererList.Create(parameters.opaqueAfterPPDesc)
                                    , RendererList.Create(parameters.transparentAfterPPDesc)
                                    , renderContext, cmd);
        }
    }
}
