using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Material Upgrader dialog text.
    /// </summary>
    public static class DialogText
    {
        /// <summary>Material Upgrader title.</summary>
        public static readonly string title = "Material Upgrader";
        /// <summary>Material Upgrader proceed.</summary>
        public static readonly string proceed = "Proceed";
        /// <summary>Material Upgrader Ok.</summary>
        public static readonly string ok = "Ok";
        /// <summary>Material Upgrader cancel.</summary>
        public static readonly string cancel = "Cancel";
        /// <summary>Material Upgrader no selection message.</summary>
        public static readonly string noSelectionMessage = "You must select at least one material.";
        /// <summary>Material Upgrader project backup message.</summary>
        public static readonly string projectBackMessage = "Make sure to have a project backup before proceeding.";
    }

    /// <summary>
    /// Material Upgrader class.
    /// </summary>
    public class MaterialUpgrader
    {
        /// <summary>
        /// Material Upgrader finalizer delegate.
        /// </summary>
        /// <param name="mat">Material</param>
        public delegate void MaterialFinalizer(Material mat);

        string m_OldShader;
        string m_NewShader;

        MaterialFinalizer m_Finalizer;

        Dictionary<string, string> m_TextureRename = new Dictionary<string, string>();
        Dictionary<string, string> m_FloatRename = new Dictionary<string, string>();
        Dictionary<string, string> m_ColorRename = new Dictionary<string, string>();

        Dictionary<string, float> m_FloatPropertiesToSet = new Dictionary<string, float>();
        Dictionary<string, Color> m_ColorPropertiesToSet = new Dictionary<string, Color>();
        List<string> m_TexturesToRemove = new List<string>();
        Dictionary<string, Texture> m_TexturesToSet = new Dictionary<string, Texture>();


        class KeywordFloatRename
        {
            public string keyword;
            public string property;
            public float setVal, unsetVal;
        }
        List<KeywordFloatRename> m_KeywordFloatRename = new List<KeywordFloatRename>();

        /// <summary>
        /// Upgrade Flags
        /// </summary>
        [Flags]
        public enum UpgradeFlags
        {
            /// <summary>None.</summary>
            None = 0,
            /// <summary>LogErrorOnNonExistingProperty.</summary>
            LogErrorOnNonExistingProperty = 1,
            /// <summary>CleanupNonUpgradedProperties.</summary>
            CleanupNonUpgradedProperties = 2,
            /// <summary>LogMessageWhenNoUpgraderFound.</summary>
            LogMessageWhenNoUpgraderFound = 4
        }

        /// <summary>
        /// Upgrade method.
        /// </summary>
        /// <param name="material">Material to upgrade.</param>
        /// <param name="flags">Upgrade flag</param>
        public void Upgrade(Material material, UpgradeFlags flags)
        {
            Material newMaterial;
            if ((flags & UpgradeFlags.CleanupNonUpgradedProperties) != 0)
            {
                newMaterial = new Material(Shader.Find(m_NewShader));
            }
            else
            {
                newMaterial = UnityEngine.Object.Instantiate(material) as Material;
                newMaterial.shader = Shader.Find(m_NewShader);
            }

            Convert(material, newMaterial);

            material.shader = Shader.Find(m_NewShader);
            material.CopyPropertiesFromMaterial(newMaterial);
            UnityEngine.Object.DestroyImmediate(newMaterial);

            if (m_Finalizer != null)
                m_Finalizer(material);
        }

        // Overridable function to implement custom material upgrading functionality
        /// <summary>
        /// Custom material conversion method.
        /// </summary>
        /// <param name="srcMaterial">Source material.</param>
        /// <param name="dstMaterial">Destination material.</param>
        public virtual void Convert(Material srcMaterial, Material dstMaterial)
        {
            foreach (var t in m_TextureRename)
            {
                if (!srcMaterial.HasProperty(t.Key) || !dstMaterial.HasProperty(t.Value))
                    continue;

                dstMaterial.SetTextureScale(t.Value, srcMaterial.GetTextureScale(t.Key));
                dstMaterial.SetTextureOffset(t.Value, srcMaterial.GetTextureOffset(t.Key));
                dstMaterial.SetTexture(t.Value, srcMaterial.GetTexture(t.Key));
            }

            foreach (var t in m_FloatRename)
            {
                if (!srcMaterial.HasProperty(t.Key) || !dstMaterial.HasProperty(t.Value))
                    continue;

                dstMaterial.SetFloat(t.Value, srcMaterial.GetFloat(t.Key));
            }

            foreach (var t in m_ColorRename)
            {
                if (!srcMaterial.HasProperty(t.Key) || !dstMaterial.HasProperty(t.Value))
                    continue;

                dstMaterial.SetColor(t.Value, srcMaterial.GetColor(t.Key));
            }

            foreach (var prop in m_TexturesToRemove)
            {
                if (!dstMaterial.HasProperty(prop))
                    continue;

                dstMaterial.SetTexture(prop, null);
            }

            foreach (var prop in m_TexturesToSet)
            {
                if (!dstMaterial.HasProperty(prop.Key))
                    continue;

                dstMaterial.SetTexture(prop.Key, prop.Value);
            }

            foreach (var prop in m_FloatPropertiesToSet)
            {
                if (!dstMaterial.HasProperty(prop.Key))
                    continue;

                dstMaterial.SetFloat(prop.Key, prop.Value);
            }

            foreach (var prop in m_ColorPropertiesToSet)
            {
                if (!dstMaterial.HasProperty(prop.Key))
                    continue;

                dstMaterial.SetColor(prop.Key, prop.Value);
            }

            foreach (var t in m_KeywordFloatRename)
            {
                if (!dstMaterial.HasProperty(t.property))
                    continue;

                dstMaterial.SetFloat(t.property, srcMaterial.IsKeywordEnabled(t.keyword) ? t.setVal : t.unsetVal);
            }
        }

        /// <summary>
        /// Rename shader.
        /// </summary>
        /// <param name="oldName">Old name.</param>
        /// <param name="newName">New name.</param>
        /// <param name="finalizer">Finalizer delegate.</param>
        public void RenameShader(string oldName, string newName, MaterialFinalizer finalizer = null)
        {
            m_OldShader = oldName;
            m_NewShader = newName;
            m_Finalizer = finalizer;
        }

        /// <summary>
        /// Rename Texture Parameter.
        /// </summary>
        /// <param name="oldName">Old name.</param>
        /// <param name="newName">New name.</param>
        public void RenameTexture(string oldName, string newName)
        {
            m_TextureRename[oldName] = newName;
        }

        /// <summary>
        /// Rename Float Parameter.
        /// </summary>
        /// <param name="oldName">Old name.</param>
        /// <param name="newName">New name.</param>
        public void RenameFloat(string oldName, string newName)
        {
            m_FloatRename[oldName] = newName;
        }

        /// <summary>
        /// Rename Color Parameter.
        /// </summary>
        /// <param name="oldName">Old name.</param>
        /// <param name="newName">New name.</param>
        public void RenameColor(string oldName, string newName)
        {
            m_ColorRename[oldName] = newName;
        }

        /// <summary>
        /// Remove Texture Parameter.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        public void RemoveTexture(string name)
        {
            m_TexturesToRemove.Add(name);
        }

        /// <summary>
        /// Set float property.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        /// <param name="value">Property value.</param>
        public void SetFloat(string propertyName, float value)
        {
            m_FloatPropertiesToSet[propertyName] = value;
        }

        /// <summary>
        /// Set color property.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        /// <param name="value">Property value.</param>
        public void SetColor(string propertyName, Color value)
        {
            m_ColorPropertiesToSet[propertyName] = value;
        }

        /// <summary>
        /// Set texture property.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        /// <param name="value">Property value.</param>
        public void SetTexture(string propertyName, Texture value)
        {
            m_TexturesToSet[propertyName] = value;
        }

        /// <summary>
        /// Rename a keyword to float.
        /// </summary>
        /// <param name="oldName">Old name.</param>
        /// <param name="newName">New name.</param>
        /// <param name="setVal">Value when set.</param>
        /// <param name="unsetVal">Value when unset.</param>
        public void RenameKeywordToFloat(string oldName, string newName, float setVal, float unsetVal)
        {
            m_KeywordFloatRename.Add(new KeywordFloatRename { keyword = oldName, property = newName, setVal = setVal, unsetVal = unsetVal });
        }

        static bool IsMaterialPath(string path)
        {
            return path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase);
        }

        static MaterialUpgrader GetUpgrader(List<MaterialUpgrader> upgraders, Material material)
        {
            if (material == null || material.shader == null)
                return null;

            string shaderName = material.shader.name;
            for (int i = 0; i != upgraders.Count; i++)
            {
                if (upgraders[i].m_OldShader == shaderName)
                    return upgraders[i];
            }

            return null;
        }

        //@TODO: Only do this when it exceeds memory consumption...
        static void SaveAssetsAndFreeMemory()
        {
            AssetDatabase.SaveAssets();
            GC.Collect();
            EditorUtility.UnloadUnusedAssetsImmediate();
            AssetDatabase.Refresh();
        }

        private static bool ShouldUpgradeShader(Material material, HashSet<string> shaderNamesToIgnore)
        {
            if (material == null)
                return false;

            if (material.shader == null)
                return false;

            return !shaderNamesToIgnore.Contains(material.shader.name);
        }

        /// <summary>
        /// Upgrade the project folder.
        /// </summary>
        /// <param name="upgraders">List of upgraders.</param>
        /// <param name="progressBarName">Name of the progress bar.</param>
        /// <param name="flags">Material Upgrader flags.</param>
        public static void UpgradeProjectFolder(List<MaterialUpgrader> upgraders, string progressBarName, UpgradeFlags flags = UpgradeFlags.None)
        {
            HashSet<string> shaderNamesToIgnore = new HashSet<string>();
            UpgradeProjectFolder(upgraders, shaderNamesToIgnore, progressBarName, flags);
        }

        /// <summary>
        /// Upgrade the project folder.
        /// </summary>
        /// <param name="upgraders">List of upgraders.</param>
        /// <param name="shaderNamesToIgnore">Set of shader names to ignore.</param>
        /// <param name="progressBarName">Name of the progress bar.</param>
        /// <param name="flags">Material Upgrader flags.</param>
        public static void UpgradeProjectFolder(List<MaterialUpgrader> upgraders, HashSet<string> shaderNamesToIgnore, string progressBarName, UpgradeFlags flags = UpgradeFlags.None)
        {
            if ((!Application.isBatchMode) && (!EditorUtility.DisplayDialog(DialogText.title, "The upgrade will overwrite materials in your project. " + DialogText.projectBackMessage, DialogText.proceed, DialogText.cancel)))
                return;

            int totalMaterialCount = 0;
            foreach (string s in UnityEditor.AssetDatabase.GetAllAssetPaths())
            {
                if (IsMaterialPath(s))
                    totalMaterialCount++;
            }
            
            int materialIndex = 0;
            foreach (string path in UnityEditor.AssetDatabase.GetAllAssetPaths())
            {
                if (IsMaterialPath(path))
                {
                    materialIndex++;
                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(progressBarName, string.Format("({0} of {1}) {2}", materialIndex, totalMaterialCount, path), (float)materialIndex / (float)totalMaterialCount))
                        break;

                    Material m = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path) as Material;

                    if (!ShouldUpgradeShader(m, shaderNamesToIgnore))
                        continue;
                    
                    Upgrade(m, upgraders, flags);

                    //SaveAssetsAndFreeMemory();
                }
            }

            // Upgrade terrain specifically since it is a builtin material
            if (Terrain.activeTerrains.Length > 0)
            {
                Material terrainMat = Terrain.activeTerrain.materialTemplate;
                Upgrade(terrainMat, upgraders, flags);
            }

            UnityEditor.EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Upgrade a material.
        /// </summary>
        /// <param name="material">Material to upgrade.</param>
        /// <param name="upgrader">Material upgrader.</param>
        /// <param name="flags">Material Upgrader flags.</param>
        public static void Upgrade(Material material, MaterialUpgrader upgrader, UpgradeFlags flags)
        {
            var upgraders = new List<MaterialUpgrader>();
            upgraders.Add(upgrader);
            Upgrade(material, upgraders, flags);
        }

        /// <summary>
        /// Upgrade a material.
        /// </summary>
        /// <param name="material">Material to upgrade.</param>
        /// <param name="upgraders">List of Material upgraders.</param>
        /// <param name="flags">Material Upgrader flags.</param>
        public static void Upgrade(Material material, List<MaterialUpgrader> upgraders, UpgradeFlags flags)
        {
            if (material == null)
                return;

            var upgrader = GetUpgrader(upgraders, material);

            if (upgrader != null)
                upgrader.Upgrade(material, flags);
            else if ((flags & UpgradeFlags.LogMessageWhenNoUpgraderFound) == UpgradeFlags.LogMessageWhenNoUpgraderFound)
                Debug.Log(string.Format("{0} material was not upgraded. There's no upgrader to convert {1} shader to selected pipeline", material.name, material.shader.name));
        }

        /// <summary>
        /// Upgrade the selection.
        /// </summary>
        /// <param name="upgraders">List of upgraders.</param>
        /// <param name="progressBarName">Name of the progress bar.</param>
        /// <param name="flags">Material Upgrader flags.</param>
        public static void UpgradeSelection(List<MaterialUpgrader> upgraders, string progressBarName, UpgradeFlags flags = UpgradeFlags.None)
        {
            HashSet<string> shaderNamesToIgnore = new HashSet<string>();
            UpgradeSelection(upgraders, shaderNamesToIgnore, progressBarName, flags);
        }

        /// <summary>
        /// Upgrade the selection.
        /// </summary>
        /// <param name="upgraders">List of upgraders.</param>
        /// <param name="shaderNamesToIgnore">Set of shader names to ignore.</param>
        /// <param name="progressBarName">Name of the progress bar.</param>
        /// <param name="flags">Material Upgrader flags.</param>
        public static void UpgradeSelection(List<MaterialUpgrader> upgraders, HashSet<string> shaderNamesToIgnore, string progressBarName, UpgradeFlags flags = UpgradeFlags.None)
        {
            var selection = Selection.objects;

            if (selection == null)
            {
                EditorUtility.DisplayDialog(DialogText.title, DialogText.noSelectionMessage, DialogText.ok);
                return;
            }

            List<Material> selectedMaterials = new List<Material>(selection.Length);
            for (int i = 0; i < selection.Length; ++i)
            {
                Material mat = selection[i] as Material;
                if (mat != null)
                    selectedMaterials.Add(mat);
            }

            int selectedMaterialsCount = selectedMaterials.Count;
            if (selectedMaterialsCount == 0)
            {
                EditorUtility.DisplayDialog(DialogText.title, DialogText.noSelectionMessage, DialogText.ok);
                return;
            }

            if (!EditorUtility.DisplayDialog(DialogText.title, string.Format("The upgrade will overwrite {0} selected material{1}. ", selectedMaterialsCount, selectedMaterialsCount > 1 ? "s" : "") +
                    DialogText.projectBackMessage, DialogText.proceed, DialogText.cancel))
                return;

            string lastMaterialName = "";
            for (int i = 0; i < selectedMaterialsCount; i++)
            {
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(progressBarName, string.Format("({0} of {1}) {2}", i, selectedMaterialsCount, lastMaterialName), (float)i / (float)selectedMaterialsCount))
                    break;

                var material = selectedMaterials[i];

                if (!ShouldUpgradeShader(material, shaderNamesToIgnore))
                    continue;

                Upgrade(material, upgraders, flags);
                if (material != null)
                    lastMaterialName = material.name;
            }

            UnityEditor.EditorUtility.ClearProgressBar();
        }
    }
}
