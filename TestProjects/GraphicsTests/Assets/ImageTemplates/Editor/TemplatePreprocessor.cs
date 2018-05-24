using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Template preprocessor.
/// This script forces all textures in the folder 'ImageTemplates' to have no compression, meaning import times will be much faster, especially when switching platforms
/// </summary>

public class TemplatePreprocessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath.Contains("ImageTemplates"))
        {
            TextureImporter textureImporter  = (TextureImporter)assetImporter;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
