using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    public abstract class BaseLitGUI : ShaderGUI
    {
        protected static class Styles
        {
            public enum DetailMapMode
            {
                DetailWithNormal,
                DetailWithAOHeight,
            }
            public static string OptionText = "Options";
            public static string SurfaceTypeText = "Surface Type";
            public static string BlendModeText = "Blend Mode";
            public static GUIContent uvSetLabel = new GUIContent("UV Set");
            public static string detailText = "Inputs Detail";
            public static string lightingText = "Inputs Lighting";

            public static GUIContent alphaCutoffEnableText = new GUIContent("Alpha Cutoff Enable", "Threshold for alpha cutoff");
            public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
            public static GUIContent doubleSidedModeText = new GUIContent("Double Sided", "This will render the two face of the objects (disable backface culling)");

            public static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(SurfaceType));
            public static readonly string[] blendModeNames = Enum.GetNames(typeof(BlendMode));

            public static string InputsOptionsText = "Inputs options";

            public static GUIContent smoothnessMapChannelText = new GUIContent("Smoothness Source", "Smoothness texture and channel");
            public static GUIContent emissiveColorModeText = new GUIContent("Emissive Color Usage", "Use emissive color or emissive mask");
            public static GUIContent detailMapModeText = new GUIContent("Detail Map with Normal", "Detail Map with AO / Height");

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

            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map (BC5) - DXT5 for test");
            public static GUIContent normalMapSpaceText = new GUIContent("Normal/Tangent Map space", "");

            public static GUIContent heightMapText = new GUIContent("Height Map (R)", "Height Map");
            public static GUIContent heightMapModeText = new GUIContent("Height Map Mode", "");

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

        private MaterialProperty surfaceType = null;
        private MaterialProperty alphaCutoffEnable = null;
        private MaterialProperty blendMode = null;
        private MaterialProperty alphaCutoff = null;
        private MaterialProperty doubleSidedMode = null;
 
        private const string kSurfaceType = "_SurfaceType";
        private const string kBlendMode = "_BlendMode";
        private const string kAlphaCutoff = "_AlphaCutoff";
        private const string kAlphaCutoffEnabled = "_AlphaCutoffEnable";
        private const string kDoubleSidedMode = "_DoubleSidedMode";
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
        public enum EmissiveColorMode
        {
            UseEmissiveColor,
            UseEmissiveMask,
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

        MaterialProperty UVDetail = null;
        MaterialProperty smoothnessMapChannel = null;
        MaterialProperty emissiveColorMode = null;
        MaterialProperty detailMapMode = null;

        MaterialProperty baseColor = null;
        MaterialProperty baseColorMap = null;
        MaterialProperty metallic = null;
        MaterialProperty smoothness = null;
        MaterialProperty maskMap = null;
        MaterialProperty specularOcclusionMap = null;
        MaterialProperty normalMap = null;
        MaterialProperty normalMapSpace = null;
        MaterialProperty heightMap = null;
        MaterialProperty heightScale = null;
        MaterialProperty heightBias = null;
        MaterialProperty tangentMap = null;
        MaterialProperty anisotropy = null;
        MaterialProperty anisotropyMap = null;
        MaterialProperty heightMapMode = null;
        MaterialProperty detailMap = null;
        MaterialProperty detailMask = null;
        MaterialProperty detailAlbedoScale = null;
        MaterialProperty detailNormalScale = null;
        MaterialProperty detailSmoothnessScale = null;
        MaterialProperty detailHeightScale = null;
        MaterialProperty detailAOScale = null;
        MaterialProperty emissiveColor = null;
        MaterialProperty emissiveColorMap = null;
        MaterialProperty emissiveIntensity = null;
//	MaterialProperty subSurfaceRadius = null;
//	MaterialProperty subSurfaceRadiusMap = null;

        protected const string kUVDetail = "_UVDetail";
        protected const string kSmoothnessTextureChannelProp = "_SmoothnessTextureChannel";
        protected const string kEmissiveColorMode = "_EmissiveColorMode";
        protected const string kNormalMapSpace = "_NormalMapSpace";
        protected const string kHeightMapMode = "_HeightMapMode";
        protected const string kDetailMapMode = "_DetailMapMode";

        protected const string kNormalMap = "_NormalMap";
        protected const string kMaskMap = "_MaskMap";
        protected const string kspecularOcclusionMap = "_SpecularOcclusionMap";
        protected const string kEmissiveColorMap = "_EmissiveColorMap";
        protected const string kHeightMap = "_HeightMap";
        protected const string kTangentMap = "_TangentMap";
        protected const string kAnisotropyMap = "_AnisotropyMap";
        protected const string kDetailMap = "_DetailMap";
        protected const string kDetailMask = "_DetailMask";
        protected const string kDetailAlbedoScale = "_DetailAlbedoScale";
        protected const string kDetailNormalScale = "_DetailNormalScale";
        protected const string kDetailSmoothnessScale = "_DetailSmoothnessScale";
        protected const string kDetailHeightScale = "_DetailHeightScale";
        protected const string kDetailAOScale = "_DetailAOScale";

        override protected void FindInputOptionProperties(MaterialProperty[] props)
        {
            UVDetail = FindProperty(kUVDetail, props);
            smoothnessMapChannel = FindProperty(kSmoothnessTextureChannelProp, props);
            emissiveColorMode = FindProperty(kEmissiveColorMode, props);
            normalMapSpace = FindProperty(kNormalMapSpace, props);
            heightMapMode = FindProperty(kHeightMapMode, props);
            detailMapMode = FindProperty(kDetailMapMode, props);
        }

        override protected void FindInputProperties(MaterialProperty[] props)
        {
            baseColor = FindProperty("_BaseColor", props);
            baseColorMap = FindProperty("_BaseColorMap", props);
            metallic = FindProperty("_Metallic", props);
            smoothness = FindProperty("_Smoothness", props);
            maskMap = FindProperty(kMaskMap, props);
            specularOcclusionMap = FindProperty(kspecularOcclusionMap, props);
            normalMap = FindProperty(kNormalMap, props);
            heightMap = FindProperty(kHeightMap, props);
            heightScale = FindProperty("_HeightScale", props);
            heightBias = FindProperty("_HeightBias", props);
            tangentMap = FindProperty("_TangentMap", props);
            anisotropy = FindProperty("_Anisotropy", props);
            anisotropyMap = FindProperty("_AnisotropyMap", props);
            emissiveColor = FindProperty("_EmissiveColor", props);
            emissiveColorMap = FindProperty(kEmissiveColorMap, props);
            emissiveIntensity = FindProperty("_EmissiveIntensity", props);
            detailMap = FindProperty(kDetailMap, props);
            detailMask = FindProperty(kDetailMask, props);
            detailAlbedoScale = FindProperty(kDetailAlbedoScale, props);
            detailNormalScale = FindProperty(kDetailNormalScale, props);
            detailSmoothnessScale = FindProperty(kDetailSmoothnessScale, props);
            detailHeightScale = FindProperty(kDetailHeightScale, props);
            detailAOScale = FindProperty(kDetailAOScale, props);
        }

        override protected void ShaderInputOptionsGUI()
        {
            EditorGUI.indentLevel++;
            GUILayout.Label(Styles.InputsOptionsText, EditorStyles.boldLabel);
            m_MaterialEditor.ShaderProperty(smoothnessMapChannel, Styles.smoothnessMapChannelText.text);
            m_MaterialEditor.ShaderProperty(emissiveColorMode, Styles.emissiveColorModeText.text);
            m_MaterialEditor.ShaderProperty(normalMapSpace, Styles.normalMapSpaceText.text);
            m_MaterialEditor.ShaderProperty(heightMapMode, Styles.heightMapModeText.text);
            m_MaterialEditor.ShaderProperty(detailMapMode, Styles.detailMapModeText.text);
            EditorGUI.indentLevel--;
        }

        override protected void ShaderInputGUI()
        {
            EditorGUI.indentLevel++;
            bool smoothnessInAlbedoAlpha = (SmoothnessMapChannel)smoothnessMapChannel.floatValue == SmoothnessMapChannel.AlbedoAlpha;
            bool useEmissiveMask = (EmissiveColorMode)emissiveColorMode.floatValue == EmissiveColorMode.UseEmissiveMask;
            bool useDetailMapWithNormal = (DetailMapMode)detailMapMode.floatValue == DetailMapMode.DetailWithNormal;

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
            m_MaterialEditor.ShaderProperty(UVDetail, Styles.uvSetLabel.text);          

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

        // TODO: try to setup minimun value to fall back to standard shaders and reverse
        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
        }

        protected virtual void SetupKeywordsForInputMaps(Material material)
        {
            SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap) || material.GetTexture(kDetailMap)); // With details map, we always use a normal map and Unity provide a default (0, 0, 1) normal map for ir
            SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));
            SetKeyword(material, "_SPECULAROCCLUSIONMAP", material.GetTexture(kspecularOcclusionMap));
            SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));
            SetKeyword(material, "_HEIGHTMAP", material.GetTexture(kHeightMap));
            SetKeyword(material, "_TANGENTMAP", material.GetTexture(kTangentMap));
            SetKeyword(material, "_ANISOTROPYMAP", material.GetTexture(kAnisotropyMap));
            SetKeyword(material, "_DETAIL_MAP", material.GetTexture(kDetailMap));
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
            SetKeyword(material, "_NORMALMAP_TANGENT_SPACE", (NormalMapSpace)material.GetFloat(kNormalMapSpace) == NormalMapSpace.TangentSpace);
            SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", ((SmoothnessMapChannel)material.GetFloat(kSmoothnessTextureChannelProp)) == SmoothnessMapChannel.AlbedoAlpha);
            SetKeyword(material, "_EMISSIVE_COLOR", ((EmissiveColorMode)material.GetFloat(kEmissiveColorMode)) == EmissiveColorMode.UseEmissiveColor);
            SetKeyword(material, "_HEIGHTMAP_AS_DISPLACEMENT", (HeightmapMode)material.GetFloat(kHeightMapMode) == HeightmapMode.Displacement);
            SetKeyword(material, "_DETAIL_MAP_WITH_NORMAL", (DetailMapMode)material.GetFloat(kDetailMapMode) == DetailMapMode.DetailWithNormal);

            SetupKeywordsForInputMaps(material);
            SetupEmissionGIFlags(material);
        }

        public static bool ShouldEmissionBeEnabled(Material mat)
        {
            float emissiveIntensity = mat.GetFloat("_EmissiveIntensity");
            var realtimeEmission = (mat.globalIlluminationFlags & MaterialGlobalIlluminationFlags.RealtimeEmissive) > 0;
            return emissiveIntensity > 0.0f || realtimeEmission;
        }

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
