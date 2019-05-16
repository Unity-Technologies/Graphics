using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


//move to core Unity at some point, make sure the hash calculation is identical to GTSBuildInfoGenerator in the meantime

public class StackStatus 
{
    public static bool AllStacksValid(Material material)
    {
        var shader = material.shader;

        int propCount = ShaderUtil.GetPropertyCount(shader);
        for (int i = 0; i < propCount; i++)
        {
            if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.Stack)
            {
                string stackPropName = ShaderUtil.GetPropertyName(shader, i);
                VTStack vtStack = material.GetStack(stackPropName);

                if (vtStack != null)
                {
                    string[] textureProperties = ShaderUtil.GetStackTextureProperties(shader, stackPropName);

                    string hash = GetStackHash(textureProperties, material);

                    if (hash != vtStack.atlasName)
                        return false;

                }
                else
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static string GetStackHash(string[] textureProperties, Material material)
    {
        // fill in a (hashed) name
        string texturesHash = "";

        TextureWrapMode wrapMode = TextureWrapMode.Clamp;
        TextureImporterNPOTScale textureScaling = TextureImporterNPOTScale.None;

        bool firstTextureAdded = false;

        for (int j = 0; j < textureProperties.Length; j++)
        {
            string textureProperty = textureProperties[j];

            if (!material.HasProperty(textureProperty)) //todo is this a broken shader? Report to user?
                continue;

            Texture2D tex2D = material.GetTexture(textureProperty) as Texture2D;

            if (tex2D == null)
                continue;

            string path = AssetDatabase.GetAssetPath(tex2D);

            if (string.IsNullOrEmpty(path))
                continue;

            string hash = tex2D.imageContentsHash.ToString();

            if (hash == null)
            {
                continue;
            }

            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (textureImporter == null)
            {
                // probably a fallback texture
                continue;
            }

            texturesHash += hash;

            if (!firstTextureAdded)
            {
                wrapMode = tex2D.wrapMode;
                textureScaling = textureImporter.npotScale;
                firstTextureAdded = true;
            }
            else
            {
                UnityEngine.Debug.Log("Texture settings don't match on all layers of StackedTexture");
            }

        }

        String assetHash = "" + wrapMode + textureScaling;

        //todo: ignoring overall settings in hash for now
        //TileSetBuildSettings tileSetBuildSettings = new TileSetBuildSettings(ts);
        String settingsHash = ""; // tileSetBuildSettings.GetHashString();

        return ("version_1_" + texturesHash + assetHash + settingsHash).GetHashCode().ToString("X");
    }


}
