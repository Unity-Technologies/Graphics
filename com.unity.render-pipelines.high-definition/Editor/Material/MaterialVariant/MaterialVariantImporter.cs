using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.Assets.MaterialVariant.Editor
{
    [ScriptedImporter(6, ".matVariant", 5)] // importQueueOffset must be higher than ShaderGraphImporter value
    public class MaterialVariantImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var assets = InternalEditorUtility.LoadSerializedFileAndForget(ctx.assetPath);
            if (assets != null && assets.Length == 1)
            {
                var matVariant = assets[0] as MaterialVariant;
                if (matVariant != null)
                {
                    string rootPath = null;
                    Material material = null;

                    // Reinit Material + Build dependency chain
                    if (matVariant.isShader)
                    {
                        rootPath = AssetDatabase.GUIDToAssetPath(matVariant.rootGUID);
                        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(rootPath);
                        material = new Material(shader);
                        ctx.DependsOnSourceAsset(AssetDatabase.GetAssetPath(shader));
                    }
                    else
                    {
                        rootPath = AssetDatabase.GUIDToAssetPath(matVariant.rootGUID);
                        Material mat = AssetDatabase.LoadAssetAtPath<Material>(rootPath);
                        mat = AssetDatabase.LoadAssetAtPath<Material>(rootPath);
                        material = new Material(mat);
                        ctx.DependsOnSourceAsset(AssetDatabase.GetAssetPath(mat));
                    }

                    // Setup as main replacement object
                    ctx.AddObjectToAsset("Material", material);
                    ctx.SetMainObject(material);

                    int hash = material.ComputeCRC();

                    // Force a save of this MaterialVariant to force an update of the asset database
                    // and propagate to children
                    if (matVariant.hash != hash)
                    {
                        matVariant.hash = hash;
                        //InternalEditorUtility.SaveToSerializedFileAndForget(assets, ctx.assetPath, true);
                    }
                    else
                    {
                        // Apply change again
                        // TODO

                    }                   
                }
            }
        }
    }
}
