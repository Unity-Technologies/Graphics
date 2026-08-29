using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEngine;

namespace UnityEngine.Rendering.Universal.Tests
{
    // Tests FilterSubTarget/UniversalFilterSubTarget: node blacklist, feature-flag gating, and codegen.
    class FilterSubTargetTests
    {
        const string kTestAssetPath = "Assets/__FilterSubTargetTest.shadergraph";

        // enableFilterShaderGraph is internal to the engine module; drive its public EditorUserSettings backing value.
        const string kEnableFilterShaderGraphKey = "UIToolkit.EnableFilterShaderGraph";
        string m_OriginalEnableFilterShaderGraphValue;

        // Track the actual GenerateUniqueAssetPath result so TearDown cleans up even after mid-test failures.
        string m_GeneratedAssetPath;

        [SetUp]
        public void SetUp()
        {
            // UniversalFilterSubTarget no longer gates discovery on this flag (see FilterSubTarget.cs);
            // forced on here because a couple of tests below exercise the creation-time gate directly.
            m_OriginalEnableFilterShaderGraphValue = EditorUserSettings.GetConfigValue(kEnableFilterShaderGraphKey);
            EditorUserSettings.SetConfigValue(kEnableFilterShaderGraphKey, true.ToString());
        }

        [TearDown]
        public void TearDown()
        {
            EditorUserSettings.SetConfigValue(kEnableFilterShaderGraphKey, m_OriginalEnableFilterShaderGraphValue);

            if (m_GeneratedAssetPath != null)
                AssetDatabase.DeleteAsset(m_GeneratedAssetPath);
        }

        [Test]
        public void EnableFilterShaderGraph_DoesNotGate_SubTargetDiscovery()
        {
            // The flag must gate creation only, never discovery -- reimports by teammates without the flag must keep working.
            EditorUserSettings.SetConfigValue(kEnableFilterShaderGraphKey, false.ToString());
            Assert.IsFalse(new UniversalFilterSubTarget().isHidden,
                "Filter subtarget must always be discoverable on reimport, regardless of the local feature flag.");
        }

        [Test]
        public void ValidateCreateFilterGraph_ReflectsFeatureFlag()
        {
            EditorUserSettings.SetConfigValue(kEnableFilterShaderGraphKey, false.ToString());
            Assert.IsFalse(CreateFilterShaderGraph.ValidateCreateFilterGraph(),
                "Creating a new Filter Shader Graph must stay disabled while the feature flag is off.");

            EditorUserSettings.SetConfigValue(kEnableFilterShaderGraphKey, true.ToString());
            Assert.IsTrue(CreateFilterShaderGraph.ValidateCreateFilterGraph(),
                "Creating a new Filter Shader Graph must be enabled once the feature flag is on.");
        }

        static GraphData NewGraphWithActiveSubTarget(System.Type subTargetType)
        {
            var graph = new GraphData();
            graph.AddContexts();

            var target = new UniversalTarget();
            graph.InitializeOutputs(new Target[] { target }, null);
            Assert.IsTrue(target.TrySetActiveSubTarget(subTargetType), $"Could not activate {subTargetType.Name}.");

            graph.OnEnable();
            return graph;
        }

        [Test]
        public void SceneColorNode_IsInvalid_InFilterGraph()
        {
            // Filter graphs read their input via Filter Input, not the generic scene-sampling nodes --
            // SceneColorNode assumes a real camera/opaque-texture context a filter pass doesn't have.
            var graph = NewGraphWithActiveSubTarget(typeof(UniversalFilterSubTarget));
            var node = new SceneColorNode();
            graph.AddNode(node);

            graph.ValidateGraph();

            Assert.IsFalse(node.isValid, "SceneColorNode should be rejected by FilterSubTarget's node blacklist.");
        }

