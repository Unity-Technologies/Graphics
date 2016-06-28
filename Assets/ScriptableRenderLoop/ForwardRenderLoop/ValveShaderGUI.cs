// Copyright (c) Valve Corporation, All rights reserved. ======================================================================================================

#if ( UNITY_EDITOR )

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
internal class ValveShaderGUI : ShaderGUI
{
	public enum BlendMode
	{
		Opaque,
		AlphaTest,
		AlphaBlend,
		Glass,
		Additive
		// TODO: MaskedGlass that will require an additional grayscale texture to act as a standard alpha blend mask
	}

	public enum SpecularMode
	{
		None,
		BlinnPhong,
		Metallic
		//Anisotropic
	}

	private static class Styles
	{
		public static GUIStyle optionsButton = "PaneOptions";
		public static GUIContent uvSetLabel = new GUIContent("UV Set");
		public static GUIContent[] uvSetOptions = new GUIContent[] { new GUIContent("UV channel 0"), new GUIContent("UV channel 1") };

		public static GUIContent unlitText = new GUIContent( "Unlit", "" );

		public static string emptyTootip = "";
		public static GUIContent albedoText = new GUIContent("Albedo", "Albedo (RGB) and Transparency (A)");
		public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
		public static GUIContent specularMapText = new GUIContent("Specular", "Reflectance (RGB) and Gloss (A)");
		public static GUIContent reflectanceMinText = new GUIContent( "Reflectance Min", "" );
		public static GUIContent reflectanceMaxText = new GUIContent( "Reflectance Max", "" );
		public static GUIContent metallicMapText = new GUIContent( "Metallic", "Metallic (R) and Gloss (A)" );
		public static GUIContent smoothnessText = new GUIContent("Gloss", "");
		public static GUIContent normalMapText = new GUIContent("Normal", "Normal Map");
		//public static GUIContent heightMapText = new GUIContent("Height Map", "Height Map (G)");
		public static GUIContent cubeMapScalarText = new GUIContent( "Cube Map Scalar", "" );
		public static GUIContent occlusionText = new GUIContent("Occlusion", "Occlusion (G)");
		public static GUIContent occlusionStrengthDirectDiffuseText = new GUIContent( "Occlusion Direct Diffuse", "" );
		public static GUIContent occlusionStrengthDirectSpecularText = new GUIContent( "Occlusion Direct Specular", "" );
		public static GUIContent occlusionStrengthIndirectDiffuseText = new GUIContent( "Occlusion Indirect Diffuse", "" );
		public static GUIContent occlusionStrengthIndirectSpecularText = new GUIContent( "Occlusion Indirect Specular", "" );
		public static GUIContent emissionText = new GUIContent( "Emission", "Emission (RGB)" );
		public static GUIContent detailMaskText = new GUIContent("Detail Mask", "Mask for Secondary Maps (A)");
		public static GUIContent detailAlbedoText = new GUIContent("Detail Albedo", "Detail Albedo (RGB) multiplied by 2");
		public static GUIContent detailNormalMapText = new GUIContent("Detail Normal", "Detail Normal Map");
		public static GUIContent overrideLightmapText = new GUIContent( "Override Lightmap", "Requires ValveOverrideLightmap.cs scrip on object" );
		public static GUIContent worldAlignedTextureText = new GUIContent( "World Aligned Texture", "" );
		public static GUIContent worldAlignedTextureSizeText = new GUIContent( "Size", "" );
		public static GUIContent worldAlignedTextureNormalText = new GUIContent( "Normal", "" );
		public static GUIContent worldAlignedTexturePositionText = new GUIContent( "World Position", "" );

		public static string whiteSpaceString = " ";
		public static string primaryMapsText = "Main Maps";
		public static string secondaryMapsText = "Secondary Maps";
		public static string renderingMode = "Rendering Mode";
		public static string specularModeText = "Specular Mode";
		public static GUIContent emissiveWarning = new GUIContent( "Emissive value is animated but the material has not been configured to support emissive. Please make sure the material itself has some amount of emissive." );
		public static GUIContent emissiveColorWarning = new GUIContent ("Ensure emissive color is non-black for emission to have effect.");
		public static readonly string[] blendNames = Enum.GetNames (typeof (BlendMode));
		public static readonly string[] specularNames = Enum.GetNames( typeof( SpecularMode ) );
	}

