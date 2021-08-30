using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    sealed partial class DiffusionProfileSettings : IVersionable<DiffusionProfileSettings.Version>
    {
        enum Version
        {
            Initial,                // 16 profiles per asset
            DiffusionProfileRework, // one profile per asset
        }

        [Obsolete("Profiles are obsolete, only one diffusion profile per asset is allowed.")]
        internal DiffusionProfile this[int index]
        {
            get => profile;
        }

        [SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>();
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        [Obsolete("Profiles are obsolete, only one diffusion profile per asset is allowed.")]
        internal DiffusionProfile[] profiles;

        static readonly MigrationDescription<Version, DiffusionProfileSettings> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.DiffusionProfileRework, (DiffusionProfileSettings d) =>
            {
#pragma warning disable 618
#if UNITY_EDITOR
                if (d.profiles == null)
                    return;

                // If the asset importer for the asset we're upgrading is null, it means that the asset
                // does not exists on the disk and we don't want to upgrade these assets
                string assetPath = AssetDatabase.GetAssetPath(d);
                var importer = AssetImporter.GetAtPath(assetPath);
                if (importer == null)
                    return;

                var currentHDAsset = HDRenderPipeline.currentAsset;
                if (currentHDAsset == null)
                    throw new Exception("Can't upgrade diffusion profile without an active HD Render Pipeline asset (see Quality Settings).");

                var defaultProfile = new DiffusionProfile(true);

                // Iterate over the diffusion profile settings and generate one new asset for each
                // diffusion profile which have been modified, and store them into a dictionary to be able to upgrade materials
                int index = 0;
                var newProfiles = new Dictionary<int, DiffusionProfileSettings>();
                string assetName = Path.GetFileNameWithoutExtension(assetPath);
                foreach (var profile in d.profiles)
                {
                    if (!profile.Equals(defaultProfile))
                    {
                        newProfiles[index] = CreateNewDiffusionProfile(d, assetName, profile, index);
                        // Update the diffusion profile hash is required for assets that are upgraded because it will
                        // be assigned to materials right after the create of the asset so we don't wait for the auto hash update
                        UnityEditor.Rendering.HighDefinition.DiffusionProfileHashTable.UpdateDiffusionProfileHashNow(newProfiles[index]);
                    }
                    index++;
                }

                // We write in the main diffusion profile meta file the list of created asset so we know where to look
                // when we upgrade materials inside scenes (from the menu item)
                SerializableGUIDs toJson;
                toJson.assetGUIDs = new string[16];
                foreach (var kp in newProfiles)
                    toJson.assetGUIDs[kp.Key] = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(kp.Value));
                importer.userData = JsonUtility.ToJson(toJson);

                // Update the diffusion profiles references in all the hd assets where this profile was set
                var hdAssetsGUIDs = AssetDatabase.FindAssets("t:HDRenderPipelineAsset");
                foreach (var hdAssetGUID in hdAssetsGUIDs)
                {
                    var hdAsset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(hdAssetGUID));

                    if (hdAsset.diffusionProfileSettings == d)
                    {
                        // Assign the new diffusion profile assets into the HD asset
                        hdAsset.m_ObsoleteDiffusionProfileSettingsList = new DiffusionProfileSettings[newProfiles.Keys.Max() + 1];
                        foreach (var kp in newProfiles)
                            hdAsset.m_ObsoleteDiffusionProfileSettingsList[kp.Key] = kp.Value;
                        UnityEditor.EditorUtility.SetDirty(hdAsset);
                    }
                }

                // If the diffusion profile settings we're upgrading was assigned to the HDAsset in use
                // then we need to go over all materials and upgrade them
                if (currentHDAsset.diffusionProfileSettings == d)
                {
                    var materialGUIDs = AssetDatabase.FindAssets("t:Material");
                    foreach (var guid in materialGUIDs)
                    {
                        var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                        UpgradeMaterial(mat, d);
                    }
                }
#endif
#pragma warning restore 618
            })
        );

