using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.VFX;

using UnityEditor.VFX.Block;
using UnityEditor.VFX.Block.Test;
using UnityEditor.VFX.UI;
using System.Reflection;

using static UnityEditor.VFX.Test.VFXControllerTestHelper;

namespace UnityEditor.VFX.Test.Integration.Controllers
{
    [TestFixture]
    public class VFXControllerUndoRedoTests
    {
        private VFXViewController m_ViewController;
        private int m_StartUndoGroupId;

        [SetUp]
        public void CreateTestAsset()
        {
            m_ViewController = VFXControllerTestHelper.CreateTestAsset();
            m_StartUndoGroupId = Undo.GetCurrentGroup();
        }

        [OneTimeSetUp]
        public void Init()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
        }

        [OneTimeTearDown]
        public void OnTimeCleanup()
        {
            VFXTestCommon.CloseAllVFXWindow();
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [TearDown]
        public void DestroyTestAsset()
        {
            VFXControllerTestHelper.DestroyTestAsset(m_ViewController, m_StartUndoGroupId, null);
        }

        [Test]
        public void UndoRedoCollapseSlot()
        {
            Undo.IncrementCurrentGroup();
            var crossDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.variant.name.Contains("Cross"));
            var cross = m_ViewController.AddVFXOperator(new Vector2(0, 0), crossDesc.variant);
            m_ViewController.ApplyChanges();

            var operatorController = m_ViewController.allChildren.OfType<VFXOperatorController>().FirstOrDefault();
            Assert.IsNotNull(operatorController);

            foreach (var controller in operatorController.inputPorts.Concat(operatorController.outputPorts).Where(t => t.model.IsMasterSlot()))
            {
                Undo.IncrementCurrentGroup();
                Assert.IsTrue(controller.model.collapsed);
                controller.ExpandPath();
                Assert.IsTrue(!controller.model.collapsed);
            }

            m_ViewController.ApplyChanges();

            var totalSlotCount = cross.inputSlots.Concat(cross.outputSlots).Count();
            for (int step = 1; step <= totalSlotCount; step++)
            {
                Undo.PerformUndo();
                var vfxOperatorController = m_ViewController.allChildren.OfType<VFXOperatorController>().FirstOrDefault();
                Assert.IsNotNull(vfxOperatorController);

                var slots = cross.inputSlots.Concat(cross.outputSlots).Reverse();
                for (int i = 0; i < totalSlotCount; ++i)
                {
                    var slot = slots.ElementAt(i);
                    Assert.AreEqual(i < step, slot.collapsed);
                }
            }

            for (int step = 1; step <= totalSlotCount; step++)
            {
                Undo.PerformRedo();
                var vfxOperatorController = m_ViewController.allChildren.OfType<VFXOperatorController>().FirstOrDefault();
                Assert.IsNotNull(vfxOperatorController);

                var slots = cross.inputSlots.Concat(cross.outputSlots);
                for (int i = 0; i < totalSlotCount; ++i)
                {
                    var slot = slots.ElementAt(i);
                    Assert.AreEqual(i >= step, slot.collapsed);
                }
            }
        }

        [Test]
        public void UndoRedoMoveOperator()
        {
            Undo.IncrementCurrentGroup();
            var absDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Absolute");
            var abs = m_ViewController.AddVFXOperator(new Vector2(0, 0), absDesc.variant);
            m_ViewController.ApplyChanges();
            var absController = m_ViewController.allChildren.OfType<VFXOperatorController>().FirstOrDefault();

            var positions = new[] { new Vector2(1, 1), new Vector2(2, 2), new Vector2(3, 3), new Vector2(4, 4) };
            foreach (var position in positions)
            {
                Undo.IncrementCurrentGroup();
                absController.position = position;
            }

            Func<Type, VFXNodeController> fnFindController = delegate(Type type)
            {
                m_ViewController.ApplyChanges();
                var allController = m_ViewController.allChildren.OfType<VFXNodeController>();
                return allController.FirstOrDefault(o => type.IsInstanceOfType(o.slotContainer));
            };

            for (int i = 0; i < positions.Length; ++i)
            {
                var currentAbs = fnFindController(typeof(Operator.Absolute));
                Assert.IsNotNull(currentAbs);
                Assert.AreEqual(positions[positions.Length - i - 1].x, currentAbs.model.position.x);
                Assert.AreEqual(positions[positions.Length - i - 1].y, currentAbs.model.position.y);
                Undo.PerformUndo();
            }

            for (int i = 0; i < positions.Length; ++i)
            {
                Undo.PerformRedo();
                var currentAbs = fnFindController(typeof(Operator.Absolute));
                Assert.IsNotNull(currentAbs);
                Assert.AreEqual(positions[i].x, currentAbs.model.position.x);
                Assert.AreEqual(positions[i].y, currentAbs.model.position.y);
            }
        }

