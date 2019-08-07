#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
            var initContextDesc = VFXLibrary.GetContexts().Where(t => typeof(VFXBasicInitialize).IsAssignableFrom(t.modelType)).First();

            var newContext = m_ViewController.AddVFXContext(new Vector2(100, 100), initContextDesc);

            m_ViewController.ApplyChanges();

            Assert.AreEqual(m_ViewController.allChildren.Where(t => t is VFXContextController).Count(), 1);

            var contextController = m_ViewController.allChildren.OfType<VFXContextController>().First();

            Assert.AreEqual(contextController.model, newContext);

            var flipBookBlockDesc = VFXLibrary.GetBlocks().First(t => t.name == "Set Tex Index");

            contextController.AddBlock(0, flipBookBlockDesc.CreateInstance());

            m_ViewController.ApplyChanges();

            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();

            VFXView view = window.graphView;
            view.controller = m_ViewController;

            view.ClearSelection();
            foreach (var element in view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }

            VFXSlot boundsSlot = newContext.GetInputSlot(0);

            AABox originalBounds = new AABox() { center = Vector3.one, size = Vector3.one * 10 };
            boundsSlot.value = originalBounds;

            VFXBlock flipBookBlock = m_ViewController.contexts.First().blockControllers.First().model;
            VFXSlot minValueSlot = flipBookBlock.GetInputSlot(0);


            float originalMinValue = 123.456f;
            minValueSlot.value = originalMinValue;

            string copyData = view.SerializeElements(view.selection.OfType<GraphElement>());

            boundsSlot.value = new AABox() { center = Vector3.zero, size = Vector3.zero };
            minValueSlot.value = 789f;

            view.UnserializeAndPasteElements("paste", copyData);
            var elements = view.Query().OfType<GraphElement>().ToList();

            var contexts = elements.OfType<VFXContextUI>().ToArray();
            var copyContext = elements.OfType<VFXContextUI>().Select(t => t.controller).First(t => t.model != newContext).model;

            var copyBoundsSlot = copyContext.GetInputSlot(0);
            var copyMinSlot = copyContext[0].GetInputSlot(0);

            Assert.AreEqual((AABox)copyBoundsSlot.value, originalBounds);
            Assert.AreEqual((float)copyMinSlot.value, originalMinValue);
            Assert.AreNotEqual(copyContext.position, newContext.position);


            view.UnserializeAndPasteElements("paste", copyData);

            elements = view.Query().OfType<GraphElement>().ToList();
            contexts = elements.OfType<VFXContextUI>().ToArray();

            var copy2Context = contexts.First(t => t.controller.model != newContext && t.controller.model != copyContext).controller.model;

            Assert.AreNotEqual(copy2Context.position, newContext.position);
            Assert.AreNotEqual(copy2Context.position, copyContext.position);
        }

        [Test]
        public void CopyPasteOperator()
        {
            var crossOperatorDesc = VFXLibrary.GetOperators().Where(t => t.name == "Cross Product").First();

            var newOperator = m_ViewController.AddVFXOperator(new Vector2(100, 100), crossOperatorDesc);

            m_ViewController.ApplyChanges();
            var operatorController = m_ViewController.allChildren.OfType<VFXOperatorController>().First();

            Assert.AreEqual(operatorController.model, newOperator);

            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();

            VFXView view = window.graphView;
            view.controller = m_ViewController;

            view.ClearSelection();
            foreach (var element in view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }


            VFXSlot aSlot = newOperator.GetInputSlot(0);

            Vector3 originalA = Vector3.one * 123;
            aSlot.value = originalA;

            string copyData = view.SerializeElements(view.selection.OfType<GraphElement>());

            aSlot.value = Vector3.one * 456;

            view.UnserializeAndPasteElements("paste", copyData);

            var elements = view.Query().OfType<GraphElement>().ToList();

            var copyOperator = elements.OfType<VFXOperatorUI>().First(t => t.controller.model != newOperator);

            var copaASlot = copyOperator.controller.model.GetInputSlot(0);

            Assert.AreEqual((Vector3)copaASlot.value, originalA);

            Assert.AreNotEqual(copyOperator.controller.model.position, newOperator.position);

            view.UnserializeAndPasteElements("paste", copyData);

            elements = view.Query().OfType<GraphElement>().ToList();
            var copy2Operator = elements.OfType<VFXOperatorUI>().First(t => t.controller.model != newOperator && t != copyOperator);

            Assert.AreNotEqual(copy2Operator.controller.model.position, newOperator.position);
            Assert.AreNotEqual(copy2Operator.controller.model.position, copyOperator.controller.model.position);
        }

        [Test]
        public void CopyPasteSpacableOperator()
        {
            var inlineOperatorDesc = VFXLibrary.GetOperators().Where(t => t.modelType == typeof(VFXInlineOperator)).First();

            var newOperator = m_ViewController.AddVFXOperator(new Vector2(100, 100), inlineOperatorDesc);
            newOperator.SetSettingValue("m_Type",new SerializableType(typeof(DirectionType)));

            m_ViewController.ApplyChanges();
            var operatorController = m_ViewController.allChildren.OfType<VFXOperatorController>().First();

            Assert.AreEqual(operatorController.model, newOperator);

            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();

            VFXView view = window.graphView;
            view.controller = m_ViewController;

            view.ClearSelection();
            foreach (var element in view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }


            VFXSlot aSlot = newOperator.GetInputSlot(0);

            Assert.IsTrue(aSlot.spaceable);

            aSlot.space = VFXCoordinateSpace.World;

            string copyData = view.SerializeElements(view.selection.OfType<GraphElement>());

            aSlot.space = VFXCoordinateSpace.Local;

            view.UnserializeAndPasteElements("paste", copyData);

            var elements = view.Query().OfType<GraphElement>().ToList();

            var copyOperator = elements.OfType<VFXOperatorUI>().First(t => t.controller.model != newOperator);

            var copyASlot = copyOperator.controller.model.GetInputSlot(0);

            Assert.AreEqual(VFXCoordinateSpace.World, copyASlot.space);
        }

        [Test]
        public void CopyPasteEdges()
        {
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/AllTests/Editor/Tests/CopyPasteTest.vfx");

            VFXViewController controller = VFXViewController.GetController(asset.GetResource(), true);

            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            VFXView view = window.graphView;

            view.controller = controller;

            view.ClearSelection();


            var originalElements = view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>().ToArray();

            Assert.AreNotEqual(originalElements.Length, 0);

            foreach (var element in originalElements)
            {
                view.AddToSelection(element);
            }

            string copyData = view.SerializeElements(view.selection.OfType<GraphElement>());

            view.controller = m_ViewController;

            view.UnserializeAndPasteElements("paste", copyData);

            m_ViewController.ApplyChanges();

            VFXParameterUI[] parameters = view.Query().OfType<VFXParameterUI>().ToList().ToArray();

            Assert.AreEqual(parameters.Length, 2);

            if (parameters[0].title == "Vector3")
            {
                var tmp = parameters[0];
                parameters[0] = parameters[1];
                parameters[1] = tmp;
            }

            VFXOperatorUI[] operators = view.Query().OfType<VFXOperatorUI>().ToList().ToArray();

            Assert.AreEqual(operators.Length, 2);

            VFXContextUI[] contexts = view.Query().OfType<VFXContextUI>().ToList().ToArray();

            Assert.AreEqual(contexts.Length, 2);

            if (contexts[0].controller.model is VFXBasicUpdate)
            {
                var tmp = contexts[0];
                contexts[0] = contexts[1];
                contexts[1] = tmp;
            }


            VFXDataEdge[] dataEdges = view.Query().OfType<VFXDataEdge>().ToList().ToArray();

            Assert.AreEqual(dataEdges.Length, 4);

            Assert.IsNotNull(dataEdges.Where(t =>
                t.output.GetFirstAncestorOfType<VFXNodeUI>() == parameters[1] &&
                operators.Contains(t.input.GetFirstAncestorOfType<VFXOperatorUI>())
                ).FirstOrDefault());

            Assert.IsNotNull(dataEdges.Where(t =>
                operators.Contains(t.input.GetFirstAncestorOfType<VFXOperatorUI>()) &&
                operators.Contains(t.output.GetFirstAncestorOfType<VFXOperatorUI>()) &&
                t.output.GetFirstAncestorOfType<VFXNodeUI>() != t.input.GetFirstAncestorOfType<VFXNodeUI>()
                ).FirstOrDefault());

            Assert.IsNotNull(dataEdges.Where(t =>
                t.output.GetFirstAncestorOfType<VFXNodeUI>() == parameters[0] &&
                t.input.GetFirstAncestorOfType<VFXNodeUI>() == contexts[0]
                ).FirstOrDefault());

            Assert.IsNotNull(dataEdges.Where(t =>
                operators.Contains(t.output.GetFirstAncestorOfType<VFXNodeUI>()) &&
                t.input.GetFirstAncestorOfType<VFXNodeUI>() == contexts[0].GetAllBlocks().First()
                ).FirstOrDefault());


            VFXFlowEdge flowEdge = view.Query().OfType<VFXFlowEdge>();

            Assert.IsNotNull(flowEdge);

            Assert.AreEqual(flowEdge.output.GetFirstAncestorOfType<VFXContextUI>(), contexts[1]);
            Assert.AreEqual(flowEdge.input.GetFirstAncestorOfType<VFXContextUI>(), contexts[0]);
        }

        [Test]
        public void CopyPasteBlock()
        {
            var initContextDesc = VFXLibrary.GetContexts().Where(t => typeof(VFXBasicInitialize).IsAssignableFrom(t.modelType)).First();

            var newContext = m_ViewController.AddVFXContext(new Vector2(100, 100), initContextDesc);

            m_ViewController.ApplyChanges();
            Assert.AreEqual(m_ViewController.allChildren.Where(t => t is VFXContextController).Count(), 1);

            var contextController = m_ViewController.allChildren.OfType<VFXContextController>().First();

            Assert.AreEqual(contextController.model, newContext);

            var flipBookBlockDesc = VFXLibrary.GetBlocks().First(t => t.name == "Set Tex Index");

            contextController.AddBlock(0, flipBookBlockDesc.CreateInstance());

            var newBlock = contextController.model.children.First();

            m_ViewController.ApplyChanges();

            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();

            VFXView view = window.graphView;
            view.controller = m_ViewController;

            view.ClearSelection();
            foreach (var element in view.Query().OfType<VFXBlockUI>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }

            VFXBlock flipBookBlock = m_ViewController.contexts.First().blockControllers.First().model;
            VFXSlot minValueSlot = flipBookBlock.GetInputSlot(0);

            float originalMinValue = 123.456f;
            minValueSlot.value = originalMinValue;

            string copyData = view.SerializeElements(view.selection.OfType<GraphElement>());

            minValueSlot.value = 789f;

            view.UnserializeAndPasteElements("Paste", copyData);

            view.controller.ApplyChanges();

            var elements = view.Query().OfType<VFXBlockUI>().ToList();

            var copyBlock = elements.Select(t => t.controller).First(t => t.model != newBlock).model;

            var copyMinSlot = copyBlock.GetInputSlot(0);

            Assert.AreEqual((float)copyMinSlot.value, originalMinValue);
        }

        [Test]
        public void CreateTemplate()
        {
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();

            VFXView view = window.graphView;
            view.controller = m_ViewController;

            view.CreateTemplateSystem("Assets/VFXEditor/Editor/Templates/Simple Particle System.vfx", Vector2.zero, null);
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

            // Query unique names
            var systemNames = view.controller.graph.systemNames;
            var uniqueNames = new List<string>();
            foreach (var system in spawners)
                uniqueNames.Add(systemNames.GetUniqueSystemName(system));
            var GPUSystems = VFXTestCommon.GetFieldValue<VFXView, List<VFXSystemBorder>>(view, "m_Systems");
            uniqueNames = uniqueNames.Concat(GPUSystems.Select(system => system.controller.title)).ToList();

            // Remove null or empty names, and duplicates
            uniqueNames = uniqueNames.Where(name => !string.IsNullOrEmpty(name)).Distinct().ToList();

            // Assert all names are unique, and the expected number of elements was obtained
            Assert.IsTrue(uniqueNames.Count() == spawnerCount + GPUSystemsCount, "Some systems have the same name or are null or empty.");
        }

    }
}
#endif
