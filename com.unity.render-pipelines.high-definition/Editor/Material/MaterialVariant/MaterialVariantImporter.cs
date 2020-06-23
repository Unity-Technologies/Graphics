using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.Assets.MaterialVariant.Editor
{
    [ScriptedImporter(1, ".matVariant", 5)] // importQueueOffset must be higher than ShaderGraphImporter value
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
                        MaterialVariant matVar = AssetDatabase.LoadAssetAtPath<MaterialVariant>(rootPath);

                        Material mat = AssetDatabase.LoadAssetAtPath<Material>(rootPath);
                        material = new Material(mat);
                        ctx.DependsOnSourceAsset(AssetDatabase.GetAssetPath(mat));
                    }

                    // Apply change again
                    MaterialPropertyModification.ApplyPropertyModificationsToMaterial(material, matVariant.overrides);

                    // Setup as main replacement object
                    ctx.AddObjectToAsset("Material", material);
                    ctx.SetMainObject(material);
                }
            }
        }
    }
}
