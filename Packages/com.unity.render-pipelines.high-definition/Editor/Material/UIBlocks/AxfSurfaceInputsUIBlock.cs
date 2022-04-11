using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// Include material common properties names
namespace UnityEditor.Rendering.HighDefinition
{
    // We don't reuse the other surface option ui block, AxF is too different
    class AxfSurfaceInputsUIBlock : MaterialUIBlock
    {
        public class Styles
        {
            public static GUIContent header { get; } = EditorGUIUtility.TrTextContent("Advanced Surface Inputs");

            public static GUIContent BRDFTypeText = new GUIContent("BRDF Type");
            public static GUIContent mapsTilingOffsetText = new GUIContent("Tiling and Offset for Map", "XY scales, ZW offsets");
            /////////////////////////////////////////////////////////////////////////////////////////////////
            // SVBRDF Parameters
            public static GUIContent diffuseColorMapText = new GUIContent("Diffuse Color");
            public static GUIContent specularColorMapText = new GUIContent("Specular Color");
            public static GUIContent specularLobeMapText = new GUIContent("Specular Lobe", "Represents the lobe roughnesses");
            public static GUIContent specularLobeMapScaleText = new GUIContent("Specular Lobe Scale", "Multiplying scale for specular lobe");
            public static GUIContent fresnelMapText = new GUIContent("Fresnel", "Fresnel0 map");
            public static GUIContent normalMapText = new GUIContent("Normal");

            // Alpha
            public static GUIContent alphaMapText = new GUIContent("Alpha");

            // Displacement
            public static GUIContent heightMapText = new GUIContent("Height");

            // Anisotropy
            public static GUIContent anisoRotationMapText = new GUIContent("Anisotropy Angle");

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // Car Paint Parameters
            public static GUIContent BRDFColorMapText = new GUIContent("BRDF Color", "Angle varying measured color modulation table");
            public static GUIContent BRDFColorMapScaleText = new GUIContent("BRDF Color Scale", "Multiplying factor for color fetched from BRDFColor table");
            public static GUIContent BRDFColorMapUVScaleText = new GUIContent("BRDF Color Map UV scale restriction", "Restrict valid domain of BRDFColor table");

            public static GUIContent BTFFlakesMapText = new GUIContent("BTF Flakes Texture2DArray", "Flake slices distributed in angular space");
            public static GUIContent BTFFlakesMapScaleText = new GUIContent("BTF Flakes Scale", "Multiplying factor for the flake intensity");
            public static GUIContent BTFFlakesTilingText = new GUIContent("BTF Flakes Tiling and Offset", "X,Y scales for tiling control, Z,W for offsets");

            public static GUIContent thetaFI_sliceLUTMapText = new GUIContent("ThetaFI Slice LUT", "First angular dimension indirection for flake slice number");

            public static GUIContent CarPaintFixedColorThetaHForIndirectLightText = new GUIContent("BRDFColor ThetaH For Indirect Light", "Select a fixed angle between normal and half-vector for indirect lighting, when this angle is unknown, to be used for the BRDF color table: "
                + "The value is an angle from 0 to PI/2."
                + "eg this will select a hue column in the BRDF color table for indirect reflection probes and raytraced indirect light");

            public static GUIContent CarPaintFixedFlakesThetaHForIndirectLightText = new GUIContent("Flakes ThetaH For Indirect Light", "Select a fixed angle between normal and half-vector for indirect lighting, when this angle is unknown, to be used for the flakes: "
                + "A value between 0 and 1 selects an angle from 0 to PI/2. "
                + "This allows one to control visibility of flakes lit from indirect lighting more precisely when lit by reflection probes and raytraced indirect light");
            public static GUIContent CarPaintIORText = new GUIContent("Clearcoat IOR");

            public static GUIContent CarPaintCTDiffuseText = new GUIContent("Diffuse coeff");
            public static GUIContent CarPaintLobeCountText = new GUIContent("CT Lobes count");
            public static GUIContent CarPaintCTF0sText = new GUIContent("CT Lobes F0s", "Fresnel0 values of 3 lobes stored in x, y and z");
            public static GUIContent CarPaintCTCoeffsText = new GUIContent("CT Lobes coeffs", "Weight multipliers for 3 lobes stored in x, y and z");
            public static GUIContent CarPaintCTSpreadsText = new GUIContent("CT Lobes spreads", "Roughnesses for 3 lobes stored in x, y and z");

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // Generic

