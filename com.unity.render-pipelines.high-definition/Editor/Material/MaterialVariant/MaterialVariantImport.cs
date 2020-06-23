using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.Assets.MaterialVariant.Editor
{
    [ScriptedImporter(1, ".matVariant")] // importQueueOffset must be higher than ShaderGraphImporter value
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

                    // Reinit Material
                    if (matVariant.isShader)
                    {
                        rootPath = AssetDatabase.GUIDToAssetPath(matVariant.rootGUID);
                        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(rootPath);
                        material = new Material(shader);
                    }
                    else
                    {
                        rootPath = AssetDatabase.GUIDToAssetPath(matVariant.rootGUID);
                        Material mat = AssetDatabase.LoadAssetAtPath<Material>(rootPath);
                        material = new Material(mat);
                    }

                    // Build dependency chain
                    ctx.DependsOnSourceAsset(matVariant.rootGUID);

                    // Apply change again
                    // TODO

                    // Setup as main replacement object
                    ctx.AddObjectToAsset("Material", material);
                    ctx.SetMainObject(material);
                }
            }
        }
    }
}
