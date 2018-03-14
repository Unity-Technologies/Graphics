using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.UI;
using System.IO;
using UnityEditor.VFX.Block.Test;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXCopyPasteTests
    {
        VFXViewController m_ViewController;

        const string testAssetName = "Assets/TmpTests/VFXGraph1.vfx";

        private int m_StartUndoGroupId;

        [SetUp]
        public void CreateTestAsset()
        {
            VisualEffectAsset asset = new VisualEffectAsset();

            var directoryPath = Path.GetDirectoryName(testAssetName);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            AssetDatabase.CreateAsset(asset, testAssetName);

            m_ViewController = VFXViewController.GetController(asset);
            m_StartUndoGroupId = Undo.GetCurrentGroup();
        }

        [TearDown]
        public void DestroyTestAsset()
        {
            Undo.RevertAllDownToGroup(m_StartUndoGroupId);
            AssetDatabase.DeleteAsset(testAssetName);
        }

        [Test]
        public void CopyPasteContextWithBlock()
        {
            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Initialize").First();

            var newContext = m_ViewController.AddVFXContext(new Vector2(100, 100), initContextDesc);

            m_ViewController.ApplyChanges();

            Assert.AreEqual(m_ViewController.allChildren.Where(t => t is VFXContextController).Count(), 1);

            var contextController = m_ViewController.allChildren.OfType<VFXContextController>().First();

            Assert.AreEqual(contextController.model, newContext);

            var flipBookBlockDesc = VFXLibrary.GetBlocks().First(t => t.name == "Flipbook Set TexIndex");

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

            VFXBlock flipBookBlock = m_ViewController.contexts.First().blockControllers.First().block;
            VFXSlot minValueSlot = flipBookBlock.GetInputSlot(0);


            float originalMinValue = 123.456f;
            minValueSlot.value = originalMinValue;

            view.CopySelectionCallback();

            boundsSlot.value = new AABox() { center = Vector3.zero, size = Vector3.zero };
            minValueSlot.value = 789f;

            view.PasteCallback();
            var elements = view.Query().OfType<GraphElement>().ToList();

            var contexts = elements.OfType<VFXContextUI>().ToArray();
            var copyContext = elements.OfType<VFXContextUI>().Select(t => t.controller).First(t => t.context != newContext).context;

            var copyBoundsSlot = copyContext.GetInputSlot(0);
            var copyMinSlot = copyContext[0].GetInputSlot(0);

            Assert.AreEqual((AABox)copyBoundsSlot.value, originalBounds);
            Assert.AreEqual((float)copyMinSlot.value, originalMinValue);
            Assert.AreNotEqual(copyContext.position, newContext.position);


            view.PasteCallback();

            elements = view.Query().OfType<GraphElement>().ToList();
            contexts = elements.OfType<VFXContextUI>().ToArray();

            var copy2Context = contexts.First(t => t.controller.context != newContext && t.controller.context != copyContext).controller.context;

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

            view.CopySelectionCallback();

            aSlot.value = Vector3.one * 456;

            view.PasteCallback();

            var elements = view.Query().OfType<GraphElement>().ToList();

            var copyOperator = elements.OfType<VFXOperatorUI>().First(t => t.controller.Operator != newOperator);

            var copaASlot = copyOperator.controller.Operator.GetInputSlot(0);

            Assert.AreEqual((Vector3)copaASlot.value, originalA);

            Assert.AreNotEqual(copyOperator.controller.Operator.position, newOperator.position);

            view.PasteCallback();

            elements = view.Query().OfType<GraphElement>().ToList();
            var copy2Operator = elements.OfType<VFXOperatorUI>().First(t => t.controller.Operator != newOperator && t != copyOperator);

            Assert.AreNotEqual(copy2Operator.controller.Operator.position, newOperator.position);
            Assert.AreNotEqual(copy2Operator.controller.Operator.position, copyOperator.controller.Operator.position);
        }

        [Test]
        public void CopyPasteEdges()
        {
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/VFXEditor/Editor/Tests/CopyPasteTest.vfx");

            VFXViewController controller = VFXViewController.GetController(asset, true);

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

            view.CopySelectionCallback();

            view.controller = m_ViewController;

            view.PasteCallback();

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

            if (contexts[0].controller.context is VFXBasicUpdate)
            {
                var tmp = contexts[0];
                contexts[0] = contexts[1];
                contexts[1] = tmp;
            }


            VFXDataEdge[] dataEdges = view.Query().OfType<VFXDataEdge>().ToList().ToArray();

            Assert.AreEqual(dataEdges.Length, 4);

            VFXOperator[] operatorModels = operators.Select(u => u.controller.Operator).ToArray();

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
            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Initialize").First();

            var newContext = m_ViewController.AddVFXContext(new Vector2(100, 100), initContextDesc);

            m_ViewController.ApplyChanges();
            Assert.AreEqual(m_ViewController.allChildren.Where(t => t is VFXContextController).Count(), 1);

            var contextController = m_ViewController.allChildren.OfType<VFXContextController>().First();

            Assert.AreEqual(contextController.model, newContext);

            var flipBookBlockDesc = VFXLibrary.GetBlocks().First(t => t.name == "Flipbook Set TexIndex");

            contextController.AddBlock(0, flipBookBlockDesc.CreateInstance());

            var newBlock = contextController.context.children.First();

            m_ViewController.ApplyChanges();

            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();

            VFXView view = window.graphView;
            view.controller = m_ViewController;

            view.ClearSelection();
            foreach (var element in view.Query().OfType<VFXBlockUI>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }

            VFXBlock flipBookBlock = m_ViewController.contexts.First().blockControllers.First().block;
            VFXSlot minValueSlot = flipBookBlock.GetInputSlot(0);

            float originalMinValue = 123.456f;
            minValueSlot.value = originalMinValue;

            view.CopySelectionCallback();

            minValueSlot.value = 789f;

            view.PasteCallback();

            view.controller.ApplyChanges();

            var elements = view.Query().OfType<VFXBlockUI>().ToList();

            var copyBlock = elements.Select(t => t.controller).First(t => t.block != newBlock).block;

            var copyMinSlot = copyBlock.GetInputSlot(0);

            Assert.AreEqual((float)copyMinSlot.value, originalMinValue);
        }

        [Test]
        public void CreateTemplate()
        {
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();

            VFXView view = window.graphView;
            view.controller = m_ViewController;

            view.CreateTemplateSystem("Assets/VFXEditor/Editor/Templates/Simple Particle System.vfx", Vector2.zero);
        }
    }
}
