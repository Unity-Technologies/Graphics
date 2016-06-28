// Copyright (c) Valve Corporation, All rights reserved. ======================================================================================================

#if ( UNITY_EDITOR )

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;

//---------------------------------------------------------------------------------------------------------------------------------------------------
public class ValveRefreshStandardShader
{
	static void SaveAssetsAndFreeMemory()
	{
		UnityEditor.AssetDatabase.SaveAssets();
		GC.Collect();
		UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate();
		UnityEditor.AssetDatabase.Refresh();
	}

	private static void RenameShadersInAllMaterials( string nameBefore, string nameAfter )
	{
		Shader destShader = Shader.Find( nameAfter );
		if ( destShader == null )
		{
			return;
		}

		int i = 0;
		int nCount = 0;
		foreach ( string s in UnityEditor.AssetDatabase.GetAllAssetPaths() )
		{
			if ( s.EndsWith( ".mat", StringComparison.OrdinalIgnoreCase ) )
			{
				nCount++;
			}
		}

		foreach ( string s in UnityEditor.AssetDatabase.GetAllAssetPaths() )
		{
			if ( s.EndsWith( ".mat", StringComparison.OrdinalIgnoreCase ) )
			{
				i++;
				if ( UnityEditor.EditorUtility.DisplayCancelableProgressBar( "Valve Material Conversion", string.Format( "({0} of {1}) {2}", i, nCount, s ), ( float )i / ( float )nCount ) )
				{
					break;
				}

				Material m = UnityEditor.AssetDatabase.LoadMainAssetAtPath( s ) as Material;
				//Debug.Log( m.name + "\n" );
				//Debug.Log( m.shader.name + "\n" );
				if ( m.shader.name.Equals( nameBefore ) )
				{
					Debug.Log( "Converting from \"" + nameBefore + "\"-->\"" + nameAfter + "\": " + m.name + "\n" );
					m.shader = destShader;
					SaveAssetsAndFreeMemory();
				}
			}
		}

		//object[] obj = GameObject.FindObjectsOfType( typeof( GameObject ) );
		//foreach ( object o in obj )
		//{
		//	GameObject g = ( GameObject )o;
		//
		//	Renderer[] renderers = g.GetComponents<Renderer>();
		//	foreach ( Renderer r in renderers )
		//	{
		//		foreach ( Material m in r.sharedMaterials )
		//		{
		//			//Debug.Log( m.name + "\n" );
		//			//Debug.Log( m.shader.name + "\n" );
		//			if ( m.shader.name.Equals( "Standard" ) )
		//			{
		//				Debug.Log( "Refreshing Standard shader for material: " + m.name + "\n" );
		//				m.shader = destShader;
		//				SaveAssetsAndFreeMemory();
		//			}
		//		}
		//	}
		//}

		UnityEditor.EditorUtility.ClearProgressBar();
	}

