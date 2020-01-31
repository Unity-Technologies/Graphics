using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal enum AxfBrdfType
    {
        SVBRDF,
        CAR_PAINT,
        BTF,
    }
        
    /// <summary>
    /// GUI for HDRP AxF materials
    /// </summary>
    class AxFGUI : ShaderGUI
    {
        // protected override uint defaultExpandedState { get { return (uint)(Expandable.Base | Expandable.Detail | Expandable.Emissive | Expandable.Input | Expandable.Other | Expandable.Tesselation | Expandable.Transparency | Expandable.VertexAnimation); } }

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.Expandable.Base, features: SurfaceOptionUIBlock.Features.Unlit),
            new AxfSurfaceInputsUIBlock(MaterialUIBlock.Expandable.Input),
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
                        SetupMaterialKeywordsAndPass(material);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // AxF material keywords
        static string               m_AxF_BRDFTypeText = "_AxF_BRDFType";

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            material.SetupBaseUnlitKeywords();
            material.SetupBaseUnlitPass();

            AxfBrdfType   BRDFType = (AxfBrdfType)material.GetFloat(m_AxF_BRDFTypeText);

            CoreUtils.SetKeyword(material, "_AXF_BRDF_TYPE_SVBRDF", BRDFType == AxfBrdfType.SVBRDF);
            CoreUtils.SetKeyword(material, "_AXF_BRDF_TYPE_CAR_PAINT", BRDFType == AxfBrdfType.CAR_PAINT);
            CoreUtils.SetKeyword(material, "_AXF_BRDF_TYPE_BTF", BRDFType == AxfBrdfType.BTF);

            // Keywords for opt-out of decals and SSR:
            bool decalsEnabled = material.HasProperty(kEnableDecals) && material.GetFloat(kEnableDecals) > 0.0f;
            CoreUtils.SetKeyword(material, "_DISABLE_DECALS", decalsEnabled == false);
            bool ssrEnabled = material.HasProperty(kEnableSSR) && material.GetFloat(kEnableSSR) > 0.0f;
            CoreUtils.SetKeyword(material, "_DISABLE_SSR", ssrEnabled == false);

            // Set the reference values for the stencil test

            // Stencil usage rules:
            // DoesntReceiveSSR and DecalsForwardOutputNormalBuffer need to be tagged during depth prepass
            // LightingMask need to be tagged during either GBuffer or Forward pass
            // ObjectMotionVectors need to be tagged in motion vectors pass.
            // As motion vectors pass can be use as a replacement of depth prepass it also need to have DoesntReceiveSSR and DecalsForwardOutputNormalBuffer
            // Object motion vectors is always render after a full depth buffer (if there is no depth prepass for GBuffer all object motion vectors are render after GBuffer)
            // so we have a guarantee than when we write object motion vectors no other object will be draw on top (and so would have require to overwrite motion vectors).
            // Final combination is:
            // Prepass: DoesntReceiveSSR,  DecalsForwardOutputNormalBuffer
            // Motion vectors: DoesntReceiveSSR,  DecalsForwardOutputNormalBuffer, ObjectMotionVectors
            // GBuffer: LightingMask, DecalsForwardOutputNormalBuffer, ObjectMotionVectors
            // Forward: LightingMask

            int stencilRef = (int)StencilLightingUsage.RegularLighting;
            int stencilWriteMask = (int)HDRenderPipeline.StencilBitMask.LightingMask;
            int stencilRefDepth = 0;
            int stencilWriteMaskDepth = 0;
            int stencilRefMV = (int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors;
            int stencilWriteMaskMV = (int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors;

            if (!ssrEnabled)
            {
                stencilRefDepth |= (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR;
                stencilRefMV |= (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR;
            }

            if (decalsEnabled)
            {
                stencilRefDepth |= (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer;
                stencilRefMV |= (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer;
            }

            stencilWriteMaskDepth |= (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR | (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer;
            stencilWriteMaskMV |= (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR | (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer;

            // As we tag both during motion vector pass and Gbuffer pass we need a separate state and we need to use the write mask
            material.SetInt(kStencilRef, stencilRef);
            material.SetInt(kStencilWriteMask, stencilWriteMask);
            material.SetInt(kStencilRefDepth, stencilRefDepth);
            material.SetInt(kStencilWriteMaskDepth, stencilWriteMaskDepth);
            material.SetInt(kStencilRefMV, stencilRefMV);
            material.SetInt(kStencilWriteMaskMV, stencilWriteMaskMV);
        }
    }
} // namespace UnityEditor
