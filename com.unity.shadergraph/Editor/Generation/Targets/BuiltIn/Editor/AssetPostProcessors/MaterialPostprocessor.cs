using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Rendering.BuiltIn;
using UnityEngine;
using static Unity.Rendering.BuiltIn.ShaderUtils;

namespace UnityEditor.Rendering.BuiltIn
{
    class MaterialModificationProcessor : AssetModificationProcessor
    {
        static void OnWillCreateAsset(string asset)
        {
            if (!asset.ToLowerInvariant().EndsWith(".mat"))
            {
                return;
            }
            MaterialPostprocessor.s_CreatedAssets.Add(asset);
        }
    }

    class MaterialReimporter : Editor
    {
        static bool s_NeedToCheckProjSettingExistence = true;

        static void ReimportAllMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:material", null);
            // There can be several materials subAssets per guid ( ie : FBX files ), remove duplicate guids.
            var distinctGuids = guids.Distinct();

            int materialIdx = 0;
            int totalMaterials = distinctGuids.Count();
            foreach (var asset in distinctGuids)
            {
                materialIdx++;
                var path = AssetDatabase.GUIDToAssetPath(asset);
                EditorUtility.DisplayProgressBar("Material Upgrader re-import", string.Format("({0} of {1}) {2}", materialIdx, totalMaterials, path), (float)materialIdx / (float)totalMaterials);
                AssetDatabase.ImportAsset(path);
            }
            EditorUtility.ClearProgressBar();

            MaterialPostprocessor.s_NeedsSavingAssets = true;
        }

        [InitializeOnLoadMethod]
        static void RegisterUpgraderReimport()
        {
        }
    }

    class MaterialPostprocessor : AssetPostprocessor
    {
        public static List<string> s_CreatedAssets = new List<string>();
        internal static List<string> s_ImportedAssetThatNeedSaving = new List<string>();
        internal static bool s_NeedsSavingAssets = false;

        internal static readonly Action<Material, ShaderID>[] k_Upgraders = {};

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var upgradeLog = "BuiltInRP Material log:";
            var upgradeCount = 0;

            foreach (var asset in importedAssets)
            {
                if (!asset.EndsWith(".mat", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var material = (Material)AssetDatabase.LoadAssetAtPath(asset, typeof(Material));

                ShaderID id = ShaderUtils.GetShaderID(material.shader);
                var wasUpgraded = false;

                var debug = "\n" + material.name;

                AssetVersion assetVersion = null;
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(asset);
                foreach (var subAsset in allAssets)
                {
                    if (subAsset is AssetVersion sub)
                    {
                        assetVersion = sub;
                    }
                }

                if (!assetVersion)
                {
                    wasUpgraded = true;
                    assetVersion = ScriptableObject.CreateInstance<AssetVersion>();
                    if (s_CreatedAssets.Contains(asset))
                    {
                        assetVersion.version = k_Upgraders.Length;
                        s_CreatedAssets.Remove(asset);
                        InitializeLatest(material, id);
                        debug += " initialized.";
                    }
                    else
                    {
                        //assetVersion.version = UniversalProjectSettings.materialVersionForUpgrade;
                        //debug += $" assumed to be version {UniversalProjectSettings.materialVersionForUpgrade} due to missing version.";
                    }

                    assetVersion.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;
                    AssetDatabase.AddObjectToAsset(assetVersion, asset);
                }

                while (assetVersion.version < k_Upgraders.Length)
                {
                    k_Upgraders[assetVersion.version](material, id);
                    debug += $" upgrading:v{assetVersion.version} to v{assetVersion.version + 1}";
                    assetVersion.version++;
                    wasUpgraded = true;
                }

                if (wasUpgraded)
                {
                    upgradeLog += debug;
                    upgradeCount++;
                    EditorUtility.SetDirty(assetVersion);
                    s_ImportedAssetThatNeedSaving.Add(asset);
                    s_NeedsSavingAssets = true;
                }
            }
        }

        static void InitializeLatest(Material material, ShaderID id)
        {
            // newly created shadergraph materials should reset their keywords immediately (in case inspector doesn't get invoked)
            if (IsShaderGraph(material))
            {
                // Debug.Log("Resetting new material: " + material.name);
                Unity.Rendering.BuiltIn.ShaderUtils.ResetMaterialKeywords(material);
            }
            // TODO: should probably call reset material keywords for all materials, not just shadergraph
        }

        // Copied from another PR. This will eventually be in GraphUtils.cs
        public static bool IsShaderGraph(Material material)
        {
            var shaderGraphTag = material.GetTag("ShaderGraphShader", false, null);
            return (shaderGraphTag != null);
        }
    }
}
