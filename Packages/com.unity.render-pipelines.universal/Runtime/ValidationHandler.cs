using System.Diagnostics;
using Unity.RenderPipelines.Core.Runtime.Shared;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class ValidationHandler
    {
        string k_ErrorMesssageHowToResolve = "The On-Tile Validation layer is activated with the setting 'Tile-Only Mode' on the URP Renderer. " +
                        "When activated, it is not allowed to sample (RenderGraph.UseTexture) the cameraColor or cameraDepth (intermediate) textures or the GBuffers or any copies of those." +
                        "You need to disable any of the following that could cause the issue: a URP setting that would break the native render pass, a ScriptableRenderPass that is enqueued " +
                        "from script, or a ScriptableRenderFeature that is installed on your URP Renderer.\n";

        OnTileValidationLayer m_OnTileValidationLayer;
        public bool active { get; set; }

        public ValidationHandler(bool onTileValidation)
        {
            if (onTileValidation)
            {                
                m_OnTileValidationLayer = new OnTileValidationLayer();
                m_OnTileValidationLayer.errorMessageHowToResolve = k_ErrorMesssageHowToResolve;
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void OnBeginRenderGraphFrame()
        {
            
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void OnBeforeRendering(RenderGraph renderGraph, UniversalResourceData resourceData)
        {
            // Will be null and therefor remove the validation layer when onTileValidation is off
            InternalRenderGraphValidation.SetAdditionalValidationLayer(renderGraph,
                active? m_OnTileValidationLayer : null);

            if (m_OnTileValidationLayer != null && active)
            {
                m_OnTileValidationLayer.renderGraph = renderGraph;

                // Note that we either set the backbuffer or the intermediate textures.
                m_OnTileValidationLayer.Add(resourceData.activeColorTexture);
                m_OnTileValidationLayer.Add(resourceData.activeDepthTexture);
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void OnBeforeGBuffers(RenderGraph renderGraph, UniversalResourceData resourceData)
        {
            if (m_OnTileValidationLayer != null && active)
            {
                foreach (TextureHandle handle in resourceData.gBuffer)
                {
                    m_OnTileValidationLayer.Add(handle);
                }
            }
        }
    }
}
