using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using IMaterial = UnityEditor.Rendering.UpgradeUtility.IMaterial;
using UID = UnityEditor.Rendering.UpgradeUtility.UID;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// A class containing static methods for updating <see cref="AnimationClip"/> assets with bindings for <see cref="Material"/> properties.
    /// </summary>
    /// <remarks>
    /// Animation clips store bindings for material properties by path name, but don't know whether those properties exist on their dependents.
    /// Because property names did not change uniformly in the material/shader upgrade process, it is not possible to patch path names indiscriminately.
    /// This class provides utilities for discovering how clips are used, so users can make decisions about whether or not to then update them.
    /// It has the limitation that it only knows about:
    /// - Clips that are directly referenced by an <see cref="Animation"/> component
    /// - Clips referenced by an <see cref="AnimatorController"/> used by an <see cref="Animator"/> component
    /// - Clips that are sub-assets of a <see cref="PlayableAsset"/> used by a <see cref="PlayableDirector"/> component with a single <see cref="Animator"/> binding
    /// It does not know about clips that might be referenced in other ways for run-time reassignment.
    /// Recommended usage is to call <see cref="DoUpgradeAllClipsMenuItem"/> from a menu item callback.
    /// The utility can also provide faster, more reliable results if it knows what <see cref="MaterialUpgrader"/> was used to upgrade specific materials.
    /// </remarks>
    static partial class AnimationClipUpgrader
    {
        static readonly Regex k_MatchMaterialPropertyName = new Regex(@"material.(\w+)(\.\w+)?", RegexOptions.Compiled);

        /// <summary>
        /// Determines whether the specified<see cref="EditorCurveBinding"/> is for a material property.
        /// </summary>
        /// <remarks>Internal only for testability.</remarks>
        /// <param name="b">An <see cref="EditorCurveBinding"/>.</param>
        /// <returns><c>true</c> if the binding is for a material property; <c>false</c> otherwise.</returns>
        internal static bool IsMaterialBinding(EditorCurveBinding b) =>
            (b.type?.IsSubclassOf(typeof(Renderer)) ?? false)
            && !string.IsNullOrEmpty(b.propertyName)
            && k_MatchMaterialPropertyName.IsMatch(b.propertyName);

        static readonly IReadOnlyCollection<string> k_ColorAttributeSuffixes =
            new HashSet<string>(new[] { ".r", ".g", ".b", ".a" });

        /// <summary>
        /// Infer a shader property name and type from an <see cref="EditorCurveBinding"/>.
        /// </summary>
        /// <remarks>Internal only for testability.</remarks>
        /// <param name="binding">A binding presumed to map to a material property. (See also <seealso cref="IsMaterialBinding"/>.)</param>
        /// <returns>A shader property name, and a guess of what type of shader property it targets.</returns>
        internal static (string Name, ShaderPropertyType Type) InferShaderProperty(EditorCurveBinding binding)
        {
            var match = k_MatchMaterialPropertyName.Match(binding.propertyName);
            var propertyName = match.Groups[1].Value;
            var propertyType = match.Groups[2].Value;
            return (propertyName,
                k_ColorAttributeSuffixes.Contains(propertyType) ? ShaderPropertyType.Color : ShaderPropertyType.Float);
        }

        /// <summary>
        /// Gets asset data for all clip assets at the specified paths, which contain bindings for material properties.
        /// (See also <seealso cref="GatherClipsUsageInDependentPrefabs"/> and <seealso cref="GatherClipsUsageInDependentScenes"/>.)
        /// </summary>
        /// <param name="clipPaths">Paths to assets containing <see cref="AnimationClip"/>.</param>
        /// <returns>
        /// Lookup table mapping <see cref="AnimationClip"/> to its asset path, bindings, property rename table, and usage.
        /// (Use <see cref="GatherClipsUsageInDependentPrefabs"/> to initialize rename table and usage.)
        /// </returns>
        internal static IDictionary<
            IAnimationClip,
            (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)
        > GetAssetDataForClipsFiltered(
            IEnumerable<ClipPath> clipPaths
        )
        {
            var result = new Dictionary<
                IAnimationClip,
                (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)
                >();
            foreach (var clipPath in clipPaths)
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(clipPath))
                {
                    if (!(asset is AnimationClip clip))
                        continue;

                    var bindings = AnimationUtility.GetCurveBindings(clip).Where(IsMaterialBinding).ToArray();
                    if (bindings.Length == 0)
                        continue;

                    result[(AnimationClipProxy)clip] =
                        (clipPath, bindings, SerializedShaderPropertyUsage.Unknown, new Dictionary<EditorCurveBinding, string>());
                }
            }

            return result;
        }

        /// <summary>
        /// Get dependency mappings between <see cref="AnimationClip"/> and their dependents.
        /// </summary>
        /// <param name="clips"> Paths to clips to consider. (See also <seealso cref="GetAssetDataForClipsFiltered"/>.)</param>
        /// <param name="assets">Paths to assets to consider.</param>
        /// <param name="clipToDependentAssets">Mapping of clip paths to paths of their dependents.</param>
        /// <param name="assetToClipDependencies">Mapping of asset paths to their clip dependencies.</param>
        /// <typeparam name="T">The type of asset path.</typeparam>
        internal static void GetClipDependencyMappings<T>(
            IEnumerable<ClipPath> clips,
            IEnumerable<T> assets,
            out IReadOnlyDictionary<ClipPath, IReadOnlyCollection<T>> clipToDependentAssets,
            out IReadOnlyDictionary<T, IReadOnlyCollection<ClipPath>> assetToClipDependencies
        ) where T : struct, IAssetPath
        {
            // ensure there are no duplicate keys
            clips = new HashSet<ClipPath>(clips);
            assets = new HashSet<T>(assets);

            // create mutable builders
            var clipsDependentsBuilder = clips.ToDictionary(c => c, c => new HashSet<T>());
            var assetsBuilder = new Dictionary<T, HashSet<ClipPath>>();

            // build dependency tables
            foreach (var asset in assets)
            {
                assetsBuilder[asset] = new HashSet<ClipPath>();

                foreach (var dependencyPath in AssetDatabase.GetDependencies(asset.Path))
                {
                    if (!clipsDependentsBuilder.TryGetValue(dependencyPath, out var dependents))
                        continue;

                    dependents.Add(asset);
                    assetsBuilder[asset].Add(dependencyPath);
                }
            }

            // return readonly results
            clipToDependentAssets =
                clipsDependentsBuilder.ToDictionary(kv => kv.Key, kv => kv.Value as IReadOnlyCollection<T>);
            assetToClipDependencies =
                assetsBuilder.ToDictionary(kv => kv.Key, kv => kv.Value as IReadOnlyCollection<ClipPath>);
        }

        // reusable buffers
        static readonly List<Animation> s_AnimationBuffer = new List<Animation>(8);
        static readonly List<Animator> s_AnimatorBuffer = new List<Animator>(8);
        static readonly List<IAnimationClipSource> s_CustomAnimationBuffer = new List<IAnimationClipSource>(8);
        static readonly List<PlayableDirector> s_PlayableDirectorBuffer = new List<PlayableDirector>(8);

        /// <summary>
        /// Get information about a clip's usage among its dependent scenes to determine whether or not it should be upgraded.
        /// </summary>
        /// <param name="clipDependents">
        /// A table mapping clip asset paths, to asset paths of their dependent prefabs.
        /// (See <seealso cref="GetClipDependencyMappings{T}"/>.)
        /// </param>
        /// <param name="assetDependencies">
        /// A table mapping prefab asset paths, to asset paths of their clip dependencies.
        /// (See <seealso cref="GetClipDependencyMappings{T}"/>.)
        /// </param>
        /// <param name="clipData">
        /// A table mapping clips to other data about them. (See also <seealso cref="GetAssetDataForClipsFiltered"/>.)
        /// </param>
        /// <param name="allUpgradePathsToNewShaders">
        /// A table of new shader names and all known upgrade paths to them in the target pipeline.
        /// (See also <seealso cref="UpgradeUtility.GetAllUpgradePathsToShaders"/>.)
        /// </param>
        /// <param name="upgradePathsUsedByMaterials">
        /// Optional table of materials known to have gone through a specific upgrade path.
        /// </param>
        /// <param name="progressFunctor">
        /// Optional functor to display a progress bar.
        /// </param>
        internal static void GatherClipsUsageInDependentPrefabs(
            IReadOnlyDictionary<ClipPath, IReadOnlyCollection<PrefabPath>> clipDependents,
            // TODO: right now, clip dependencies are gathered in Animation/Animator, so this may not be needed...
            IReadOnlyDictionary<PrefabPath, IReadOnlyCollection<ClipPath>> assetDependencies,
            IDictionary<
                IAnimationClip,
                (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)
            > clipData,
            IReadOnlyDictionary<string, IReadOnlyList<MaterialUpgrader>> allUpgradePathsToNewShaders,
            IReadOnlyDictionary<UID, MaterialUpgrader> upgradePathsUsedByMaterials = default,
            Func<string, float, bool> progressFunctor = null
        )
        {
            int clipIndex = 0;
            int totalNumberOfClips = clipDependents.Count;

            // check all dependents for usage
            foreach (var kv in clipDependents)
            {
                float currentProgress = (float)++clipIndex / totalNumberOfClips;
                if (progressFunctor != null && progressFunctor($"({clipIndex} of {totalNumberOfClips}) {kv.Key.Path}", currentProgress))
                    break;

                foreach (var prefabPath in kv.Value)
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    GatherClipsUsageForGameObject(go, clipData, allUpgradePathsToNewShaders, upgradePathsUsedByMaterials);
                }
            }
        }

        /// <summary>
        /// Get information about a clip's usage among its dependent scenes to determine whether or not it should be upgraded.
        /// </summary>
        /// <remarks>
        /// Because this method will open scenes to search for usages, it is recommended you first prompt for user input.
        /// It is also a good idea to first call <see cref="GatherClipsUsageInDependentPrefabs"/> to generate usage data.
        /// Clips that are already known to be unsafe for upgrading based on their prefab usage can be skipped here.
        /// </remarks>
        /// <param name="clipDependents">
        /// A table mapping clip asset paths, to asset paths of their dependent scenes.
        /// (See <seealso cref="GetClipDependencyMappings{T}"/>.)
        /// </param>
        /// <param name="assetDependencies">
        /// A table mapping scene asset paths, to asset paths of their clip dependencies.
        /// (See <seealso cref="GetClipDependencyMappings{T}"/>.)
        /// </param>
        /// <param name="clipData">
        /// A table mapping clips to other data about them. (See also <seealso cref="GetAssetDataForClipsFiltered"/>.)
        /// </param>
        /// <param name="allUpgradePathsToNewShaders">
        /// A table of new shader names and all known upgrade paths to them in the target pipeline.
        /// (See also <seealso cref="UpgradeUtility.GetAllUpgradePathsToShaders"/>.)
        /// </param>
        /// <param name="upgradePathsUsedByMaterials">
        /// Optional table of materials known to have gone through a specific upgrade path.
        /// </param>
        /// <param name="progressFunctor">
        /// Optional functor to display a progress bar.
        /// </param>
        internal static void GatherClipsUsageInDependentScenes(
            IReadOnlyDictionary<ClipPath, IReadOnlyCollection<ScenePath>> clipDependents,
            // TODO: right now, clip dependencies are gathered in Animation/Animator, so this may not be needed...
            IReadOnlyDictionary<ScenePath, IReadOnlyCollection<ClipPath>> assetDependencies,
            IDictionary<
                IAnimationClip,
                (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string>
                    PropertyRenames)
            > clipData,
            IReadOnlyDictionary<string, IReadOnlyList<MaterialUpgrader>> allUpgradePathsToNewShaders,
            IReadOnlyDictionary<UID, MaterialUpgrader> upgradePathsUsedByMaterials = default,
            Func<string, float, bool> progressFunctor = null
        )
        {
            int clipIndex = 0;
            int totalNumberOfClips = clipDependents.Count;

            // check all dependents for usage
            foreach (var kv in clipDependents)
            {
                float currentProgress = (float)++clipIndex / totalNumberOfClips;
                if (progressFunctor != null && progressFunctor($"({clipIndex} of {totalNumberOfClips}) {kv.Key.Path}", currentProgress))
                    break;

                foreach (var scenePath in kv.Value)
                {
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    foreach (var go in scene.GetRootGameObjects())
                        GatherClipsUsageForGameObject(go, clipData, allUpgradePathsToNewShaders, upgradePathsUsedByMaterials);
                }
            }
        }

        /// <summary>
        /// Update usage information about the specified clips in the clip data table.
        /// </summary>
        /// <param name="go">A prefab, or a <see cref="GameObject"/> in a scene.</param>
        /// <param name="clipData">
        /// A table mapping clips to other data about them. (See also <seealso cref="GetAssetDataForClipsFiltered"/>.)
        /// </param>
        /// <param name="allUpgradePathsToNewShaders">
        /// A table of new shader names and all known upgrade paths to them in the target pipeline.
        /// (See also <seealso cref="UpgradeUtility.GetAllUpgradePathsToShaders"/>.)
        /// </param>
        /// <param name="upgradePathsUsedByMaterials">
        /// Optional table of materials known to have gone through a specific upgrade path.
        /// </param>
        static void GatherClipsUsageForGameObject(
            GameObject go,
            IDictionary<
                IAnimationClip,
                (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)
            > clipData,
            IReadOnlyDictionary<string, IReadOnlyList<MaterialUpgrader>> allUpgradePathsToNewShaders,
            IReadOnlyDictionary<UID, MaterialUpgrader> upgradePathsUsedByMaterials = default
        )
        {
            go.GetComponentsInChildren(true, s_AnimationBuffer);
            go.GetComponentsInChildren(true, s_AnimatorBuffer);
            go.GetComponentsInChildren(true, s_CustomAnimationBuffer);

            // first check clip usage among GameObjects with legacy Animation
            var gameObjects = new HashSet<GameObject>(s_AnimationBuffer.Select(a => a.gameObject)
                .Union(s_AnimatorBuffer.Select(a => a.gameObject))
                .Union(s_CustomAnimationBuffer.Where(a => a is Component).Select(a => ((Component)a).gameObject)));

            foreach (var gameObject in gameObjects)
            {
                var clips = AnimationUtility.GetAnimationClips(gameObject).Select(clip => (IAnimationClip)(AnimationClipProxy)clip);

                GatherClipsUsageForAnimatedHierarchy(
                    gameObject.transform, clips, clipData, allUpgradePathsToNewShaders, upgradePathsUsedByMaterials
                );
            }

            // next check clip usage among GameObjects with PlayableDirector
            go.GetComponentsInChildren(true, s_PlayableDirectorBuffer);
            foreach (var playableDirector in s_PlayableDirectorBuffer)
            {
                var playableAsset = playableDirector.playableAsset;
                if (playableAsset == null)
                    continue;

                var assetPath = AssetDatabase.GetAssetPath(playableAsset);

                // get all clip sub-assets
                var clips = new HashSet<IAnimationClip>(
                    AssetDatabase.LoadAllAssetsAtPath(assetPath)
                        .Where(asset => asset is AnimationClip)
                        .Select(asset => (IAnimationClip)(AnimationClipProxy)(asset as AnimationClip))
                );

                // get all clip dependency-assets
                // this will not handle nested clips in FBX like assets, but these are less likely to be editable
                clips.UnionWith(AssetDatabase.GetDependencies(assetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<AnimationClip>)
                    .Where(asset => asset is AnimationClip)
                    .Select(asset => (IAnimationClip)(AnimationClipProxy)asset));

                // check if the value of a binding is an animator, and examines clip usage relative to it
                // this is imprecise, but is suitable to catch the majority of cases (i.e., a single animator binding)
                using (var so = new SerializedObject(playableDirector))
                {
                    var clipsProp = so.FindProperty("m_SceneBindings");
                    for (int i = 0, count = clipsProp.arraySize; i < count; ++i)
                    {
                        var elementProp = clipsProp.GetArrayElementAtIndex(i);
                        var value = elementProp.FindPropertyRelative("value");
                        if (value.objectReferenceValue is Animator animator)
                        {
                            GatherClipsUsageForAnimatedHierarchy(
                                animator.transform, clips, clipData, allUpgradePathsToNewShaders, upgradePathsUsedByMaterials
                            );
                        }
                    }
                }
            }

            // release UnityObject references
            s_AnimationBuffer.Clear();
            s_AnimatorBuffer.Clear();
            s_CustomAnimationBuffer.Clear();
            s_PlayableDirectorBuffer.Clear();
        }

        // reusable buffers
        static readonly List<Renderer> s_RendererBuffer = new List<Renderer>(8);

        static readonly Dictionary<string, (IRenderer Renderer, List<IMaterial> Materials)> s_RenderersByPath =
            new Dictionary<string, (IRenderer Renderer, List<IMaterial> Materials)>();

        /// <summary>
        /// Update usage information about the specified clips in the clip data table.
        /// </summary>
        /// <param name="root">The root of the animated hierarchy (i.e., object with Animation or Animator).</param>
        /// <param name="clips">Collection of animation clips</param>
        /// <param name="clipData">
        /// A table mapping clips to other data about them. (See also <seealso cref="GetAssetDataForClipsFiltered"/>.)
        /// </param>
        /// <param name="allUpgradePathsToNewShaders">
        /// A table of new shader names and all known upgrade paths to them in the target pipeline.
        /// (See also <seealso cref="UpgradeUtility.GetAllUpgradePathsToShaders"/>.)
        /// </param>
        /// <param name="upgradePathsUsedByMaterials">
        /// Optional table of materials known to have gone through a specific upgrade path.
        /// </param>
        static void GatherClipsUsageForAnimatedHierarchy(
            Transform root,
            IEnumerable<IAnimationClip> clips,
            IDictionary<
                IAnimationClip,
                (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)
            > clipData,
            IReadOnlyDictionary<string, IReadOnlyList<MaterialUpgrader>> allUpgradePathsToNewShaders,
            IReadOnlyDictionary<UID, MaterialUpgrader> upgradePathsUsedByMaterials
        )
        {
            // TODO: report paths of specific assets that contribute to problematic results?

            // find all renderers in the animated hierarchy
            root.GetComponentsInChildren(true, s_RendererBuffer);
            foreach (var renderer in s_RendererBuffer)
            {
                var path = AnimationUtility.CalculateTransformPath(renderer.transform, root);
                var m = ListPool<IMaterial>.Get();
                var r = (RendererProxy)renderer;
                r.GetSharedMaterials(m);
                s_RenderersByPath[path] = (r, m);
            }

            // if there are any renderers, check all clips for usage
            if (s_RendererBuffer.Count > 0)
            {
                foreach (var clip in clips)
                    GatherClipUsage(clip, clipData, s_RenderersByPath, allUpgradePathsToNewShaders, upgradePathsUsedByMaterials);
            }

            // release UnityObject references
            s_RendererBuffer.Clear();
            foreach (var (_, materials) in s_RenderersByPath.Values)
                ListPool<IMaterial>.Release(materials);
            s_RenderersByPath.Clear();
        }

        /// <summary>
        /// Update usage information about the specified clip in the clip data table.
        /// </summary>
        /// <remarks>
        /// This method works by looking at shaders used by materials assigned to the specified renderers.
        /// Usage and property renames for the clip are updated, if a binding in the clip matches an upgrader.
        /// Internal only for testability.
        /// </remarks>
        /// <param name="clip">An animation clip.</param>
        /// <param name="clipData">
        /// A table mapping clips to other data about them. (See also <seealso cref="GetAssetDataForClipsFiltered"/>.)
        /// </param>
        /// <param name="renderersByPath">
        /// A table mapping transform paths of renderers to lists of the materials they use.
        /// </param>
        /// <param name="allUpgradePathsToNewShaders">
        /// A table of new shader names and all known upgrade paths to them in the target pipeline.
        /// (See also <seealso cref="UpgradeUtility.GetAllUpgradePathsToShaders"/>.)
        /// </param>
        /// <param name="upgradePathsUsedByMaterials">
        /// Optional table of materials known to have gone through a specific upgrade path.
        /// </param>
        internal static void GatherClipUsage(
            IAnimationClip clip,
            IDictionary<
                IAnimationClip,
                (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)
            > clipData,
            IReadOnlyDictionary<string, (IRenderer Renderer, List<IMaterial> Materials)> renderersByPath,
            IReadOnlyDictionary<string, IReadOnlyList<MaterialUpgrader>> allUpgradePathsToNewShaders,
            IReadOnlyDictionary<UID, MaterialUpgrader> upgradePathsUsedByMaterials
        )
        {
            // exit if clip is unknown; it may have been filtered at an earlier stage
            if (!clipData.TryGetValue(clip, out var data))
                return;

            // see if any animated material bindings in the clip refer to renderers in animated hierarchy
            foreach (var binding in data.Bindings)
            {
                // skip if binding is not for material, or refers to a nonexistent renderer
                if (
                    !IsMaterialBinding(binding)
                    || !renderersByPath.TryGetValue(binding.path, out var rendererData)
                ) continue;

                // determine the shader property name and type from the binding
                var shaderProperty = InferShaderProperty(binding);
                var renameType = shaderProperty.Type == ShaderPropertyType.Color
                    ? MaterialUpgrader.MaterialPropertyType.Color
                    : MaterialUpgrader.MaterialPropertyType.Float;

                // material property animations apply to all materials, so check shader usage in all of them
                foreach (var material in rendererData.Materials)
                {
                    var usage = UpgradeUtility.GetNewPropertyName(
                        shaderProperty.Name,
                        material,
                        renameType,
                        allUpgradePathsToNewShaders,
                        upgradePathsUsedByMaterials,
                        out var newPropertyName
                    );

                    // if the property has already been upgraded with a different name, mark the upgrade as ambiguous
                    if (
                        usage == SerializedShaderPropertyUsage.UsedByUpgraded
                        && data.PropertyRenames.TryGetValue(binding, out var propertyRename)
                        && propertyRename != newPropertyName
                    )
                        usage |= SerializedShaderPropertyUsage.UsedByAmbiguouslyUpgraded;

                    data.Usage |= usage;
                    data.PropertyRenames[binding] = newPropertyName;
                }
            }

            clipData[clip] = data;
        }

        /// <summary>
        /// Upgrade the specified clips using the associated property rename table.
        /// </summary>
        /// <param name="clipsToUpgrade">
        /// A table mapping clips to property renaming tables that can be safely applied to their bindings.
        /// </param>
        /// <param name="excludeFlags">Do not upgrade clips that have any of these flags set.</param>
        /// <param name="upgraded">Collector for all clips that are upgraded.</param>
        /// <param name="notUpgraded">Collector for all clips that are not upgraded.</param>
        /// <param name="progressFunctor">Optional functor to display a progress bar.</param>
        internal static void UpgradeClips(
            IDictionary<IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)> clipsToUpgrade,
            SerializedShaderPropertyUsage excludeFlags,
            HashSet<(IAnimationClip Clip, ClipPath Path, SerializedShaderPropertyUsage Usage)> upgraded,
            HashSet<(IAnimationClip Clip, ClipPath Path, SerializedShaderPropertyUsage Usage)> notUpgraded,
            Func<string, float, bool> progressFunctor = null
        )
        {
            upgraded.Clear();
            notUpgraded.Clear();

            int clipIndex = 0;
            int totalNumberOfClips = clipsToUpgrade.Count;

            foreach (var kv in clipsToUpgrade)
            {
                float currentProgress = (float)++clipIndex / totalNumberOfClips;
                if (progressFunctor != null && progressFunctor($"({clipIndex} of {totalNumberOfClips}) {kv.Value.Path.Path}", currentProgress))
                    break;

                if (kv.Value.Usage == SerializedShaderPropertyUsage.Unknown || (kv.Value.Usage & excludeFlags) != 0)
                {
                    notUpgraded.Add((kv.Key, kv.Value.Path, kv.Value.Usage));
                    continue;
                }

                var renames = kv.Value.PropertyRenames;
                var bindings = kv.Key.GetCurveBindings().Where(IsMaterialBinding).ToArray();
                if (bindings.Length > 0)
                {
                    var newBindings = new EditorCurveBinding[bindings.Length];

                    for (int i = 0; i < bindings.Length; ++i)
                    {
                        var binding = bindings[i];

                        newBindings[i] = binding;
                        if (renames.TryGetValue(binding, out var newName))
                            newBindings[i].propertyName = k_MatchMaterialPropertyName.Replace(newBindings[i].propertyName, $"material.{newName}$2");
                    }

                    kv.Key.ReplaceBindings(bindings, newBindings);
                }

                upgraded.Add((kv.Key, kv.Value.Path, kv.Value.Usage));
            }
        }

        /// <summary>
        /// A function to call from a menu item callback, which will upgrade all <see cref="AnimationClip"/> in the project.
        /// </summary>
        /// <param name="allUpgraders">All <see cref="MaterialUpgrader"/> for the current render pipeline.</param>
        /// <param name="knownUpgradePaths">
        /// Optional mapping of materials to upgraders they are known to have used.
        /// Without this mapping, the method makes inferences about how a material might have been upgraded.
        /// Making these inferences is slower and possibly not sensitive to ambiguous upgrade paths.
        /// </param>
        /// <param name="filterFlags">
        /// Optional flags to filter out clips that are used in ways that are not safe for upgrading.
        /// </param>
        public static void DoUpgradeAllClipsMenuItem(
            IEnumerable<MaterialUpgrader> allUpgraders,
            string progressBarName,
            IReadOnlyDictionary<UID, MaterialUpgrader> knownUpgradePaths = default,
            SerializedShaderPropertyUsage filterFlags = ~(SerializedShaderPropertyUsage.UsedByUpgraded | SerializedShaderPropertyUsage.UsedByNonUpgraded)
        )
        {
            var clipPaths = AssetDatabase.FindAssets("t:AnimationClip")
                .Select(p => (ClipPath)AssetDatabase.GUIDToAssetPath(p))
                .ToArray();
            DoUpgradeClipsMenuItem(clipPaths, allUpgraders, progressBarName, knownUpgradePaths, filterFlags);
        }

        static void DoUpgradeClipsMenuItem(
            ClipPath[] clipPaths,
            IEnumerable<MaterialUpgrader> allUpgraders,
            string progressBarName,
            IReadOnlyDictionary<UID, MaterialUpgrader> upgradePathsUsedByMaterials,
            SerializedShaderPropertyUsage filterFlags
        )
        {
            // exit early if no clips
            if (clipPaths?.Length == 0)
                return;

            // display dialog box
            var dialogMessage = L10n.Tr(
                "Upgrading Material curves in AnimationClips assumes you have already upgraded Materials and shaders as needed. " +
                "It also requires loading assets that use clips to inspect their usage, which can be a slow process. " +
                "Do you want to proceed?"
            );

            if (!EditorUtility.DisplayDialog(
                L10n.Tr("Upgrade AnimationClips"),
                dialogMessage,
                DialogText.proceed,
                DialogText.cancel))
                return;

            // only include scene paths if user requested it
            var prefabPaths = AssetDatabase.FindAssets("t:Prefab")
                .Select(p => (PrefabPath)AssetDatabase.GUIDToAssetPath(p))
                .ToArray();
            var scenePaths = AssetDatabase.FindAssets("t:Scene")
                .Select(p => (ScenePath)AssetDatabase.GUIDToAssetPath(p))
                .ToArray();

            // retrieve clip assets with material animation
            var clipData = GetAssetDataForClipsFiltered(clipPaths);

            const float kGatherInPrefabsTotalProgress = 0.33f;
            const float kGatherInScenesTotalProgress = 0.66f;
            const float kUpgradeClipsTotalProgress = 1f;

            // create table mapping all upgrade paths to new shaders
            var allUpgradePathsToNewShaders = UpgradeUtility.GetAllUpgradePathsToShaders(allUpgraders);

            // retrieve interdependencies with prefabs to figure out which clips can be safely upgraded
            GetClipDependencyMappings(clipPaths, prefabPaths, out var clipPrefabDependents, out var prefabDependencies);

            GatherClipsUsageInDependentPrefabs(
                clipPrefabDependents, prefabDependencies, clipData, allUpgradePathsToNewShaders, upgradePathsUsedByMaterials,
                (info, progress) => EditorUtility.DisplayCancelableProgressBar(progressBarName, $"Gathering from prefabs {info}", Mathf.Lerp(0f, kGatherInPrefabsTotalProgress, progress))
            );

            if (EditorUtility.DisplayCancelableProgressBar(progressBarName, "", kGatherInPrefabsTotalProgress))
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            // if any scenes should be considered, do the same for clips used by scenes
            if (scenePaths.Any())
            {
                GetClipDependencyMappings(clipPaths, scenePaths, out var clipSceneDependents, out var sceneDependencies);
                GatherClipsUsageInDependentScenes(
                    clipSceneDependents, sceneDependencies, clipData, allUpgradePathsToNewShaders, upgradePathsUsedByMaterials,
                    (info, progress) => EditorUtility.DisplayCancelableProgressBar(progressBarName, $"Gathering from scenes {info}", Mathf.Lerp(kGatherInPrefabsTotalProgress, kGatherInScenesTotalProgress, progress))
                );
            }

            if (EditorUtility.DisplayCancelableProgressBar(progressBarName, "", kGatherInScenesTotalProgress))
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            // patch clips that should be upgraded
            var upgraded = new HashSet<(IAnimationClip Clip, ClipPath Path, SerializedShaderPropertyUsage Usage)>();
            var notUpgraded = new HashSet<(IAnimationClip Clip, ClipPath Path, SerializedShaderPropertyUsage Usage)>();

            AssetDatabase.StartAssetEditing();
            UpgradeClips(
                clipData, filterFlags, upgraded, notUpgraded,
                (info, progress) => EditorUtility.DisplayCancelableProgressBar(progressBarName, $"Upgrading clips {info}", Mathf.Lerp(kGatherInScenesTotalProgress, kUpgradeClipsTotalProgress, progress))
            );
            AssetDatabase.SaveAssets();
            AssetDatabase.StopAssetEditing();

            EditorUtility.ClearProgressBar();

            // report results
            if (upgraded.Count > 0)
            {
                var successes = upgraded.Select(data => $"- {data.Path}: ({data.Usage})");
                Debug.Log(
                    "Upgraded the following clips:\n" +
                    $"{string.Join("\n", successes)}"
                );
            }

            if (notUpgraded.Count == clipData.Count)
            {
                Debug.LogWarning("No clips were upgraded. Did you remember to upgrade materials first?");
            }
            else if (notUpgraded.Count > 0)
            {
                var errors = notUpgraded.Select(data => $"- {data.Path}: ({data.Usage})");
                Debug.LogWarning(
                    $"Did not modify following clips because they they were used in ways other than {~filterFlags}:\n" +
                    $"{string.Join("\n", errors)}"
                );
            }
        }
    }
}
