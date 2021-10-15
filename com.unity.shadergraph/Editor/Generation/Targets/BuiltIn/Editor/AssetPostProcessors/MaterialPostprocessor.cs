using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering.BuiltIn;
using UnityEngine;
using static UnityEditor.Rendering.BuiltIn.ShaderUtils;

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
        // Currently this is never called because there's no way for built-in target shader graph
        // materials to track when they need to be upgraded at a global level. To do this currently
        // we'd have to iterate over all materials to see if they need upgrading. We may want to add a
        // global settings object like UniversalProjectSettings but for built-in to track this.
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

        internal static readonly Action<Material, ShaderID>[] k_Upgraders = { };

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var upgradeCount = 0;

            foreach (var asset in importedAssets)
            {
                // We only care about materials
                if (!asset.EndsWith(".mat", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                // Load the material and look for it's BuiltIn ShaderID.
                // We only care about versioning materials using a known BuiltIn ShaderID.
                // This skips any materials that only target other render pipelines, are user shaders,
                // or are shaders we don't care to version
                var material = (Material)AssetDatabase.LoadAssetAtPath(asset, typeof(Material));
                var shaderID = GetShaderID(material.shader);
                if (shaderID == ShaderID.Unknown)
                    continue;

                var wasUpgraded = false;

                // Look for the BuiltIn AssetVersion
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
                    // The asset was newly created, force initialize them
                    if (s_CreatedAssets.Contains(asset))
                    {
                        assetVersion.version = k_Upgraders.Length;
                        s_CreatedAssets.Remove(asset);
                        InitializeLatest(material, shaderID);
                    }
                    else if (shaderID.IsShaderGraph())
                    {
                        // Assumed to be version 0 since no asset version was found
                        assetVersion.version = 0;
                    }

                    assetVersion.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;
                    AssetDatabase.AddObjectToAsset(assetVersion, asset);
                }

                while (assetVersion.version < k_Upgraders.Length)
                {
                    k_Upgraders[assetVersion.version](material, shaderID);
                    assetVersion.version++;
                    wasUpgraded = true;
                }

                if (wasUpgraded)
                {
                    upgradeCount++;
                    EditorUtility.SetDirty(assetVersion);
                    s_ImportedAssetThatNeedSaving.Add(asset);
                    s_NeedsSavingAssets = true;
                }
            }
        }

        static void InitializeLatest(Material material, ShaderID shaderID)
        {
            // newly created shadergraph materials should reset their keywords immediately (in case inspector doesn't get invoked)
            ShaderUtils.ResetMaterialKeywords(material);
        }
    }
}
