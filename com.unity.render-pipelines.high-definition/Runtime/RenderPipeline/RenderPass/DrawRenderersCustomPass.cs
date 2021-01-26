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
    public class DrawRenderersCustomPass : CustomPass
    {
        /// <summary>
        /// HDRP Shader passes
        /// </summary>
        public enum ShaderPass
        {
            // Ordered by frame time in HDRP
            ///<summary>Object Depth pre-pass, only the depth of the object will be rendered.</summary>
            DepthPrepass    = 1,
            ///<summary>Forward pass, render the object color.</summary>
            Forward         = 0,
        }

        // Used only for the UI to keep track of the toggle state
        [SerializeField] internal bool filterFoldout;
        [SerializeField] internal bool rendererFoldout;

        //Filter settings
        /// <summary>
        /// Render Queue filter to select which kind of object to render.
        /// </summary>
        public RenderQueueType renderQueueType = RenderQueueType.AllOpaque;
        /// <summary>
        /// Layer Mask filter, select which layer to render.
        /// </summary>
        public LayerMask layerMask = 1; // Layer mask Default enabled
        /// <summary>
        /// Sorting flags of the objects to render.
        /// </summary>
        public SortingCriteria sortingCriteria = SortingCriteria.CommonOpaque;

        // Override material
        /// <summary>
        /// Replaces the material of selected renders by this one, be sure to also set overrideMaterialPassName to a good value when using this property.
        /// </summary>
        public Material overrideMaterial = null;
        [SerializeField]
        int overrideMaterialPassIndex = 0;
        /// <summary>
        /// Select which pass will be used to render objects when using an override material.
        /// </summary>
        public string overrideMaterialPassName = "Forward";

        /// <summary>
        /// When true, overrides the depth state of the objects.
        /// </summary>
        public bool overrideDepthState = false;
        /// <summary>
        /// Overrides the Depth comparison function, only used when overrideDepthState is true.
        /// </summary>
        public CompareFunction depthCompareFunction = CompareFunction.LessEqual;
        /// <summary>
        /// Overrides the Depth write, only used when overrideDepthState is true.
        /// </summary>
        public bool depthWrite = true;

        /// <summary>
        /// Set the shader pass to use when the override material is null
        /// </summary>
        public ShaderPass shaderPass = ShaderPass.Forward;

        int fadeValueId;

        static ShaderTagId[] forwardShaderTags;
        static ShaderTagId[] depthShaderTags;

        // Cache the shaderTagIds so we don't allocate a new array each frame
        ShaderTagId[]   cachedShaderTagIDs;

        /// <summary>
        /// Called before the first execution of the pass occurs.
        /// Allow you to allocate custom buffers.
        /// </summary>
        /// <param name="renderContext">The render context</param>
        /// <param name="cmd">Current command buffer of the frame</param>
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            fadeValueId = Shader.PropertyToID("_FadeValue");

            // In case there was a pass index assigned, we retrieve the name of this pass
            if (String.IsNullOrEmpty(overrideMaterialPassName) && overrideMaterial != null)
                overrideMaterialPassName = overrideMaterial.GetPassName(overrideMaterialPassIndex);

            forwardShaderTags = new ShaderTagId[]
            {
                HDShaderPassNames.s_ForwardName,            // HD Lit shader
                HDShaderPassNames.s_ForwardOnlyName,        // HD Unlit shader
                HDShaderPassNames.s_SRPDefaultUnlitName,    // Cross SRP Unlit shader
                HDShaderPassNames.s_EmptyName,              // Add an empty slot for the override material
            };

            depthShaderTags = new ShaderTagId[]
            {
                HDShaderPassNames.s_DepthForwardOnlyName,
                HDShaderPassNames.s_DepthOnlyName,
                HDShaderPassNames.s_EmptyName,              // Add an empty slot for the override material
            };
        }

        /// <summary>
        /// Use this method if you want to draw objects that are not visible in the camera.
        /// For example if you disable a layer in the camera and add it in the culling parameters, then the culling result will contains your layer.
        /// </summary>
        /// <param name="cullingParameters">Aggregate the parameters in this property (use |= for masks fields, etc.)</param>
        /// <param name="hdCamera">The camera where the culling is being done</param>
        protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
        {
            cullingParameters.cullingMask |= (uint)(int)layerMask;
        }

        ShaderTagId[] GetShaderTagIds()
        {
            if (shaderPass == ShaderPass.DepthPrepass)
                return depthShaderTags;
            else
                return forwardShaderTags;
        }

        /// <summary>
        /// Execute the DrawRenderers with parameters setup from the editor
        /// </summary>
        /// <param name="ctx">The context of the custom pass. Contains command buffer, render context, buffer, etc.</param>
        protected override void Execute(CustomPassContext ctx)
        {
            var shaderPasses = GetShaderTagIds();
            if (overrideMaterial != null)
            {
                shaderPasses[shaderPasses.Length - 1] = new ShaderTagId(overrideMaterialPassName);
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

            PerObjectData renderConfig = ctx.hdCamera.frameSettings.IsEnabled(FrameSettingsField.Shadowmask) ? HDUtils.GetBakedLightingWithShadowMaskRenderConfig() : HDUtils.GetBakedLightingRenderConfig();

            var result = new RendererListDesc(shaderPasses, ctx.cullingResults, ctx.hdCamera.camera)
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

            CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, RendererList.Create(result));
        }

        /// <summary>
        /// List all the materials that need to be displayed at the bottom of the component.
        /// All the materials gathered by this method will be used to create a Material Editor and then can be edited directly on the custom pass.
        /// </summary>
        /// <returns>An enumerable of materials to show in the inspector. These materials can be null, the list is cleaned afterwards</returns>
        public override IEnumerable<Material> RegisterMaterialForInspector() { yield return overrideMaterial; }
    }
}