	//---------------------------------------------------------------------------------------------------------------------------------------------------
	public static bool StandardToValveSingleMaterial( Material m, Shader srcShader, Shader destShader, bool bRecordUnknownShaders, List<string> unknownShaders )
	{
		string n = srcShader.name;

		if ( n.Equals( destShader.name ) )
		{
			// Do nothing
			//Debug.Log( "     Skipping " + m.name + "\n" );
			return false;
		}
		else if ( n.Equals( "Standard" ) || n.Equals( "Valve/VR/Standard" ) )
		{
			// Metallic specular
			Debug.Log( "     Converting from \"" + n + "\"-->\"" + destShader.name + "\": " + m.name + "\n" );

			m.shader = destShader;
			m.SetOverrideTag( "OriginalShader", n );

			m.SetInt( "_SpecularMode", 2 );
			m.DisableKeyword( "S_SPECULAR_NONE" );
			m.DisableKeyword( "S_SPECULAR_BLINNPHONG" );
			m.EnableKeyword( "S_SPECULAR_METALLIC" );
			return true;
		}
		else if ( n.Equals( "Standard (Specular setup)" ) || n.Equals( "Legacy Shaders/Bumped Diffuse" ) || n.Equals( "Legacy Shaders/Transparent/Diffuse" ) )
		{
			// Regular specular
			Debug.Log( "     Converting from \"" + n + "\"-->\"" + destShader.name + "\": " + m.name + "\n" );

			m.shader = destShader;
			m.SetOverrideTag( "OriginalShader", n );

			m.SetInt( "_SpecularMode", 1 );
			m.DisableKeyword( "S_SPECULAR_NONE" );
			m.EnableKeyword( "S_SPECULAR_BLINNPHONG" );
			m.DisableKeyword( "S_SPECULAR_METALLIC" );

			if ( n.Equals( "Legacy Shaders/Transparent/Diffuse" ) )
			{
				m.SetFloat( "_Mode", 2 );
				m.SetOverrideTag( "RenderType", "Transparent" );
				m.SetInt( "_SrcBlend", ( int )UnityEngine.Rendering.BlendMode.SrcAlpha );
				m.SetInt( "_DstBlend", ( int )UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha );
				m.SetInt( "_ZWrite", 0 );
				m.DisableKeyword( "_ALPHATEST_ON" );
				m.EnableKeyword( "_ALPHABLEND_ON" );
				m.DisableKeyword( "_ALPHAPREMULTIPLY_ON" );
				m.renderQueue = 3000;
			}
			return true;
		}
		else if ( n.Equals( "Unlit/Color" ) || n.Equals( "Unlit/Texture" ) || n.Equals( "Unlit/Transparent" ) || n.Equals( "Unlit/Transparent Cutout" ) )
		{
			// Unlit
			Debug.Log( "     Converting from \"" + n + "\"-->\"" + destShader.name + "\": " + m.name + "\n" );

			m.shader = destShader;
			m.SetOverrideTag( "OriginalShader", n );
			m.SetInt( "g_bUnlit", 1 );
			m.EnableKeyword( "S_UNLIT" );

			m.SetColor( "_EmissionColor", Color.black );

			if ( n.Equals( "Unlit/Color" ) )
			{
				m.SetTexture( "_MainTex", Texture2D.whiteTexture );
			}
			else
			{
				m.SetColor( "_Color", Color.white );

				if ( n.Equals( "Unlit/Transparent Cutout" ) )
				{
					m.SetFloat( "_Mode", 1 );
					m.SetOverrideTag( "RenderType", "TransparentCutout" );
					m.SetInt( "_SrcBlend", ( int )UnityEngine.Rendering.BlendMode.One );
					m.SetInt( "_DstBlend", ( int )UnityEngine.Rendering.BlendMode.Zero );
					m.SetInt( "_ZWrite", 1 );
					m.EnableKeyword( "_ALPHATEST_ON" );
					m.DisableKeyword( "_ALPHABLEND_ON" );
					m.DisableKeyword( "_ALPHAPREMULTIPLY_ON" );
					m.renderQueue = 2450;
				}
				else if ( n.Equals( "Unlit/Transparent" ) )
				{
					m.SetFloat( "_Mode", 2 );
					m.SetOverrideTag( "RenderType", "Transparent" );
					m.SetInt( "_SrcBlend", ( int )UnityEngine.Rendering.BlendMode.SrcAlpha );
					m.SetInt( "_DstBlend", ( int )UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha );
					m.SetInt( "_ZWrite", 0 );
					m.DisableKeyword( "_ALPHATEST_ON" );
					m.EnableKeyword( "_ALPHABLEND_ON" );
					m.DisableKeyword( "_ALPHAPREMULTIPLY_ON" );
					m.renderQueue = 3000;
				}
			}
			
			return true;
		}
		else if ( bRecordUnknownShaders )
		{
			// Don't know how to convert, so add to list to spew at the end
			Debug.LogWarning( "     Don't know how to convert shader \"" + n + "\"" + " in material \"" + m.name + "\"" + "\n" );
			if ( !unknownShaders.Contains( n ) )
			{
				unknownShaders.Add( n );
			}
			return false;
		}

		return false;
	}

