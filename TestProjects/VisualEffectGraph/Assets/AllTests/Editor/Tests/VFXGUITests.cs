using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.UI;
using UnityEditor.VFX.Block.Test;
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

        VFXViewController m_ViewPresenter;
        VFXViewWindow m_Window;

        const string testAssetName = "Assets/TmpTests/{0}.asset";


        [Test]
        public void CreateFlowEdgesTest()
        {
            CreateTestAsset("GUITest4");
            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Initialize").First();
            var initContext = m_ViewPresenter.AddVFXContext(new Vector2(300, 100), initContextDesc);

            m_ViewPresenter.ApplyChanges();

            var initContextPresenter = m_ViewPresenter.allChildren.OfType<VFXContextController>().First(t => t.model == initContext) as VFXContextController;

            var updateContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Update").First();
            var updateContext = m_ViewPresenter.AddVFXContext(new Vector2(300, 1000), updateContextDesc);

            m_ViewPresenter.ApplyChanges();
            var updateContextPresenter = m_ViewPresenter.allChildren.OfType<VFXContextController>().First(t => t.model == updateContext) as VFXContextController;

            var outputContextDesc = VFXLibrary.GetContexts().Where(t => t.name.Contains("Output")).First();
            var outputContext = m_ViewPresenter.AddVFXContext(new Vector2(300, 1000), outputContextDesc);

            m_ViewPresenter.ApplyChanges();
            var outputContextPresenter = m_ViewPresenter.allChildren.OfType<VFXContextController>().First(t => t.model == outputContext) as VFXContextController;

            CreateFlowEdges(initContextPresenter, updateContextPresenter, outputContextPresenter);

            DestroyTestAsset("GUITest4");
        }

        void CreateFlowEdges(VFXContextController initContext, VFXContextController updateContext, VFXContextController outputContext)
        {
            VFXFlowEdgePresenter edgePresenter = VFXFlowEdgePresenter.CreateInstance<VFXFlowEdgePresenter>();
            edgePresenter.Init(updateContext.flowInputAnchors.First(), initContext.flowOutputAnchors.First());

            m_ViewPresenter.AddElement(edgePresenter);

            edgePresenter = VFXFlowEdgePresenter.CreateInstance<VFXFlowEdgePresenter>();
            edgePresenter.Init(outputContext.flowInputAnchors.First(), updateContext.flowOutputAnchors.First());

            m_ViewPresenter.AddElement(edgePresenter);
        }

        void CreateDataEdges(VFXContextController updateContext, List<VFXParameter> parameters)
        {
            m_ViewPresenter.ApplyChanges();
            foreach (var param in parameters)
            {
                VFXParameterController paramPresenter = m_ViewPresenter.allChildren.OfType<VFXParameterController>().First(t => t.model == param);

                VFXDataAnchorController outputAnchor = paramPresenter.outputPorts.First() as VFXDataAnchorController;
                System.Type type = outputAnchor.portType;

                bool found = false;
                foreach (var block in updateContext.blockControllers)
                {
                    foreach (var anchor in block.inputPorts)
                    {
                        if (anchor.portType == type)
                        {
                            found = true;

                            (anchor as VFXDataAnchorController).model.Link(outputAnchor.model);

                            break;
                        }
                    }
                    if (found)
                        break;
                }
            }
        }

        public VFXAsset m_Asset;

        public void CreateTestAsset(string name)
        {
            m_Asset = new VFXAsset();

            var filePath = string.Format(testAssetName, name);
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            AssetDatabase.CreateAsset(m_Asset, filePath);
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            window.Close();
            window = EditorWindow.GetWindow<VFXViewWindow>();
            m_ViewPresenter = VFXViewController.Manager.GetController(m_Asset, true);
            window.graphView.controller = m_ViewPresenter;
            //m_View = m_ViewPresenter.View;
        }

        void DestroyTestAsset(string name)
        {
            var filePath = string.Format(testAssetName, name);
            AssetDatabase.DeleteAsset(filePath);
            UnityEngine.Object.DestroyImmediate(m_Asset);

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

        VFXContextController CreateAllInitializeBlocks()
        {
            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Initialize").First();

            var newContext = m_ViewPresenter.AddVFXContext(new Vector2(300, 100), initContextDesc);
            m_ViewPresenter.ApplyChanges();

            Assert.AreEqual(m_ViewPresenter.allChildren.Where(t => t is VFXContextController).Count(), 1);

            var contextPresenter = m_ViewPresenter.allChildren.Where(t => t is VFXContextController).First() as VFXContextController;

            Assert.AreEqual(contextPresenter.model, newContext);

            // Adding every block compatible with an init context
            foreach (var block in VFXLibrary.GetBlocks().Where(t => t.AcceptParent(newContext)))
            {
                var newBlock = block.CreateInstance();
                contextPresenter.AddBlock(0, newBlock);
                m_ViewPresenter.ApplyChanges();

                Assert.AreEqual(contextPresenter.blockControllers.Where(t => t.block == newBlock).Count(), 1);

                var blockPresenter = contextPresenter.blockControllers.Where(t => t.block == newBlock).First() as VFXBlockController;

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

        VFXContextController CreateAllUpdateBlocks()
        {
            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Update").First();

            var newContext = m_ViewPresenter.AddVFXContext(new Vector2(300, 1000), initContextDesc);

            m_ViewPresenter.ApplyChanges();
            var contextPresenter = m_ViewPresenter.allChildren.Where(t => t is VFXContextController && (t as VFXContextController).model == newContext).First() as VFXContextController;


            Assert.AreEqual(contextPresenter.model, newContext);

            // Adding every block compatible with an init context
            foreach (var block in VFXLibrary.GetBlocks().Where(t => t.AcceptParent(newContext)))
            {
                var newBlock = block.CreateInstance();
                contextPresenter.AddBlock(0, newBlock);

                m_ViewPresenter.ApplyChanges();

                Assert.AreEqual(contextPresenter.blockControllers.Where(t => t.block == newBlock).Count(), 1);

                var blockPresenter = contextPresenter.blockControllers.Where(t => t.block == newBlock).First() as VFXBlockController;

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

        VFXContextController CreateAllOutputBlocks()
        {
            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name.Contains("Output")).First();

            var newContext = m_ViewPresenter.AddVFXContext(new Vector2(300, 2000), initContextDesc);

            m_ViewPresenter.ApplyChanges();

            var contextPresenter = m_ViewPresenter.nodes.Where(t => t is VFXContextController && (t as VFXContextController).model == newContext).First() as VFXContextController;

            Assert.AreEqual(contextPresenter.model, newContext);

            // Adding every block compatible with an init context
            foreach (var block in VFXLibrary.GetBlocks().Where(t => t.AcceptParent(newContext)))
            {
                var newBlock = block.CreateInstance();
                contextPresenter.AddBlock(0, newBlock);

                m_ViewPresenter.ApplyChanges();

                Assert.AreEqual(contextPresenter.blockControllers.Where(t => t.block == newBlock).Count(), 1, "Failing Block" + newBlock.name + "in context" + newContext.name);

                var blockPresenter = contextPresenter.blockControllers.Where(t => t.block == newBlock).First() as VFXBlockController;

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
            m_ViewPresenter.ApplyChanges();

            Assert.AreEqual(m_ViewPresenter.allChildren.Where(t => t is VFXContextController).Count(), 1);

            var contextPresenter = m_ViewPresenter.allChildren.Where(t => t is VFXContextController).First() as VFXContextController;

            Assert.AreEqual(contextPresenter.model, newContext);

            // Adding every block compatible with an init context

            var block = VFXLibrary.GetBlocks().Where(t => t.name == "Test").First();

            var newBlock = block.CreateInstance();
            contextPresenter.AddBlock(0, newBlock);

            Assert.IsTrue(newBlock is AllType);
            m_ViewPresenter.ApplyChanges();

            Assert.AreEqual(contextPresenter.blockControllers.Where(t => t.block == newBlock).Count(), 1);

            var blockPresenter = contextPresenter.blockControllers.Where(t => t.block == newBlock).First();

            Assert.NotNull(blockPresenter);

            Assert.NotZero(blockPresenter.inputPorts.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).name == "aVector3").Count());

            VFXSlot slot = blockPresenter.block.inputSlots.First(t => t.name == "aVector3");


            var aVector3Presenter = blockPresenter.inputPorts.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).name == "aVector3").First() as VFXContextDataInputAnchorPresenter;

            Assert.AreEqual(blockPresenter.inputPorts.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockPresenter.inputPorts.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockPresenter.inputPorts.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.z").Count(), 1);

            aVector3Presenter.ExpandPath();
            m_ViewPresenter.ApplyChanges();

            Assert.AreEqual(blockPresenter.inputPorts.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockPresenter.inputPorts.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockPresenter.inputPorts.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.z").Count(), 1);


            aVector3Presenter.RetractPath();
            m_ViewPresenter.ApplyChanges();

            Assert.AreEqual(blockPresenter.inputPorts.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockPresenter.inputPorts.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockPresenter.inputPorts.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.z").Count(), 1);


            aVector3Presenter.SetPropertyValue(new Vector3(1.2f, 3.4f, 5.6f));

            Assert.AreEqual(slot.value, new Vector3(1.2f, 3.4f, 5.6f));

            aVector3Presenter.ExpandPath();
            m_ViewPresenter.ApplyChanges();

            var vector3yPresenter = blockPresenter.inputPorts.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.y").First() as VFXContextDataInputAnchorPresenter;

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
