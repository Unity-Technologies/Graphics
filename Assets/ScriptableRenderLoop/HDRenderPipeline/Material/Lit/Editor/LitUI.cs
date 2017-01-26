using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
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
            UV2,
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
        protected MaterialProperty UVMappingPlanar = null;
        protected const string kUVMappingPlanar = "_UVMappingPlanar";      
        protected MaterialProperty normalMapSpace = null;
        protected const string kNormalMapSpace = "_NormalMapSpace";
        protected MaterialProperty enablePerPixelDisplacement = null;
        protected const string kEnablePerPixelDisplacement = "_EnablePerPixelDisplacement";
        protected MaterialProperty ppdMinSamples = null;
        protected const string kPpdMinSamples = "_PPDMinSamples";
        protected MaterialProperty ppdMaxSamples = null;
        protected const string kPpdMaxSamples = "_PPDMaxSamples";
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
        protected const string kMetallic = "_Metallic";
        protected MaterialProperty smoothness = null;
        protected const string kSmoothness = "_Smoothness";
        protected MaterialProperty maskMap = null;
        protected const string kMaskMap = "_MaskMap";
        protected MaterialProperty specularOcclusionMap = null;
        protected const string kSpecularOcclusionMap = "_SpecularOcclusionMap";
        protected MaterialProperty normalMap = null;
        protected const string kNormalMap = "_NormalMap";
        protected MaterialProperty normalScale = null;
        protected const string kNormalScale = "_NormalScale";
        protected MaterialProperty heightMap = null;
        protected const string kHeightMap = "_HeightMap";
        protected MaterialProperty heightAmplitude = null;
        protected const string kHeightAmplitude = "_HeightAmplitude";
        protected MaterialProperty heightCenter = null;
        protected const string kHeightCenter = "_HeightCenter";
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


        // These are options that are shared with the LayeredLit shader. Don't put anything that can't be shared here:
        // For instance, properties like BaseColor and such don't exist in the LayeredLit so don't put them here.
        protected void FindMaterialOptionProperties(MaterialProperty[] props)
        {   
            smoothnessMapChannel = FindProperty(kSmoothnessTextureChannel, props);
            normalMapSpace = FindProperty(kNormalMapSpace, props);
            enablePerPixelDisplacement = FindProperty(kEnablePerPixelDisplacement, props);
            ppdMinSamples = FindProperty(kPpdMinSamples, props);
            ppdMaxSamples = FindProperty(kPpdMaxSamples, props);
            detailMapMode = FindProperty(kDetailMapMode, props);
            emissiveColorMode = FindProperty(kEmissiveColorMode, props);
        }

        override protected void FindMaterialProperties(MaterialProperty[] props)
        {
            FindMaterialOptionProperties(props);

            baseColor = FindProperty(kBaseColor, props);
            baseColorMap = FindProperty(kBaseColorMap, props);
            metallic = FindProperty(kMetallic, props);
            smoothness = FindProperty(kSmoothness, props);
            maskMap = FindProperty(kMaskMap, props);
            specularOcclusionMap = FindProperty(kSpecularOcclusionMap, props);
            normalMap = FindProperty(kNormalMap, props);
            normalScale = FindProperty(kNormalScale, props);           
            heightMap = FindProperty(kHeightMap, props);
            heightAmplitude = FindProperty(kHeightAmplitude, props);
            heightCenter = FindProperty(kHeightCenter, props);
            tangentMap = FindProperty(kTangentMap, props);
            anisotropy = FindProperty(kAnisotropy, props);
            anisotropyMap = FindProperty(kAnisotropyMap, props);

            UVBase = FindProperty(kUVBase, props);
            UVDetail = FindProperty(kUVDetail, props);
            TexWorldScale = FindProperty(kTexWorldScale, props);
            UVMappingMask = FindProperty(kUVMappingMask, props);
            UVMappingPlanar = FindProperty(kUVMappingPlanar, props);
            UVDetailsMappingMask = FindProperty(kUVDetailsMappingMask, props);    
            
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
            UVMappingMask.colorValue = new Color(X, 0.0f, 0.0f, 0.0f);
            UVMappingPlanar.floatValue = ((UVBaseMapping)UVBase.floatValue == UVBaseMapping.Planar) ? 1.0f : 0.0f;
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
            Z = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV2) ? 1.0f : 0.0f;
            W = ((UVDetailMapping)UVDetail.floatValue == UVDetailMapping.UV3) ? 1.0f : 0.0f;
            UVDetailsMappingMask.colorValue = new Color(X, Y, Z, W);

            //m_MaterialEditor.ShaderProperty(detailMapMode, Styles.detailMapModeText.text);
            m_MaterialEditor.ShaderProperty(normalMapSpace, Styles.normalMapSpaceText.text);            
            m_MaterialEditor.ShaderProperty(emissiveColorMode, Styles.emissiveColorModeText.text);
            m_MaterialEditor.ShaderProperty(enablePerPixelDisplacement, Styles.enablePerPixelDisplacementText.text);
            m_MaterialEditor.ShaderProperty(ppdMinSamples, Styles.ppdMinSamplesText.text);
            m_MaterialEditor.ShaderProperty(ppdMaxSamples, Styles.ppdMaxSamplesText.text);
            ppdMinSamples.floatValue = Mathf.Min(ppdMinSamples.floatValue, ppdMaxSamples.floatValue);
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

            m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap, normalScale);

            m_MaterialEditor.TexturePropertySingleLine(Styles.heightMapText, heightMap);
            if(!heightMap.hasMixedValue && heightMap.textureValue != null)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(heightAmplitude, Styles.heightMapAmplitudeText);
                heightAmplitude.floatValue = Math.Max(0.0f, heightAmplitude.floatValue); // Must be positive
                m_MaterialEditor.ShaderProperty(heightCenter, Styles.heightMapCenterText);
                EditorGUI.showMixedValue = false;
                EditorGUI.indentLevel--;
            }

            m_MaterialEditor.TexturePropertySingleLine(Styles.tangentMapText, tangentMap);

            m_MaterialEditor.ShaderProperty(anisotropy, Styles.anisotropyText);
            
            m_MaterialEditor.TexturePropertySingleLine(Styles.anisotropyMapText, anisotropyMap);

            EditorGUILayout.Space();
            GUILayout.Label(Styles.textureControlText, EditorStyles.label);
            m_MaterialEditor.TextureScaleOffsetProperty(baseColorMap);

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
            //m_MaterialEditor.ShaderProperty(detailHeightScale, Styles.detailHeightScaleText);
            //m_MaterialEditor.ShaderProperty(detailAOScale, Styles.detailAOScaleText);
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

            EditorGUILayout.Space();
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
        }

        protected override bool ShouldEmissionBeEnabled(Material mat)
        {
            float emissiveIntensity = mat.GetFloat(kEmissiveIntensity);
            var realtimeEmission = (mat.globalIlluminationFlags & MaterialGlobalIlluminationFlags.RealtimeEmissive) > 0;
            return emissiveIntensity > 0.0f || realtimeEmission;
        }

        override protected void SetupMaterialKeywords(Material material)
        {
			SetupCommonOptionsKeywords(material);

            // Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
            // (MaterialProperty value might come from renderer material property block)
            SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", ((SmoothnessMapChannel)material.GetFloat(kSmoothnessTextureChannel)) == SmoothnessMapChannel.AlbedoAlpha);
            SetKeyword(material, "_MAPPING_TRIPLANAR", ((UVBaseMapping)material.GetFloat(kUVBase)) == UVBaseMapping.Triplanar);
            SetKeyword(material, "_NORMALMAP_TANGENT_SPACE", ((NormalMapSpace)material.GetFloat(kNormalMapSpace)) == NormalMapSpace.TangentSpace);
            bool perPixelDisplacement = material.GetFloat(kEnablePerPixelDisplacement) == 1.0;
            SetKeyword(material, "_PER_PIXEL_DISPLACEMENT", perPixelDisplacement);
            SetKeyword(material, "_DETAIL_MAP_WITH_NORMAL", ((DetailMapMode)material.GetFloat(kDetailMapMode)) == DetailMapMode.DetailWithNormal);
            SetKeyword(material, "_EMISSIVE_COLOR", ((EmissiveColorMode)material.GetFloat(kEmissiveColorMode)) == EmissiveColorMode.UseEmissiveColor);

			SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap) || material.GetTexture(kDetailMap)); // With details map, we always use a normal map and Unity provide a default (0, 0, 1) normal map for ir
			SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));
			SetKeyword(material, "_SPECULAROCCLUSIONMAP", material.GetTexture(kSpecularOcclusionMap));
			SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));
			SetKeyword(material, "_HEIGHTMAP", material.GetTexture(kHeightMap));
			SetKeyword(material, "_TANGENTMAP", material.GetTexture(kTangentMap));
			SetKeyword(material, "_ANISOTROPYMAP", material.GetTexture(kAnisotropyMap));
			SetKeyword(material, "_DETAIL_MAP", material.GetTexture(kDetailMap));

            bool needUV2 = (UVDetailMapping)material.GetFloat(kUVDetail) == UVDetailMapping.UV2 && (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV0;
            bool needUV3 = (UVDetailMapping)material.GetFloat(kUVDetail) == UVDetailMapping.UV3 && (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV0;

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
         }
    }
} // namespace UnityEditor
