using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace UnityEditor.Rendering
{
    internal class TextureCombiner
    {
        static Texture2D _midGrey;

        /// <summary>
        /// Returns a 1 by 1 mid grey (0.5, 0.5, 0.5, 1) Texture.
        /// </summary>
        public static Texture2D midGrey
        {
            get
            {
                if (_midGrey == null)
                    _midGrey = TextureFromColor(Color.grey);

                return _midGrey;
            }
        }

        private static Dictionary<Color, Texture2D> singleColorTextures = new Dictionary<Color, Texture2D>();

        /// <summary>
        /// Returns a 1 by 1 Texture that is the color that you pass in.
        /// </summary>
        /// <param name="color">The color that Unity uses to create the Texture.</param>
        /// <returns></returns>
        public static Texture2D TextureFromColor(Color color)
        {
            if (color == Color.white) return Texture2D.whiteTexture;
            if (color == Color.black) return Texture2D.blackTexture;

            bool makeTexture = !singleColorTextures.ContainsKey(color);
            if (!makeTexture)
                makeTexture = (singleColorTextures[color] == null);

            if (makeTexture)
            {
                Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false, true);
                tex.SetPixel(0, 0, color);
                tex.Apply();

                singleColorTextures[color] = tex;
            }

            return singleColorTextures[color];
        }

        /// <summary>
        /// Returns the Texture assigned to the property "propertyName" of "srcMaterial".
        /// If no matching property is found, or no Texture is assigned, returns a 1 by 1 Texture of "fallback" color.
        /// </summary>
        /// <param name="srcMaterial">The Material to get the Texture from.</param>
        /// <param name="propertyName">The name of the Texture property.</param>
        /// <param name="fallback">The fallback color that Unity uses to create a Texture if it could not find the Texture property on the Material.</param>
        /// <returns></returns>
        public static Texture GetTextureSafe(Material srcMaterial, string propertyName, Color fallback)
        {
            return GetTextureSafe(srcMaterial, propertyName, TextureFromColor(fallback));
        }

        /// <summary>
        /// Returns the Texture assigned to the property "propertyName" of "srcMaterial".
        /// If no matching property is found, or no Texture is assigned, returns the "fallback" Texture.
        /// </summary>
        /// <param name="srcMaterial">The Material to get the Texture from.</param>
        /// <param name="propertyName">The name of the Texture property.</param>
        /// <param name="fallback">The fallback color that Unity uses to create a Texture if it could not find the Texture property on the Material.</param>
        /// <returns></returns>
        public static Texture GetTextureSafe(Material srcMaterial, string propertyName, Texture fallback)
        {
            if (!srcMaterial.HasProperty(propertyName))
                return fallback;

            Texture tex = srcMaterial.GetTexture(propertyName);
            if (tex == null)
                return fallback;
            else
                return tex;
        }

        private static TextureFormat[] TextureFormatsWithouthAlpha =
        {
            TextureFormat.BC4,
            TextureFormat.BC5,
            TextureFormat.DXT1,
            TextureFormat.DXT1Crunched,
            TextureFormat.EAC_R,
            TextureFormat.EAC_R_SIGNED,
            TextureFormat.EAC_RG,
            TextureFormat.EAC_RG_SIGNED,
            TextureFormat.ETC2_RGB,
            TextureFormat.ETC_RGB4,
            TextureFormat.ETC_RGB4Crunched,
            TextureFormat.PVRTC_RGB2,
            TextureFormat.PVRTC_RGB4,
            TextureFormat.R16,
            TextureFormat.R8,
            TextureFormat.RFloat,
            TextureFormat.RG16,
            TextureFormat.RGB24,
            TextureFormat.RGB565,
            TextureFormat.RGB9e5Float,
            TextureFormat.RGFloat,
            TextureFormat.RGHalf,
            TextureFormat.RHalf,
            TextureFormat.YUY2
        };

        /// <summary>
        /// Specifies whether the Texture has an alpha channel or not. Returns true if it does and false otherwise.
        /// </summary>
        /// <param name="tex">The Texture for this function to check.</param>
        /// <returns></returns>
        public static bool TextureHasAlpha(Texture2D tex)
        {
            if (tex == null) return false;

            bool o = true;
            int i = 0;

            while (i < TextureFormatsWithouthAlpha.Length && o)
            {
                o = tex.format != TextureFormatsWithouthAlpha[i];
                ++i;
            }

            return o;
        }

        private Texture m_rSource;
        private Texture m_gSource;
        private Texture m_bSource;
        private Texture m_aSource;

        // Chanels are : r=0, g=1, b=2, a=3, greyscale from rgb = 4
        // If negative, the chanel is inverted
        private int m_rChanel;
        private int m_gChanel;
        private int m_bChanel;
        private int m_aChanel;

        // Chanels remaping
        private Vector4[] m_remapings =
        {
            new Vector4(0f, 1f, 0f, 0f),
            new Vector4(0f, 1f, 0f, 0f),
            new Vector4(0f, 1f, 0f, 0f),
            new Vector4(0f, 1f, 0f, 0f)
        };

        private bool m_bilinearFilter;

        private Dictionary<Texture, Texture> m_RawTextures;

        /// <summary>
        /// Creates a TextureCombiner object.
        /// </summary>
        /// <param name="rSource">Source Texture for the RED output.</param>
        /// <param name="rChanel">Channel index to use for the RED output.</param>
        /// <param name="gSource">Source Texture for the GREEN output.</param>
        /// <param name="gChanel">Channel index to use for the GREEN output.</param>
        /// <param name="bSource">Source Texture for the BLUE output.</param>
        /// <param name="bChanel">Channel index to use for the BLUE output.</param>
        /// <param name="aSource">Source Texture for the ALPHA output.</param>
        /// <param name="aChanel">Channel index to use for the ALPHA output.</param>
        /// <param name="bilinearFilter">Use bilinear filtering when combining (default = true).</param>
        public TextureCombiner(Texture rSource, int rChanel, Texture gSource, int gChanel, Texture bSource, int bChanel, Texture aSource, int aChanel, bool bilinearFilter = true)
        {
            m_rSource = rSource;
            m_gSource = gSource;
            m_bSource = bSource;
            m_aSource = aSource;
            m_rChanel = rChanel;
            m_gChanel = gChanel;
            m_bChanel = bChanel;
            m_aChanel = aChanel;
            m_bilinearFilter = bilinearFilter;
        }

        /// <summary>
        /// Set the remapping of a specific color channel.
        /// </summary>
        /// <param name="channel">Target color channel (Red:0, Green:1, Blue:2, Alpha:3).</param>
        /// <param name="min">Minimum input value mapped to 0 in output.</param>
        /// <param name="max">Maximum input value mapped to 1 in output.</param>
        public void SetRemapping(int channel, float min, float max)
        {
            if (channel > 3 || channel < 0) return;

            m_remapings[channel].x = min;
            m_remapings[channel].y = max;
        }

        /// <summary>
        /// Process the TextureCombiner.
        /// Unity creates the Texture Asset at the "savePath", and returns the Texture object.
        /// </summary>
        /// <param name="savePath">The path to save the Texture Asset to, relative to the Project folder.</param>
        /// <returns></returns>
        public Texture2D Combine(string savePath)
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

            combinerMaterial.SetVector("_RRemap", m_remapings[0]);
            combinerMaterial.SetVector("_GRemap", m_remapings[1]);
            combinerMaterial.SetVector("_BRemap", m_remapings[2]);
            combinerMaterial.SetVector("_ARemap", m_remapings[3]);

            RenderTexture combinedRT = new RenderTexture(xMin, yMin, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);

            Graphics.Blit(Texture2D.whiteTexture, combinedRT, combinerMaterial);

            // Readback the render texture
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = combinedRT;
            combined.ReadPixels(new Rect(0, 0, xMin, yMin), 0, 0, false);
            combined.Apply();
            RenderTexture.active = previousActive;

            byte[] bytes = new byte[0];

            if (savePath.EndsWith("png"))
                bytes = ImageConversion.EncodeToPNG(combined);
            if (savePath.EndsWith("exr"))
                bytes = ImageConversion.EncodeToEXR(combined);
            if (savePath.EndsWith("jpg"))
                bytes = ImageConversion.EncodeToJPG(combined);

            string systemPath = Path.Combine(Application.dataPath.Remove(Application.dataPath.Length - 6), savePath);
            File.WriteAllBytes(systemPath, bytes);

            Object.DestroyImmediate(combined);

            AssetDatabase.ImportAsset(savePath);

            TextureImporter combinedImporter = (TextureImporter)AssetImporter.GetAtPath(savePath);
            combinedImporter.sRGBTexture = false;
            combinedImporter.SaveAndReimport();

            if (savePath.EndsWith("exr"))
            {
                // The options for the platform string are: "Standalone", "iPhone", "Android", "WebGL", "Windows Store Apps", "PSP2", "PS4", "XboxOne", "Nintendo 3DS", "WiiU", "tvOS".
                combinedImporter.SetPlatformTextureSettings(new TextureImporterPlatformSettings() { name = "Standalone", format = TextureImporterFormat.DXT5, overridden = true });
            }

            combined = AssetDatabase.LoadAssetAtPath<Texture2D>(savePath);

            //cleanup "raw" textures
            foreach (KeyValuePair<Texture, Texture> prop in m_RawTextures)
            {
                if (prop.Key != prop.Value && AssetDatabase.Contains(prop.Value))
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(prop.Value));
            }

            Object.DestroyImmediate(combinerMaterial);

            m_RawTextures.Clear();

            return combined;
        }

        private Texture GetRawTexture(Texture original, bool sRGBFallback = false)
        {
            if (m_RawTextures == null) m_RawTextures = new Dictionary<Texture, Texture>();
            if (!m_RawTextures.ContainsKey(original))
            {
                string path = AssetDatabase.GetAssetPath(original);
                string rawPath = "Assets/raw_" + Path.GetFileName(path);
                bool isBuiltinResource = path.Contains("unity_builtin");

                if (!isBuiltinResource && AssetDatabase.Contains(original) && AssetDatabase.CopyAsset(path, rawPath))
                {
                    AssetDatabase.ImportAsset(rawPath);

                    TextureImporter rawImporter = (TextureImporter)AssetImporter.GetAtPath(rawPath);
                    rawImporter.textureType = TextureImporterType.Default;
                    rawImporter.mipmapEnabled = false;
                    rawImporter.isReadable = true;
                    rawImporter.filterMode = m_bilinearFilter ? FilterMode.Bilinear : FilterMode.Point;
                    rawImporter.npotScale = TextureImporterNPOTScale.None;
                    rawImporter.wrapMode = TextureWrapMode.Clamp;

                    Texture2D originalTex2D = original as Texture2D;
                    rawImporter.sRGBTexture = (originalTex2D == null) ? sRGBFallback : (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(original)) as TextureImporter).sRGBTexture;

                    rawImporter.maxTextureSize = 8192;

                    rawImporter.textureCompression = TextureImporterCompression.Uncompressed;

                    rawImporter.SaveAndReimport();

                    m_RawTextures.Add(original, AssetDatabase.LoadAssetAtPath<Texture>(rawPath));
                }
                else
                    m_RawTextures.Add(original, original);
            }

            return m_RawTextures[original];
        }
    }
}