            // Clearcoat
            public static GUIContent clearcoatColorMapText = new GUIContent("Clearcoat Color");
            public static GUIContent clearcoatNormalMapText = new GUIContent("Clearcoat Normal");
            public static GUIContent clearcoatNormalMapTilingText = new GUIContent("Clearcoat Normal Tiling and Offset");
            public static GUIContent clearcoatIORMapText = new GUIContent("Clearcoat IOR");
        }

        static readonly string[] AxfBrdfTypeNames = Enum.GetNames(typeof(AxfBrdfType));
        static readonly string[] SvbrdfDiffuseTypeNames = Enum.GetNames(typeof(SvbrdfDiffuseType));
        static readonly string[] SvbrdfSpecularTypeNames = Enum.GetNames(typeof(SvbrdfSpecularType));
        static readonly string[] SvbrdfSpecularVariantWardNames = Enum.GetNames(typeof(SvbrdfSpecularVariantWard));
        static readonly string[] SvbrdfSpecularVariantBlinnNames = Enum.GetNames(typeof(SvbrdfSpecularVariantBlinn));
        static readonly string[] SvbrdfFresnelVariantNames = Enum.GetNames(typeof(SvbrdfFresnelVariant));

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Generic Parameters

        MaterialProperty m_DiffuseColorMapST = null;
        MaterialProperty m_SpecularColorMapST = null;
        MaterialProperty m_NormalMapST = null;
        MaterialProperty m_SpecularLobeMapST = null;
        MaterialProperty m_AlphaMapST = null;
        MaterialProperty m_FresnelMapST = null;
        MaterialProperty m_AnisoRotationMapST = null;
        MaterialProperty m_HeightMapST = null;
        MaterialProperty m_ClearcoatColorMapST = null;
        MaterialProperty m_ClearcoatNormalMapST = null;
        MaterialProperty m_ClearcoatIORMapST = null;
        MaterialProperty m_CarPaint2_BTFFlakeMapST = null;

        static string tilingOffsetPropNameSuffix = "_SO";

        static string m_AxF_BRDFTypeText = "_AxF_BRDFType";
        MaterialProperty m_AxF_BRDFType = null;

        static string m_FlagsText = "_Flags";
        MaterialProperty m_Flags;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // SVBRDF Parameters
        static string m_SVBRDF_BRDFTypeText = "_SVBRDF_BRDFType";
        MaterialProperty m_SVBRDF_BRDFType;
        static string m_SVBRDF_BRDFVariantsText = "_SVBRDF_BRDFVariants";
        MaterialProperty m_SVBRDF_BRDFVariants;
        static string m_SVBRDF_HeightMapMaxMMText = "_SVBRDF_HeightMapMaxMM";
        MaterialProperty m_SVBRDF_HeightMapMaxMM;

        // Regular maps
        static string m_DiffuseColorMapText = "_SVBRDF_DiffuseColorMap";
        MaterialProperty m_DiffuseColorMap = null;
        static string m_SpecularColorMapText = "_SVBRDF_SpecularColorMap";
        MaterialProperty m_SpecularColorMap = null;

        static string m_SpecularLobeMapText = "_SVBRDF_SpecularLobeMap";
        MaterialProperty m_SpecularLobeMap = null;
        static string m_SpecularLobeMapScaleText = "_SVBRDF_SpecularLobeMapScale";
        MaterialProperty m_SpecularLobeMapScale;

        static string m_FresnelMapText = "_SVBRDF_FresnelMap";
        MaterialProperty m_FresnelMap = null;
        static string m_NormalMapText = "_SVBRDF_NormalMap";
        MaterialProperty m_NormalMap = null;

        // Alpha
        static string m_AlphaMapText = "_SVBRDF_AlphaMap";
        MaterialProperty m_AlphaMap = null;

        // Displacement
        static string m_HeightMapText = "_SVBRDF_HeightMap";
        MaterialProperty m_HeightMap = null;

