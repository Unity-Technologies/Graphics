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

            SerializedShaderPropertyUsage filterFlags = ~(SerializedShaderPropertyUsage.UsedByUpgraded |
                SerializedShaderPropertyUsage.UsedByNonUpgraded);
            foreach (var cd in clipData)
            {
                var item = new ConverterItemDescriptor()
                {
                    name = cd.Key.Clip.name, // We should add name property to IAnimationClip
                    info = cd.Value.Path,
                };
                if ((cd.Value.Usage & filterFlags) == filterFlags)
                {
                    item.warningMessage = cd.Value.Usage.ToString();
                }
                m_AssetsToConvert.Add(cd.Value.Path);
                ctx.AddAssetToConvert(item);
            }

            //Debug.Log("Init Animation Clip");
            callback.Invoke();
        }

        public override void OnRun(ref RunItemContext ctx)
        {
            Debug.Log("Running Animation clip");
        }

        public override void OnClicked(int index)
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<AnimationClip>(m_AssetsToConvert[index]));
        }

        // public override void OnPostRun(ConverterState converterState, List<ConverterItemDescriptor> itemsToConvert)
        // {
        //     //var matUpgraders = new List<MaterialUpgrader>();
        //     //UniversalRenderPipelineMaterialUpgrader urpUpgrader = new UniversalRenderPipelineMaterialUpgrader();
        //     //matUpgraders = urpUpgrader.upgraders;
        //
        //     //AnimationClipUpgrader.DoUpgradeAllClipsMenuItem(matUpgraders, "Upgrade Animation Clips to URP Materials");
        //
        //     // Create a list here with all our data
        //     // Populate that into items
        //     // The Converter need to know the items it supposed to show
        //     converterState.items = new List<ConverterItemState>();
        //
        //     for (int i = 0; i < 15; i++)
        //     {
        //         itemsToConvert.Add(new ConverterItemDescriptor
        //         {
        //             name = "Muppet : 12345678910111213141516171819 ::  " + i,
        //             info = "",
        //             warningMessage = "",
        //             helpLink = "",
        //         });
        //
        //         Status status;
        //         string msg;
        //         status = i % 2 == 0 ? Status.Success : Status.Error;
        //         msg = i % 2 == 0 ? "Status.Success" : "Status.Error";
        //         converterState.items.Add(new ConverterItemState
        //         {
        //             isActive = true,
        //             message = msg,
        //             status = status,
        //             hasConverted = true,
        //         });
        //         if (status == Status.Success)
        //         {
        //             converterState.success++;
        //         }
        //         else
        //         {
        //             converterState.errors++;
        //         }
        //     }
        // }
    }
}
