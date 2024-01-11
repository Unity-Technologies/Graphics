using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    internal static class VolumeUtils
    {
#if UNITY_EDITOR
        public static string GetDefaultNameForVolumeProfile(VolumeProfile profileInResourcesFolder)
        {
            return BuildDefaultNameForVolumeProfile(profileInResourcesFolder);
        }

        internal static string BuildDefaultNameForVolumeProfile(VolumeProfile profileInResourcesFolder)
        {
            return $"Assets/{HDProjectSettingsReadOnlyBase.projectSettingsFolderPath}/{profileInResourcesFolder.name}.asset";
        }

        public static VolumeProfile CopyVolumeProfileFromResourcesToAssets(VolumeProfile profileInResourcesFolder)
        {
            if (profileInResourcesFolder == null)
                return null;

            string path = BuildDefaultNameForVolumeProfile(profileInResourcesFolder);
            var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                CoreUtils.EnsureFolderTreeInAssetFilePath(path);
                UnityEditor.AssetDatabase.CopyAsset(UnityEditor.AssetDatabase.GetAssetPath(profileInResourcesFolder), path);
                profile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            }
            else
            {
                profile.components.Clear();
                foreach (var resourceComponent in profileInResourcesFolder.components)
                {
                    var c = profile.Add(resourceComponent.GetType());
                    for (int i = 0; i < c.parameters.Count; i++)
                    {
                        c.parameters[i].SetValue(resourceComponent.parameters[i]);
                    }
                }

            }

            return profile;
        }

        public static bool IsDefaultVolumeProfile(VolumeProfile volumeProfile, VolumeProfile profileInResourcesFolder)
        {
            return volumeProfile != null && volumeProfile.Equals(profileInResourcesFolder);
        }

        public static DiffusionProfileSettings[] CreateArrayWithDefaultDiffusionProfileSettingsList(HDRenderPipelineEditorAssets editorAssets = null)
        {
            var diffusionProfileSettingsArray = (editorAssets ?? GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineEditorAssets>())
                .defaultDiffusionProfileSettingsList;

            var length = diffusionProfileSettingsArray.Length;
            var diffusionProfileSettingsArrayCopy = new DiffusionProfileSettings[diffusionProfileSettingsArray.Length];
            Array.Copy(diffusionProfileSettingsArray, diffusionProfileSettingsArrayCopy, length);

            return diffusionProfileSettingsArrayCopy;
        }

        public static DiffusionProfileList GetOrCreateDiffusionProfileList(VolumeProfile volumeProfile)
        {
            if (!volumeProfile.TryGet<DiffusionProfileList>(out var component))
            {
                component = volumeProfile.Add<DiffusionProfileList>(true);

                if (EditorUtility.IsPersistent(volumeProfile))
                {
                    UnityEditor.AssetDatabase.AddObjectToAsset(component, volumeProfile);
                    EditorUtility.SetDirty(volumeProfile);
                }
            }

            component.diffusionProfiles.value ??= Array.Empty<DiffusionProfileSettings>();
            return component;
        }

        public static bool IsDiffusionProfileRegistered(DiffusionProfileSettings profile, VolumeProfile volumeProfile)
        {
            if (volumeProfile == null)
            {
                Debug.LogError($"Invalid {nameof(VolumeProfile)} to obtain {nameof(DiffusionProfileSettings)}");
                return false;
            }

            var diffusionProfileList = VolumeUtils.GetOrCreateDiffusionProfileList(volumeProfile);
            return IsDiffusionProfileRegistered(profile, diffusionProfileList.ToArray());
        }

        public static bool IsDiffusionProfileRegistered(DiffusionProfileSettings profile, DiffusionProfileSettings[] profiles)
        {
            if (profile == null || profiles == null || profiles.Length == 0)
                return false;

            for (var index = 0; index < profiles.Length; index++)
            {
                if (profiles[index] == profile)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryAddDiffusionProfiles(VolumeProfile volumeProfile, DiffusionProfileSettings[] profiles)
        {
            if (volumeProfile == null)
            {
                Debug.LogError($"Invalid {nameof(VolumeProfile)} to register {nameof(DiffusionProfileSettings)}");
                return false;
            }

            var diffusionProfileList = VolumeUtils.GetOrCreateDiffusionProfileList(volumeProfile);

            using (HashSetPool<DiffusionProfileSettings>.Get(out var tmp))
            {
                var currentOverrides = diffusionProfileList.ToArray();

                bool changed = false;

                // Clear null DiffusionProfileSettings by NOT adding them to tmp
                for (var index = 0; index < currentOverrides.Length; index++)
                {
                    var it = currentOverrides[index];
                    if (it != null)
                    {
                        tmp.Add(it);
                    }
                    else
                    {
                        changed = true; // null entry found, update required
                    }
                }

                bool ok = true;

                if (profiles != null && profiles.Length > 0)
                {
                    // Add new ones
                    for (var index = 0; index < profiles.Length; index++)
                    {
                        var it = profiles[index];
                        if (tmp.Count >= DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1)
                        {
                            Debug.LogError($"Failed to register some diffusion profiles. You have reached the limit of {DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1} custom diffusion profiles in HDRP's Global Settings Default Volume. Please remove one before adding a new one.");
                            ok = false;
                            break; // We already reach the limit, no need to continue looping over the profiles.
                        }

                        if (!tmp.Contains(it))
                        {
                            tmp.Add(it);
                            changed = true; // new entry added, update required
                        }
                        else
                            ok = false;
                    }
                }

                if (changed)
                {
                    DiffusionProfileSettings[] array = new DiffusionProfileSettings[tmp.Count];
                    tmp.CopyTo(array);

                    diffusionProfileList.ReplaceWithArray(array);
                    EditorUtility.SetDirty(volumeProfile);
                    VolumeManager.instance.OnVolumeProfileChanged(volumeProfile);
                }

                return ok;
            }
        }

        public static bool TryAddSingleDiffusionProfile(VolumeProfile volumeProfile, DiffusionProfileSettings profile)
        {
            return profile != null && TryAddDiffusionProfiles(volumeProfile, new [] { profile });
        }

        public class DiffusionProfileRegisterScope : IDisposable
        {
            HashSet<DiffusionProfileSettings> m_Profiles = new HashSet<DiffusionProfileSettings>();
            private Dictionary<Shader, IEnumerable<int>> m_DiffusionProfileShaderPropertiesCache = new Dictionary<Shader, IEnumerable<int>>();
            bool m_RegisterProfiles = false;

            public void RegisterReferencedDiffusionProfilesFromMaterial(Material material)
            {
                if (!m_RegisterProfiles)
                    return;

                if (!m_DiffusionProfileShaderPropertiesCache.TryGetValue(material.shader, out var shaderDiffusionProfileProperties))
                {
                    shaderDiffusionProfileProperties = HDMaterial.GetShaderDiffusionProfileProperties(material.shader);
                    m_DiffusionProfileShaderPropertiesCache[material.shader] = shaderDiffusionProfileProperties;
                }

                foreach (var nameID in shaderDiffusionProfileProperties)
                {
                    if (!material.HasProperty(nameID))
                        continue;

                    var diffusionProfileAsset = HDMaterial.GetDiffusionProfileAsset(material, nameID);
                    if (diffusionProfileAsset != null)
                        m_Profiles.Add(diffusionProfileAsset);
                }
            }

            public DiffusionProfileRegisterScope()
            {
                var defaultVolumeSettings = GraphicsSettings.GetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>();
                var diffusionProfileSettings = GraphicsSettings.GetRenderPipelineSettings<DiffusionProfileDefaultSettings>();
                m_RegisterProfiles = defaultVolumeSettings != null && diffusionProfileSettings is { autoRegister: true };
            }

            public void Dispose()
            {
                var defaultVolumeSettings = GraphicsSettings.GetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>();
                if (defaultVolumeSettings == null)
                    return;

                if (defaultVolumeSettings.volumeProfile == null)
                {
                    if (m_Profiles.Count > 0)
                        Debug.LogError($"Invalid {nameof(VolumeProfile)} to auto register {nameof(DiffusionProfileSettings)}. Please use set one in Graphics Settings > HDRP.");
                    return;
                }

                DiffusionProfileSettings[] array = new DiffusionProfileSettings[m_Profiles.Count];
                m_Profiles.CopyTo(array);

                TryAddDiffusionProfiles(defaultVolumeSettings.volumeProfile, array);
                m_Profiles.Clear();
            }
        }
#endif
    }
}
