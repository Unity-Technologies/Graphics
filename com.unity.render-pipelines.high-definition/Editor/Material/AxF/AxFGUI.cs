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

    internal enum AxFMappingMode
    {
        UV0,
        UV1,
        UV2,
        UV3,
        PlanarXY,
        PlanarYZ,
        PlanarZX,
        Triplanar,
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
            new AdvancedOptionsUIBlock(MaterialUIBlock.Expandable.Advance, AdvancedOptionsUIBlock.Features.Instancing | AdvancedOptionsUIBlock.Features.SpecularOcclusion | AdvancedOptionsUIBlock.Features.AddPrecomputedVelocity),
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
        const string kAxF_BRDFType = "_AxF_BRDFType";
        const string kEnableGeometricSpecularAA = "_EnableGeometricSpecularAA";
        const string kSpecularOcclusionMode = "_SpecularOcclusionMode"; // match AdvancedOptionsUIBlock.kSpecularOcclusionMode : TODO move both to HDStringConstants.

        const string kMappingMode = "_MappingMode";
        const string kMappingMask = "_MappingMask";
        const string kPlanarSpace = "_PlanarSpace";

        static public Vector4 AxFMappingModeToMask(AxFMappingMode mappingMode)
        {
            Vector4 mask = Vector4.zero;
            if (mappingMode <= AxFMappingMode.UV3)
            {
                float X,Y,Z,W;
                X = (mappingMode == AxFMappingMode.UV0) ? 1.0f : 0.0f;
                Y = (mappingMode == AxFMappingMode.UV1) ? 1.0f : 0.0f;
                Z = (mappingMode == AxFMappingMode.UV2) ? 1.0f : 0.0f;
                W = (mappingMode == AxFMappingMode.UV3) ? 1.0f : 0.0f;
                mask = new Vector4(X, Y, Z, W);
            }
            else if (mappingMode < AxFMappingMode.Triplanar)
            {
                float X,Y,Z,W;
                X = (mappingMode == AxFMappingMode.PlanarYZ) ? 1.0f : 0.0f;
                Y = (mappingMode == AxFMappingMode.PlanarZX) ? 1.0f : 0.0f;
                Z = (mappingMode == AxFMappingMode.PlanarXY) ? 1.0f : 0.0f;
                W = 0.0f;
                mask = new Vector4(X, Y, Z, W);
            }
            return mask;
        }
        
        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            material.SetupBaseUnlitKeywords();
            material.SetupBaseUnlitPass();

            AxfBrdfType BRDFType = (AxfBrdfType)material.GetFloat(kAxF_BRDFType);

            CoreUtils.SetKeyword(material, "_AXF_BRDF_TYPE_SVBRDF", BRDFType == AxfBrdfType.SVBRDF);
            CoreUtils.SetKeyword(material, "_AXF_BRDF_TYPE_CAR_PAINT", BRDFType == AxfBrdfType.CAR_PAINT);
            CoreUtils.SetKeyword(material, "_AXF_BRDF_TYPE_BTF", BRDFType == AxfBrdfType.BTF);


            // Mapping Modes:
            AxFMappingMode mappingMode = (AxFMappingMode)material.GetFloat(kMappingMode);

            // Make sure the mask is synched:
            material.SetVector(kMappingMask, AxFMappingModeToMask(mappingMode));

            bool mappingIsPlanar = (mappingMode >= AxFMappingMode.PlanarXY) && (mappingMode < AxFMappingMode.Triplanar);
            bool planarIsLocal = (material.GetFloat(kPlanarSpace) > 0.0f);

            CoreUtils.SetKeyword(material, "_MAPPING_PLANAR", mappingIsPlanar);
            CoreUtils.SetKeyword(material, "_MAPPING_TRIPLANAR", mappingMode == AxFMappingMode.Triplanar);

            if (mappingIsPlanar || mappingMode == AxFMappingMode.Triplanar)
            {
                CoreUtils.SetKeyword(material, "_PLANAR_LOCAL", planarIsLocal);
            }

            // Note: for ShaderPass defines for vertmesh/varyingmesh setup, we still use the same 
            // defines _REQUIRE_UV2 and _REQUIRE_UV3, and thus if eg _REQUIRE_UV3 is defined, _REQUIRE_UV2 will
            // be assumed to be needed. But here in the AxFData sampling code, we use these to indicate precisely
            // the single set used (if not using planar/triplanar) only and thus add _REQUIRE_UV1.
            // Extra UVs might be transfered but we only need and support a single set at a time for the whole material.
            CoreUtils.SetKeyword(material, "_REQUIRE_UV1", mappingMode == AxFMappingMode.UV1);
            CoreUtils.SetKeyword(material, "_REQUIRE_UV2", mappingMode == AxFMappingMode.UV2);
            CoreUtils.SetKeyword(material, "_REQUIRE_UV3", mappingMode == AxFMappingMode.UV3);

            // Keywords for opt-out of decals and SSR:
            bool decalsEnabled = material.HasProperty(kEnableDecals) && material.GetFloat(kEnableDecals) > 0.0f;
            CoreUtils.SetKeyword(material, "_DISABLE_DECALS", decalsEnabled == false);
            bool ssrEnabled = material.HasProperty(kEnableSSR) && material.GetFloat(kEnableSSR) > 0.0f;
            CoreUtils.SetKeyword(material, "_DISABLE_SSR", ssrEnabled == false);
            CoreUtils.SetKeyword(material, "_ENABLE_GEOMETRIC_SPECULAR_AA", material.HasProperty(kEnableGeometricSpecularAA) && material.GetFloat(kEnableGeometricSpecularAA) > 0.0f);
            CoreUtils.SetKeyword(material, "_SPECULAR_OCCLUSION_NONE", material.HasProperty(kSpecularOcclusionMode) && material.GetFloat(kSpecularOcclusionMode) == 0.0f);

            BaseLitGUI.SetupStencil(material, receivesSSR: ssrEnabled, useSplitLighting: false);

            if (material.HasProperty(kAddPrecomputedVelocity))
            {
                CoreUtils.SetKeyword(material, "_ADD_PRECOMPUTED_VELOCITY", material.GetInt(kAddPrecomputedVelocity) != 0);
            }

        }
    }
} // namespace UnityEditor