	MaterialProperty unlit = null;
	MaterialProperty blendMode = null;
	MaterialProperty specularMode = null;
	MaterialProperty albedoMap = null;
	MaterialProperty albedoColor = null;
	MaterialProperty alphaCutoff = null;
	MaterialProperty specularMap = null;
	MaterialProperty specularColor = null;
	MaterialProperty reflectanceMin = null;
	MaterialProperty reflectanceMax = null;
	MaterialProperty metallicMap = null;
	MaterialProperty metallic = null;
	MaterialProperty smoothness = null;
	MaterialProperty bumpScale = null;
	MaterialProperty bumpMap = null;
	MaterialProperty cubeMapScalar = null;
	MaterialProperty occlusionStrength = null;
	MaterialProperty occlusionMap = null;
	MaterialProperty occlusionStrengthDirectDiffuse = null;
	MaterialProperty occlusionStrengthDirectSpecular = null;
	MaterialProperty occlusionStrengthIndirectDiffuse = null;
	MaterialProperty occlusionStrengthIndirectSpecular = null;
	//MaterialProperty heigtMapScale = null;
	//MaterialProperty heightMap = null;
	MaterialProperty emissionColorForRendering = null;
	MaterialProperty emissionMap = null;
	MaterialProperty detailMask = null;
	MaterialProperty detailAlbedoMap = null;
	MaterialProperty detailNormalMapScale = null;
	MaterialProperty detailNormalMap = null;
	MaterialProperty uvSetSecondary = null;
	MaterialProperty overrideLightmap = null;
	MaterialProperty worldAlignedTexture = null;
	MaterialProperty worldAlignedTextureSize = null;
	MaterialProperty worldAlignedTextureNormal = null;
	MaterialProperty worldAlignedTexturePosition = null;

	MaterialEditor m_MaterialEditor;
	ColorPickerHDRConfig m_ColorPickerHDRConfig = new ColorPickerHDRConfig(0f, 99f, 1/99f, 3f);

	bool m_FirstTimeApply = true;

	public void FindProperties (MaterialProperty[] props)
	{
		unlit = FindProperty( "g_bUnlit", props );
		blendMode = FindProperty( "_Mode", props );
		specularMode = FindProperty( "_SpecularMode", props );
		albedoMap = FindProperty( "_MainTex", props );
		albedoColor = FindProperty ("_Color", props);
		alphaCutoff = FindProperty ("_Cutoff", props);
		specularMap = FindProperty ("_SpecGlossMap", props, false);
		specularColor = FindProperty ("_SpecColor", props, false);
		reflectanceMin = FindProperty( "g_flReflectanceMin", props );
		reflectanceMax = FindProperty( "g_flReflectanceMax", props );
		metallicMap = FindProperty ("_MetallicGlossMap", props, false);
		metallic = FindProperty ("_Metallic", props, false);
		smoothness = FindProperty ("_Glossiness", props);
		bumpScale = FindProperty ("_BumpScale", props);
		bumpMap = FindProperty ("_BumpMap", props);
		//heigtMapScale = FindProperty ("_Parallax", props);
		//heightMap = FindProperty("_ParallaxMap", props);
		cubeMapScalar = FindProperty( "g_flCubeMapScalar", props );
		occlusionStrength = FindProperty ("_OcclusionStrength", props);
		occlusionStrengthDirectDiffuse = FindProperty( "_OcclusionStrengthDirectDiffuse", props );
		occlusionStrengthDirectSpecular = FindProperty( "_OcclusionStrengthDirectSpecular", props );
		occlusionStrengthIndirectDiffuse = FindProperty( "_OcclusionStrengthIndirectDiffuse", props );
		occlusionStrengthIndirectSpecular = FindProperty( "_OcclusionStrengthIndirectSpecular", props );
		occlusionMap = FindProperty ("_OcclusionMap", props);
		emissionColorForRendering = FindProperty ("_EmissionColor", props);
		emissionMap = FindProperty ("_EmissionMap", props);
		detailMask = FindProperty ("_DetailMask", props);
		detailAlbedoMap = FindProperty ("_DetailAlbedoMap", props);
		detailNormalMapScale = FindProperty ("_DetailNormalMapScale", props);
		detailNormalMap = FindProperty ("_DetailNormalMap", props);
		uvSetSecondary = FindProperty ("_UVSec", props);
		overrideLightmap = FindProperty( "g_tOverrideLightmap", props );
		worldAlignedTexture = FindProperty( "g_bWorldAlignedTexture", props, false );
		worldAlignedTextureSize = FindProperty( "g_vWorldAlignedTextureSize", props, worldAlignedTexture != null );
		worldAlignedTextureNormal = FindProperty( "g_vWorldAlignedTextureNormal", props, worldAlignedTexture != null );
		worldAlignedTexturePosition = FindProperty( "g_vWorldAlignedTexturePosition", props, worldAlignedTexture != null );
	}

