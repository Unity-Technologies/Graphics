using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEditor.Experimental.Rendering
{
    public class MaterialUpgrader
    {
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


        class KeywordFloatRename
        {
            public string keyword;
            public string property;
            public float setVal, unsetVal;
        }
        List<KeywordFloatRename> m_KeywordFloatRename = new List<KeywordFloatRename>();

        [Flags]
        public enum UpgradeFlags
        {
            None = 0,
            LogErrorOnNonExistingProperty = 1,
            CleanupNonUpgradedProperties = 2,
            LogMessageWhenNoUpgraderFound = 4
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

            if (m_Finalizer != null)
                m_Finalizer(material);
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

            foreach (var prop in m_TexturesToRemove)
                dstMaterial.SetTexture(prop, null);

            foreach (var prop in m_FloatPropertiesToSet)
                dstMaterial.SetFloat(prop.Key, prop.Value);

            foreach (var prop in m_ColorPropertiesToSet)
                dstMaterial.SetColor(prop.Key, prop.Value);
            foreach (var t in m_KeywordFloatRename)
                dstMaterial.SetFloat(t.property, srcMaterial.IsKeywordEnabled(t.keyword) ? t.setVal : t.unsetVal);
        }

        public void RenameShader(string oldName, string newName, MaterialFinalizer finalizer = null)
        {
            m_OldShader = oldName;
            m_NewShader = newName;
            m_Finalizer = finalizer;
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

        public void RemoveTexture(string name)
        {
            m_TexturesToRemove.Add(name);
        }

        public void SetFloat(string propertyName, float value)
        {
            m_FloatPropertiesToSet[propertyName] = value;
        }

        public void SetColor(string propertyName, Color value)
        {
            m_ColorPropertiesToSet[propertyName] = value;
        }

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

        public static void UpgradeProjectFolder(List<MaterialUpgrader> upgraders, string progressBarName, UpgradeFlags flags = UpgradeFlags.None)
        {
            if (!EditorUtility.DisplayDialog("Material Upgrader", "The upgrade will overwrite material settings in your project." +
                    "Be sure to have a project backup before proceeding", "Proceed", "Cancel"))
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
                    Upgrade(m, upgraders, flags);

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
            else if ((flags & UpgradeFlags.LogMessageWhenNoUpgraderFound) == UpgradeFlags.LogMessageWhenNoUpgraderFound)
                Debug.Log(string.Format("There's no upgrader to convert {0} shader to selected pipeline", material.shader.name));
        }

        public static void UpgradeSelection(List<MaterialUpgrader> upgraders, string progressBarName, UpgradeFlags flags = UpgradeFlags.None)
        {
            var selection = Selection.objects;
            if (!EditorUtility.DisplayDialog("Material Upgrader", string.Format("The upgrade will possibly overwrite all the {0} selected material settings", selection.Length) +
                    "Be sure to have a project backup before proceeding", "Proceed", "Cancel"))
                return;

            string lastMaterialName = "";
            for (int i = 0; i < selection.Length; i++)
            {
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(progressBarName, string.Format("({0} of {1}) {2}", i, selection.Length, lastMaterialName), (float)i / (float)selection.Length))
                    break;

                var material = selection[i] as Material;
                Upgrade(material, upgraders, flags);
                if (material != null)
                    lastMaterialName = material.name;
            }

            UnityEditor.EditorUtility.ClearProgressBar();
        }
    }
}
