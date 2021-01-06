using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.MaterialVariants
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
                return GetMaterialVariantFromAssetPath(parentPath) ?? parentAsset;

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
            Undo.RecordObject(this, $"'Reset Override'");
            MaterialPropertyModification.RevertModification(property, rootGUID);
            overrides.RemoveAll(modification => IsSameProperty(modification, property.name));
        }

        public void ResetOverrides(MaterialProperty[] properties)
        {
            Undo.RecordObject(this, $"'Reset Override'");

            foreach (var property in properties)
            {
                MaterialPropertyModification.RevertModification(property, rootGUID);
                overrides.RemoveAll(modification => IsSameProperty(modification, property.name));
            }
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

            /* TODO This is intended to check for locks at the ShaderGraph level, but wasn't working, so I'm commenting it out
             * We'd need to find a way to store those locks that doesn't depend on the specific RP anyway
            if (parent is Shader shader)
            {
                List<string> locks = HDMetaDataHelper.GetLocksFromMetaData(shader);
                if (locks != null)
                    return locks.Any(l => l == propertyName);
            }
            */

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

        #region MaterialVariant Editor
        static Dictionary<UnityEditor.Editor, MaterialVariant[]> registeredVariants = new Dictionary<UnityEditor.Editor, MaterialVariant[]>();

        public static MaterialVariant[] GetMaterialVariantsFor(MaterialEditor editor)
        {
            if (registeredVariants.TryGetValue(editor, out var variants))
                return variants;

            var deletedEntries = new List<UnityEditor.Editor>();
            foreach (var entry in registeredVariants)
            {
                if (entry.Key == null)
                    deletedEntries.Add(entry.Key);
            }
            foreach (var key in deletedEntries)
                registeredVariants.Remove(key);

            int i = 0;
            variants = new MaterialVariant[editor.targets.Length];
            foreach (var target in editor.targets)
            {
                var variant = MaterialVariant.GetMaterialVariantFromObject(target);
                if (variant == null)
                    return null;
                variants[i++] = variant;
            }

            registeredVariants.Add(editor, variants);
            return variants;
        }

        #endregion

        #region MaterialVariant Deserialization
        // Caution: GetMaterialVariantFromAssetPath can't be call inside OnImportAsset() as ctx.AddObjectToAsset("Variant", matVariant) is not define yet
        public static MaterialVariant GetMaterialVariantFromAssetPath(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<MaterialVariant>().FirstOrDefault();
        }

        public static MaterialVariant GetMaterialVariantFromGUID(string GUID)
        {
            return GetMaterialVariantFromAssetPath(AssetDatabase.GUIDToAssetPath(GUID));
        }

        public static MaterialVariant GetMaterialVariantFromObject(Object obj)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(obj));
            return System.Array.Find(assets, o => o.GetType() == typeof(MaterialVariant)) as MaterialVariant;
        }

        #endregion

        #region MaterialVariant Create Menu
        private const string MATERIAL_VARIANT_MENU_PATH = "Assets/Create/Material Variant";
        private const int MATERIAL_VARIANT_MENU_PRIORITY = 302; // right after material

        private static bool IsValidRoot(Object root)
        {
            // We allow to create a MaterialVariant without parent (for parenting later)
            // DefaultAsset identify the null case
            return (root is UnityEditor.DefaultAsset) || (EditorUtility.IsPersistent(root) && ((root is Material) || (root is Shader)));
        }

        class DoCreateNewMaterialVariant : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                if (resourceFile == "")
                    resourceFile = AssetDatabase.GetAssetPath(GraphicsSettings.renderPipelineAsset.defaultShader);

                Material material;
                Object parentAsset = AssetDatabase.LoadAssetAtPath<Object>(resourceFile);
                if (parentAsset is Material)
                    material = new Material(parentAsset as Material);
                else if (parentAsset is Shader)
                    material = new Material(parentAsset as Shader);
                else return;

                var matVariant = CreateInstance<MaterialVariant>();
                matVariant.rootGUID = AssetDatabase.AssetPathToGUID(resourceFile); // if resourceFile is "", it return "";
                matVariant.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;
                matVariant.name = Path.GetFileName(pathName);

                AssetDatabase.CreateAsset(material, pathName);
                AssetDatabase.AddObjectToAsset(matVariant, pathName);
                AssetDatabase.ImportAsset(pathName);
            }
        }

        [MenuItem(MATERIAL_VARIANT_MENU_PATH, true)]
        static bool ValidateMaterialVariantMenu()
        {
            return IsValidRoot(Selection.activeObject);
        }

        [MenuItem(MATERIAL_VARIANT_MENU_PATH, false, MATERIAL_VARIANT_MENU_PRIORITY)]
        static void CreateMaterialVariantMenu()
        {
            var target = Selection.activeObject;

            string sourcePath, variantPath;
            if (target is UnityEditor.DefaultAsset)
            {
                sourcePath = "";
                variantPath = "New Material Variant.mat";
            }
            else
            {
                sourcePath = AssetDatabase.GetAssetPath(target);
                variantPath = Path.Combine(Path.GetDirectoryName(sourcePath),
                    Path.GetFileNameWithoutExtension(sourcePath) + " Variant.mat");
            }

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<DoCreateNewMaterialVariant>(),
                variantPath,
                null,
                sourcePath);
        }

        #endregion
    }
}