        // Anisotropy
        static string m_AnisoRotationMapText = "_SVBRDF_AnisoRotationMap";
        MaterialProperty m_AnisoRotationMap = null;


        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Car Paint Parameters
        static string m_CarPaint2_BRDFColorMapText = "_CarPaint2_BRDFColorMap";
        MaterialProperty m_CarPaint2_BRDFColorMap = null;

        static string m_CarPaint2_BRDFColorMapScaleText = "_CarPaint2_BRDFColorMapScale";
        MaterialProperty m_CarPaint2_BRDFColorMapScale;

        static string m_CarPaint2_BRDFColorMapUVScaleText = "_CarPaint2_BRDFColorMapUVScale";
        MaterialProperty m_CarPaint2_BRDFColorMapUVScale;

        static string m_CarPaint2_BTFFlakeMapText = "_CarPaint2_BTFFlakeMap";
        MaterialProperty m_CarPaint2_BTFFlakeMap = null;

        static string m_CarPaint2_BTFFlakeMapScaleText = "_CarPaint2_BTFFlakeMapScale";
        MaterialProperty m_CarPaint2_BTFFlakeMapScale;

        static string m_CarPaint2_FlakeThetaFISliceLUTMapText = "_CarPaint2_FlakeThetaFISliceLUTMap";
        MaterialProperty m_CarPaint2_FlakeThetaFISliceLUTMap;

        static string m_CarPaint2_FlakeMaxThetaIText = "_CarPaint2_FlakeMaxThetaI";
        MaterialProperty m_CarPaint2_FlakeMaxThetaI;
        static string m_CarPaint2_FlakeNumThetaFText = "_CarPaint2_FlakeNumThetaF";
        MaterialProperty m_CarPaint2_FlakeNumThetaF;
        static string m_CarPaint2_FlakeNumThetaIText = "_CarPaint2_FlakeNumThetaI";
        MaterialProperty m_CarPaint2_FlakeNumThetaI;

        static string m_CarPaint2_FixedColorThetaHForIndirectLightText = "_CarPaint2_FixedColorThetaHForIndirectLight";
        MaterialProperty m_CarPaint2_FixedColorThetaHForIndirectLight;
        static string m_CarPaint2_FixedFlakesThetaHForIndirectLightText = "_CarPaint2_FixedFlakesThetaHForIndirectLight";
        MaterialProperty m_CarPaint2_FixedFlakesThetaHForIndirectLight;

        static string m_CarPaint2_ClearcoatIORText = "_CarPaint2_ClearcoatIOR";
        MaterialProperty m_CarPaint2_ClearcoatIOR;

        static string m_CarPaint2_CTDiffuseText = "_CarPaint2_CTDiffuse";
        MaterialProperty m_CarPaint2_CTDiffuse;
        static string m_CarPaint2_LobeCountText = "_CarPaint2_LobeCount";
        MaterialProperty m_CarPaint2_LobeCount;
        static string m_CarPaint2_CTF0sText = "_CarPaint2_CTF0s";
        MaterialProperty m_CarPaint2_CTF0s;
        static string m_CarPaint2_CTCoeffsText = "_CarPaint2_CTCoeffs";
        MaterialProperty m_CarPaint2_CTCoeffs;
        static string m_CarPaint2_CTSpreadsText = "_CarPaint2_CTSpreads";
        MaterialProperty m_CarPaint2_CTSpreads;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Clearcoat
        static string m_ClearcoatColorMapText = "_SVBRDF_ClearcoatColorMap";
        MaterialProperty m_ClearcoatColorMap = null;
        static string m_ClearcoatNormalMapText = "_ClearcoatNormalMap";
        MaterialProperty m_ClearcoatNormalMap = null;
        static string m_ClearcoatIORMapText = "_SVBRDF_ClearcoatIORMap";
        MaterialProperty m_ClearcoatIORMap = null;

        public AxfSurfaceInputsUIBlock(ExpandableBit expandableBit)
            : base(expandableBit, Styles.header)
        {
        }

