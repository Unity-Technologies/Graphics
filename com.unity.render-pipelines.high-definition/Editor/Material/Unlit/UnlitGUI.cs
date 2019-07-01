using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    /// <summary>
    /// GUI for HDRP unlit shaders (does not include shader graphs)
    /// </summary>
    class UnlitGUI : HDShaderGUI
    {
        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.Expandable.Base, features: SurfaceOptionUIBlock.Features.Unlit),
            new UnlitSurfaceInputsUIBlock(MaterialUIBlock.Expandable.Input),
            new TransparencyUIBlock(MaterialUIBlock.Expandable.Transparency),
            new EmissionUIBlock(MaterialUIBlock.Expandable.Emissive),
            new AdvancedOptionsUIBlock(MaterialUIBlock.Expandable.Advance, AdvancedOptionsUIBlock.Features.Instancing),
        };

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                uiBlocks.OnGUI(materialEditor, props);

                // Apply material keywords and pass:
                if (changed.changed)
                {
                    foreach (var material in uiBlocks.materials)
                        SetupMaterialKeywordsAndPassInternal(material);
                }
            }
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material) => SetupUnlitMaterialKeywordsAndPass(material);

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        public static void SetupUnlitMaterialKeywordsAndPass(Material material)
        {
            material.SetupBaseUnlitKeywords();
            material.SetupBaseUnlitPass();

            if (material.HasProperty("_EMISSIVE_COLOR_MAP"))
                CoreUtils.SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));

            // Stencil usage rules:
            // DoesntReceiveSSR and DecalsForwardOutputNormalBuffer need to be tagged during depth prepass
            // LightingMask need to be tagged during either GBuffer or Forward pass
            // ObjectVelocity need to be tagged in velocity pass.
            // As velocity pass can be use as a replacement of depth prepass it also need to have DoesntReceiveSSR and DecalsForwardOutputNormalBuffer
            // As GBuffer pass can have no depth prepass, it also need to have DoesntReceiveSSR and DecalsForwardOutputNormalBuffer
            // Object velocity is always render after a full depth buffer (if there is no depth prepass for GBuffer all object motion vectors are render after GBuffer)
            // so we have a guarantee than when we write object velocity no other object will be draw on top (and so would have require to overwrite velocity).
            // Final combination is:
            // Prepass: DoesntReceiveSSR,  DecalsForwardOutputNormalBuffer
            // Motion vectors: DoesntReceiveSSR,  DecalsForwardOutputNormalBuffer, ObjectVelocity
            // Forward: LightingMask

            int stencilRef = (int)StencilLightingUsage.NoLighting;
            int stencilWriteMask = (int)HDRenderPipeline.StencilBitMask.LightingMask;
            int stencilRefDepth = (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR;
            int stencilWriteMaskDepth = (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR | (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer;
            int stencilRefMV = (int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors | (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR;
            int stencilWriteMaskMV = (int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors | (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR | (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer;

            // As we tag both during velocity pass and Gbuffer pass we need a separate state and we need to use the write mask
            material.SetInt(kStencilRef, stencilRef);
            material.SetInt(kStencilWriteMask, stencilWriteMask);
            material.SetInt(kStencilRefDepth, stencilRefDepth);
            material.SetInt(kStencilWriteMaskDepth, stencilWriteMaskDepth);
            material.SetInt(kStencilRefMV, stencilRefMV);
            material.SetInt(kStencilWriteMaskMV, stencilWriteMaskMV);
            material.SetInt(kStencilRefDistortionVec, (int)HDRenderPipeline.StencilBitMask.DistortionVectors);
            material.SetInt(kStencilWriteMaskDistortionVec, (int)HDRenderPipeline.StencilBitMask.DistortionVectors);
        }
    }
} // namespace UnityEditor