	//---------------------------------------------------------------------------------------------------------------------------------------------------
	private static void StandardToValve( bool bConvertAllMaterials )
	{
		int nNumMaterialsConverted = 0;
		Debug.Log( "Begin Convert to Valve Shaders\n\n" + Time.realtimeSinceStartup );

		Shader destShader = Shader.Find( "Valve/vr_standard" );
		if ( destShader == null )
		{
			Debug.LogWarning( "     ERROR! Cannot find the \"Valve/vr_standard\" shader!" + "\n" );
			return;
		}

		List< string > unknownShaders = new List< string >();
		List< string > alreadyConvertedMaterials = new List<string>();

		if ( bConvertAllMaterials )
		{
			int i = 0;
			int nCount = 0;
			foreach ( string s in UnityEditor.AssetDatabase.GetAllAssetPaths() )
			{
				if ( s.EndsWith( ".mat", StringComparison.OrdinalIgnoreCase ) )
				{
					nCount++;
				}
			}

			foreach ( string s in UnityEditor.AssetDatabase.GetAllAssetPaths() )
			{
				if ( s.EndsWith( ".mat", StringComparison.OrdinalIgnoreCase ) )
				{
					i++;
					if ( UnityEditor.EditorUtility.DisplayCancelableProgressBar( "Valve Material Conversion to Valve Shaders", string.Format( "({0} of {1}) {2}", i, nCount, s ), ( float )i / ( float )nCount ) )
					{
						break;
					}

					Material m = UnityEditor.AssetDatabase.LoadMainAssetAtPath( s ) as Material;

					if ( ( m == null ) || ( m.shader == null ) )
						continue;

					if ( !m.name.StartsWith( "" ) )
						continue;

					if ( alreadyConvertedMaterials.Contains( m.name ) )
						continue;
					alreadyConvertedMaterials.Add( m.name );

					if ( StandardToValveSingleMaterial( m, m.shader, destShader, true, unknownShaders ) )
					{
						nNumMaterialsConverted++;
					}

					SaveAssetsAndFreeMemory();
				}
			}
		}
		else
		{
			int i = 0;
			int nCount = 0;
			Renderer[] renderers = GameObject.FindObjectsOfType<Renderer>();
			List<string> countedMaterials = new List<string>();
			foreach ( Renderer r in renderers )
			{
				if ( r.sharedMaterials == null )
					continue;

				foreach ( Material m in r.sharedMaterials )
				{
					if ( ( m == null ) || ( m.shader == null ) )
						continue;

					if ( !m.name.StartsWith( "" ) )
						continue;

					if ( countedMaterials.Contains( m.name ) )
						continue;
					countedMaterials.Add( m.name );

					string assetPath = UnityEditor.AssetDatabase.GetAssetPath( m );
					Material mainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath( assetPath ) as Material;
					if ( !mainAsset )
					{
						Debug.LogError( "Error calling LoadMainAssetAtPath( " + assetPath + " )!\n\n" );
						continue;
					}

					nCount++;
				}
			}

			bool bCanceled = false;
			foreach ( Renderer r in renderers )
			{
				if ( r.sharedMaterials == null )
					continue;

				foreach ( Material m in r.sharedMaterials )
				{
					if ( ( m == null ) || ( m.shader == null ) )
						continue;

					if ( !m.name.StartsWith( "" ) )
						continue;

					if ( alreadyConvertedMaterials.Contains( m.name ) )
						continue;
					alreadyConvertedMaterials.Add( m.name );

					string assetPath = UnityEditor.AssetDatabase.GetAssetPath( m );
					Material mainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath( assetPath ) as Material;
					if ( !mainAsset )
					{
						Debug.LogError( "Error calling LoadMainAssetAtPath( " + assetPath + " )!\n\n" );
						continue;
					}

					i++;
					if ( UnityEditor.EditorUtility.DisplayCancelableProgressBar( "Valve Material Conversion to Valve Shaders", string.Format( "({0} of {1}) {2}", i, nCount, assetPath ), ( float )i / ( float )nCount ) )
					{
						bCanceled = true;
						break;
					}

					if ( StandardToValveSingleMaterial( mainAsset, mainAsset.shader, destShader, true, unknownShaders ) )
					{
						nNumMaterialsConverted++;
					}

					SaveAssetsAndFreeMemory();
				}

				if ( bCanceled )
					break;
			}
		}

		foreach ( string s in unknownShaders )
		{
			Debug.LogWarning( "     Don't know how to convert shader \"" + s + "\"" + "\n" );
		}

		Debug.Log( "Converted " + nNumMaterialsConverted + " materials to Valve Shaders\n\n" + Time.realtimeSinceStartup );

		UnityEditor.EditorUtility.ClearProgressBar();
	}