        public override void LoadMaterialProperties()
        {
            m_DiffuseColorMapST = FindProperty(m_DiffuseColorMapText + tilingOffsetPropNameSuffix);
            m_SpecularColorMapST = FindProperty(m_SpecularColorMapText + tilingOffsetPropNameSuffix);
            m_NormalMapST = FindProperty(m_NormalMapText + tilingOffsetPropNameSuffix);
            m_SpecularLobeMapST = FindProperty(m_SpecularLobeMapText + tilingOffsetPropNameSuffix);
            m_AlphaMapST = FindProperty(m_AlphaMapText + tilingOffsetPropNameSuffix);
            m_FresnelMapST = FindProperty(m_FresnelMapText + tilingOffsetPropNameSuffix);
            m_AnisoRotationMapST = FindProperty(m_AnisoRotationMapText + tilingOffsetPropNameSuffix);
            m_HeightMapST = FindProperty(m_HeightMapText + tilingOffsetPropNameSuffix);
            m_ClearcoatColorMapST = FindProperty(m_ClearcoatColorMapText + tilingOffsetPropNameSuffix);
            m_ClearcoatNormalMapST = FindProperty(m_ClearcoatNormalMapText + tilingOffsetPropNameSuffix);
            m_ClearcoatIORMapST = FindProperty(m_ClearcoatIORMapText + tilingOffsetPropNameSuffix);
            m_CarPaint2_BTFFlakeMapST = FindProperty(m_CarPaint2_BTFFlakeMapText + tilingOffsetPropNameSuffix);

            m_AxF_BRDFType = FindProperty(m_AxF_BRDFTypeText);

            m_Flags = FindProperty(m_FlagsText);
            //////////////////////////////////////////////////////////////////////////
            // SVBRDF
            m_SVBRDF_BRDFType = FindProperty(m_SVBRDF_BRDFTypeText);
            m_SVBRDF_BRDFVariants = FindProperty(m_SVBRDF_BRDFVariantsText);
            m_SVBRDF_HeightMapMaxMM = FindProperty(m_SVBRDF_HeightMapMaxMMText);

            // Regular maps
            m_DiffuseColorMap = FindProperty(m_DiffuseColorMapText);
            m_SpecularColorMap = FindProperty(m_SpecularColorMapText);
            m_SpecularLobeMap = FindProperty(m_SpecularLobeMapText);
            m_SpecularLobeMapScale = FindProperty(m_SpecularLobeMapScaleText);
            m_FresnelMap = FindProperty(m_FresnelMapText);
            m_NormalMap = FindProperty(m_NormalMapText);

            // Alpha
            m_AlphaMap = FindProperty(m_AlphaMapText);

            // Displacement
            m_HeightMap = FindProperty(m_HeightMapText);

            // Anisotropy
            m_AnisoRotationMap = FindProperty(m_AnisoRotationMapText);


            //////////////////////////////////////////////////////////////////////////
            // Car Paint
            m_CarPaint2_BRDFColorMap = FindProperty(m_CarPaint2_BRDFColorMapText);
            m_CarPaint2_BTFFlakeMap = FindProperty(m_CarPaint2_BTFFlakeMapText);
            m_CarPaint2_FlakeThetaFISliceLUTMap = FindProperty(m_CarPaint2_FlakeThetaFISliceLUTMapText);

            m_CarPaint2_BRDFColorMapScale = FindProperty(m_CarPaint2_BRDFColorMapScaleText);
            m_CarPaint2_BRDFColorMapUVScale = FindProperty(m_CarPaint2_BRDFColorMapUVScaleText);
            m_CarPaint2_BTFFlakeMapScale = FindProperty(m_CarPaint2_BTFFlakeMapScaleText);

            m_CarPaint2_FlakeMaxThetaI = FindProperty(m_CarPaint2_FlakeMaxThetaIText);
            m_CarPaint2_FlakeNumThetaF = FindProperty(m_CarPaint2_FlakeNumThetaFText);
            m_CarPaint2_FlakeNumThetaI = FindProperty(m_CarPaint2_FlakeNumThetaIText);
            m_CarPaint2_FixedColorThetaHForIndirectLight = FindProperty(m_CarPaint2_FixedColorThetaHForIndirectLightText);
            m_CarPaint2_FixedFlakesThetaHForIndirectLight = FindProperty(m_CarPaint2_FixedFlakesThetaHForIndirectLightText);

            m_CarPaint2_ClearcoatIOR = FindProperty(m_CarPaint2_ClearcoatIORText);

            m_CarPaint2_CTDiffuse = FindProperty(m_CarPaint2_CTDiffuseText);
            m_CarPaint2_LobeCount = FindProperty(m_CarPaint2_LobeCountText);
            m_CarPaint2_CTF0s = FindProperty(m_CarPaint2_CTF0sText);
            m_CarPaint2_CTCoeffs = FindProperty(m_CarPaint2_CTCoeffsText);
            m_CarPaint2_CTSpreads = FindProperty(m_CarPaint2_CTSpreadsText);

            //////////////////////////////////////////////////////////////////////////
            // Clearcoat
            m_ClearcoatColorMap = FindProperty(m_ClearcoatColorMapText);
            m_ClearcoatNormalMap = FindProperty(m_ClearcoatNormalMapText);
            m_ClearcoatIORMap = FindProperty(m_ClearcoatIORMapText);
        }

