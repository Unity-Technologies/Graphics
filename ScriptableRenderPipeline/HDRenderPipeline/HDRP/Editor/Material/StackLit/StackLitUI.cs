using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class StackLitGUI : BaseMaterialGUI
    {
        #region Strings

        protected const string k_DoubleSidedNormalMode = "_DoubleSidedNormalMode";

        protected const string k_UVBase = "_UVBase";

        // Base
        protected const string k_BaseColor = "_BaseColor";
        protected const string k_BaseColorMap = "_BaseColorMap";

        protected const string k_Metallic = "_Metallic";
        protected const string k_MetallicMap = "_MetallicMap";
        protected const string k_MetallicRemapMin = "_MetallicRemap";

        protected const string k_Smoothness1 = "_SmoothnessA";
        protected const string k_Smoothness1Map = "_SmoothnessAMap";
        protected const string k_Smoothness1RemapMin = "_SmoothnessARemap";

        protected const string k_NormalMap = "_NormalMap";
        protected const string k_NormalScale = "_NormalScale";

        // Emissive
        protected const string k_EmissiveColor = "_EmissiveColor";
        protected const string k_EmissiveColorMap = "_EmissiveColorMap";
        protected const string k_EmissiveIntensity = "_EmissiveIntensity";
        protected const string k_AlbedoAffectEmissive = "_AlbedoAffectEmissive";

        // SSS
        protected const string k_DiffusionProfileName = "_DiffusionProfile";
        protected const string k_SubsurfaceMaskName = "_SubsurfaceMask";
        protected const string k_SubsurfaceMaskMap = "_SubsurfaceMaskMap";

        protected const string k_ThicknessName = "_Thickness";
        protected const string k_ThicknessMapName = "_ThicknessMap";
        protected const string k_ThicknessRemapName = "_ThicknessRemap";

        // Second Lobe.
        protected const string k_Smoothness2 = "_SmoothnessB";
        protected const string k_Smoothness2Map = "_SmoothnessBMap";
        protected const string k_SmoothnessRemap2Min = "_SmoothnessBRemap";

        protected const string k_LobeMix = "_LobeMix";

        //// transparency params
        //protected MaterialProperty transmissionEnable = null;
        //protected const string kTransmissionEnable = "_TransmissionEnable";

        //protected MaterialProperty ior = null;
        //protected const string kIor = "_Ior";
        //protected MaterialProperty transmittanceColor = null;
        //protected const string kTransmittanceColor = "_TransmittanceColor";
        //protected MaterialProperty transmittanceColorMap = null;
        //protected const string kTransmittanceColorMap = "_TransmittanceColorMap";
        //protected MaterialProperty atDistance = null;
        //protected const string kATDistance = "_ATDistance";
        //protected MaterialProperty thicknessMultiplier = null;
        //protected const string kThicknessMultiplier = "_ThicknessMultiplier";
        //protected MaterialProperty refractionModel = null;
        //protected const string kRefractionModel = "_RefractionModel";
        //protected MaterialProperty refractionSSRayModel = null;
        //protected const string kRefractionSSRayModel = "_RefractionSSRayModel";
        #endregion

        // Add the properties into an array.
        private readonly GroupProperty _baseMaterialProperties = null;
        private readonly GroupProperty _materialProperties = null;

        public StackLitGUI()
        {
            _baseMaterialProperties = new GroupProperty(this, new BaseProperty[]
            {
                // JFFTODO: Find the proper condition, and proper way to display this.
                new Property(this, k_DoubleSidedNormalMode, "Normal mode", "This will modify the normal base on the selected mode. Mirror: Mirror the normal with vertex normal plane, Flip: Flip the normal.", false), 
            });

            _materialProperties = new GroupProperty(this, new BaseProperty[]
            {
                new GroupProperty(this, "Standard", new BaseProperty[]
                {
                    new TextureProperty(this, k_BaseColorMap, k_BaseColor, "Base Color + Opacity", "Albedo (RGB) and Opacity (A)", false),
                    new TextureProperty(this, k_MetallicMap, k_Metallic, "Metallic", "Metallic", false), 
                    new TextureProperty(this, k_Smoothness1Map, k_Smoothness1, "Smoothness", "Smoothness", false),
                    // TODO: Special case for normal maps.
                    new TextureProperty(this, k_NormalMap, k_NormalScale, "Normal TODO", "Normal Map", false, true),

                    //new TextureProperty(this, k_BaseColorMap, k_BaseColor, "Dielectric IoR", "Index of Refraction for Dielectric", false),
                }),

                new GroupProperty(this, "Emissive", new BaseProperty[]
                {
                    new TextureProperty(this, k_EmissiveColorMap, k_EmissiveColor, "Emissive Color", "Emissive", false),
                    new Property(this, k_EmissiveIntensity, "Emissive Intensity", "Emissive", false),
                    new Property(this, k_AlbedoAffectEmissive, "Albedo Affect Emissive", "Specifies whether or not the emissive color is multiplied by the albedo.", false),
                }),

                //new GroupProperty(this, "Coat", new BaseProperty[]
                //{
                //    new TextureProperty(this, k_BaseColorMap, k_BaseColor, "SmoothnessCoat", "smoothnessCoat", false),
                //    new TextureProperty(this, k_BaseColorMap, k_BaseColor, "Index Of Refraction", "iorCoat", false),
                //    new TextureProperty(this, k_BaseColorMap, k_BaseColor, "Normal", "normal Coat", false),
                //}),

                new GroupProperty(this, "Sub-Surface Scattering", new BaseProperty[]
                {
                    new DiffusionProfileProperty(this, k_DiffusionProfileName, "Diffusion Profile", "A profile determines the shape of the SSS/transmission filter.", false),
                    new TextureProperty(this, k_SubsurfaceMaskName, "Subsurface mask map (R)", "Determines the strength of the subsurface scattering effect.", false),
                }/*, _ => _materialId == MaterialId.SubSurfaceScattering*/),

                new GroupProperty(this, "Second Specular Lobe", new BaseProperty[]
                {
                    new TextureProperty(this, k_Smoothness2Map, k_Smoothness2, "Smoothness2", "Smoothness2", false),
                    new Property(this, k_LobeMix, "Lobe Mix", "Lobe Mix", false), 
                }),

                //new GroupProperty(this, "Anisotropy", new BaseProperty[]
                //{
                //    new TextureProperty(this, k_BaseColorMap, k_BaseColor, "Anisotropy Strength", "anisotropy strength", false),
                //    new TextureProperty(this, k_BaseColorMap, k_BaseColor, "Rotation", "rotation", false),
                //    new TextureProperty(this, k_BaseColorMap, k_BaseColor, "Tangent", "tangent", false),
                //}),

                new GroupProperty(this, "Transmission", new BaseProperty[]
                {
                    new DiffusionProfileProperty(this, k_DiffusionProfileName, "Diffusion Profile", "A profile determines the shape of the SSS/transmission filter.", false),
                    new TextureProperty(this, k_ThicknessName, "Thickness", "If subsurface scattering is enabled, low values allow some light to be transmitted through the object.", false),
                }),

                //new GroupProperty(this, "Iridescence", new BaseProperty[]
                //{
                //    new TextureProperty(this, k_BaseColorMap, k_BaseColor, "Index of Refraction", "Index of Refraction for Iridescence", false),
                //    new TextureProperty(this, k_BaseColorMap, k_BaseColor, "Thickness", "Thickness", false),
                //}),

                //new GroupProperty(this, "Glint", new BaseProperty[]
                //{
                //    new TextureProperty(this, k_BaseColorMap, k_BaseColor, "Density", "Density:", false),
                //    new TextureProperty(this, k_BaseColorMap, k_BaseColor, "Tint", "Tint", false),
                //}),
            });
        }

        protected override bool ShouldEmissionBeEnabled(Material material)
        {
            return material.GetFloat(k_EmissiveIntensity) > 0.0f;
        }

        protected override void FindBaseMaterialProperties(MaterialProperty[] props)
        {
            base.FindBaseMaterialProperties(props);
            _baseMaterialProperties.OnFindProperty(props);
        }

        protected override void FindMaterialProperties(MaterialProperty[] props)
        {
            //base.FindMaterialProperties(props);
            _materialProperties.OnFindProperty(props);
            }

        protected override void BaseMaterialPropertiesGUI()
        {
            base.BaseMaterialPropertiesGUI();
            _baseMaterialProperties.OnGUI();
        }

        protected override void MaterialPropertiesGUI(Material material)
        {
            _materialProperties.OnGUI();
        }

        protected override void MaterialPropertiesAdvanceGUI(Material material)
        {
        }

        protected override void VertexAnimationPropertiesGUI()
        {
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
                BaseLitGUI.DoubleSidedNormalMode doubleSidedNormalMode = (BaseLitGUI.DoubleSidedNormalMode)material.GetFloat(k_DoubleSidedNormalMode);
                switch (doubleSidedNormalMode)
                {
                    case BaseLitGUI.DoubleSidedNormalMode.Mirror: // Mirror mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, -1.0f, 0.0f));
                        break;

                    case BaseLitGUI.DoubleSidedNormalMode.Flip: // Flip mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(-1.0f, -1.0f, -1.0f, 0.0f));
                        break;

                    case BaseLitGUI.DoubleSidedNormalMode.None: // None mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
                        break;
                }
            }

            //NOTE: For SSS in forward and split lighting, obviously we don't have a gbuffer pass, 
            // so no stencil tagging there, but velocity? To check...

            //TODO: stencil state, displacement, wind, depthoffset, tessellation

            SetupMainTexForAlphaTestGI("_BaseColorMap", "_BaseColor", material);

            //TODO: disable DBUFFER

            CoreUtils.SetKeyword(material, "_NORMALMAP_TANGENT_SPACE", true);
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(k_NormalMap));
            CoreUtils.SetKeyword(material, "_SMOOTHNESSMASKMAPA", material.GetTexture(k_Smoothness1Map));
            CoreUtils.SetKeyword(material, "_SMOOTHNESSMASKMAPB", material.GetTexture(k_Smoothness2Map));
            CoreUtils.SetKeyword(material, "_METALLICMAP", material.GetTexture(k_MetallicMap));
            CoreUtils.SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(k_EmissiveColorMap));

            //bool needUV2 = (LitGUI.UVBaseMapping)material.GetFloat(k_UVBase) == LitGUI.UVBaseMapping.UV2;
            //bool needUV3 = (LitGUI.UVBaseMapping)material.GetFloat(k_UVBase) == LitGUI.UVBaseMapping.UV3;

            //if (needUV3)
            //{
            //    material.DisableKeyword("_REQUIRE_UV2");
            //    material.EnableKeyword("_REQUIRE_UV3");
            //}
            //else if (needUV2)
            //{
            //    material.EnableKeyword("_REQUIRE_UV2");
            //    material.DisableKeyword("_REQUIRE_UV3");
            //}
            //else
            //{
            //    material.DisableKeyword("_REQUIRE_UV2");
            //    material.DisableKeyword("_REQUIRE_UV3");
            //}

            CoreUtils.SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(k_EmissiveColorMap));
        }
    }
} // namespace UnityEditor