	[UnityEditor.MenuItem( "Valve/Shader Dev/Convert Active Materials to Valve Shaders", false, 50 )]
	private static void StandardToValveCurrent()
	{
		StandardToValve( false );
	}

	[UnityEditor.MenuItem( "Valve/Shader Dev/Convert All Materials to Valve Shaders", false, 100 )]
	private static void StandardToValveAll()
	{
		StandardToValve( true );
	}

	//---------------------------------------------------------------------------------------------------------------------------------------------------
	private static bool ValveToStandardSingleMaterial( Material m, Shader destShaderStandard, Shader destShaderStandardSpecular )
	{
		if ( m.shader.name.Equals( "Valve/vr_standard" ) )
		{
			if ( ( m.GetTag( "OriginalShader", true ) != null ) && ( m.GetTag( "OriginalShader", true ).Length > 0 ) )
			{
				Debug.Log( "     Converting from \"" + m.shader.name + "\"-->\"" + m.GetTag( "OriginalShader", true ) + "\": " + m.name + "\n" );
				m.shader = Shader.Find( m.GetTag( "OriginalShader", true ) );
				return true;
			}
			else if ( m.GetInt( "_SpecularMode" ) == 2 )
			{
				// Metallic specular
				Debug.Log( "     Converting from \"" + m.shader.name + "\"-->\"" + destShaderStandard.name + "\": " + m.name + "\n" );
				m.shader = destShaderStandard;
				return true;
			}
			else
			{
				// Regular specular
				Debug.Log( "     Converting from \"" + m.shader.name + "\"-->\"" + destShaderStandardSpecular.name + "\": " + m.name + "\n" );
				m.shader = destShaderStandardSpecular;
				return true;
			}
		}

		return false;
	}

