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
            DepthPrepass = 1,
            ///<summary>Forward pass, render the object color.</summary>
            Forward = 0,
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
        public SortingCriteria sortingCriteria = HDUtils.k_OpaqueSortingCriteria;

        /// <summary>
        /// Select which type of override to apply on the DrawRenderers pass.
        /// </summary>
        public enum OverrideMaterialMode
        {
            /// <summary> Disable the material override </summary>
            None,
            /// <summary> Override the material for all renderers </summary>
            Material,
            /// <summary> Override the shader for all renderers. This option keeps the material properties of the renderer and can be used like a replacement shader. </summary>
            Shader
        };

        /// <summary>
        /// Controls how the material on each renderer will be replaced. Material mode uses overrideMaterial. Shader mode uses overrideShader.
        /// </summary>
        public OverrideMaterialMode overrideMode = OverrideMaterialMode.Material; //default to Material as this was previously the only option

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

        // Override shader
        /// <summary>
        /// Replaces the shader of selected renderers while using the current material properties.
        /// </summary>
        public Shader overrideShader = null;
        [SerializeField]
        int overrideShaderPassIndex = 0;
        ///<summary>
        /// Select whih pass will be used to render objects when using an override material.
        /// </summary>
        public string overrideShaderPassName = "Forward";

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
        /// Override the stencil state of the objects.
        /// </summary>
        public bool overrideStencil = false;

        /// <summary>
        /// Stencil reference value. Be careful when using this value to write in the stencil buffer to not overwrite HDRP stencil bits.
        /// </summary>
        public int stencilReferenceValue = (int)UserStencilUsage.UserBit0;

        /// <summary>
        /// Write mask of the stencil.
        /// </summary>
        public int stencilWriteMask = (int)(UserStencilUsage.AllUserBits);

        /// <summary>
        /// Read mask of the stencil
        /// </summary>
        public int stencilReadMask = (int)(UserStencilUsage.AllUserBits);

        /// <summary>
        /// Comparison operation between the stencil buffer and the reference value.
        /// </summary>
        public CompareFunction stencilCompareFunction = CompareFunction.Always;

        /// <summary>
        /// Operation to execute if the stencil test passes.
        /// </summary>
        public StencilOp stencilPassOperation;

        /// <summary>
        /// Operation to execute if the stencil test fails.
        /// </summary>
        public StencilOp stencilFailOperation;

        /// <summary>
        /// Operation to execute if the depth test fails.
        /// </summary>
        public StencilOp stencilDepthFailOperation;

        /// <summary>
        /// Set the shader pass to use when the override material is null
        /// </summary>
        public ShaderPass shaderPass = ShaderPass.Forward;

        /// <summary>
        /// Apply variable rate shading using the shading rate image.
        /// </summary>
        public bool variableRateShading = false;

        /// <summary>
        /// True if you want your custom pass to enable and set variable rate shading (VRS) texture. False for regular passes.
        /// </summary>
        protected override bool enableVariableRateShading => variableRateShading;

        int fadeValueId;

        static ShaderTagId[] forwardShaderTags;
        static ShaderTagId[] depthShaderTags;

        // Cache the shaderTagIds so we don't allocate a new array each frame
        ShaderTagId[] cachedShaderTagIDs;

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
            if (String.IsNullOrEmpty(overrideShaderPassName) && overrideShader != null)
                overrideShaderPassName = new Material(overrideShader).GetPassName(overrideShaderPassIndex);

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
            if (overrideStencil)
                mask |= RenderStateMask.Stencil;
            var stateBlock = new RenderStateBlock(mask)
            {
                depthState = new DepthState(depthWrite, depthCompareFunction),
                // We disable the stencil when the depth is overwritten but we don't write to it, to prevent writing to the stencil.
                stencilState = new StencilState(overrideStencil, (byte)stencilReadMask, (byte)stencilWriteMask, stencilCompareFunction, stencilPassOperation, stencilFailOperation, stencilDepthFailOperation),
                stencilReference = overrideStencil ? stencilReferenceValue : 0,
            };

            PerObjectData renderConfig = HDUtils.GetRendererConfiguration(ctx.hdCamera.frameSettings.IsEnabled(FrameSettingsField.AdaptiveProbeVolume), ctx.hdCamera.frameSettings.IsEnabled(FrameSettingsField.Shadowmask));
            var overrideShaderMaterial = (overrideShader != null) ? new Material(overrideShader) : null;

            var result = new RendererUtils.RendererListDesc(shaderPasses, ctx.cullingResults, ctx.hdCamera.camera)
            {
                rendererConfiguration = renderConfig,
                renderQueueRange = GetRenderQueueRange(renderQueueType),
                sortingCriteria = sortingCriteria,
                excludeObjectMotionVectors = false,
                overrideShader = overrideMode == OverrideMaterialMode.Shader ? overrideShader : null,
                overrideMaterial = overrideMode == OverrideMaterialMode.Material ? overrideMaterial : null,
                overrideMaterialPassIndex = (overrideMaterial != null) ? overrideMaterial.FindPass(overrideMaterialPassName) : 0,
                overrideShaderPassIndex = (overrideShader != null) ? overrideShaderMaterial.FindPass(overrideShaderPassName) : 0,
                stateBlock = stateBlock,
                layerMask = layerMask,
            };

            Object.DestroyImmediate(overrideShaderMaterial);
            var renderCtx = ctx.renderContext;
            var rendererList = renderCtx.CreateRendererList(result);
            bool opaque = renderQueueType == RenderQueueType.AllOpaque || renderQueueType == RenderQueueType.OpaqueAlphaTest || renderQueueType == RenderQueueType.OpaqueNoAlphaTest;
            HDRenderPipeline.RenderForwardRendererList(ctx.hdCamera.frameSettings, rendererList, opaque, ctx.renderContext, ctx.cmd);
        }

        /// <summary>
        /// List all the materials that need to be displayed at the bottom of the component.
        /// All the materials gathered by this method will be used to create a Material Editor and then can be edited directly on the custom pass.
        /// </summary>
        /// <returns>An enumerable of materials to show in the inspector. These materials can be null, the list is cleaned afterwards</returns>
        public override IEnumerable<Material> RegisterMaterialForInspector() { yield return overrideMaterial; }
    }
}
