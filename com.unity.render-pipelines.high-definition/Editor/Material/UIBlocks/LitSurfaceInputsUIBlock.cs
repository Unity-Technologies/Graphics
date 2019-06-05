using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // This block is shared for Lit and Layered surface inputs + tesselation variants
    public class LitSurfaceInputsUIBlock : MaterialUIBlock
    {
        public enum Features
        {
            None            = 0,
            CoatMask        = 1 << 0,
            HeightMap       = 1 << 1,
            LayerOptions    = 1 << 2,
            SubHeader       = 1 << 3,
            Standard        = 1 << 4,
            All             = ~0 ^ SubHeader // By default we don't want a sub-header
        }

        public class Styles
        {
            public const string header = "Surface Inputs";

            public static GUIContent colorText = new GUIContent("Color", " Albedo (RGB) and Transparency (A).");

            public static GUIContent baseColorText = new GUIContent("Base Map", "Specifies the base color (RGB) and opacity (A) of the Material.");

            public static GUIContent metallicText = new GUIContent("Metallic", "Controls the scale factor for the Material's metallic effect.");
            public static GUIContent smoothnessText = new GUIContent("Smoothness", "Controls the scale factor for the Material's smoothness.");
            public static GUIContent smoothnessRemappingText = new GUIContent("Smoothness Remapping", "Controls a remap for the smoothness channel in the Mask Map.");
            public static GUIContent aoRemappingText = new GUIContent("Ambient Occlusion Remapping", "Controls a remap for the ambient occlusion channel in the Mask Map.");
            public static GUIContent maskMapSText = new GUIContent("Mask Map", "Specifies the Mask Map for this Material - Metallic (R), Ambient occlusion (G), Detail mask (B), Smoothness (A).");
            public static GUIContent maskMapSpecularText = new GUIContent("Mask Map", "Specifies the Mask Map for this Material - Ambient occlusion (G), Detail mask (B), Smoothness (A).");

            public static GUIContent normalMapSpaceText = new GUIContent("Normal Map Space", "");
            public static GUIContent normalMapText = new GUIContent("Normal Map", "Specifies the Normal Map for this Material (BC7/BC5/DXT5(nm)) and controls its strength.");
            public static GUIContent normalMapOSText = new GUIContent("Normal Map OS", "Specifies the object space Normal Map (BC7/DXT1/RGB).");
            public static GUIContent bentNormalMapText = new GUIContent("Bent normal map", "Specifies the cosine weighted Bent Normal Map (BC7/BC5/DXT5(nm)) for this Material. Use only with indirect diffuse lighting (Lightmaps and Light Probes).");
            public static GUIContent bentNormalMapOSText = new GUIContent("Bent normal map OS", "Specifies the object space Bent Normal Map (BC7/DXT1/RGB) for this Material. Use only with indirect diffuse lighting (Lightmaps and Light Probes).");

            // Height
            public static GUIContent heightMapText = new GUIContent("Height Map", "Specifies the Height Map (R) for this Material.\nFor floating point textures, set the Min, Max, and base values to 0, 1, and 0 respectively.");
            public static GUIContent heightMapCenterText = new GUIContent("Base", "Controls the base of the Height Map (between 0 and 1).");
            public static GUIContent heightMapMinText = new GUIContent("Min", "Sets the minimum value in the Height Map (in centimeters).");
            public static GUIContent heightMapMaxText = new GUIContent("Max", "Sets the maximum value in the Height Map (in centimeters).");
            public static GUIContent heightMapAmplitudeText = new GUIContent("Amplitude", "Sets the amplitude of the Height Map (in centimeters).");
            public static GUIContent heightMapOffsetText = new GUIContent("Offset", "Sets the offset HDRP applies to the Height Map (in centimeters).");
            public static GUIContent heightMapParametrization = new GUIContent("Parametrization", "Specifies the parametrization method for the Height Map.");

            public static GUIContent normalMapSpaceWarning = new GUIContent("HDRP does not support object space normals with triplanar mapping.");
            public static GUIContent tangentMapText = new GUIContent("Tangent Map", "Specifies the Tangent Map (BC7/BC5/DXT5(nm)) for this Material.");
            public static GUIContent tangentMapOSText = new GUIContent("Tangent Map OS", "Specifies the object space Tangent Map (BC7/DXT1/RGB) for this Material.");
            public static GUIContent anisotropyText = new GUIContent("Anisotropy", "Controls the scale factor for anisotropy.");
            public static GUIContent anisotropyMapText = new GUIContent("Anisotropy Map", "Specifies the Anisotropy Map(R) for this Material.");

            public static GUIContent UVBaseMappingText = new GUIContent("Base UV mapping", "");
            public static GUIContent texWorldScaleText = new GUIContent("World scale", "Sets the tiling factor HDRP applies to Planar/Trilinear mapping.");

            // Specular color
            public static GUIContent energyConservingSpecularColorText = new GUIContent("Energy Conserving Specular Color", "When enabled, HDRP simulates energy conservation when using Specular Color mode. This results in high Specular Color values producing lower Diffuse Color values.");
            public static GUIContent specularColorText = new GUIContent("Specular Color", "Specifies the Specular color (RGB) of this Material.");

            // Subsurface
            public static GUIContent diffusionProfileText = new GUIContent("Diffusion Profile", "Specifies the Diffusion Profie HDRP uses to determine the behavior of the subsurface scattering/transmission effect.");
            public static GUIContent subsurfaceMaskText = new GUIContent("Subsurface Mask", "Controls the overall strength of the subsurface scattering effect.");
            public static GUIContent subsurfaceMaskMapText = new GUIContent("Subsurface Mask Map", "Specifies the Subsurface mask map (R) for this Material - This map controls the strength of the subsurface scattering effect.");
            public static GUIContent thicknessText = new GUIContent("Thickness", "Controls the strength of the Thickness Map, low values allow some light to transmit through the object.");
            public static GUIContent thicknessMapText = new GUIContent("Thickness Map", "Specifies the Thickness Map (R) for this Material - This map describes the thickness of the object. When subsurface scattering is enabled, low values allow some light to transmit through the object.");
            public static GUIContent thicknessRemapText = new GUIContent("Thickness Remap", "Controls a remap for the Thickness Map from [0, 1] to the specified range.");

            // Iridescence
            public static GUIContent iridescenceMaskText = new GUIContent("Iridescence Mask", "Specifies the Iridescence Mask (R) for this Material - This map controls the intensity of the iridescence.");
            public static GUIContent iridescenceThicknessText = new GUIContent("Iridescence Layer Thickness");
            public static GUIContent iridescenceThicknessMapText = new GUIContent("Iridescence Layer Thickness map", "Specifies the Iridescence Layer Thickness map (R) for this Material.");
            public static GUIContent iridescenceThicknessRemapText = new GUIContent("Iridescence Layer Thickness remap");
            
            // Clear Coat
            public static GUIContent coatMaskText = new GUIContent("Coat Mask", "Attenuate the coating effect.");

            // Layer Options
            public static readonly GUIContent layerTexWorldScaleText = EditorGUIUtility.TrTextContent("World Scale", "Sets the tiling factor of the Planar/Trilinear mapping.");
            public static readonly GUIContent UVBlendMaskText = EditorGUIUtility.TrTextContent("BlendMask UV Mapping", "Specifies the UV Mapping mode of the layer.");
            public static readonly GUIContent layerMapMaskText = EditorGUIUtility.TrTextContent("Layer Mask", "Specifies the Layer Mask for this Material");
            public static readonly GUIContent vertexColorModeText = EditorGUIUtility.TrTextContent("Vertex Color Mode", "Specifies the method HDRP uses to color vertices.\nMultiply: Multiplies vertex color with the mask.\nAdditive: Remaps vertex color values between [-1, 1] and adds them to the mask (neutral value is 0.5 vertex color).");
            public static readonly GUIContent layerCountText = EditorGUIUtility.TrTextContent("Layer Count", "Controls the number of layers for this Material.");
            public static readonly GUIContent objectScaleAffectTileText = EditorGUIUtility.TrTextContent("Lock layers 0123 tiling with object Scale", "When enabled, tiling of each layer is affected by the Transform's Scale.");
            public static readonly GUIContent objectScaleAffectTileText2 = EditorGUIUtility.TrTextContent("Lock layers  123 tiling with object Scale", "When enabled, tiling of each influenced layer (except the main layer) is affected by the Transform's Scale.");
            public static readonly GUIContent useHeightBasedBlendText = EditorGUIUtility.TrTextContent("Use Height Based Blend", "When enabled, HDRP blends the layer with the underlying layer based on the height.");
            public static readonly GUIContent useMainLayerInfluenceModeText = EditorGUIUtility.TrTextContent("Main Layer Influence", "Switches between regular layers mode and base/layers mode.");
            public static readonly GUIContent heightTransition = EditorGUIUtility.TrTextContent("Height Transition", "Sets the size, in world units, of the smooth transition between layers.");
        }

        MaterialProperty[] UVBase = new MaterialProperty[kMaxLayerCount];
        const string kUVBase = "_UVBase";
        MaterialProperty[] TexWorldScale = new MaterialProperty[kMaxLayerCount];
        const string kTexWorldScale = "_TexWorldScale";
        MaterialProperty[] InvTilingScale = new MaterialProperty[kMaxLayerCount];
        const string kInvTilingScale = "_InvTilingScale";
        MaterialProperty[] UVMappingMask = new MaterialProperty[kMaxLayerCount];
        const string kUVMappingMask = "_UVMappingMask";

        MaterialProperty[] baseColor = new MaterialProperty[kMaxLayerCount];
        const string kBaseColor = "_BaseColor";
        MaterialProperty[] baseColorMap = new MaterialProperty[kMaxLayerCount];
        const string kBaseColorMap = "_BaseColorMap";
        MaterialProperty[] metallic = new MaterialProperty[kMaxLayerCount];
        const string kMetallic = "_Metallic";
        MaterialProperty[] smoothness = new MaterialProperty[kMaxLayerCount];
        const string kSmoothness = "_Smoothness";
        MaterialProperty[] smoothnessRemapMin = new MaterialProperty[kMaxLayerCount];
        const string kSmoothnessRemapMin = "_SmoothnessRemapMin";
        MaterialProperty[] smoothnessRemapMax = new MaterialProperty[kMaxLayerCount];
        const string kSmoothnessRemapMax = "_SmoothnessRemapMax";
        MaterialProperty[] aoRemapMin = new MaterialProperty[kMaxLayerCount];
        const string kAORemapMin = "_AORemapMin";
        MaterialProperty[] aoRemapMax = new MaterialProperty[kMaxLayerCount];
        const string kAORemapMax = "_AORemapMax";
        MaterialProperty[] maskMap = new MaterialProperty[kMaxLayerCount];
        const string kMaskMap = "_MaskMap";
        MaterialProperty[] normalScale = new MaterialProperty[kMaxLayerCount];
        const string kNormalScale = "_NormalScale";
        MaterialProperty[] normalMap = new MaterialProperty[kMaxLayerCount];
        const string kNormalMap = "_NormalMap";
        MaterialProperty[] normalMapOS = new MaterialProperty[kMaxLayerCount];
        const string kNormalMapOS = "_NormalMapOS";
        MaterialProperty[] bentNormalMap = new MaterialProperty[kMaxLayerCount];
        const string kBentNormalMap = "_BentNormalMap";
        MaterialProperty[] bentNormalMapOS = new MaterialProperty[kMaxLayerCount];
        const string kBentNormalMapOS = "_BentNormalMapOS";
        MaterialProperty[] normalMapSpace = new MaterialProperty[kMaxLayerCount];
        const string kNormalMapSpace = "_NormalMapSpace";

        MaterialProperty[] heightMap = new MaterialProperty[kMaxLayerCount];
        const string kHeightMap = "_HeightMap";
        MaterialProperty[] heightAmplitude = new MaterialProperty[kMaxLayerCount];
        const string kHeightAmplitude = "_HeightAmplitude";
        MaterialProperty[] heightCenter = new MaterialProperty[kMaxLayerCount];
        const string kHeightCenter = "_HeightCenter";
        MaterialProperty[] heightPoMAmplitude = new MaterialProperty[kMaxLayerCount];
        const string kHeightPoMAmplitude = "_HeightPoMAmplitude";
        MaterialProperty[] heightTessCenter = new MaterialProperty[kMaxLayerCount];
        const string kHeightTessCenter = "_HeightTessCenter";
        MaterialProperty[] heightTessAmplitude = new MaterialProperty[kMaxLayerCount];
        const string kHeightTessAmplitude = "_HeightTessAmplitude";
        MaterialProperty[] heightMin = new MaterialProperty[kMaxLayerCount];
        const string kHeightMin = "_HeightMin";
        MaterialProperty[] heightMax = new MaterialProperty[kMaxLayerCount];
        const string kHeightMax = "_HeightMax";
        MaterialProperty[] heightOffset = new MaterialProperty[kMaxLayerCount];
        const string kHeightOffset = "_HeightOffset";
        MaterialProperty[] heightParametrization = new MaterialProperty[kMaxLayerCount];
        const string kHeightParametrization = "_HeightMapParametrization";

        MaterialProperty displacementMode = null;
        const string kDisplacementMode = "_DisplacementMode";

        MaterialProperty tangentMap = null;
        const string kTangentMap = "_TangentMap";
        MaterialProperty tangentMapOS = null;
        const string kTangentMapOS = "_TangentMapOS";
        MaterialProperty anisotropy = null;
        const string kAnisotropy = "_Anisotropy";
        MaterialProperty anisotropyMap = null;
        const string kAnisotropyMap = "_AnisotropyMap";

        MaterialProperty energyConservingSpecularColor = null;
        const string kEnergyConservingSpecularColor = "_EnergyConservingSpecularColor";
        MaterialProperty specularColor = null;
        const string kSpecularColor = "_SpecularColor";
        MaterialProperty specularColorMap = null;
        const string kSpecularColorMap = "_SpecularColorMap";

        MaterialProperty[] diffusionProfileHash = new MaterialProperty[kMaxLayerCount];
        const string kDiffusionProfileHash = "_DiffusionProfileHash";
        MaterialProperty[] diffusionProfileAsset = new MaterialProperty[kMaxLayerCount];
        const string kDiffusionProfileAsset = "_DiffusionProfileAsset";
        MaterialProperty[] subsurfaceMask = new MaterialProperty[kMaxLayerCount];
        const string kSubsurfaceMask = "_SubsurfaceMask";
        MaterialProperty[] subsurfaceMaskMap = new MaterialProperty[kMaxLayerCount];
        const string kSubsurfaceMaskMap = "_SubsurfaceMaskMap";
        MaterialProperty[] thickness = new MaterialProperty[kMaxLayerCount];
        const string kThickness = "_Thickness";
        MaterialProperty[] thicknessMap = new MaterialProperty[kMaxLayerCount];
        const string kThicknessMap = "_ThicknessMap";
        MaterialProperty[] thicknessRemap = new MaterialProperty[kMaxLayerCount];
        const string kThicknessRemap = "_ThicknessRemap";

        MaterialProperty iridescenceMask = null;
        const string kIridescenceMask = "_IridescenceMask";
        MaterialProperty iridescenceMaskMap = null;
        const string kIridescenceMaskMap = "_IridescenceMaskMap";
        MaterialProperty iridescenceThickness = null;
        const string kIridescenceThickness = "_IridescenceThickness";
        MaterialProperty iridescenceThicknessMap = null;
        const string kIridescenceThicknessMap = "_IridescenceThicknessMap";
        MaterialProperty iridescenceThicknessRemap = null;
        const string kIridescenceThicknessRemap = "_IridescenceThicknessRemap";

        // Material ID
        MaterialProperty materialID  = null;
        MaterialProperty transmissionEnable = null;
        const string kTransmissionEnable = "_TransmissionEnable";

        // Coat mask
        MaterialProperty coatMask = null;
        const string kCoatMask = "_CoatMask";
        MaterialProperty coatMaskMap = null;
        const string kCoatMaskMap = "_CoatMaskMap";

        // Layer options
        MaterialProperty layerCount = null;
        const string kLayerCount = "_LayerCount";
        MaterialProperty layerMaskMap = null;
        const string kLayerMaskMap = "_LayerMaskMap";
        MaterialProperty layerInfluenceMaskMap = null;
        const string kLayerInfluenceMaskMap = "_LayerInfluenceMaskMap";
        MaterialProperty vertexColorMode = null;
        const string kVertexColorMode = "_VertexColorMode";
        MaterialProperty objectScaleAffectTile = null;
        const string kObjectScaleAffectTile = "_ObjectScaleAffectTile";
        MaterialProperty UVBlendMask = null;
        const string kUVBlendMask = "_UVBlendMask";
        MaterialProperty UVMappingMaskBlendMask = null;
        const string kUVMappingMaskBlendMask = "_UVMappingMaskBlendMask";
        MaterialProperty texWorldScaleBlendMask = null;
        const string kTexWorldScaleBlendMask = "_TexWorldScaleBlendMask";
        MaterialProperty useMainLayerInfluence = null;
        const string kkUseMainLayerInfluence = "_UseMainLayerInfluence";
        MaterialProperty useHeightBasedBlend = null;
        const string kUseHeightBasedBlend = "_UseHeightBasedBlend";

        // Height blend
        MaterialProperty heightTransition = null;
        const string kHeightTransition = "_HeightTransition";

        Expandable  m_ExpandableBit;
        Features    m_Features;
        int         m_LayerCount;
        int         m_LayerIndex;
        bool        m_UseHeightBasedBlend;
        Color       m_DotColor;

        bool        isLayeredLit => m_LayerCount > 1;

        public LitSurfaceInputsUIBlock(Expandable expandableBit, int layerCount = 1, int layerIndex = 0, Features features = Features.All, Color dotColor = default(Color))
        {
            m_ExpandableBit = expandableBit;
            m_Features = features;
            m_LayerCount = layerCount;
            m_LayerIndex = layerIndex;
            m_DotColor = dotColor;
        }

        public override void LoadMaterialProperties()
        {
            UVBase = FindPropertyLayered(kUVBase, m_LayerCount, true);
            TexWorldScale = FindPropertyLayered(kTexWorldScale, m_LayerCount);
            InvTilingScale = FindPropertyLayered(kInvTilingScale, m_LayerCount);
            UVMappingMask = FindPropertyLayered(kUVMappingMask, m_LayerCount);

            baseColor = FindPropertyLayered(kBaseColor, m_LayerCount);
            baseColorMap = FindPropertyLayered(kBaseColorMap, m_LayerCount);
            metallic = FindPropertyLayered(kMetallic, m_LayerCount);
            smoothness = FindPropertyLayered(kSmoothness, m_LayerCount);
            smoothnessRemapMin = FindPropertyLayered(kSmoothnessRemapMin, m_LayerCount);
            smoothnessRemapMax = FindPropertyLayered(kSmoothnessRemapMax, m_LayerCount);
            aoRemapMin = FindPropertyLayered(kAORemapMin, m_LayerCount);
            aoRemapMax = FindPropertyLayered(kAORemapMax, m_LayerCount);
            maskMap = FindPropertyLayered(kMaskMap, m_LayerCount);
            normalMap = FindPropertyLayered(kNormalMap, m_LayerCount);
            normalMapOS = FindPropertyLayered(kNormalMapOS, m_LayerCount);
            normalScale = FindPropertyLayered(kNormalScale, m_LayerCount);
            bentNormalMap = FindPropertyLayered(kBentNormalMap, m_LayerCount);
            bentNormalMapOS = FindPropertyLayered(kBentNormalMapOS, m_LayerCount);
            normalMapSpace = FindPropertyLayered(kNormalMapSpace, m_LayerCount);

            // Height
            heightMap = FindPropertyLayered(kHeightMap, m_LayerCount);
            heightAmplitude = FindPropertyLayered(kHeightAmplitude, m_LayerCount);
            heightCenter = FindPropertyLayered(kHeightCenter, m_LayerCount);
            heightPoMAmplitude = FindPropertyLayered(kHeightPoMAmplitude, m_LayerCount);
            heightMin = FindPropertyLayered(kHeightMin, m_LayerCount);
            heightMax = FindPropertyLayered(kHeightMax, m_LayerCount);
            heightTessCenter = FindPropertyLayered(kHeightTessCenter, m_LayerCount);
            heightTessAmplitude = FindPropertyLayered(kHeightTessAmplitude, m_LayerCount);
            heightOffset = FindPropertyLayered(kHeightOffset, m_LayerCount);
            heightParametrization = FindPropertyLayered(kHeightParametrization, m_LayerCount);

            // Specular Color
            energyConservingSpecularColor = FindProperty(kEnergyConservingSpecularColor);
            specularColor = FindProperty(kSpecularColor);
            specularColorMap = FindProperty(kSpecularColorMap);

            // Anisotropy
            tangentMap = FindProperty(kTangentMap);
            tangentMapOS = FindProperty(kTangentMapOS);
            anisotropy = FindProperty(kAnisotropy);
            anisotropyMap = FindProperty(kAnisotropyMap);

            // Iridescence
            iridescenceMask = FindProperty(kIridescenceMask);
            iridescenceMaskMap = FindProperty(kIridescenceMaskMap);
            iridescenceThickness = FindProperty(kIridescenceThickness);
            iridescenceThicknessMap = FindProperty(kIridescenceThicknessMap);
            iridescenceThicknessRemap = FindProperty(kIridescenceThicknessRemap);

            // Sub surface
            diffusionProfileHash = FindPropertyLayered(kDiffusionProfileHash, m_LayerCount);
            diffusionProfileAsset = FindPropertyLayered(kDiffusionProfileAsset, m_LayerCount);
            subsurfaceMask = FindPropertyLayered(kSubsurfaceMask, m_LayerCount);
            subsurfaceMaskMap = FindPropertyLayered(kSubsurfaceMaskMap, m_LayerCount);
            thickness = FindPropertyLayered(kThickness, m_LayerCount);
            thicknessMap = FindPropertyLayered(kThicknessMap, m_LayerCount);
            thicknessRemap = FindPropertyLayered(kThicknessRemap, m_LayerCount);
            
            // clear coat
            coatMask = FindProperty(kCoatMask);
            coatMaskMap = FindProperty(kCoatMaskMap);

            displacementMode = FindProperty(kDisplacementMode);

            materialID = FindProperty(kMaterialID);
            transmissionEnable = FindProperty(kTransmissionEnable);

            // Layer options
            layerCount = FindProperty(kLayerCount);
            layerMaskMap = FindProperty(kLayerMaskMap);
            layerInfluenceMaskMap = FindProperty(kLayerInfluenceMaskMap);
            vertexColorMode = FindProperty(kVertexColorMode);
            objectScaleAffectTile = FindProperty(kObjectScaleAffectTile);
            UVBlendMask = FindProperty(kUVBlendMask);
            UVMappingMaskBlendMask = FindProperty(kUVMappingMaskBlendMask);
            texWorldScaleBlendMask = FindProperty(kTexWorldScaleBlendMask);
            useMainLayerInfluence = FindProperty(kkUseMainLayerInfluence);
            useHeightBasedBlend = FindProperty(kUseHeightBasedBlend);

            // Height blend
            heightTransition = FindProperty(kHeightTransition);
        }

        public override void OnGUI()
        {
            bool subHeader = (m_Features & Features.SubHeader) != 0;

            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor, subHeader: subHeader, colorDot: m_DotColor))
            {
                if (header.expanded)
                {
                    if ((m_Features & Features.Standard) != 0)
                        DrawSurfaceInputsGUI();
                    if ((m_Features & Features.LayerOptions) != 0)
                        DrawLayerOptionsGUI();
                }
            }
        }

        void DrawSurfaceInputsGUI()
        {
            UVBaseMapping uvBaseMapping = (UVBaseMapping)UVBase[m_LayerIndex].floatValue;
            float X, Y, Z, W;

            materialEditor.TexturePropertySingleLine(Styles.baseColorText, baseColorMap[m_LayerIndex], baseColor[m_LayerIndex]);

            // TODO: does not work with multi-selection
            MaterialId materialIdValue = materials[0].GetMaterialId();

            if (materialIdValue == MaterialId.LitStandard ||
                materialIdValue == MaterialId.LitAniso ||
                materialIdValue == MaterialId.LitIridescence)
            {
                materialEditor.ShaderProperty(metallic[m_LayerIndex], Styles.metallicText);
            }

            if (maskMap[m_LayerIndex].textureValue == null)
            {
                materialEditor.ShaderProperty(smoothness[m_LayerIndex], Styles.smoothnessText);
            }
            else
            {
                float remapMin = smoothnessRemapMin[m_LayerIndex].floatValue;
                float remapMax = smoothnessRemapMax[m_LayerIndex].floatValue;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider(Styles.smoothnessRemappingText, ref remapMin, ref remapMax, 0.0f, 1.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    smoothnessRemapMin[m_LayerIndex].floatValue = remapMin;
                    smoothnessRemapMax[m_LayerIndex].floatValue = remapMax;
                }

                float aoMin = aoRemapMin[m_LayerIndex].floatValue;
                float aoMax = aoRemapMax[m_LayerIndex].floatValue;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider(Styles.aoRemappingText, ref aoMin, ref aoMax, 0.0f, 1.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    aoRemapMin[m_LayerIndex].floatValue = aoMin;
                    aoRemapMax[m_LayerIndex].floatValue = aoMax;
                }
            }

            materialEditor.TexturePropertySingleLine((materialIdValue == MaterialId.LitSpecular) ? Styles.maskMapSpecularText : Styles.maskMapSText, maskMap[m_LayerIndex]);

            materialEditor.ShaderProperty(normalMapSpace[m_LayerIndex], Styles.normalMapSpaceText);

            // Triplanar only work with tangent space normal
            if ((NormalMapSpace)normalMapSpace[m_LayerIndex].floatValue == NormalMapSpace.ObjectSpace && ((UVBaseMapping)UVBase[m_LayerIndex].floatValue == UVBaseMapping.Triplanar))
            {
                EditorGUILayout.HelpBox(Styles.normalMapSpaceWarning.text, MessageType.Error);
            }

            // We have two different property for object space and tangent space normal map to allow
            // 1. to go back and forth
            // 2. to avoid the warning that ask to fix the object normal map texture (normalOS are just linear RGB texture
            if ((NormalMapSpace)normalMapSpace[m_LayerIndex].floatValue == NormalMapSpace.TangentSpace)
            {
                materialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap[m_LayerIndex], normalScale[m_LayerIndex]);
                materialEditor.TexturePropertySingleLine(Styles.bentNormalMapText, bentNormalMap[m_LayerIndex]);
            }
            else
            {
                // No scaling in object space
                materialEditor.TexturePropertySingleLine(Styles.normalMapOSText, normalMapOS[m_LayerIndex]);
                materialEditor.TexturePropertySingleLine(Styles.bentNormalMapOSText, bentNormalMapOS[m_LayerIndex]);
            }

            DisplacementMode displaceMode = (DisplacementMode)displacementMode.floatValue;
            if (displaceMode != DisplacementMode.None || (m_Features & Features.HeightMap) != 0)
            {
                EditorGUI.BeginChangeCheck();
                materialEditor.TexturePropertySingleLine(Styles.heightMapText, heightMap[m_LayerIndex]);
                if (!heightMap[m_LayerIndex].hasMixedValue && heightMap[m_LayerIndex].textureValue != null && !displacementMode.hasMixedValue)
                {
                    EditorGUI.indentLevel++;
                    if (displaceMode == DisplacementMode.Pixel)
                    {
                        materialEditor.ShaderProperty(heightPoMAmplitude[m_LayerIndex], Styles.heightMapAmplitudeText);
                    }
                    else
                    {
                        materialEditor.ShaderProperty(heightParametrization[m_LayerIndex], Styles.heightMapParametrization);
                        if (!heightParametrization[m_LayerIndex].hasMixedValue)
                        {
                            HeightmapParametrization parametrization = (HeightmapParametrization)heightParametrization[m_LayerIndex].floatValue;
                            if (parametrization == HeightmapParametrization.MinMax)
                            {
                                EditorGUI.BeginChangeCheck();
                                materialEditor.ShaderProperty(heightMin[m_LayerIndex], Styles.heightMapMinText);
                                if (EditorGUI.EndChangeCheck())
                                    heightMin[m_LayerIndex].floatValue = Mathf.Min(heightMin[m_LayerIndex].floatValue, heightMax[m_LayerIndex].floatValue);
                                EditorGUI.BeginChangeCheck();
                                materialEditor.ShaderProperty(heightMax[m_LayerIndex], Styles.heightMapMaxText);
                                if (EditorGUI.EndChangeCheck())
                                    heightMax[m_LayerIndex].floatValue = Mathf.Max(heightMin[m_LayerIndex].floatValue, heightMax[m_LayerIndex].floatValue);
                            }
                            else
                            {
                                EditorGUI.BeginChangeCheck();
                                materialEditor.ShaderProperty(heightTessAmplitude[m_LayerIndex], Styles.heightMapAmplitudeText);
                                if (EditorGUI.EndChangeCheck())
                                    heightTessAmplitude[m_LayerIndex].floatValue = Mathf.Max(0f, heightTessAmplitude[m_LayerIndex].floatValue);
                                materialEditor.ShaderProperty(heightTessCenter[m_LayerIndex], Styles.heightMapCenterText);
                            }

                            materialEditor.ShaderProperty(heightOffset[m_LayerIndex], Styles.heightMapOffsetText);
                        }
                    }
                    EditorGUI.indentLevel--;
                }

                // UI only updates intermediate values, this will update the values actually used by the shader.
                if (EditorGUI.EndChangeCheck())
                {
                    // Fetch the surface option block which contains the function to update the displacement datas
                    var surfaceOptionUIBlock = FetchUIBlockInCurrentList< SurfaceOptionUIBlock >();
                    surfaceOptionUIBlock.UpdateDisplacement(m_LayerIndex);
                }
            }

            switch (materialIdValue)
            {
                case MaterialId.LitSSS:
                case MaterialId.LitTranslucent:
                    ShaderSSSAndTransmissionInputGUI();
                    break;
                case MaterialId.LitStandard:
                    // Nothing
                    break;

                // Following mode are not supported by layered lit and will not be call by it
                // as the MaterialId enum don't define it
                case MaterialId.LitAniso:
                    ShaderAnisoInputGUI();
                    break;
                case MaterialId.LitSpecular:
                    ShaderSpecularColorInputGUI();
                    break;
                case MaterialId.LitIridescence:
                    ShaderIridescenceInputGUI();
                    break;

                default:
                    Debug.Assert(false, "Encountered an unsupported MaterialID.");
                    break;
            }

            if (!isLayeredLit)
            {
                ShaderClearCoatInputGUI();
            }

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            materialEditor.ShaderProperty(UVBase[m_LayerIndex], Styles.UVBaseMappingText);
            uvBaseMapping = (UVBaseMapping)UVBase[m_LayerIndex].floatValue;

            X = (uvBaseMapping == UVBaseMapping.UV0) ? 1.0f : 0.0f;
            Y = (uvBaseMapping == UVBaseMapping.UV1) ? 1.0f : 0.0f;
            Z = (uvBaseMapping == UVBaseMapping.UV2) ? 1.0f : 0.0f;
            W = (uvBaseMapping == UVBaseMapping.UV3) ? 1.0f : 0.0f;

            UVMappingMask[m_LayerIndex].colorValue = new Color(X, Y, Z, W);

            if ((uvBaseMapping == UVBaseMapping.Planar) || (uvBaseMapping == UVBaseMapping.Triplanar))
            {
                materialEditor.ShaderProperty(TexWorldScale[m_LayerIndex], Styles.texWorldScaleText);
            }
            materialEditor.TextureScaleOffsetProperty(baseColorMap[m_LayerIndex]);
            if (EditorGUI.EndChangeCheck())
            {
                // Precompute.
                InvTilingScale[m_LayerIndex].floatValue = 2.0f / (Mathf.Abs(baseColorMap[m_LayerIndex].textureScaleAndOffset.x) + Mathf.Abs(baseColorMap[m_LayerIndex].textureScaleAndOffset.y));
                if ((uvBaseMapping == UVBaseMapping.Planar) || (uvBaseMapping == UVBaseMapping.Triplanar))
                {
                    InvTilingScale[m_LayerIndex].floatValue = InvTilingScale[m_LayerIndex].floatValue / TexWorldScale[m_LayerIndex].floatValue;
                }
            }
        }

        void ShaderSSSAndTransmissionInputGUI()
        {
            var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

            if (hdPipeline == null)
                return;

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // We can't cache these fields because of several edge cases like undo/redo or pressing escape in the object picker
                    string guid = HDUtils.ConvertVector4ToGUID(diffusionProfileAsset[m_LayerIndex].vectorValue);
                    DiffusionProfileSettings diffusionProfile = AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>(AssetDatabase.GUIDToAssetPath(guid));

                    // is it okay to do this every frame ?
                    using (var changeScope = new EditorGUI.ChangeCheckScope())
                    {
                        diffusionProfile = (DiffusionProfileSettings)EditorGUILayout.ObjectField(Styles.diffusionProfileText, diffusionProfile, typeof(DiffusionProfileSettings), false);
                        if (changeScope.changed)
                        {
                            Vector4 newGuid = Vector4.zero;
                            float    hash = 0;

                            if (diffusionProfile != null)
                            {
                                guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(diffusionProfile));
                                newGuid = HDUtils.ConvertGUIDToVector4(guid);
                                hash = HDShadowUtils.Asfloat(diffusionProfile.profile.hash);
                            }

                            // encode back GUID and it's hash
                            diffusionProfileAsset[m_LayerIndex].vectorValue = newGuid;
                            diffusionProfileHash[m_LayerIndex].floatValue = hash;
                        }
                    }
                }
            }

            // TODO: does not work with multi-selection
            if ((int)materialID.floatValue == (int)MaterialId.LitSSS && materials[0].GetSurfaceType() != SurfaceType.Transparent)
            {
                materialEditor.ShaderProperty(subsurfaceMask[m_LayerIndex], Styles.subsurfaceMaskText);
                materialEditor.TexturePropertySingleLine(Styles.subsurfaceMaskMapText, subsurfaceMaskMap[m_LayerIndex]);
            }

            if ((int)materialID.floatValue == (int)MaterialId.LitTranslucent ||
                ((int)materialID.floatValue == (int)MaterialId.LitSSS && transmissionEnable.floatValue > 0.0f))
            {
                materialEditor.TexturePropertySingleLine(Styles.thicknessMapText, thicknessMap[m_LayerIndex]);
                if (thicknessMap[m_LayerIndex].textureValue != null)
                {
                    // Display the remap of texture values.
                    Vector2 remap = thicknessRemap[m_LayerIndex].vectorValue;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.MinMaxSlider(Styles.thicknessRemapText, ref remap.x, ref remap.y, 0.0f, 1.0f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        thicknessRemap[m_LayerIndex].vectorValue = remap;
                    }
                }
                else
                {
                    // Allow the user to set the constant value of thickness if no thickness map is provided.
                    materialEditor.ShaderProperty(thickness[m_LayerIndex], Styles.thicknessText);
                }
            }
        }

        void ShaderAnisoInputGUI()
        {
            if ((NormalMapSpace)normalMapSpace[0].floatValue == NormalMapSpace.TangentSpace)
            {
                materialEditor.TexturePropertySingleLine(Styles.tangentMapText, tangentMap);
            }
            else
            {
                materialEditor.TexturePropertySingleLine(Styles.tangentMapOSText, tangentMapOS);
            }
            materialEditor.ShaderProperty(anisotropy, Styles.anisotropyText);
            materialEditor.TexturePropertySingleLine(Styles.anisotropyMapText, anisotropyMap);
        }

        void ShaderSpecularColorInputGUI()
        {
            materialEditor.TexturePropertySingleLine(Styles.specularColorText, specularColorMap, specularColor);
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(energyConservingSpecularColor, Styles.energyConservingSpecularColorText);
            EditorGUI.indentLevel--;
        }

        void ShaderIridescenceInputGUI()
        {
            materialEditor.TexturePropertySingleLine(Styles.iridescenceMaskText, iridescenceMaskMap, iridescenceMask);

            if (iridescenceThicknessMap.textureValue != null)
            {
                materialEditor.TexturePropertySingleLine(Styles.iridescenceThicknessMapText, iridescenceThicknessMap);
                // Display the remap of texture values.
                Vector2 remap = iridescenceThicknessRemap.vectorValue;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider(Styles.iridescenceThicknessRemapText, ref remap.x, ref remap.y, 0.0f, 1.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    iridescenceThicknessRemap.vectorValue = remap;
                }
            }
            else
            {
                // Allow the user to set the constant value of thickness if no thickness map is provided.
                materialEditor.TexturePropertySingleLine(Styles.iridescenceThicknessMapText, iridescenceThicknessMap, iridescenceThickness);
            }
        }

        void ShaderClearCoatInputGUI()
        {
            materialEditor.TexturePropertySingleLine(Styles.coatMaskText, coatMaskMap, coatMask);
        }

        void DrawLayerOptionsGUI()
        {
            EditorGUI.showMixedValue = layerCount.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            int newLayerCount = EditorGUILayout.IntSlider(Styles.layerCountText, (int)layerCount.floatValue, 2, 4);
            if (EditorGUI.EndChangeCheck())
            {
                Material material = materialEditor.target as Material;
                Undo.RecordObject(material, "Change layer count");
                // Technically not needed (i think), TODO: check
                // numLayer = newLayerCount;
                layerCount.floatValue = (float)newLayerCount;
            }

            materialEditor.TexturePropertySingleLine(Styles.layerMapMaskText, layerMaskMap);

            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(UVBlendMask, Styles.UVBlendMaskText);
            UVBaseMapping uvBlendMask = (UVBaseMapping)UVBlendMask.floatValue;

            float X, Y, Z, W;
            X = (uvBlendMask == UVBaseMapping.UV0) ? 1.0f : 0.0f;
            Y = (uvBlendMask == UVBaseMapping.UV1) ? 1.0f : 0.0f;
            Z = (uvBlendMask == UVBaseMapping.UV2) ? 1.0f : 0.0f;
            W = (uvBlendMask == UVBaseMapping.UV3) ? 1.0f : 0.0f;

            UVMappingMaskBlendMask.colorValue = new Color(X, Y, Z, W);

            if (((UVBaseMapping)UVBlendMask.floatValue == UVBaseMapping.Planar) ||
                ((UVBaseMapping)UVBlendMask.floatValue == UVBaseMapping.Triplanar))
            {
                materialEditor.ShaderProperty(texWorldScaleBlendMask, Styles.layerTexWorldScaleText);
            }
            materialEditor.TextureScaleOffsetProperty(layerMaskMap);
            EditorGUI.indentLevel--;

            materialEditor.ShaderProperty(vertexColorMode, Styles.vertexColorModeText);

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = useMainLayerInfluence.hasMixedValue;
            bool mainLayerModeInfluenceEnable = EditorGUILayout.Toggle(Styles.useMainLayerInfluenceModeText, useMainLayerInfluence.floatValue > 0.0f);
            if (EditorGUI.EndChangeCheck())
            {
                useMainLayerInfluence.floatValue = mainLayerModeInfluenceEnable ? 1.0f : 0.0f;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = useHeightBasedBlend.hasMixedValue;
            m_UseHeightBasedBlend = EditorGUILayout.Toggle(Styles.useHeightBasedBlendText, useHeightBasedBlend.floatValue > 0.0f);
            if (EditorGUI.EndChangeCheck())
            {
                useHeightBasedBlend.floatValue = m_UseHeightBasedBlend ? 1.0f : 0.0f;
            }

            if (m_UseHeightBasedBlend)
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(heightTransition, Styles.heightTransition);
                EditorGUI.indentLevel--;
            }

            materialEditor.ShaderProperty(objectScaleAffectTile, mainLayerModeInfluenceEnable ? Styles.objectScaleAffectTileText2 : Styles.objectScaleAffectTileText);
        }
    }
}