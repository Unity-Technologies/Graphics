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
        //unsupported for now: BTF,
    }
    internal enum SvbrdfDiffuseType
    {
        LAMBERT = 0,
        OREN_NAYAR = 1,
    }
    internal enum SvbrdfSpecularType
    {
        WARD = 0,
        BLINN_PHONG = 1,
        COOK_TORRANCE = 2,
        GGX = 3,
        PHONG = 4,
    }
    internal enum SvbrdfSpecularVariantWard   // Ward variants
    {
        GEISLERMORODER,     // 2010 (albedo-conservative, should always be preferred!)
        DUER,               // 2006
        WARD,               // 1992 (original paper)
    }
    internal enum SvbrdfSpecularVariantBlinn  // Blinn-Phong variants
    {
        ASHIKHMIN_SHIRLEY,  // 2000
        BLINN,              // 1977 (original paper)
        VRAY,
        LEWIS,              // 1993
    }
    internal enum SvbrdfFresnelVariant
    {
        NO_FRESNEL,         // No fresnel
        FRESNEL,            // Full fresnel (1818)
        SCHLICK,            // Schlick's Approximation (1994)
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
    class AxFGUI : HDShaderGUI
    {
        // protected override uint defaultExpandedState { get { return (uint)(Expandable.Base | Expandable.Detail | Expandable.Emissive | Expandable.Input | Expandable.Other | Expandable.Tesselation | Expandable.Transparency | Expandable.VertexAnimation); } }

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.ExpandableBit.Base,
                features: SurfaceOptionUIBlock.Features.Surface | SurfaceOptionUIBlock.Features.BlendMode | SurfaceOptionUIBlock.Features.DoubleSided |
                SurfaceOptionUIBlock.Features.AlphaCutoff |  SurfaceOptionUIBlock.Features.AlphaCutoffShadowThreshold | SurfaceOptionUIBlock.Features.DoubleSidedNormalMode |
                SurfaceOptionUIBlock.Features.ReceiveSSR | SurfaceOptionUIBlock.Features.ReceiveDecal | SurfaceOptionUIBlock.Features.PreserveSpecularLighting
            ),
            new AxfMainSurfaceInputsUIBlock(MaterialUIBlock.ExpandableBit.Input),
            new AxfSurfaceInputsUIBlock(MaterialUIBlock.ExpandableBit.Other),
            new AdvancedOptionsUIBlock(MaterialUIBlock.ExpandableBit.Advance, AdvancedOptionsUIBlock.Features.Instancing | AdvancedOptionsUIBlock.Features.SpecularOcclusion | AdvancedOptionsUIBlock.Features.AddPrecomputedVelocity),
        };

        public override void ValidateMaterial(Material material) => SetupAxFKeywordsAndPass(material);

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            uiBlocks.OnGUI(materialEditor, props);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // AxF material keywords
        const string kIntPropAsFloatSuffix = "F"; // for _FlagsF _SVBRDF_BRDFTypeF _SVBRDF_BRDFVariantsF _CarPaint2_FlakeMaxThetaIF _CarPaint2_FlakeNumThetaFF _CarPaint2_FlakeNumThetaIF
        const string kFlags = "_Flags";
        const string kFlagsB = "_FlagsB";
        const string kSVBRDF_BRDFType = "_SVBRDF_BRDFType";
        const string kSVBRDF_BRDFVariants = "_SVBRDF_BRDFVariants";

        const string kSVBRDF_BRDFType_DiffuseType = "_SVBRDF_BRDFType_DiffuseType";
        const string kSVBRDF_BRDFType_SpecularType = "_SVBRDF_BRDFType_SpecularType";
        const string kSVBRDF_BRDFVariants_FresnelType = "_SVBRDF_BRDFVariants_FresnelType";
        const string kSVBRDF_BRDFVariants_WardType = "_SVBRDF_BRDFVariants_WardType";
        const string kSVBRDF_BRDFVariants_BlinnType = "_SVBRDF_BRDFVariants_BlinnType";

        const string kCarPaint2_FlakeMaxThetaI = "_CarPaint2_FlakeMaxThetaI";
        const string kCarPaint2_FlakeNumThetaF = "_CarPaint2_FlakeNumThetaF";
        const string kCarPaint2_FlakeNumThetaI = "_CarPaint2_FlakeNumThetaI";

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
                float X, Y, Z, W;
                X = (mappingMode == AxFMappingMode.UV0) ? 1.0f : 0.0f;
                Y = (mappingMode == AxFMappingMode.UV1) ? 1.0f : 0.0f;
                Z = (mappingMode == AxFMappingMode.UV2) ? 1.0f : 0.0f;
                W = (mappingMode == AxFMappingMode.UV3) ? 1.0f : 0.0f;
                mask = new Vector4(X, Y, Z, W);
            }
            else if (mappingMode < AxFMappingMode.Triplanar)
            {
                float X, Y, Z, W;
                X = (mappingMode == AxFMappingMode.PlanarYZ) ? 1.0f : 0.0f;
                Y = (mappingMode == AxFMappingMode.PlanarZX) ? 1.0f : 0.0f;
                Z = (mappingMode == AxFMappingMode.PlanarXY) ? 1.0f : 0.0f;
                W = 0.0f;
                mask = new Vector4(X, Y, Z, W);
            }
            return mask;
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupAxFKeywordsAndPass(Material material)
        {
            material.SetupBaseUnlitKeywords();
            material.SetupBaseUnlitPass();

            AxfBrdfType BRDFType = (AxfBrdfType)material.GetFloat(kAxF_BRDFType);

            CoreUtils.SetKeyword(material, "_AXF_BRDF_TYPE_SVBRDF", BRDFType == AxfBrdfType.SVBRDF);
            CoreUtils.SetKeyword(material, "_AXF_BRDF_TYPE_CAR_PAINT", BRDFType == AxfBrdfType.CAR_PAINT);
            //unsupported for now: CoreUtils.SetKeyword(material, "_AXF_BRDF_TYPE_BTF", BRDFType == AxfBrdfType.BTF);


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
            CoreUtils.SetKeyword(material, "_DISABLE_DECALS", !decalsEnabled);

            bool ssrEnabled = false;
            if (material.GetSurfaceType() == SurfaceType.Transparent)
                ssrEnabled = material.HasProperty(kReceivesSSRTransparent) ? material.GetFloat(kReceivesSSRTransparent) != 0 : false;
            else
                ssrEnabled = material.HasProperty(kReceivesSSR) ? material.GetFloat(kReceivesSSR) != 0 : false;
            CoreUtils.SetKeyword(material, "_DISABLE_SSR", material.HasProperty(kReceivesSSR) && material.GetFloat(kReceivesSSR) == 0.0f);
            CoreUtils.SetKeyword(material, "_DISABLE_SSR_TRANSPARENT", material.HasProperty(kReceivesSSRTransparent) && material.GetFloat(kReceivesSSRTransparent) == 0.0);
            CoreUtils.SetKeyword(material, "_ENABLE_GEOMETRIC_SPECULAR_AA", material.HasProperty(kEnableGeometricSpecularAA) && material.GetFloat(kEnableGeometricSpecularAA) > 0.0f);
            CoreUtils.SetKeyword(material, "_SPECULAR_OCCLUSION_NONE", material.HasProperty(kSpecularOcclusionMode) && material.GetFloat(kSpecularOcclusionMode) == 0.0f);

            BaseLitGUI.SetupStencil(material, receivesSSR: ssrEnabled, useSplitLighting: false);
            //
            // Patch for raytracing for now: mirror int props as float explicitly
            //
            uint flags = (uint)material.GetFloat(kFlags);
            flags |= (uint)AxF.FeatureFlags.AxfDebugTest; // force bit 23 = 1
            material.SetFloat(kFlagsB, flags);

            uint SVBRDFType = (uint)material.GetFloat(kSVBRDF_BRDFType);
            uint SVBRDFVariants = (uint)material.GetFloat(kSVBRDF_BRDFVariants);

            SvbrdfDiffuseType diffuseType = (SvbrdfDiffuseType)(SVBRDFType & 0x1);
            SvbrdfSpecularType specularType = (SvbrdfSpecularType)((SVBRDFType >> 1) & 0x7);
            SvbrdfFresnelVariant fresnelVariant = (SvbrdfFresnelVariant)(SVBRDFVariants & 0x3);
            SvbrdfSpecularVariantWard wardVariant = (SvbrdfSpecularVariantWard)((SVBRDFVariants >> 2) & 0x3);
            SvbrdfSpecularVariantBlinn blinnVariant = (SvbrdfSpecularVariantBlinn)((SVBRDFVariants >> 4) & 0x3);

            material.SetFloat(kSVBRDF_BRDFType_DiffuseType, (float)diffuseType);
            material.SetFloat(kSVBRDF_BRDFType_SpecularType, (float)specularType);
            material.SetFloat(kSVBRDF_BRDFVariants_FresnelType, (float)fresnelVariant);
            material.SetFloat(kSVBRDF_BRDFVariants_WardType, (float)wardVariant);
            material.SetFloat(kSVBRDF_BRDFVariants_BlinnType, (float)blinnVariant);

            material.SetFloat(kCarPaint2_FlakeMaxThetaI + kIntPropAsFloatSuffix, material.GetFloat(kCarPaint2_FlakeMaxThetaI));
            material.SetFloat(kCarPaint2_FlakeNumThetaF + kIntPropAsFloatSuffix, material.GetFloat(kCarPaint2_FlakeNumThetaF));
            material.SetFloat(kCarPaint2_FlakeNumThetaI + kIntPropAsFloatSuffix, material.GetFloat(kCarPaint2_FlakeNumThetaI));
        }
    }
} // namespace UnityEditor
