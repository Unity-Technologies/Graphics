using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Rendering.Universal.Converters;
using ClipPath = UnityEditor.Rendering.AnimationClipUpgrader.ClipPath;
using ClipProxy = UnityEditor.Rendering.AnimationClipUpgrader.AnimationClipProxy;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Rendering.Universal
{
    internal sealed class AnimationClipConverter : RenderPipelineConverter
    {
        public override string name => "Animation Clip Converter";
        public override string info => "Need to update all Animation Clips. This will run after Materials has been converted.";
        public override string category { get; }
        public override Type container => typeof(BuiltInToURPConverterContainer);

        List<GlobalObjectId> m_AssetsToConvert = new List<GlobalObjectId>(64);

        IDictionary<AnimationClipUpgrader.IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)> m_ClipData =
            new Dictionary<AnimationClipUpgrader.IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)>();

        public override void OnInitialize(InitializeConverterContext ctx, Action callback)
        {
            // get paths to all animation clips
            var clipPaths = AssetDatabase.FindAssets("t:AnimationClip")
                .Select(p => (ClipPath)AssetDatabase.GUIDToAssetPath(p))
                .ToArray();

            // retrieve clip assets with material animation
            m_ClipData = AnimationClipUpgrader.GetAssetDataForClipsFiltered(clipPaths);

            // collect all clips and add them to the context
            var keys = m_ClipData.Keys.ToArray();
            var clipIds = new GlobalObjectId[keys.Length];
            GlobalObjectId.GetGlobalObjectIdsSlow(keys.Select(c => c.Clip as UnityObject).ToArray(), clipIds);
            for (int i = 0; i < keys.Length; ++i)
            {
                AnimationClipUpgrader.IAnimationClip key = keys[i];
                var cd = m_ClipData[key];
                var clip = key.Clip;
                var item = new ConverterItemDescriptor()
                {
                    name = clip.name,
                    info = cd.Path,
                };
                // TODO: need to know how materials will be upgraded in order to generate warnings
                m_AssetsToConvert.Add(clipIds[i]);

                ctx.AddAssetToConvert(item);
            }

            callback.Invoke();
        }

        public override void OnPreRun()
        {
            // get paths to all animation clips
            var clipPaths = new HashSet<ClipPath>(m_ClipData.Values.Select(cd => cd.Path));

            // get paths to all prefabs and scenes in order to inspect clip usage
            var prefabPaths = AssetDatabase.FindAssets("t:Prefab")
                .Select(p => (AnimationClipUpgrader.PrefabPath)AssetDatabase.GUIDToAssetPath(p))
                .ToArray();
            var scenePaths = AssetDatabase.FindAssets("t:Scene")
                .Select(p => (AnimationClipUpgrader.ScenePath)AssetDatabase.GUIDToAssetPath(p))
                .ToArray();

            // create table mapping all upgrade paths to new shaders
            var upgraders = new UniversalRenderPipelineMaterialUpgrader().upgraders;
            var allUpgradePathsToNewShaders = UpgradeUtility.GetAllUpgradePathsToShaders(upgraders);

            // TODO: could pass in upgrade paths used by materials in the future

            // retrieve interdependencies with prefabs to figure out which clips can be safely upgraded
            AnimationClipUpgrader.GetClipDependencyMappings(clipPaths, prefabPaths, out var clipPrefabDependents, out var prefabDependencies);
            AnimationClipUpgrader.GatherClipsUsageInDependentPrefabs(
                clipPrefabDependents, prefabDependencies, m_ClipData, allUpgradePathsToNewShaders, default, default);

            // do the same for clips used by scenes
            AnimationClipUpgrader.GetClipDependencyMappings(clipPaths, scenePaths, out var clipSceneDependents, out var sceneDependencies);
            AnimationClipUpgrader.GatherClipsUsageInDependentScenes(
                clipSceneDependents, sceneDependencies, m_ClipData, allUpgradePathsToNewShaders, default, default);
        }

        HashSet<(AnimationClipUpgrader.IAnimationClip Clip, ClipPath Path, SerializedShaderPropertyUsage Usage)> m_Upgraded =
            new HashSet<(AnimationClipUpgrader.IAnimationClip Clip, ClipPath Path, SerializedShaderPropertyUsage Usage)>();

        HashSet<(AnimationClipUpgrader.IAnimationClip Clip, ClipPath Path, SerializedShaderPropertyUsage Usage)> m_NotUpgraded =
            new HashSet<(AnimationClipUpgrader.IAnimationClip Clip, ClipPath Path, SerializedShaderPropertyUsage Usage)>();

        IDictionary<AnimationClipUpgrader.IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)> m_TempClipData =
            new Dictionary<AnimationClipUpgrader.IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)>();

        public override void OnRun(ref RunItemContext ctx)
        {
            // filter flags are used to determine which clips to skip over during upgrade process
            // we want to process all clips that are not ambiguously upgraded
            const SerializedShaderPropertyUsage kFilterFlags =
                ~(
                    SerializedShaderPropertyUsage.UsedByUpgraded
                    | SerializedShaderPropertyUsage.UsedByNonUpgraded
                );

            const SerializedShaderPropertyUsage kSuccessFlags =
                SerializedShaderPropertyUsage.UsedByUpgraded;

            var clipKey = (ClipProxy)GlobalObjectId.GlobalObjectIdentifierToObjectSlow(m_AssetsToConvert[ctx.item.index]);

            m_TempClipData.Clear();
            m_TempClipData[clipKey] = m_ClipData[clipKey];

            m_Upgraded.Clear();
            m_NotUpgraded.Clear();

            AnimationClipUpgrader.UpgradeClips(m_TempClipData, kFilterFlags, m_Upgraded, m_NotUpgraded, default);

            var usage = m_TempClipData[clipKey].Usage;

            // Success
            if ((usage & kSuccessFlags) != 0 && (usage & kFilterFlags) == 0)
                return;

            if (usage == SerializedShaderPropertyUsage.Unknown)
            {
                ctx.didFail = true;
                ctx.info = L10n.Tr("The animation clip is not used by any objects with renderers currently in the project, so it may not be safe to automatically upgrade.");
                return;
            }

            var sb = new StringBuilder();

            sb.Append(L10n.Tr("The animation clip was not modified for one or more reasons:"));

            if ((usage & SerializedShaderPropertyUsage.UsedByAmbiguouslyUpgraded) != 0)
            {
                ctx.didFail = true;
                sb.Append(L10n.Tr("\n - The animation clip is used by objects with materials that took different upgrade paths for the animated property."));
            }

            if ((usage & SerializedShaderPropertyUsage.UsedByNonUpgraded) != 0)
            {
                ctx.didFail = true;
                sb.Append(L10n.Tr("\n - The animation clip is used by objects with materials that have not been upgraded."));
            }

            if (ctx.didFail)
                ctx.info = sb.ToString();
        }

        public override void OnClicked(int index)
        {
            var id = m_AssetsToConvert[index];

            var clip = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
            if (clip == null)
                return;

            EditorGUIUtility.PingObject(clip);
        }
    }
}
