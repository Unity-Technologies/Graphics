using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.AssetImporters;
using UnityEditor.Rendering.ShaderGraph;

namespace UnityEditor.Rendering.MaterialVariants
{
    public class MaterialVariant : ScriptableObject
    {
        [SerializeField]
        private string m_ParentGUID;
        public string parentGUID
        {
            get => m_ParentGUID;
        }

        private string m_GUID = null;
        public string GUID
        {
            get => m_GUID;
        }

        [SerializeField]
        private List<MaterialPropertyModification> overrides = new List<MaterialPropertyModification>();
        [SerializeField]
        private List<string> blocks = new List<string>();

        private Material m_Material = null;
        public Material material
        {
            get
            {
                if (m_Material == null)
                    m_Material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GetAssetPath(this));
                return m_Material;
            }
        }

        public Object GetParent()
        {
            string parentPath = AssetDatabase.GUIDToAssetPath(m_ParentGUID);

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

        public void SetParent(Object asset)
        {
            Undo.RecordObject(this, "Change Parent");
            m_ParentGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
            overrides.Clear();
            blocks.Clear();
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

        public void ResetOverrides(MaterialProperty[] properties)
        {
            foreach (var property in properties)
            {
                overrides.RemoveAll(modification => IsSameProperty(modification, property.name));
                MaterialPropertyModification.ResetOverridenProperty(property, m_ParentGUID);
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

            if (parent is Shader shader)
            {
                var locks = Metadata.GetLocksFromMetadata(shader);
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

        public class HierarchyCache
        {
            public HashSet<MaterialVariant> children = new HashSet<MaterialVariant>();
            public struct Node
            {
                public MaterialVariant variant;
                public int nextSibling, parent;
            }
            public List<Node> sortedChildren = new List<Node>();
            public int cacheHash;

            public void Sort(string rootGUID)
            {
                sortedChildren.Clear();
                AppendChildren(rootGUID, -1);
            }

            void AppendChildren(string GUID, int parent)
            {
                foreach (var child in children)
                {
                    if (child.parentGUID == GUID)
                    {
                        int i = sortedChildren.Count;
                        sortedChildren.Add(default);
                        AppendChildren(child.GUID, i);
                        sortedChildren[i] = new Node()
                        {
                            variant = child,
                            nextSibling = sortedChildren.Count,
                            parent = parent
                        };
                    }
                }
            }
        };

        static HashSet<MaterialVariant> m_Variants = null;
        static Dictionary<string, HierarchyCache> cachedHierarchy = new Dictionary<string, HierarchyCache>();
        public static List<HierarchyCache.Node> GetChildren(string GUID)
        {
            if (m_Variants == null)
                m_Variants = new HashSet<MaterialVariant>(Resources.FindObjectsOfTypeAll<MaterialVariant>());

            bool validate = false, sort = false;
            if (cachedHierarchy.TryGetValue(GUID, out var cache))
            {
                if (cache.cacheHash == MaterialVariantEditorManager.instance.cacheHash)
                {
                    //Debug.Log("fetched directly from cache");
                    return cache.sortedChildren;
                }

                // Check if the hierarchy has changed
                foreach (var node in cache.sortedChildren)
                {
                    if (node.variant == null ||
                        (node.parent == -1 && node.variant.parentGUID != GUID) ||
                        (node.parent != -1 && cache.sortedChildren[node.parent].variant.GUID != node.variant.parentGUID))
                    {
                        //Debug.Log("hierarchy has changed. Recomputing");
                        cache.children.Clear();
                        sort = true;
                        break;
                    }
                }

                // Don't return yet, check if new elements have been added to the hierarchy
                validate = true;
            }
            else
            {
                //Debug.Log("Not found");
                cache = new HierarchyCache();
            }

            var hierarchy = new HashSet<string>();
            hierarchy.Add(GUID);

            // Remove delete material variants
            m_Variants.RemoveWhere(mv => mv == null);

            IEnumerator<MaterialVariant> iterator = m_Variants.GetEnumerator();
            while (iterator.MoveNext())
            {
                var variant = iterator.Current;
                if (hierarchy.Contains(variant.parentGUID) && !hierarchy.Contains(variant.GUID))
                {
                    hierarchy.Add(variant.GUID);
                    cache.children.Add(variant);
                    iterator.Reset();
                    sort = true;
                }
            }

            if (sort)
                cache.Sort(GUID);

            if (!validate)
                cachedHierarchy.Add(GUID, cache);
            cache.cacheHash = MaterialVariantEditorManager.instance.cacheHash;
            return cache.sortedChildren;
        }

        public static void UpdateHierarchy(string guid, MaterialProperty[] modifiedProperties)
        {
            var children = GetChildren(guid);

            // Debug display
            #if false
            string temp = "root: " + guid;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].variant.parentGUID == guid)
                    temp += "\n - " + children[i].variant.material.name;
                else
                    temp += " -> " + children[i].variant.material.name;
            }
            Debug.Log(temp);
            #endif

            foreach (var property in modifiedProperties)
            {
                int nameId = Shader.PropertyToID(property.name);
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i].variant.IsOverriddenProperty(property))
                        i = children[i].nextSibling - 1;
                    else if (children[i].variant.material.HasProperty(nameId))
                        MaterialPropertyModification.SyncPropertyWithParent(children[i].variant.material, property, nameId);
                }
            }
        }

        #endregion

        #region MaterialVariant Import

        public bool Import(AssetImportContext context, Material material)
        {
            if (m_Variants == null)
                m_Variants = new HashSet<MaterialVariant>(Resources.FindObjectsOfTypeAll<MaterialVariant>());
            m_Variants.Add(this);

            m_Material = material;
            m_GUID = AssetDatabase.AssetPathToGUID(context.assetPath);

            var newMaterial = GetMaterialFromRoot(context, m_ParentGUID);
            if (newMaterial != null)
            {
                material.shader = newMaterial.shader;
                material.CopyPropertiesFromMaterial(newMaterial);

                // Apply local modification
                MaterialPropertyModification.ApplyPropertyModificationsToMaterial(material, overrides);

                return true;
            }
            return false;
        }

        Material GetMaterialFromRoot(AssetImportContext ctx, string rootGUID)
        {
            string rootPath = AssetDatabase.GUIDToAssetPath(rootGUID);

            // If rootPath is empty it mean that the parent have been deleted. In this case return null
            if (rootPath == "")
                return null;

            ctx.DependsOnSourceAsset(rootGUID);

            Material rootMaterial = null;
            var assets = AssetDatabase.LoadAllAssetsAtPath(rootPath);
            foreach (var subAsset in assets)
            {
                if (subAsset == null)
                    continue;
                else if (subAsset.GetType() == typeof(Material))
                    rootMaterial = subAsset as Material; // Don't return yet in case there is a MaterialVariant after
                else if (subAsset.GetType() == typeof(MaterialVariant))
                {
                    MaterialVariant rootMatVariant = subAsset as MaterialVariant;
                    rootMaterial = GetMaterialFromRoot(ctx, rootMatVariant.m_ParentGUID);

                    // Apply root modification
                    if (rootMaterial != null)
                        MaterialPropertyModification.ApplyPropertyModificationsToMaterial(rootMaterial, rootMatVariant.overrides);

                    return rootMaterial;
                }
                else if (subAsset.GetType() == typeof(Shader))
                    return new Material(subAsset as Shader);
            }

            return rootMaterial ? new Material(rootMaterial) : null;
        }

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
                matVariant.m_ParentGUID = AssetDatabase.AssetPathToGUID(resourceFile); // if resourceFile is "", it return "";
                matVariant.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;

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

        #region MaterialVariant live editor

        class MaterialVariantEditorManager : ScriptableSingleton<MaterialVariantEditorManager>
        {
            int hotControl = 1;
            public int cacheHash = 1;

            void OnEnable()
            {
                EditorApplication.update += Update;
            }

            void OnDisable()
            {
                EditorApplication.update -= Update;
            }

            void Update()
            {
                if (GUIUtility.hotControl == 0 && hotControl != 0)
                    cacheHash++;
                hotControl = GUIUtility.hotControl;
            }
        }

        public static void RecordObjectsUndo(MaterialVariant[] variants, MaterialProperty[] properties)
        {
            var objects = new List<Object>(variants);
            foreach (var variant in variants)
            {
                if (variant == null)
                    continue;
                var children = GetChildren(variant.GUID);
                foreach (var property in properties)
                {
                    for (int i = 0; i < children.Count; i++)
                    {
                        if (children[i].variant.IsOverriddenProperty(property))
                            i = children[i].nextSibling - 1;
                        else
                            objects.Add(children[i].variant.material);
                    }
                }
            }
            Undo.RecordObjects(objects.ToArray(), "");

#if false
            string temp = "Saved assets";
            foreach (var o in objects)
                temp += "\n" + (o as MaterialVariant).material.name;
            Debug.Log(temp);
#endif
        }

        public static void RecordObjectsUndo(string guid, MaterialProperty[] properties)
        {
            var objects = new List<Object>();
            var children = GetChildren(guid);
            foreach (var property in properties)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i].variant.IsOverriddenProperty(property))
                        i = children[i].nextSibling - 1;
                    else
                        objects.Add(children[i].variant.material);
                }
            }
            Undo.RecordObjects(objects.ToArray(), "");
        }

        #endregion
    }
}
