using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.Assets.MaterialVariant.Editor
{
    [ScriptedImporter(1, ".matVariant", 5)] // importQueueOffset must be higher than ShaderGraphImporter value
    public class MaterialVariantImporter : ScriptedImporter
    {
        protected Material GetMaterialFromRoot(AssetImportContext ctx, string rootPath)
        {
            // As only a write of a file trigger the OnImportAsset we need to add
            // all member of the hierarchy as a dependency so we can detect changes
            // (Change of shader graph write a file, change of Material change a file, change of a MaterialVariant write a file)
            // If we were only adding dependency on the Parent we will not catch the change of a GrandParent as it will only
            // trigger OnImportAsset for the Parent but not propagate it for child (as it don't write on disk)
            ctx.DependsOnSourceAsset(rootPath);

            // When we call LoadAssetAtPath on a MaterialVariant or a ShaderGraph it return respectively a Material or a Shader
            // because they are set as MainObject in their respective importer.
            // For a Material or a Shader it return what you expect
            Object subAsset = AssetDatabase.LoadAssetAtPath<Object>(rootPath);

            // We need to know if we are a MaterialVariant so we can dig into the hierarchy
            // When a Root (Material or Shader) write on disk, it invalidate all the child that depends on it (i.e the on the fly
            // Material created is invalided, so LoadAssetAtPath will return null as we haven't yet call OnImportAsset.
            // We can't store MaterialVariant as a subasset (i.e AddObjectToAsset) as OnImportAsset may not be call before we try to
            // access it.
            // In order to work around this dependency order issue we check if subAsset is null (Which must mean that a MaterialVariant
            // have been invalidated). Otherwise if OnImportAsset have been call correctly, then (importer is MaterialVariantImporter) will be true.
            var importer = AssetImporter.GetAtPath(rootPath);
            if (subAsset == null || importer is MaterialVariantImporter)
            {
                // We use LoadSerializedFileAndForget to load the MaterialVariant, any other asset database function will try to Load
                // the Material as it is setup as a MainObject and thus will return null. Even if you used LoadAllAssetsAtPath (it still return 0 asset).
                var assets = InternalEditorUtility.LoadSerializedFileAndForget(rootPath);
                subAsset = assets[0]; // Here we assume we are a MaterialVariant - Don't know yet if there is failure possible by doing this.
            }

            Material material = null;

            if (subAsset is MaterialVariant)
            {
                MaterialVariant rootMatVariant = subAsset as MaterialVariant;
                material = GetMaterialFromRoot(ctx, AssetDatabase.GUIDToAssetPath(rootMatVariant.rootGUID));

                // Apply root modification
                MaterialPropertyModification.ApplyPropertyModificationsToMaterial(material, rootMatVariant.overrides);
            }
            else if (subAsset is Shader)
            {
                Shader rootShader = subAsset as Shader;
                material = new Material(rootShader);
            }
            else if (subAsset is Material)
            {
                Material rootMaterial = subAsset as Material;
                material = new Material(rootMaterial);
            }

            return material;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var assets = InternalEditorUtility.LoadSerializedFileAndForget(ctx.assetPath);
            if (assets != null && assets.Length == 1)
            {
                var matVariant = assets[0] as MaterialVariant;
                if (matVariant != null)
                {
                    Material material = GetMaterialFromRoot(ctx, AssetDatabase.GUIDToAssetPath(matVariant.rootGUID));

                    // Apply local modification
                    MaterialPropertyModification.ApplyPropertyModificationsToMaterial(material, matVariant.overrides);

                    // We need to update keyword now that everything is override properly
                    UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(material);

                    // Keep trace of variant in order to register any override.
                    matVariant.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild | HideFlags.HideInInspector;
                    ctx.AddObjectToAsset("Variant", matVariant); // This allows finding it in "GetMaterialVariantFromAssetPath"

                    ctx.AddObjectToAsset("Material", material);
                    ctx.SetMainObject(material);
                }
            }
        }

        public static MaterialVariant GetMaterialVariantFromAssetPath(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<MaterialVariant>().FirstOrDefault();
        }

        public static MaterialVariant GetMaterialVariantFromGUID(string GUID)
        {
            return GetMaterialVariantFromAssetPath(AssetDatabase.GUIDToAssetPath(GUID));
        }
    }
}
