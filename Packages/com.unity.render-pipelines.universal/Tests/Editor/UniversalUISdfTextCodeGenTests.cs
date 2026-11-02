using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Graphing;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.Tests
{
    [TestFixture]
    class UniversalUISdfTextCodeGenTests
    {
        const string k_TestAssetPath = "Assets/__UISdfTextCodeGenTest.shadergraph";

        static readonly (string name, SlotValueType type)[] k_TextParameters =
        {
            ("Dilate", SlotValueType.Vector1),
            ("Sharpness", SlotValueType.Vector1),
            ("Outline Color", SlotValueType.Vector4),
            ("Outline Dilate", SlotValueType.Vector1),
            ("Shadow Color", SlotValueType.Vector4),
            ("Shadow Offset", SlotValueType.Vector2),
            ("Shadow Softness", SlotValueType.Vector1),
        };

        string m_GeneratedAssetPath;

        [TearDown]
        public void TearDown()
        {
            if (m_GeneratedAssetPath != null)
                AssetDatabase.DeleteAsset(m_GeneratedAssetPath);
        }

        static GraphData NewUIGraph(out BlockNode baseColorBlock)
        {
            var graph = new GraphData();
            graph.AddContexts();

            var target = new UniversalTarget();
            graph.InitializeOutputs(new Target[] { target }, null);
            Assert.IsTrue(target.TrySetActiveSubTarget(typeof(UniversalUISubTarget)), "Could not activate UniversalUISubTarget.");

            baseColorBlock = new BlockNode();
            baseColorBlock.Init(BlockFields.SurfaceDescription.BaseColor);
            graph.AddBlock(baseColorBlock, graph.fragmentContext, graph.fragmentContext.blocks.Count);
            return graph;
        }

        static MaterialSlot FindSlot(AbstractMaterialNode node, string name, SlotType slotType)
        {
            var slots = new List<MaterialSlot>();
            node.GetSlots(slots);
            var slot = slots.FirstOrDefault(s => s.slotType == slotType && s.RawDisplayName() == name);
            Assert.IsNotNull(slot, $"{node.name} has no {slotType} slot named '{name}'.");
            return slot;
        }

        static SDFTextParametersNode AddParametersNodeWiredIntoVertexPosition(GraphData graph)
        {
            var positionBlock = graph.vertexContext.blocks.Select(b => b.value).FirstOrDefault(b => b.descriptor == BlockFields.VertexDescription.Position);
            if (positionBlock == null)
            {
                positionBlock = new BlockNode();
                positionBlock.Init(BlockFields.VertexDescription.Position);
                graph.AddBlock(positionBlock, graph.vertexContext, graph.vertexContext.blocks.Count);
            }

            var parameters = new SDFTextParametersNode();
            graph.AddNode(parameters);
            graph.Connect(parameters.GetSlotReference(FindSlot(parameters, "Dilate", SlotType.Output).id), positionBlock.GetSlotReference(0));
            return parameters;
        }

        static DefaultSDFTextNode AddSdfTextNode(GraphData graph, BlockNode baseColorBlock)
        {
            var sdfText = new DefaultSDFTextNode();
            graph.AddNode(sdfText);
            graph.Connect(sdfText.GetSlotReference(FindSlot(sdfText, "SDF Text", SlotType.Output).id), baseColorBlock.GetSlotReference(0));
            return sdfText;
        }

        static SDFTextParametersNode AddParametersNodeWiredInto(GraphData graph, DefaultSDFTextNode sdfText)
        {
            var parameters = new SDFTextParametersNode();
            graph.AddNode(parameters);
            foreach (var (name, _) in k_TextParameters)
            {
                graph.Connect(
                    parameters.GetSlotReference(FindSlot(parameters, name, SlotType.Output).id),
                    sdfText.GetSlotReference(FindSlot(sdfText, name, SlotType.Input).id));
            }
            return parameters;
        }

        static void AssertGeneratesWithoutTemplateErrors(GraphData graph)
        {
            graph.ValidateGraph();
            var code = new Generator(graph, graph.outputNode, GenerationMode.ForReals, "UISdfTextCodeGen").generatedShader;

            // A malformed template directive splices a caret marker into the shader instead of failing.
            StringAssert.DoesNotMatch(@"(?m)^\s*//\s*\^ ", code, "template preprocessor error");
        }

        [Test]
        public void DefaultSDFTextNode_ExposesOneInputPerTextParameter()
        {
            var node = new DefaultSDFTextNode();

            foreach (var (name, type) in k_TextParameters)
            {
                var slot = FindSlot(node, name, SlotType.Input);
                Assert.AreEqual(type, slot.valueType, name);
                Assert.AreEqual("Default", slot.CustomBinding, name);
            }

            // Serialized graphs reference the original slots by id.
            Assert.AreEqual(0, FindSlot(node, "Tint", SlotType.Input).id);
            Assert.AreEqual(1, FindSlot(node, "SDF Text", SlotType.Output).id);
        }

        [Test]
        public void SDFTextParametersNode_ExposesOneOutputPerTextParameterInBothStages()
        {
            var node = new SDFTextParametersNode();
            var outputs = new List<MaterialSlot>();
            node.GetOutputSlots(outputs);
            Assert.AreEqual(k_TextParameters.Length, outputs.Count);

            foreach (var (name, type) in k_TextParameters)
            {
                var slot = FindSlot(node, name, SlotType.Output);
                Assert.AreEqual(type, slot.valueType, name);
                Assert.AreEqual(ShaderStageCapability.All, slot.stageCapability, name);
            }
        }

#if ENABLE_CORECLR
        [Explicit("MultiJson-serialized ShaderGraph written to disk fails to reimport on CoreCLR (JSON parse error: The document root must not follow by other values), so no Shader sub-assets are produced, see https://jira.unity3d.com/browse/UUM-150436")]
#endif
        [Test]
        public void UIGraph_WithTextParametersInVertexStage_Compiles()
        {
            var graph = NewUIGraph(out _);
            AddParametersNodeWiredIntoVertexPosition(graph);
            AssertGeneratesWithoutTemplateErrors(graph);

            var path = AssetDatabase.GenerateUniqueAssetPath(k_TestAssetPath);
            m_GeneratedAssetPath = path;
            FileUtilities.WriteShaderGraphToDisk(path, graph);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);

            AssertShaderCompiles(path);
        }

#if ENABLE_CORECLR
        [Explicit("MultiJson-serialized ShaderGraph written to disk fails to reimport on CoreCLR (JSON parse error: The document root must not follow by other values), so no Shader sub-assets are produced, see https://jira.unity3d.com/browse/UUM-150436")]
#endif
        [Test]
        public void UIGraph_WithTextParametersWiredIntoDefaultSDFText_Compiles()
        {
            var graph = NewUIGraph(out var baseColorBlock);
            var sdfText = AddSdfTextNode(graph, baseColorBlock);
            AddParametersNodeWiredInto(graph, sdfText);
            AssertGeneratesWithoutTemplateErrors(graph);

            var path = AssetDatabase.GenerateUniqueAssetPath(k_TestAssetPath);
            m_GeneratedAssetPath = path;
            FileUtilities.WriteShaderGraphToDisk(path, graph);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);

            AssertShaderCompiles(path);
        }

