using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.ShaderGraph;

namespace Unity.Assets.MaterialVariant.Editor
{
    public class MaterialVariant : ScriptableObject
    {
        public string rootGUID;
        public bool isShader;

        public List<MaterialPropertyModification> overrides;
        
        public void TrimPreviousOverridesAndAdd(IEnumerable<MaterialPropertyModification> modifications)
        {
            foreach(var modification in modifications)
            {
                int pos = overrides.FindIndex(o => o.propertyPath == modification.propertyPath);
                if (pos > -1)
                    overrides[pos] = modification;
                else
                    overrides.Add(modification);
            }
        }

        private static bool IsValidRoot(Object root)
        {
            return EditorUtility.IsPersistent(root) && ((root is Material) || (root is Shader));
        }

        public static void CreateVariant(Object target)
        {
            var rootMaterial = target as Material;
            var rootShader = target as Shader;

            if (EditorUtility.IsPersistent(target) && (rootMaterial || rootShader))
            {
                var matVariant = ScriptableObject.CreateInstance<MaterialVariant>();

                if (rootShader)
                {
                    matVariant.isShader = true;

                    var path = AssetDatabase.GetAssetPath(rootShader);
                    var importer = AssetImporter.GetAtPath(path);

                    if (importer is ShaderGraphImporter)
                        matVariant.rootGUID = AssetDatabase.AssetPathToGUID(importer.assetPath);
                    else
                        matVariant.rootGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(rootShader));
                }
                else
                {
                    matVariant.isShader = false;

                    var path = AssetDatabase.GetAssetPath(rootMaterial);
                    var importer = AssetImporter.GetAtPath(path);

                    if (importer is MaterialVariantImporter)
                        matVariant.rootGUID = AssetDatabase.AssetPathToGUID(importer.assetPath);
                    else
                        matVariant.rootGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(rootMaterial));
                }

                matVariant.overrides = new List<MaterialPropertyModification>();

                var targetPath = AssetDatabase.GetAssetPath(target);
                targetPath = Path.Combine(Path.GetDirectoryName(targetPath),
                    Path.GetFileNameWithoutExtension(targetPath) + " Variant.matVariant");
                targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);

                InternalEditorUtility.SaveToSerializedFileAndForget(new[] { matVariant }, targetPath, true);
                AssetDatabase.ImportAsset(targetPath);
            }
        }
        
        /// <summary>
        /// Try to find an attached MaterialVariant for the given target
        /// Could work on Material and shadergraph
        /// Return null if nothing found
        /// </summary>
        public static MaterialVariant[] FindMaterialVariants(MaterialEditor materialEditor)
        {
            int targetCount = materialEditor.targets.Length;
            MaterialVariant[] result = new MaterialVariant [targetCount];

            bool atLeastOne = false;

            for(int i = 0; i < targetCount; ++i)
            {
                Object target = materialEditor.targets[i];
                var rootMaterial = target as Material;
                var rootShader = target as Shader;

                if (EditorUtility.IsPersistent(target) && (rootMaterial || rootShader))
                {
                    if (rootShader)
                    {
                        var path = AssetDatabase.GetAssetPath(rootShader);
                        var importer = AssetImporter.GetAtPath(path);

                        if (importer is ShaderGraphImporter)
                        {
                            result[i] = AssetDatabase.LoadAssetAtPath<MaterialVariant>(importer.assetPath);
                            atLeastOne = true;
                        }
                    }

                    if (rootMaterial)
                    {
                        var path = AssetDatabase.GetAssetPath(rootMaterial);
                        var importer = AssetImporter.GetAtPath(path);

                        if (importer is MaterialVariantImporter)
                        {
                            result[i] = AssetDatabase.LoadAssetAtPath<MaterialVariant>(importer.assetPath);
                            atLeastOne = true;
                        }
                    }
                }
            }

            return atLeastOne ? result : null;
        }
        
        private const string MENU_ITEM_PATH = "Assets/Create/Variants/Material Variant";

        [MenuItem(MENU_ITEM_PATH, false)]
        private static void CreateMaterialVariantMenu()
        {
            CreateVariant(Selection.activeObject);
        }

        [MenuItem(MENU_ITEM_PATH, true)]
        private static bool ValidateMaterialVariantMenu()
        {
            return IsValidRoot(Selection.activeObject);
        }
    }
}
