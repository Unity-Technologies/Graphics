using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
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
            new SurfaceOptionUIBlock(MaterialUIBlock.Expandable.Base, features: SurfaceOptionUIBlock.Features.Unlit | SurfaceOptionUIBlock.Features.ReceiveSSR),
            new AxfSurfaceInputsUIBlock(MaterialUIBlock.Expandable.Input),
            new AdvancedOptionsUIBlock(MaterialUIBlock.Expandable.Advance, AdvancedOptionsUIBlock.Features.Instancing | AdvancedOptionsUIBlock.Features.AddPrecomputedVelocity),
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

            BaseLitGUI.SetupStencil(material, receivesSSR: ssrEnabled, useSplitLighting: false);

            if (material.HasProperty(kAddPrecomputedVelocity))
            {
                CoreUtils.SetKeyword(material, "_ADD_PRECOMPUTED_VELOCITY", material.GetInt(kAddPrecomputedVelocity) != 0);
            }

        }
    }
} // namespace UnityEditor