#if ENABLE_CORECLR
        [Explicit("MultiJson-serialized ShaderGraph written to disk fails to reimport on CoreCLR (JSON parse error: The document root must not follow by other values), so no Shader sub-assets are produced, see https://jira.unity3d.com/browse/UUM-150436")]
#endif
        [Test]
        public void UIGraph_WithRenderTypeAndBitmapTextNodes_Compiles()
        {
            // Both nodes emit the shared text-fragment predicates from UnityUIE.cginc.
            var graph = NewUIGraph(out var baseColorBlock);
            var renderType = new RenderTypeNode();
            graph.AddNode(renderType);
            graph.Connect(renderType.GetSlotReference(FindSlot(renderType, "SDF Text", SlotType.Output).id), baseColorBlock.GetSlotReference(0));

            var alphaBlock = new BlockNode();
            alphaBlock.Init(BlockFields.SurfaceDescription.Alpha);
            graph.AddBlock(alphaBlock, graph.fragmentContext, graph.fragmentContext.blocks.Count);
            var bitmapText = new DefaultBitmapTextNode();
            graph.AddNode(bitmapText);
            graph.Connect(bitmapText.GetSlotReference(FindSlot(bitmapText, "Bitmap Text", SlotType.Output).id), alphaBlock.GetSlotReference(0));
            AssertGeneratesWithoutTemplateErrors(graph);

            var path = AssetDatabase.GenerateUniqueAssetPath(k_TestAssetPath);
            m_GeneratedAssetPath = path;
            FileUtilities.WriteShaderGraphToDisk(path, graph);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);

            AssertShaderCompiles(path);
        }

        static void AssertShaderCompiles(string path)
        {
            var shader = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Shader>().FirstOrDefault();
            Assert.IsNotNull(shader, "No Shader asset was produced for the UI graph.");
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "The generated UI shader has compile errors:\n" + string.Join("\n", ShaderUtil.GetShaderMessages(shader).Select(m => m.message)));
        }
    }
}
