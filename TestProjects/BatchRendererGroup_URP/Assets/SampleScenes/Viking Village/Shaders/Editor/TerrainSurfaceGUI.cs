using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using System;

internal class BlendShaderGUI : ShaderGUI
{
 	private static class Styles
	{
		public static GUIContent albedo = new GUIContent("Albedo", "Albedo (RGB) Emissive (A)");
		public static GUIContent specular = new GUIContent("Specular", "Specular (RGB) and Smoothness (A)");
		public static GUIContent normal = new GUIContent("Normal", "Normal Map");
		public static GUIContent blendMask = new GUIContent("Mask", "Mask (A) -> blend");

		public static string material0Header = "Primary Maps";
		public static string material1Header = "Secondary Maps";
		public static string maskHeader = "Blend : Mask";
	}

	MaterialProperty blendMask = null;
	MaterialProperty albedoMap = null;
	MaterialProperty specularMap = null;
	MaterialProperty bumpMap = null;
	
	MaterialProperty albedoMap2 = null;
	MaterialProperty specularMap2 = null;
	MaterialProperty bumpMap2 = null;

	const int kSecondLevelIndentOffset = 2;
	const float kVerticalSpacing = 2f;

	public void FindProperties (MaterialProperty[] props)
	{
		blendMask = FindProperty ("_Mask", props);

		albedoMap = FindProperty ("_MainTex", props);
		albedoMap2 = FindProperty ("_MainTex2", props);
		
		specularMap = FindProperty ("_SpecGlossMap", props);
		specularMap2 = FindProperty ("_SpecGlossMap2", props);

		bumpMap = FindProperty ("_NormalMap", props);
		bumpMap2 = FindProperty ("_NormalMap2", props);
	}

	public override void OnGUI (MaterialEditor materialEditor, MaterialProperty[] props)
	{
		FindProperties (props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly

		// Use default labelWidth
		EditorGUIUtility.labelWidth = 0f;

		// Detect any changes to the material
		EditorGUI.BeginChangeCheck();
		{
			GUILayout.Label (Styles.material0Header, EditorStyles.boldLabel);
				
			// Texture
			materialEditor.TexturePropertySingleLine (Styles.albedo, albedoMap);
			materialEditor.TexturePropertySingleLine (Styles.specular, specularMap);
			materialEditor.TexturePropertySingleLine (Styles.normal, bumpMap);
			materialEditor.TextureScaleOffsetProperty (albedoMap);
			
			GUILayout.Label (Styles.maskHeader, EditorStyles.boldLabel);
				
			materialEditor.TexturePropertySingleLine (Styles.blendMask, blendMask);
			materialEditor.TextureScaleOffsetProperty (blendMask);


			GUILayout.Label (Styles.material1Header, EditorStyles.boldLabel);
			
			materialEditor.TexturePropertySingleLine (Styles.albedo, albedoMap2);
			materialEditor.TexturePropertySingleLine (Styles.specular, specularMap2);
			materialEditor.TexturePropertySingleLine (Styles.normal, bumpMap2);
			materialEditor.TextureScaleOffsetProperty (albedoMap2);
		}
	}
}