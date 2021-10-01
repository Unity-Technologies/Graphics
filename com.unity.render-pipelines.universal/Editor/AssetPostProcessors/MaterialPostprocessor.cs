using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEditor.ShaderGraph;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static Unity.Rendering.Universal.ShaderUtils;

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

    class MaterialPostprocessor : AssetPostprocessor
    {
        public const string materialVersionDependencyName = "urp-material-version";

        [InitializeOnLoadMethod]
        static void RegisterUpgraderReimport()
        {
            UnityEditor.MaterialPostprocessor.OnImportedMaterial += OnImportedMaterial;

            // Register custom dependency on Material version
            AssetDatabase.RegisterCustomDependency(materialVersionDependencyName, Hash128.Compute(MaterialPostprocessor.k_Upgraders.Length));
            AssetDatabase.Refresh();
        }

        private void OnPreprocessMaterialAsset(Material material)
        {
            var shaderID = GetShaderID(material.shader);
            if(shaderID == ShaderID.Unknown)
                return;
            context.DependsOnCustomDependency(materialVersionDependencyName);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material.shader, out var guid, out long _);
            context.GetArtifactFilePath(new GUID(guid), "urp-material");
        }

        public static List<string> s_CreatedAssets = new List<string>();

        internal static readonly Action<Material, ShaderID>[] k_Upgraders = { UpgradeV1, UpgradeV2, UpgradeV3, UpgradeV4, UpgradeV5 };

        static void OnImportedMaterial(Material material, string assetPath)
        {
            // load the material and look for it's Universal ShaderID
            // we only care about versioning materials using a known Universal ShaderID
            // this skips any materials that only target other render pipelines, are user shaders,
            // or are shaders we don't care to version
            var shaderID = GetShaderID(material.shader);
            if (shaderID == ShaderID.Unknown)
                return;

            var wasUpgraded = false;

            // look for the Universal AssetVersion
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

            var latestVersion = k_Upgraders.Length;

            if (!assetVersion)
            {
                wasUpgraded = true;
                assetVersion = ScriptableObject.CreateInstance<AssetVersion>();
                assetVersion.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;
                if (s_CreatedAssets.Contains(assetPath))
                {
                    assetVersion.version = latestVersion;
                    s_CreatedAssets.Remove(assetPath);
                    InitializeLatest(material, shaderID);
                }
                else
                {
                    // if we see a ShaderGraph material with no assetVersion, set it to 0 to ensure we apply all necessary versions.
                    assetVersion.version = 0;
                }

                AssetDatabase.AddObjectToAsset(assetVersion, assetPath);
            }

            // Upgrade
            while (assetVersion.version < latestVersion)
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

        // TODOJENNY: ask URP if speed tree is supported if so, we need the following:
        // TODOREMI: wait for Tianliang PR ( https://unity.slack.com/archives/C89KFUUCT/p1631810113244300 ) to land and merge with it
        // to be a upgrade step
        /*
        public void OnPostprocessSpeedTree(GameObject speedTree)
        {
            SpeedTreeImporter stImporter = assetImporter as SpeedTreeImporter;
            SpeedTree8MaterialUpgrader.PostprocessSpeedTree8Materials(speedTree,stImporter,HDSpeedTree8MaterialUpgrader.HDSpeedTree8MaterialFinalizer);
        }
        */

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
        {}

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
            material.SetFloat("_SmoothnessSource" , smoothnessSource);
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