        [Test]
        public void UndoRedoAddOperator()
        {
            Func<VFXNodeController[]> fnAllOperatorController = delegate()
            {
                m_ViewController.ApplyChanges();
                var allController = m_ViewController.allChildren.OfType<VFXNodeController>();
                return allController.OfType<VFXOperatorController>().ToArray();
            };


            Action fnTestShouldExist = delegate()
            {
                var allOperatorController = fnAllOperatorController();
                Assert.AreEqual(1, allOperatorController.Length);
                Assert.IsInstanceOf(typeof(Operator.Absolute), allOperatorController[0].model);
            };

            Action fnTestShouldNotExist = delegate()
            {
                var allOperatorController = fnAllOperatorController();
                Assert.AreEqual(0, allOperatorController.Length);
            };

            Undo.IncrementCurrentGroup();
            var absDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Absolute");
            m_ViewController.AddVFXOperator(new Vector2(0, 0), absDesc.variant);

            fnTestShouldExist();
            Undo.PerformUndo();
            fnTestShouldNotExist();
            Undo.PerformRedo();
            fnTestShouldExist();

            Undo.IncrementCurrentGroup();
            m_ViewController.RemoveElement(fnAllOperatorController()[0]);
            fnTestShouldNotExist();
            Undo.PerformUndo();
            fnTestShouldExist();
            Undo.PerformRedo();
            fnTestShouldNotExist();
        }

        [Test]
        public void UndoRedoSetSlotValue()
        {
            Func<VFXNodeController[]> fnAllOperatorController = delegate()
            {
                m_ViewController.ApplyChanges();
                var allController = m_ViewController.allChildren.OfType<VFXNodeController>();
                return allController.OfType<VFXOperatorController>().ToArray();
            };

            var absDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Absolute");
            m_ViewController.AddVFXOperator(new Vector2(0, 0), absDesc.variant);

            var absOperator = fnAllOperatorController()[0];

            Undo.IncrementCurrentGroup();
            absOperator.inputPorts[0].value = 0;
            Undo.IncrementCurrentGroup();

            absOperator.inputPorts[0].value = 123;

            Undo.PerformUndo();

            Assert.AreEqual(0, absOperator.inputPorts[0].value);

            Undo.PerformRedo();

            Assert.AreEqual(123, absOperator.inputPorts[0].value);
        }

        [Test]
        public void UndoRedoChangeSpace()
        {
            var inlineOperatorDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.variant.modelType == typeof(VFXInlineOperator));
            var inlineOperator = m_ViewController.AddVFXOperator(new Vector2(0, 0), inlineOperatorDesc.variant);

            m_ViewController.ApplyChanges();
            var allController = m_ViewController.allChildren.OfType<VFXNodeController>().ToArray();
            var inlineOperatorController = allController.OfType<VFXOperatorController>().FirstOrDefault();
            inlineOperator.SetSettingValue("m_Type", (SerializableType)typeof(Position));

            Assert.AreEqual(inlineOperator.inputSlots[0].space, VFXSpace.Local);
            Assert.AreEqual((inlineOperatorController.model as VFXInlineOperator).inputSlots[0].space, VFXSpace.Local);
            Assert.AreEqual((inlineOperatorController.model as VFXInlineOperator).inputSlots[0].GetSpaceTransformationType(), SpaceableType.Position);

            Undo.IncrementCurrentGroup();
            inlineOperator.inputSlots[0].space = VFXSpace.World;
            Assert.AreEqual((inlineOperatorController.model as VFXInlineOperator).inputSlots[0].space, VFXSpace.World);
            Assert.AreEqual((inlineOperatorController.model as VFXInlineOperator).inputSlots[0].GetSpaceTransformationType(), SpaceableType.Position);

