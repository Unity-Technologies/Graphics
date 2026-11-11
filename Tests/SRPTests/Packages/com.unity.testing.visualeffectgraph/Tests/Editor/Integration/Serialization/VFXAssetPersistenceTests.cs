using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.Serialization
{
    [TestFixture]
    public class VFXAssetPersistenceTests
    {
        private static readonly string kTestAssetPath = VFXSerializationTestHelper.kTestAssetPath;
        private bool m_CreatedTestAsset;
        private bool m_CreatedTestAssetDirectory;

        [OneTimeSetUp]
        public void SetUp()
        {
            VFXSerializationTestHelper.CreateSeedAsset(out m_CreatedTestAsset, out m_CreatedTestAssetDirectory);
        }

        /*
        [Test]
        public void SerializeModel()
        {
            VisualEffectAsset assetSrc = new VisualEffectAsset();
            VisualEffectAsset assetDst = new VisualEffectAsset();

            InitAsset(assetSrc);
            EditorUtility.CopySerialized(assetSrc, assetDst);
            CheckAsset(assetDst);

            Object.DestroyImmediate(assetSrc);
            Object.DestroyImmediate(assetDst);
        }*/

        [Test]
        public void LoadAssetFromPath()
        {
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(kTestAssetPath);
            CheckAsset(asset);
        }

        private void CheckAsset(VisualEffectAsset asset)
        {
            VisualEffectResource resource = asset.GetResource();
            var graph = resource.GetGraph();

            Assert.AreEqual(6, graph.GetNbChildren());

            Assert.AreEqual(1, graph[0].GetNbChildren());
            Assert.AreEqual(1, graph[1].GetNbChildren());
            Assert.AreEqual(1, graph[2].GetNbChildren());
            Assert.AreEqual(0, graph[3].GetNbChildren());
            Assert.AreEqual(0, graph[4].GetNbChildren());
            Assert.AreEqual(0, graph[5].GetNbChildren());

            Assert.IsNotNull((graph[0])[0]);
            Assert.IsNotNull((graph[1])[0]);
            Assert.IsNotNull((graph[2])[0]);

            Assert.AreEqual(VFXContextType.Init,   ((VFXContext)(graph[0])).contextType);
            Assert.AreEqual(VFXContextType.Update, ((VFXContext)(graph[1])).contextType);
            Assert.AreEqual(VFXContextType.Output, ((VFXContext)(graph[2])).contextType);
            Assert.AreEqual(VFXContextType.Init,   ((VFXContext)(graph[3])).contextType);
            Assert.AreEqual(VFXContextType.Output, ((VFXContext)(graph[4])).contextType);

            Assert.IsNotNull(graph[5] as Operator.Add);
        }

        //Cover unexpected behavior : 1307562
        [Test]
        public void Verify_Orphan_Dependencies_Are_Correctly_Cleared()
        {
            string path = null;
            {
                var graph = VFXTestCommon.MakeTemporaryGraph();
                path = AssetDatabase.GetAssetPath(graph);

                var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
                var blockConstantRate = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();
                var slotCount = blockConstantRate.GetInputSlot(0);

                var basicInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
                var quadOutput = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();
                quadOutput.SetSettingValue("blendMode", VFXAbstractParticleOutput.BlendMode.Additive);

                var setPosition = ScriptableObject.CreateInstance<Block.SetAttribute>();
                setPosition.SetSettingValue("attribute", "position");
                setPosition.inputSlots[0].value = VFX.Position.defaultValue;
                basicInitialize.AddChild(setPosition);

                slotCount.value = 1.0f;

                spawnerContext.AddChild(blockConstantRate);
                graph.AddChild(spawnerContext);
                graph.AddChild(basicInitialize);
                graph.AddChild(quadOutput);

                basicInitialize.LinkFrom(spawnerContext);
                quadOutput.LinkFrom(basicInitialize);
            }

            var recordedSize = new List<long>();
            for (uint i = 0; i < 16; ++i)
            {
                AssetDatabase.ImportAsset(path);
                var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                var graph = asset.GetResource().GetGraph();
                graph.GetResource().WriteAsset();

                recordedSize.Add(new FileInfo(path).Length);

                var quadOutput = graph.children.OfType<VFXPlanarPrimitiveOutput>().FirstOrDefault();

                quadOutput.UnlinkAll();
                graph.RemoveChild(quadOutput);

                var newQuadOutput = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();
                newQuadOutput.SetSettingValue("blendMode", VFXAbstractParticleOutput.BlendMode.Additive);

                graph.AddChild(newQuadOutput);
                var basicInitialize = graph.children.OfType<VFXBasicInitialize>().FirstOrDefault();
                newQuadOutput.LinkFrom(basicInitialize);
            }

            Assert.AreEqual(1, recordedSize.GroupBy(o => o).Count());
            Assert.AreNotEqual(0u, recordedSize[0]);
        }

        [Test]
        public void Check_Directory_Not_Imported()
        {
            var folderEndingWithVFX = "Assets/FolderTest.vfx";
            Directory.CreateDirectory(folderEndingWithVFX);
// See this PR https://github.com/Unity-Technologies/Graphics/pull/6890
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            AssetDatabase.Refresh();
            Assert.False(VisualEffectAssetModificationProcessor.HasVFXExtension(folderEndingWithVFX));
#else
            Assert.True(VisualEffectAssetModificationProcessor.HasVFXExtension(folderEndingWithVFX));
#endif

            Directory.Delete(folderEndingWithVFX);
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            AssetDatabase.Refresh();
#endif
        }

        [Test]
        public void Check_Null_Or_Empty_Path_Not_Imported()
        {
            Assert.False(VisualEffectAssetModificationProcessor.HasVFXExtension(null));
            Assert.False(VisualEffectAssetModificationProcessor.HasVFXExtension(string.Empty));
        }

        [Test(Description = "Cover issue 1403202")]
        public void Check_GPU_Event_In_Subgraph_With_Link()
        {
            string kSourceAssetMain = "Packages/com.unity.testing.visualeffectgraph/Tests/TestData/Subgraphs/GPUEvents/VFXGPUEvent_Main.vfx_";
            var main = VFXTestCommon.CopyTemporaryGraph(kSourceAssetMain);

            string kSourceAssetSubGraph = "Packages/com.unity.testing.visualeffectgraph/Tests/TestData/Subgraphs/GPUEvents/VFXGPUEvent_Subgraph.vfx_";
            var subGraph = VFXTestCommon.CopyTemporaryGraph(kSourceAssetSubGraph);

            var subgraphContext = main.children.OfType<VFXSubgraphContext>().FirstOrDefault();
            Assert.IsNotNull(subgraphContext);

            var subGraphAsset = subGraph.GetResource().asset;
            Assert.IsNotNull(subGraphAsset);

            subgraphContext.SetSettingValue("m_Subgraph", subGraphAsset);
            Assert.AreEqual(subGraphAsset, subgraphContext.GetSettingValue("m_Subgraph"));

            VFXTestCommon.ReimportVFXGraph(main);
        }


        //Cover regression test : 1315191
        [UnityTest]
        public IEnumerator Save_Then_Modify_Something_Check_The_Content_Isnt_Reverted()
        {
            string path = null;
            uint baseValue = 100;
            var graph = VFXTestCommon.MakeTemporaryGraph();
            {
                path = AssetDatabase.GetAssetPath(graph);

                var unsigned = ScriptableObject.CreateInstance<VFXInlineOperator>();
                unsigned.SetSettingValue("m_Type", (SerializableType)typeof(uint));
                unsigned.inputSlots[0].value = baseValue;
                graph.AddChild(unsigned);

                AssetDatabase.ImportAsset(path);
            }
            yield return null;

            for (uint i = 0; i < 3; ++i)
            {
                var inlineOperator = graph.children.OfType<VFXInlineOperator>().FirstOrDefault();
                Assert.IsNotNull(inlineOperator);
                Assert.AreEqual(baseValue + i, (uint)inlineOperator.inputSlots[0].value, "Failing at iteration : " + i);
                graph.GetResource().WriteAsset();

                inlineOperator.inputSlots[0].value = baseValue + i + 1; //Update for next iteration
                Assert.AreEqual(baseValue + i + 1, (uint)inlineOperator.inputSlots[0].value);
                AssetDatabase.ImportAsset(path);
                yield return null;
            }
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            try
            {
                VFXTestCommon.DeleteAllTemporaryGraph();
            }
            finally
            {
                VFXSerializationTestHelper.CleanupSeedAsset(m_CreatedTestAsset, m_CreatedTestAssetDirectory);
            }
        }
    }

}
