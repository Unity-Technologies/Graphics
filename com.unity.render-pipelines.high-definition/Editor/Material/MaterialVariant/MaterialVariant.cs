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

        public List<MaterialPropertyModification> overrides = new List<MaterialPropertyModification>();
        
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
            if (EditorUtility.IsPersistent(target) && (target is Material || target is Shader))
            {
                var matVariant = ScriptableObject.CreateInstance<MaterialVariant>();
                matVariant.rootGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target));
                matVariant.overrides = new List<MaterialPropertyModification>();

                var targetPath = AssetDatabase.GetAssetPath(target);
                targetPath = Path.Combine(Path.GetDirectoryName(targetPath),
                    Path.GetFileNameWithoutExtension(targetPath) + " Variant.matVariant");
                targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);

                InternalEditorUtility.SaveToSerializedFileAndForget(new[] { matVariant }, targetPath, true);
                AssetDatabase.ImportAsset(targetPath);
            }
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
