#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;
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
        //[MenuItem("VFX Editor/Run GUI Tests")]
        public static void RunGUITests()
        {
            VFXGUITests tests = new VFXGUITests();

            tests.CreateTestAsset("GUITest");
            var initContext = tests.CreateAllInitializeBlocks();
            var updateContext = tests.CreateAllUpdateBlocks();
            var outputContext = tests.CreateAllOutputBlocks();

            tests.CreateFlowEdges(new VFXContextController[] {initContext, updateContext, outputContext});
            tests.CreateAllOperators();
            List<VFXParameter> parameters = tests.CreateAllParameters();

            tests.CreateDataEdges(updateContext, parameters);
        }

        VFXViewController m_ViewController;
        VFXViewWindow m_Window;

        const string testAssetName = "Assets/TmpTests/{0}.vfx";


        [Test]
        public void CreateFlowEdgesTest()
        {
            CreateTestAsset("GUITest4");

            var eventContextDesc = VFXLibrary.GetContexts().Where(t => t.model.contextType == VFXContextType.kEvent).First();
            var eventContext = m_ViewController.AddVFXContext(new Vector2(300, 100), eventContextDesc);

            var spawnerContextDesc = VFXLibrary.GetContexts().Where(t => t.model.contextType == VFXContextType.kSpawner).First();
            var spawnerContext = m_ViewController.AddVFXContext(new Vector2(300, 100), spawnerContextDesc);

            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.model.contextType == VFXContextType.kInit).First();
            var initContext = m_ViewController.AddVFXContext(new Vector2(300, 100), initContextDesc);

            var updateContextDesc = VFXLibrary.GetContexts().Where(t => t.model.contextType == VFXContextType.kUpdate).First();
            var updateContext = m_ViewController.AddVFXContext(new Vector2(300, 1000), updateContextDesc);

            var outputContextDesc = VFXLibrary.GetContexts().Where(t => t.model.contextType == VFXContextType.kOutput).First();
            var outputContext = m_ViewController.AddVFXContext(new Vector2(300, 2000), outputContextDesc);

            m_ViewController.ApplyChanges();

            var contextControllers = new List<VFXContextController>();

            contextControllers.Add(m_ViewController.allChildren.OfType<VFXContextController>().First(t => t.model == eventContext) as VFXContextController);
            contextControllers.Add(m_ViewController.allChildren.OfType<VFXContextController>().First(t => t.model == spawnerContext) as VFXContextController);
            contextControllers.Add(m_ViewController.allChildren.OfType<VFXContextController>().First(t => t.model == initContext) as VFXContextController);
            contextControllers.Add(m_ViewController.allChildren.OfType<VFXContextController>().First(t => t.model == updateContext) as VFXContextController);
            contextControllers.Add(m_ViewController.allChildren.OfType<VFXContextController>().First(t => t.model == outputContext) as VFXContextController);

            CreateFlowEdges(contextControllers);

            DestroyTestAsset("GUITest4");
        }

        void CreateFlowEdges(IList<VFXContextController> contextControllers)
        {
            for (int i = 0; i < contextControllers.Count() - 1; ++i)
            {
                VFXFlowEdgeController edgeController = new VFXFlowEdgeController(contextControllers[i + 1].flowInputAnchors.First(), contextControllers[i].flowOutputAnchors.First());
                m_ViewController.AddElement(edgeController);
            }

            m_ViewController.ApplyChanges();
        }

        void CreateDataEdges(VFXContextController updateContext, List<VFXParameter> parameters)
        {
            m_ViewController.ApplyChanges();
            foreach (var param in parameters)
            {
                VFXParameterNodeController paramController = m_ViewController.allChildren.OfType<VFXParameterNodeController>().First(t => t.model == param);

                VFXDataAnchorController outputAnchor = paramController.outputPorts.First() as VFXDataAnchorController;
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

        public VisualEffectAsset m_Asset;

        public void CreateTestAsset(string name)
        {
            var filePath = string.Format(testAssetName, name);
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }


            m_Asset = VisualEffectResource.CreateNewAsset(filePath);
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            window.Close();
            window = EditorWindow.GetWindow<VFXViewWindow>();
            m_ViewController = VFXViewController.GetController(m_Asset.GetResource(), true);
            window.graphView.controller = m_ViewController;
        }

        void DestroyTestAsset(string name)
        {
            var filePath = string.Format(testAssetName, name);
            AssetDatabase.DeleteAsset(filePath);

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
            return CreateAllBlocks(VFXContextType.kInit);
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
            return CreateAllBlocks(VFXContextType.kUpdate);
        }

        [Test]
        public void CreateAllOutputBlocksTest()
        {
            CreateTestAsset("TestGUI3");
            CreateAllOutputBlocks();
            DestroyTestAsset("TestGUI3");
        }

        [Test]
        public void CreateAllSpawnerBlocksTest()
        {
            CreateTestAsset("TestGUI1");
            CreateAllBlocks(VFXContextType.kSpawner);
            DestroyTestAsset("TestGUI1");
        }

        [Test]
        public void CreateAllEventBlocksTest()
        {
            CreateTestAsset("TestGUI1");
            CreateAllBlocks(VFXContextType.kEvent);
            DestroyTestAsset("TestGUI1");
        }

        VFXContextController CreateAllBlocks(VFXContextType type)
        {
            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.model.contextType == type).First();

            var newContext = m_ViewController.AddVFXContext(new Vector2(300, 2000), initContextDesc);

            m_ViewController.ApplyChanges();

            var contextController = m_ViewController.nodes.Where(t => t is VFXContextController && (t as VFXContextController).model == newContext).First() as VFXContextController;

            Assert.AreEqual(contextController.model, newContext);

            // Adding every block compatible with an init context

            var newBlocks  = new List<VFXBlock>();
            foreach (var block in VFXLibrary.GetBlocks().Where(t => t.AcceptParent(newContext)))
            {
                var newBlock = block.CreateInstance();
                contextController.AddBlock(0, newBlock);
                newBlocks.Add(newBlock);
            }

            m_ViewController.ApplyChanges();

            foreach (var newBlock in newBlocks)
            {
                Assert.AreEqual(contextController.blockControllers.Where(t => t.model == newBlock).Count(), 1, "Failing Block" + newBlock.name + "in context" + newContext.name);

                var blockController = contextController.blockControllers.Where(t => t.model == newBlock).First() as VFXBlockController;

                Assert.NotNull(blockController);
            }

            return contextController;
        }

        VFXContextController CreateAllOutputBlocks()
        {
            return CreateAllBlocks(VFXContextType.kOutput);
        }

        [Test]
        public void ExpandRetractAndSetPropertyValue()
        {
            CreateTestAsset("TestGUI4");

            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Initialize").First();

            var newContext = m_ViewController.AddVFXContext(new Vector2(300, 100), initContextDesc);
            m_ViewController.ApplyChanges();

            Assert.AreEqual(m_ViewController.allChildren.Where(t => t is VFXContextController).Count(), 1);

            var contextController = m_ViewController.allChildren.Where(t => t is VFXContextController).First() as VFXContextController;

            Assert.AreEqual(contextController.model, newContext);

            // Adding every block compatible with an init context

            var blockDesc = new VFXModelDescriptor<VFXBlock>(ScriptableObject.CreateInstance<AllType>());

            var newBlock = blockDesc.CreateInstance();
            contextController.AddBlock(0, newBlock);

            Assert.IsTrue(newBlock is AllType);
            m_ViewController.ApplyChanges();

            Assert.AreEqual(contextController.blockControllers.Where(t => t.model == newBlock).Count(), 1);

            var blockController = contextController.blockControllers.Where(t => t.model == newBlock).First();

            Assert.NotNull(blockController);

            Assert.NotZero(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).name == "aVector3").Count());

            VFXSlot slot = blockController.model.inputSlots.First(t => t.name == "aVector3");


            var aVector3Controller = blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).name == "aVector3").First() as VFXContextDataInputAnchorController;

            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.z").Count(), 1);

            aVector3Controller.ExpandPath();
            m_ViewController.ApplyChanges();

            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.z").Count(), 1);


            aVector3Controller.RetractPath();
            m_ViewController.ApplyChanges();

            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.z").Count(), 1);


            aVector3Controller.SetPropertyValue(new Vector3(1.2f, 3.4f, 5.6f));

            Assert.AreEqual(slot.value, new Vector3(1.2f, 3.4f, 5.6f));

            aVector3Controller.ExpandPath();
            m_ViewController.ApplyChanges();

            var vector3yController = blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.y").First() as VFXContextDataInputAnchorController;

            vector3yController.SetPropertyValue(7.8f);

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
#endif
