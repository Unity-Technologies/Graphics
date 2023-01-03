using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering.Analytics;
using UnityEditor.Rendering.Universal.Analytics;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Unity.Rendering.Universal.ShaderUtils;
using BlendMode = UnityEngine.Rendering.BlendMode;

namespace UnityEditor.Rendering.Universal
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
            AssetReimportUtils.ReimportAll<Material>(out var duration, out var numberOfAssetsReimported);
            AssetReimporterAnalytic.Send<Material>(duration, numberOfAssetsReimported);
            MaterialPostprocessor.s_NeedsSavingAssets = true;
        }

        [InitializeOnLoadMethod]
        static void RegisterUpgraderReimport()
        {
            EditorApplication.update += () =>
            {
                if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset universalRenderPipeline)
                    return;

                if (Time.renderedFrameCount > 0)
                {
                    bool fileExist = true;
                    // We check the file existence only once to avoid IO operations every frame.
                    if (s_NeedToCheckProjSettingExistence)
                    {
                        fileExist = System.IO.File.Exists(UniversalProjectSettings.filePath);
                        s_NeedToCheckProjSettingExistence = false;
                    }

                    //This method is called at opening and when URP package change (update of manifest.json)
                    var curUpgradeVersion = UniversalProjectSettings.materialVersionForUpgrade;

                    if (curUpgradeVersion != MaterialPostprocessor.k_Upgraders.Length)
                    {
                        string commandLineOptions = Environment.CommandLine;
                        bool inTestSuite = commandLineOptions.Contains("-testResults");
                        if (!inTestSuite && fileExist)
                        {
                            EditorUtility.DisplayDialog("URP Material upgrade", "The Materials in your Project were created using an older version of the Universal Render Pipeline (URP)." +
                                " Unity must upgrade them to be compatible with your current version of URP. \n" +
                                " Unity will re-import all of the Materials in your project, save the upgraded Materials to disk, and check them out in source control if needed.\n" +
                                " Please see the Material upgrade guide in the URP documentation for more information.", "Ok");
                        }

                        ReimportAllMaterials();
                    }

                    if (MaterialPostprocessor.s_NeedsSavingAssets)
                        MaterialPostprocessor.SaveAssetsToDisk();
                }
            };
        }
    }

    class MaterialPostprocessor : AssetPostprocessor
    {
        public static List<string> s_CreatedAssets = new List<string>();
        internal static List<string> s_ImportedAssetThatNeedSaving = new List<string>();
        internal static bool s_NeedsSavingAssets = false;

        internal static readonly Action<Material, ShaderID>[] k_Upgraders = { UpgradeV1, UpgradeV2, UpgradeV3, UpgradeV4, UpgradeV5, UpgradeV6, UpgradeV7 };

        static internal void SaveAssetsToDisk()
        {
            string commandLineOptions = System.Environment.CommandLine;
            bool inTestSuite = commandLineOptions.Contains("-testResults");
            if (inTestSuite)
            {
                // Need to update material version to prevent infinite loop in the upgrader
                // when running tests.
                UniversalProjectSettings.materialVersionForUpgrade = k_Upgraders.Length;
                return;
            }

            foreach (var asset in s_ImportedAssetThatNeedSaving)
            {
                AssetDatabase.MakeEditable(asset);
            }

            AssetDatabase.SaveAssets();
            //to prevent data loss, only update the saved version if user applied change and assets are written to
            UniversalProjectSettings.materialVersionForUpgrade = k_Upgraders.Length;
            UniversalProjectSettings.Save();

            s_ImportedAssetThatNeedSaving.Clear();
            s_NeedsSavingAssets = false;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            string upgradeLog = "";
            var upgradeCount = 0;

            foreach (var asset in importedAssets)
            {
                // we only care about materials
                if (!asset.EndsWith(".mat", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                // load the material and look for it's Universal ShaderID
                // we only care about versioning materials using a known Universal ShaderID
                // this skips any materials that only target other render pipelines, are user shaders,
                // or are shaders we don't care to version
                var material = (Material)AssetDatabase.LoadAssetAtPath(asset, typeof(Material));
                var shaderID = GetShaderID(material.shader);
                if (shaderID == ShaderID.Unknown)
                    continue;

                var wasUpgraded = false;
                var debug = "\n" + material.name + "(" + shaderID + ")";

                // look for the Universal AssetVersion
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
                        InitializeLatest(material, shaderID);
                        debug += " initialized.";
                    }
                    else
                    {
                        if (shaderID.IsShaderGraph())
                        {
                            // ShaderGraph materials NEVER had asset versioning applied prior to version 5.
                            // so if we see a ShaderGraph material with no assetVersion, set it to 5 to ensure we apply all necessary versions.
                            assetVersion.version = 5;
                            debug += $" shadergraph material assumed to be version 5 due to missing version.";
                        }
                        else
                        {
                            assetVersion.version = UniversalProjectSettings.materialVersionForUpgrade;
                            debug += $" assumed to be version {UniversalProjectSettings.materialVersionForUpgrade} due to missing version.";
                        }
                    }

                    assetVersion.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;
                    AssetDatabase.AddObjectToAsset(assetVersion, asset);
                }

                while (assetVersion.version >= 0 && assetVersion.version < k_Upgraders.Length)
                {
                    k_Upgraders[assetVersion.version](material, shaderID);
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

            // Uncomment to show upgrade debug logs
            //if (!string.IsNullOrEmpty(upgradeLog))
            //    Debug.Log("UniversalRP Material log: " + upgradeLog);
        }

        static void InitializeLatest(Material material, ShaderID id)
        {
            // newly created materials should reset their keywords immediately (in case inspector doesn't get invoked)
            Unity.Rendering.Universal.ShaderUtils.UpdateMaterial(material, MaterialUpdateType.CreatedNewMaterial, id);
        }

        static void UpgradeV1(Material material, ShaderID shaderID)
        {
            if (shaderID.IsShaderGraph())
                return;

            var shaderPath = ShaderUtils.GetShaderPath((ShaderPathID)shaderID);
            var upgradeFlag = MaterialUpgrader.UpgradeFlags.LogMessageWhenNoUpgraderFound;

            switch (shaderID)
            {
                case ShaderID.Unlit:
                    MaterialUpgrader.Upgrade(material, new UnlitUpdaterV1(shaderPath), upgradeFlag);
                    UnlitShader.SetMaterialKeywords(material);
                    break;
                case ShaderID.SimpleLit:
                    MaterialUpgrader.Upgrade(material, new SimpleLitUpdaterV1(shaderPath), upgradeFlag);
                    SimpleLitShader.SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords);
                    break;
                case ShaderID.Lit:
                    MaterialUpgrader.Upgrade(material, new LitUpdaterV1(shaderPath), upgradeFlag);
                    LitShader.SetMaterialKeywords(material, LitGUI.SetMaterialKeywords);
                    break;
                case ShaderID.ParticlesLit:
                    MaterialUpgrader.Upgrade(material, new ParticleUpdaterV1(shaderPath), upgradeFlag);
                    ParticlesLitShader.SetMaterialKeywords(material, LitGUI.SetMaterialKeywords, ParticleGUI.SetMaterialKeywords);
                    break;
                case ShaderID.ParticlesSimpleLit:
                    MaterialUpgrader.Upgrade(material, new ParticleUpdaterV1(shaderPath), upgradeFlag);
                    ParticlesSimpleLitShader.SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords, ParticleGUI.SetMaterialKeywords);
                    break;
                case ShaderID.ParticlesUnlit:
                    MaterialUpgrader.Upgrade(material, new ParticleUpdaterV1(shaderPath), upgradeFlag);
                    ParticlesUnlitShader.SetMaterialKeywords(material, null, ParticleGUI.SetMaterialKeywords);
                    break;
            }
        }

        static void UpgradeV2(Material material, ShaderID shaderID)
        {
            if (shaderID.IsShaderGraph())
                return;

            // fix 50 offset on shaders
            if (material.HasProperty("_QueueOffset"))
                BaseShaderGUI.SetupMaterialBlendMode(material);
        }

        static void UpgradeV3(Material material, ShaderID shaderID)
        {
            if (shaderID.IsShaderGraph())
                return;

            switch (shaderID)
            {
                case ShaderID.Lit:
                case ShaderID.SimpleLit:
                case ShaderID.ParticlesLit:
                case ShaderID.ParticlesSimpleLit:
                case ShaderID.ParticlesUnlit:
                    var propertyID = Shader.PropertyToID("_EmissionColor");
                    if (material.HasProperty(propertyID))
                    {
                        // In older version there was a bug that these shaders did not had HDR attribute on emission property.
                        // This caused emission color to be converted from gamma to linear space.
                        // In order to avoid visual regression on older projects we will do gamma to linear conversion here.
                        var emissionGamma = material.GetColor(propertyID);
                        var emissionLinear = emissionGamma.linear;
                        material.SetColor(propertyID, emissionLinear);
                    }
                    break;
            }
        }

        static void UpgradeV4(Material material, ShaderID shaderID)
        { }

        static void UpgradeV5(Material material, ShaderID shaderID)
        {
            if (shaderID.IsShaderGraph())
                return;

            var propertyID = Shader.PropertyToID("_Surface");
            if (material.HasProperty(propertyID))
            {
                float surfaceType = material.GetFloat(propertyID);
                if (surfaceType >= 1.0f)
                {
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                }
                else
                {
                    material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                }
            }
        }

        // Separate Preserve Specular Lighting from Premultiplied blend mode.
        // Update materials params for backwards compatibility. (Keep the same end result).
        // - Previous (incorrect) premultiplied blend mode --> Alpha blend mode + Preserve Specular Lighting
        // - Otherwise keep the blend mode and disable Preserve Specular Lighting
        // - Correct premultiply mode is not possible in V5.
        //
        // This is run both hand-written and shadergraph materials.
        //
        // Hand-written and overridable shadergraphs always have blendModePreserveSpecular property, which
        // is assumed to be new since we only run this for V5 -> V6 upgrade.
        //
        // Fixed shadergraphs do not have this keyword and are filtered out.
        // The blend mode is baked in the generated shader, so there's no material properties to be upgraded.
        // The shadergraph upgrade on re-import will handle the fixed shadergraphs.
        static void UpgradeV6(Material material, ShaderID shaderID)
        {
            var surfaceTypePID = Shader.PropertyToID(Property.SurfaceType);
            bool isTransparent = material.HasProperty(surfaceTypePID) && material.GetFloat(surfaceTypePID) >= 1.0f;

            if (isTransparent)
            {
                if (shaderID == ShaderID.Unlit)
                {
                    var blendModePID = Shader.PropertyToID(Property.BlendMode);
                    var blendMode = (BaseShaderGUI.BlendMode)material.GetFloat(blendModePID);

                    // Premultiply used to be "Premultiply (* alpha in shader)" aka Alpha blend
                    if (blendMode == BaseShaderGUI.BlendMode.Premultiply)
                        material.SetFloat(blendModePID, (float)BaseShaderGUI.BlendMode.Alpha);
                }
                else
                {
                    var blendModePreserveSpecularPID = Shader.PropertyToID(Property.BlendModePreserveSpecular);
                    if (material.HasProperty(blendModePreserveSpecularPID))
                    {
                        var blendModePID = Shader.PropertyToID(Property.BlendMode);
                        var blendMode = (BaseShaderGUI.BlendMode)material.GetFloat(blendModePID);
                        if (blendMode == BaseShaderGUI.BlendMode.Premultiply)
                        {
                            material.SetFloat(blendModePID, (float)BaseShaderGUI.BlendMode.Alpha);
                            material.SetFloat(blendModePreserveSpecularPID, 1.0f);
                        }
                        else
                        {
                            material.SetFloat(blendModePreserveSpecularPID, 0.0f);
                        }

                        BaseShaderGUI.SetMaterialKeywords(material);
                    }
                }
            }
        }

        // Upgrades alpha-clipped materials to include logic for automatic alpha-to-coverage support
        static void UpgradeV7(Material material, ShaderID shaderID)
        {
            var surfacePropertyID = Shader.PropertyToID(Property.SurfaceType);
            var alphaClipPropertyID = Shader.PropertyToID(Property.AlphaClip);
            var alphaToMaskPropertyID = Shader.PropertyToID(Property.AlphaToMask);
            if (material.HasProperty(surfacePropertyID) &&
                material.HasProperty(alphaClipPropertyID) &&
                material.HasProperty(alphaToMaskPropertyID))
            {
                bool isOpaque = material.GetFloat(surfacePropertyID) < 1.0f;
                bool isAlphaClipEnabled = material.GetFloat(alphaClipPropertyID) > 0.0f;

                float alphaToMask = (isOpaque && isAlphaClipEnabled) ? 1.0f : 0.0f;

                material.SetFloat(alphaToMaskPropertyID, alphaToMask);
            }
        }
    }

    // Upgraders v1
    #region UpgradersV1

    internal class LitUpdaterV1 : MaterialUpgrader
    {
        public static void UpdateLitDetails(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            if (material.GetTexture("_MetallicGlossMap") || material.GetTexture("_SpecGlossMap") || material.GetFloat("_SmoothnessTextureChannel") >= 0.5f)
                material.SetFloat("_Smoothness", material.GetFloat("_GlossMapScale"));
            else
                material.SetFloat("_Smoothness", material.GetFloat("_Glossiness"));
        }

        public LitUpdaterV1(string oldShaderName)
        {
            if (oldShaderName == null)
                throw new ArgumentNullException("oldShaderName");

            string standardShaderPath = ShaderUtils.GetShaderPath(ShaderPathID.Lit);

            RenameShader(oldShaderName, standardShaderPath, UpdateLitDetails);

            RenameTexture("_MainTex", "_BaseMap");
            RenameColor("_Color", "_BaseColor");
            RenameFloat("_GlossyReflections", "_EnvironmentReflections");
        }
    }

    internal class UnlitUpdaterV1 : MaterialUpgrader
    {
        static Shader bakedLit = Shader.Find(ShaderUtils.GetShaderPath(ShaderPathID.BakedLit));

        public static void UpgradeToUnlit(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            if (material.GetFloat("_SampleGI") != 0)
            {
                material.shader = bakedLit;
                material.EnableKeyword("_NORMALMAP");
            }
        }

        public UnlitUpdaterV1(string oldShaderName)
        {
            if (oldShaderName == null)
                throw new ArgumentNullException("oldShaderName");

            RenameShader(oldShaderName, ShaderUtils.GetShaderPath(ShaderPathID.Unlit), UpgradeToUnlit);

            RenameTexture("_MainTex", "_BaseMap");
            RenameColor("_Color", "_BaseColor");
        }
    }

    internal class SimpleLitUpdaterV1 : MaterialUpgrader
    {
        public SimpleLitUpdaterV1(string oldShaderName)
        {
            if (oldShaderName == null)
                throw new ArgumentNullException("oldShaderName");

            RenameShader(oldShaderName, ShaderUtils.GetShaderPath(ShaderPathID.SimpleLit), UpgradeToSimpleLit);

            RenameTexture("_MainTex", "_BaseMap");
            RenameColor("_Color", "_BaseColor");
            RenameFloat("_SpecSource", "_SpecularHighlights");
            RenameFloat("_Shininess", "_Smoothness");
        }

        public static void UpgradeToSimpleLit(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            var smoothnessSource = 1 - (int)material.GetFloat("_GlossinessSource");
            material.SetFloat("_SmoothnessSource", smoothnessSource);
            if (material.GetTexture("_SpecGlossMap") == null)
            {
                var col = material.GetColor("_SpecColor");
                var colBase = material.GetColor("_Color");
                var smoothness = material.GetFloat("_Shininess");

                if (material.GetFloat("_Surface") == 0)
                {
                    if (smoothnessSource == 1)
                        colBase.a = smoothness;
                    else
                        col.a = smoothness;
                    material.SetColor("_BaseColor", colBase);
                }

                material.SetColor("_BaseColor", colBase);
                material.SetColor("_SpecColor", col);
            }
        }
    }

    internal class ParticleUpdaterV1 : MaterialUpgrader
    {
        public ParticleUpdaterV1(string shaderName)
        {
            if (shaderName == null)
                throw new ArgumentNullException("oldShaderName");

            RenameShader(shaderName, shaderName, ParticleUpgrader.UpdateSurfaceBlendModes);

            RenameTexture("_MainTex", "_BaseMap");
            RenameColor("_Color", "_BaseColor");
            RenameFloat("_FlipbookMode", "_FlipbookBlending");

            switch (ShaderUtils.GetEnumFromPath(shaderName))
            {
                case ShaderPathID.ParticlesLit:
                    RenameFloat("_Glossiness", "_Smoothness");
                    break;
                case ShaderPathID.ParticlesSimpleLit:
                    RenameFloat("_Glossiness", "_Smoothness");
                    break;
                case ShaderPathID.ParticlesUnlit:
                    break;
            }
        }
    }
    #endregion
}
