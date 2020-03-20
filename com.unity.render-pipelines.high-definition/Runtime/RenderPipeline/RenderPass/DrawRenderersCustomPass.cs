using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// DrawRenderers Custom Pass
    /// </summary>
    [System.Serializable]
    class DrawRenderersCustomPass : CustomPass
    {
        /// <summary>
        /// HDRP Shader passes
        /// </summary>
        public enum ShaderPass
        {
            // Ordered by frame time in HDRP
            DepthPrepass    = 1,
            Forward         = 0,
        }

        // Used only for the UI to keep track of the toggle state
        public bool filterFoldout;
        public bool rendererFoldout;

        //Filter settings
        public CustomPass.RenderQueueType renderQueueType = CustomPass.RenderQueueType.AllOpaque;
        public string[] passNames = new string[1] { "Forward" };
        public LayerMask layerMask = 1; // Layer mask Default enabled
        public SortingCriteria sortingCriteria = SortingCriteria.CommonOpaque;

        // Override material
        public Material overrideMaterial = null;
        [SerializeField]
        int overrideMaterialPassIndex = 0;
        public string overrideMaterialPassName = "Forward";

        public bool overrideDepthState = false;
        public CompareFunction depthCompareFunction = CompareFunction.LessEqual;
        public bool depthWrite = true;

        public ShaderPass shaderPass = ShaderPass.Forward;

        int fadeValueId;

        static ShaderTagId[] forwardShaderTags;
        static ShaderTagId[] depthShaderTags;

        // Cache the shaderTagIds so we don't allocate a new array each frame
        ShaderTagId[]   cachedShaderTagIDs;

        /// <inheritdoc />
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            fadeValueId = Shader.PropertyToID("_FadeValue");

            // In case there was a pass index assigned, we retrieve the name of this pass
            if (String.IsNullOrEmpty(overrideMaterialPassName) && overrideMaterial != null)
                overrideMaterialPassName = overrideMaterial.GetPassName(overrideMaterialPassIndex);

            forwardShaderTags = new ShaderTagId[] {
                HDShaderPassNames.s_ForwardName,            // HD Lit shader
                HDShaderPassNames.s_ForwardOnlyName,        // HD Unlit shader
                HDShaderPassNames.s_SRPDefaultUnlitName,    // Cross SRP Unlit shader
                HDShaderPassNames.s_EmptyName,              // Add an empty slot for the override material
            };

            depthShaderTags = new ShaderTagId[] {
                HDShaderPassNames.s_DepthForwardOnlyName,
                HDShaderPassNames.s_DepthOnlyName,
                HDShaderPassNames.s_EmptyName,              // Add an empty slot for the override material
            };
        }

        /// <inheritdoc />
        protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
        {
            cullingParameters.cullingMask |= (uint)(int)layerMask;
        }

        protected ShaderTagId[] GetShaderTagIds()
        {
            if (shaderPass == ShaderPass.DepthPrepass)
                return depthShaderTags;
            else
                return forwardShaderTags;
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
            var shaderPasses = GetShaderTagIds();
            if (overrideMaterial != null)
            {
                shaderPasses[forwardShaderTags.Length - 1] = new ShaderTagId(overrideMaterialPassName);
                overrideMaterial.SetFloat(fadeValueId, fadeValue);
            }

            if (shaderPasses.Length == 0)
            {
                Debug.LogWarning("Attempt to call DrawRenderers with an empty shader passes. Skipping the call to avoid errors");
                return;
            }

            var mask = overrideDepthState ? RenderStateMask.Depth : 0;
            mask |= overrideDepthState && !depthWrite ? RenderStateMask.Stencil : 0;
            var stateBlock = new RenderStateBlock(mask)
            {
                depthState = new DepthState(depthWrite, depthCompareFunction),
                // We disable the stencil when the depth is overwritten but we don't write to it, to prevent writing to the stencil.
                stencilState = new StencilState(false),
            };

            PerObjectData renderConfig = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Shadowmask) ? HDUtils.k_RendererConfigurationBakedLightingWithShadowMask : HDUtils.k_RendererConfigurationBakedLighting;

            var result = new RendererListDesc(shaderPasses, cullingResult, hdCamera.camera)
            {
                rendererConfiguration = renderConfig,
                renderQueueRange = GetRenderQueueRange(renderQueueType),
                sortingCriteria = sortingCriteria,
                excludeObjectMotionVectors = false,
                overrideMaterial = overrideMaterial,
                overrideMaterialPassIndex = (overrideMaterial != null) ? overrideMaterial.FindPass(overrideMaterialPassName) : 0,
                stateBlock = stateBlock,
                layerMask = layerMask,
            };

            HDUtils.DrawRendererList(renderContext, cmd, RendererList.Create(result));
        }

        /// <inheritdoc />
        public override IEnumerable<Material> RegisterMaterialForInspector() { yield return overrideMaterial; }
    }
}
