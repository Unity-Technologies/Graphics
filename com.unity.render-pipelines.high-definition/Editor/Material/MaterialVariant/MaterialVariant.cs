using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

using UnityEditor.Rendering.HighDefinition.ShaderGraph; //We store locks in HDMetaData as metadata are accessible without deserializing the whole graph.

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
                return MaterialVariantImporter.GetMaterialVariantFromAssetPath(parentPath);

            return parentAsset;
        }

        #region MaterialVariant Overrides Management
        public void TrimPreviousOverridesAndAdd(IEnumerable<MaterialPropertyModification> modifications)
        {
            foreach (var modification in modifications)
            {
                int pos;
                if (modification.propertyPath.StartsWith("::"))
                {
                    string key = $"::{modification.propertyPath.TrimStart(':').Split(':')[0]}:";
                    pos = overrides.FindIndex(o => o.propertyPath.StartsWith(key));
                }
                else
                    pos = overrides.FindIndex(o => o.propertyPath == modification.propertyPath);

                if (pos > -1)
                {
                    //prevent registration at frame for broken inspector that update material at inspector frame
                    if (overrides[pos] != modification)
                        overrides[pos] = modification;
                }
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

        public void ResetOverrides(MaterialProperty[] properties)
        {
            overrides.RemoveAll(modification => properties.Any(property => IsSameProperty(modification, property.name)));
        }

        public void ResetOverrideForNonMaterialProperty(string propertyName)
        {
            propertyName = $"::{propertyName}:";
            overrides.RemoveAll(modification => modification.propertyPath.StartsWith(propertyName));
        }

        #endregion

        #region MaterialVariant Blocks Management
        public bool IsPropertyBlockedInCurrent(string propertyName)
        {
            return blocks.Any(b => b == propertyName);
        }

        public bool IsPropertyBlockedInAncestors(string propertyName)
        {
            var parent = GetParent();
            if (parent is MaterialVariant matVariant)
                return matVariant.IsPropertyBlocked(propertyName);

            if (parent is Shader shader)
            {
                List<string> locks = HDMetaDataHelper.GetLocksFromMetaData(shader);
                if (locks != null)
                    return locks.Any(l => l == propertyName);
            }

            return false;
        }

        public bool IsPropertyBlocked(string propertyName)
        {
            return IsPropertyBlockedInCurrent(propertyName) || IsPropertyBlockedInAncestors(propertyName);
        }

        public void SetPropertyBlocked(string propertyName, bool block)
        {
            if (!block)
                blocks.Remove(propertyName);
            else if (!blocks.Contains(propertyName))
                blocks.Add(propertyName);
        }

        public void TogglePropertyBlocked(string propertyName)
        {
            if (!blocks.Remove(propertyName))
                blocks.Add(propertyName);
        }

        public void TogglePropertiesBlocked(MaterialProperty[] properties)
        {
            foreach (var prop in properties)
            {
                if (!blocks.Remove(prop.name))
                    blocks.Add(prop.name);
            }
        }

        public IEnumerable<MaterialPropertyModification> TrimOverridesList(IEnumerable<MaterialPropertyModification> overrides)
        {
            var parent = GetParent();
            if (parent is MaterialVariant matVariant)
                return overrides.Where(mpm => matVariant.IsPropertyBlocked(mpm.key));

            return overrides;
        }

        #endregion

        #region MaterialVariant Create Menu
        private const string MATERIAL_VARIANT_MENU_PATH = "Assets/Create/Material Variant";
        private const int MATERIAL_VARIANT_MENU_PRIORITY = 302; // right after material

        private static bool IsValidRoot(Object root)
        {
            // We allow to create a MaterialVariant without parent (for parenting later)
            // DefaultAsset identify the null case
            return EditorUtility.IsPersistent(root) && ((root is Material) || (root is Shader));
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
                Material og = AssetDatabase.LoadAssetAtPath(resourceFile, typeof(Material)) as Material;
                Material material = new Material(og);

                var matVariant = CreateInstance<MaterialVariant>();
                matVariant.rootGUID = AssetDatabase.AssetPathToGUID(resourceFile); // if resourceFile is "", it return "";
                matVariant.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;
                matVariant.name = Path.GetFileName(pathName);

                AssetDatabase.CreateAsset(material, pathName);
                AssetDatabase.AddObjectToAsset(matVariant, pathName);
                AssetDatabase.ImportAsset(pathName);
            }
        }

        [MenuItem(MATERIAL_VARIANT_MENU_PATH, false, MATERIAL_VARIANT_MENU_PRIORITY)]
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
                    "New Material Variant.mat",
                    null,
                    "");
            }
            else
            {
                string sourcePath = AssetDatabase.GetAssetPath(target);
                string variantPath = Path.Combine(Path.GetDirectoryName(sourcePath),
                    Path.GetFileNameWithoutExtension(sourcePath) + " Variant.mat");

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