#if UNITY_EDITOR
        public static void UpgradeMaterial(Material mat, DiffusionProfileSettings mainProfile)
        {
            UpgradeMaterial(mat, mainProfile, "_DiffusionProfile", "_DiffusionProfileAsset", "_DiffusionProfileHash");
            // For layered material:
            UpgradeMaterial(mat, mainProfile, "_DiffusionProfile0", "_DiffusionProfileAsset0", "_DiffusionProfileHash0");
            UpgradeMaterial(mat, mainProfile, "_DiffusionProfile1", "_DiffusionProfileAsset1", "_DiffusionProfileHash1");
            UpgradeMaterial(mat, mainProfile, "_DiffusionProfile2", "_DiffusionProfileAsset2", "_DiffusionProfileHash2");
            UpgradeMaterial(mat, mainProfile, "_DiffusionProfile3", "_DiffusionProfileAsset3", "_DiffusionProfileHash3");
        }

        [System.Serializable]
        struct SerializableGUIDs
        {
            [SerializeField]
            public string[] assetGUIDs;
        }

        static void UpgradeMaterial(Material mat, DiffusionProfileSettings mainProfile, string diffusionProfile, string diffusionProfileAsset, string diffusionProfileHash)
        {
            // if the material don't have a diffusion profile
            if (!mat.HasProperty(diffusionProfile) || !mat.HasProperty(diffusionProfileAsset) || !mat.HasProperty(diffusionProfileHash))
                return;

            // We can't upgrade materials if the diffusion profile is null
            if (mainProfile == null)
                return;

            // or if it already have been upgraded
            int index = mat.GetInt(diffusionProfile) - 1; // the index in the material is stored with +1 because 0 is none
            if (index < 0)
                return;
            mat.SetInt(diffusionProfile, -1);

            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mainProfile));
            SerializableGUIDs profiles = JsonUtility.FromJson<SerializableGUIDs>(importer.userData);

            if (String.IsNullOrEmpty(profiles.assetGUIDs ? [index]))
            {
                Debug.LogError("Could not upgrade diffusion profile reference in material " + mat + ": index " + index + " not found in main diffusion profile");
                return;
            }

            string diffusionProfileGUID = profiles.assetGUIDs[index];
            var newProfile = AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>(AssetDatabase.GUIDToAssetPath(diffusionProfileGUID));
            mat.SetVector(diffusionProfileAsset, HDUtils.ConvertGUIDToVector4(diffusionProfileGUID));
            mat.SetFloat(diffusionProfileHash, HDShadowUtils.Asfloat(newProfile.profile.hash));
            if (newProfile.profile.hash == 0)
                Debug.LogError("Diffusion profile hash of " + newProfile + " have not been initialized !");

            if (mat.shader.name.StartsWith("HDRP/"))
            {
                // Set the reference value for the stencil test.
                int stencilRef = (int)StencilUsage.Clear;
                int stencilWriteMask = (int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.SubsurfaceScattering;
                int stencilGBufferRef = (int)StencilUsage.RequiresDeferredLighting;
                int stencilGBufferMask = (int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.SubsurfaceScattering;

                if (mat.HasProperty("_MaterialID") && (int)mat.GetFloat("_MaterialID") == 0) // 0 is MaterialId.LitSSS
                {
                    stencilRef = (int)StencilUsage.SubsurfaceScattering;
                    stencilGBufferRef |= (int)StencilUsage.SubsurfaceScattering;
                }

                if (mat.HasProperty("_ReceivesSSR") && mat.GetInt("_ReceivesSSR") == 1)
                {
                    stencilWriteMask |= (int)StencilUsage.TraceReflectionRay;
                    stencilRef |= (int)StencilUsage.TraceReflectionRay;
                    stencilGBufferMask |= (int)StencilUsage.TraceReflectionRay;
                    stencilGBufferRef |= (int)StencilUsage.TraceReflectionRay;
                }

                // As we tag both during motion vector pass and Gbuffer pass we need a separate state and we need to use the write mask
                mat.SetInt("_StencilRef", stencilRef);
                mat.SetInt("_StencilWriteMask", stencilWriteMask);
                mat.SetInt("_StencilRefGBuffer", stencilGBufferRef);
                mat.SetInt("_StencilWriteMaskGBuffer", stencilGBufferMask);
                mat.SetInt("_StencilRefMV", (int)StencilUsage.ObjectMotionVector);
                mat.SetInt("_StencilWriteMaskMV", (int)StencilUsage.ObjectMotionVector);
            }
        }

        static DiffusionProfileSettings CreateNewDiffusionProfile(DiffusionProfileSettings asset, string assetName, DiffusionProfile profile, int index)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            path = Path.GetDirectoryName(path) + "/" + assetName + Path.GetExtension(path);
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            if (index == 0)
            {
                asset.profile = profile;
                profile.Validate();
                asset.UpdateCache();
                AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(asset), path);
                return asset;
            }

            var newDiffusionProfile = ScriptableObject.CreateInstance<DiffusionProfileSettings>();
            newDiffusionProfile.name = asset.name;
            newDiffusionProfile.profile = profile;
            newDiffusionProfile.m_Version = Version.DiffusionProfileRework;
            profile.Validate();
            newDiffusionProfile.UpdateCache();

            AssetDatabase.CreateAsset(newDiffusionProfile, path);
            return newDiffusionProfile;
        }

        public void TryToUpgrade()
        {
            if (k_Migration.Migrate(this))
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
                // Do not refresh the database now because it will force the reimport of all new diffusion profile settings
                EditorApplication.delayCall += UnityEditor.AssetDatabase.Refresh;
            }
        }

#endif
    }
}
