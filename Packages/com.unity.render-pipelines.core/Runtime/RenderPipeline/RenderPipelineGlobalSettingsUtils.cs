#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Set of utilities for <see cref="RenderPipelineGlobalSettings"/>
    /// </summary>
    public class RenderPipelineGlobalSettingsUtils
    {
        /// <summary>
        /// Creates a <see cref="RenderPipelineGlobalSettings"/> asset
        /// </summary>
        /// <param name="path">The path where Unity generates the asset.</param>
        /// <param name="dataSource">Another `RenderPipelineGlobalSettings` that Unity uses as a data source.</param>
        /// <typeparam name="TGlobalSetting"><see cref="RenderPipelineGlobalSettings"/> </typeparam>
        /// <returns>Returns the asset created.</returns>
        public static TGlobalSetting Create<TGlobalSetting>(string path, TGlobalSetting dataSource = null)
            where TGlobalSetting : RenderPipelineGlobalSettings
        {
            return Create(typeof(TGlobalSetting), path, dataSource) as TGlobalSetting;
        }

        /// <summary>
        /// Creates a <see cref="RenderPipelineGlobalSettings"/> asset
        /// </summary>
        /// <param name="renderPipelineGlobalSettingsType"></param>
        /// <param name="path">The path where Unity generates the asset.</param>
        /// <param name="dataSource">Another `RenderPipelineGlobalSettings` that Unity uses as a data source.</param>
        /// <returns>Returns the asset created.</returns>
        public static RenderPipelineGlobalSettings Create(Type renderPipelineGlobalSettingsType, string path, RenderPipelineGlobalSettings dataSource = null)
        {
            if (!typeof(RenderPipelineGlobalSettings).IsAssignableFrom(renderPipelineGlobalSettingsType))
                throw new ArgumentException(
                    $"{nameof(renderPipelineGlobalSettingsType)} must be a valid {typeof(RenderPipelineGlobalSettings)}");

            // Sanitize the path
            if (string.IsNullOrEmpty(path))
                path = $"Assets/{renderPipelineGlobalSettingsType.Name}.asset";

            if (!path.StartsWith("assets/", StringComparison.CurrentCultureIgnoreCase))
                path = $"Assets/{path}";

            CoreUtils.EnsureFolderTreeInAssetFilePath(path);
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            var assetCreated = ScriptableObject.CreateInstance(renderPipelineGlobalSettingsType) as RenderPipelineGlobalSettings;
            if (assetCreated != null)
            {
                AssetDatabase.CreateAsset(assetCreated, path);

                // copy data from provided source
                if (dataSource != null)
                    EditorUtility.CopySerializedManagedFieldsOnly(dataSource, assetCreated);

                EditorGraphicsSettings.PopulateRenderPipelineGraphicsSettings(assetCreated);

                assetCreated.Initialize(dataSource);

                EditorUtility.SetDirty(assetCreated);
                AssetDatabase.SaveAssetIfDirty(assetCreated);
                AssetDatabase.Refresh();
            }

            return assetCreated;
        }

        /// <summary>
        /// Checks that a <see cref="RenderPipelineGlobalSettings"/> asset exists.
        /// If the asset isn't valid, Unity tries the following in order:
        /// 1. Loads the asset at the default path.
        /// 2. Finds any asset in the project with the same type.
        /// 3. If `canCreateNewAsset` is true, creates a new asset in the default path.
        /// If Unity finds or creates a valid asset, Unity updates the <see cref="GraphicsSettings"/> with it. Otherwise Unity will unregister the settings for the given pipeline.
        /// </summary>
        /// <param name="instance">The current instance of the asset.</param>
        /// <param name="defaultPath">The default path.</param>
        /// <param name="canCreateNewAsset">If set to `true`, Unity creates a new asset if it can't find an existing asset in the project.</param>
        /// <typeparam name="TGlobalSetting">The type of global settings asset to check.</typeparam>
        /// <typeparam name="TRenderPipeline">The type of `RenderPipeline` that this asset belongs to.</typeparam>
        /// <returns>The asset that Unity found or created, or `null` if Unity can't find or create a valid asset.</returns>
        public static bool TryEnsure<TGlobalSetting, TRenderPipeline>(ref TGlobalSetting instance, string defaultPath = "", bool canCreateNewAsset = true)
            where TGlobalSetting : RenderPipelineGlobalSettings<TGlobalSetting, TRenderPipeline>
            where TRenderPipeline : RenderPipeline
        {
            if (!TryEnsure<TGlobalSetting, TRenderPipeline>(ref instance, defaultPath, canCreateNewAsset, out var error))
            {
                Debug.LogError(error.Message);
                return false;
            }

            return true;
        }

        // This method is exposed to Unit Tests
        internal static bool TryEnsure<TGlobalSetting, TRenderPipeline>(ref TGlobalSetting instance, string defaultPath, bool canCreateNewAsset, out Exception error)
            where TGlobalSetting : RenderPipelineGlobalSettings<TGlobalSetting, TRenderPipeline>
            where TRenderPipeline : RenderPipeline
        {
            var globalSettingsName = typeof(TGlobalSetting).GetCustomAttribute<DisplayInfoAttribute>()?.name ?? typeof(TGlobalSetting).Name;

            if (instance == null || instance.Equals(null))
            {
                if (!string.IsNullOrEmpty(defaultPath))
                {
                    // Look at default path, if the asset exist, is the one that we need
                    instance = AssetDatabase.LoadAssetAtPath<TGlobalSetting>(defaultPath);
                }

                if (instance == null)
                {
                    // There was not saved into the default path, fetch the asset database to see if there is one defined in the project
                    instance = CoreUtils.LoadAllAssets<TGlobalSetting>().FirstOrDefault();
                    if (instance == null)
                    {
                        // Try to create one if possible
                        if (canCreateNewAsset)
                        {
                            instance = Create<TGlobalSetting>(defaultPath);
                            if (instance != null)
                            {
                                Debug.LogWarning($"{globalSettingsName} has been created for you. If you want to modify it, go to Project Settings > Graphics > {globalSettingsName}");
                            }
                        }
                    }
                }
            }

            error = instance == null || instance.Equals(null)
                ? new Exception(
                    $"Unable to find or create a {globalSettingsName}. The configured Render Pipeline may not work correctly. Go to Project Settings > Graphics > {globalSettingsName} for additional help.")
                : null;

            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<TRenderPipeline>(instance);

            return error == null;
        }
    }
}
#endif