        [Test]
        public void VertexColorNode_IsInvalid_InFilterGraph()
        {
            var graph = NewGraphWithActiveSubTarget(typeof(UniversalFilterSubTarget));
            var node = new VertexColorNode();
            graph.AddNode(node);

            graph.ValidateGraph();

            Assert.IsFalse(node.isValid, "VertexColorNode should be rejected: FilterStructs declares no color varying.");
        }

        [Test]
        public void ScreenPositionNode_IsInvalid_InFilterGraph()
        {
            var graph = NewGraphWithActiveSubTarget(typeof(UniversalFilterSubTarget));
            var node = new ScreenPositionNode();
            graph.AddNode(node);

            graph.ValidateGraph();

            Assert.IsFalse(node.isValid, "ScreenPositionNode should be rejected: FilterStructs declares no world-position varying.");
        }

        [Test]
        public void UVNode_DefaultChannel_IsValid_InFilterGraph()
        {
            var graph = NewGraphWithActiveSubTarget(typeof(UniversalFilterSubTarget));
            var node = new UVNode();
            graph.AddNode(node);

            graph.ValidateGraph();

            Assert.IsTrue(node.isValid, "UVNode defaults to UV0, which Filter graphs do support.");
        }

        [Test]
        public void UVNode_NonUV0Channel_IsInvalid_InFilterGraph()
        {
            // UVNode's channel is per-instance, invisible to the type-only check; ValidateNodeCompatibility must catch it.
            var graph = NewGraphWithActiveSubTarget(typeof(UniversalFilterSubTarget));
            var node = new UVNode { uvChannel = UVChannel.UV1 };
            graph.AddNode(node);

            graph.ValidateGraph();

            Assert.IsFalse(node.isValid, "UVNode set to UV1 should be rejected in a Filter graph.");
        }

        [Test]
        public void SampleTexture2DNode_UVSlot_NonUV0Channel_IsInvalid_InFilterGraph()
        {
            // Any node's UVMaterialSlot can point at non-UV0, not just UVNode; must be caught generically.
            var graph = NewGraphWithActiveSubTarget(typeof(UniversalFilterSubTarget));
            var node = new SampleTexture2DNode();
            graph.AddNode(node);

            var uvSlots = new List<UVMaterialSlot>();
            node.GetInputSlots(uvSlots);
            uvSlots[0].channel = UVChannel.UV1;

            graph.ValidateGraph();

            Assert.IsFalse(node.isValid, "A UV input slot set to a non-UV0 channel should be rejected in a Filter graph.");
        }

        [Test]
        public void FilterInputNode_IsValid_InFilterGraph()
        {
            var graph = NewGraphWithActiveSubTarget(typeof(UniversalFilterSubTarget));
            var node = new FilterInputNode();
            graph.AddNode(node);

            graph.ValidateGraph();

            Assert.IsTrue(node.isValid, "Filter Input should be allowed inside its own Filter subtarget.");
        }

        [Test]
        public void FilterInputNode_IsInvalid_OutsideFilterGraph()
        {
            // Outside a Filter subtarget, _MainTex/sampler_MainTex are never declared, so this must be
            // rejected at graph-validation time via [SubTargetFilter(typeof(UniversalFilterSubTarget))].
            var graph = NewGraphWithActiveSubTarget(typeof(UniversalUnlitSubTarget));
            var node = new FilterInputNode();
            graph.AddNode(node);

            graph.ValidateGraph();

            Assert.IsFalse(node.isValid, "Filter Input must not be allowed outside a Filter subtarget.");
        }

        [Test]
        public void NormalizeFilterUVNode_IsInvalid_OutsideFilterGraph()
        {
            var graph = NewGraphWithActiveSubTarget(typeof(UniversalUnlitSubTarget));
            var normalizeNode = new NormalizeFilterUVNode();
            graph.AddNode(normalizeNode);

            graph.ValidateGraph();

            Assert.IsFalse(normalizeNode.isValid, "Normalize Filter UV must not be allowed outside a Filter subtarget.");
        }

