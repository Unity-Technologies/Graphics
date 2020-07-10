using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// GUI for HDRP Shadergraph Decal materials
    /// </summary>
    class DecalGUI : HDShaderGUI
    {
        [Flags]
        enum Expandable : uint
        {
            Sorting = 1 << 0,
            ShaderGraph = 1 << 1,
            SurfaceInputs = 1 << 2,
        }

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new ShaderGraphDecalSurfaceInputsUIBlock((MaterialUIBlock.Expandable)Expandable.SurfaceInputs),
            new ShaderGraphUIBlock((MaterialUIBlock.Expandable)Expandable.ShaderGraph, ShaderGraphUIBlock.Features.None),
            new DecalSortingInputsUIBlock((MaterialUIBlock.Expandable)Expandable.Sorting),
        };

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            // always instanced
            SerializedProperty instancing = materialEditor.serializedObject.FindProperty("m_EnableInstancingVariants");
            instancing.boolValue = true;

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                uiBlocks.OnGUI(materialEditor, props);
                ApplyKeywordsAndPassesIfNeeded(changed.changed, uiBlocks.materials);
            }

            // We should always do this call at the end
            materialEditor.serializedObject.ApplyModifiedProperties();
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            if (material.HasProperty(kAffectsAlbedo))
                CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_ALBEDO", material.GetFloat(kAffectsAlbedo) == 1.0f);
            if (material.HasProperty(kAffectsNormal))
                CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_NORMAL", material.GetFloat(kAffectsNormal) == 1.0f);
            if (material.HasProperty(kAffectsMetal))
                CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_MASKMAP", material.GetFloat(kAffectsMetal) == 1.0f);
            if (material.HasProperty(kAffectsAO))
                CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_MASKMAP", material.GetFloat(kAffectsAO) == 1.0f);
            if (material.HasProperty(kAffectsSmoothness))
                CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_MASKMAP", material.GetFloat(kAffectsSmoothness) == 1.0f);
            if (material.HasProperty(kAffectsEmission))
                CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_EMISSION", material.GetFloat(kAffectsEmission) == 1.0f);

            // Setup color mask for mask map
            Decal.MaskBlendFlags flags = 0;
            if (material.HasProperty(kAffectsMetal) && material.GetFloat(kAffectsMetal) == 1.0f)
                flags |= Decal.MaskBlendFlags.Metal;
            if (material.HasProperty(kAffectsAO) && material.GetFloat(kAffectsAO) == 1.0f)
                flags |= Decal.MaskBlendFlags.AO;
            if (material.HasProperty(kAffectsSmoothness) && material.GetFloat(kAffectsSmoothness) == 1.0f)
                flags |= Decal.MaskBlendFlags.Smoothness;

            SetupColorMaskProperties(material, flags);
        }

        internal static void SetupColorMaskProperties(Material material, Decal.MaskBlendFlags flags)
        {
            ColorWriteMask mask2 = 0, mask3 = 0;
            if ((flags & Decal.MaskBlendFlags.Metal) != 0)
                mask2 |= mask3 |= ColorWriteMask.Red;
            if ((flags & Decal.MaskBlendFlags.AO) != 0)
                mask2 |= mask3 |= ColorWriteMask.Green;
            if ((flags & Decal.MaskBlendFlags.Smoothness) != 0)
                mask2 |= ColorWriteMask.Blue | ColorWriteMask.Alpha;

            material.SetInt(HDShaderIDs._DecalColorMask2, (int)mask2);
            material.SetInt(HDShaderIDs._DecalColorMask3, (int)mask3);
        }

        // We don't have any keyword/pass to setup currently for decal shader graphs
        protected override void SetupMaterialKeywordsAndPassInternal(Material material) => SetupMaterialKeywordsAndPass(material);
    }
}
