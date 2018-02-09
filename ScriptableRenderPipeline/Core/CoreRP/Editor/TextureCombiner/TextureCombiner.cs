using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureCombiner
{
	public static Texture2D _midGrey;
	public static Texture2D midGrey
	{
		get
		{
			if (_midGrey == null)
			{
				_midGrey = new Texture2D(4, 4, TextureFormat.ARGB32, false, false);
				_midGrey.SetPixels(new Color[] {
					Color.gray, Color.gray, Color.gray, Color.gray,
					Color.gray, Color.gray, Color.gray, Color.gray,
					Color.gray, Color.gray, Color.gray, Color.gray,
					Color.gray, Color.gray, Color.gray, Color.gray
				});
				_midGrey.Apply();
			}

			return _midGrey;
		}
	}

	public static Texture GetTextureSafe( Material srcMaterial, string propertyName, int fallback = 0)
	{
		Texture tex = srcMaterial.GetTexture(propertyName);
		if (tex == null)
		{
			switch(fallback)
			{
				case 0: tex = Texture2D.whiteTexture; break;
				case 1: tex = Texture2D.blackTexture; break;
				case 2: tex = TextureCombiner.midGrey; break;
			}
		}

		return tex;
	}

	private Texture m_rSource;
	private Texture m_gSource;
	private Texture m_bSource;
	private Texture m_aSource;

	// Chanels are : r=0, g=1, b=2, a=3, greyscale from rgb = 4
	private int m_rChanel;
	private int m_gChanel;
	private int m_bChanel;
	private int m_aChanel;

	private bool m_bilinearFilter;

	private Dictionary<Texture, Texture> m_RawTextures;

	public TextureCombiner( Texture rSource, int rChanel, Texture gSource, int gChanel, Texture bSource, int bChanel, Texture aSource, int achanel, bool bilinearFilter = true )
	{
		m_rSource = rSource;
		m_gSource = gSource;
		m_bSource = bSource;
		m_aSource = aSource;
		m_rChanel = rChanel;
		m_gChanel = gChanel;
		m_bChanel = bChanel;
		m_aChanel = achanel;
		m_bilinearFilter = bilinearFilter;
	}

	public Texture2D Combine( string savePath )
	{
		int xMin = int.MaxValue;
		int yMin = int.MaxValue;

		if (m_rSource.width > 4 && m_rSource.width < xMin) xMin = m_rSource.width;
		if (m_gSource.width > 4 && m_gSource.width < xMin) xMin = m_gSource.width;
		if (m_bSource.width > 4 && m_bSource.width < xMin) xMin = m_bSource.width;
		if (m_aSource.width > 4 && m_aSource.width < xMin) xMin = m_aSource.width;
		if (xMin == int.MaxValue) xMin = 4;
		
		if (m_rSource.height > 4 && m_rSource.height < yMin) yMin = m_rSource.height;
		if (m_gSource.height > 4 && m_gSource.height < yMin) yMin = m_gSource.height;
		if (m_bSource.height > 4 && m_bSource.height < yMin) yMin = m_bSource.height;
		if (m_aSource.height > 4 && m_aSource.height < yMin) yMin = m_aSource.height;
		if (yMin == int.MaxValue) yMin = 4;

		Texture2D combined = new Texture2D(xMin, yMin, TextureFormat.RGBAFloat, true, true);
		combined.hideFlags = HideFlags.DontUnloadUnusedAsset;

		Material combinerMaterial = new Material(Shader.Find("Hidden/SRP_Core/TextureCombiner"));
		combinerMaterial.hideFlags = HideFlags.DontUnloadUnusedAsset;

		combinerMaterial.SetTexture("_RSource", GetRawTexture(m_rSource));
		combinerMaterial.SetTexture("_GSource", GetRawTexture(m_gSource));
		combinerMaterial.SetTexture("_BSource", GetRawTexture(m_bSource));
		combinerMaterial.SetTexture("_ASource", GetRawTexture(m_aSource));

		combinerMaterial.SetFloat("_RChannel", m_rChanel);
		combinerMaterial.SetFloat("_GChannel", m_gChanel);
		combinerMaterial.SetFloat("_BChannel", m_bChanel);
		combinerMaterial.SetFloat("_AChannel", m_aChanel);

		RenderTexture combinedRT =  new RenderTexture(xMin, yMin, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

		Graphics.Blit(Texture2D.whiteTexture, combinedRT, combinerMaterial);

		// Readback the render texture
		RenderTexture previousActive = RenderTexture.active;
		RenderTexture.active = combinedRT;
		combined.ReadPixels(new Rect(0, 0, xMin, yMin), 0, 0, false);
		combined.Apply();
		RenderTexture.active = previousActive;

		byte[] bytes = ImageConversion.EncodeToEXR(combined);

		string systemPath = Path.Combine(Application.dataPath.Remove(Application.dataPath.Length-6), savePath);
		File.WriteAllBytes(systemPath, bytes);

		Object.DestroyImmediate(combined);

		AssetDatabase.ImportAsset(savePath);

		TextureImporter combinedImporter = (TextureImporter) AssetImporter.GetAtPath(savePath);
		combinedImporter.sRGBTexture = false;
		combinedImporter.SaveAndReimport();

		combined = AssetDatabase.LoadAssetAtPath<Texture2D>(savePath);

		//cleanup "raw" textures
		foreach( KeyValuePair<Texture, Texture> prop in m_RawTextures )
		{
			if (AssetDatabase.Contains(prop.Value))
				AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(prop.Value));
		}
		Object.DestroyImmediate(combinerMaterial);

		m_RawTextures.Clear();

		return combined;
	}

	private Texture GetRawTexture (Texture original)
	{
		if (m_RawTextures == null) m_RawTextures = new Dictionary<Texture, Texture>();
		if (!m_RawTextures.ContainsKey(original))
		{
			if ( AssetDatabase.Contains(original))
			{
				string path = AssetDatabase.GetAssetPath(original);
				string rawPath = "Assets/raw_"+Path.GetFileName(path);

				AssetDatabase.CopyAsset(path, rawPath);

				AssetDatabase.ImportAsset(rawPath);

				Debug.Log("Import raw texture: "+rawPath);

				TextureImporter rawImporter = (TextureImporter) TextureImporter.GetAtPath(rawPath);
				rawImporter.mipmapEnabled = false;
				rawImporter.isReadable = true;
				rawImporter.filterMode = m_bilinearFilter? FilterMode.Bilinear : FilterMode.Point;
				rawImporter.npotScale = TextureImporterNPOTScale.None;
				rawImporter.wrapMode = TextureWrapMode.Clamp;

				rawImporter.textureCompression = TextureImporterCompression.Uncompressed;
				rawImporter.textureType = TextureImporterType.Default;
				
				rawImporter.SaveAndReimport();

				m_RawTextures.Add(original, AssetDatabase.LoadAssetAtPath<Texture>(rawPath));
			}
			else
				m_RawTextures.Add(original, original);
		}

		return m_RawTextures[original];
	}
}