        [Test]
        public void FilterInputNode_UVSpace_DefaultsToAtlas_AndCanBeSetToLocal()
        {
            // The UV Space toggle must round-trip through the node's own property.
            var node = new FilterInputNode();
            Assert.AreEqual(FilterUVSpace.Atlas, node.uvSpace, "Filter Input must default to Atlas UV Space to preserve existing pass-through behavior.");

            node.uvSpace = FilterUVSpace.Local;
            Assert.AreEqual(FilterUVSpace.Local, node.uvSpace, "Filter Input's UV Space must be settable to Local.");
        }

#if ENABLE_CORECLR
        [Explicit("MultiJson-serialized ShaderGraph written to disk fails to reimport on CoreCLR (JSON parse error: The document root must not follow by other values), so no Shader sub-assets are produced, see https://jira.unity3d.com/browse/UUM-150436")]
#endif
        [Test]
        public void UniversalFilterSubTarget_GeneratesCompilingShader_ForTrivialPassthroughGraph()
        {
            var graph = new GraphData();
            graph.AddContexts();

            var target = new UniversalTarget();
            graph.InitializeOutputs(new Target[] { target }, null);

            bool success = target.TrySetActiveSubTarget(typeof(UniversalFilterSubTarget));
            Assert.IsTrue(success, "Could not set UniversalFilterSubTarget active on a UniversalTarget.");

            graph.OnEnable();
            graph.ValidateGraph();

            // No nodes wired -- BaseColor/Alpha blocks keep their default values (trivial pass-through).
            var path = AssetDatabase.GenerateUniqueAssetPath(kTestAssetPath);
            m_GeneratedAssetPath = path;
            FileUtilities.WriteShaderGraphToDisk(path, graph);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);

            var shader = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Shader>().FirstOrDefault();
            Assert.IsNotNull(shader, "No Shader asset was produced by UniversalFilterSubTarget for a trivial pass-through graph.");
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "The generated filter shader has compile errors:\n" + string.Join("\n", ShaderUtil.GetShaderMessages(shader).Select(m => m.message)));
        }

#if ENABLE_CORECLR
        [Explicit("MultiJson-serialized ShaderGraph written to disk fails to reimport on CoreCLR (JSON parse error: The document root must not follow by other values), so no Shader sub-assets are produced, see https://jira.unity3d.com/browse/UUM-150436")]
#endif
        [Test]
        public void UniversalFilterSubTarget_GeneratesCompilingShader_ForFilterInputWithLocalUVSpace()
        {
            // Only a real shader compile exercises the Unity_FilterInput_Local HLSL body.
            var graph = NewGraphWithActiveSubTarget(typeof(UniversalFilterSubTarget));

            var filterInput = new FilterInputNode { uvSpace = FilterUVSpace.Local };
            graph.AddNode(filterInput);

            var baseColorBlock = new BlockNode();
            baseColorBlock.Init(BlockFields.SurfaceDescription.BaseColor);
            graph.AddBlock(baseColorBlock, graph.fragmentContext, graph.fragmentContext.blocks.Count);
            graph.Connect(filterInput.GetSlotReference(1), baseColorBlock.GetSlotReference(0));

            graph.ValidateGraph();
            Assert.IsTrue(filterInput.isValid, "Filter Input with UV Space = Local should still be allowed inside its own Filter subtarget.");

            var path = AssetDatabase.GenerateUniqueAssetPath(kTestAssetPath);
            m_GeneratedAssetPath = path;
            FileUtilities.WriteShaderGraphToDisk(path, graph);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);

            var shader = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Shader>().FirstOrDefault();
            Assert.IsNotNull(shader, "No Shader asset was produced for a Filter Input node with UV Space = Local.");
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "The generated filter shader has compile errors:\n" + string.Join("\n", ShaderUtil.GetShaderMessages(shader).Select(m => m.message)));
        }
    }
}