	public override void OnGUI (MaterialEditor materialEditor, MaterialProperty[] props)
	{
		FindProperties (props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
		m_MaterialEditor = materialEditor;
		Material material = materialEditor.target as Material;

		ShaderPropertiesGUI (material);

		// Make sure that needed keywords are set up if we're switching some existing
		// material to a standard shader.
		if (m_FirstTimeApply)
		{
			SetMaterialKeywords (material);
			m_FirstTimeApply = false;
		}
	}

	public void Vector3GUI( GUIContent label, MaterialProperty materialProperty )
	{
		Vector4 v4 = materialProperty.vectorValue;
		Vector3 v3 = EditorGUILayout.Vector3Field( label, new Vector3( v4.x, v4.y, v4.z ) );
		materialProperty.vectorValue = new Vector4( v3.x, v3.y, v3.z, 0.0f );
	}

	public void ShaderPropertiesGUI (Material material)
	{
		// Use default labelWidth
		EditorGUIUtility.labelWidth = 0f;

		// Detect any changes to the material
		EditorGUI.BeginChangeCheck();
		{
			m_MaterialEditor.ShaderProperty( unlit, Styles.unlitText.text );
			bool bUnlit = ( unlit.floatValue != 0.0f );

			BlendModePopup();

			if ( !bUnlit )
			{
				SpecularModePopup();
			}

			EditorGUILayout.Space();

			//GUILayout.Label( Styles.primaryMapsText, EditorStyles.boldLabel );
			DoAlbedoArea( material );
			if ( !bUnlit )
			{
				m_MaterialEditor.TexturePropertySingleLine( Styles.normalMapText, bumpMap, bumpMap.textureValue != null ? bumpScale : null );
				DoSpecularMetallicArea( material );
				m_MaterialEditor.TexturePropertySingleLine( Styles.occlusionText, occlusionMap, occlusionMap.textureValue != null ? occlusionStrength : null );
				if ( occlusionMap.textureValue != null )
				{
					m_MaterialEditor.ShaderProperty( occlusionStrengthDirectDiffuse, Styles.occlusionStrengthDirectDiffuseText.text, 2 );
					m_MaterialEditor.ShaderProperty( occlusionStrengthDirectSpecular, Styles.occlusionStrengthDirectSpecularText.text, 2 );
					m_MaterialEditor.ShaderProperty( occlusionStrengthIndirectDiffuse, Styles.occlusionStrengthIndirectDiffuseText.text, 2 );
					m_MaterialEditor.ShaderProperty( occlusionStrengthIndirectSpecular, Styles.occlusionStrengthIndirectSpecularText.text, 2 );
				}
				m_MaterialEditor.ShaderProperty( cubeMapScalar, Styles.cubeMapScalarText.text, 0 );
			}
			//m_MaterialEditor.TexturePropertySingleLine(Styles.heightMapText, heightMap, heightMap.textureValue != null ? heigtMapScale : null);
			DoEmissionArea( material );
			m_MaterialEditor.TexturePropertySingleLine( Styles.detailMaskText, detailMask );
			if ( !bUnlit )
			{
				m_MaterialEditor.TexturePropertySingleLine( Styles.overrideLightmapText, overrideLightmap );
			}

			EditorGUI.BeginChangeCheck(); // !!! AV - This is from Unity's script. Can these Begin/End calls be nested like this?
			m_MaterialEditor.TextureScaleOffsetProperty( albedoMap );
			if ( EditorGUI.EndChangeCheck() )
			{
				emissionMap.textureScaleAndOffset = albedoMap.textureScaleAndOffset; // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake
			}

			if ( worldAlignedTexture != null )
			{
				m_MaterialEditor.ShaderProperty( worldAlignedTexture, Styles.worldAlignedTextureText.text );

				if ( worldAlignedTexture.floatValue != 0.0f )
				{
					EditorGUI.indentLevel = 2;
					Vector3GUI( Styles.worldAlignedTextureSizeText, worldAlignedTextureSize );
					Vector3GUI( Styles.worldAlignedTextureNormalText, worldAlignedTextureNormal );
					Vector3GUI( Styles.worldAlignedTexturePositionText, worldAlignedTexturePosition );
					EditorGUI.indentLevel = 0;
				}
			}

			EditorGUILayout.Space();

			// Secondary properties
			GUILayout.Label( Styles.secondaryMapsText, EditorStyles.boldLabel );
			m_MaterialEditor.TexturePropertySingleLine( Styles.detailAlbedoText, detailAlbedoMap );
			if ( !bUnlit )
			{
				m_MaterialEditor.TexturePropertySingleLine( Styles.detailNormalMapText, detailNormalMap, detailNormalMapScale );
			}
			m_MaterialEditor.TextureScaleOffsetProperty( detailAlbedoMap );
			m_MaterialEditor.ShaderProperty( uvSetSecondary, Styles.uvSetLabel.text );
		}
		if ( EditorGUI.EndChangeCheck() )
		{
			foreach ( var obj in blendMode.targets )
			{
				MaterialChanged( ( Material )obj );
			}

			foreach ( var obj in specularMode.targets )
			{
				MaterialChanged( ( Material )obj );
			}
		}
	}

	public override void AssignNewShaderToMaterial (Material material, Shader oldShader, Shader newShader)
	{
		base.AssignNewShaderToMaterial( material, oldShader, newShader );

		if ( oldShader == null )
			return;

		// Convert to vr_standard
		if ( newShader.name.Equals( "Valve/vr_standard" ) )
		{
			List<string> unknownShaders = new List<string>();
			ValveRefreshStandardShader.StandardToValveSingleMaterial( material, oldShader, newShader, false, unknownShaders );
		}

		// Legacy shaders
		if ( !oldShader.name.Contains( "Legacy Shaders/" ) )
			return;

		BlendMode blendMode = BlendMode.Opaque;
		if (oldShader.name.Contains("/Transparent/Cutout/"))
		{
			blendMode = BlendMode.AlphaTest;
		}
		else if (oldShader.name.Contains("/Transparent/"))
		{
			// NOTE: legacy shaders did not provide physically based transparency
			// therefore Fade mode
			blendMode = BlendMode.AlphaBlend;
		}
		material.SetFloat("_Mode", (float)blendMode);

		MaterialChanged(material);
	}

	void BlendModePopup()
	{
		EditorGUI.showMixedValue = blendMode.hasMixedValue;
		var mode = (BlendMode)blendMode.floatValue;

		EditorGUI.BeginChangeCheck();
		mode = (BlendMode)EditorGUILayout.Popup(Styles.renderingMode, (int)mode, Styles.blendNames);
		if (EditorGUI.EndChangeCheck())
		{
			m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Mode");
			blendMode.floatValue = (float)mode;
		}

		EditorGUI.showMixedValue = false;
	}

	void SpecularModePopup()
	{
		EditorGUI.showMixedValue = specularMode.hasMixedValue;
		var mode = ( SpecularMode )specularMode.floatValue;

		EditorGUI.BeginChangeCheck();
		mode = ( SpecularMode )EditorGUILayout.Popup( Styles.specularModeText, ( int )mode, Styles.specularNames );
		if ( EditorGUI.EndChangeCheck() )
		{
			m_MaterialEditor.RegisterPropertyChangeUndo( "Specular Mode" );
			specularMode.floatValue = ( float )mode;
		}

		EditorGUI.showMixedValue = false;
	}

	void DoAlbedoArea(Material material)
	{
		m_MaterialEditor.TexturePropertySingleLine(Styles.albedoText, albedoMap, albedoColor);
		if (((BlendMode)material.GetFloat("_Mode") == BlendMode.AlphaTest))
		{
			m_MaterialEditor.ShaderProperty(alphaCutoff, Styles.alphaCutoffText.text, MaterialEditor.kMiniTextureFieldLabelIndentLevel+1);
		}
	}

	void DoEmissionArea(Material material)
	{
		float brightness = emissionColorForRendering.colorValue.maxColorComponent;
		bool showHelpBox = !HasValidEmissiveKeyword(material);
		bool showEmissionColorAndGIControls = brightness > 0.0f;
		
		bool hadEmissionTexture = emissionMap.textureValue != null;

		// Texture and HDR color controls
		m_MaterialEditor.TexturePropertyWithHDRColor(Styles.emissionText, emissionMap, emissionColorForRendering, m_ColorPickerHDRConfig, false);

		// If texture was assigned and color was black set color to white
		if (emissionMap.textureValue != null && !hadEmissionTexture && brightness <= 0f)
			emissionColorForRendering.colorValue = Color.white;

		// Dynamic Lightmapping mode
		if (showEmissionColorAndGIControls)
		{
			bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled(emissionColorForRendering.colorValue);
			using ( new EditorGUI.DisabledScope( !shouldEmissionBeEnabled ) )
			{
				m_MaterialEditor.LightmapEmissionProperty( MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1 );
			}
		}

		if (showHelpBox)
		{
			EditorGUILayout.HelpBox(Styles.emissiveWarning.text, MessageType.Warning);
		}
	}

	void DoSpecularMetallicArea( Material material )
	{
		SpecularMode specularMode = ( SpecularMode )material.GetInt( "_SpecularMode" );
		if ( specularMode == SpecularMode.BlinnPhong )
		{
			if (specularMap.textureValue == null)
			{
				m_MaterialEditor.TexturePropertyTwoLines( Styles.specularMapText, specularMap, specularColor, Styles.smoothnessText, smoothness );
			}
			else
			{
				m_MaterialEditor.TexturePropertySingleLine( Styles.specularMapText, specularMap );
				m_MaterialEditor.ShaderProperty( reflectanceMin, Styles.reflectanceMinText.text, 2 );
				m_MaterialEditor.ShaderProperty( reflectanceMax, Styles.reflectanceMaxText.text, 2 );
			}
		}
		else if ( specularMode == SpecularMode.Metallic )
		{
			if (metallicMap.textureValue == null)
				m_MaterialEditor.TexturePropertyTwoLines(Styles.metallicMapText, metallicMap, metallic, Styles.smoothnessText, smoothness);
			else
				m_MaterialEditor.TexturePropertySingleLine(Styles.metallicMapText, metallicMap);
		}
	}

	public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
	{
		switch (blendMode)
		{
			case BlendMode.Opaque:
				material.SetOverrideTag("RenderType", "");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = -1;
				break;
			case BlendMode.AlphaTest:
				material.SetOverrideTag("RenderType", "TransparentCutout");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.EnableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 2450;
				break;
			case BlendMode.AlphaBlend:
				material.SetOverrideTag("RenderType", "Transparent");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.EnableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 3000;
				break;
			case BlendMode.Glass:
				material.SetOverrideTag("RenderType", "Transparent");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 3000;
				break;
			case BlendMode.Additive:
				material.SetOverrideTag( "RenderType", "Transparent" );
				material.SetInt( "_SrcBlend", ( int )UnityEngine.Rendering.BlendMode.One );
				material.SetInt( "_DstBlend", ( int )UnityEngine.Rendering.BlendMode.One );
				material.SetInt( "_ZWrite", 0 );
				material.DisableKeyword( "_ALPHATEST_ON" );
				material.DisableKeyword( "_ALPHABLEND_ON" );
				material.DisableKeyword( "_ALPHAPREMULTIPLY_ON" );
				material.renderQueue = 3000;
				break;
		}
	}

	static bool ShouldEmissionBeEnabled (Color color)
	{
		return color.maxColorComponent > (0.1f / 255.0f);
	}

	static void SetMaterialKeywords(Material material)
	{
		// Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
		// (MaterialProperty value might come from renderer material property block)
		SetKeyword (material, "_NORMALMAP", material.GetTexture ("_BumpMap") || material.GetTexture ("_DetailNormalMap"));

		SpecularMode specularMode = ( SpecularMode )material.GetInt( "_SpecularMode" );
		if ( specularMode == SpecularMode.BlinnPhong )
		{
			SetKeyword( material, "_SPECGLOSSMAP", material.GetTexture( "_SpecGlossMap" ) );
		}
		else if ( specularMode == SpecularMode.Metallic )
		{
			SetKeyword( material, "_METALLICGLOSSMAP", material.GetTexture( "_MetallicGlossMap" ) );
		}
		SetKeyword( material, "S_SPECULAR_NONE", specularMode == SpecularMode.None );
		SetKeyword( material, "S_SPECULAR_BLINNPHONG", specularMode == SpecularMode.BlinnPhong );
		SetKeyword( material, "S_SPECULAR_METALLIC", specularMode == SpecularMode.Metallic );
		SetKeyword( material, "S_OCCLUSION", material.GetTexture("_OcclusionMap") );

		SetKeyword( material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap"));
		SetKeyword( material, "_DETAIL_MULX2", material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap"));
		SetKeyword( material, "S_OVERRIDE_LIGHTMAP", material.GetTexture( "g_tOverrideLightmap" ) );
		
		SetKeyword( material, "S_UNLIT", material.GetInt( "g_bUnlit" ) == 1 );
		SetKeyword( material, "S_WORLD_ALIGNED_TEXTURE", material.GetInt( "g_bWorldAlignedTexture" ) == 1 );

		bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled (material.GetColor("_EmissionColor"));
		SetKeyword (material, "_EMISSION", shouldEmissionBeEnabled);

		// Setup lightmap emissive flags
		MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
		if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0)
		{
			flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
			if (!shouldEmissionBeEnabled)
				flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

			material.globalIlluminationFlags = flags;
		}

		// Reflectance constants
		float flReflectanceMin = material.GetFloat( "g_flReflectanceMin" );
		float flReflectanceMax = material.GetFloat( "g_flReflectanceMax" );
		material.SetFloat( "g_flReflectanceScale", Mathf.Max( flReflectanceMin, flReflectanceMax ) - flReflectanceMin );
		material.SetFloat( "g_flReflectanceBias", flReflectanceMin );
		
		// World aligned texture constants
		Vector4 worldAlignedTextureNormal = material.GetVector( "g_vWorldAlignedTextureNormal" );
		Vector3 normal = new Vector3( worldAlignedTextureNormal.x, worldAlignedTextureNormal.y, worldAlignedTextureNormal.z );
		normal = ( normal.sqrMagnitude > 0.0f ) ? normal : Vector3.up;
		Vector3 tangentU = Vector3.zero, tangentV = Vector3.zero;
		Vector3.OrthoNormalize( ref normal, ref tangentU, ref tangentV );
		material.SetVector( "g_vWorldAlignedNormalTangentU", new Vector4( tangentU.x, tangentU.y, tangentU.z, 0.0f ) );
		material.SetVector( "g_vWorldAlignedNormalTangentV", new Vector4( tangentV.x, tangentV.y, tangentV.z, 0.0f ) );

		// Static combo skips
		if ( material.GetInt( "g_bUnlit" ) == 1 )
		{
			material.DisableKeyword( "_NORMALMAP" );
			material.EnableKeyword( "S_SPECULAR_NONE" );
			material.DisableKeyword( "S_SPECULAR_BLINNPHONG" );
			material.DisableKeyword( "S_SPECULAR_METALLIC" );
			material.DisableKeyword( "_METALLICGLOSSMAP" );
			material.DisableKeyword( "_SPECGLOSSMAP" );
			material.DisableKeyword( "S_OVERRIDE_LIGHTMAP" );
		}
	}

	bool HasValidEmissiveKeyword (Material material)
	{
		// Material animation might be out of sync with the material keyword.
		// So if the emission support is disabled on the material, but the property blocks have a value that requires it, then we need to show a warning.
		// (note: (Renderer MaterialPropertyBlock applies its values to emissionColorForRendering))
		bool hasEmissionKeyword = material.IsKeywordEnabled ("_EMISSION");
		if (!hasEmissionKeyword && ShouldEmissionBeEnabled (emissionColorForRendering.colorValue))
			return false;
		else
			return true;
	}

	static void MaterialChanged(Material material)
	{
		SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));

		SetMaterialKeywords(material);
	}

	static void SetKeyword(Material m, string keyword, bool state)
	{
		if (state)
			m.EnableKeyword (keyword);
		else
			m.DisableKeyword (keyword);
	}
}

} // namespace UnityEditor

#endif