        public static uint GenFlags(bool anisotropy = false, bool clearcoat = false, bool clearcoatRefraction = false, bool useHeightMap = false, bool brdfColorDiagonalClamp = false,
            bool honorMinRoughness = false)
        {
            uint flags = 0;
            flags |= anisotropy ? (uint)AxF.FeatureFlags.AxfAnisotropy : 0U;
            flags |= clearcoat ? (uint)AxF.FeatureFlags.AxfClearCoat : 0U;
            flags |= clearcoatRefraction ? (uint)AxF.FeatureFlags.AxfClearCoatRefraction : 0U;
            flags |= useHeightMap ? (uint)AxF.FeatureFlags.AxfUseHeightMap : 0U;
            flags |= brdfColorDiagonalClamp ? (uint)AxF.FeatureFlags.AxfBRDFColorDiagonalClamp : 0U;
            flags |= honorMinRoughness ? (uint)AxF.FeatureFlags.AxfHonorMinRoughness : 0U;
            return flags;
        }

        public static void ExtractFlags(uint flags,
            out bool anisotropy, out bool clearcoat, out bool clearcoatRefraction, out bool useHeightMap, out bool brdfColorDiagonalClamp,
            out bool honorMinRoughness)
        {
            anisotropy = (flags & (uint)AxF.FeatureFlags.AxfAnisotropy) != 0;
            clearcoat = (flags & (uint)AxF.FeatureFlags.AxfClearCoat) != 0;
            clearcoatRefraction = (flags & (uint)AxF.FeatureFlags.AxfClearCoatRefraction) != 0;
            useHeightMap = (flags & (uint)AxF.FeatureFlags.AxfUseHeightMap) != 0;
            brdfColorDiagonalClamp = (flags & (uint)AxF.FeatureFlags.AxfBRDFColorDiagonalClamp) != 0;
            honorMinRoughness = (flags & (uint)AxF.FeatureFlags.AxfHonorMinRoughness) != 0;
        }

