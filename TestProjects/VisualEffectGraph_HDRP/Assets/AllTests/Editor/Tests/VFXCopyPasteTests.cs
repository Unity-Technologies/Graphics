#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXCopyPasteTests
    {
        VFXViewController m_ViewController;

        const string testAssetName = "Assets/TmpTests/VFXGraph{0}.vfx";

        private int m_StartUndoGroupId;


        string lastFileName;
        static int cpt = 0;

        [SetUp]
        public void CreateTestAsset()
        {
            lastFileName = string.Format(testAssetName, cpt++);

            var directoryPath = Path.GetDirectoryName(lastFileName);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }


            if (File.Exists(lastFileName))
            {
                AssetDatabase.DeleteAsset(lastFileName);
            }

            var asset = VisualEffectAssetEditorUtility.CreateNewAsset(lastFileName);

            VisualEffectResource resource = asset.GetResource(); // force resource creation

            m_ViewController = VFXViewController.GetController(resource);
            m_ViewController.useCount++;

            m_StartUndoGroupId = Undo.GetCurrentGroup();
        }

        [TearDown]
        public void DestroyTestAsset()
        {
            m_ViewController.useCount--;
            Undo.RevertAllDownToGroup(m_StartUndoGroupId);
            AssetDatabase.DeleteAsset(lastFileName);
            lastFileName = null;
        }

        [Test]
        public void CopyPasteContextWithBlock()
        {
            // Create a BasicInitialize context
            var initContextDesc = VFXLibrary.GetContexts().First(t => typeof(VFXBasicInitialize).IsAssignableFrom(t.modelType));
            var newContext = m_ViewController.AddVFXContext(new Vector2(100, 100), initContextDesc);
            m_ViewController.ApplyChanges();
            Assert.AreEqual(1, m_ViewController.allChildren.Count(t => t is VFXContextController));
            var contextController = m_ViewController.allChildren.OfType<VFXContextController>().Single();
            Assert.AreEqual(contextController.model, newContext);

            // Add a block to that context
            var flipBookBlockDesc = VFXLibrary.GetBlocks().First(t => t.name == "Set Tex Index");
            contextController.AddBlock(0, flipBookBlockDesc.CreateInstance());
            m_ViewController.ApplyChanges();

            // Select the created context (which now contains a single block)
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            VFXView view = window.graphView;
            view.controller = m_ViewController;
            view.ClearSelection();
            foreach (var element in view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }

            // Set the original block slot values
            VFXSlot boundsSlot = newContext.GetInputSlot(0);
            AABox originalBounds = new AABox { center = Vector3.one, size = Vector3.one * 10 };
            boundsSlot.value = originalBounds;
            VFXBlock flipBookBlock = m_ViewController.contexts.First().blockControllers.First().model;
            VFXSlot minValueSlot = flipBookBlock.GetInputSlot(0);
            float originalMinValue = 123.456f;
            minValueSlot.value = originalMinValue;

            // Copy selection
            string copyData = view.SerializeElements(view.selection.OfType<GraphElement>());
            // Than change value to check the original value is pasted (not the modified one after copy)
            boundsSlot.value = new AABox { center = Vector3.zero, size = Vector3.zero };
            minValueSlot.value = 789f;

            // Paste selection
            var pasteCenter = view.pasteCenter;
            view.UnserializeAndPasteElements("paste", copyData);

            // Get the only context that has a different model than the original one (which means it's the context that we just pasted)
            var copyContextModel = view.Query()
                .OfType<VFXContextUI>()
                .ToList()
                .Select(x => x.controller)
                .Single(x => x.model != newContext).model;

            var copyBoundsSlot = copyContextModel.GetInputSlot(0);
            var copyMinSlot = copyContextModel[0].GetInputSlot(0);

            Assert.AreEqual((AABox)copyBoundsSlot.value, originalBounds);
            Assert.AreEqual((float)copyMinSlot.value, originalMinValue);
            Assert.AreEqual(pasteCenter + newContext.position, copyContextModel.position);
        }

        [Test]
        public void CopyPasteOperator()
        {
            // Create an operator
            var crossOperatorDesc = VFXLibrary.GetOperators().First(t => t.name == "Cross Product");
            var newOperator = m_ViewController.AddVFXOperator(new Vector2(100, 100), crossOperatorDesc);
            m_ViewController.ApplyChanges();
            var operatorController = m_ViewController.allChildren.OfType<VFXOperatorController>().Single();
            Assert.AreEqual(operatorController.model, newOperator);

            // Select the newly created operator
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            VFXView view = window.graphView;
            view.controller = m_ViewController;
            view.ClearSelection();
            foreach (var element in view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }


            // Setup original operator slot values
            VFXSlot aSlot = newOperator.GetInputSlot(0);
            Vector3 originalA = Vector3.one * 123;
            aSlot.value = originalA;

            // Copy selection and modify operator slot value afterwards
            string copyData = view.SerializeElements(view.selection.OfType<GraphElement>());
            aSlot.value = Vector3.one * 456;

            // Paste selection
            var pasteCenter = view.pasteCenter;
            view.UnserializeAndPasteElements("paste", copyData);

            // Retrieve the pasted operator
            var copyOperator = view.Query()
                .OfType<GraphElement>()
                .ToList()
                .OfType<VFXOperatorUI>()
                .Single(x => x.controller.model != newOperator);

            var copyASlot = copyOperator.controller.model.GetInputSlot(0);
            Assert.AreEqual(originalA, (Vector3)copyASlot.value);
            Assert.AreEqual(pasteCenter + newOperator.position, copyOperator.controller.model.position);
        }

        [Test]
        public void CopyPasteSpacableOperator()
        {
            // Create a spaceable operator
            var inlineOperatorDesc = VFXLibrary.GetOperators().First(t => t.modelType == typeof(VFXInlineOperator));
            var newOperator = m_ViewController.AddVFXOperator(new Vector2(100, 100), inlineOperatorDesc);
            newOperator.SetSettingValue("m_Type", new SerializableType(typeof(DirectionType)));
            m_ViewController.ApplyChanges();
            var operatorController = m_ViewController.allChildren.OfType<VFXOperatorController>().First();
            Assert.AreEqual(newOperator, operatorController.model);

            // Select the newly created operator
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            VFXView view = window.graphView;
            view.controller = m_ViewController;
            view.ClearSelection();
            foreach (var element in view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }

            // Setup original operator slot value
            VFXSlot aSlot = newOperator.GetInputSlot(0);
            Assert.IsTrue(aSlot.spaceable);
            aSlot.space = VFXCoordinateSpace.World;

            // Copy selection and then modify the slot value
            string copyData = view.SerializeElements(view.selection.OfType<GraphElement>());
            aSlot.space = VFXCoordinateSpace.Local;

            // Paste selection
            view.UnserializeAndPasteElements("paste", copyData);

            // Retrieve the pasted operator
            var copyOperator = view.Query()
                .OfType<GraphElement>()
                .ToList()
                .OfType<VFXOperatorUI>()
                .Single(x => x.controller.model != newOperator);

            var copyASlot = copyOperator.controller.model.GetInputSlot(0);
            Assert.AreEqual(VFXCoordinateSpace.World, copyASlot.space);
        }

        [Test]
        public void CopyPasteEdges()
        {
            // Load a pre-made vfx asset into a VFX Graph window
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/AllTests/Editor/Tests/CopyPasteTest.vfx");
            VFXViewController controller = VFXViewController.GetController(asset.GetResource(), true);
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            VFXView view = window.graphView;
            view.controller = controller;
            view.ClearSelection();


            // Select all nodes in the graph
            var originalElements = view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>().ToArray();
            Assert.AreNotEqual(0, originalElements.Length);
            foreach (var element in originalElements)
            {
                view.AddToSelection(element);
            }

            // Copy / Paste selection
            string copyData = view.SerializeElements(view.selection.OfType<GraphElement>());
            view.controller = m_ViewController;
            view.UnserializeAndPasteElements("paste", copyData);

            var parameters = view.Query().OfType<VFXParameterUI>().ToList();
            Assert.AreEqual(2, parameters.Count);
            if (parameters[0].title == "Vector3")
            {
                (parameters[0], parameters[1]) = (parameters[1], parameters[0]);
            }

            var operators = view.Query().OfType<VFXOperatorUI>().ToList();
            Assert.AreEqual(2, operators.Count);

            var contexts = view.Query().OfType<VFXContextUI>().ToList();
            Assert.AreEqual(2, contexts.Count);

            if (contexts[0].controller.model is VFXBasicUpdate)
            {
                (contexts[0], contexts[1]) = (contexts[1], contexts[0]);
            }


            var dataEdges = view.Query().OfType<VFXDataEdge>().ToList();

            Assert.AreEqual(4, dataEdges.Count);

            Assert.IsNotNull(dataEdges.SingleOrDefault(t => t.output.GetFirstAncestorOfType<VFXNodeUI>() == parameters[1] &&
                                                           operators.Contains(t.input.GetFirstAncestorOfType<VFXOperatorUI>())));

            Assert.IsNotNull(dataEdges.SingleOrDefault(t => operators.Contains(t.input.GetFirstAncestorOfType<VFXOperatorUI>()) &&
                                                           operators.Contains(t.output.GetFirstAncestorOfType<VFXOperatorUI>()) &&
                                                           t.output.GetFirstAncestorOfType<VFXNodeUI>() != t.input.GetFirstAncestorOfType<VFXNodeUI>()));

            Assert.IsNotNull(dataEdges.SingleOrDefault(t => t.output.GetFirstAncestorOfType<VFXNodeUI>() == parameters[0] &&
                                                           t.input.GetFirstAncestorOfType<VFXNodeUI>() == contexts[0]));

            Assert.IsNotNull(dataEdges.FirstOrDefault(t => operators.Contains<VFXNodeUI>(t.output.GetFirstAncestorOfType<VFXNodeUI>()) &&
                                                           t.input.GetFirstAncestorOfType<VFXNodeUI>() == contexts[0].GetAllBlocks().First()));


            VFXFlowEdge flowEdge = view.Query().OfType<VFXFlowEdge>();

            Assert.IsNotNull(flowEdge);

            Assert.AreEqual(flowEdge.output.GetFirstAncestorOfType<VFXContextUI>(), contexts[1]);
            Assert.AreEqual(flowEdge.input.GetFirstAncestorOfType<VFXContextUI>(), contexts[0]);
        }

        [Test]
        public void CopyPasteBlock()
        {
            // Create a new BasicInitialize context
            var initContextDesc = VFXLibrary.GetContexts().First(t => typeof(VFXBasicInitialize).IsAssignableFrom(t.modelType));
            var newContext = m_ViewController.AddVFXContext(new Vector2(100, 100), initContextDesc);
            m_ViewController.ApplyChanges();
            Assert.AreEqual(1, m_ViewController.allChildren.Count(t => t is VFXContextController));
            var contextController = m_ViewController.allChildren.OfType<VFXContextController>().First();
            Assert.AreEqual(contextController.model, newContext);

            // Add a block to that context
            var flipBookBlockDesc = VFXLibrary.GetBlocks().First(t => t.name == "Set Tex Index");
            contextController.AddBlock(0, flipBookBlockDesc.CreateInstance());
            var newBlock = contextController.model.children.First();
            m_ViewController.ApplyChanges();

            // Select the created context
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            VFXView view = window.graphView;
            view.controller = m_ViewController;
            view.ClearSelection();
            foreach (var element in view.Query().OfType<VFXBlockUI>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }

            // Set original block slots values
            VFXBlock flipBookBlock = contextController.blockControllers.First().model;
            VFXSlot minValueSlot = flipBookBlock.GetInputSlot(0);
            float originalMinValue = 123.456f;
            minValueSlot.value = originalMinValue;

            // Copy selection and change the block value
            string copyData = view.SerializeElements(view.selection.OfType<GraphElement>());
            minValueSlot.value = 789f;

            // Paste selection
            view.UnserializeAndPasteElements("Paste", copyData);

            // Retrieve the block from the copied context
            var copyBlock = view.Query()
                .OfType<VFXBlockUI>()
                .ToList()
                .Select(x => x.controller)
                .First(x => x.model != newBlock).model;

            // Check the block slot value is the same as the original (not the modified value after copy operation)
            var copyMinSlot = copyBlock.GetInputSlot(0);
            Assert.AreEqual((float)copyMinSlot.value, originalMinValue);
        }

        [Test]
        public void CreateTemplate()
        {
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();

            VFXView view = window.graphView;
            view.controller = m_ViewController;

            view.CreateTemplateSystem("Assets/VFXEditor/Editor/Templates/SimpleParticleSystem.vfx", Vector2.zero, null);
        }

        [Test]
        public void PasteSystems()
        {
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();

            VFXView view = window.graphView;
            view.controller = m_ViewController;

            // Create a bunch of systems
            const int spawnerCount = 4, GPUSystemsCount = 4;
            var spawners = VFXTestCommon.CreateSpawners(view, m_ViewController, spawnerCount);
            VFXTestCommon.CreateSystems(view, m_ViewController, GPUSystemsCount, 0);

            // Copy paste them
            view.ClearSelection();
            foreach (var element in view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }
            view.CopySelectionCallback();
            view.PasteCallback();
            m_ViewController.ApplyChanges();

            // Query unique names
            var systemNames = view.controller.graph.systemNames;

            var uniqueNames = m_ViewController.graph.children.OfType<VFXBasicSpawner>()
                .Select(x => systemNames.GetUniqueSystemName(x))
                .Union(m_ViewController.graph.children.OfType<VFXBasicInitialize>().Select(x => systemNames.GetUniqueSystemName(x.GetData())))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            // Assert all names are unique, and the expected number of elements was obtained
            Assert.AreEqual(2 * (spawnerCount + GPUSystemsCount), uniqueNames.Count, "Some systems have the same name or are null or empty.");
        }
    }
}
#endif
