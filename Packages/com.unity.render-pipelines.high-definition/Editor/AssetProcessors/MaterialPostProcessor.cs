using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;

// Material property names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    class MaterialModificationProcessor : AssetModificationProcessor
    {
        static void OnWillCreateAsset(string asset)
        {
            if (asset.ToLowerInvariant().EndsWith(".mat"))
            {
                MaterialPostprocessor.s_CreatedAssets.Add(asset);
                return;
            }

            // For .shadergraph assets, this is tricky since the callback will be for the .meta
            // file only as we don't create it with AssetDatabase.CreateAsset but later AddObjectToAsset
            // to the .shadergraph via the importer context.
            // At the time the meta file is created, the .shadergraph is already present
            // but Load*AtPAth(), GetMainAssetTypeAtPath() etc. won't find anything.
            // The GUID is already present though, and we actually use those facts to infer we
            // have a newly created shadergraph.

            //
            // HDMetaData subasset will be included after SG creation anyway so unlike for materials
            // (cf .mat with AssetVersion in OnPostprocessAllAssets) we dont need to manually add a subasset.
            // For adding them to MaterialPostprocessor.s_ImportedAssetThatNeedSaving for SaveAssetsToDisk()
            // to make them editable (flag for checkout), we still detect those here, but not sure this is
            // helpful as right now, even on re-import, a .shadergraph multijson is not rewritten, so only
            // /Library side serialized data is actually changed (including the generated .shader that can
            // also change which is why we run shadergraph reimports), and re-import from the same .shadergraph
            // should be idempotent.
            // In other words, there shouldn't be anything to checkout for the .shadergraph per se.
            //
            if (asset.ToLowerInvariant().EndsWith($".{ShaderGraphImporter.Extension}.meta"))
            {
                var sgPath = System.IO.Path.ChangeExtension(asset, null);
                var importer = AssetImporter.GetAtPath(sgPath);
                var guid = AssetDatabase.AssetPathToGUID(sgPath);
                if (!String.IsNullOrEmpty(guid) && importer == null)
                {
                    MaterialPostprocessor.s_CreatedAssets.Add(sgPath);
                    return;
                }
            }

            // Like stated above, doesnt happen:
            if (asset.ToLowerInvariant().EndsWith($".{ShaderGraphImporter.Extension}"))
            {
                MaterialPostprocessor.s_CreatedAssets.Add(asset);
                return;
            }
        }
    }

    class MaterialReimporter : Editor
    {
        static bool s_NeedToCheckProjSettingExistence = true;
        static bool s_AutoReimportProjectShaderGraphsOnVersionUpdate = false;
        internal static bool s_ReimportShaderGraphDependencyOnMaterialUpdate = true;

        static internal void ReimportAllMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:material", null);
            // There can be several materials subAssets per guid ( ie : FBX files ), remove duplicate guids.
            var distinctGuids = guids.Distinct();

            int materialIdx = 0;
            int totalMaterials = distinctGuids.Count();

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var asset in distinctGuids)
                {
                    materialIdx++;
                    var path = AssetDatabase.GUIDToAssetPath(asset);
                    EditorUtility.DisplayProgressBar("Material Upgrader re-import", string.Format("({0} of {1}) {2}", materialIdx, totalMaterials, path), (float)materialIdx / (float)totalMaterials);
                    AssetDatabase.ImportAsset(path);
                }
            }
            finally
            {
                // Ensure the AssetDatabase knows we're finished editing
                AssetDatabase.StopAssetEditing();
            }

            UnityEditor.EditorUtility.ClearProgressBar();

            MaterialPostprocessor.s_NeedsSavingAssets = true;
        }

        static internal void ReimportAllHDShaderGraphs()
        {
            string[] guids = AssetDatabase.FindAssets("t:shader", null);
            // There can be several materials subAssets per guid ( ie : FBX files ), remove duplicate guids.
            var distinctGuids = guids.Distinct();

            int shaderIdx = 0;
            int totalShaders = distinctGuids.Count();

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var asset in distinctGuids)
                {
                    shaderIdx++;
                    var path = AssetDatabase.GUIDToAssetPath(asset);
                    EditorUtility.DisplayProgressBar("HD ShaderGraph Upgrader re-import", string.Format("({0} of {1}) {2}", shaderIdx, totalShaders, path), (float)shaderIdx / (float)totalShaders);

                    if (CheckHDShaderGraphVersionsForUpgrade(path))
                    {
                        AssetDatabase.ImportAsset(path);
                    }
                }
            }
            finally
            {
                // Ensure the AssetDatabase knows we're finished editing
                AssetDatabase.StopAssetEditing();
            }

            UnityEditor.EditorUtility.ClearProgressBar();

            MaterialPostprocessor.s_NeedsSavingAssets = true;
        }

        internal static bool CheckHDShaderGraphVersionsForUpgrade(string assetPath, Shader shader = null, bool ignoreNonHDRPShaderGraphs = false)
        {
            bool upgradeNeeded = false;

            shader = shader ?? (Shader)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Shader));
            if (ignoreNonHDRPShaderGraphs)
            {
                // Note: this might still be an HDRP shadergraph but old, without HDMetaData,
                // eg using a masternode.
                if (!HDShaderUtils.IsHDRPShaderGraph(shader))
                {
                    return false;
                }
            }
            else
            {
                if (!GraphUtil.IsShaderGraphAsset(shader))
                    return false;
            }

            GUID subTargetGUID;
            if (shader.TryGetMetadataOfType<HDMetadata>(out var hdMetaData))
            {
                subTargetGUID = hdMetaData.subTargetGuid;
            }
            else
            {
                // If HDMetaData doesn't even exist, if it is an HDRP shadergraph,
                // we're sure it's an old one (masternode based)
                return true;
            }

            if (subTargetGUID.Empty())
            {
                upgradeNeeded = true;
                // If subTargetGUID doesn't have any values, it means the SG was last imported
                // pre-plugin materials aware, need upgrade
                return upgradeNeeded;
            }

            bool isSubTargetPlugin = HDShaderUtils.GetMaterialPluginSubTarget(subTargetGUID, out IPluginSubTargetMaterialUtils subTargetMaterialUtils);

            upgradeNeeded |= hdMetaData.hdSubTargetVersion < MigrationDescription.LastVersion<ShaderGraphVersion>();
            if (isSubTargetPlugin)
            {
                // For potential subtarget-specific versioning - ie for plugin subtargets, a single version is enough:
                // When the active subtarget is changed for the HDTarget, the shadergraph is thus reserialized and HDMetaData
                // is also rewritten.
                // This is in contrast with the subasset AssetVersion in materials having a dictionary of versions by GUID.
                // This is needed because a shader can be changed on a material without any hooks to update it, so the material
                // postprocessor which updates the .mat version on behalf of the shaders must make sure to not mis-interpret a version
                // from a previous plugin shader. That's why the AssetVersion asset tracks a dictionary of current (material) versions
                // by guid. (The .version field is of course the main HDRP versioning line as given by k_Migrations in MaterialPostProcessor)

                upgradeNeeded |= (hdMetaData.subTargetSpecificVersion < subTargetMaterialUtils.latestSubTargetVersion);
            }

            return upgradeNeeded;
        }

        [InitializeOnLoadMethod]
        static void RegisterUpgraderReimport()
        {
            EditorApplication.update += () =>
            {
                if (Time.renderedFrameCount > 0)
                {
                    bool reimportAllHDShaderGraphsTriggered = false;
                    bool reimportAllMaterialsTriggered = false;
                    bool fileExist = true;
                    // We check the file existence only once to avoid IO operations every frame.
                    if (s_NeedToCheckProjSettingExistence)
                    {
                        fileExist = System.IO.File.Exists("ProjectSettings/HDRPProjectSettings.asset");
                        s_NeedToCheckProjSettingExistence = false;
                    }

                    //This method is called at opening and when HDRP package change (update of manifest.json)
                    int curMaterialVersion = HDProjectSettings.materialVersionForUpgrade;
                    bool scanMaterialsForUpgradeNeeded = (curMaterialVersion < MaterialPostprocessor.k_Migrations.Length)
                        || (HDProjectSettings.pluginSubTargetLastSeenMaterialVersionsSum < HDShaderUtils.GetHDPluginSubTargetMaterialVersionsSum());

                    var curHDSubTargetVersion = HDProjectSettings.hdShaderGraphLastSeenVersion;
                    bool scanHDShaderGraphsForUpgradeNeeded = (curHDSubTargetVersion < MigrationDescription.LastVersion<ShaderGraphVersion>())
                        || (HDProjectSettings.pluginSubTargetLastSeenSubTargetVersionsSum < HDShaderUtils.GetHDPluginSubTargetVersionsSum());

                    // First test if scan for shadergraph version upgrade might be needed (triggered through their importer)
                    // We do shadergraphs first as the HDMetaData object might need to be updated and the material update might depend
                    // on it.
                    // Note: this auto-update is disabled by default for now.
                    if (s_AutoReimportProjectShaderGraphsOnVersionUpdate && scanHDShaderGraphsForUpgradeNeeded)
                    {
                        string commandLineOptions = System.Environment.CommandLine;
                        bool inTestSuite = commandLineOptions.Contains("-testResults");
                        if (!inTestSuite && fileExist && !Application.isBatchMode)
                        {
                            EditorUtility.DisplayDialog("HDRP ShaderGraph scan for upgrade", "The ShaderGraphs with HDRP SubTarget in your Project were created using an older version of the High Definition Render Pipeline (HDRP)." +
                                " Unity must upgrade them to be compatible with your current version of HDRP. \n" +
                                " Unity will re-import all of the HDRP ShaderGraphs in your project, save them to disk, and check them out in source control if needed.\n" +
                                " Please see the Material upgrade guide in the HDRP documentation for more information.", "Ok");
                        }

                        // When we open a project from scratch all the material have been converted and we don't need to do it two time.
                        // However if we update with an open editor from the package manager we must call reimport.
                        // As we can't know, we are always calling reimport resulting in unecessary work when opening a project.
                        ReimportAllHDShaderGraphs();
                        reimportAllHDShaderGraphsTriggered = true;
                    }
                    // we do else here, as we want to first call SaveAssetsToDisk() for shadergraphs, as it updates the HDProjectSettings version sentinels
                    // (tocheck: it's probably safe to also do materials right after)
                    else if (scanMaterialsForUpgradeNeeded)
                    {
                        string commandLineOptions = System.Environment.CommandLine;
                        bool inTestSuite = commandLineOptions.Contains("-testResults");
                        if (!inTestSuite && fileExist && !Application.isBatchMode)
                        {
                            EditorUtility.DisplayDialog("HDRP Material upgrade", "The Materials in your Project were created using an older version of the High Definition Render Pipeline (HDRP)." +
                                " Unity must upgrade them to be compatible with your current version of HDRP. \n" +
                                " Unity will re-import all of the Materials in your project, save the upgraded Materials to disk, and check them out in source control if needed.\n" +
                                " Please see the Material upgrade guide in the HDRP documentation for more information.", "Ok");
                        }

                        // When we open a project from scratch all the material have been converted and we don't need to do it two time.
                        // However if we update with an open editor from the package manager we must call reimport.
                        // As we can't know, we are always calling reimport resulting in unecessary work when opening a project.
                        ReimportAllMaterials();
                        reimportAllMaterialsTriggered = true;
                    }

                    // Note: A reimport all above should really have gone through all .mat or .shadergraph, because the SaveAssetsToDisk() call will update
                    // our versions in HDProjectSettings on which we base the version change detection and trigger the scan for reimport.
                    // We can still have these two flags set at the same time, however, if the user multi-selects .mat and .shadergraph,
                    // as the MaterialPostProcessor.OnPostprocessAllAssets() will be called for both and set both of those flags if needed.
                    // This should be safe however since by the time we reach this point here, we already checked if we needed a re-import scan just
                    // above.
                    if (MaterialPostprocessor.s_NeedsSavingAssets)
                    {
                        MaterialPostprocessor.SaveAssetsToDisk(updateMaterialLastSeenVersions: reimportAllMaterialsTriggered, updateShaderGraphLastSeenVersions: reimportAllHDShaderGraphsTriggered);
                        reimportAllHDShaderGraphsTriggered = false;
                        reimportAllMaterialsTriggered = false;
                    }
                }
            };
        }
    }

    class MaterialPostprocessor : AssetPostprocessor
    {
        internal static List<string> s_CreatedAssets = new List<string>();
        internal static List<string> s_ImportedAssetThatNeedSaving = new List<string>();
        internal static Dictionary<string, int> s_ImportedMaterialCounter = new Dictionary<string, int>();
        internal static bool s_NeedsSavingAssets = false;

        // Important: This should only be called by the RegisterUpgraderReimport(), ie the shadegraph/material version
        // change detection and project rescan function since SaveAssetsToDisk() here will update the lastseen versions
        // that the upgrader uses to detect version changes.
        static internal void SaveAssetsToDisk(bool updateMaterialLastSeenVersions = true, bool updateShaderGraphLastSeenVersions = false)
        {
            string commandLineOptions = System.Environment.CommandLine;
            bool inTestSuite = commandLineOptions.Contains("-testResults");
            if (inTestSuite)
                return;

            foreach (var asset in s_ImportedAssetThatNeedSaving)
            {
                AssetDatabase.MakeEditable(asset);
            }

            AssetDatabase.SaveAssets();
            //to prevent data loss, only update the last saved version trackers if user applied change and assets are written to
            if (updateShaderGraphLastSeenVersions)
            {
                HDProjectSettings.hdShaderGraphLastSeenVersion = MigrationDescription.LastVersion<ShaderGraphVersion>();
                HDProjectSettings.UpdateLastSeenSubTargetVersionsOfPluginSubTargets();
            }
            if (updateMaterialLastSeenVersions)
            {
                HDProjectSettings.materialVersionForUpgrade = MaterialPostprocessor.k_Migrations.Length;
                HDProjectSettings.UpdateLastSeenMaterialVersionsOfPluginSubTargets();
            }

            s_ImportedAssetThatNeedSaving.Clear();
            s_NeedsSavingAssets = false;
        }

        void OnPostprocessMaterial(Material material)
        {
            if (!HDShaderUtils.IsHDRPShader(material.shader, upgradable: true))
                return;

            if (HDSpeedTree8MaterialUpgrader.IsHDSpeedTree8Material(material))
                SpeedTree8MaterialUpgrader.SpeedTree8MaterialFinalizer(material);

            HDShaderUtils.ResetMaterialKeywords(material);
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var asset in importedAssets)
            {
                // We intercept shadergraphs just to add them to s_ImportedAssetThatNeedSaving to make them editable when we save assets
                if (asset.ToLowerInvariant().EndsWith($".{ShaderGraphImporter.Extension}"))
                {
                    bool justCreated = s_CreatedAssets.Contains(asset);

                    if (!justCreated)
                    {
                        s_ImportedAssetThatNeedSaving.Add(asset);
                        s_NeedsSavingAssets = true;
                    }
                    else
                    {
                        s_CreatedAssets.Remove(asset);
                    }
                    continue;
                }
                else if (!asset.ToLowerInvariant().EndsWith(".mat"))
                {
                    continue;
                }

                // Materials (.mat) post processing:

                var material = (Material)AssetDatabase.LoadAssetAtPath(asset, typeof(Material));

                if (material == null)
                    continue;

                if (MaterialReimporter.s_ReimportShaderGraphDependencyOnMaterialUpdate && GraphUtil.IsShaderGraphAsset(material.shader))
                {
                    // Check first if the HDRP shadergraph assigned needs a migration:
                    // Here ignoreNonHDRPShaderGraphs = false is useful to not ignore non HDRP ShaderGraphs as
                    // the detection is based on the presence of the "HDMetaData" object and old HDRP ShaderGraphs don't have these,
                    // so we can conservatively force a re-import of any ShaderGraphs. Unity might not have reimported such ShaderGraphs
                    // based on declared source dependencies by the ShaderGraphImporter because these might have moved / changed
                    // for old ones. We can cover these cases here.
                    //
                    // Note we could also check this dependency in ReimportAllMaterials but in case a user manually re-imports a material,
                    // (ie the OnPostprocessAllAssets call here is not generated from ReimportAllMaterials())
                    // we would miss re-importing that dependency.
                    if (MaterialReimporter.CheckHDShaderGraphVersionsForUpgrade("", material.shader, ignoreNonHDRPShaderGraphs: false))
                    {
                        s_ImportedMaterialCounter.TryGetValue(asset, out var importCounter);
                        s_ImportedMaterialCounter[asset] = ++importCounter;

                        // CheckHDShaderGraphVersionsForUpgrade always return true if a ShaderGraph don't have an HDMetaData attached
                        // we need a check to avoid importing the same assets over and over again.
                        if (importCounter > 2)
                            continue;

                        var shaderPath = AssetDatabase.GetAssetPath(material.shader.GetInstanceID());
                        AssetDatabase.ImportAsset(shaderPath);

                        // Restart the material import instead of proceeding otherwise the shadergraph will be processed after
                        // (the above ImportAsset(shaderPath) returns before the actual re-importing taking place).
                        AssetDatabase.ImportAsset(asset);
                        continue;
                    }
                }

                if (!HDShaderUtils.IsHDRPShader(material.shader, upgradable: true))
                    continue;


                (HDShaderUtils.ShaderID id, GUID subTargetGUID) = HDShaderUtils.GetShaderIDsFromShader(material.shader);
                var latestVersion = k_Migrations.Length;

                bool isMaterialUsingPlugin = HDShaderUtils.GetMaterialPluginSubTarget(subTargetGUID, out IPluginSubTargetMaterialUtils subTargetMaterialUtils);

                var wasUpgraded = false;
                var assetVersions = AssetDatabase.LoadAllAssetsAtPath(asset);
                AssetVersion assetVersion = null;
                foreach (var subAsset in assetVersions)
                {
                    if (subAsset != null && subAsset.GetType() == typeof(AssetVersion))
                    {
                        assetVersion = subAsset as AssetVersion;
                        break;
                    }
                }

                //subasset not found
                if (!assetVersion)
                {
                    wasUpgraded = true;
                    assetVersion = ScriptableObject.CreateInstance<AssetVersion>();
                    assetVersion.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;
                    if (s_CreatedAssets.Contains(asset))
                    {
                        //just created
                        s_CreatedAssets.Remove(asset);
                        assetVersion.version = latestVersion;
                        if (isMaterialUsingPlugin)
                        {
                            assetVersion.hdPluginSubTargetMaterialVersions.Add(subTargetGUID, subTargetMaterialUtils.latestMaterialVersion);
                        }

                        //[TODO: remove comment once fixed]
                        //due to FB 1175514, this not work. It is being fixed though.
                        //delayed call of the following work in some case and cause infinite loop in other cases.
                        AssetDatabase.AddObjectToAsset(assetVersion, asset);

                        // Init material in case it's used before an inspector window is opened
                        HDShaderUtils.ResetMaterialKeywords(material);
                    }
                    else
                    {
                        //asset exist prior migration
                        assetVersion.version = 0;
                        if (isMaterialUsingPlugin)
                        {
                            assetVersion.hdPluginSubTargetMaterialVersions.Add(subTargetGUID, (int)(PluginMaterial.GenericVersions.NeverMigrated));
                        }
                        AssetDatabase.AddObjectToAsset(assetVersion, asset);
                    }
                }
                // TODO: Maybe systematically remove from s_CreateAssets just in case

                //upgrade
                while (assetVersion.version < latestVersion)
                {
                    k_Migrations[assetVersion.version](material, id);
                    assetVersion.version++;
                    wasUpgraded = true;
                }

                if (isMaterialUsingPlugin)
                {
                    int hdPluginMaterialVersion = (int)(PluginMaterial.GenericVersions.NeverMigrated);
                    bool neverMigrated = (assetVersion.hdPluginSubTargetMaterialVersions.Count == 0)
                        || (false == assetVersion.hdPluginSubTargetMaterialVersions.TryGetValue(subTargetGUID, out hdPluginMaterialVersion));
                    if (neverMigrated)
                    {
                        assetVersion.hdPluginSubTargetMaterialVersions.Add(subTargetGUID, hdPluginMaterialVersion);
                    }

                    if (hdPluginMaterialVersion < subTargetMaterialUtils.latestMaterialVersion)
                    {
                        if (subTargetMaterialUtils.MigrateMaterial(material, hdPluginMaterialVersion))
                        {
                            assetVersion.hdPluginSubTargetMaterialVersions[subTargetGUID] = subTargetMaterialUtils.latestMaterialVersion;
                            wasUpgraded = true;
                        }
                    }
                }

                if (wasUpgraded)
                {
                    EditorUtility.SetDirty(assetVersion);
                    s_ImportedAssetThatNeedSaving.Add(asset);
                    s_NeedsSavingAssets = true;
                }
            }
        }

        public void OnPostprocessSpeedTree(GameObject speedTree)
        {
            SpeedTreeImporter stImporter = assetImporter as SpeedTreeImporter;
            SpeedTree8MaterialUpgrader.PostprocessSpeedTree8Materials(speedTree, stImporter, HDSpeedTree8MaterialUpgrader.HDSpeedTree8MaterialFinalizer);
        }

        // Note: It is not possible to separate migration step by kind of shader
        // used. This is due that user can change shader that material reflect.
        // And when user do this, the material is not reimported and we have no
        // hook on this event.
        // So we must have migration step that work on every materials at once.
        // Which also means that if we want to update only one shader, we need
        // to bump all materials version...
        static internal Action<Material, HDShaderUtils.ShaderID>[] k_Migrations = new Action<Material, HDShaderUtils.ShaderID>[]
        {
            StencilRefactor,
            ZWriteForTransparent,
            RenderQueueUpgrade,
            ShaderGraphStack,
            MoreMaterialSurfaceOptionFromShaderGraph,
            AlphaToMaskUIFix,
            MigrateDecalRenderQueue,
            ExposedDecalInputsFromShaderGraph,
            FixIncorrectEmissiveColorSpace,
            ExposeRefraction,
            MetallicRemapping,
            ForceForwardEmissiveForDeferred,
        };

        #region Migrations

        //example migration method:
        //static void Example(Material material, HDShaderUtils.ShaderID id)
        //{
        //    const string kSupportDecals = "_SupportDecals";
        //    var serializedMaterial = new SerializedObject(material);
        //    if (!TryFindProperty(serializedMaterial, kSupportDecals, SerializedType.Integer, out var property, out _, out _))
        //        return;

        //    // Caution: order of operation is important, we need to keep the current value of the property (if done after it is 0)
        //    // then we remove it and apply the result
        //    // then we can modify the material (otherwise the material change are lost)
        //    bool supportDecal = property.floatValue == 1.0f;

        //    RemoveSerializedInt(serializedMaterial, kSupportDecals);
        //    serializedMaterial.ApplyModifiedProperties();

        //    // We need to reset the custom RenderQueue to take into account the move to specific RenderQueue for Opaque with Decal.
        //    // this should be handled correctly with reset below
        //    HDShaderUtils.ResetMaterialKeywords(material);
        //}
        //}

        static void StencilRefactor(Material material, HDShaderUtils.ShaderID id)
        {
            HDShaderUtils.ResetMaterialKeywords(material);
        }

        static void ZWriteForTransparent(Material material, HDShaderUtils.ShaderID id)
        {
            // For transparent materials, the ZWrite property that is now used is _TransparentZWrite.
            if (material.GetSurfaceType() == SurfaceType.Transparent)
                material.SetFloat(kTransparentZWrite, material.GetZWrite() ? 1.0f : 0.0f);

            HDShaderUtils.ResetMaterialKeywords(material);
        }

        #endregion
        static void RenderQueueUpgrade(Material material, HDShaderUtils.ShaderID id)
        {
            // Replace previous ray tracing render queue for opaque to regular opaque with raytracing
            if (material.renderQueue == ((int)UnityEngine.Rendering.RenderQueue.GeometryLast + 20))
            {
                material.renderQueue = (int)HDRenderQueue.Priority.Opaque;
                material.SetFloat(kRayTracing, 1.0f);
            }
            // Replace previous ray tracing render queue for transparent to regular transparent with raytracing
            else if (material.renderQueue == 3900)
            {
                material.renderQueue = (int)HDRenderQueue.Priority.Transparent;
                material.SetFloat(kRayTracing, 1.0f);
            }

            // In order for the ray tracing keyword to be taken into account, we need to make it dirty so that the parameter is created first
            HDShaderUtils.ResetMaterialKeywords(material);

            // For shader graphs, there is an additional pass we need to do
            if (material.HasProperty("_RenderQueueType"))
            {
                int renderQueueType = (int)material.GetFloat("_RenderQueueType");
                switch (renderQueueType)
                {
                    // This was ray tracing opaque, should go back to opaque
                    case 3:
                    {
                        renderQueueType = 1;
                    }
                    break;
                    // If it was in the transparent range, reduce it by 1
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    {
                        renderQueueType = renderQueueType - 1;
                    }
                    break;
                    // If it was in the ray tracing transparent, should go back to transparent
                    case 8:
                    {
                        renderQueueType = renderQueueType - 4;
                    }
                    break;
                    // If it was in overlay should be reduced by 2
                    case 10:
                    {
                        renderQueueType = renderQueueType - 2;
                    }
                    break;
                    // background, opaque and AfterPostProcessOpaque are not impacted
                    default:
                        break;
                }


                // Push it back to the material
                material.SetFloat("_RenderQueueType", (float)renderQueueType);
            }

            HDShaderUtils.ResetMaterialKeywords(material);
        }

        // properties in this tab should be properties from Unlit or PBR cross pipeline shader
        // that are suppose to be synchronize with the Material during upgrade
        readonly static string[] s_ShadergraphStackFloatPropertiesToSynchronize =
        {
            "_SurfaceType",
            "_BlendMode",
            "_DstBlend",
            "_SrcBlend",
            "_AlphaDstBlend",
            "_AlphaSrcBlend",
            "_AlphaCutoff",
            "_AlphaCutoffEnable",
            "_DoubleSidedEnable",
            "_DoubleSidedNormalMode",
            "_ZWrite", // Needed to fix older bug
            "_RenderQueueType"  // Needed as seems to not reset correctly
        };

        static void ShaderGraphStack(Material material, HDShaderUtils.ShaderID id)
        {
            Shader shader = material.shader;

            if (shader.IsShaderGraphAsset())
            {
                if (shader.TryGetMetadataOfType<HDMetadata>(out var obj))
                {
                    // Material coming from old cross pipeline shader (Unlit and PBR) are not synchronize correctly with their
                    // shader graph. This code below ensure it is
                    if (obj.migrateFromOldCrossPipelineSG) // come from PBR or Unlit cross pipeline SG?
                    {
                        var defaultProperties = new Material(material.shader);

                        foreach (var floatToSync in s_ShadergraphStackFloatPropertiesToSynchronize)
                            if (material.HasProperty(floatToSync))
                                material.SetFloat(floatToSync, defaultProperties.GetFloat(floatToSync));

                        defaultProperties = null;

                        // Postprocess now that material is correctly sync
                        bool isTransparent = material.HasProperty("_SurfaceType") && material.GetFloat("_SurfaceType") > 0.0f;
                        bool alphaTest = material.HasProperty("_AlphaCutoffEnable") && material.GetFloat("_AlphaCutoffEnable") > 0.0f;

                        material.renderQueue = isTransparent ? (int)HDRenderQueue.Priority.Transparent :
                            alphaTest ? (int)HDRenderQueue.Priority.OpaqueAlphaTest : (int)HDRenderQueue.Priority.Opaque;

                        material.SetFloat("_RenderQueueType", isTransparent ? (float)HDRenderQueue.RenderQueueType.Transparent : (float)HDRenderQueue.RenderQueueType.Opaque);
                    }
                }
            }

            HDShaderUtils.ResetMaterialKeywords(material);
        }

        static void MoreMaterialSurfaceOptionFromShaderGraph(Material material, HDShaderUtils.ShaderID id)
        {
            if (material.IsShaderGraph())
            {
                // Synchronize properties we exposed from SG to the material
                ResetFloatProperty(kReceivesSSR);
                ResetFloatProperty(kReceivesSSRTransparent);
                ResetFloatProperty(kEnableDecals);
                ResetFloatProperty(kEnableBlendModePreserveSpecularLighting);
                ResetFloatProperty(kTransparentWritingMotionVec);
                ResetFloatProperty(kAddPrecomputedVelocity);
                ResetFloatProperty(kDepthOffsetEnable);
            }

            void ResetFloatProperty(string propName)
            {
                int propIndex = material.shader.FindPropertyIndex(propName);
                if (propIndex == -1)
                    return;
                float defaultValue = material.shader.GetPropertyDefaultFloatValue(propIndex);
                material.SetFloat(propName, defaultValue);
            }

            HDShaderUtils.ResetMaterialKeywords(material);
        }

        static void AlphaToMaskUIFix(Material material, HDShaderUtils.ShaderID id)
        {
            if (material.HasProperty(kAlphaToMask) && material.HasProperty(kAlphaToMaskInspector))
            {
                material.SetFloat(kAlphaToMaskInspector, material.GetFloat(kAlphaToMask));
                HDShaderUtils.ResetMaterialKeywords(material);
            }
        }

        static void MigrateDecalRenderQueue(Material material, HDShaderUtils.ShaderID id)
        {
            const string kSupportDecals = "_SupportDecals";

            // Take the opportunity to remove _SupportDecals from Unlit as it is not suppose to be here
            if (HDShaderUtils.IsUnlitHDRPShader(material.shader))
            {
                var serializedMaterial = new SerializedObject(material);
                if (TryFindProperty(serializedMaterial, kSupportDecals, SerializedType.Integer, out var property, out _, out _))
                {
                    RemoveSerializedInt(serializedMaterial, kSupportDecals);
                    serializedMaterial.ApplyModifiedProperties();
                }
            }

            if (material.HasProperty(kSupportDecals))
            {
                bool supportDecal = material.GetFloat(kSupportDecals) > 0.0f;

                if (supportDecal)
                {
                    // Update material render queue to be in Decal render queue based on the value of decal property (see HDRenderQueue.cs)
                    if (material.renderQueue == ((int)UnityEngine.Rendering.RenderQueue.Geometry))
                    {
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry + 225;
                    }
                    else if (material.renderQueue == ((int)UnityEngine.Rendering.RenderQueue.AlphaTest))
                    {
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest + 25;
                    }
                }
            }

            HDShaderUtils.ResetMaterialKeywords(material);
        }

        static void ExposedDecalInputsFromShaderGraph(Material material, HDShaderUtils.ShaderID id)
        {
            if (id == HDShaderUtils.ShaderID.Decal)
            {
                // In order for the new properties (kAffectsAlbedo...) to be taken into account, we need to make it dirty so that the parameter is created first
                HDShaderUtils.ResetMaterialKeywords(material);

                var serializedMaterial = new SerializedObject(material);

                // Note: the property must not exist in the .shader for RemoveSerializedFloat to work (otherwise it will be re-added)
                const string kAlbedoMode = "_AlbedoMode";
                float albedoMode = 1.0f;
                if (TryFindProperty(serializedMaterial, kAlbedoMode, SerializedType.Float, out var propertyAlbedoMode, out _, out _))
                {
                    albedoMode = propertyAlbedoMode.floatValue;
                    RemoveSerializedFloat(serializedMaterial, kAlbedoMode);
                }

                // For normal map we don't remove the property _NormalMap but just check if there is a texture assign and then enable _AffectNormal
                const string kNormalMap = "_NormalMap";
                float normalMap = 0.0f;
                if (TryFindProperty(serializedMaterial, kNormalMap, SerializedType.Texture, out var propertyNormalTexture, out _, out _))
                {
                    normalMap = propertyNormalTexture.FindPropertyRelative("m_Texture").objectReferenceValue != null ? 1.0f : 0.0f;
                }

                // For normal map we don't remove the property _NormalMap but just check if there is a texture assign and then enable _AffectNormal
                const string kMaskMap = "_MaskMap";
                float maskMap = 0.0f;
                if (TryFindProperty(serializedMaterial, kMaskMap, SerializedType.Texture, out var propertyMaskMapTexture, out _, out _))
                {
                    maskMap = propertyMaskMapTexture.FindPropertyRelative("m_Texture").objectReferenceValue != null ? 1.0f : 0.0f;
                }

                const string kMaskmapMetal = "_MaskmapMetal";
                float maskMapMetal = 0.0f;
                if (TryFindProperty(serializedMaterial, kMaskmapMetal, SerializedType.Float, out var propertyMaskMapMetal, out _, out _))
                {
                    maskMapMetal = propertyMaskMapMetal.floatValue;
                    RemoveSerializedFloat(serializedMaterial, kMaskmapMetal);
                }

                const string kMaskmapAO = "_MaskmapAO";
                float maskMapAO = 0.0f;
                if (TryFindProperty(serializedMaterial, kMaskmapAO, SerializedType.Float, out var propertyMaskMapAO, out _, out _))
                {
                    maskMapAO = propertyMaskMapAO.floatValue;
                    RemoveSerializedFloat(serializedMaterial, kMaskmapAO);
                }

                const string kMaskmapSmoothness = "_MaskmapSmoothness";
                float maskMapSmoothness = 0.0f;
                if (TryFindProperty(serializedMaterial, kMaskmapSmoothness, SerializedType.Float, out var propertyMaskMapSmoothness, out _, out _))
                {
                    maskMapSmoothness = propertyMaskMapSmoothness.floatValue;
                    RemoveSerializedFloat(serializedMaterial, kMaskmapSmoothness);
                }

                const string kEmissive = "_Emissive";
                float emissive = 0.0f;
                if (TryFindProperty(serializedMaterial, kEmissive, SerializedType.Float, out var propertyEmissive, out _, out _))
                {
                    emissive = propertyEmissive.floatValue;
                    RemoveSerializedFloat(serializedMaterial, kEmissive);
                }

                // Not used anymore, just removed
                const string kMaskBlendMode = "_MaskBlendMode";
                if (TryFindProperty(serializedMaterial, kMaskBlendMode, SerializedType.Float, out var propertyUnused, out _, out _))
                {
                    RemoveSerializedFloat(serializedMaterial, kMaskBlendMode);
                }

                serializedMaterial.ApplyModifiedProperties();

                // Now apply old value to new properties
                const string kAffectAlbedo = "_AffectAlbedo";
                material.SetFloat(kAffectAlbedo, albedoMode);

                const string kAffectNormal = "_AffectNormal";
                material.SetFloat(kAffectNormal, normalMap);

                const string kAffectSmoothness = "_AffectSmoothness";
                material.SetFloat(kAffectSmoothness, maskMapSmoothness * maskMap);

                const string kAffectMetal = "_AffectMetal";
                material.SetFloat(kAffectMetal, maskMapMetal * maskMap);

                const string kAffectAO = "_AffectAO";
                material.SetFloat(kAffectAO, maskMapAO * maskMap);

                const string kAffectEmission = "_AffectEmission";
                material.SetFloat(kAffectEmission, emissive);

                // We can't erase obsolete disabled pass from already existing Material, so we need to re-enable all of them
                const string s_MeshDecalsMStr = "DBufferMesh_M";
                const string s_MeshDecalsSStr = "DBufferMesh_S";
                const string s_MeshDecalsMSStr = "DBufferMesh_MS";
                const string s_MeshDecalsAOStr = "DBufferMesh_AO";
                const string s_MeshDecalsMAOStr = "DBufferMesh_MAO";
                const string s_MeshDecalsAOSStr = "DBufferMesh_AOS";
                const string s_MeshDecalsMAOSStr = "DBufferMesh_MAOS";
                const string s_MeshDecals3RTStr = "DBufferMesh_3RT";
                const string s_MeshDecalsForwardEmissive = "Mesh_Emissive";

                material.SetShaderPassEnabled(s_MeshDecalsMStr, true);
                material.SetShaderPassEnabled(s_MeshDecalsSStr, true);
                material.SetShaderPassEnabled(s_MeshDecalsMSStr, true);
                material.SetShaderPassEnabled(s_MeshDecalsAOStr, true);
                material.SetShaderPassEnabled(s_MeshDecalsMAOStr, true);
                material.SetShaderPassEnabled(s_MeshDecalsAOSStr, true);
                material.SetShaderPassEnabled(s_MeshDecalsMAOSStr, true);
                material.SetShaderPassEnabled(s_MeshDecals3RTStr, true);
                material.SetShaderPassEnabled(s_MeshDecalsForwardEmissive, true);
            }

            if (id == HDShaderUtils.ShaderID.SG_Decal)
            {
                // We can't erase obsolete disabled pass from already existing Material, so we need to re-enable all of them
                const string s_ShaderGraphMeshDecals4RT = "ShaderGraph_DBufferMesh4RT";
                const string s_ShaderGraphMeshDecals3RT = "ShaderGraph_DBufferMesh3RT";
                const string s_ShaderGraphMeshDecalForwardEmissive = "ShaderGraph_MeshEmissive";

                material.SetShaderPassEnabled(s_ShaderGraphMeshDecals4RT, true);
                material.SetShaderPassEnabled(s_ShaderGraphMeshDecals3RT, true);
                material.SetShaderPassEnabled(s_ShaderGraphMeshDecalForwardEmissive, true);
            }

            if (id == HDShaderUtils.ShaderID.Decal || id == HDShaderUtils.ShaderID.SG_Decal)
            {
                HDShaderUtils.ResetMaterialKeywords(material);
            }
        }

        static void FixIncorrectEmissiveColorSpace(Material material, HDShaderUtils.ShaderID id)
        {
            // kEmissiveColorLDR wasn't correctly converted to linear color space.
            // so here we adjust the value of kEmissiveColorLDR to compensate. But only if not using a HDR Color
            const string kUseEmissiveIntensity = "_UseEmissiveIntensity";

            if (material.HasProperty(kUseEmissiveIntensity) && material.GetInt(kUseEmissiveIntensity) == 1)
            {
                const string kEmissiveColorLDR = "_EmissiveColorLDR";
                const string kEmissiveColor = "_EmissiveColor";
                const string kEmissiveIntensity = "_EmissiveIntensity";

                if (material.HasProperty(kEmissiveColorLDR) && material.HasProperty(kEmissiveIntensity) && material.HasProperty(kEmissiveColor))
                {
                    // Important:  The color picker for kEmissiveColorLDR is LDR and in sRGB color space but Unity don't perform any color space conversion in the color
                    // picker BUT only when sending the color data to the shader... So as we are doing our own calculation here in C#, we must do the conversion ourselves.
                    Color emissiveColorLDR = material.GetColor(kEmissiveColorLDR);
                    Color emissiveColorLDRsRGB = new Color(Mathf.LinearToGammaSpace(emissiveColorLDR.r), Mathf.LinearToGammaSpace(emissiveColorLDR.g), Mathf.LinearToGammaSpace(emissiveColorLDR.b));
                    material.SetColor(kEmissiveColorLDR, emissiveColorLDRsRGB);
                }

                // Reset the value of kEmissiveColor
                material.UpdateEmissiveColorFromIntensityAndEmissiveColorLDR();
            }
        }

        static void ExposeRefraction(Material material, HDShaderUtils.ShaderID id)
        {
            // Lit SG now have a shader feature for refraction instead of an hardcoded material
            if (id == HDShaderUtils.ShaderID.SG_Lit)
            {
                // Sync the default refraction model from the shader graph to the shader
                // We need to do this because the material may already have a refraction model information (from the Lit)
                // In order to not break the rendering of the material, we patch the refraction model:
                if (material.HasProperty(kRefractionModel))
                {
                    var refractionModel = material.shader.GetPropertyDefaultFloatValue(material.shader.FindPropertyIndex(kRefractionModel));
                    material.SetFloat(kRefractionModel, refractionModel);
                }
                HDShaderUtils.ResetMaterialKeywords(material);
            }
        }

        static void MetallicRemapping(Material material, HDShaderUtils.ShaderID id)
        {
            const string kMetallicRemapMax = "_MetallicRemapMax";

            // Lit shaders now have metallic remapping for the mask map
            if (id == HDShaderUtils.ShaderID.Lit || id == HDShaderUtils.ShaderID.LitTesselation
                || id == HDShaderUtils.ShaderID.LayeredLit || id == HDShaderUtils.ShaderID.LayeredLitTesselation)
            {
                const string kMetallic = "_Metallic";
                if (material.HasProperty(kMetallic) && material.HasProperty(kMetallicRemapMax))
                {
                    var metallic = material.GetFloat(kMetallic);
                    material.SetFloat(kMetallicRemapMax, metallic);
                }
            }
            else if (id == HDShaderUtils.ShaderID.Decal)
            {
                HDShaderUtils.ResetMaterialKeywords(material);
                var serializedMaterial = new SerializedObject(material);

                const string kMetallicScale = "_MetallicScale";
                float metallicScale = 1.0f;
                if (TryFindProperty(serializedMaterial, kMetallicScale, SerializedType.Float, out var propertyMetallicScale, out _, out _))
                {
                    metallicScale = propertyMetallicScale.floatValue;
                    RemoveSerializedFloat(serializedMaterial, kMetallicScale);
                }

                serializedMaterial.ApplyModifiedProperties();

                material.SetFloat(kMetallicRemapMax, metallicScale);
            }
        }

        // This function below is not used anymore - the code have been removed - however we must maintain the function to keep already converted Material coherent
        // As it doesn't do anything special, there is no problem to let it
        static void ForceForwardEmissiveForDeferred(Material material, HDShaderUtils.ShaderID id)
        {
            // Force Forward emissive for deferred pass is only setup for Lit shader
            if (id == HDShaderUtils.ShaderID.SG_Lit || id == HDShaderUtils.ShaderID.Lit || id == HDShaderUtils.ShaderID.LitTesselation
                || id == HDShaderUtils.ShaderID.LayeredLit || id == HDShaderUtils.ShaderID.LayeredLitTesselation)
            {
                HDShaderUtils.ResetMaterialKeywords(material);
            }
        }

        #region Serialization_API
        //Methods in this region interact on the serialized material
        //without filtering on what used shader knows

        enum SerializedType
        {
            Boolean,
            Integer,
            Float,
            Vector,
            Color,
            Texture
        }

        // do not use directly in migration function
        static bool TryFindBase(SerializedObject material, SerializedType type, out SerializedProperty propertyBase)
        {
            propertyBase = material.FindProperty("m_SavedProperties");

            switch (type)
            {
                case SerializedType.Boolean:
                case SerializedType.Integer:
                case SerializedType.Float:
                    propertyBase = propertyBase.FindPropertyRelative("m_Floats");
                    return true;
                case SerializedType.Color:
                case SerializedType.Vector:
                    propertyBase = propertyBase.FindPropertyRelative("m_Colors");
                    return true;
                case SerializedType.Texture:
                    propertyBase = propertyBase.FindPropertyRelative("m_TexEnvs");
                    return true;
            }

            return false;
        }

        static SerializedProperty FindBase(SerializedObject material, SerializedType type)
        {
            if (!TryFindBase(material, type, out var propertyBase))
                throw new ArgumentException($"Unknown SerializedType {type}");
            return propertyBase;
        }

        // do not use directly in migration function
        static bool TryFindProperty(SerializedObject material, string propertyName, SerializedType type, out SerializedProperty property, out int indexOf, out SerializedProperty propertyBase)
        {
            propertyBase = FindBase(material, type);

            property = null;
            int maxSearch = propertyBase.arraySize;
            indexOf = 0;
            for (; indexOf < maxSearch; ++indexOf)
            {
                property = propertyBase.GetArrayElementAtIndex(indexOf);
                if (property.FindPropertyRelative("first").stringValue == propertyName)
                    break;
            }

            if (indexOf == maxSearch)
                return false;

            property = property.FindPropertyRelative("second");
            return true;
        }

        static (SerializedProperty property, int index, SerializedProperty parent) FindProperty(SerializedObject material, string propertyName, SerializedType type)
        {
            if (!TryFindProperty(material, propertyName, type, out var property, out var index, out var parent))
                throw new ArgumentException($"Unknown property: {propertyName}");

            return (property, index, parent);
        }

        static Color GetSerializedColor(SerializedObject material, string propertyName)
            => FindProperty(material, propertyName, SerializedType.Color)
            .property.colorValue;

        static bool GetSerializedBoolean(SerializedObject material, string propertyName)
            => FindProperty(material, propertyName, SerializedType.Boolean)
            .property.floatValue > 0.5f;

        static int GetSerializedInt(SerializedObject material, string propertyName)
            => (int)FindProperty(material, propertyName, SerializedType.Integer)
            .property.floatValue;

        static Vector2Int GetSerializedVector2Int(SerializedObject material, string propertyName)
        {
            var property = FindProperty(material, propertyName, SerializedType.Vector).property;
            return new Vector2Int(
                (int)property.FindPropertyRelative("r").floatValue,
                (int)property.FindPropertyRelative("g").floatValue);
        }

        static Vector3Int GetSerializedVector3Int(SerializedObject material, string propertyName)
        {
            var property = FindProperty(material, propertyName, SerializedType.Vector).property;
            return new Vector3Int(
                (int)property.FindPropertyRelative("r").floatValue,
                (int)property.FindPropertyRelative("g").floatValue,
                (int)property.FindPropertyRelative("b").floatValue);
        }

        static float GetSerializedFloat(SerializedObject material, string propertyName)
            => FindProperty(material, propertyName, SerializedType.Float)
            .property.floatValue;

        static Vector2 GetSerializedVector2(SerializedObject material, string propertyName)
        {
            var property = FindProperty(material, propertyName, SerializedType.Vector).property;
            return new Vector2(
                property.FindPropertyRelative("r").floatValue,
                property.FindPropertyRelative("g").floatValue);
        }

        static Vector3 GetSerializedVector3(SerializedObject material, string propertyName)
        {
            var property = FindProperty(material, propertyName, SerializedType.Vector).property;
            return new Vector3(
                property.FindPropertyRelative("r").floatValue,
                property.FindPropertyRelative("g").floatValue,
                property.FindPropertyRelative("b").floatValue);
        }

        static Vector4 GetSerializedVector4(SerializedObject material, string propertyName)
        {
            var property = FindProperty(material, propertyName, SerializedType.Vector).property;
            return new Vector4(
                property.FindPropertyRelative("r").floatValue,
                property.FindPropertyRelative("g").floatValue,
                property.FindPropertyRelative("b").floatValue,
                property.FindPropertyRelative("a").floatValue);
        }

        static (Texture texture, Vector2 scale, Vector2 offset) GetSerializedTexture(SerializedObject material, string propertyName)
        {
            var property = FindProperty(material, propertyName, SerializedType.Texture).property;
            return (
                property.FindPropertyRelative("m_Texture").objectReferenceValue as Texture,
                property.FindPropertyRelative("m_Scale").vector2Value,
                property.FindPropertyRelative("m_Offset").vector2Value);
        }

        static void RemoveSerializedColor(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Color);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedBoolean(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Boolean);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedInt(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Integer);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedVector2Int(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Vector);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedVector3Int(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Vector);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedFloat(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Float);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedVector2(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Vector);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedVector3(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Vector);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedVector4(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Vector);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedTexture(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Texture);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void AddSerializedColor(SerializedObject material, string name, Color value)
        {
            var propertyBase = FindBase(material, SerializedType.Color);
            int lastPos = propertyBase.arraySize;
            propertyBase.InsertArrayElementAtIndex(lastPos);
            var newProperty = propertyBase.GetArrayElementAtIndex(lastPos);
            newProperty.FindPropertyRelative("first").stringValue = name;
            newProperty.FindPropertyRelative("second").colorValue = value;
        }

        static void AddSerializedBoolean(SerializedObject material, string name, bool value)
        {
            var propertyBase = FindBase(material, SerializedType.Boolean);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            newProperty.FindPropertyRelative("second").floatValue = value ? 1f : 0f;
        }

        static void AddSerializedInt(SerializedObject material, string name, int value)
        {
            var propertyBase = FindBase(material, SerializedType.Integer);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            newProperty.FindPropertyRelative("second").floatValue = value;
        }

        static void AddSerializedVector2Int(SerializedObject material, string name, Vector2Int value)
        {
            var propertyBase = FindBase(material, SerializedType.Vector);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            var container = newProperty.FindPropertyRelative("second");
            container.FindPropertyRelative("r").floatValue = value.x;
            container.FindPropertyRelative("g").floatValue = value.y;
            container.FindPropertyRelative("b").floatValue = 0;
            container.FindPropertyRelative("a").floatValue = 0;
        }

        static void AddSerializedVector3Int(SerializedObject material, string name, Vector3Int value)
        {
            var propertyBase = FindBase(material, SerializedType.Vector);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            var container = newProperty.FindPropertyRelative("second");
            container.FindPropertyRelative("r").floatValue = value.x;
            container.FindPropertyRelative("g").floatValue = value.y;
            container.FindPropertyRelative("b").floatValue = value.z;
            container.FindPropertyRelative("a").floatValue = 0;
        }

        static void AddSerializedFloat(SerializedObject material, string name, float value)
        {
            var propertyBase = FindBase(material, SerializedType.Float);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            newProperty.FindPropertyRelative("second").floatValue = value;
        }

        static void AddSerializedVector2(SerializedObject material, string name, Vector2 value)
        {
            var propertyBase = FindBase(material, SerializedType.Vector);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            var container = newProperty.FindPropertyRelative("second");
            container.FindPropertyRelative("r").floatValue = value.x;
            container.FindPropertyRelative("g").floatValue = value.y;
            container.FindPropertyRelative("b").floatValue = 0;
            container.FindPropertyRelative("a").floatValue = 0;
        }

        static void AddSerializedVector3(SerializedObject material, string name, Vector3 value)
        {
            var propertyBase = FindBase(material, SerializedType.Vector);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            var container = newProperty.FindPropertyRelative("second");
            container.FindPropertyRelative("r").floatValue = value.x;
            container.FindPropertyRelative("g").floatValue = value.y;
            container.FindPropertyRelative("b").floatValue = value.z;
            container.FindPropertyRelative("a").floatValue = 0;
        }

        static void AddSerializedVector4(SerializedObject material, string name, Vector4 value)
        {
            var propertyBase = FindBase(material, SerializedType.Vector);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            var container = newProperty.FindPropertyRelative("second");
            container.FindPropertyRelative("r").floatValue = value.x;
            container.FindPropertyRelative("g").floatValue = value.y;
            container.FindPropertyRelative("b").floatValue = value.z;
            container.FindPropertyRelative("a").floatValue = value.w;
        }

        static void AddSerializedTexture(SerializedObject material, string name, Texture texture, Vector2 scale, Vector2 offset)
        {
            var propertyBase = FindBase(material, SerializedType.Texture);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            var container = newProperty.FindPropertyRelative("second");
            container.FindPropertyRelative("m_Texture").objectReferenceValue = texture;
            container.FindPropertyRelative("m_Scale").vector2Value = scale;
            container.FindPropertyRelative("m_Offset").vector2Value = offset;
        }

        static void RenameSerializedScalar(SerializedObject material, string oldName, string newName)
        {
            var res = FindProperty(material, oldName, SerializedType.Float);
            var value = res.property.floatValue;
            res.parent.InsertArrayElementAtIndex(0);
            var newProperty = res.parent.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = newName;
            newProperty.FindPropertyRelative("second").floatValue = value;
            res.parent.DeleteArrayElementAtIndex(res.index + 1);
        }

        static void RenameSerializedVector(SerializedObject material, string oldName, string newName)
        {
            var res = FindProperty(material, oldName, SerializedType.Vector);
            var valueX = res.property.FindPropertyRelative("r").floatValue;
            var valueY = res.property.FindPropertyRelative("g").floatValue;
            var valueZ = res.property.FindPropertyRelative("b").floatValue;
            var valueW = res.property.FindPropertyRelative("a").floatValue;
            res.parent.InsertArrayElementAtIndex(0);
            var newProperty = res.parent.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = newName;
            var container = newProperty.FindPropertyRelative("second");
            container.FindPropertyRelative("r").floatValue = valueX;
            container.FindPropertyRelative("g").floatValue = valueY;
            container.FindPropertyRelative("b").floatValue = valueZ;
            container.FindPropertyRelative("a").floatValue = valueW;
            res.parent.DeleteArrayElementAtIndex(res.index + 1);
        }

        static void RenameSerializedTexture(SerializedObject material, string oldName, string newName)
        {
            var res = FindProperty(material, oldName, SerializedType.Texture);
            var texture = res.property.FindPropertyRelative("m_Texture").objectReferenceValue;
            var scale = res.property.FindPropertyRelative("m_Scale").vector2Value;
            var offset = res.property.FindPropertyRelative("m_Offset").vector2Value;
            res.parent.InsertArrayElementAtIndex(0);
            var newProperty = res.parent.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = newName;
            var container = newProperty.FindPropertyRelative("second");
            container.FindPropertyRelative("m_Texture").objectReferenceValue = texture;
            container.FindPropertyRelative("m_Scale").vector2Value = scale;
            container.FindPropertyRelative("m_Offset").vector2Value = offset;
            res.parent.DeleteArrayElementAtIndex(res.index + 1);
        }

        #endregion
    }
}
