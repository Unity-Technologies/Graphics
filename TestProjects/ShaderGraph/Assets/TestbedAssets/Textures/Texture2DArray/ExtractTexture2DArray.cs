using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

static class ExtractTexture2DArray
{
    private const string AssetsExtractTextureDArray = "Assets/Extract Texture 2D Array";

    [MenuItem(AssetsExtractTextureDArray)]
    static void Execute()
    {
        var textureArray = (Texture2DArray) Selection.activeObject;
        var assetPath = AssetDatabase.GetAssetPath(textureArray);
        var basePath = Path.Combine(Path.GetDirectoryName(assetPath), Path.GetFileNameWithoutExtension(assetPath));
        for (var arrayElement = 0; arrayElement < textureArray.depth; arrayElement++)
        {
            var texture = new Texture2D(textureArray.width, textureArray.height, textureArray.format, false);
            texture.SetPixels(textureArray.GetPixels(arrayElement));
            var tgaBytes = texture.EncodeToTGA();
            var tgaPath = $"{basePath}{arrayElement}.tga";
            File.WriteAllBytes(tgaPath, tgaBytes);
        }
        AssetDatabase.Refresh();
    }

    [MenuItem(AssetsExtractTextureDArray, true)]
    static bool Validate()
    {
        return Selection.activeObject is Texture2DArray;
    }
}
