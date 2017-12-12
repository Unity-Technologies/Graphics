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

        VFXViewController m_ViewController;
        VFXViewWindow m_Window;

        const string testAssetName = "Assets/TmpTests/{0}.asset";


        [Test]
        public void CreateFlowEdgesTest()
        {
            CreateTestAsset("GUITest4");
            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Initialize").First();
            var initContext = m_ViewController.AddVFXContext(new Vector2(300, 100), initContextDesc);

            m_ViewController.ApplyChanges();

            var initContextControlller = m_ViewController.allChildren.OfType<VFXContextController>().First(t => t.model == initContext) as VFXContextController;

            var updateContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Update").First();
            var updateContext = m_ViewController.AddVFXContext(new Vector2(300, 1000), updateContextDesc);

            m_ViewController.ApplyChanges();
            var updateContextControlller = m_ViewController.allChildren.OfType<VFXContextController>().First(t => t.model == updateContext) as VFXContextController;

            var outputContextDesc = VFXLibrary.GetContexts().Where(t => t.name.Contains("Output")).First();
            var outputContext = m_ViewController.AddVFXContext(new Vector2(300, 1000), outputContextDesc);

            m_ViewController.ApplyChanges();
            var outputContextControlller = m_ViewController.allChildren.OfType<VFXContextController>().First(t => t.model == outputContext) as VFXContextController;

            CreateFlowEdges(initContextControlller, updateContextControlller, outputContextControlller);

            DestroyTestAsset("GUITest4");
        }

        void CreateFlowEdges(VFXContextController initContext, VFXContextController updateContext, VFXContextController outputContext)
        {
            VFXFlowEdgeController edgeControlller = new VFXFlowEdgeController(updateContext.flowInputAnchors.First(), initContext.flowOutputAnchors.First());

            m_ViewController.AddElement(edgeControlller);

            edgeControlller = new VFXFlowEdgeController(outputContext.flowInputAnchors.First(), updateContext.flowOutputAnchors.First());

            m_ViewController.AddElement(edgeControlller);
        }

        void CreateDataEdges(VFXContextController updateContext, List<VFXParameter> parameters)
        {
            m_ViewController.ApplyChanges();
            foreach (var param in parameters)
            {
                VFXParameterController paramControlller = m_ViewController.allChildren.OfType<VFXParameterController>().First(t => t.model == param);

                VFXDataAnchorController outputAnchor = paramControlller.outputPorts.First() as VFXDataAnchorController;
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
            m_ViewController = VFXViewController.Manager.GetController(m_Asset, true);
            window.graphView.controller = m_ViewController;
            //m_View = m_ViewControlller.View;
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

            var newContext = m_ViewController.AddVFXContext(new Vector2(300, 100), initContextDesc);
            m_ViewController.ApplyChanges();

            Assert.AreEqual(m_ViewController.allChildren.Where(t => t is VFXContextController).Count(), 1);

            var contextControlller = m_ViewController.allChildren.Where(t => t is VFXContextController).First() as VFXContextController;

            Assert.AreEqual(contextControlller.model, newContext);

            // Adding every block compatible with an init context
            foreach (var block in VFXLibrary.GetBlocks().Where(t => t.AcceptParent(newContext)))
            {
                var newBlock = block.CreateInstance();
                contextControlller.AddBlock(0, newBlock);
                m_ViewController.ApplyChanges();

                Assert.AreEqual(contextControlller.blockControllers.Where(t => t.block == newBlock).Count(), 1);

                var blockControlller = contextControlller.blockControllers.Where(t => t.block == newBlock).First() as VFXBlockController;

                Assert.NotNull(blockControlller);
            }

            return contextControlller;
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

            var newContext = m_ViewController.AddVFXContext(new Vector2(300, 1000), initContextDesc);

            m_ViewController.ApplyChanges();
            var contextControlller = m_ViewController.allChildren.Where(t => t is VFXContextController && (t as VFXContextController).model == newContext).First() as VFXContextController;


            Assert.AreEqual(contextControlller.model, newContext);

            // Adding every block compatible with an init context
            foreach (var block in VFXLibrary.GetBlocks().Where(t => t.AcceptParent(newContext)))
            {
                var newBlock = block.CreateInstance();
                contextControlller.AddBlock(0, newBlock);

                m_ViewController.ApplyChanges();

                Assert.AreEqual(contextControlller.blockControllers.Where(t => t.block == newBlock).Count(), 1);

                var blockControlller = contextControlller.blockControllers.Where(t => t.block == newBlock).First() as VFXBlockController;

                Assert.NotNull(blockControlller);
            }
            return contextControlller;
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

            var newContext = m_ViewController.AddVFXContext(new Vector2(300, 2000), initContextDesc);

            m_ViewController.ApplyChanges();

            var contextControlller = m_ViewController.nodes.Where(t => t is VFXContextController && (t as VFXContextController).model == newContext).First() as VFXContextController;

            Assert.AreEqual(contextControlller.model, newContext);

            // Adding every block compatible with an init context
            foreach (var block in VFXLibrary.GetBlocks().Where(t => t.AcceptParent(newContext)))
            {
                var newBlock = block.CreateInstance();
                contextControlller.AddBlock(0, newBlock);

                m_ViewController.ApplyChanges();

                Assert.AreEqual(contextControlller.blockControllers.Where(t => t.block == newBlock).Count(), 1, "Failing Block" + newBlock.name + "in context" + newContext.name);

                var blockControlller = contextControlller.blockControllers.Where(t => t.block == newBlock).First() as VFXBlockController;

                Assert.NotNull(blockControlller);
            }

            return contextControlller;
        }

        [Test]
        public void ExpandRetractAndSetPropertyValue()
        {
            CreateTestAsset("TestGUI4");

            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Initialize").First();

            var newContext = m_ViewController.AddVFXContext(new Vector2(300, 100), initContextDesc);
            m_ViewController.ApplyChanges();

            Assert.AreEqual(m_ViewController.allChildren.Where(t => t is VFXContextController).Count(), 1);

            var contextControlller = m_ViewController.allChildren.Where(t => t is VFXContextController).First() as VFXContextController;

            Assert.AreEqual(contextControlller.model, newContext);

            // Adding every block compatible with an init context

            var block = VFXLibrary.GetBlocks().Where(t => t.name == "Test").First();

            var newBlock = block.CreateInstance();
            contextControlller.AddBlock(0, newBlock);

            Assert.IsTrue(newBlock is AllType);
            m_ViewController.ApplyChanges();

            Assert.AreEqual(contextControlller.blockControllers.Where(t => t.block == newBlock).Count(), 1);

            var blockControlller = contextControlller.blockControllers.Where(t => t.block == newBlock).First();

            Assert.NotNull(blockControlller);

            Assert.NotZero(blockControlller.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).name == "aVector3").Count());

            VFXSlot slot = blockControlller.block.inputSlots.First(t => t.name == "aVector3");


            var aVector3Controlller = blockControlller.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).name == "aVector3").First() as VFXContextDataInputAnchorController;

            Assert.AreEqual(blockControlller.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockControlller.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockControlller.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.z").Count(), 1);

            aVector3Controlller.ExpandPath();
            m_ViewController.ApplyChanges();

            Assert.AreEqual(blockControlller.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockControlller.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockControlller.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.z").Count(), 1);


            aVector3Controlller.RetractPath();
            m_ViewController.ApplyChanges();

            Assert.AreEqual(blockControlller.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockControlller.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockControlller.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.z").Count(), 1);


            aVector3Controlller.SetPropertyValue(new Vector3(1.2f, 3.4f, 5.6f));

            Assert.AreEqual(slot.value, new Vector3(1.2f, 3.4f, 5.6f));

            aVector3Controlller.ExpandPath();
            m_ViewController.ApplyChanges();

            var vector3yControlller = blockControlller.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.y").First() as VFXContextDataInputAnchorController;

            vector3yControlller.SetPropertyValue(7.8f);

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
                operators.Add(m_ViewController.AddVFXOperator(new Vector2(700, 150 * cpt), op));
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
                parameters.Add(m_ViewController.AddVFXParameter(new Vector2(-400, 150 * cpt), param));
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
