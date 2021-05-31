using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.Rendering.Universal.Converters;
using UnityEngine;
using ClipPath = UnityEditor.Rendering.AnimationClipUpgrader.ClipPath;

namespace UnityEditor.Rendering.Universal
{
    internal sealed class AnimationClipConverter : RenderPipelineConverter
    {
        public override string name => "Animation Clip Converter";
        public override string info => "Need to update all Animation Clips. This will run after Materials has been converted.";
        public override string category { get; }
        public override Type container => typeof(BuiltInToURPConverterContainer);

        List<string> m_AssetsToConvert = new List<string>();
        public override void OnInitialize(InitializeConverterContext ctx, Action callback)
        {
            var clipPaths = AssetDatabase.FindAssets("t:AnimationClip")
                .Select(p => (ClipPath)AssetDatabase.GUIDToAssetPath(p))
                .ToArray();

            // only include scene paths if user requested it
            var prefabPaths = AssetDatabase.FindAssets("t:Prefab")
                .Select(p => (AnimationClipUpgrader.PrefabPath)AssetDatabase.GUIDToAssetPath(p))
                .ToArray();
            var scenePaths = AssetDatabase.FindAssets("t:Scene")
                .Select(p => (AnimationClipUpgrader.ScenePath)AssetDatabase.GUIDToAssetPath(p))
                .ToArray();

            // retrieve clip assets with material animation
            var clipData = AnimationClipUpgrader.GetAssetDataForClipsFiltered(clipPaths);

            const float kGatherInPrefabsTotalProgress = 0.33f;
            const float kGatherInScenesTotalProgress = 0.66f;
            const float kUpgradeClipsTotalProgress = 1f;

            UniversalRenderPipelineMaterialUpgrader matUpgrader = new UniversalRenderPipelineMaterialUpgrader();

            // create table mapping all upgrade paths to new shaders
            var allUpgradePathsToNewShaders = UpgradeUtility.GetAllUpgradePathsToShaders(matUpgrader.upgraders);

            // retrieve interdependencies with prefabs to figure out which clips can be safely upgraded
            AnimationClipUpgrader.GetClipDependencyMappings(clipPaths, prefabPaths, out var clipPrefabDependents, out var prefabDependencies);

            // Upgradepathusedbymaterials could be used in the future
            AnimationClipUpgrader.GatherClipsUsageInDependentPrefabs(
                clipPrefabDependents, prefabDependencies, clipData, allUpgradePathsToNewShaders, default, default);

            // if any scenes should be considered, do the same for clips used by scenes
            if (scenePaths.Any())
            {
                AnimationClipUpgrader.GetClipDependencyMappings(clipPaths, scenePaths, out var clipSceneDependents, out var sceneDependencies);
                AnimationClipUpgrader.GatherClipsUsageInDependentScenes(
                    clipSceneDependents, sceneDependencies, clipData, allUpgradePathsToNewShaders, default, default);
            }

            foreach (var cd in clipData)
            {
                var item = new ConverterItemDescriptor()
                {
                    name = cd.Key.Clip.name, // We should add name property to IAnimationClip
                    info = cd.Value.Path,
                };
                // Can make this a bit better with a warning if pre populate list of materials
                // And check cd.value.usage
                m_AssetsToConvert.Add($"{cd.Value.Path}|{cd.Key.Clip.name}");

                ctx.AddAssetToConvert(item);
            }

            callback.Invoke();
        }

        public override void OnRun(ref RunItemContext ctx)
        {
            //convert(m_AssetsToConvert[ctx.item.index])
        }

        public override void OnClicked(int index)
        {
            string[] tokenizedString = m_AssetsToConvert[index].Split('|');
            var clips = AssetDatabase.LoadAllAssetsAtPath(tokenizedString[0]).Where(o => o is AnimationClip).Select(o => o as AnimationClip).ToList();
            EditorGUIUtility.PingObject(clips.Find(o => o.name == tokenizedString[1]));
        }
    }
}
