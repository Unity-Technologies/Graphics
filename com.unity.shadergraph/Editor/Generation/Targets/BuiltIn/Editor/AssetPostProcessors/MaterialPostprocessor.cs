using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering.BuiltIn;
using UnityEngine;
using UnityEditorInternal;
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

    class MaterialPostprocessor : AssetPostprocessor
    {
        public const string materialVersionDependencyName = "builtin-material-version";

        [InitializeOnLoadMethod]
        static void RegisterUpgraderReimport()
        {
            UnityEditor.MaterialPostProcessor.onImportedMaterial += OnImportedMaterial;

            // Register custom dependency on Material version
            AssetDatabase.RegisterCustomDependency(materialVersionDependencyName, Hash128.Compute(MaterialPostprocessor.k_Upgraders.Length));
            AssetDatabase.Refresh();
        }

        // TODOJENNY: [HACK] remove this when Material class add custom dependency itself
        // we need to ensure existing mat also have the dependency
        void OnPreprocessAsset()
        {
            if (!UnityEditor.MaterialPostProcessor.IsMaterialPath(assetPath))
                return;

            var objs = InternalEditorUtility.LoadSerializedFileAndForget(assetPath);
            foreach (var obj in objs)
            {
                if (obj is Material material)
                {
                    // check if Built-in material
                    var shaderID = GetShaderID(material.shader);
                    if (shaderID == ShaderID.Unknown)
                        continue;

                    context.DependsOnCustomDependency(materialVersionDependencyName);

                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material.shader, out var guid, out long _);
                    //context.DependsOnArtifact(new GUID(guid)); //artifact is the result of an import
                    context.DependsOnSourceAsset(new GUID(guid)); //TODOJENNY try this until the other one works
                }
            }
        }

        public static List<string> s_CreatedAssets = new List<string>();
        internal static readonly Action<Material, ShaderID>[] k_Upgraders = {};

        static void OnImportedMaterial(Material material, string assetPath)
        {
            // Load the material and look for it's BuiltIn ShaderID.
            // We only care about versioning materials using a known BuiltIn ShaderID.
            // This skips any materials that only target other render pipelines, are user shaders,
            // or are shaders we don't care to version
            var shaderID = GetShaderID(material.shader);
            if (shaderID == ShaderID.Unknown)
                return;

            var wasUpgraded = false;

            // Look for the BuiltIn AssetVersion
            AssetVersion assetVersion = null;
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var subAsset in allAssets)
            {
                if (subAsset is AssetVersion sub)
                {
                    assetVersion = sub;
                    break;
                }
            }

            if (!assetVersion)
            {
                wasUpgraded = true;
                assetVersion = ScriptableObject.CreateInstance<AssetVersion>();
                assetVersion.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;

                // The asset was newly created, force initialize them
                if (s_CreatedAssets.Contains(assetPath))
                {
                    assetVersion.version = k_Upgraders.Length;
                    s_CreatedAssets.Remove(assetPath);
                    InitializeLatest(material, shaderID);
                }
                else
                {
                    // Assumed to be version 0 since no asset version was found
                    assetVersion.version = 0;
                }

                AssetDatabase.AddObjectToAsset(assetVersion, assetPath);
            }

            // Upgrade
            while (assetVersion.version < k_Upgraders.Length)
            {
                k_Upgraders[assetVersion.version](material, shaderID);
                assetVersion.version++;
                wasUpgraded = true;
            }

            if (wasUpgraded)
            {
                EditorUtility.SetDirty(assetVersion);
            }
        }

        static void InitializeLatest(Material material, ShaderID shaderID)
        {
            // newly created shadergraph materials should reset their keywords immediately (in case inspector doesn't get invoked)
            ShaderUtils.ResetMaterialKeywords(material);
        }
    }
}
