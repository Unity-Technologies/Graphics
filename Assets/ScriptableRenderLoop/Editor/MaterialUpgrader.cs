using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEditor.Experimental.Rendering
{
    public class MaterialUpgrader
    {
        string m_OldShader;
        string m_NewShader;

        Dictionary<string, string> m_TextureRename = new Dictionary<string, string>();
        Dictionary<string, string> m_FloatRename = new Dictionary<string, string>();
        Dictionary<string, string> m_ColorRename = new Dictionary<string, string>();

        [Flags]
        public enum UpgradeFlags
        {
            None = 0,
            LogErrorOnNonExistingProperty = 1,
            CleanupNonUpgradedProperties = 2,
        }

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
        }

        // Overridable function to implement custom material upgrading functionality
        public virtual void Convert(Material srcMaterial, Material dstMaterial)
        {
            foreach (var t in m_TextureRename)
            {
                dstMaterial.SetTextureScale(t.Value, srcMaterial.GetTextureScale(t.Key));
                dstMaterial.SetTextureOffset(t.Value, srcMaterial.GetTextureOffset(t.Key));
                dstMaterial.SetTexture(t.Value, srcMaterial.GetTexture(t.Key));
            }

            foreach (var t in m_FloatRename)
                dstMaterial.SetFloat(t.Value, srcMaterial.GetFloat(t.Key));

            foreach (var t in m_ColorRename)
                dstMaterial.SetColor(t.Value, srcMaterial.GetColor(t.Key));
        }

        public void RenameShader(string oldName, string newName)
        {
            m_OldShader = oldName;
            m_NewShader = newName;
        }

        public void RenameTexture(string oldName, string newName)
        {
            m_TextureRename[oldName] = newName;
        }

        public void RenameFloat(string oldName, string newName)
        {
            m_FloatRename[oldName] = newName;
        }

        public void RenameColor(string oldName, string newName)
        {
            m_ColorRename[oldName] = newName;
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

        public static void UpgradeProjectFolder(List<MaterialUpgrader> upgraders, string progressBarName)
        {
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
                    Upgrade(m, upgraders, UpgradeFlags.None);

                    //SaveAssetsAndFreeMemory();
                }
            }

            UnityEditor.EditorUtility.ClearProgressBar();
        }

        public static void Upgrade(Material material, MaterialUpgrader upgrader, UpgradeFlags flags)
        {
            var upgraders = new List<MaterialUpgrader>();
            upgraders.Add(upgrader);
            Upgrade(material, upgraders, flags);
        }

        public static void Upgrade(Material material, List<MaterialUpgrader> upgraders, UpgradeFlags flags)
        {
            var upgrader = GetUpgrader(upgraders, material);
            if (upgrader != null)
                upgrader.Upgrade(material, flags);
        }

        public static void UpgradeSelection(List<MaterialUpgrader> upgraders, string progressBarName)
        {
            string lastMaterialName = "";
            var selection = Selection.objects;
            for (int i = 0; i < selection.Length; i++)
            {

                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(progressBarName, string.Format("({0} of {1}) {2}", i, selection.Length, lastMaterialName), (float)i / (float)selection.Length))
                    break;

                var material = selection[i] as Material;
                Upgrade(material, upgraders, UpgradeFlags.None);
                if (material != null)
                    lastMaterialName = material.name;
            }

            UnityEditor.EditorUtility.ClearProgressBar();
        }
    }
}
