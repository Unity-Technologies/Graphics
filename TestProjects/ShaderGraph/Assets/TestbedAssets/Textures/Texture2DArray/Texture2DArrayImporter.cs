using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

[ScriptedImporter(0, "texture2darray")]
[Serializable]
class Texture2DArrayImporter : ScriptedImporter
{
    [SerializeField] private Texture2D[] m_Textures = null;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        if (m_Textures == null || m_Textures.Length < 1)
        {
            return;
        }

        var first = m_Textures[0];
        var textureArray = new Texture2DArray(first.width, first.height, m_Textures.Length, TextureFormat.ARGB32, false);
        for (var arrayElement = 0; arrayElement < textureArray.depth; arrayElement++)
        {
            textureArray.SetPixels(m_Textures[arrayElement].GetPixels(), arrayElement);
        }

        ctx.AddObjectToAsset("MainObject", textureArray);
        ctx.SetMainObject(textureArray);
    }

    [MenuItem("Assets/Create/Texture 2D Array")]
    static void CreateAsset()
    {
        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<EditAction>(), "New Texture 2D Array.texture2darray", null, null);
    }

    class EditAction : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            File.WriteAllText(pathName, "");
            AssetDatabase.Refresh();
        }
    }
}
