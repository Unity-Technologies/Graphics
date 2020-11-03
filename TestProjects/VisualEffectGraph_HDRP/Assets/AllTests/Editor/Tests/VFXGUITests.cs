#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using UnityEngine.TestTools;
using System.Collections;
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
        private const int testAssetCount = 9;
        private VisualEffectAsset[] m_GuiTestAssets = new VisualEffectAsset[testAssetCount];

        [OneTimeSetUp]
        public void CreateTestAssets()
        {
            for (int i = 0; i < testAssetCount; ++i)
                m_GuiTestAssets[i] = CreateTestAsset("GUITest" + i);
        }

        [OneTimeTearDown]
        public void DestroyTestAssets()
        {
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            window.Close();

            for (int i = 0; i < testAssetCount; ++i)
                DestroyTestAsset("GUITest" + i);
        }

        public static void RunGUITests()
        {
            VFXGUITests tests = new VFXGUITests();

            tests.CreateTestAssets();
            var initContext = tests.CreateAllInitializeBlocks();
            var updateContext = tests.CreateAllUpdateBlocks();
            var outputContext = tests.CreateAllOutputBlocks();

            tests.CreateFlowEdges(new VFXContextController[] {initContext, updateContext, outputContext});
            tests.CreateAllOperators();
            List<VFXParameter> parameters = tests.CreateAllParameters();

            tests.CreateDataEdges(updateContext, parameters);
            tests.DestroyTestAssets();
        }

        VFXViewController m_ViewController;
        VFXViewWindow m_Window;

        const string testAssetName = "Assets/TmpTests/{0}.vfx";

        [Test]
        public void CreateFlowEdgesTest()
        {
            EditTestAsset(3);

            var eventContextDesc = VFXLibrary.GetContexts().Where(t => t.model.contextType == VFXContextType.Event).First();
            var eventContext = m_ViewController.AddVFXContext(new Vector2(300, 100), eventContextDesc);

            var spawnerContextDesc = VFXLibrary.GetContexts().Where(t => t.model.contextType == VFXContextType.Spawner).First();
            var spawnerContext = m_ViewController.AddVFXContext(new Vector2(300, 100), spawnerContextDesc);

            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.model.contextType == VFXContextType.Init).First();
            var initContext = m_ViewController.AddVFXContext(new Vector2(300, 100), initContextDesc);

            var updateContextDesc = VFXLibrary.GetContexts().Where(t => t.model.contextType == VFXContextType.Update).First();
            var updateContext = m_ViewController.AddVFXContext(new Vector2(300, 1000), updateContextDesc);

            var outputContextDesc = VFXLibrary.GetContexts().Where(t => t.model.contextType == VFXContextType.Output && t.model.name.Contains("Particle")).First();
            var outputContext = m_ViewController.AddVFXContext(new Vector2(300, 2000), outputContextDesc);

            m_ViewController.ApplyChanges();

            var contextControllers = new List<VFXContextController>();

            contextControllers.Add(m_ViewController.allChildren.OfType<VFXContextController>().First(t => t.model == eventContext) as VFXContextController);
            contextControllers.Add(m_ViewController.allChildren.OfType<VFXContextController>().First(t => t.model == spawnerContext) as VFXContextController);
            contextControllers.Add(m_ViewController.allChildren.OfType<VFXContextController>().First(t => t.model == initContext) as VFXContextController);
            contextControllers.Add(m_ViewController.allChildren.OfType<VFXContextController>().First(t => t.model == updateContext) as VFXContextController);
            contextControllers.Add(m_ViewController.allChildren.OfType<VFXContextController>().First(t => t.model == outputContext) as VFXContextController);

            CreateFlowEdges(contextControllers);
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

        private VisualEffectAsset CreateTestAsset(string name)
        {
            var filePath = string.Format(testAssetName, name);
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }


            return VisualEffectAssetEditorUtility.CreateNewAsset(filePath);
        }

        private void EditTestAsset(int assetIndex)
        {
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            window.Close();
            window = EditorWindow.GetWindow<VFXViewWindow>();
            m_ViewController = VFXViewController.GetController(m_GuiTestAssets[assetIndex].GetResource(), true);
            window.graphView.controller = m_ViewController;
        }

        private void DestroyTestAsset(string name)
        {
            var filePath = string.Format(testAssetName, name);
            AssetDatabase.DeleteAsset(filePath);
        }

        [Test]
        public void CreateAllInitializeBlocksTest()
        {
            EditTestAsset(0);
            CreateAllInitializeBlocks();
        }

        VFXContextController CreateAllInitializeBlocks()
        {
            return CreateAllBlocks(VFXContextType.Init);
        }

        [Test]
        public void CreateAllUpdateBlocksTest()
        {
            EditTestAsset(1);
            CreateAllUpdateBlocks();
        }

        VFXContextController CreateAllUpdateBlocks()
        {
            return CreateAllBlocks(VFXContextType.Update);
        }

        [Test]
        public void CreateAllOutputBlocksTest()
        {
            EditTestAsset(2);
            CreateAllOutputBlocks();
        }

        [Test]
        public void CreateAllSpawnerBlocksTest()
        {
            EditTestAsset(0);
            CreateAllBlocks(VFXContextType.Spawner);
        }

        [Test]
        public void CreateAllEventBlocksTest()
        {
            EditTestAsset(0);
            CreateAllBlocks(VFXContextType.Event);
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
            return CreateAllBlocks(VFXContextType.Output);
        }

        [Test]
        public void ExpandRetractAndSetPropertyValue()
        {
            EditTestAsset(7);

            var initContextDesc = VFXLibrary.GetContexts().Where(t => typeof(VFXBasicInitialize).IsAssignableFrom(t.modelType)).First();

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
        }

        [Test]
        public void CreateAllOperatorsTest()
        {
            EditTestAsset(4);
            CreateAllOperators();
        }


        [UnityTest]
        public IEnumerator CollapseTest()
        {
            EditTestAsset(8);

            var builtInItem = VFXLibrary.GetOperators().Where(t => typeof(VFXDynamicBuiltInParameter).IsAssignableFrom(t.modelType)).First();

            var builtIn = m_ViewController.AddVFXOperator(Vector2.zero, builtInItem);

            yield return null;

            builtIn.collapsed = true;

            yield return null;

            yield return null;

            builtIn.collapsed = false;

            yield return null;

            yield return null;

            builtIn.superCollapsed = true;

            yield return null;

            yield return null;

            builtIn.superCollapsed = false;

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
            EditTestAsset(5);
            CreateAllParameters();
        }

        [Test]
        public void CreateAllDataEdgesTest()
        {
            EditTestAsset(6);
            CreateDataEdges(CreateAllOutputBlocks(), CreateAllParameters());
        }
    }
}
#endif
