using System;
using UnityEditor;
using UnityEngine;
using System.IO;

public class NormalMapAverageLengthTexturePostprocessor : AssetPostprocessor
{
    // This class will process a normal map and add the value of average normal length to the blue or alpha channel
    // The texture is save as BC7.
    // Tangent space normal map: BC7 RGB (normal xy - average normal length)
    // Object space normal map: BC7 RGBA (normal xyz - average normal length)
    static string s_Suffix = "_NA";
    //static string s_SuffixOS = "_OSNA"; // Suffix for object space case - TODO

    void OnPreprocessTexture()
    {
        // Any texture with _NA suffix will store average normal lenght in alpha
        if (Path.GetFileNameWithoutExtension(assetPath).EndsWith(s_Suffix, StringComparison.InvariantCultureIgnoreCase))
        {
            // Make sure we don't convert as a normal map.
            TextureImporter textureImporter = (TextureImporter)assetImporter;
            textureImporter.convertToNormalmap = false;
            textureImporter.alphaSource = TextureImporterAlphaSource.None;
            textureImporter.mipmapEnabled = true;
            textureImporter.textureCompression = TextureImporterCompression.CompressedHQ; // This is BC7 for Mac/PC

#pragma warning disable 618 // remove obsolete warning for this one
            textureImporter.linearTexture = true; // Says deprecated but won't work without it.
#pragma warning restore 618
            textureImporter.sRGBTexture = false;  // But we're setting the new property just in case it changes later...
        }
    }

    private static Color GetColor(Color[] source, int x, int y, int width, int height)
    {
        x = (x + width) % width;
        y = (y + height) % height;

        int index = y * width + x;
        var c = source[index];

        return c;
    }

    private static Vector3 GetNormal(Color[] source, int x, int y, int width, int height)
    {
        Vector3 n = (Vector4)GetColor(source, x, y, width, height);
        n = 2.0f * n - Vector3.one;
        n.Normalize();

        return n;
    }

    private static Vector3 GetAverageNormal(Color[] source, int x, int y, int width, int height, int texelFootprint)
    {
        Vector3 averageNormal = new Vector3(0, 0, 0);

        // Calculate the average color over the texel footprint.
        for (int i = 0; i < texelFootprint; ++i)
        {
            for (int j = 0; j < texelFootprint; ++j)
            {
                averageNormal += GetNormal(source, x + i, y + j, width, height);
            }
        }

        averageNormal /= (texelFootprint * texelFootprint);

        return averageNormal;
    }

    void OnPostprocessTexture(Texture2D texture)
    {
        if (Path.GetFileNameWithoutExtension(assetPath).EndsWith(s_Suffix, StringComparison.InvariantCultureIgnoreCase))
        {
            // Based on The Order : 1886 SIGGRAPH course notes implementation. Sample all normal map
            // texels from the base mip level that are within the footprint of the current mipmap texel.
            Color[] source = texture.GetPixels(0);
            for (int m = 1; m < texture.mipmapCount; m++)
            {
                Color[] c = texture.GetPixels(m);

                int mipWidth = Math.Max(1, texture.width >> m);
                int mipHeight = Math.Max(1, texture.height >> m);

                for (int x = 0; x < mipWidth; ++x)
                {
                    for (int y = 0; y < mipHeight; ++y)
                    {
                        int texelFootprint = 1 << m;
                        Vector3 averageNormal = GetAverageNormal(source, x * texelFootprint, y * texelFootprint,
                                texture.width, texture.height, texelFootprint);

                        // Store the normal length for the average normal.
                        int outputPosition = y * mipWidth + x;

                        // Clamp to avoid any issue (TODO: Check this)
                        // Write into the blue channel
                        float averageNormalLength = Math.Max(0.0f, Math.Min(1.0f, averageNormal.magnitude));

                        c[outputPosition].b = averageNormalLength;
                        c[outputPosition].a = 1.0f;
                    }
                }

                texture.SetPixels(c, m);
            }

            // Now overwrite the first mip average normal channel - order is important as above we read the mip0
            // For mip 0, set the normal length to 1.
            {
                Color[] c = texture.GetPixels(0);
                for (int i = 0; i < c.Length; i++)
                {
                    c[i].b = 1.0f;
                    c[i].a = 1.0f;
                }
                texture.SetPixels(c, 0);
            }
        }
    }
}
