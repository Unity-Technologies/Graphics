using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using System.Linq;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// The UI block that represents lit surface input properties.
    /// This block is shared for Lit and Layered surface inputs + tesselation variants.
    /// </summary>
    public class LitSurfaceInputsUIBlock : MaterialUIBlock
    {
        /// <summary>Options for lit surface input features.</summary>
        [Flags]
        public enum Features
        {
            /// <summary>Minimal Lit Surface Inputs fields.</summary>
            None = 0,
            /// <summary>Displays Coat Mask fields.</summary>
            CoatMask = 1 << 0,
            /// <summary>Displays the height Map fields.</summary>
            HeightMap = 1 << 1,
            /// <summary>Displays the layer Options fields.</summary>
            LayerOptions = 1 << 2,
            /// <summary>Displays the foldout header as a SubHeader.</summary>
            SubHeader = 1 << 3,
            /// <summary>Displays the default surface inputs.</summary>
            Standard = 1 << 4,
            /// <summary>Displays everything with a header.</summary>
            All = ~0 ^ SubHeader // By default we don't want a sub-header
        }

        internal class Styles
        {
            public static GUIContent header { get; } = EditorGUIUtility.TrTextContent("Surface Inputs");

            public static GUIContent colorText = new GUIContent("Color", " Albedo (RGB) and Transparency (A).");

            public static GUIContent baseColorText = new GUIContent("Base Map", "Specifies the base color (RGB) and opacity (A) of the Material.");

            public static GUIContent metallicText = new GUIContent("Metallic", "Controls the scale factor for the Material's metallic effect.");
            public static GUIContent metallicRemappingText = new GUIContent("Metallic Remapping", "Controls a remap for the metallic channel in the Mask Map.");
            public static GUIContent smoothnessText = new GUIContent("Smoothness", "Controls the scale factor for the Material's smoothness.");
            public static GUIContent smoothnessRemappingText = new GUIContent("Smoothness Remapping", "Controls a remap for the smoothness channel in the Mask Map.");
            public static GUIContent aoRemappingText = new GUIContent("Ambient Occlusion Remapping", "Controls a remap for the ambient occlusion channel in the Mask Map.");
            public static GUIContent maskMapSText = new GUIContent("Mask Map", "Specifies the Mask Map for this Material - Metallic (R), Ambient occlusion (G), Detail mask (B), Smoothness (A).");
            public static GUIContent maskMapSpecularText = new GUIContent("Mask Map", "Specifies the Mask Map for this Material - Ambient occlusion (G), Detail mask (B), Smoothness (A).");
            public static GUIContent alphaRemappingText = new GUIContent("Alpha Remapping", "Controls a remap for the alpha channel in the Base Color.");

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
            public static GUIContent uvMappingSpace = new GUIContent("UV Mapping Space", "Sets the space for the input position used for Planar/Trilinear mapping.");

            // Specular color
            public static GUIContent energyConservingSpecularColorText = new GUIContent("Energy Conserving Specular Color", "When enabled, HDRP simulates energy conservation when using Specular Color mode. This results in high Specular Color values producing lower Diffuse Color values.");
            public static GUIContent specularColorText = new GUIContent("Specular Color", "Specifies the Specular color (RGB) of this Material.");

            // Subsurface
            public static GUIContent diffusionProfileText = new GUIContent("Diffusion Profile", "Specifies the Diffusion Profie HDRP uses to determine the behavior of the subsurface scattering/transmission effect.");
            public static GUIContent subsurfaceEnableText = new GUIContent("Subsurface Scattering", "Enables the subsurface scattering on the material.");
            public static GUIContent subsurfaceMaskText = new GUIContent("Subsurface Mask", "Specifies the Subsurface mask map (R) for this Material and controls the overall strength of the subsurface scattering effect.");
            public static GUIContent transmissionMaskText = new GUIContent("Transmission Mask", "Specifies the Transmission mask map (R) for this Material and controls the overall strength of the transmission effect.\nThis has no effect when using raytracing.");
            public static GUIContent thicknessText = new GUIContent("Thickness", "Controls the strength of the Thickness Map, low values allow some light to transmit through the object.");
            public static GUIContent thicknessMapText = new GUIContent("Thickness Map", "Specifies the Thickness Map (R) for this Material - This map describes the thickness of the object. When subsurface scattering is enabled, low values allow some light to transmit through the object.");
            public static GUIContent thicknessRemapText = new GUIContent("Thickness Remapping", "Controls a remap for the Thickness Map from [0, 1] to the specified range.");

            // Iridescence
            public static GUIContent iridescenceMaskText = new GUIContent("Iridescence Mask", "Specifies the Iridescence Mask (R) for this Material - This map controls the intensity of the iridescence.");
            public static GUIContent iridescenceThicknessText = new GUIContent("Iridescence Layer Thickness");
            public static GUIContent iridescenceThicknessMapText = new GUIContent("Iridescence Layer Thickness map", "Specifies the Thickness map (R) of the thin iridescence layer over the material. Unit is micrometer multiplied by 3. A value of 1 is remapped to 3 micrometers or 3000 nanometers.");
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
        MaterialProperty[] TexWorldScale = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] InvTilingScale = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] UVMappingMask = new MaterialProperty[kMaxLayerCount];

        MaterialProperty[] baseColor = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] baseColorMap = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] metallic = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] metallicRemapMin = new MaterialProperty[kMaxLayerCount];
        const string kMetallicRemapMin = "_MetallicRemapMin";
        MaterialProperty[] metallicRemapMax = new MaterialProperty[kMaxLayerCount];
        const string kMetallicRemapMax = "_MetallicRemapMax";
        MaterialProperty[] smoothness = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] smoothnessRemapMin = new MaterialProperty[kMaxLayerCount];
        const string kSmoothnessRemapMin = "_SmoothnessRemapMin";
        MaterialProperty[] smoothnessRemapMax = new MaterialProperty[kMaxLayerCount];
        const string kSmoothnessRemapMax = "_SmoothnessRemapMax";
        MaterialProperty[] alphaRemapMin = new MaterialProperty[kMaxLayerCount];
        const string kAlphaRemapMin = "_AlphaRemapMin";
        MaterialProperty[] alphaRemapMax = new MaterialProperty[kMaxLayerCount];
        const string kAlphaRemapMax = "_AlphaRemapMax";
        MaterialProperty[] aoRemapMin = new MaterialProperty[kMaxLayerCount];
        const string kAORemapMin = "_AORemapMin";
        MaterialProperty[] aoRemapMax = new MaterialProperty[kMaxLayerCount];
        const string kAORemapMax = "_AORemapMax";

        MaterialProperty[] maskMap = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] normalScale = new MaterialProperty[kMaxLayerCount];
        const string kNormalScale = "_NormalScale";
        MaterialProperty[] normalMap = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] normalMapOS = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] bentNormalMap = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] bentNormalMapOS = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] normalMapSpace = new MaterialProperty[kMaxLayerCount];

        MaterialProperty[] heightMap = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] heightAmplitude = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] heightCenter = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] heightPoMAmplitude = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] heightTessCenter = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] heightTessAmplitude = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] heightMin = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] heightMax = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] heightOffset = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] heightParametrization = new MaterialProperty[kMaxLayerCount];

        MaterialProperty displacementMode = null;

        MaterialProperty tangentMap = null;
        MaterialProperty tangentMapOS = null;
        MaterialProperty anisotropy = null;
        const string kAnisotropy = "_Anisotropy";
        MaterialProperty anisotropyMap = null;

        MaterialProperty uvMappingSpace = null;

        MaterialProperty energyConservingSpecularColor = null;
        const string kEnergyConservingSpecularColor = "_EnergyConservingSpecularColor";
        MaterialProperty specularColor = null;
        const string kSpecularColor = "_SpecularColor";
        MaterialProperty specularColorMap = null;

        MaterialProperty[] diffusionProfileHash = new MaterialProperty[kMaxLayerCount];
        const string kDiffusionProfileHash = "_DiffusionProfileHash";
        MaterialProperty[] diffusionProfileAsset = new MaterialProperty[kMaxLayerCount];
        const string kDiffusionProfileAsset = "_DiffusionProfileAsset";
        MaterialProperty[] subsurfaceMask = new MaterialProperty[kMaxLayerCount];
        const string kSubsurfaceMask = "_SubsurfaceMask";
        MaterialProperty[] subsurfaceMaskMap = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] transmissionMask = new MaterialProperty[kMaxLayerCount];
        const string kTransmissionMask = "_TransmissionMask";
        MaterialProperty[] transmissionMaskMap = new MaterialProperty[kMaxLayerCount];
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
        MaterialProperty materialID = null;
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

        Features m_Features;
        int m_LayerCount;
        int m_LayerIndex;
        bool m_UseHeightBasedBlend;

        bool isLayeredLit => m_LayerCount > 1;

        /// <summary>
        /// Constructs a LitSurfaceInputsUIBlock based on the parameters.
        /// </summary>
        /// <param name="expandableBit">Bit index to store the foldout state.</param>
        /// <param name="layerCount">Number of layers in the shader.</param>
        /// <param name="layerIndex">Current layer index to display. 0 if it's not a layered shader</param>
        /// <param name="features">Features of the block.</param>
        public LitSurfaceInputsUIBlock(ExpandableBit expandableBit, int layerCount = 1, int layerIndex = 0, Features features = Features.All)
            : base(expandableBit, Styles.header)
        {
            m_Features = features;
            m_LayerCount = layerCount;
            m_LayerIndex = layerIndex;
        }

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties()
        {
            UVBase = FindPropertyLayered(kUVBase, m_LayerCount, true);
            TexWorldScale = FindPropertyLayered(kTexWorldScale, m_LayerCount);
            InvTilingScale = FindPropertyLayered(kInvTilingScale, m_LayerCount);
            UVMappingMask = FindPropertyLayered(kUVMappingMask, m_LayerCount);
            TexWorldScale = FindPropertyLayered(kTexWorldScale, m_LayerCount);
            uvMappingSpace = FindProperty(kObjectSpaceUVMapping);

            baseColor = FindPropertyLayered(kBaseColor, m_LayerCount);
            baseColorMap = FindPropertyLayered(kBaseColorMap, m_LayerCount);

            metallic = FindPropertyLayered(kMetallic, m_LayerCount);
            metallicRemapMin = FindPropertyLayered(kMetallicRemapMin, m_LayerCount);
            metallicRemapMax = FindPropertyLayered(kMetallicRemapMax, m_LayerCount);
            smoothness = FindPropertyLayered(kSmoothness, m_LayerCount);
            smoothnessRemapMin = FindPropertyLayered(kSmoothnessRemapMin, m_LayerCount);
            smoothnessRemapMax = FindPropertyLayered(kSmoothnessRemapMax, m_LayerCount);
            alphaRemapMin = FindPropertyLayered(kAlphaRemapMin, m_LayerCount);
            alphaRemapMax = FindPropertyLayered(kAlphaRemapMax, m_LayerCount);
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
            transmissionMask = FindPropertyLayered(kTransmissionMask, m_LayerCount);
            transmissionMaskMap = FindPropertyLayered(kTransmissionMaskMap, m_LayerCount);
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

        /// <summary>
        /// Property that specifies if the scope is a subheader
        /// </summary>
        protected override bool isSubHeader => (m_Features & Features.SubHeader) != 0;

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
        protected override void OnGUIOpen()
        {
            if ((m_Features & Features.Standard) != 0)
                DrawSurfaceInputsGUI();
            if ((m_Features & Features.LayerOptions) != 0)
                DrawLayerOptionsGUI();
        }

        void DrawSurfaceInputsGUI()
        {
            UVBaseMapping uvBaseMapping = (UVBaseMapping)UVBase[m_LayerIndex].floatValue;
            float X, Y, Z, W;

            materialEditor.TexturePropertySingleLine(Styles.baseColorText, baseColorMap[m_LayerIndex], baseColor[m_LayerIndex]);

            if (baseColorMap[m_LayerIndex].textureValue != null && materials.All(m => m.GetSurfaceType() == SurfaceType.Transparent))
            {
                materialEditor.MinMaxShaderProperty(alphaRemapMin[m_LayerIndex], alphaRemapMax[m_LayerIndex], 0.0f, 1.0f, Styles.alphaRemappingText);
            }

            materialEditor.TexturePropertySingleLine((materials.All(m => m.GetMaterialId() == MaterialId.LitSpecular)) ? Styles.maskMapSpecularText : Styles.maskMapSText, maskMap[m_LayerIndex]);

            bool hasMetallic = materials.All(m =>
                m.GetMaterialId() == MaterialId.LitStandard ||
                m.GetMaterialId() == MaterialId.LitAniso ||
                m.GetMaterialId() == MaterialId.LitIridescence);

            if (maskMap[m_LayerIndex].textureValue == null)
            {
                if (hasMetallic)
                    materialEditor.ShaderProperty(metallic[m_LayerIndex], Styles.metallicText);
                materialEditor.ShaderProperty(smoothness[m_LayerIndex], Styles.smoothnessText);
            }
            else
            {
                if (hasMetallic)
                    materialEditor.MinMaxShaderProperty(metallicRemapMin[m_LayerIndex], metallicRemapMax[m_LayerIndex], 0.0f, 1.0f, Styles.metallicRemappingText);

                materialEditor.MinMaxShaderProperty(smoothnessRemapMin[m_LayerIndex], smoothnessRemapMax[m_LayerIndex], 0.0f, 1.0f, Styles.smoothnessRemappingText);
                materialEditor.MinMaxShaderProperty(aoRemapMin[m_LayerIndex], aoRemapMax[m_LayerIndex], 0.0f, 1.0f, Styles.aoRemappingText);
            }

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

            DisplacementMode displaceMode = SurfaceOptionUIBlock.GetFilteredDisplacementMode(displacementMode);
            if (displaceMode != DisplacementMode.None || (m_Features & Features.HeightMap) != 0)
            {
                materialEditor.TexturePropertySingleLine(Styles.heightMapText, heightMap[m_LayerIndex]);
                if (!heightMap[m_LayerIndex].hasMixedValue && heightMap[m_LayerIndex].textureValue != null && !SurfaceOptionUIBlock.HasMixedDisplacementMode(displacementMode))
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
            }

            if (materials.All(m => m.GetMaterialId() == materials[0].GetMaterialId()))
            {
                // We can use materials[0] because all the material IDs have the same value
                switch (materials[0].GetMaterialId())
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
                // The uv mapping is not always defined in shader (e.g. for layered lit).
                if (uvMappingSpace != null)
                    materialEditor.ShaderProperty(uvMappingSpace, Styles.uvMappingSpace);

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

            DiffusionProfileMaterialUI.OnGUI(materialEditor, diffusionProfileAsset[m_LayerIndex], diffusionProfileHash[m_LayerIndex], m_LayerIndex);

            if ((int)materialID.floatValue == (int)MaterialId.LitSSS && materials.All(m => m.GetSurfaceType() != SurfaceType.Transparent))
            {
                materialEditor.TexturePropertySingleLine(Styles.subsurfaceMaskText, subsurfaceMaskMap[m_LayerIndex], subsurfaceMask[m_LayerIndex]);
            }

            if ((int)materialID.floatValue == (int)MaterialId.LitTranslucent ||
                ((int)materialID.floatValue == (int)MaterialId.LitSSS && transmissionEnable.floatValue > 0.0f))
            {
                materialEditor.TexturePropertySingleLine(Styles.transmissionMaskText, transmissionMaskMap[m_LayerIndex], transmissionMask[m_LayerIndex]);

                if (thicknessMap[m_LayerIndex].textureValue != null)
                {
                    materialEditor.TexturePropertySingleLine(Styles.thicknessMapText, thicknessMap[m_LayerIndex]);
                    // Display the remap of texture values.
                    materialEditor.MinMaxShaderProperty(thicknessRemap[m_LayerIndex], 0.0f, 1.0f, Styles.thicknessRemapText);
                }
                else
                {
                    // Allow the user to set the constant value of thickness if no thickness map is provided.
                    materialEditor.TexturePropertySingleLine(Styles.thicknessText, thicknessMap[m_LayerIndex], thickness[m_LayerIndex]);
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
                materialEditor.MinMaxShaderProperty(iridescenceThicknessRemap, 0.0f, 1.0f, Styles.iridescenceThicknessRemapText);
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
            materialEditor.IntSliderShaderProperty(layerCount, 2, 4, Styles.layerCountText);

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
            materialEditor.ShaderProperty(useMainLayerInfluence, Styles.useMainLayerInfluenceModeText);
            materialEditor.ShaderProperty(useHeightBasedBlend, Styles.useHeightBasedBlendText);

            if (m_UseHeightBasedBlend)
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(heightTransition, Styles.heightTransition);
                EditorGUI.indentLevel--;
            }

            bool mainLayerModeInfluenceEnable = useMainLayerInfluence.floatValue > 0.0f;
            materialEditor.ShaderProperty(objectScaleAffectTile, mainLayerModeInfluenceEnable ? Styles.objectScaleAffectTileText2 : Styles.objectScaleAffectTileText);
        }
    }
}
