using System;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class that holds data related to culling.
    /// </summary>
    public class CullContextData : ContextItem
    {
        internal ScriptableRenderContext? m_RenderContext;

        /// <inheritdoc/>
        public override void Reset()
        {
            m_RenderContext = null;
        }

        /// <summary>
        /// Assigns the render context once at initialization time. 
        /// </summary>
        /// <param name="renderContext">The render context to assign.</param>
        public void SetRenderContext(in ScriptableRenderContext renderContext)
        {
            m_RenderContext = renderContext;
        }

        /// <summary>
        /// Performs scene culling based on the provided parameters.
        /// </summary>
        /// <param name="parameters">The parameters used for the culling.</param>
        /// <returns>The culling results.</returns>
        public CullingResults Cull(ref ScriptableCullingParameters parameters)
        {
            if (!m_RenderContext.HasValue)
            {
                throw new InvalidOperationException("The ScriptableRenderContext member is not set.");
            }

            return m_RenderContext.Value.Cull(ref parameters);
        }

        /// <summary>
        /// Performs shadow casters culling based on the provided parameters.
        /// </summary>
        /// <param name="cullingResults">The scene culling results.</param>
        /// <param name="shadowCastersCullingInfos">The shadow casters culling informations.</param>
        /// <exception cref="InvalidOperationException">The parameters used for the shadow culling.</exception>
        public void CullShadowCasters(CullingResults cullingResults, ShadowCastersCullingInfos shadowCastersCullingInfos)
        {
            if (!m_RenderContext.HasValue)
            {
                throw new InvalidOperationException("The ScriptableRenderContext member is not set.");
            }

            m_RenderContext.Value.CullShadowCasters(cullingResults, shadowCastersCullingInfos);
        }
    }
}