            Undo.PerformUndo(); //Should go back to local
            Assert.AreEqual((inlineOperatorController.model as VFXInlineOperator).inputSlots[0].space, VFXSpace.Local);
            Assert.AreEqual((inlineOperatorController.model as VFXInlineOperator).inputSlots[0].GetSpaceTransformationType(), SpaceableType.Position);
        }

        [Test]
        public void UndoRedoSetSlotValueThenGraphChange()
        {
            Func<VFXNodeController[]> fnAllOperatorController = delegate()
            {
                m_ViewController.ApplyChanges();
                var allController = m_ViewController.allChildren.OfType<VFXNodeController>();
                return allController.OfType<VFXOperatorController>().ToArray();
            };

            var absDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Absolute");
            m_ViewController.AddVFXOperator(new Vector2(0, 0), absDesc.variant);

            var absOperator = fnAllOperatorController()[0];

            Undo.IncrementCurrentGroup();
            absOperator.inputPorts[0].value = 0;

            absOperator.position = new Vector2(1, 2);


            Undo.IncrementCurrentGroup();

            absOperator.inputPorts[0].value = 123;

            Undo.IncrementCurrentGroup();

            absOperator.position = new Vector2(123, 456);

            Undo.IncrementCurrentGroup();

            absOperator.inputPorts[0].value = 789;

            Undo.PerformUndo(); // this should undo value change only

            Assert.AreEqual(123, absOperator.inputPorts[0].value);
            Assert.AreEqual(new Vector2(123, 456), absOperator.position);

            Undo.PerformUndo(); // this should undo position change only

            Assert.AreEqual(123, absOperator.inputPorts[0].value);
            Assert.AreEqual(new Vector2(1, 2), absOperator.position);

            Undo.PerformUndo(); // this should undo value change only

            Assert.AreEqual(0, absOperator.inputPorts[0].value);
            Assert.AreEqual(new Vector2(1, 2), absOperator.position);

            Undo.PerformRedo(); // this should redo value change only

            Assert.AreEqual(123, absOperator.inputPorts[0].value);
            Assert.AreEqual(new Vector2(1, 2), absOperator.position);

            Undo.PerformRedo(); // this should redo position change only

            Assert.AreEqual(123, absOperator.inputPorts[0].value);
            Assert.AreEqual(new Vector2(123, 456), absOperator.position);

            Undo.PerformRedo(); // this should redo value change only

            Assert.AreEqual(789, absOperator.inputPorts[0].value);
            Assert.AreEqual(new Vector2(123, 456), absOperator.position);
        }

        [Test]
        public void UndoRedoSetSlotValueAndGraphChange()
        {
            Func<VFXNodeController[]> fnAllOperatorController = delegate()
            {
                m_ViewController.ApplyChanges();
                var allController = m_ViewController.allChildren.OfType<VFXNodeController>();
                return allController.OfType<VFXOperatorController>().ToArray();
            };

            var absDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Absolute");
            m_ViewController.AddVFXOperator(new Vector2(0, 0), absDesc.variant);

            var absOperator = fnAllOperatorController()[0];

            Undo.IncrementCurrentGroup();

            absOperator.inputPorts[0].value = 0;
            absOperator.position = new Vector2(1, 2);

            Undo.IncrementCurrentGroup();

            absOperator.inputPorts[0].value = 123;
            absOperator.position = new Vector2(123, 456);

            Undo.PerformUndo();

            Assert.AreEqual(0, absOperator.inputPorts[0].value);
            Assert.AreEqual(new Vector2(1, 2), absOperator.position);

            Undo.PerformRedo();

            Assert.AreEqual(123, absOperator.inputPorts[0].value);
            Assert.AreEqual(new Vector2(123, 456), absOperator.position);
        }

        [Test]
        public void UndoRedoOperatorLinkSimple()
        {
            Func<Type, VFXNodeController> fnFindController = delegate(Type type)
            {
                m_ViewController.ApplyChanges();
                var allController = m_ViewController.allChildren.OfType<VFXNodeController>();
                return allController.FirstOrDefault(o => type.IsInstanceOfType(o.slotContainer));
            };

            var cosDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Cosine");
            var sinDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Sine");
            Undo.IncrementCurrentGroup();
            m_ViewController.AddVFXOperator(new Vector2(0, 0), cosDesc.variant);
            Undo.IncrementCurrentGroup();
            m_ViewController.AddVFXOperator(new Vector2(1, 1), sinDesc.variant);
            var cosController = fnFindController(typeof(Operator.Cosine));
            var sinController = fnFindController(typeof(Operator.Sine));

            Func<int> fnCountEdge = delegate()
            {
                return m_ViewController.allChildren.OfType<VFXDataEdgeController>().Count();
            };

            Undo.IncrementCurrentGroup();
            Assert.AreEqual(0, fnCountEdge());

            var edgeControllerSin = new VFXDataEdgeController(sinController.inputPorts[0], cosController.outputPorts[0]);
            m_ViewController.AddElement(edgeControllerSin);
            Assert.AreEqual(1, fnCountEdge());

            Undo.PerformUndo();
            m_ViewController.ApplyChanges();
            Assert.AreEqual(0, fnCountEdge());
            Assert.NotNull(fnFindController(typeof(Operator.Cosine)));
            Assert.NotNull(fnFindController(typeof(Operator.Sine)));
        }

        [Test]
        public void UndoRedoOperatorLinkToBlock()
        {
            Func<VFXContextController> fnFirstContextController = delegate()
            {
                m_ViewController.ApplyChanges();
                return m_ViewController.allChildren.OfType<VFXContextController>().FirstOrDefault();
            };

            Func<Type, VFXNodeController> fnFindController = delegate(Type type)
            {
                m_ViewController.ApplyChanges();
                var allController = m_ViewController.allChildren.OfType<VFXNodeController>();
                return allController.FirstOrDefault(o => type.IsInstanceOfType(o.slotContainer));
            };

            Func<VFXBlockController> fnFirstBlockController = delegate()
            {
                m_ViewController.ApplyChanges();
                return m_ViewController.allChildren.OfType<VFXContextController>().SelectMany(t => t.blockControllers).FirstOrDefault();
            };

            Func<VFXDataEdgeController> fnFirstEdgeController = delegate()
            {
                m_ViewController.ApplyChanges();
                return m_ViewController.allChildren.OfType<VFXDataEdgeController>().FirstOrDefault();
            };

            Undo.IncrementCurrentGroup();
            var cosDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Cosine");
            var contextUpdateDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.variant.name.Contains("Update"));
            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.variant.modelType == typeof(Block.SetAttribute));

            m_ViewController.AddVFXOperator(new Vector2(0, 0), cosDesc.variant);
            m_ViewController.AddVFXContext(new Vector2(2, 2), contextUpdateDesc.variant);
            var blockAttribute = blockAttributeDesc.CreateInstance();
            blockAttribute.SetSettingValue("attribute", "color");
            blockAttribute.SetSettingValue("Source", Block.SetAttribute.ValueSource.Slot);
            fnFirstContextController().AddBlock(0, blockAttribute);

            var firstBlockController = fnFirstBlockController();
            var cosController = fnFindController(typeof(Operator.Cosine));

            var blockInputPorts = firstBlockController.inputPorts.ToArray();
            var cosOutputPorts = cosController.outputPorts.ToArray();

            var edgeController = new VFXDataEdgeController(blockInputPorts[0], cosOutputPorts[0]);
            m_ViewController.AddElement(edgeController);
            Undo.IncrementCurrentGroup();

            m_ViewController.RemoveElement(fnFirstEdgeController());
            Assert.IsNull(fnFirstEdgeController());
            Undo.IncrementCurrentGroup();

            Undo.PerformUndo();
            Assert.IsNotNull(fnFirstEdgeController());

            Undo.PerformRedo();
            Assert.IsNull(fnFirstEdgeController());
        }

        [Test]
        public void UndoRedoOperatorSettings()
        {
            Func<VFXOperatorController> fnFirstOperatorController = delegate()
            {
                m_ViewController.ApplyChanges();
                return m_ViewController.allChildren.OfType<VFXOperatorController>().FirstOrDefault();
            };

            var swizzleDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Swizzle");
            m_ViewController.AddVFXOperator(new Vector2(0, 0), swizzleDesc.variant);

            var maskList = new string[] { "xy", "yww", "xw", "z" };
            for (int i = 0; i < maskList.Length; ++i)
            {
                var componentMaskController = fnFirstOperatorController();
                Undo.IncrementCurrentGroup();
                (componentMaskController.model as Operator.Swizzle).SetSettingValue("mask", maskList[i]);
                Assert.AreEqual(maskList[i], (componentMaskController.model as Operator.Swizzle).mask);
            }

            for (int i = maskList.Length - 1; i > 0; --i)
            {
                Undo.PerformUndo();
                var componentMaskController = fnFirstOperatorController();
                Assert.AreEqual(maskList[i - 1], (componentMaskController.model as Operator.Swizzle).mask);
            }

            for (int i = 0; i < maskList.Length - 1; ++i)
            {
                Undo.PerformRedo();
                var componentMaskController = fnFirstOperatorController();
                Assert.AreEqual(maskList[i + 1], (componentMaskController.model as Operator.Swizzle).mask);
            }
        }

        [Test]
        public void UndoRedoAddBlockContext()
        {
            var contextUpdateDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.variant.name.Contains("Update"));
            var blockDesc = new VFXModelDescriptor<VFXBlock>(new Variant(null, null, typeof(AllType), null), null);

            m_ViewController.AddVFXContext(Vector2.one, contextUpdateDesc.variant);
            Func<VFXContextController> fnContextController = delegate()
            {
                m_ViewController.ApplyChanges();
                var allContextController = m_ViewController.allChildren.OfType<VFXContextController>().ToArray();
                return allContextController.FirstOrDefault() as VFXContextController;
            };
            Assert.IsNotNull(fnContextController());
            //Creation
            Undo.IncrementCurrentGroup();
            fnContextController().AddBlock(0, blockDesc.CreateInstance());
            Assert.AreEqual(1, fnContextController().model.children.Count());
            Undo.PerformUndo();
            Assert.AreEqual(0, fnContextController().model.children.Count());

            //Deletion
            var block = blockDesc.CreateInstance();
            fnContextController().AddBlock(0, block);
            Assert.AreEqual(1, fnContextController().model.children.Count());
            Undo.IncrementCurrentGroup();
            fnContextController().RemoveBlock(block);
            Assert.AreEqual(0, fnContextController().model.children.Count());

            Undo.PerformUndo();

            m_ViewController.ApplyChanges();


            Assert.IsNotNull(fnContextController());
            Assert.AreEqual(1, fnContextController().model.children.Count());
            Assert.IsInstanceOf(typeof(AllType), fnContextController().model.children.First());
        }

        [Test]
        public void UndoRedoContext()
        {
            Func<VFXContextController> fnFirstContextController = delegate()
            {
                m_ViewController.ApplyChanges();
                return m_ViewController.allChildren.OfType<VFXContextController>().FirstOrDefault() as VFXContextController;
            };

            var contextDesc = VFXLibrary.GetContexts().FirstOrDefault();
            Undo.IncrementCurrentGroup();
            m_ViewController.AddVFXContext(Vector2.zero, contextDesc.variant);

            Assert.NotNull(fnFirstContextController());
            Undo.PerformUndo();
            Assert.Null(fnFirstContextController(), "Fail Undo Create");

            Undo.IncrementCurrentGroup();
            m_ViewController.AddVFXContext(Vector2.zero, contextDesc.variant);
            Assert.NotNull(fnFirstContextController());

            Undo.IncrementCurrentGroup();
            m_ViewController.RemoveElement(fnFirstContextController());
            Assert.Null(fnFirstContextController());

            Undo.PerformUndo();
            Assert.NotNull(fnFirstContextController(), "Fail Undo Delete");
        }

        [Test]
        public void UndoRedoContextLinkMultiSlot()
        {
            Func<VFXContextController[]> fnContextController = delegate()
            {
                m_ViewController.ApplyChanges();
                return m_ViewController.allChildren.OfType<VFXContextController>().Cast<VFXContextController>().ToArray();
            };

            Func<VFXContextController> fnSpawner = delegate()
            {
                var controller = fnContextController();
                return controller.FirstOrDefault(o => o.model.name.Contains("Spawn"));
            };

            Func<string, VFXContextController> fnEvent = delegate(string name)
            {
                var controller = fnContextController();
                var allEvent = controller.Where(o => o.model.name.Contains("Event"));
                return allEvent.FirstOrDefault(o => (o.model as VFXBasicEvent).eventName == name) as VFXContextController;
            };

            Func<VFXContextController> fnStart = delegate()
            {
                return fnEvent("Start");
            };

            Func<VFXContextController> fnStop = delegate()
            {
                return fnEvent("Stop");
            };

            Func<int> fnFlowEdgeCount = delegate()
            {
                m_ViewController.ApplyChanges();
                return m_ViewController.allChildren.OfType<VFXFlowEdgeController>().Count();
            };

            var contextSpawner = VFXLibrary.GetContexts().First(x => x.modelType == typeof(VFXBasicSpawner));
            var contextEvent = VFXLibrary.GetContexts().First(x => x.modelType == typeof(VFXBasicEvent));

            m_ViewController.AddVFXContext(new Vector2(1, 1), contextSpawner.variant);
            var eventStartController = m_ViewController.AddVFXContext(new Vector2(2, 2), contextEvent.variant);
            var eventStopController = m_ViewController.AddVFXContext(new Vector2(3, 3), contextEvent.variant);
            eventStartController.SetSettingValue("eventName", "Start");
            eventStopController.SetSettingValue("eventName", "Stop");

            //Creation
            var flowEdge = new VFXFlowEdgeController(fnSpawner().flowInputAnchors.ElementAt(0), fnStart().flowOutputAnchors.FirstOrDefault());

            Undo.IncrementCurrentGroup();
            m_ViewController.AddElement(flowEdge);
            Assert.AreEqual(1, fnFlowEdgeCount());

            flowEdge = new VFXFlowEdgeController(fnSpawner().flowInputAnchors.ElementAt(1), fnStop().flowOutputAnchors.FirstOrDefault());

            Undo.IncrementCurrentGroup();
            m_ViewController.AddElement(flowEdge);
            Assert.AreEqual(2, fnFlowEdgeCount());

            //Test a single deletion
            var allFlowEdge = m_ViewController.allChildren.OfType<VFXFlowEdgeController>().ToArray();

            // Integrity test...
            var inputSlotIndex = allFlowEdge.Select(o => (o.input as VFXFlowAnchorController).slotIndex).OrderBy(o => o).ToArray();
            var outputSlotIndex = allFlowEdge.Select(o => (o.output as VFXFlowAnchorController).slotIndex).OrderBy(o => o).ToArray();

            Assert.AreEqual(inputSlotIndex[0], 0);
            Assert.AreEqual(inputSlotIndex[1], 1);
            Assert.AreEqual(outputSlotIndex[0], 0);
            Assert.AreEqual(outputSlotIndex[1], 0);

            var edge = allFlowEdge.First(o => o.input == fnSpawner().flowInputAnchors.ElementAt(1) && o.output == fnStop().flowOutputAnchors.FirstOrDefault());

            Undo.IncrementCurrentGroup();
            m_ViewController.RemoveElement(edge);
            Assert.AreEqual(1, fnFlowEdgeCount());

            Undo.PerformUndo();
            Assert.AreEqual(2, fnFlowEdgeCount());

            Undo.PerformRedo();
            Assert.AreEqual(1, fnFlowEdgeCount());

            Undo.PerformUndo();
            Assert.AreEqual(2, fnFlowEdgeCount());

            //Global Deletion
            Undo.PerformUndo();
            Assert.AreEqual(1, fnFlowEdgeCount());

            Undo.PerformUndo();
            Assert.AreEqual(0, fnFlowEdgeCount());
        }

        [Test]
        public void UndoRedoContextLink()
        {
            Func<VFXContextController[]> fnContextController = delegate()
            {
                m_ViewController.ApplyChanges();
                return m_ViewController.allChildren.OfType<VFXContextController>().Cast<VFXContextController>().ToArray();
            };

            Func<VFXContextController> fnInitializeController = delegate()
            {
                var controller = fnContextController();
                return controller.FirstOrDefault(o => o.model.name.Contains("Init"));
            };

            Func<VFXContextController> fnUpdateController = delegate()
            {
                var controller = fnContextController();
                return controller.FirstOrDefault(o => o.model.name.Contains("Update"));
            };

            Func<int> fnFlowEdgeCount = delegate()
            {
                m_ViewController.ApplyChanges();
                return m_ViewController.allChildren.OfType<VFXFlowEdgeController>().Count();
            };

            var contextInitializeDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.variant.name.Contains("Init"));
            var contextUpdateDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.variant.name.Contains("Update"));

            m_ViewController.AddVFXContext(new Vector2(1, 1), contextInitializeDesc.variant);
            m_ViewController.AddVFXContext(new Vector2(2, 2), contextUpdateDesc.variant);

            //Creation
            var flowEdge = new VFXFlowEdgeController(fnUpdateController().flowInputAnchors.FirstOrDefault(), fnInitializeController().flowOutputAnchors.FirstOrDefault());

            Undo.IncrementCurrentGroup();
            m_ViewController.AddElement(flowEdge);
            Assert.AreEqual(1, fnFlowEdgeCount());

            Undo.PerformUndo();
            Assert.AreEqual(0, fnFlowEdgeCount(), "Fail undo Create");

            //Deletion
            flowEdge = new VFXFlowEdgeController(fnUpdateController().flowInputAnchors.FirstOrDefault(), fnInitializeController().flowOutputAnchors.FirstOrDefault());
            m_ViewController.AddElement(flowEdge);
            Assert.AreEqual(1, fnFlowEdgeCount());

            Undo.IncrementCurrentGroup();
            m_ViewController.RemoveElement(m_ViewController.allChildren.OfType<VFXFlowEdgeController>().FirstOrDefault());
            Assert.AreEqual(0, fnFlowEdgeCount());

            Undo.PerformUndo();
            Assert.AreEqual(1, fnFlowEdgeCount(), "Fail undo Delete");
        }

        [Test]
        public void UndoRedoEnableBlock()
        {
            var contextUpdateDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Update"));
            var gravityDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.name == "Gravity");
            var notOperatorDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Not");

            var updateContext = m_ViewController.AddVFXContext(new Vector2(10, 0), contextUpdateDesc.variant);
            var gravityBlock = gravityDesc.CreateInstance();
            var notOperator = m_ViewController.AddVFXOperator(new Vector2(0, 8), notOperatorDesc.variant);

            notOperator.outputSlots[0].Link(gravityBlock.activationSlot);
            updateContext.AddChild(gravityBlock);
            m_ViewController.ApplyChanges();

            Assert.IsTrue(gravityBlock.enabled);

            Undo.IncrementCurrentGroup();

            var notController = m_ViewController.GetNodeController(notOperator, 0);
            notController.inputPorts[0].value = true;
            Assert.IsFalse(gravityBlock.enabled);

            Undo.PerformUndo();
            Assert.IsTrue(gravityBlock.enabled);

            Undo.PerformRedo();
            Assert.IsFalse(gravityBlock.enabled);
        }

        [Test]
        public void UndoRedoAddRemoveGroup()
        {
            var notOperatorDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Not");
            var notOperatorA = m_ViewController.AddVFXOperator(new Vector2(0, 0), notOperatorDesc.variant);
            var notOperatorB = m_ViewController.AddVFXOperator(new Vector2(0, 40), notOperatorDesc.variant);

            m_ViewController.ApplyChanges();

            var notControllerA = m_ViewController.GetNodeController(notOperatorA, 0);
            var notControllerB = m_ViewController.GetNodeController(notOperatorB, 0);

            Undo.IncrementCurrentGroup();

            m_ViewController.GroupNodes(new [] { notControllerA, notControllerB });
            m_ViewController.ApplyChanges();

            Assert.AreEqual(1, m_ViewController.groupNodes.Count);
            Assert.IsTrue(m_ViewController.groupNodes.First().ContainsNode(notControllerA));
            Assert.IsTrue(m_ViewController.groupNodes.First().ContainsNode(notControllerB));

            Undo.PerformUndo();

            Assert.AreEqual(0, m_ViewController.groupNodes.Count);

            Undo.PerformRedo();

            notControllerA = m_ViewController.GetNodeController(notOperatorA, 0);
            notControllerB = m_ViewController.GetNodeController(notOperatorB, 0);

            Assert.AreEqual(1, m_ViewController.groupNodes.Count);
            Assert.IsTrue(m_ViewController.groupNodes.First().ContainsNode(notControllerA));
            Assert.IsTrue(m_ViewController.groupNodes.First().ContainsNode(notControllerB));
        }

        [Test]
        public void UndoRedoAddModifyRemoveStickyNote()
        {
            const string testStr = "TEST";

            m_ViewController.AddStickyNote(new Vector2(0, 0), null);
            m_ViewController.ApplyChanges();

            Assert.AreEqual(1, m_ViewController.stickyNotes.Count);
            Assert.AreNotEqual(testStr, m_ViewController.stickyNotes.First().title);

            Undo.IncrementCurrentGroup();

            m_ViewController.stickyNotes.First().title = testStr;
            Assert.AreEqual(testStr, m_ViewController.stickyNotes.First().title);

            Undo.IncrementCurrentGroup();

            m_ViewController.RemoveElement(m_ViewController.stickyNotes.First());
            Assert.AreEqual(0, m_ViewController.stickyNotes.Count);

            Undo.PerformUndo();
            Assert.AreEqual(1, m_ViewController.stickyNotes.Count);
            Assert.AreEqual(testStr, m_ViewController.stickyNotes.First().title);

            Undo.PerformUndo();
            Assert.AreEqual(1, m_ViewController.stickyNotes.Count);
            Assert.AreNotEqual(testStr, m_ViewController.stickyNotes.First().title);

            Undo.PerformUndo();
            Assert.AreEqual(0, m_ViewController.stickyNotes.Count);

            Undo.PerformRedo();
            Assert.AreEqual(1, m_ViewController.stickyNotes.Count);
            Assert.AreNotEqual(testStr, m_ViewController.stickyNotes.First().title);

            Undo.PerformRedo();
            Assert.AreEqual(1, m_ViewController.stickyNotes.Count);
            Assert.AreEqual(testStr, m_ViewController.stickyNotes.First().title);

            Undo.PerformRedo();
            Assert.AreEqual(0, m_ViewController.stickyNotes.Count);
        }

        [Test]
        public void UndoRedo_CustomHLSL_Changing_Function_List()
        {
            {
                //Arrange
                var customHLSLDesc = VFXLibrary.GetOperators().SingleOrDefault(o => o.variant.name.Contains("Custom HLSL"));
                Assert.IsNotNull(customHLSLDesc);
                var customHLSL = m_ViewController.AddVFXOperator(new Vector2(0, 0), customHLSLDesc.variant);
                var twoFunctionCode = "float Fn_A(in float value){{ return value; }}\n";
                twoFunctionCode += "float Fn_B(in float value){{ return value; }}\n";
                var threeFunctionCode = twoFunctionCode;
                threeFunctionCode += "float Fn_C(in float value){{ return value; }}\n";

                customHLSL.SetSettingValue("m_HLSLCode", threeFunctionCode);
                m_ViewController.ApplyChanges();

                //Forward Execution
                Undo.IncrementCurrentGroup();
                var availableFunction = (MultipleValuesChoice<string>)customHLSL.GetSettingValue("m_AvailableFunctions");
                Assert.IsNotNull(availableFunction.values);
                Assert.IsTrue(availableFunction.values.SequenceEqual(new[] { "Fn_A", "Fn_B", "Fn_C" }));

                Undo.IncrementCurrentGroup();
                customHLSL.SetSettingValue("m_HLSLCode", twoFunctionCode);
                availableFunction = (MultipleValuesChoice<string>)customHLSL.GetSettingValue("m_AvailableFunctions");
                Assert.IsNotNull(availableFunction.values);
                Assert.IsTrue(availableFunction.values.SequenceEqual(new[] { "Fn_A", "Fn_B" }));

                Undo.IncrementCurrentGroup();
                var nodeController = m_ViewController.GetNodeController(customHLSL, 0);
                Assert.IsNotNull(nodeController);
                m_ViewController.RemoveElement(nodeController);
                Assert.AreEqual(0, m_ViewController.graph.children.Count());
            }

            //Backward execution
            {
                Undo.PerformUndo();
                Assert.AreEqual(1, m_ViewController.graph.children.Count());
                var customHLSL = m_ViewController.graph.children.OfType<Operator.CustomHLSL>().SingleOrDefault();
                Assert.IsNotNull(customHLSL);

                var availableFunction = (MultipleValuesChoice<string>)customHLSL.GetSettingValue("m_AvailableFunctions");
                Assert.IsNotNull(availableFunction.values);
                Assert.IsTrue(availableFunction.values.SequenceEqual(new[] { "Fn_A", "Fn_B" }));

                Undo.PerformUndo();
                availableFunction = (MultipleValuesChoice<string>)customHLSL.GetSettingValue("m_AvailableFunctions");
                Assert.IsNotNull(availableFunction.values);
                Assert.IsTrue(availableFunction.values.SequenceEqual(new[] { "Fn_A", "Fn_B", "Fn_C" }));
            }
        }

        [UnityTest]
        public IEnumerator Clear_Undo_Stack_After_Closing_Window()
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            var resource = graph.GetResource();
            var asset = resource.asset;

            // Count only this graph's entries: the fixture's own controller can record entries for its asset at any time
            var graphUndoName = $"VFX Graph - {Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(asset))} (";
            var undoList = new List<string>();

            var window = VFXTestCommon.GetWindow(asset, true);
            window.LoadAsset(asset, null);
            for (int frame = 0; frame < 10; frame++)
            {
                if (VisualEffectAssetUtility.GetCompilationMode(asset) != VFXCompilationMode.Edition)
                    yield return null;
                else
                    break;
            }
            Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(asset));

            undoList.Clear();
            Undo.GetUndoList(undoList, out var cursor);
            var loadUndoVFXCount = undoList.Count(o => o.Contains(graphUndoName));

            // A new group before each move lets late graph changes from loading merge into a move's entry
            var eventCount = 4;
            for (int eventIndex = 0; eventIndex < eventCount; eventIndex++)
            {
                Undo.IncrementCurrentGroup();
                graph.children.OfType<VFXContext>().First().position += Vector2.down;
                yield return null;
            }
            undoList.Clear();
            Undo.GetUndoList(undoList, out cursor);
            var undoVFXCount = undoList.Count(o => o.Contains(graphUndoName));
            Assert.AreEqual(eventCount, undoVFXCount - loadUndoVFXCount, string.Join("\n", undoList));

            window.Close();
            yield return null;

            undoList.Clear();
            Undo.GetUndoList(undoList, out cursor);
            undoVFXCount = undoList.Count(o => o.Contains(graphUndoName));
            Assert.AreEqual(0, undoVFXCount, string.Join("\n", undoList));
        }
    }
}
