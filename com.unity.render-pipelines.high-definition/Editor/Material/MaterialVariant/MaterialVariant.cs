using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.ShaderGraph;

namespace Unity.Assets.MaterialVariant.Editor
{
    public class MaterialVariant : ScriptableObject
    {
        public string rootGUID;
        public List<MaterialPropertyModification> overrides = new List<MaterialPropertyModification>();

        public Object GetParent()
        {
            string parentPath = AssetDatabase.GUIDToAssetPath(rootGUID);
            Object parentAsset = AssetDatabase.LoadAssetAtPath<Object>(parentPath);

            // parentAsset is either a Shader (for Shader or ShaderGraph) or a Material (for Material or MaterialVariant)
            // If a MaterialVariant, we're interested in it, not in the Material it generates
            if (parentAsset is Material)
            {
                var importer = AssetImporter.GetAtPath(parentPath);
                if (importer is MaterialVariantImporter)
                {
                    parentAsset = MaterialVariantImporter.GetMaterialVariantFromAssetPath(parentPath);
                }
            }

            return parentAsset;
        }
        
        #region MaterialVariant Overrides Management
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

        private bool IsSameProperty(MaterialPropertyModification modification, string propertyName)
        {
            string modificationPropertyName = modification.propertyPath.Split('.')[0];
            return propertyName == modificationPropertyName;
        }

        public bool IsOverriddenProperty(MaterialProperty property)
        {
            return overrides.Any(modification => IsSameProperty(modification, property.name));
        }

        public bool IsOverriddenPropertyForNonMaterialProperty(string propertyName)
        {
            propertyName = $"::{propertyName}:";
            return overrides.Any(modification => modification.propertyPath.StartsWith(propertyName));
        }

        public void ResetOverride(MaterialProperty property)
        {
            overrides.RemoveAll(modification => IsSameProperty(modification, property.name));
        }
        
        public void ResetOverrideForNonMaterialProperty(string propertyName)
        {
            propertyName = "::" + propertyName;
            overrides.RemoveAll(modification => IsSameProperty(modification, propertyName));
        }
        #endregion

        #region MaterialVariant Create Menu
        private const string MATERIAL_VARIANT_MENU_PATH = "Assets/Create/Variants/Material Variant";

        [MenuItem(MATERIAL_VARIANT_MENU_PATH, true)]
        private static bool ValidateMaterialVariantMenu()
        {
            return IsValidRoot(Selection.activeObject);
        }

        [MenuItem(MATERIAL_VARIANT_MENU_PATH, false)]
        private static void CreateMaterialVariantMenu()
        {
            CreateVariant(Selection.activeObject);
        }

        private static bool IsValidRoot(Object root)
        {
            return EditorUtility.IsPersistent(root) && ((root is Material) || (root is Shader));
        }

        public static void CreateVariant(Object target)
        {
            if (IsValidRoot(target))
            {
                var targetPath = AssetDatabase.GetAssetPath(target);

                var matVariant = ScriptableObject.CreateInstance<MaterialVariant>();
                matVariant.rootGUID = AssetDatabase.AssetPathToGUID(targetPath);

                var variantPath = Path.Combine(Path.GetDirectoryName(targetPath),
                    Path.GetFileNameWithoutExtension(targetPath) + " Variant.matVariant");
                variantPath = AssetDatabase.GenerateUniqueAssetPath(variantPath);

                InternalEditorUtility.SaveToSerializedFileAndForget(new[] { matVariant }, variantPath, true);
                AssetDatabase.ImportAsset(variantPath);
            }
        }
        #endregion
    }
}
