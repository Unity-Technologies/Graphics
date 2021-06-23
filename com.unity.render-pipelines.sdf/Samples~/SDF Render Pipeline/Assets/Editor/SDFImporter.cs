using UnityEngine;
using System.IO;

[UnityEditor.AssetImporters.ScriptedImporter(1, "sdf")]
public class SDFImporter : UnityEditor.AssetImporters.ScriptedImporter
{
    public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
    {
        GameObject sdfGameObject = new GameObject();

        SDFFilter sdfFilter = sdfGameObject.AddComponent<SDFFilter>();
        sdfFilter.InitializeFromFile(ctx.assetPath);

        SDFMaterial sdfMaterial = sdfGameObject.AddComponent<SDFMaterial>();
        SDFRenderer sdfRenderer = sdfGameObject.AddComponent<SDFRenderer>();

        sdfRenderer.SDFFilter = sdfFilter;
        sdfRenderer.SDFMaterial = sdfMaterial;

        sdfGameObject.name = sdfFilter.VoxelField.m_Description;

        ctx.AddObjectToAsset("My Main Asset", sdfGameObject);
        ctx.AddObjectToAsset("SDFFilter", sdfFilter);
        ctx.AddObjectToAsset("SDFVoxelField", sdfFilter.VoxelField);
        ctx.AddObjectToAsset("SDFMaterial", sdfMaterial);
        ctx.AddObjectToAsset("SDFRenderer", sdfRenderer);
        ctx.SetMainObject(sdfGameObject);
    }
}
