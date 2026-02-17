using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEditor.Rendering.Converter;
using UnityEngine;
using UnityEngine.Categorization;
using UnityEngine.Rendering.Universal;
using static UnityEditor.Rendering.AnimationClipUpgrader;
using ClipPath = UnityEditor.Rendering.AnimationClipUpgrader.ClipPath;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Represents an animation clip asset that needs to be converted from Built-in RP to URP.
    /// Tracks the animation clip and all its dependent assets (materials, prefabs, etc.).
    /// </summary>
    [Serializable]
    internal class AnimationClipConverterItem : RenderPipelineConverterAssetItem
    {
        public AnimationClipConverterItem(GlobalObjectId gid, string assetPath)
            : base(gid, assetPath)
        {
            info = assetPath;
        }

        /// <summary>
        /// List of assets that reference this animation clip (e.g., prefabs, scene GameObjects).
        /// Used to determine if the clip can be safely upgraded based on its usage context.
        /// </summary>
        [SerializeReference]
        public List<RenderPipelineConverterAssetItem> dependencies = new();
    }

    /// <summary>
    /// Converts animation clips that animate material properties from Built-in RP to URP.
    /// Updates property paths in animation clips to match the new URP shader properties,
    /// ensuring animations continue to work after material conversion.
    /// </summary>
    [Serializable]
    [URPHelpURL("features/rp-converter")]
    [PipelineConverter("Built-in", "Universal Render Pipeline (Universal Renderer)")]
    [BatchModeConverterClassInfo("BuiltInToURP", "AnimationClip")]
    [ElementInfo(Name = "Animation Clip",
                 Order = 110,
                 Description = "Updates animation clips that reference material properties to work with URP shaders.\nEnsures material animations continue working after converting Materials from Built-in RP to URP.")]
    internal sealed class AnimationClipConverter : IRenderPipelineConverter
    {
        [SerializeField]
        internal List<AnimationClipConverterItem> assets = new();

        /// <summary>
        /// Builds a list of animation clips that animate material properties and their search queries.
        /// Each entry contains a search query to find assets referencing the clip, and the clip's path.
        /// </summary>
        /// <returns>List of tuples containing search queries and animation clip paths.</returns>
        public static List<(string animationClip, string searchQuery)> GetAnimationClipsSearchList()
        {
            List<(string materialName, string searchQuery)> list = new();

            // Find all animation clips in the project
            foreach (var animationClip in AssetDatabaseHelper.FindAssets<AnimationClip>())
            {
                // Only include clips that animate material properties (e.g., color, float values)
                if (!AnimationClipUpgrader.IsAnimatingMaterialProperties(animationClip))
                    continue;

                // Create a search query using the GlobalObjectId to find all assets referencing this clip
                string formattedId = $"<$object:{GlobalObjectId.GetGlobalObjectIdSlow(animationClip)},UnityEngine.Object$>";
                list.Add(($"p: ref={formattedId}", AssetDatabase.GetAssetPath(animationClip)));
            }
            return list;
        }

        /// <summary>
        /// Scans the project for animation clips that need conversion and identifies their dependencies.
        /// </summary>
        /// <param name="onScanFinish">Callback invoked with the list of items to convert when scanning completes.</param>
        public void Scan(Action<List<IRenderPipelineConverterItem>> onScanFinish)
        {
            assets.Clear();
            AnimationClipConverterItem currentAnimationClipItem = null;

            void OnSearchFinish()
            {
                // Create a defensive copy to avoid exposing the internal mutable collection
                var results = new List<IRenderPipelineConverterItem>(assets.Count);
                foreach (var asset in assets)
                {
                    if (asset.dependencies.Count == 0)
                        continue;

                    results.Add(asset);
                }

                onScanFinish?.Invoke(results);
            }

            using (UnityEngine.Pool.DictionaryPool<string, HashSet<AnimationClipConverterItem>>.Get(out var animatorUsingClip))
            using (UnityEngine.Pool.HashSetPool<(string,string)>.Get(out var animatorReferences))
            {
                void OnAnimationClipDependenciesSearchFinish()
                {
                    var query = new List<(string, string)>(animatorReferences);
                    SearchServiceUtils.RunQueuedSearch
                    (
                        SearchServiceUtils.IndexingOptions.DeepSearch,
                        query,
                        (searchItem, path) =>
                        {
                            if (searchItem.ToObject() is not GameObject go || go.scene == null)
                                return;

                            var gid = GlobalObjectId.GetGlobalObjectIdSlow(go);

                            var assetItem = new RenderPipelineConverterAssetItem(gid, go.scene.path);

                            if (animatorUsingClip.TryGetValue(path, out var list))
                            {
                                foreach (var i in list)
                                {
                                    i.dependencies.Add(assetItem);
                                }
                            }
                        },
                        OnSearchFinish  // Invoked when all queued searches complete
                    );
                }

                SearchServiceUtils.RunQueuedSearch
                (
                    SearchServiceUtils.IndexingOptions.DeepSearch,
                    GetAnimationClipsSearchList(),
                    (searchItem, animationClipPath) =>
                    {
                        // Search callback invoked for each asset that references an animation clip.
                        // The queued search processes results grouped by animation clip. When we encounter
                        // a different animationClipPath than the current one, it indicates we've started
                        // processing dependencies for a new animation clip.
                        // 
                        // This batching approach efficiently groups all dependencies (prefabs, GameObjects, etc.)
                        // under their respective animation clips without requiring separate searches per clip.

                        // Create a new converter item when we encounter a different animation clip
                        if (currentAnimationClipItem == null ||
                            !currentAnimationClipItem.assetPath.Equals(animationClipPath))
                        {
                            currentAnimationClipItem = new AnimationClipConverterItem(
                                GlobalObjectId.GetGlobalObjectIdSlow(AssetDatabase.LoadAssetAtPath<AnimationClip>(animationClipPath)),
                                animationClipPath);
                            assets.Add(currentAnimationClipItem);
                        }

                        // Add the dependent asset (prefab, GameObject, etc.) to the current clip's dependency list
                        var assetItem = new RenderPipelineConverterAssetItem(searchItem.id);
                        
                        // If the animation clip is being referenced by a controller, we need to enqueue where this controller is being used
                        // Handle everything at the end and fill clip dependencies, with controller dependencies.
                        if (assetItem.assetPath.EndsWith(".controller"))
                        {
                            var controller = assetItem.LoadObject();
                            string formattedId = $"<$object:{GlobalObjectId.GetGlobalObjectIdSlow(controller)},UnityEngine.Object$>";
                            animatorReferences.Add(($"ref={formattedId}", AssetDatabase.GetAssetPath(controller)));

                            if (!animatorUsingClip.TryGetValue(assetItem.assetPath, out var list))
                            {
                                list = new ();
                                animatorUsingClip[assetItem.assetPath] = list;
                            }

                            list.Add(currentAnimationClipItem);
                        }
                        else
                        {
                            // A game object with an Animation component directly referencing the animation clip
                            currentAnimationClipItem.dependencies.Add(assetItem);
                        }
                    },
                    OnAnimationClipDependenciesSearchFinish  // Queue animators
                );
            }
        }

        /// <summary>
        /// Cache of material upgrade paths from Built-in shaders to URP shaders.
        /// Used to determine how animated material properties should be remapped.
        /// </summary>
        internal AnimationClipUpgradePathsCache m_UpgradePathsToNewShaders;

        /// <summary>
        /// Prepares the converter by loading all material upgrade paths.
        /// Called once before converting any animation clips.
        /// </summary>
        public void BeforeConvert()
        {
            m_UpgradePathsToNewShaders = new (BuiltInToURP3DMaterialUpgrader.FetchMaterialUpgraders());
        }

        /// <summary>
        /// Cleans up resources after conversion completes.
        /// </summary>
        public void AfterConvert()
        {
            m_UpgradePathsToNewShaders.Dispose();
            m_UpgradePathsToNewShaders = null;
        }

        /// <summary>
        /// Converts a single animation clip item by upgrading its material property paths.
        /// </summary>
        /// <param name="item">The animation clip item to convert.</param>
        /// <param name="message">Output message describing the conversion result or any errors.</param>
        /// <returns>Status indicating success or failure of the conversion.</returns>
        public Status Convert(IRenderPipelineConverterItem item, out string message)
        {
            var assetItem = item as AnimationClipConverterItem;
            var animationClip = assetItem.LoadObject() as AnimationClip;
            if (animationClip == null)
            {
                message = $"Failed to load {assetItem.name} - Asset Path {assetItem.assetPath}";
                return Status.Error;
            }

            var errorString = new StringBuilder();

            var status = ConvertObject(animationClip, assetItem.dependencies, errorString);
            message = errorString.ToString();
            return status;
        }

        /// <summary>
        /// Performs the actual conversion of an animation clip by analyzing its usage context
        /// and upgrading material property paths where safe to do so.
        /// </summary>
        /// <param name="clip">The animation clip to convert.</param>
        /// <param name="dependencies">List of assets that reference this clip (prefabs, GameObjects, etc.).</param>
        /// <param name="message">Output message builder for logging results and errors.</param>
        /// <returns>Status indicating success or failure of the conversion.</returns>
        Status ConvertObject(AnimationClip clip, List<RenderPipelineConverterAssetItem> dependencies, StringBuilder message)
        {
            var status = Status.Success;
            try
            {
                var clipPaths = new ClipPath[] { clip };

                // Extract material property animation data from the clip
                var clipData = AnimationClipUpgrader.GetAssetDataForClipsFiltered(clipPaths);
                if (clipData.Count == 0)
                {
                    message.AppendLine($"The given animation clip ({clip}) does not have material bindings");
                    return Status.Error;
                }

                using (UnityEngine.Pool.ListPool<GameObject>.Get(out var tmpGameObjects))
                {
                    // Analyze how the clip is used across different GameObjects/prefabs to determine
                    // if it can be safely upgraded. A clip used with both upgraded and non-upgraded
                    // materials cannot be converted without breaking one of those use cases.
                    // 
                    // Dependencies include:
                    // - Prefabs that use this animation clip
                    // - GameObjects in scenes that use this clip
                    // - Scene assets (skipped as GameObjects provide sufficient context)
                    foreach (var d in dependencies)
                    {
                        if (d.LoadObject() is GameObject go)
                            GatherClipsUsageForGameObject(go, clipData, m_UpgradePathsToNewShaders, default);
                    }
                    
                    // Track which clips were successfully upgraded and which could not be converted
                    var upgraded = new HashSet<(IAnimationClip Clip, ClipPath Path, ShaderPropertyUsage Usage)>();
                    var notUpgraded = new HashSet<(IAnimationClip Clip, ClipPath Path, ShaderPropertyUsage Usage)>();

                    // Perform the upgrade, filtering out clips with ambiguous usage
                    UpgradeClips(clipData, upgraded, notUpgraded);

                    // Generate result message based on upgrade outcome
                    if (upgraded.Count == 1)
                    {
                        status = Status.Success;

                        bool setDirty = true;

                        foreach (var item in upgraded)
                        {
                            var (Clip, Path, Usage) = item;

                            if (Usage.HasFlag(ShaderPropertyUsage.InvalidShader))
                            {
                                message.Append("Unable to migrate Animation Clip");
                                message.AppendLine("- There is not converter defined for the target shader, or the target shader must be upgraded before.");
                                status = Status.Warning;
                                setDirty = false;
                            }
                        }

                        if (setDirty)
                            EditorUtility.SetDirty(clip);
                    }
                    else if (notUpgraded.Count == 1)
                    {
                        status = Status.Warning;

                        // Provide detailed information about why the clip couldn't be upgraded
                        foreach (var item in notUpgraded)
                        {
                            var (Clip, Path, Usage) = item;

                            message.AppendLine("Unable to migrate Animation Clip:");
                            if (Usage.HasFlag(ShaderPropertyUsage.InvalidShader))
                            {
                                message.AppendLine("- There is not converter defined for the target shader, or the target shader must be upgraded before.");
                                status = Status.Warning;
                            }

                            if (Usage.HasFlag(ShaderPropertyUsage.MultipleUpgradePaths))
                            {
                                message.Append("- The upgraders define multiple upgrade paths for one of the properties");
                                status = Status.Error;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                message.AppendLine($"Error converting {clip}: {ex.Message}");
            }

            return status;
        }
    }
}