	//---------------------------------------------------------------------------------------------------------------------------------------------------
	[UnityEditor.MenuItem( "Valve/Shader Dev/Convert All Materials Back to Unity Shaders", false, 101 )]
	private static void ValveToStandard( bool bConvertAllMaterials )
	{
		int nNumMaterialsConverted = 0;
		Debug.Log( "Begin Convert to Unity Shaders\n\n" + Time.realtimeSinceStartup );

		Shader destShaderStandard = Shader.Find( "Standard" );
		if ( destShaderStandard == null )
		{
			Debug.LogWarning( "     ERROR! Cannot find the \"Standard\" shader!" + "\n" );
			return;
		}

		Shader destShaderStandardSpecular = Shader.Find( "Standard (Specular setup)" );
		if ( destShaderStandardSpecular == null )
		{
			Debug.LogWarning( "     ERROR! Cannot find the \"Standard (Specular setup)\" shader!" + "\n" );
			return;
		}

		List<string> alreadyConvertedMaterials = new List<string>();

		if ( bConvertAllMaterials )
		{
			int i = 0;
			int nCount = 0;
			foreach ( string s in UnityEditor.AssetDatabase.GetAllAssetPaths() )
			{
				if ( s.EndsWith( ".mat", StringComparison.OrdinalIgnoreCase ) )
				{
					nCount++;
				}
			}

			foreach ( string s in UnityEditor.AssetDatabase.GetAllAssetPaths() )
			{
				if ( s.EndsWith( ".mat", StringComparison.OrdinalIgnoreCase ) )
				{
					i++;
					if ( UnityEditor.EditorUtility.DisplayCancelableProgressBar( "Valve Material Conversion Back to Unity Shaders", string.Format( "({0} of {1}) {2}", i, nCount, s ), ( float )i / ( float )nCount ) )
					{
						break;
					}

					Material m = UnityEditor.AssetDatabase.LoadMainAssetAtPath( s ) as Material;
					if ( ValveToStandardSingleMaterial( m, destShaderStandard, destShaderStandardSpecular ) )
					{
						nNumMaterialsConverted++;
					}

					SaveAssetsAndFreeMemory();
				}
			}
		}
		else
		{
			int i = 0;
			int nCount = 0;
			Renderer[] renderers = GameObject.FindObjectsOfType<Renderer>();
			List<string> countedMaterials = new List<string>();
			foreach ( Renderer r in renderers )
			{
				if ( r.sharedMaterials == null )
					continue;

				foreach ( Material m in r.sharedMaterials )
				{
					if ( ( m == null ) || ( m.shader == null ) )
						continue;

					if ( !m.name.StartsWith( "" ) )
						continue;

					if ( countedMaterials.Contains( m.name ) )
						continue;
					countedMaterials.Add( m.name );

					string assetPath = UnityEditor.AssetDatabase.GetAssetPath( m );
					Material mainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath( assetPath ) as Material;
					if ( !mainAsset )
					{
						Debug.LogError( "Error calling LoadMainAssetAtPath( " + assetPath + " )!\n\n" );
						continue;
					}

					nCount++;
				}
			}

			bool bCanceled = false;
			foreach ( Renderer r in renderers )
			{
				if ( r.sharedMaterials == null )
					continue;

				foreach ( Material m in r.sharedMaterials )
				{
					if ( ( m == null ) || ( m.shader == null ) )
						continue;

					if ( !m.name.StartsWith( "" ) )
						continue;

					if ( alreadyConvertedMaterials.Contains( m.name ) )
						continue;
					alreadyConvertedMaterials.Add( m.name );

					string assetPath = UnityEditor.AssetDatabase.GetAssetPath( m );
					Material mainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath( assetPath ) as Material;
					if ( !mainAsset )
					{
						Debug.LogError( "Error calling LoadMainAssetAtPath( " + assetPath + " )!\n\n" );
						continue;
					}

					i++;
					if ( UnityEditor.EditorUtility.DisplayCancelableProgressBar( "Valve Material Conversion Back to Unity Shaders", string.Format( "({0} of {1}) {2}", i, nCount, assetPath ), ( float )i / ( float )nCount ) )
					{
						bCanceled = true;
						break;
					}

					if ( ValveToStandardSingleMaterial( mainAsset, destShaderStandard, destShaderStandardSpecular ) )
					{
						nNumMaterialsConverted++;
					}

					SaveAssetsAndFreeMemory();
				}

				if ( bCanceled )
					break;
			}
		}

		Debug.Log( "Converted " + nNumMaterialsConverted + " materials to Unity Shaders\n\n" + Time.realtimeSinceStartup );

		UnityEditor.EditorUtility.ClearProgressBar();
	}

	[UnityEditor.MenuItem( "Valve/Shader Dev/Convert Active Materials Back to Unity Shaders", false, 51 )]
	private static void ValveToStandardCurrent()
	{
		ValveToStandard( false );
	}