        public static void DrawRightJustifiedHeader(string header)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(EditorGUIUtility.labelWidth - 5));
            GUILayout.Label(header, EditorStyles.boldLabel);
            //EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
        protected override void OnGUIOpen()
        {
            materialEditor.PopupShaderProperty(m_AxF_BRDFType, Styles.BRDFTypeText, AxfBrdfTypeNames);

            // Extract flag:
            uint flags = (uint)m_Flags.floatValue;
            ExtractFlags(flags,
                out bool anisotropy, out bool clearcoat, out bool clearcoatRefraction, out bool useHeightMap, out bool brdfColorDiagonalClamp,
                out bool honorMinRoughness);

            switch ((AxfBrdfType)m_AxF_BRDFType.floatValue)
            {
                case AxfBrdfType.SVBRDF:
                {
                    EditorGUILayout.Space();
                    ++EditorGUI.indentLevel;

                    // Read as compact flags
                    //uint    flags = (uint)m_Flags.floatValue;
                    uint BRDFType = (uint)m_SVBRDF_BRDFType.floatValue;
                    uint BRDFVariants = (uint)m_SVBRDF_BRDFVariants.floatValue;

                    SvbrdfDiffuseType diffuseType = (SvbrdfDiffuseType)(BRDFType & 0x1);
                    SvbrdfSpecularType specularType = (SvbrdfSpecularType)((BRDFType >> 1) & 0x7);
                    SvbrdfFresnelVariant fresnelVariant = (SvbrdfFresnelVariant)(BRDFVariants & 0x3);
                    SvbrdfSpecularVariantWard wardVariant = (SvbrdfSpecularVariantWard)((BRDFVariants >> 2) & 0x3);
                    SvbrdfSpecularVariantBlinn blinnVariant = (SvbrdfSpecularVariantBlinn)((BRDFVariants >> 4) & 0x3);

                    // Expand as user-friendly UI
                    EditorGUILayout.LabelField("BRDF Variants", EditorStyles.boldLabel);

                    MaterialEditor.BeginProperty(m_SVBRDF_BRDFType);
                    diffuseType = (SvbrdfDiffuseType)EditorGUILayout.Popup("Diffuse Type", (int)diffuseType, SvbrdfDiffuseTypeNames);
                    specularType = (SvbrdfSpecularType)EditorGUILayout.Popup("Specular Type", (int)specularType, SvbrdfSpecularTypeNames);
                    MaterialEditor.EndProperty();

                    MaterialEditor.BeginProperty(m_SVBRDF_BRDFVariants);
                    if (specularType == SvbrdfSpecularType.WARD)
                    {
                        fresnelVariant = (SvbrdfFresnelVariant)EditorGUILayout.Popup("Fresnel Variant", (int)fresnelVariant, SvbrdfFresnelVariantNames);
                        wardVariant = (SvbrdfSpecularVariantWard)EditorGUILayout.Popup("Ward Variant", (int)wardVariant, SvbrdfSpecularVariantWardNames);
                    }
                    else if (specularType == SvbrdfSpecularType.BLINN_PHONG)
                    {
                        blinnVariant = (SvbrdfSpecularVariantBlinn)EditorGUILayout.Popup("Blinn Variant", (int)blinnVariant, SvbrdfSpecularVariantBlinnNames);
                    }
                    MaterialEditor.EndProperty();

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Parameters and Maps", EditorStyles.boldLabel);

                    // Regular maps
                    DrawRightJustifiedHeader(Styles.mapsTilingOffsetText.text);

                    materialEditor.TexturePropertySingleLine(Styles.diffuseColorMapText, m_DiffuseColorMap, m_DiffuseColorMapST);
                    materialEditor.TexturePropertySingleLine(Styles.specularColorMapText, m_SpecularColorMap, m_SpecularColorMapST);
                    materialEditor.TexturePropertySingleLine(Styles.specularLobeMapText, m_SpecularLobeMap, m_SpecularLobeMapST);
                    materialEditor.ShaderProperty(m_SpecularLobeMapScale, Styles.specularLobeMapScaleText);

                    EditorGUILayout.Space();
                    DrawRightJustifiedHeader(Styles.mapsTilingOffsetText.text);

                    materialEditor.TexturePropertySingleLine(Styles.fresnelMapText, m_FresnelMap, m_FresnelMapST);
                    materialEditor.TexturePropertySingleLine(Styles.normalMapText, m_NormalMap, m_NormalMapST);

                    // Alpha
                    materialEditor.TexturePropertySingleLine(Styles.alphaMapText, m_AlphaMap, m_AlphaMapST);

                    // Displacement
                    //TODO: unsupported for now
                    //useHeightMap = EditorGUILayout.Toggle("Enable Displacement Map", useHeightMap);
                    useHeightMap = false;
                    if (useHeightMap)
                    {
                        ++EditorGUI.indentLevel;
                        DrawRightJustifiedHeader(Styles.mapsTilingOffsetText.text);
                        materialEditor.TexturePropertySingleLine(Styles.heightMapText, m_HeightMap, m_HeightMapST);
                        materialEditor.ShaderProperty(m_SVBRDF_HeightMapMaxMM, "Max Displacement (mm)");
                        --EditorGUI.indentLevel;
                    }

                    // Anisotropy
                    MaterialEditor.BeginProperty(m_Flags);
                    anisotropy = EditorGUILayout.Toggle("Is Anisotropic", anisotropy);
                    MaterialEditor.EndProperty();
                    if (anisotropy)
                    {
                        ++EditorGUI.indentLevel;
                        DrawRightJustifiedHeader(Styles.mapsTilingOffsetText.text);
                        materialEditor.TexturePropertySingleLine(Styles.anisoRotationMapText, m_AnisoRotationMap, m_AnisoRotationMapST);
                        --EditorGUI.indentLevel;
                    }

                    // Clearcoat
                    MaterialEditor.BeginProperty(m_Flags);
                    clearcoat = EditorGUILayout.Toggle("Enable Clearcoat", clearcoat);
                    MaterialEditor.EndProperty();
                    if (clearcoat)
                    {
                        ++EditorGUI.indentLevel;
                        DrawRightJustifiedHeader(Styles.mapsTilingOffsetText.text);
                        materialEditor.TexturePropertySingleLine(Styles.clearcoatColorMapText, m_ClearcoatColorMap, m_ClearcoatColorMapST);
                        materialEditor.TexturePropertySingleLine(Styles.clearcoatNormalMapText, m_ClearcoatNormalMap, m_ClearcoatNormalMapST);
                        MaterialEditor.BeginProperty(m_Flags);
                        clearcoatRefraction = EditorGUILayout.Toggle("Enable Refraction", clearcoatRefraction);
                        MaterialEditor.EndProperty();
                        // The IOR map is always required for the coat F0, while in the CAR_PAINT model, the IOR
                        // is given by a scalar value.
                        DrawRightJustifiedHeader(Styles.mapsTilingOffsetText.text);
                        materialEditor.TexturePropertySingleLine(Styles.clearcoatIORMapText, m_ClearcoatIORMap, m_ClearcoatIORMapST);
                        --EditorGUI.indentLevel;
                    }

                    BRDFType = 0;
                    BRDFType |= (uint)diffuseType;
                    BRDFType |= ((uint)specularType) << 1;

                    BRDFVariants = 0;
                    BRDFVariants |= (uint)fresnelVariant;
                    BRDFVariants |= ((uint)wardVariant) << 2;
                    BRDFVariants |= ((uint)blinnVariant) << 4;

                    m_SVBRDF_BRDFType.floatValue = (float)BRDFType;
                    m_SVBRDF_BRDFVariants.floatValue = (float)BRDFVariants;
                    --EditorGUI.indentLevel;
                    break;
                }

                case AxfBrdfType.CAR_PAINT:
                {
                    EditorGUILayout.Space();
                    ++EditorGUI.indentLevel;

                    // Expand as user-friendly UI

                    // Regular maps
                    materialEditor.TexturePropertySingleLine(Styles.BRDFColorMapText, m_CarPaint2_BRDFColorMap);
                    materialEditor.ShaderProperty(m_CarPaint2_BRDFColorMapScale, Styles.BRDFColorMapScaleText);

                    MaterialEditor.BeginProperty(m_Flags);
                    brdfColorDiagonalClamp = EditorGUILayout.Toggle("BRDF Color Table Diagonal Clamping", brdfColorDiagonalClamp);
                    MaterialEditor.EndProperty();
                    if (brdfColorDiagonalClamp)
                    {
                        ++EditorGUI.indentLevel;
                        MaterialEditor.BeginProperty(m_CarPaint2_BRDFColorMapUVScale);
                        m_CarPaint2_BRDFColorMapUVScale.vectorValue = EditorGUILayout.Vector2Field(Styles.BRDFColorMapUVScaleText, m_CarPaint2_BRDFColorMapUVScale.vectorValue);
                        MaterialEditor.EndProperty();
                        --EditorGUI.indentLevel;
                    }

                    DrawRightJustifiedHeader(Styles.BTFFlakesTilingText.text);
                    materialEditor.TexturePropertySingleLine(Styles.BTFFlakesMapText, m_CarPaint2_BTFFlakeMap, m_CarPaint2_BTFFlakeMapST);
                    //materialEditor.TexturePropertySingleLine(Styles.BTFFlakesMapText, m_CarPaint2_BTFFlakeMap);
                    //m_CarPaint2_BTFFlakeMapST.vectorValue = EditorGUILayout.Vector4Field(Styles.BTFFlakesTilingText, m_CarPaint2_BTFFlakeMapST.vectorValue);

                    //EditorGUILayout.LabelField( "Texture Dimension = " + m_CarPaint_BTFFlakesMap_sRGB.textureDimension );
                    //EditorGUILayout.LabelField( "Texture Format = " + m_CarPaint_BTFFlakesMap_sRGB.textureValue. );
                    materialEditor.ShaderProperty(m_CarPaint2_BTFFlakeMapScale, Styles.BTFFlakesMapScaleText);

                    materialEditor.TexturePropertySingleLine(Styles.thetaFI_sliceLUTMapText, m_CarPaint2_FlakeThetaFISliceLUTMap);

                    materialEditor.ShaderProperty(m_CarPaint2_FixedColorThetaHForIndirectLight, Styles.CarPaintFixedColorThetaHForIndirectLightText);
                    materialEditor.ShaderProperty(m_CarPaint2_FixedFlakesThetaHForIndirectLight, Styles.CarPaintFixedFlakesThetaHForIndirectLightText);

                    //m_CarPaint2_FlakeMaxThetaI = FindProperty(m_CarPaint2_FlakeMaxThetaIText);
                    //m_CarPaint2_FlakeNumThetaF = FindProperty(m_CarPaint2_FlakeNumThetaFText);
                    //m_CarPaint2_FlakeNumThetaI = FindProperty(m_CarPaint2_FlakeNumThetaIText);

                    materialEditor.ShaderProperty(m_CarPaint2_CTDiffuse, Styles.CarPaintCTDiffuseText);
                    materialEditor.IntSliderShaderProperty(m_CarPaint2_LobeCount, 0, 3, Styles.CarPaintLobeCountText);
                    materialEditor.Vector3ShaderProperty(m_CarPaint2_CTF0s, Styles.CarPaintCTF0sText);
                    materialEditor.Vector3ShaderProperty(m_CarPaint2_CTCoeffs, Styles.CarPaintCTCoeffsText);
                    materialEditor.Vector3ShaderProperty(m_CarPaint2_CTSpreads, Styles.CarPaintCTSpreadsText);

                    if (useHeightMap)
                    {
                        materialEditor.ShaderProperty(m_SVBRDF_HeightMapMaxMM, "Max Displacement (mm)");
                    }

                    // Clearcoat
                    MaterialEditor.BeginProperty(m_Flags);
                    clearcoat = EditorGUILayout.Toggle("Enable Clearcoat", clearcoat);
                    MaterialEditor.EndProperty();
                    if (clearcoat)
                    {
                        ++EditorGUI.indentLevel;
                        //materialEditor.TexturePropertySingleLine( Styles.clearcoatColorMapText, m_ClearcoatColorMap );

                        DrawRightJustifiedHeader(Styles.clearcoatNormalMapTilingText.text);
                        materialEditor.TexturePropertySingleLine(Styles.clearcoatNormalMapText, m_ClearcoatNormalMap, m_ClearcoatNormalMapST);
                        //materialEditor.TexturePropertySingleLine(Styles.clearcoatNormalMapText, m_ClearcoatNormalMap);
                        //m_ClearcoatNormalMapST.vectorValue = EditorGUILayout.Vector4Field(Styles.clearcoatNormalMapTilingText, m_ClearcoatNormalMapST.vectorValue);

                        //materialEditor.TexturePropertySingleLine( Styles.clearcoatIORMapText, m_ClearcoatIORMap );
                        materialEditor.ShaderProperty(m_CarPaint2_ClearcoatIOR, Styles.CarPaintIORText);
                        MaterialEditor.BeginProperty(m_Flags);
                        clearcoatRefraction = EditorGUILayout.Toggle("Enable Refraction", clearcoatRefraction);
                        MaterialEditor.EndProperty();
                        --EditorGUI.indentLevel;
                    }

                    --EditorGUI.indentLevel;
                    break;
                }
            }

            // Finally write back flags:
            flags = GenFlags(anisotropy, clearcoat, clearcoatRefraction, useHeightMap, brdfColorDiagonalClamp,
                honorMinRoughness);
            m_Flags.floatValue = (float)flags;
        }//DrawAxfSurfaceOptionsGUI
    }
}
