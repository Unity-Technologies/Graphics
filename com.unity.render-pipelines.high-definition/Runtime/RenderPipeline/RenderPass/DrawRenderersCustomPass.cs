using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// DrawRenderers Custom Pass
    /// </summary>
    [System.Serializable]
    public class DrawRenderersCustomPass : CustomPass
    {
        // Used only for the UI to keep track of the toggle state
        public bool filterFoldout;
        public bool rendererFoldout;

        //Filter settings
        public CustomPass.RenderQueueType renderQueueType = CustomPass.RenderQueueType.AllOpaque;
        public string[] passNames = new string[1] { "Forward" };
        public LayerMask layerMask = -1;
        public SortingCriteria sortingCriteria = SortingCriteria.CommonOpaque;

        // Override material
        public Material overrideMaterial = null;
        public int overrideMaterialPassIndex = 0;

        Material m_DefaultOverrideMaterial;
        Material defaultOverrideMaterial
        {
            get
            {
                if (m_DefaultOverrideMaterial == null)
                {
                    var res = HDRenderPipeline.defaultAsset.renderPipelineResources;
                    m_DefaultOverrideMaterial = CoreUtils.CreateEngineMaterial(res.shaders.defaultRendererCustomPass);
                }

                return m_DefaultOverrideMaterial;
            }
        }
        
        static List<ShaderTagId> m_HDRPShaderTags;
        static List<ShaderTagId> hdrpShaderTags
        {
            get
            {
                if (m_HDRPShaderTags == null)
                {
                    m_HDRPShaderTags = new List<ShaderTagId>() {
                        HDShaderPassNames.s_ForwardName,            // HD Lit shader
                        HDShaderPassNames.s_ForwardOnlyName,        // HD Unlit shader
                        HDShaderPassNames.s_SRPDefaultUnlitName,    // Cross SRP Unlit shader
                    };
                }
                return m_HDRPShaderTags;
            }
        }

        /// <summary>
        /// Execute the DrawRenderers with parameters setup from the editor
        /// </summary>
        /// <param name="renderContext"></param>
        /// <param name="cmd"></param>
        /// <param name="camera"></param>
        /// <param name="cullingResult"></param>
        protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
        {
            ShaderTagId[] shaderPasses = new ShaderTagId[hdrpShaderTags.Count + ((overrideMaterial != null) ? 1 : 0)];
            System.Array.Copy(hdrpShaderTags.ToArray(), shaderPasses, hdrpShaderTags.Count);
            if (overrideMaterial != null)
            {
                shaderPasses[hdrpShaderTags.Count] = new ShaderTagId(overrideMaterial.GetPassName(overrideMaterialPassIndex));
            }

            if (shaderPasses.Length == 0)
            {
                Debug.LogWarning("Attempt to call DrawRenderers with an empty shader passes. Skipping the call to avoid errors");
                return;
            }

            var result = new RendererListDesc(shaderPasses, cullingResult, hdCamera.camera)
            {
                rendererConfiguration = PerObjectData.None,
                renderQueueRange = GetRenderQueueRange(renderQueueType),
                sortingCriteria = sortingCriteria,
                excludeObjectMotionVectors = true,
                overrideMaterial = (overrideMaterial != null) ? overrideMaterial : defaultOverrideMaterial,
                overrideMaterialPassIndex = (overrideMaterial != null) ? overrideMaterialPassIndex : 0,
                layerMask = layerMask,
            };

            HDUtils.DrawRendererList(renderContext, cmd, RendererList.Create(result));
        }

        /// <summary>
        /// Returns the render queue range associated with the custom render queue type
        /// </summary>
        /// <returns></returns>
        protected RenderQueueRange GetRenderQueueRange(CustomPass.RenderQueueType type)
        {
            switch (type)
            {
                case CustomPass.RenderQueueType.OpaqueNoAlphaTest: return HDRenderQueue.k_RenderQueue_OpaqueNoAlphaTest;
                case CustomPass.RenderQueueType.OpaqueAlphaTest: return HDRenderQueue.k_RenderQueue_OpaqueAlphaTest;
                case CustomPass.RenderQueueType.AllOpaque: return HDRenderQueue.k_RenderQueue_AllOpaque;
                case CustomPass.RenderQueueType.AfterPostProcessOpaque: return HDRenderQueue.k_RenderQueue_AfterPostProcessOpaque;
                case CustomPass.RenderQueueType.PreRefraction: return HDRenderQueue.k_RenderQueue_PreRefraction;
                case CustomPass.RenderQueueType.Transparent: return HDRenderQueue.k_RenderQueue_Transparent;
                case CustomPass.RenderQueueType.LowTransparent: return HDRenderQueue.k_RenderQueue_LowTransparent;
                case CustomPass.RenderQueueType.AllTransparent: return HDRenderQueue.k_RenderQueue_AllTransparent;
                case CustomPass.RenderQueueType.AllTransparentWithLowRes: return HDRenderQueue.k_RenderQueue_AllTransparentWithLowRes;
                case CustomPass.RenderQueueType.AfterPostProcessTransparent: return HDRenderQueue.k_RenderQueue_AfterPostProcessTransparent;
                case CustomPass.RenderQueueType.All:
                default:
                    return HDRenderQueue.k_RenderQueue_All;
            }
        }

        protected override void Cleanup()
        {
            if (m_DefaultOverrideMaterial != null)
                CoreUtils.Destroy(m_DefaultOverrideMaterial);
        }
    }
}