using System.Diagnostics;
using Unity.RenderPipelines.Core.Runtime.Shared;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class ValidationHandler
    {
        OnTileValidationLayer m_OnTileValidationLayer;

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void OnBeginRenderGraphFrame(bool onTileValidation)
        {
            if (onTileValidation)
            {
                if (m_OnTileValidationLayer == null)
                    m_OnTileValidationLayer = new OnTileValidationLayer();
            }
            else
            {
                m_OnTileValidationLayer = null;
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void OnBeforeRendering(RenderGraph renderGraph, UniversalResourceData resourceData)
        {
            // Will be null and therefor remove the validation layer when onTileValidation is off
            InternalRenderGraphValidation.SetAdditionalValidationLayer(renderGraph, m_OnTileValidationLayer);

            if (m_OnTileValidationLayer != null)
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
            if (m_OnTileValidationLayer != null)
            {
                foreach (TextureHandle handle in resourceData.gBuffer)
                {
                    m_OnTileValidationLayer.Add(handle);
                }
            }
        }
    }
}
