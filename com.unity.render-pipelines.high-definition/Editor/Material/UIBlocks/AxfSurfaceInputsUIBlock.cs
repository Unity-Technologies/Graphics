using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System.Linq;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // We don't reuse the other surface option ui block, AxF is too different
    public class AxfSurfaceInputsUIBlock : MaterialUIBlock
    {
        public class Styles
        {
            public const string header = "Surface Inputs";

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // SVBRDF Parameters
            public static GUIContent    diffuseColorMapText = new GUIContent("Diffuse Color");
            public static GUIContent    specularColorMapText = new GUIContent("Specular Color");
            public static GUIContent    specularLobeMapText = new GUIContent("Specular Lobe");
            public static GUIContent    specularLobeMapScaleText = new GUIContent("Specular Lobe Scale");
            public static GUIContent    fresnelMapText = new GUIContent("Fresnel");
            public static GUIContent    normalMapText = new GUIContent("Normal");

            // Alpha
            public static GUIContent    alphaMapText = new GUIContent("Alpha");

            // Displacement
            public static GUIContent    heightMapText = new GUIContent("Height");

            // Anisotropy
            public static GUIContent    anisoRotationMapText = new GUIContent("Anisotropy Angle");

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // Car Paint Parameters
            public static GUIContent    BRDFColorMapText = new GUIContent("BRDF Color");
            public static GUIContent    BRDFColorMapScaleText = new GUIContent("BRDF Color Scale");
            public static GUIContent    BRDFColorMapUVScaleText = new GUIContent("BRDF Color Map UV scale restriction");

            public static GUIContent    BTFFlakesMapText = new GUIContent("BTF Flake Color Texture2DArray");
            public static GUIContent    BTFFlakesMapScaleText = new GUIContent("BTF Flakes Scale");
            public static GUIContent    FlakesTilingText = new GUIContent("Flakes Tiling");

            public static GUIContent    thetaFI_sliceLUTMapText = new GUIContent("ThetaFI Slice LUT");

            public static GUIContent    CarPaintIORText = new GUIContent("IOR");

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // Generic

            // Clearcoat
            public static GUIContent    clearcoatColorMapText = new GUIContent("Clearcoat Color");
            public static GUIContent    clearcoatNormalMapText = new GUIContent("Clearcoat Normal");
            public static GUIContent    clearcoatIORMapText = new GUIContent("Clearcoat IOR");

            public static GUIContent    supportDecalsText = new GUIContent("Enable Decal", "Specify whether the material can receive decals.");
            public static GUIContent    receivesSSRText = new GUIContent("Receives SSR", "Specify whether the material can receive screen space reflection.");
        }

        static readonly string[]    AxfBrdfTypeNames = Enum.GetNames(typeof(AxfBrdfType));

        enum    SvbrdfDiffuseType
        {
            LAMBERT = 0,
            OREN_NAYAR = 1,
        }
        static readonly string[]    SvbrdfDiffuseTypeNames = Enum.GetNames(typeof(SvbrdfDiffuseType));

        enum    SvbrdfSpecularType
        {
            WARD = 0,
            BLINN_PHONG = 1,
            COOK_TORRANCE = 2,
            GGX = 3,
            PHONG = 4,
        }
        static readonly string[]    SvbrdfSpecularTypeNames = Enum.GetNames(typeof(SvbrdfSpecularType));

        enum    SvbrdfSpecularVariantWard   // Ward variants
        {
            GEISLERMORODER,     // 2010 (albedo-conservative, should always be preferred!)
            DUER,               // 2006
            WARD,               // 1992 (original paper)
        }
        static readonly string[]    SvbrdfSpecularVariantWardNames = Enum.GetNames(typeof(SvbrdfSpecularVariantWard));
        enum    SvbrdfSpecularVariantBlinn  // Blinn-Phong variants
        {
            ASHIKHMIN_SHIRLEY,  // 2000
            BLINN,              // 1977 (original paper)
            VRAY,
            LEWIS,              // 1993
        }
        static readonly string[]    SvbrdfSpecularVariantBlinnNames = Enum.GetNames(typeof(SvbrdfSpecularVariantBlinn));

        enum    SvbrdfFresnelVariant
        {
            NO_FRESNEL,         // No fresnel
            FRESNEL,            // Full fresnel (1818)
            SCHLICK,            // Schlick's Approximation (1994)
        }
        static readonly string[]    SvbrdfFresnelVariantNames = Enum.GetNames(typeof(SvbrdfFresnelVariant));

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Generic Parameters
        static string               m_MaterialTilingUText = "_MaterialTilingU";
        MaterialProperty  m_MaterialTilingU;
        static string               m_MaterialTilingVText = "_MaterialTilingV";
        MaterialProperty  m_MaterialTilingV;

        static string               m_AxF_BRDFTypeText = "_AxF_BRDFType";
        MaterialProperty  m_AxF_BRDFType = null;

        static string               m_FlagsText = "_Flags";
        MaterialProperty  m_Flags;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // SVBRDF Parameters
        static string               m_SVBRDF_BRDFTypeText = "_SVBRDF_BRDFType";
        MaterialProperty  m_SVBRDF_BRDFType;
        static string               m_SVBRDF_BRDFVariantsText = "_SVBRDF_BRDFVariants";
        MaterialProperty  m_SVBRDF_BRDFVariants;
        static string               m_SVBRDF_HeightMapMaxMMText = "_SVBRDF_HeightMapMaxMM";
        MaterialProperty  m_SVBRDF_HeightMapMaxMM;

        // Regular maps
        static string               m_DiffuseColorMapText = "_SVBRDF_DiffuseColorMap";
        MaterialProperty  m_DiffuseColorMap = null;
        static string               m_SpecularColorMapText = "_SVBRDF_SpecularColorMap";
        MaterialProperty  m_SpecularColorMap = null;

        static string               m_SpecularLobeMapText = "_SVBRDF_SpecularLobeMap";
        MaterialProperty  m_SpecularLobeMap = null;
        static string               m_SpecularLobeMapScaleText = "_SVBRDF_SpecularLobeMapScale";
        MaterialProperty  m_SpecularLobeMapScale;

        static string               m_FresnelMapText = "_SVBRDF_FresnelMap";
        MaterialProperty  m_FresnelMap = null;
        static string               m_NormalMapText = "_SVBRDF_NormalMap";
        MaterialProperty  m_NormalMap = null;

        // Alpha
        static string               m_AlphaMapText = "_SVBRDF_AlphaMap";
        MaterialProperty  m_AlphaMap = null;

        // Displacement
        static string               m_HeightMapText = "_SVBRDF_HeightMap";
        MaterialProperty  m_HeightMap = null;

        // Anisotropy
        static string               m_AnisoRotationMapText = "_SVBRDF_AnisoRotationMap";
        MaterialProperty  m_AnisoRotationMap = null;


        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Car Paint Parameters
        static string               m_CarPaint2_BRDFColorMapText = "_CarPaint2_BRDFColorMap";
        MaterialProperty  m_CarPaint2_BRDFColorMap = null;

        static string               m_CarPaint2_BRDFColorMapScaleText = "_CarPaint2_BRDFColorMapScale";
        MaterialProperty  m_CarPaint2_BRDFColorMapScale;

        static string               m_CarPaint2_BRDFColorMapUVScaleText = "_CarPaint2_BRDFColorMapUVScale";
        MaterialProperty  m_CarPaint2_BRDFColorMapUVScale;

        static string               m_CarPaint2_BTFFlakeMapText = "_CarPaint2_BTFFlakeMap";
        MaterialProperty  m_CarPaint2_BTFFlakeMap = null;

        static string               m_CarPaint2_BTFFlakeMapScaleText = "_CarPaint2_BTFFlakeMapScale";
        MaterialProperty  m_CarPaint2_BTFFlakeMapScale;

        static string               m_CarPaint2_FlakeTilingText = "_CarPaint2_FlakeTiling";
        MaterialProperty  m_CarPaint2_FlakeTiling;

        static string               m_CarPaint2_FlakeThetaFISliceLUTMapText = "_CarPaint2_FlakeThetaFISliceLUTMap";
        MaterialProperty  m_CarPaint2_FlakeThetaFISliceLUTMap;

        static string               m_CarPaint2_FlakeMaxThetaIText = "_CarPaint2_FlakeMaxThetaI";
        MaterialProperty  m_CarPaint2_FlakeMaxThetaI;
        static string               m_CarPaint2_FlakeNumThetaFText = "_CarPaint2_FlakeNumThetaF";
        MaterialProperty  m_CarPaint2_FlakeNumThetaF;
        static string               m_CarPaint2_FlakeNumThetaIText = "_CarPaint2_FlakeNumThetaI";
        MaterialProperty  m_CarPaint2_FlakeNumThetaI;

        static string               m_CarPaint2_ClearcoatIORText = "_CarPaint2_ClearcoatIOR";
        MaterialProperty  m_CarPaint2_ClearcoatIOR;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Clearcoat
        static string               m_ClearcoatColorMapText = "_SVBRDF_ClearcoatColorMap";
        MaterialProperty  m_ClearcoatColorMap = null;
        static string               m_ClearcoatNormalMapText = "_ClearcoatNormalMap";
        MaterialProperty  m_ClearcoatNormalMap = null;
        static string               m_ClearcoatIORMapText = "_SVBRDF_ClearcoatIORMap";
        MaterialProperty  m_ClearcoatIORMap = null;

        // Stencil refs and masks
        const string kStencilRef = "_StencilRef";
        const string kStencilWriteMask = "_StencilWriteMask";
        const string kStencilRefDepth = "_StencilRefDepth";
        const string kStencilWriteMaskDepth = "_StencilWriteMaskDepth";
        const string kStencilRefMV = "_StencilRefMV";
        const string kStencilWriteMaskMV = "_StencilWriteMaskMV";

        // Decals and SSR
        const string kEnableDecals = "_SupportDecals";
        const string kEnableSSR = "_ReceivesSSR";
        MaterialProperty m_SupportDecals = null;
        MaterialProperty m_ReceivesSSR = null;

        Expandable  m_ExpandableBit;

        public AxfSurfaceInputsUIBlock(Expandable expandableBit)
        {
            m_ExpandableBit = expandableBit;
        }

        public override void LoadMaterialProperties()
        {
            m_MaterialTilingU = FindProperty(m_MaterialTilingUText);
            m_MaterialTilingV = FindProperty(m_MaterialTilingVText);

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
            m_CarPaint2_FlakeTiling = FindProperty(m_CarPaint2_FlakeTilingText);

            m_CarPaint2_FlakeMaxThetaI = FindProperty(m_CarPaint2_FlakeMaxThetaIText);
            m_CarPaint2_FlakeNumThetaF = FindProperty(m_CarPaint2_FlakeNumThetaFText);
            m_CarPaint2_FlakeNumThetaI = FindProperty(m_CarPaint2_FlakeNumThetaIText);

            m_CarPaint2_ClearcoatIOR = FindProperty(m_CarPaint2_ClearcoatIORText);

            //////////////////////////////////////////////////////////////////////////
            // Clearcoat
            m_ClearcoatColorMap = FindProperty(m_ClearcoatColorMapText);
            m_ClearcoatNormalMap = FindProperty(m_ClearcoatNormalMapText);
            m_ClearcoatIORMap = FindProperty(m_ClearcoatIORMapText);

            // Decals and SSR
            m_SupportDecals = FindProperty(kEnableDecals);
            m_ReceivesSSR = FindProperty(kEnableSSR);
        }

        public override void OnGUI()
        {
            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                {
                    DrawAxfSurfaceOptionsGUI();
                }
            }
        }

        void DrawAxfSurfaceOptionsGUI()
        {
            materialEditor.ShaderProperty(m_MaterialTilingU, "Material Tiling U");
            materialEditor.ShaderProperty(m_MaterialTilingV, "Material Tiling V");

            AxfBrdfType AxF_BRDFType = (AxfBrdfType)m_AxF_BRDFType.floatValue;
            AxF_BRDFType = (AxfBrdfType)EditorGUILayout.Popup("BRDF Type", (int)AxF_BRDFType, AxfBrdfTypeNames);
            m_AxF_BRDFType.floatValue = (float)AxF_BRDFType;

            switch (AxF_BRDFType)
            {
                case AxfBrdfType.SVBRDF:
                {
                    EditorGUILayout.Space();
                    ++EditorGUI.indentLevel;

                    // Read as compact flags
                    uint    flags = (uint)m_Flags.floatValue;
                    uint    BRDFType = (uint)m_SVBRDF_BRDFType.floatValue;
                    uint    BRDFVariants = (uint)m_SVBRDF_BRDFVariants.floatValue;

                    SvbrdfDiffuseType diffuseType = (SvbrdfDiffuseType)(BRDFType & 0x1);
                    SvbrdfSpecularType specularType = (SvbrdfSpecularType)((BRDFType >> 1) & 0x7);
                    SvbrdfFresnelVariant fresnelVariant = (SvbrdfFresnelVariant)(BRDFVariants & 0x3);
                    SvbrdfSpecularVariantWard wardVariant = (SvbrdfSpecularVariantWard)((BRDFVariants >> 2) & 0x3);
                    SvbrdfSpecularVariantBlinn blinnVariant = (SvbrdfSpecularVariantBlinn)((BRDFVariants >> 4) & 0x3);

                    // Expand as user-friendly UI
//                     EditorGUILayout.LabelField( "Flags", EditorStyles.boldLabel );
                    EditorGUILayout.LabelField("BRDF Variants", EditorStyles.boldLabel);

                    diffuseType = (SvbrdfDiffuseType)EditorGUILayout.Popup("Diffuse Type", (int)diffuseType, SvbrdfDiffuseTypeNames);
                    specularType = (SvbrdfSpecularType)EditorGUILayout.Popup("Specular Type", (int)specularType, SvbrdfSpecularTypeNames);

                    if (specularType == SvbrdfSpecularType.WARD)
                    {
                        fresnelVariant = (SvbrdfFresnelVariant)EditorGUILayout.Popup("Fresnel Variant", (int)fresnelVariant, SvbrdfFresnelVariantNames);
                        wardVariant = (SvbrdfSpecularVariantWard)EditorGUILayout.Popup("Ward Variant", (int)wardVariant, SvbrdfSpecularVariantWardNames);
                    }
                    else if (specularType == SvbrdfSpecularType.BLINN_PHONG)
                    {
                        blinnVariant = (SvbrdfSpecularVariantBlinn)EditorGUILayout.Popup("Blinn Variant", (int)blinnVariant, SvbrdfSpecularVariantBlinnNames);
                    }

                    // Regular maps
                    materialEditor.TexturePropertySingleLine(Styles.diffuseColorMapText, m_DiffuseColorMap);
                    materialEditor.TexturePropertySingleLine(Styles.specularColorMapText, m_SpecularColorMap);
                    materialEditor.TexturePropertySingleLine(Styles.specularLobeMapText, m_SpecularLobeMap);
                    m_SpecularLobeMapScale.floatValue = EditorGUILayout.FloatField(Styles.specularLobeMapScaleText, m_SpecularLobeMapScale.floatValue);
                    materialEditor.TexturePropertySingleLine(Styles.fresnelMapText, m_FresnelMap);
                    materialEditor.TexturePropertySingleLine(Styles.normalMapText, m_NormalMap);

                    // Alpha
                    materialEditor.TexturePropertySingleLine(Styles.alphaMapText, m_AlphaMap);

                    // Displacement
                    bool    useDisplacementMap = EditorGUILayout.Toggle("Enable Displacement Map", (flags & 8) != 0);
                    if (useDisplacementMap)
                    {
                        ++EditorGUI.indentLevel;
                        materialEditor.TexturePropertySingleLine(Styles.heightMapText, m_HeightMap);
                        materialEditor.ShaderProperty(m_SVBRDF_HeightMapMaxMM, "Max Displacement (mm)");
                        --EditorGUI.indentLevel;
                    }

                    // Anisotropy
                    bool    isAnisotropic = EditorGUILayout.Toggle("Is Anisotropic", (flags & 1) != 0);
                    if (isAnisotropic)
                    {
                        ++EditorGUI.indentLevel;
                        materialEditor.TexturePropertySingleLine(Styles.anisoRotationMapText, m_AnisoRotationMap);
                        --EditorGUI.indentLevel;
                    }

                    // Clearcoat
                    bool    hasClearcoat = EditorGUILayout.Toggle("Enable Clearcoat", (flags & 2) != 0);
                    bool    clearcoatUsesRefraction = (flags & 4) != 0;
                    if (hasClearcoat)
                    {
                        ++EditorGUI.indentLevel;
                        materialEditor.TexturePropertySingleLine(Styles.clearcoatColorMapText, m_ClearcoatColorMap);
                        materialEditor.TexturePropertySingleLine(Styles.clearcoatNormalMapText, m_ClearcoatNormalMap);
                        clearcoatUsesRefraction = EditorGUILayout.Toggle("Enable Refraction", clearcoatUsesRefraction);
                        if (clearcoatUsesRefraction)
                        {
                            ++EditorGUI.indentLevel;
                            materialEditor.TexturePropertySingleLine(Styles.clearcoatIORMapText, m_ClearcoatIORMap);
                            --EditorGUI.indentLevel;
                        }
                        --EditorGUI.indentLevel;
                    }

                    // Write back as compact flags
                    flags = 0;
                    flags |= isAnisotropic ? 1U : 0U;
                    flags |= hasClearcoat ? 2U : 0U;
                    flags |= clearcoatUsesRefraction ? 4U : 0U;
                    flags |= useDisplacementMap ? 8U : 0U;

                    BRDFType = 0;
                    BRDFType |= (uint)diffuseType;
                    BRDFType |= ((uint)specularType) << 1;

                    BRDFVariants = 0;
                    BRDFVariants |= (uint)fresnelVariant;
                    BRDFVariants |= ((uint)wardVariant) << 2;
                    BRDFVariants |= ((uint)blinnVariant) << 4;

//                    cmd.SetGlobalFloat( HDShaderIDs._TexturingModeFlags, *(float*) &texturingModeFlags );
                    m_Flags.floatValue = (float)flags;
                    m_SVBRDF_BRDFType.floatValue = (float)BRDFType;
                    m_SVBRDF_BRDFVariants.floatValue = (float)BRDFVariants;

                    --EditorGUI.indentLevel;
                    break;
                }

                case AxfBrdfType.CAR_PAINT:
                {
                    EditorGUILayout.Space();
                    ++EditorGUI.indentLevel;

                    // Read as compact flags
                    uint    flags = (uint)m_Flags.floatValue;

                    bool    isAnisotropic = false;
                    bool    useDisplacementMap = false;

                    // Expand as user-friendly UI

                    // Regular maps
                    materialEditor.TexturePropertySingleLine(Styles.BRDFColorMapText, m_CarPaint2_BRDFColorMap);
                    m_CarPaint2_BRDFColorMapScale.floatValue = EditorGUILayout.FloatField(Styles.BRDFColorMapScaleText, m_CarPaint2_BRDFColorMapScale.floatValue);

                    bool    brdfColorUseDiagonalClamp = EditorGUILayout.Toggle("BRDF Color Table Diagonal Clamping", (flags & 16) != 0);
                    if (brdfColorUseDiagonalClamp)
                    {
                        ++EditorGUI.indentLevel;
                        m_CarPaint2_BRDFColorMapUVScale.vectorValue = EditorGUILayout.Vector2Field(Styles.BRDFColorMapUVScaleText, m_CarPaint2_BRDFColorMapUVScale.vectorValue);
                        --EditorGUI.indentLevel;
                    }
                    

                    materialEditor.TexturePropertySingleLine(Styles.BTFFlakesMapText, m_CarPaint2_BTFFlakeMap);
                    //EditorGUILayout.LabelField( "Texture Dimension = " + m_CarPaint_BTFFlakesMap_sRGB.textureDimension );
                    //EditorGUILayout.LabelField( "Texture Format = " + m_CarPaint_BTFFlakesMap_sRGB.textureValue. );
                    m_CarPaint2_BTFFlakeMapScale.floatValue = EditorGUILayout.FloatField(Styles.BTFFlakesMapScaleText, m_CarPaint2_BTFFlakeMapScale.floatValue);
                    m_CarPaint2_FlakeTiling.floatValue = EditorGUILayout.FloatField(Styles.FlakesTilingText, m_CarPaint2_FlakeTiling.floatValue);

                    materialEditor.TexturePropertySingleLine(Styles.thetaFI_sliceLUTMapText, m_CarPaint2_FlakeThetaFISliceLUTMap);

                    // m_CarPaint_maxThetaI = FindProperty( m_CarPaint_maxThetaIText, props );
                    // m_CarPaint_numThetaF = FindProperty( m_CarPaint_numThetaFText, props );
                    // m_CarPaint_numThetaI = FindProperty( m_CarPaint_numThetaIText, props );


                    // Clearcoat
                    bool    hasClearcoat = EditorGUILayout.Toggle("Enable Clearcoat", (flags & 2) != 0);
                    bool    clearcoatUsesRefraction = (flags & 4) != 0;
                    if (hasClearcoat)
                    {
                        ++EditorGUI.indentLevel;
//                        materialEditor.TexturePropertySingleLine( Styles.clearcoatColorMapText, m_ClearcoatColorMap );
                        materialEditor.TexturePropertySingleLine(Styles.clearcoatNormalMapText, m_ClearcoatNormalMap);
//                        if ( clearcoatUsesRefraction ) {
                        {
                            ++EditorGUI.indentLevel;
//                            materialEditor.TexturePropertySingleLine( Styles.clearcoatIORMapText, m_ClearcoatIORMap );
                            m_CarPaint2_ClearcoatIOR.floatValue = EditorGUILayout.FloatField(Styles.CarPaintIORText, m_CarPaint2_ClearcoatIOR.floatValue);
                            --EditorGUI.indentLevel;
                        }
                        --EditorGUI.indentLevel;
                        clearcoatUsesRefraction = EditorGUILayout.Toggle("Enable Refraction", clearcoatUsesRefraction);
                    }

                    // Write back as compact flags
                    flags = 0;
                    flags |= isAnisotropic ? 1U : 0U;
                    flags |= hasClearcoat ? 2U : 0U;
                    flags |= clearcoatUsesRefraction ? 4U : 0U;
                    flags |= useDisplacementMap ? 8U : 0U;
                    flags |= brdfColorUseDiagonalClamp ? 16U : 0U;

//                    cmd.SetGlobalFloat( HDShaderIDs._TexturingModeFlags, *(float*) &texturingModeFlags );
                    m_Flags.floatValue = (float)flags;

                    --EditorGUI.indentLevel;
                    break;
                }
            }
        }
    }
}