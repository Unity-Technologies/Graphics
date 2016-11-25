using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    public abstract class BaseLitGUI : ShaderGUI
    {
        protected static class Styles
        {
            public static string OptionText = "Options";
            public static string SurfaceTypeText = "Surface Type";
            public static string BlendModeText = "Blend Mode";
            public static string detailText = "Inputs Detail";
            public static string lightingText = "Inputs Lighting";

            public static GUIContent alphaCutoffEnableText = new GUIContent("Alpha Cutoff Enable", "Threshold for alpha cutoff");
            public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
            public static GUIContent doubleSidedModeText = new GUIContent("Double Sided", "This will render the two face of the objects (disable backface culling)");

            public static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(SurfaceType));
            public static readonly string[] blendModeNames = Enum.GetNames(typeof(BlendMode));

            public static string InputsOptionsText = "Inputs options";

            public static GUIContent smoothnessMapChannelText = new GUIContent("Smoothness Source", "Smoothness texture and channel");
            public static GUIContent UVBaseMappingText = new GUIContent("UV set for Base", "");
            public static GUIContent texWorldScaleText = new GUIContent("Scale to apply on world coordinate in case of Planar/Triplanar", "");
            public static GUIContent UVBaseDetailMappingText = new GUIContent("UV set for Base and Detail", "");
            public static GUIContent normalMapSpaceText = new GUIContent("Normal/Tangent Map space", "");
            public static GUIContent heightMapModeText = new GUIContent("Height Map Mode", "");
            public static GUIContent detailMapModeText = new GUIContent("Detail Map with Normal", "Detail Map with AO / Height");
            public static GUIContent UVDetailMappingText = new GUIContent("UV set for Detail", "");
            public static GUIContent emissiveColorModeText = new GUIContent("Emissive Color Usage", "Use emissive color or emissive mask");

            public static string InputsText = "Inputs";

            public static string InputsMapText = "";

            public static GUIContent baseColorText = new GUIContent("Base Color + Opacity", "Albedo (RGB) and Opacity (A)");
            public static GUIContent baseColorSmoothnessText = new GUIContent("Base Color + Smoothness", "Albedo (RGB) and Smoothness (A)");

            public static GUIContent metallicText = new GUIContent("Metallic", "Metallic scale factor");
            public static GUIContent smoothnessText = new GUIContent("Smoothness", "Smoothness scale factor");
            public static GUIContent maskMapESText = new GUIContent("Mask Map - M(R), AO(G), E(B), S(A)", "Mask map");
            public static GUIContent maskMapEText = new GUIContent("Mask Map - M(R), AO(G), E(B)", "Mask map");
            public static GUIContent maskMapText = new GUIContent("Mask Map - M(R), AO(G)", "Mask map");
            public static GUIContent maskMapSText = new GUIContent("Mask Map - M(R), AO(G), S(A)", "Mask map");

            public static GUIContent specularOcclusionMapText = new GUIContent("Specular Occlusion Map (RGBA)", "Specular Occlusion Map");

            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map (DXT5) - Need to implement BC5");
 
            public static GUIContent heightMapText = new GUIContent("Height Map (R)", "Height Map");

            public static GUIContent tangentMapText = new GUIContent("Tangent Map", "Tangent Map (BC5) - DXT5 for test");
            public static GUIContent anisotropyText = new GUIContent("Anisotropy", "Anisotropy scale factor");
            public static GUIContent anisotropyMapText = new GUIContent("Anisotropy Map (G)", "Anisotropy");

            public static GUIContent detailMapNormalText = new GUIContent("Detail Map A(R) Ny(G) S(B) Nx(A)", "Detail Map");
            public static GUIContent detailMapAOHeightText = new GUIContent("Detail Map  A(R) AO(G) S(B) H(A)", "Detail Map");
            public static GUIContent detailMaskText = new GUIContent("Detail Mask (B)", "Mask for detailMap");
            public static GUIContent detailAlbedoScaleText = new GUIContent("Detail AlbedoScale", "Detail Albedo Scale factor");
            public static GUIContent detailNormalScaleText = new GUIContent("Detail NormalScale", "Normal Scale factor");
            public static GUIContent detailSmoothnessScaleText = new GUIContent("Detail SmoothnessScale", "Smoothness Scale factor");
            public static GUIContent detailHeightScaleText = new GUIContent("Detail HeightScale", "Height Scale factor");
            public static GUIContent detailAOScaleText = new GUIContent("Detail AOScale", "AO Scale factor");

            public static GUIContent emissiveText = new GUIContent("Emissive Color", "Emissive");
            public static GUIContent emissiveIntensityText = new GUIContent("Emissive Intensity", "Emissive");

            public static GUIContent emissiveWarning = new GUIContent("Emissive value is animated but the material has not been configured to support emissive. Please make sure the material itself has some amount of emissive.");
            public static GUIContent emissiveColorWarning = new GUIContent("Ensure emissive color is non-black for emission to have effect.");
        }

        public enum SurfaceType
        {
            Opaque,
            Transparent
        }
        public enum BlendMode
        {
            Lerp,
            Add,
            SoftAdd,
            Multiply,
            Premultiply
        }
        public enum DoubleSidedMode
        {
            None,
            DoubleSided,
            DoubleSidedLightingFlip,
            DoubleSidedLightingMirror,
        }

        void SurfaceTypePopup()
        {
            EditorGUI.showMixedValue = surfaceType.hasMixedValue;
            var mode = (SurfaceType)surfaceType.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (SurfaceType)EditorGUILayout.Popup(Styles.SurfaceTypeText, (int)mode, Styles.surfaceTypeNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Surface Type");
                surfaceType.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        protected void ShaderOptionsGUI()
        {
            EditorGUI.indentLevel++;
            GUILayout.Label(Styles.OptionText, EditorStyles.boldLabel);
            SurfaceTypePopup();
            if ((SurfaceType)surfaceType.floatValue == SurfaceType.Transparent)
            {
                BlendModePopup();
            }
            m_MaterialEditor.ShaderProperty(alphaCutoffEnable, Styles.alphaCutoffEnableText.text);
            if (alphaCutoffEnable.floatValue == 1.0)
            {
                m_MaterialEditor.ShaderProperty(alphaCutoff, Styles.alphaCutoffText.text);
            }
            m_MaterialEditor.ShaderProperty(doubleSidedMode, Styles.doubleSidedModeText.text);

            EditorGUI.indentLevel--;
        }

        private void BlendModePopup()
        {
            EditorGUI.showMixedValue = blendMode.hasMixedValue;
            var mode = (BlendMode)blendMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (BlendMode)EditorGUILayout.Popup(Styles.BlendModeText, (int)mode, Styles.blendModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Blend Mode");
                blendMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        protected void FindOptionProperties(MaterialProperty[] props)
        {
            surfaceType = FindProperty(kSurfaceType, props);
            blendMode = FindProperty(kBlendMode, props);
            alphaCutoff = FindProperty(kAlphaCutoff, props);
            alphaCutoffEnable = FindProperty(kAlphaCutoffEnabled, props);
            doubleSidedMode = FindProperty(kDoubleSidedMode, props);
            FindInputOptionProperties(props);
        }

        protected void SetupMaterial(Material material)
        {
            bool alphaTestEnable = material.GetFloat(kAlphaCutoffEnabled) == 1.0;
            SurfaceType surfaceType = (SurfaceType)material.GetFloat(kSurfaceType);
            BlendMode blendMode = (BlendMode)material.GetFloat(kBlendMode);
            DoubleSidedMode doubleSidedMode = (DoubleSidedMode)material.GetFloat(kDoubleSidedMode);

            if (surfaceType == SurfaceType.Opaque)
            {
                material.SetOverrideTag("RenderType", alphaTestEnable ? "TransparentCutout" : "");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.renderQueue = alphaTestEnable ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : -1;
            }
            else
            {
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_ZWrite", 0);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                switch (blendMode)
                {
                    case BlendMode.Lerp:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        break;

                    case BlendMode.Add:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        break;

                    case BlendMode.SoftAdd:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        break;

                    case BlendMode.Multiply:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        break;

                    case BlendMode.Premultiply:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        break;
                }
            }

            if (doubleSidedMode == DoubleSidedMode.None)
            {
                material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Back);
            }
            else
            {
                material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
            }

            if (doubleSidedMode == DoubleSidedMode.DoubleSidedLightingFlip)
            {
                material.EnableKeyword("_DOUBLESIDED_LIGHTING_FLIP");
                material.DisableKeyword("_DOUBLESIDED_LIGHTING_MIRROR");
            }
            else if (doubleSidedMode == DoubleSidedMode.DoubleSidedLightingMirror)
            {
                material.DisableKeyword("_DOUBLESIDED_LIGHTING_FLIP");
                material.EnableKeyword("_DOUBLESIDED_LIGHTING_MIRROR");
            }
            else
            {
                material.DisableKeyword("_DOUBLESIDED_LIGHTING_FLIP");
                material.DisableKeyword("_DOUBLESIDED_LIGHTING_MIRROR");
            }

            SetKeyword(material, "_ALPHATEST_ON", alphaTestEnable);
            SetupInputMaterial(material);
        }

        protected void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }

        public void ShaderPropertiesGUI(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                ShaderOptionsGUI();
                EditorGUILayout.Space();

                ShaderInputOptionsGUI();

                EditorGUILayout.Space();
                ShaderInputGUI();
            }

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in m_MaterialEditor.targets)
                    SetupMaterial((Material)obj);
            }
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            FindOptionProperties(props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
            FindInputProperties(props);

            m_MaterialEditor = materialEditor;
            Material material = materialEditor.target as Material;
            ShaderPropertiesGUI(material);
        }

        protected MaterialEditor m_MaterialEditor;

        MaterialProperty surfaceType = null;
        MaterialProperty alphaCutoffEnable = null;
        MaterialProperty blendMode = null;
        MaterialProperty alphaCutoff = null;
        MaterialProperty doubleSidedMode = null;
 
        const string kSurfaceType = "_SurfaceType";
        const string kBlendMode = "_BlendMode";
        const string kAlphaCutoff = "_AlphaCutoff";
        const string kAlphaCutoffEnabled = "_AlphaCutoffEnable";
        const string kDoubleSidedMode = "_DoubleSidedMode";
        protected static string[] reservedProperties = new string[] { kSurfaceType, kBlendMode, kAlphaCutoff, kAlphaCutoffEnabled, kDoubleSidedMode };

        protected abstract void FindInputProperties(MaterialProperty[] props);
        protected abstract void ShaderInputGUI();
        protected abstract void ShaderInputOptionsGUI();
        protected abstract void FindInputOptionProperties(MaterialProperty[] props);
        protected abstract void SetupInputMaterial(Material material);
    }

    class LitGUI : BaseLitGUI
    {
        public enum SmoothnessMapChannel
        {
            MaskAlpha,
            AlbedoAlpha,
        }

        public enum UVBaseMapping
        {
            UV0,
            Planar, 
            Triplanar
        }

        public enum NormalMapSpace
        {
            TangentSpace,
            ObjectSpace,
        }

        public enum HeightmapMode
        {
            Parallax,
            Displacement,
        }

        public enum DetailMapMode
        {
            DetailWithNormal,
            DetailWithAOHeight,
        }

        public enum UVDetailMapping
        {
            UV0,
            UV1,
            UV3
        }

        public enum EmissiveColorMode
        {
            UseEmissiveColor,
            UseEmissiveMask,
        }

        protected MaterialProperty smoothnessMapChannel = null;
        protected const string kSmoothnessTextureChannel = "_SmoothnessTextureChannel";
        protected MaterialProperty UVBase = null;
        protected const string kUVBase = "_UVBase";
        protected MaterialProperty TexWorldScale = null;
        protected const string kTexWorldScale = "_TexWorldScale";
        protected MaterialProperty UVMappingMask = null;
        protected const string kUVMappingMask = "_UVMappingMask";
        protected MaterialProperty normalMapSpace = null;
        protected const string kNormalMapSpace = "_NormalMapSpace";
        protected MaterialProperty heightMapMode = null;
        protected const string kHeightMapMode = "_HeightMapMode";
        protected MaterialProperty detailMapMode = null;
        protected const string kDetailMapMode = "_DetailMapMode";
        protected MaterialProperty UVDetail = null;
        protected const string kUVDetail = "_UVDetail";
        protected MaterialProperty UVDetailsMappingMask = null;
        protected const string kUVDetailsMappingMask = "_UVDetailsMappingMask";
        protected MaterialProperty emissiveColorMode = null;
        protected const string kEmissiveColorMode = "_EmissiveColorMode";

        protected MaterialProperty baseColor = null;
        protected const string kBaseColor = "_BaseColor";
        protected MaterialProperty baseColorMap = null;
        protected const string kBaseColorMap = "_BaseColorMap";
        protected MaterialProperty metallic = null;
        protected const string kMettalic = "_Mettalic";
        protected MaterialProperty smoothness = null;
        protected const string kSmoothness = "_Smoothness";
        protected MaterialProperty maskMap = null;
        protected const string kMaskMap = "_MaskMap";
        protected MaterialProperty specularOcclusionMap = null;
        protected const string kSpecularOcclusionMap = "_SpecularOcclusionMap";
        protected MaterialProperty normalMap = null;
        protected const string kNormalMap = "_NormalMap";
        protected MaterialProperty heightMap = null;
        protected const string kHeightMap = "_HeightMap";
        protected MaterialProperty heightScale = null;
        protected const string kHeightScale = "_HeightScale";
        protected MaterialProperty heightBias = null;
        protected const string kHeightBias= "_HeightBias";
        protected MaterialProperty tangentMap = null;
        protected const string kTangentMap = "_TangentMap";
        protected MaterialProperty anisotropy = null;
        protected const string kAnisotropy = "_Anisotropy";
        protected MaterialProperty anisotropyMap = null;
        protected const string kAnisotropyMap = "_AnisotropyMap";

        protected MaterialProperty detailMap = null;
        protected const string kDetailMap = "_DetailMap";
        protected MaterialProperty detailMask = null;
        protected const string kDetailMask = "_DetailMask";
        protected MaterialProperty detailAlbedoScale = null;
        protected const string kDetailAlbedoScale = "_DetailAlbedoScale";
        protected MaterialProperty detailNormalScale = null;
        protected const string kDetailNormalScale = "_DetailNormalScale";
        protected MaterialProperty detailSmoothnessScale = null;
        protected const string kDetailSmoothnessScale = "_DetailSmoothnessScale";
        protected MaterialProperty detailHeightScale = null;
        protected const string kDetailHeightScale = "_DetailHeightScale";
        protected MaterialProperty detailAOScale = null;
        protected const string kDetailAOScale = "_DetailAOScale";

        //	MaterialProperty subSurfaceRadius = null;
        //	MaterialProperty subSurfaceRadiusMap = null;

        protected MaterialProperty emissiveColor = null;
        protected const string kEmissiveColor = "_EmissiveColor";
        protected MaterialProperty emissiveColorMap = null;
        protected const string kEmissiveColorMap = "_EmissiveColorMap";
        protected MaterialProperty emissiveIntensity = null;
        protected const string kEmissiveIntensity = "_EmissiveIntensity";

        override protected void FindInputOptionProperties(MaterialProperty[] props)
        {   
            smoothnessMapChannel = FindProperty(kSmoothnessTextureChannel, props);
            UVBase = FindProperty(kUVBase, props);
            TexWorldScale = FindProperty(kTexWorldScale, props);
            UVMappingMask = FindProperty(kUVMappingMask, props);
            normalMapSpace = FindProperty(kNormalMapSpace, props);
            heightMapMode = FindProperty(kHeightMapMode, props);
            detailMapMode = FindProperty(kDetailMapMode, props);
            UVDetail = FindProperty(kUVDetail, props);
            UVDetailsMappingMask = FindProperty(kUVDetailsMappingMask, props);    
            emissiveColorMode = FindProperty(kEmissiveColorMode, props);
        }

        override protected void FindInputProperties(MaterialProperty[] props)
        {
            baseColor = FindProperty(kBaseColor, props);
            baseColorMap = FindProperty(kBaseColorMap, props);
            metallic = FindProperty(kMetallic, props);
            smoothness = FindProperty(kSmoothness, props);
            maskMap = FindProperty(kMaskMap, props);
            specularOcclusionMap = FindProperty(kSpecularOcclusionMap, props);
            normalMap = FindProperty(kNormalMap, props);
            heightMap = FindProperty(kHeightMap, props);
            heightScale = FindProperty(kHeightScale, props);
            heightBias = FindProperty(kHeightBias, props);
            tangentMap = FindProperty(kTangentMap, props);
            anisotropy = FindProperty(kAnisotropy, props);
            anisotropyMap = FindProperty(kAnisotropyMap, props);

            detailMap = FindProperty(kDetailMap, props);
            detailMask = FindProperty(kDetailMask, props);
            detailAlbedoScale = FindProperty(kDetailAlbedoScale, props);
            detailNormalScale = FindProperty(kDetailNormalScale, props);
            detailSmoothnessScale = FindProperty(kDetailSmoothnessScale, props);
            detailHeightScale = FindProperty(kDetailHeightScale, props);
            detailAOScale = FindProperty(kDetailAOScale, props);

            emissiveColor = FindProperty(kEmissiveColor, props);
            emissiveColorMap = FindProperty(kEmissiveColorMap, props);
            emissiveIntensity = FindProperty(kEmissiveIntensity, props);
        }

        override protected void ShaderInputOptionsGUI()
        {
            // When Planar or Triplanar is enable the UVDetail use the same mode, so we disable the choice on UVDetail
            bool enableUVDetail = (UVBaseMapping)UVBase.floatValue == UVBaseMapping.UV0;

            EditorGUI.indentLevel++;
            GUILayout.Label(Styles.InputsOptionsText, EditorStyles.boldLabel);
            m_MaterialEditor.ShaderProperty(smoothnessMapChannel, Styles.smoothnessMapChannelText.text);
            m_MaterialEditor.ShaderProperty(UVBase, enableUVDetail ? Styles.UVBaseDetailMappingText.text : Styles.UVBaseMappingText.text);

            float X, Y, Z, W;
            X = ((UVBaseMapping)UVBase.floatValue == UVBaseMapping.UV0) ? 1.0f : 0.0f;
            W = ((UVBaseMapping)UVBase.floatValue == UVBaseMapping.Planar) ? 1.0f : 0.0f;
            UVMappingMask.colorValue = new Color(X, 0.0f, 0.0f, W);
            if (((UVBaseMapping)UVBase.floatValue == UVBaseMapping.Planar) || ((UVBaseMapping)UVBase.floatValue == UVBaseMapping.Triplanar))
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(TexWorldScale, Styles.texWorldScaleText.text);
                EditorGUI.indentLevel--;
            }
            if (enableUVDetail)
            {
                m_MaterialEditor.ShaderProperty(UVDetail, Styles.UVDetailMappingText.text);
            }
            X = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV0) ? 1.0f : 0.0f;
            Y = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV1) ? 1.0f : 0.0f;
            Z = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV3) ? 1.0f : 0.0f;
            UVDetailsMappingMask.colorValue = new Color(X, Y, Z, 0.0f); // W Reuse planar mode from base

            m_MaterialEditor.ShaderProperty(detailMapMode, Styles.detailMapModeText.text);
            m_MaterialEditor.ShaderProperty(normalMapSpace, Styles.normalMapSpaceText.text);
            m_MaterialEditor.ShaderProperty(heightMapMode, Styles.heightMapModeText.text);
            m_MaterialEditor.ShaderProperty(emissiveColorMode, Styles.emissiveColorModeText.text);
            EditorGUI.indentLevel--;
        }

        override protected void ShaderInputGUI()
        {
            EditorGUI.indentLevel++;
            bool smoothnessInAlbedoAlpha = (SmoothnessMapChannel)smoothnessMapChannel.floatValue == SmoothnessMapChannel.AlbedoAlpha;            
            bool useDetailMapWithNormal = (DetailMapMode)detailMapMode.floatValue == DetailMapMode.DetailWithNormal;
            bool useEmissiveMask = (EmissiveColorMode)emissiveColorMode.floatValue == EmissiveColorMode.UseEmissiveMask;

            GUILayout.Label(Styles.InputsText, EditorStyles.boldLabel);
            m_MaterialEditor.TexturePropertySingleLine(smoothnessInAlbedoAlpha ? Styles.baseColorSmoothnessText : Styles.baseColorText, baseColorMap, baseColor);
            m_MaterialEditor.ShaderProperty(metallic, Styles.metallicText);
            m_MaterialEditor.ShaderProperty(smoothness, Styles.smoothnessText);

            if (smoothnessInAlbedoAlpha && useEmissiveMask)
                m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapEText, maskMap);
            else if (smoothnessInAlbedoAlpha && !useEmissiveMask)
                m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapText, maskMap);
            else if (!smoothnessInAlbedoAlpha && useEmissiveMask)
                m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapESText, maskMap);
            else if (!smoothnessInAlbedoAlpha && !useEmissiveMask)
                m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapSText, maskMap);

            m_MaterialEditor.TexturePropertySingleLine(Styles.specularOcclusionMapText, specularOcclusionMap);

            m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap);

            m_MaterialEditor.TexturePropertySingleLine(Styles.heightMapText, heightMap, heightScale, heightBias);

            m_MaterialEditor.TexturePropertySingleLine(Styles.tangentMapText, tangentMap);

            m_MaterialEditor.ShaderProperty(anisotropy, Styles.anisotropyText);
            
            m_MaterialEditor.TexturePropertySingleLine(Styles.anisotropyMapText, anisotropyMap);

            EditorGUILayout.Space();
            GUILayout.Label(Styles.detailText, EditorStyles.boldLabel);

            m_MaterialEditor.TexturePropertySingleLine(Styles.detailMaskText, detailMask);               

            if (useDetailMapWithNormal)
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.detailMapNormalText, detailMap);
            }
            else
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.detailMapAOHeightText, detailMap);
            }
            m_MaterialEditor.TextureScaleOffsetProperty(detailMap);
            EditorGUI.indentLevel++;
            m_MaterialEditor.ShaderProperty(detailAlbedoScale, Styles.detailAlbedoScaleText);
            m_MaterialEditor.ShaderProperty(detailNormalScale, Styles.detailNormalScaleText);
            m_MaterialEditor.ShaderProperty(detailSmoothnessScale, Styles.detailSmoothnessScaleText);
            m_MaterialEditor.ShaderProperty(detailHeightScale, Styles.detailHeightScaleText);
            m_MaterialEditor.ShaderProperty(detailAOScale, Styles.detailAOScaleText);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            GUILayout.Label(Styles.lightingText, EditorStyles.boldLabel);

            if (!useEmissiveMask)
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.emissiveText, emissiveColorMap, emissiveColor);
            }
            m_MaterialEditor.ShaderProperty(emissiveIntensity, Styles.emissiveIntensityText);
            m_MaterialEditor.LightmapEmissionProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);

            EditorGUI.indentLevel--;
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
        }

        protected virtual void SetupKeywordsForInputMaps(Material material)
        {
            SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap) || material.GetTexture(kDetailMap)); // With details map, we always use a normal map and Unity provide a default (0, 0, 1) normal map for ir
            SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));
            SetKeyword(material, "_SPECULAROCCLUSIONMAP", material.GetTexture(kSpecularOcclusionMap));
            SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));
            SetKeyword(material, "_HEIGHTMAP", material.GetTexture(kHeightMap));
            SetKeyword(material, "_TANGENTMAP", material.GetTexture(kTangentMap));
            SetKeyword(material, "_ANISOTROPYMAP", material.GetTexture(kAnisotropyMap));
            SetKeyword(material, "_DETAIL_MAP", material.GetTexture(kDetailMap));
        }

        public static bool ShouldEmissionBeEnabled(Material mat)
        {
            float emissiveIntensity = mat.GetFloat(kEmissiveIntensity);
            var realtimeEmission = (mat.globalIlluminationFlags & MaterialGlobalIlluminationFlags.RealtimeEmissive) > 0;
            return emissiveIntensity > 0.0f || realtimeEmission;
        }

        protected virtual void SetupEmissionGIFlags(Material material)
        {
            // Setup lightmap emissive flags
            MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
            if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0)
            {
                if (ShouldEmissionBeEnabled(material))
                    flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                else
                    flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                material.globalIlluminationFlags = flags;
            }
        }

        override protected void SetupInputMaterial(Material material)
        {
            // Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
            // (MaterialProperty value might come from renderer material property block)
            SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", ((SmoothnessMapChannel)material.GetFloat(kSmoothnessTextureChannel)) == SmoothnessMapChannel.AlbedoAlpha);
            SetKeyword(material, "_MAPPING_TRIPLANAR", ((UVBaseMapping)material.GetFloat(kUVBase)) == UVBaseMapping.Triplanar);
            SetKeyword(material, "_NORMALMAP_TANGENT_SPACE", ((NormalMapSpace)material.GetFloat(kNormalMapSpace)) == NormalMapSpace.TangentSpace);
            SetKeyword(material, "_HEIGHTMAP_AS_DISPLACEMENT", ((HeightmapMode)material.GetFloat(kHeightMapMode)) == HeightmapMode.Displacement);
            SetKeyword(material, "_DETAIL_MAP_WITH_NORMAL", ((DetailMapMode)material.GetFloat(kDetailMapMode)) == DetailMapMode.DetailWithNormal);
            SetKeyword(material, "_REQUIRE_UV3", ((UVDetailMapping)material.GetFloat(kUVDetail)) == UVDetailMapping.UV3 && (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV0);
            SetKeyword(material, "_EMISSIVE_COLOR", ((EmissiveColorMode)material.GetFloat(kEmissiveColorMode)) == EmissiveColorMode.UseEmissiveColor);
 
            SetupKeywordsForInputMaps(material);
            SetupEmissionGIFlags(material);
        }

        // TODO: ? or remove
        bool HasValidEmissiveKeyword(Material material)
        {
            /*
            // Material animation might be out of sync with the material keyword.
            // So if the emission support is disabled on the material, but the property blocks have a value that requires it, then we need to show a warning.
            // (note: (Renderer MaterialPropertyBlock applies its values to emissionColorForRendering))
            bool hasEmissionKeyword = material.IsKeywordEnabled ("_EMISSION");
            if (!hasEmissionKeyword && ShouldEmissionBeEnabled (material, emissionColorForRendering.colorValue))
                return false;
            else
                return true;
            */

            return true;
        }
    }
} // namespace UnityEditor
