using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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

            EditorUtility.ClearProgressBar();

            MaterialPostprocessor.s_NeedsSavingAssets = true;
        }

        [InitializeOnLoadMethod]
        static void RegisterUpgraderReimport()
        {
            EditorApplication.update += () =>
            {
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

        internal static readonly Action<Material, ShaderPathID>[] k_Upgraders = { UpgradeV1, UpgradeV2, UpgradeV3, UpgradeV4 };

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
            var upgradeLog = "UniversalRP Material log:";
            var upgradeCount = 0;

            foreach (var asset in importedAssets)
            {
                if (!asset.EndsWith(".mat", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var material = (Material)AssetDatabase.LoadAssetAtPath(asset, typeof(Material));
                if (!ShaderUtils.IsLWShader(material.shader))
                    continue;

                ShaderPathID id = ShaderUtils.GetEnumFromPath(material.shader.name);
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
                        assetVersion.version = UniversalProjectSettings.materialVersionForUpgrade;
                        debug += $" assumed to be version {UniversalProjectSettings.materialVersionForUpgrade} due to missing version.";
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

        static void InitializeLatest(Material material, ShaderPathID id)
        {

        }

        static void UpgradeV1(Material material, ShaderPathID shaderID)
        {
            var shaderPath = ShaderUtils.GetShaderPath(shaderID);
            var upgradeFlag = MaterialUpgrader.UpgradeFlags.LogMessageWhenNoUpgraderFound;

            switch (shaderID)
            {
                case ShaderPathID.Unlit:
                    MaterialUpgrader.Upgrade(material, new UnlitUpdaterV1(shaderPath), upgradeFlag);
                    UnlitShader.SetMaterialKeywords(material);
                    break;
                case ShaderPathID.SimpleLit:
                    MaterialUpgrader.Upgrade(material, new SimpleLitUpdaterV1(shaderPath), upgradeFlag);
                    SimpleLitShader.SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords);
                    break;
                case ShaderPathID.Lit:
                    MaterialUpgrader.Upgrade(material, new LitUpdaterV1(shaderPath), upgradeFlag);
                    LitShader.SetMaterialKeywords(material, LitGUI.SetMaterialKeywords);
                    break;
                case ShaderPathID.ParticlesLit:
                    MaterialUpgrader.Upgrade(material, new ParticleUpdaterV1(shaderPath), upgradeFlag);
                    ParticlesLitShader.SetMaterialKeywords(material, LitGUI.SetMaterialKeywords, ParticleGUI.SetMaterialKeywords);
                    break;
                case ShaderPathID.ParticlesSimpleLit:
                    MaterialUpgrader.Upgrade(material, new ParticleUpdaterV1(shaderPath), upgradeFlag);
                    ParticlesSimpleLitShader.SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords, ParticleGUI.SetMaterialKeywords);
                    break;
                case ShaderPathID.ParticlesUnlit:
                    MaterialUpgrader.Upgrade(material, new ParticleUpdaterV1(shaderPath), upgradeFlag);
                    ParticlesUnlitShader.SetMaterialKeywords(material, null, ParticleGUI.SetMaterialKeywords);
                    break;
            }
        }

        static void UpgradeV2(Material material, ShaderPathID shaderID)
        {
            // fix 50 offset on shaders
            if(material.HasProperty("_QueueOffset"))
                BaseShaderGUI.SetupMaterialBlendMode(material);
        }

        static void UpgradeV3(Material material, ShaderPathID shaderID)
        {
            switch (shaderID)
            {
                case ShaderPathID.Lit:
                case ShaderPathID.SimpleLit:
                case ShaderPathID.ParticlesLit:
                case ShaderPathID.ParticlesSimpleLit:
                case ShaderPathID.ParticlesUnlit:
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

        static void UpgradeV4(Material material, ShaderPathID shaderID)
        {}
    }

    // Upgraders v1
    #region UpgradersV1

    internal class LitUpdaterV1 : MaterialUpgrader
    {
        public static void UpdateLitDetails(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            if(material.GetTexture("_MetallicGlossMap") || material.GetTexture("_SpecGlossMap") || material.GetFloat("_SmoothnessTextureChannel") >= 0.5f)
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
            material.SetFloat("_SmoothnessSource" ,smoothnessSource);
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

