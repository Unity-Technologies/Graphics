using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class StackLitGUI : BaseUnlitGUI
    {
        protected static class Styles
        {
            public static string InputsText = "Inputs";
            public static GUIContent doubleSidedNormalModeText = new GUIContent("Normal mode", "This will modify the normal base on the selected mode. Mirror: Mirror the normal with vertex normal plane, Flip: Flip the normal");

            public static GUIContent baseColorText = new GUIContent("Base Color + Opacity", "Albedo (RGB) and Opacity (A)");


            // Scalar scale factors for: metallic and the two lobe perceptual smoothness.
            public static GUIContent metallicText = new GUIContent("Metallic", "Metallic scale factor");
            public static GUIContent smoothnessAText = new GUIContent("Primary Lobe Smoothness", "Primary lobe smoothness scale factor");
            public static GUIContent smoothnessBText = new GUIContent("Secondary Lobe Smoothness", "Secondary lobe smoothness scale factor");
            public static GUIContent lobeMixText = new GUIContent("Lobe Mixing", "Lobe mixing factor");

            public static GUIContent smoothnessARemappingText = new GUIContent("Primary Lobe Smoothness Remapping", "Primary lobe smoothness remapping");
            public static GUIContent smoothnessBRemappingText = new GUIContent("Secondary Lobe Smoothness Remapping", "Secondary  lobe smoothness remapping");
            public static GUIContent maskMapASText = new GUIContent("Primary mask map - M(R), AO(G), D(B), S1(A)", "Primary mask map");
            public static GUIContent maskMapBSText = new GUIContent("Secondary mask Map - (R), (G), (B), S2(A)", "Secondary mask map");

            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map (BC7/BC5/DXT5(nm))");

            public static GUIContent UVBaseMappingText = new GUIContent("UV mapping usage", "");


            // Emissive
            public static string emissiveLabelText = "Emissive Inputs";
            public static GUIContent emissiveText = new GUIContent("Emissive Color", "Emissive");
            public static GUIContent emissiveIntensityText = new GUIContent("Emissive Intensity", "Emissive");
            public static GUIContent albedoAffectEmissiveText = new GUIContent("Albedo Affect Emissive", "Specifies whether or not the emissive color is multiplied by the albedo.");

        }

        public enum DoubleSidedNormalMode
        {
            Flip,
            Mirror,
            None
        }

        public enum UVBaseMapping
        {
            UV0,
            UV1,
            UV2,
            UV3,
            Planar,
            Triplanar
        }

        protected MaterialProperty doubleSidedNormalMode = null;
        protected const string kDoubleSidedNormalMode = "_DoubleSidedNormalMode";

        // Example UV mapping mask, TODO: could have for multiple maps, and channel mask for scalars
        protected MaterialProperty UVBase = null;
        protected const string kUVBase = "_UVBase";
        protected MaterialProperty UVMappingMask = null;
        protected const string kUVMappingMask = "_UVMappingMask"; // hidden, see enum material property drawer in .shader


        protected MaterialProperty baseColor = null;
        protected const string kBaseColor = "_BaseColor";
        protected MaterialProperty baseColorMap = null;
        protected const string kBaseColorMap = "_BaseColorMap";

        protected const string kMetallic = "_Metallic";
        protected MaterialProperty metallic = null;

        // Primary lobe smoothness
        protected MaterialProperty smoothnessA = null;
        protected const string kSmoothnessA = "_SmoothnessA";
        protected MaterialProperty smoothnessARemapMin = null;
        protected const string kSmoothnessARemapMin = "_SmoothnessARemapMin";
        protected MaterialProperty smoothnessARemapMax = null;
        protected const string kSmoothnessARemapMax = "_SmoothnessARemapMax";
        protected const string klobeMix = "_LobeMix";
        protected MaterialProperty lobeMix = null;

        // Secondary lobe smoothness
        protected MaterialProperty smoothnessB = null;
        protected const string kSmoothnessB = "_SmoothnessB";
        protected MaterialProperty smoothnessBRemapMin = null;
        protected const string kSmoothnessBRemapMin = "_SmoothnessBRemapMin";
        protected MaterialProperty smoothnessBRemapMax = null;
        protected const string kSmoothnessBRemapMax = "_SmoothnessBRemapMax";


        // Two mask maps for the two smoothnesses
        protected MaterialProperty maskMapA = null;
        protected const string kMaskMapA = "_MaskMapA";
        protected MaterialProperty maskMapB = null;
        protected const string kMaskMapB = "_MaskMapB";

        protected MaterialProperty normalScale = null;
        protected const string kNormalScale = "_NormalScale";
        protected MaterialProperty normalMap = null;
        protected const string kNormalMap = "_NormalMap";

        protected MaterialProperty emissiveColor = null;
        protected const string kEmissiveColor = "_EmissiveColor";
        protected MaterialProperty emissiveColorMap = null;
        protected const string kEmissiveColorMap = "_EmissiveColorMap";
        protected MaterialProperty emissiveIntensity = null;
        protected const string kEmissiveIntensity = "_EmissiveIntensity";
        protected MaterialProperty albedoAffectEmissive = null;
        protected const string kAlbedoAffectEmissive = "_AlbedoAffectEmissive";

        protected override void FindBaseMaterialProperties(MaterialProperty[] props)
        {
            base.FindBaseMaterialProperties(props);

            doubleSidedNormalMode = FindProperty(kDoubleSidedNormalMode, props);
        }

        override protected void FindMaterialProperties(MaterialProperty[] props)
        {

            UVBase = FindProperty(kUVBase, props);
            UVMappingMask = FindProperty(kUVMappingMask, props);

            baseColor = FindProperty(kBaseColor, props);
            baseColorMap = FindProperty(kBaseColorMap, props);

            metallic = FindProperty(kMetallic, props);

            smoothnessA = FindProperty(kSmoothnessA, props);
            smoothnessARemapMin = FindProperty(kSmoothnessARemapMin, props);
            smoothnessARemapMax = FindProperty(kSmoothnessARemapMax, props);

            smoothnessB = FindProperty(kSmoothnessB, props);
            smoothnessBRemapMin = FindProperty(kSmoothnessBRemapMin, props);
            smoothnessBRemapMax = FindProperty(kSmoothnessBRemapMax, props);

            lobeMix = FindProperty(klobeMix, props);

            maskMapA = FindProperty(kMaskMapA, props);
            maskMapB = FindProperty(kMaskMapB, props);

            normalMap = FindProperty(kNormalMap, props);
            normalScale = FindProperty(kNormalScale, props);

            emissiveColor = FindProperty(kEmissiveColor, props);
            emissiveColorMap = FindProperty(kEmissiveColorMap, props);
            emissiveIntensity = FindProperty(kEmissiveIntensity, props);
            albedoAffectEmissive = FindProperty(kAlbedoAffectEmissive, props);
        }


        protected override void BaseMaterialPropertiesGUI()
        {
            base.BaseMaterialPropertiesGUI();

            EditorGUI.indentLevel++;

            // This follow double sided option, see BaseUnlitUI.BaseMaterialPropertiesGUI()
            // Don't put anything between base.BaseMaterialPropertiesGUI(); above and this:
            if (doubleSidedEnable.floatValue > 0.0f)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(doubleSidedNormalMode, Styles.doubleSidedNormalModeText);
                EditorGUI.indentLevel--;
            }

            //TODO: m_MaterialEditor.ShaderProperty(enableMotionVectorForVertexAnimation, StylesBaseUnlit.enableMotionVectorForVertexAnimationText);
            //refs to this ?

            EditorGUI.indentLevel--;

        }

        protected override void MaterialPropertiesGUI(Material material)
        {
            EditorGUILayout.LabelField(Styles.InputsText, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            m_MaterialEditor.TexturePropertySingleLine(Styles.baseColorText, baseColorMap, baseColor);

            m_MaterialEditor.ShaderProperty(metallic, Styles.metallicText);

            // maskMaps and smoothness rescaling controls:
            if(maskMapA.textureValue == null)
            {
                m_MaterialEditor.ShaderProperty(smoothnessA, Styles.smoothnessAText);
            }
            else
            {
                float remapMin = smoothnessARemapMin.floatValue;
                float remapMax = smoothnessARemapMax.floatValue;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider(Styles.smoothnessARemappingText, ref remapMin, ref remapMax, 0.0f, 1.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    smoothnessARemapMin.floatValue = remapMin;
                    smoothnessARemapMax.floatValue = remapMax;
                }
            }
            if(maskMapB.textureValue == null)
            {
                m_MaterialEditor.ShaderProperty(smoothnessB, Styles.smoothnessBText);
            }
            else
            {
                float remapMin = smoothnessBRemapMin.floatValue;
                float remapMax = smoothnessBRemapMax.floatValue;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider(Styles.smoothnessBRemappingText, ref remapMin, ref remapMax, 0.0f, 1.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    smoothnessBRemapMin.floatValue = remapMin;
                    smoothnessBRemapMax.floatValue = remapMax;
                }
            }
            m_MaterialEditor.ShaderProperty(lobeMix, Styles.lobeMixText);
            m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapASText, maskMapA);
            m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapBSText, maskMapB);


            // Normal map: 
            m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap, normalScale);

            // UV Mapping:
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck(); // UV mapping selection
            m_MaterialEditor.ShaderProperty(UVBase, Styles.UVBaseMappingText);

            UVBaseMapping uvBaseMapping = (UVBaseMapping)UVBase.floatValue;

            float X, Y, Z, W;
            X = (uvBaseMapping == UVBaseMapping.UV0) ? 1.0f : 0.0f;
            Y = (uvBaseMapping == UVBaseMapping.UV1) ? 1.0f : 0.0f;
            Z = (uvBaseMapping == UVBaseMapping.UV2) ? 1.0f : 0.0f;
            W = (uvBaseMapping == UVBaseMapping.UV3) ? 1.0f : 0.0f;

            UVMappingMask.colorValue = new Color(X, Y, Z, W);


            //TODO:
            //if ((uvBaseMapping == UVBaseMapping.Planar) || (uvBaseMapping == UVBaseMapping.Triplanar))
            //{
            //    m_MaterialEditor.ShaderProperty(TexWorldScale, Styles.texWorldScaleText);
            //}
            m_MaterialEditor.TextureScaleOffsetProperty(baseColorMap);
            if (EditorGUI.EndChangeCheck()) // ...UV mapping selection
            {
            }

            EditorGUI.indentLevel--; // inputs
            EditorGUILayout.Space();

            // Surface type:
            var surfaceTypeValue = (SurfaceType)surfaceType.floatValue;
            if (surfaceTypeValue == SurfaceType.Transparent)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(StylesBaseUnlit.TransparencyInputsText, EditorStyles.boldLabel);
                ++EditorGUI.indentLevel;

                DoDistortionInputsGUI();

                --EditorGUI.indentLevel;
            }


            // TODO: see DoEmissiveGUI( ) in LitUI.cs: custom uvmapping for emissive
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.emissiveLabelText, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            m_MaterialEditor.TexturePropertySingleLine(Styles.emissiveText, emissiveColorMap, emissiveColor);
            m_MaterialEditor.ShaderProperty(emissiveIntensity, Styles.emissiveIntensityText);
            m_MaterialEditor.ShaderProperty(albedoAffectEmissive, Styles.albedoAffectEmissiveText);
            EditorGUI.indentLevel--;
        }

        protected override void MaterialPropertiesAdvanceGUI(Material material)
        {
        }

        protected override void VertexAnimationPropertiesGUI()
        {

        }

        protected override bool ShouldEmissionBeEnabled(Material mat)
        {
            return mat.GetFloat(kEmissiveIntensity) > 0.0f;
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            //TODO see BaseLitUI.cs:SetupBaseLitKeywords (stencil etc)
            SetupBaseUnlitKeywords(material);
            SetupBaseUnlitMaterialPass(material);

            bool doubleSidedEnable = material.GetFloat(kDoubleSidedEnable) > 0.0f;

            if (doubleSidedEnable)
            {
                DoubleSidedNormalMode doubleSidedNormalMode = (DoubleSidedNormalMode)material.GetFloat(kDoubleSidedNormalMode);
                switch (doubleSidedNormalMode)
                {
                    case DoubleSidedNormalMode.Mirror: // Mirror mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, -1.0f, 0.0f));
                        break;

                    case DoubleSidedNormalMode.Flip: // Flip mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(-1.0f, -1.0f, -1.0f, 0.0f));
                        break;

                    case DoubleSidedNormalMode.None: // None mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
                        break;
                }
            }

            //NOTE: For SSS in forward and split lighting, obviously we don't have a gbuffer pass, 
            // so no stencil tagging there, but velocity? To check...
            
            //TODO: stencil state, displacement, wind, depthoffset, tesselation

            SetupMainTexForAlphaTestGI("_BaseColorMap", "_BaseColor", material);

            //TODO: disable DBUFFER


            CoreUtils.SetKeyword(material, "_NORMALMAP_TANGENT_SPACE", true);
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap));
            CoreUtils.SetKeyword(material, "_MASKMAPA", material.GetTexture(kMaskMapA));
            CoreUtils.SetKeyword(material, "_MASKMAPB", material.GetTexture(kMaskMapB));

            bool needUV2 = (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV2;
            bool needUV3 = (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV3;

            if (needUV3)
            {
                material.DisableKeyword("_REQUIRE_UV2");
                material.EnableKeyword("_REQUIRE_UV3");
            }
            else if (needUV2)
            {
                material.EnableKeyword("_REQUIRE_UV2");
                material.DisableKeyword("_REQUIRE_UV3");
            }
            else
            {
                material.DisableKeyword("_REQUIRE_UV2");
                material.DisableKeyword("_REQUIRE_UV3");
            }

            CoreUtils.SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));
        }
    }
} // namespace UnityEditor