	[UnityEditor.MenuItem( "Valve/Shader Dev/Convert All Materials Back to Unity Shaders", false, 101 )]
	private static void ValveToStandardAll()
	{
		ValveToStandard( true );
	}

	//---------------------------------------------------------------------------------------------------------------------------------------------------
	//[UnityEditor.MenuItem( "Valve/Shader Dev/Refresh Standard", false, 150 )]
	//private static void RefreshStandard()
	//{
	//	RenameShadersInAllMaterials( "Standard", "Standard" );
	//}

	//---------------------------------------------------------------------------------------------------------------------------------------------------
	[UnityEditor.MenuItem( "Valve/Shader Dev/Ensure Consistency in Valve Materials", false, 200 )]
	private static void EnsureConsistencyInValveMaterials()
	{
		int nNumMaterialChanges = 0;
		Debug.Log( "Begin consistency check for all Valve materials\n\n" + Time.realtimeSinceStartup );

		int i = 0;
		int nCount = 0;
		foreach ( string s in UnityEditor.AssetDatabase.GetAllAssetPaths() )
		{
			if ( s.EndsWith( ".mat", StringComparison.OrdinalIgnoreCase ) )
			{
				nCount++;
			}
		}

		foreach ( string s in UnityEditor.AssetDatabase.GetAllAssetPaths() )
		{
			if ( s.EndsWith( ".mat", StringComparison.OrdinalIgnoreCase ) )
			{
				i++;
				if ( UnityEditor.EditorUtility.DisplayCancelableProgressBar( "Consistency check for all Valve materials", string.Format( "({0} of {1}) {2}", i, nCount, s ), ( float )i / ( float )nCount ) )
				{
					break;
				}

				Material m = UnityEditor.AssetDatabase.LoadMainAssetAtPath( s ) as Material;
				if ( m.shader.name.Equals( "Valve/vr_standard" ) )
				{
					// Convert old metallic bool to new specular mode
					if ( ( m.HasProperty( "g_bEnableMetallic" ) && m.GetInt( "g_bEnableMetallic" ) == 1 ) || ( m.IsKeywordEnabled( "_METALLIC_ENABLED" ) ) )
					{
						Debug.Log( "     Converting old metallic checkbox to specular mode on material \"" + m.name + "\"\n" );
						m.DisableKeyword( "_METALLIC_ENABLED" );
						m.SetInt( "g_bEnableMetallic", 0 );
						m.SetInt( "_SpecularMode", 2 );
						m.EnableKeyword( "S_SPECULAR_METALLIC" );
						nNumMaterialChanges++;
					}
					else if ( !m.IsKeywordEnabled( "S_SPECULAR_NONE" ) && !m.IsKeywordEnabled( "S_SPECULAR_BLINNPHONG" ) && !m.IsKeywordEnabled( "S_SPECULAR_METALLIC" ) )
					{
						Debug.Log( "     Converting old specular to BlinnPhong specular mode on material \"" + m.name + "\"\n" );
						m.SetInt( "_SpecularMode", 1 );
						m.EnableKeyword( "S_SPECULAR_BLINNPHONG" );
						nNumMaterialChanges++;
					}

					// If occlusion map is set, enable S_OCCLUSION static combo
					if ( m.GetTexture( "_OcclusionMap" ) && !m.IsKeywordEnabled( "S_OCCLUSION" ) )
					{
						Debug.Log( "     Enabling new occlusion combo S_OCCLUSION on material \"" + m.name + "\"\n" );
						m.EnableKeyword( "S_OCCLUSION" );
						nNumMaterialChanges++;
					}

					SaveAssetsAndFreeMemory();
				}
			}
		}

		Debug.Log( "Consistency check made " + nNumMaterialChanges + " changes to Valve materials\n\n" + Time.realtimeSinceStartup );

		UnityEditor.EditorUtility.ClearProgressBar();
	}
}

#endif
