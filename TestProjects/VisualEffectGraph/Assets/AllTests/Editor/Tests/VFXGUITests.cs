using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.UI;
using System.IO;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXGUITests
    {
        [MenuItem("VFX Editor/Run GUI Tests")]
        static void RunGUITests()
        {
            VFXGUITests tests = new VFXGUITests();

            tests.CreateTestAsset("GUITest");
            var initContext = tests.CreateAllInitializeBlocks();
            var updateContext = tests.CreateAllUpdateBlocks();
            var outputContext = tests.CreateAllOutputBlocks();

            tests.CreateFlowEdges(initContext, updateContext, outputContext);
            tests.CreateAllOperators();
            List<VFXParameter> parameters = tests.CreateAllParameters();

            tests.CreateDataEdges(updateContext, parameters);
        }

        VFXViewPresenter m_ViewPresenter;
        VFXViewWindow m_Window;
        VFXView m_View;

        const string testAssetName = "Assets/TmpTests/{0}.asset";


        [Test]
        public void CreateFlowEdgesTest()
        {
            CreateTestAsset("GUITest4");
            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Initialize").First();
            var initContext = m_ViewPresenter.AddVFXContext(new Vector2(300, 100), initContextDesc);
            var initContextPresenter = m_ViewPresenter.allChildren.OfType<VFXContextPresenter>().First(t => t.model == initContext) as VFXContextPresenter;

            var updateContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Update").First();
            var updateContext = m_ViewPresenter.AddVFXContext(new Vector2(300, 1000), updateContextDesc);
            var updateContextPresenter = m_ViewPresenter.allChildren.OfType<VFXContextPresenter>().First(t => t.model == updateContext) as VFXContextPresenter;

            var outputContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Output").First();
            var outputContext = m_ViewPresenter.AddVFXContext(new Vector2(300, 1000), outputContextDesc);
            var outputContextPresenter = m_ViewPresenter.allChildren.OfType<VFXContextPresenter>().First(t => t.model == outputContext) as VFXContextPresenter;

            CreateFlowEdges(initContextPresenter, updateContextPresenter, outputContextPresenter);

            DestroyTestAsset("GUITest4");
        }

        void CreateFlowEdges(VFXContextPresenter initContext, VFXContextPresenter updateContext, VFXContextPresenter outputContext)
        {
            VFXFlowEdgePresenter edgePresenter = VFXFlowEdgePresenter.CreateInstance<VFXFlowEdgePresenter>();

            edgePresenter.output = initContext.flowOutputAnchors.First();
            edgePresenter.input = updateContext.flowInputAnchors.First();

            m_ViewPresenter.AddElement(edgePresenter);

            edgePresenter = VFXFlowEdgePresenter.CreateInstance<VFXFlowEdgePresenter>();

            edgePresenter.output = updateContext.flowOutputAnchors.First();
            edgePresenter.input = outputContext.flowInputAnchors.First();

            m_ViewPresenter.AddElement(edgePresenter);
        }

        void CreateDataEdges(VFXContextPresenter updateContext, List<VFXParameter> parameters)
        {
            foreach (var param in parameters)
            {
                VFXParameterPresenter paramPresenter = m_ViewPresenter.allChildren.OfType<VFXParameterPresenter>().First(t => t.model == param);

                VFXDataAnchorPresenter outputAnchor = paramPresenter.outputAnchors.First() as VFXDataAnchorPresenter;
                System.Type type = outputAnchor.anchorType;

                bool found = false;
                foreach (var block in updateContext.blockPresenters)
                {
                    foreach (var anchor in block.inputAnchors)
                    {
                        if (anchor.anchorType == type)
                        {
                            found = true;

                            (anchor as VFXDataAnchorPresenter).model.Link(outputAnchor.model);

                            break;
                        }
                    }
                    if (found)
                        break;
                }
            }
        }

        public void CreateTestAsset(string name)
        {
            VFXAsset asset = new VFXAsset();

            var filePath = string.Format(testAssetName, name);
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            AssetDatabase.CreateAsset(asset, filePath);

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<VFXAsset>(filePath);

            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            window.Close();
            window = EditorWindow.GetWindow<VFXViewWindow>();
            m_ViewPresenter = window.GetPresenter<VFXViewPresenter>();
            m_View = m_ViewPresenter.View;
        }

        void DestroyTestAsset(string name)
        {
            var filePath = string.Format(testAssetName, name);
            AssetDatabase.DeleteAsset(filePath);
            UnityEngine.Object.DestroyImmediate(Selection.activeObject);

            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            window.Close();
        }

        [Test]
        public void CreateAllInitializeBlocksTest()
        {
            CreateTestAsset("TestGUI1");
            CreateAllInitializeBlocks();
            DestroyTestAsset("TestGUI1");
        }

        VFXContextPresenter CreateAllInitializeBlocks()
        {
            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Initialize").First();

            var newContext = m_ViewPresenter.AddVFXContext(new Vector2(300, 100), initContextDesc);

            Assert.AreEqual(m_ViewPresenter.allChildren.Where(t => t is VFXContextPresenter).Count(), 1);

            var contextPresenter = m_ViewPresenter.allChildren.Where(t => t is VFXContextPresenter).First() as VFXContextPresenter;

            Assert.AreEqual(contextPresenter.model, newContext);

            // Adding every block compatible with an init context
            foreach (var block in VFXLibrary.GetBlocks().Where(t => t.AcceptParent(newContext)))
            {
                var newBlock = block.CreateInstance();
                contextPresenter.AddBlock(0, newBlock);

                Assert.AreEqual(contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).block == newBlock).Count(), 1);

                var blockPresenter = contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).block == newBlock).First() as VFXBlockPresenter;

                Assert.NotNull(blockPresenter);
            }

            return contextPresenter;
        }

        [Test]
        public void CreateAllUpdateBlocksTest()
        {
            CreateTestAsset("TestGUI2");
            CreateAllUpdateBlocks();
            DestroyTestAsset("TestGUI2");
        }

        VFXContextPresenter CreateAllUpdateBlocks()
        {
            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Update").First();

            var newContext = m_ViewPresenter.AddVFXContext(new Vector2(300, 1000), initContextDesc);

            var contextPresenter = m_ViewPresenter.allChildren.Where(t => t is VFXContextPresenter && (t as VFXContextPresenter).model == newContext).First() as VFXContextPresenter;

            Assert.AreEqual(contextPresenter.model, newContext);

            // Adding every block compatible with an init context
            foreach (var block in VFXLibrary.GetBlocks().Where(t => t.AcceptParent(newContext)))
            {
                var newBlock = block.CreateInstance();
                contextPresenter.AddBlock(0, newBlock);

                Assert.AreEqual(contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).block == newBlock).Count(), 1);

                var blockPresenter = contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).block == newBlock).First() as VFXBlockPresenter;

                Assert.NotNull(blockPresenter);
            }
            return contextPresenter;
        }

        [Test]
        public void CreateAllOutputBlocksTest()
        {
            CreateTestAsset("TestGUI3");
            CreateAllOutputBlocks();
            DestroyTestAsset("TestGUI3");
        }

        VFXContextPresenter CreateAllOutputBlocks()
        {
            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Output").First();

            var newContext = m_ViewPresenter.AddVFXContext(new Vector2(300, 2000), initContextDesc);

            var contextPresenter = m_ViewPresenter.allChildren.Where(t => t is VFXContextPresenter && (t as VFXContextPresenter).model == newContext).First() as VFXContextPresenter;

            Assert.AreEqual(contextPresenter.model, newContext);

            // Adding every block compatible with an init context
            foreach (var block in VFXLibrary.GetBlocks().Where(t => t.AcceptParent(newContext)))
            {
                var newBlock = block.CreateInstance();
                contextPresenter.AddBlock(0, newBlock);

                Assert.AreEqual(contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).block == newBlock).Count(), 1);

                var blockPresenter = contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).block == newBlock).First() as VFXBlockPresenter;

                Assert.NotNull(blockPresenter);
            }
            return contextPresenter;
        }

        [Test]
        public void ExpandRetractAndSetPropertyValue()
        {
            CreateTestAsset("TestGUI4");

            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Initialize").First();

            var newContext = m_ViewPresenter.AddVFXContext(new Vector2(300, 100), initContextDesc);

            Assert.AreEqual(m_ViewPresenter.allChildren.Where(t => t is VFXContextPresenter).Count(), 1);

            var contextPresenter = m_ViewPresenter.allChildren.Where(t => t is VFXContextPresenter).First() as VFXContextPresenter;

            Assert.AreEqual(contextPresenter.model, newContext);

            // Adding every block compatible with an init context

            var block = VFXLibrary.GetBlocks().Where(t => t.name == "Test").First();

            var newBlock = block.CreateInstance();
            contextPresenter.AddBlock(0, newBlock);

            Assert.IsTrue(newBlock is VFXAllType);

            Assert.AreEqual(contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).block == newBlock).Count(), 1);

            var blockPresenter = contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).block == newBlock).First() as VFXBlockPresenter;

            Assert.NotNull(blockPresenter);

            Assert.NotZero(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).name == "aVector3").Count());

            VFXSlot slot = blockPresenter.block.inputSlots.First(t => t.name == "aVector3");


            var aVector3Presenter = blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).name == "aVector3").First() as VFXContextDataInputAnchorPresenter;

            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.z").Count(), 1);

            aVector3Presenter.ExpandPath();

            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.z").Count(), 1);


            aVector3Presenter.RetractPath();

            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.z").Count(), 1);


            aVector3Presenter.SetPropertyValue(new Vector3(1.2f, 3.4f, 5.6f));

            Assert.AreEqual(slot.value, new Vector3(1.2f, 3.4f, 5.6f));

            aVector3Presenter.ExpandPath();

            var vector3yPresenter = blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.y").First() as VFXContextDataInputAnchorPresenter;

            vector3yPresenter.SetPropertyValue(7.8f);

            Assert.AreEqual(slot.value, new Vector3(1.2f, 7.8f, 5.6f));

            DestroyTestAsset("TestGUI4");
        }

        [Test]
        public void CreateAllOperatorsTest()
        {
            CreateTestAsset("TestGUI5");
            CreateAllOperators();
            DestroyTestAsset("TestGUI5");
        }

        List<VFXOperator> CreateAllOperators()
        {
            List<VFXOperator> operators = new List<VFXOperator>();

            int cpt = 0;
            foreach (var op in VFXLibrary.GetOperators())
            {
                operators.Add(m_ViewPresenter.AddVFXOperator(new Vector2(700, 150 * cpt), op));
                ++cpt;
            }

            return operators;
        }

        List<VFXParameter> CreateAllParameters()
        {
            List<VFXParameter> parameters = new List<VFXParameter>();

            int cpt = 0;
            foreach (var param in VFXLibrary.GetParameters())
            {
                parameters.Add(m_ViewPresenter.AddVFXParameter(new Vector2(-400, 150 * cpt), param));
                ++cpt;
            }

            return parameters;
        }

        [Test]
        public void CreateAllParametersTest()
        {
            CreateTestAsset("TestGUI6");
            CreateAllParameters();

            DestroyTestAsset("TestGUI6");
        }

        [Test]
        public void CreateAllDataEdgesTest()
        {
            CreateTestAsset("TestGUI7");
            CreateDataEdges(CreateAllOutputBlocks(), CreateAllParameters());
            DestroyTestAsset("TestGUI7");
        }
    }
}
