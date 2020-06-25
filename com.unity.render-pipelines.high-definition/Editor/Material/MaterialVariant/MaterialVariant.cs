using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace Unity.Assets.MaterialVariant.Editor
{
    public class MaterialVariant : ScriptableObject
    {
        public string rootGUID;
        public List<MaterialPropertyModification> overrides = new List<MaterialPropertyModification>();
        public List<string> blocks = new List<string>();

        public Object GetParent()
        {
            string parentPath = AssetDatabase.GUIDToAssetPath(rootGUID);

            // If parent is deleted, just return null
            if (parentPath == null)
                return null;

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

        public bool IsPropertyBlockedInCurrent(MaterialProperty property)
        {
            return blocks.Any(b => b == property.name);
        }

        public bool IsPropertyBlockedInAncestors(MaterialProperty property)
        {
            var parent = GetParent();
            if (parent is MaterialVariant)
                return (parent as MaterialVariant).IsPropertyBlocked(property);

            return false;
        }

        public bool IsPropertyBlocked(MaterialProperty property)
        {
            return IsPropertyBlockedInCurrent(property) || IsPropertyBlockedInAncestors(property);
        }

        public void SetPropertyBlocked(MaterialProperty property, bool block)
        {
            string propertyName = property.name;

            if (!block)
                blocks.Remove(propertyName);
            else if (!blocks.Contains(propertyName))
                blocks.Add(propertyName);
        }

        public void TogglePropertyBlocked(MaterialProperty property)
        {
            string propertyName = property.name;
            if (!blocks.Remove(propertyName))
                blocks.Add(propertyName);
        }

        #region MaterialVariant Create Menu
        private const string MATERIAL_VARIANT_MENU_PATH = "Assets/Create/Variants/Material Variant";

        private static bool IsValidRoot(Object root)
        {
            // We allow to create a MaterialVariant without parent (for parenting later)
            // DefaultAsset identify the null case
            return (root is UnityEditor.DefaultAsset) || (EditorUtility.IsPersistent(root) && ((root is Material) || (root is Shader)));
        }

        [MenuItem(MATERIAL_VARIANT_MENU_PATH, true)]
        private static bool ValidateMaterialVariantMenu()
        {
            return IsValidRoot(Selection.activeObject);
        }

        class DoCreateNewMaterialVariant : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var matVariant = CreateInstance<MaterialVariant>();
                matVariant.rootGUID = AssetDatabase.AssetPathToGUID(resourceFile); // if resourceFile is "", it return "";
                matVariant.name = Path.GetFileName(pathName);

                InternalEditorUtility.SaveToSerializedFileAndForget(new[] { matVariant }, pathName, true);
                AssetDatabase.ImportAsset(pathName);
            }
        }

        [MenuItem(MATERIAL_VARIANT_MENU_PATH, false)]
        static void CreateMaterialVariantMenu()
        {
            var target = Selection.activeObject;
            if (!IsValidRoot(target))
                return;

            if (target is UnityEditor.DefaultAsset)
            {
                ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                    0,
                    ScriptableObject.CreateInstance<DoCreateNewMaterialVariant>(),
                    "New Material Variant.asset",
                    null,
                    "");
            }
            else
            {
                string sourcePath = AssetDatabase.GetAssetPath(target);
                string variantPath = Path.Combine(Path.GetDirectoryName(sourcePath),
                                        Path.GetFileNameWithoutExtension(sourcePath) + " Variant.matVariant");

                ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                    0,
                    ScriptableObject.CreateInstance<DoCreateNewMaterialVariant>(),
                    variantPath,
                    null,
                    sourcePath);
            }
        }
        #endregion
    }
}
