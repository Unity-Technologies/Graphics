#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
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

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXControllersTests
    {
        VFXViewController m_ViewController;

        const string testAssetName = "Assets/TmpTests/VFXGraph1.vfx";

        const string testSubgraphAssetName = "Assets/TmpTests/VFXGraphSub.vfx";
        const string testSubgraphSubAssetName = "Assets/TmpTests/VFXGraphSub_Subgraph.vfx";

        const string testAssetMainSubgraph = "Assets/TmpTests/VFXGraphSubGraph_Main.vfx";
        const string testSubgraphSubOperatorAssetName = "Assets/TmpTests/VFXGraphSub_Subgraph.vfxoperator";
        const string testSubgraphBlockAssetName = "Assets/TmpTests/VFXGraphSub_Subgraph.vfxblock";

        private int m_StartUndoGroupId;
        private string testAssetRandomFileName;

        [SetUp]
        public void CreateTestAsset()
        {
            var directoryPath = Path.GetDirectoryName(testAssetName);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            if (File.Exists(testAssetName))
            {
                AssetDatabase.DeleteAsset(testAssetName);
            }

            VisualEffectAsset asset = VisualEffectAssetEditorUtility.CreateNewAsset(testAssetName);
            VisualEffectResource resource = asset.GetResource(); // force resource creation

            m_ViewController = VFXViewController.GetController(resource);
            m_ViewController.useCount++;

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
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [TearDown]
        public void DestroyTestAsset()
        {
            m_ViewController.useCount--;
            Undo.RevertAllDownToGroup(m_StartUndoGroupId);
            AssetDatabase.DeleteAsset(testAssetName);
            AssetDatabase.DeleteAsset(testAssetMainSubgraph);
            AssetDatabase.DeleteAsset(testSubgraphAssetName);
            AssetDatabase.DeleteAsset(testSubgraphSubAssetName);
            AssetDatabase.DeleteAsset(testSubgraphSubOperatorAssetName);
            AssetDatabase.DeleteAsset(testSubgraphBlockAssetName);
            if (!string.IsNullOrEmpty(testAssetRandomFileName))
            {
                AssetDatabase.DeleteAsset(testAssetRandomFileName);
            }
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        #pragma warning disable 0414
        static private bool[] usePosition = { true, false };

        #pragma warning restore 0414
        [Test]
        public void LinkPositionOrVectorAndDirection([ValueSource("usePosition")] bool usePosition)
        {
            var crossDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.variant.name.Contains("Cross"));
            var positionDesc = VFXLibrary.GetParameters().FirstOrDefault(o => o.variant.name.Contains("Position"));
            var vectorDesc = VFXLibrary.GetParameters().FirstOrDefault(o => o.name == "Vector");
            var directionDesc = VFXLibrary.GetParameters().FirstOrDefault(o => o.variant.name.Contains("Direction"));

            var cross = m_ViewController.AddVFXOperator(new Vector2(1, 1), crossDesc.variant);
            var position = m_ViewController.AddVFXParameter(new Vector2(2, 2), positionDesc.variant);
            var vector = m_ViewController.AddVFXParameter(new Vector2(3, 3), vectorDesc.variant);
            var direction = m_ViewController.AddVFXParameter(new Vector2(4, 4), directionDesc.variant);
            (cross as IVFXOperatorUniform).SetOperandType(typeof(Vector3));

            m_ViewController.ApplyChanges();

            Func<IVFXSlotContainer, VFXNodeController> fnFindController = delegate(IVFXSlotContainer slotContainer)
            {
                var allController = m_ViewController.allChildren.OfType<VFXNodeController>();
                return allController.FirstOrDefault(o => o.slotContainer == slotContainer);
            };

            var controllerCross = fnFindController(cross);

            var vA = new Vector3(2, 3, 4);
            position.outputSlots[0].value = new Position() { position = vA };
            vector.outputSlots[0].value = new Vector() { vector = vA };

            var vB = new Vector3(5, 6, 7);
            direction.outputSlots[0].value = new DirectionType() { direction = vB };

            var edgeControllerAppend_A = new VFXDataEdgeController(controllerCross.inputPorts.Where(o => o.portType == typeof(Vector3)).First(), fnFindController(usePosition ? position : vector).outputPorts.First());
            m_ViewController.AddElement(edgeControllerAppend_A);
            (cross as IVFXOperatorUniform).SetOperandType(typeof(Vector3));
            m_ViewController.ApplyChanges();

            var edgeControllerAppend_B = new VFXDataEdgeController(controllerCross.inputPorts.Where(o => o.portType == typeof(Vector3)).Last(), fnFindController(direction).outputPorts.First());
            m_ViewController.AddElement(edgeControllerAppend_B);
            (cross as IVFXOperatorUniform).SetOperandType(typeof(Vector3));
            m_ViewController.ApplyChanges();

            m_ViewController.ForceReload();

            Assert.AreEqual(1, cross.inputSlots[0].LinkedSlots.Count());
            Assert.AreEqual(1, cross.inputSlots[1].LinkedSlots.Count());

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation | VFXExpressionContextOption.ConstantFolding);

            var currentA = context.Compile(cross.inputSlots[0].GetExpression()).Get<Vector3>();
            var currentB = context.Compile(cross.inputSlots[1].GetExpression()).Get<Vector3>();
            var result = context.Compile(cross.outputSlots[0].GetExpression()).Get<Vector3>();

            Assert.AreEqual((double)vA.x, (double)currentA.x, 0.001f);
            Assert.AreEqual((double)vA.y, (double)currentA.y, 0.001f);
            Assert.AreEqual((double)vA.z, (double)currentA.z, 0.001f);

            Assert.AreEqual((double)vB.normalized.x, (double)currentB.x, 0.001f);
            Assert.AreEqual((double)vB.normalized.y, (double)currentB.y, 0.001f);
            Assert.AreEqual((double)vB.normalized.z, (double)currentB.z, 0.001f);

            var expectedResult = Vector3.Cross(vA, vB.normalized);
            Assert.AreEqual((double)expectedResult.x, (double)result.x, 0.001f);
            Assert.AreEqual((double)expectedResult.y, (double)result.y, 0.001f);
            Assert.AreEqual((double)expectedResult.z, (double)result.z, 0.001f);
        }

        [Test]
        public void LinkToDirection()
        {
            var directionDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.modelType == typeof(VFXInlineOperator) && o.HasSettingValue(typeof(DirectionType)));
            var vector3Desc = VFXLibrary.GetOperators().FirstOrDefault(o => o.modelType == typeof(VFXInlineOperator) && o.HasSettingValue(typeof(Vector3)));

            var direction = m_ViewController.AddVFXOperator(new Vector2(1, 1), directionDesc.variant);
            var vector3 = m_ViewController.AddVFXOperator(new Vector2(2, 2), vector3Desc.variant);
            m_ViewController.ApplyChanges();

            Func<IVFXSlotContainer, VFXNodeController> fnFindController = delegate(IVFXSlotContainer slotContainer)
            {
                var allController = m_ViewController.allChildren.OfType<VFXNodeController>();
                return allController.FirstOrDefault(o => o.slotContainer == slotContainer);
            };

            var vA = new Vector3(8, 9, 6);
            vector3.inputSlots[0].value = vA;

            var controllerDirection = fnFindController(direction);
            var controllerVector3 = fnFindController(vector3);

            var edgeControllerAppend_A = new VFXDataEdgeController(controllerDirection.inputPorts.First(), controllerVector3.outputPorts.First());
            m_ViewController.AddElement(edgeControllerAppend_A);
            m_ViewController.ApplyChanges();

            m_ViewController.ForceReload();

            Assert.AreEqual(1, direction.inputSlots[0].LinkedSlots.Count());
            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation | VFXExpressionContextOption.ConstantFolding);

            var result = context.Compile(direction.outputSlots[0].GetExpression()).Get<Vector3>();

            Assert.AreEqual((double)vA.normalized.x, (double)result.x, 0.001f);
            Assert.AreEqual((double)vA.normalized.y, (double)result.y, 0.001f);
            Assert.AreEqual((double)vA.normalized.z, (double)result.z, 0.001f);
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

        [Test]
        public void DeleteSubSlotWithLink()
        {
            var crossProductDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.variant.name.Contains("Cross"));
            var sinDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.variant.name.Contains("Sine"));
            var cosDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.variant.name.Contains("Cosine"));

            var crossProduct = m_ViewController.AddVFXOperator(new Vector2(0, 0), crossProductDesc.variant);
            var sin = m_ViewController.AddVFXOperator(new Vector2(8, 8), sinDesc.variant);
            var cos = m_ViewController.AddVFXOperator(new Vector2(-8, 8), cosDesc.variant);

            m_ViewController.ApplyChanges();

            crossProduct.outputSlots[0].children.ElementAt(1).Link(sin.inputSlots[0]);
            crossProduct.outputSlots[0].children.ElementAt(1).Link(cos.inputSlots[0]);

            var crossController = m_ViewController.allChildren.OfType<VFXOperatorController>().First(o => o.model.name.Contains("Cross"));
            m_ViewController.RemoveElement(crossController);

            Assert.IsFalse(cos.inputSlots[0].HasLink(true));
            Assert.IsFalse(sin.inputSlots[0].HasLink(true));
        }

        [Test]
        public void ConvertParameterToInline()
        {
            VFXParameter newParameter = m_ViewController.AddVFXParameter(Vector2.zero, VFXLibrary.GetParameters().First(t => t.modelType == typeof(AABox)).variant);

            m_ViewController.LightApplyChanges();

            VFXParameterController parameterController = m_ViewController.GetParameterController(newParameter);

            parameterController.model.AddNode(new Vector2(123, 456));

            AABox value = new AABox { center = new Vector3(1, 2, 3), size = new Vector3(4, 5, 6) };

            parameterController.value = value;

            m_ViewController.LightApplyChanges();

            VFXParameterNodeController parameterNode = parameterController.nodes.First();

            parameterNode.ConvertToInline();

            VFXInlineOperator op = m_ViewController.graph.children.OfType<VFXInlineOperator>().First();

            Assert.AreEqual(new Vector2(123, 456), op.position);
            Assert.AreEqual(typeof(AABox), op.type);
            Assert.AreEqual(value, op.inputSlots[0].value);
        }

        [Test]
        public void ConvertInlineToParameter()
        {
            var op = ScriptableObject.CreateInstance<VFXInlineOperator>();
            m_ViewController.graph.AddChild(op);
            op.SetSettingValue("m_Type", (SerializableType)typeof(AABox));
            op.position = new Vector2(123, 456);
            AABox value = new AABox { center = new Vector3(1, 2, 3), size = new Vector3(4, 5, 6) };

            op.inputSlots[0].value = value;

            m_ViewController.LightApplyChanges();

            var nodeController = m_ViewController.GetNodeController(op, 0) as VFXOperatorController;

            nodeController.ConvertToProperty();

            VFXParameter param = m_ViewController.graph.children.OfType<VFXParameter>().First();

            Assert.AreEqual(new Vector2(123, 456), param.nodes[0].position);
            Assert.AreEqual(typeof(AABox), param.type);
            Assert.AreEqual(value, param.value);
        }

        [Test]
        public void Convert_Output_Quad_To_ShaderGraph_Triangle()
        {
            testAssetRandomFileName = $"Assets/TmpTests/random_{Guid.NewGuid()}.vfx";
            var templateString = File.ReadAllText(VFXTestCommon.simpleParticleSystemPath);
            File.WriteAllText(testAssetRandomFileName, templateString);
            AssetDatabase.ImportAsset(testAssetRandomFileName);

            var window = VFXViewWindow.GetWindow<VFXViewWindow>();
            window.LoadAsset(AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(testAssetRandomFileName), null);

            var viewController = VFXViewController.GetController(window.displayedResource);
            var test = viewController.allChildren.ToArray();

            var outputContextController = viewController.allChildren.OfType<VFXContextController>().Single(o => o.model.contextType == VFXContextType.Output);
            var outputContextUI = window.graphView.Query().OfType<VFXContextUI>().Where(o => o.controller == outputContextController).ToList().Single();

            var originalSetting = outputContextController.model.GetSetting("primitiveType");
            Assert.IsTrue(originalSetting.valid);
            Assert.AreEqual(originalSetting.value, VFXPrimitiveType.Quad);

            Assert.IsNotNull(outputContextUI);

            var variantProvider = new VFXTopologySubVariantProvider();
            var triangleVariant = variantProvider.GetVariants().Single(o => o.name.Contains("Triangle"));

            var fnConvertContext = outputContextUI.GetType().GetMethod("ConvertContext", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(fnConvertContext);
            fnConvertContext.Invoke(outputContextUI, new object[] {triangleVariant, Vector2.zero});
            viewController.ApplyChanges();

            outputContextController = viewController.allChildren.OfType<VFXContextController>().Single(o => o.model.contextType == VFXContextType.Output);
            Assert.IsInstanceOf<VFXComposedParticleOutput>(outputContextController.model);
            var topology = outputContextController.model.GetSetting("m_Topology");
            Assert.IsTrue(topology.valid);
            Assert.IsInstanceOf<ParticleTopologyPlanarPrimitive>(topology.value);

            var planarTopology = topology.value as ParticleTopologyPlanarPrimitive;
            Assert.IsNotNull(planarTopology);
            var primitiveTypeFieldAccess = planarTopology.GetType().GetField("primitiveType", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(primitiveTypeFieldAccess);

            Assert.AreEqual(VFXPrimitiveType.Triangle, primitiveTypeFieldAccess.GetValue(planarTopology));
        }

        [Test]
        public void Avoid_Loop_In_Flow_Input()
        {
            var spawner_A = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var spawner_B = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var spawner_C = ScriptableObject.CreateInstance<VFXBasicSpawner>();

            spawner_B.LinkFrom(spawner_A);
            spawner_C.LinkFrom(spawner_B);

            m_ViewController.graph.AddChild(spawner_A);
            m_ViewController.graph.AddChild(spawner_B);
            m_ViewController.graph.AddChild(spawner_C);

            m_ViewController.LightApplyChanges();

            var flowAnchorController = m_ViewController.allChildren.OfType<VFXContextController>().SelectMany(o => o.flowInputAnchors.Concat(o.flowOutputAnchors));
            var outputControllers = flowAnchorController.Where(o => o.owner == spawner_C && o.direction == Experimental.GraphView.Direction.Output).ToArray();
            Assert.AreEqual(1, outputControllers.Length);

            var compatiblePorts = m_ViewController.GetCompatiblePorts(outputControllers[0], null);
            Assert.AreEqual(0, compatiblePorts.Count);
        }

        [Test]
        public void UniqueDefaultSystemNames()
        {
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            VFXView view = window.graphView;
            view.controller = m_ViewController;

            const int count = 16;
            var spawners = VFXTestCommon.CreateSpawners(view, m_ViewController, count);

            var systemNames = view.controller.graph.systemNames;
            var names = new List<string>();
            foreach (var spawner in spawners)
                names.Add(systemNames.GetUniqueSystemName(spawner.GetData()));

            Assert.IsTrue(names.Where(name => !string.IsNullOrEmpty(name)).Distinct().Count() == count, "Some spawners have the same name or are null or empty.");

            var GPUSystems = VFXTestCommon.GetFieldValue<VFXView, List<VFXSystemBorder>>(view, "m_Systems");
            VFXTestCommon.CreateSystems(view, m_ViewController, count, count);
            var uniqueSystemNames = GPUSystems.Select(system => system.controller.title).Distinct();

            Assert.IsTrue(uniqueSystemNames.Count() == count, "Some GPU systems have the same name or are null or empty.");
        }

        //Regression test for case 1345426
        [UnityTest]
        public IEnumerator ConvertToSubGraphOperator()
        {
            var window = VFXViewWindow.GetWindow<VFXViewWindow>();

            VisualEffectAsset asset = VisualEffectAssetEditorUtility.CreateNewAsset(testAssetMainSubgraph);
            VisualEffectResource resource = asset.GetResource();
            window.LoadAsset(AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(testAssetMainSubgraph), null);

            var viewController = VFXViewController.GetController(VisualEffectResource.GetResourceAtPath(testAssetMainSubgraph));

            var graph = viewController.graph;
            var add_A = ScriptableObject.CreateInstance<Operator.Add>();
            graph.AddChild(add_A);
            var add_B = ScriptableObject.CreateInstance<Operator.Add>();
            graph.AddChild(add_B);
            Assert.IsTrue(add_A.outputSlots[0].Link(add_B.inputSlots[1]));

            //Simple compilable system
            {
                var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();

                var spawnerInit = ScriptableObject.CreateInstance<VFXBasicInitialize>();
                var spawnerOutput = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();

                var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.variant.modelType == typeof(Block.SetAttribute));
                var blockAttribute = blockAttributeDesc.CreateInstance();
                blockAttribute.SetSettingValue("attribute", "position");
                spawnerInit.AddChild(blockAttribute);

                graph.AddChild(spawnerContext);
                graph.AddChild(spawnerInit);
                graph.AddChild(spawnerOutput);

                spawnerInit.LinkFrom(spawnerContext);
                spawnerOutput.LinkFrom(spawnerInit);
            }

            var initialize = viewController.graph.children.OfType<VFXBasicInitialize>().FirstOrDefault();
            Assert.IsNotNull(initialize);
            Assert.IsTrue(initialize.children.Any());

            var allSlot = initialize.children.SelectMany(o => o.inputSlots);
            var firstVector3 = allSlot.FirstOrDefault(o => o.property.type == typeof(Position));
            Assert.IsNotNull(firstVector3);
            Assert.IsTrue(add_B.outputSlots[0].Link(firstVector3[0][0]));
            Assert.IsTrue(firstVector3.HasLink(true));
            viewController.LightApplyChanges();

            yield return null;

            var controller = window.graphView.Query<VFXOperatorUI>().ToList().Where(t => t.controller.model is Operator.Add).Select(o => o.controller).Cast<Controller>();
            Assert.AreEqual(2, controller.Count());
            VFXConvertSubgraph.ConvertToSubgraphOperator(window.graphView, controller, Rect.zero, testSubgraphSubOperatorAssetName);

            for (int i = 0; i < 32; ++i)
                yield return null;

            //Check the status of the newly integrated subgraph, expecting one output
            var subgraph = viewController.graph.children.OfType<VFXSubgraphOperator>().FirstOrDefault();
            Assert.IsNotNull(subgraph);
            Assert.AreEqual(1, subgraph.outputSlots.Count);
            Assert.IsFalse(subgraph.outputSlots.Any(s => s == null));

            //If we reach here without any error or crash, the bug has been fixed
            window.graphView.controller = null;
        }

        //Extension of previous test: create two outputs in subgraph (instead of one), revert and restore
        [UnityTest, Description("Cover case 1345426")]
        public IEnumerator ConvertToSubGraphOperator_And_ModifySubgraph()
        {
            var previousTest = ConvertToSubGraphOperator();
            while (previousTest.MoveNext())
                yield return previousTest.Current;

            var resource = VisualEffectResource.GetResourceAtPath(testSubgraphSubOperatorAssetName);
            resource.WriteAsset();

            var oneOutputState = File.ReadAllText(testSubgraphSubOperatorAssetName);
            Assert.IsFalse(string.IsNullOrEmpty(oneOutputState));

            resource = VisualEffectResource.GetResourceAtPath(testSubgraphSubOperatorAssetName);
            Assert.IsNotNull(resource);
            var window = VFXViewWindow.GetWindow<VFXViewWindow>();
            window.LoadResource(resource, null);

            var viewController = window.graphView.controller;
            var graph = viewController.graph;
            Assert.IsNotNull(graph);

            var parameter = VFXLibrary.GetParameters().FirstOrDefault(o => o.modelType == typeof(Sphere));
            Assert.IsNotNull(parameter);
            var newParam = viewController.AddVFXParameter(Vector2.zero, parameter.variant);
            newParam.isOutput = true;
            var otherParamName = "programatically_new_name_test";
            newParam.SetSettingValue("m_ExposedName", otherParamName);
            viewController.ApplyChanges();

            resource.WriteAsset();

            yield return null;
            window.Close();

            var twoOutputState = File.ReadAllText(testSubgraphSubOperatorAssetName);
            Assert.IsFalse(string.IsNullOrEmpty(twoOutputState));
            Assert.AreNotEqual(oneOutputState, twoOutputState);
            Assert.IsTrue(twoOutputState.Contains(otherParamName));

            for (int i = 0; i < 16; ++i)
                yield return null;

            //Check the actual status, should have now two slots
            {
                AssetDatabase.ImportAsset(testAssetMainSubgraph);
                var mainAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(testAssetMainSubgraph);
                var mainGraph = mainAsset.GetOrCreateResource().GetOrCreateGraph();

                var subGraph = mainGraph.children.OfType<VFXSubgraphOperator>().FirstOrDefault();
                Assert.IsNotNull(subGraph);
                Assert.AreEqual(2, subGraph.outputSlots.Count);
                Assert.IsFalse(subGraph.outputSlots.Any(o => o == null));
            }
            yield return null;

            //Removing old slots, shouldn't get unexpected removed
            {
                File.WriteAllText(testSubgraphSubOperatorAssetName, oneOutputState);
                AssetDatabase.Refresh();

                AssetDatabase.ImportAsset(testAssetMainSubgraph);
                var mainAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(testAssetMainSubgraph);
                var mainGraph = mainAsset.GetOrCreateResource().GetOrCreateGraph();

                var subGraph = mainGraph.children.OfType<VFXSubgraphOperator>().FirstOrDefault();
                Assert.IsNotNull(subGraph);
                Assert.AreEqual(1, subGraph.outputSlots.Count);
                Assert.IsFalse(subGraph.outputSlots.Any(o => o == null));
            }
            yield return null;

            //Here, restore to the previous state, if one outputSlot is null, then we did a resync slot during compilation
            {
                File.WriteAllText(testSubgraphSubOperatorAssetName, twoOutputState);
                AssetDatabase.Refresh();

                AssetDatabase.ImportAsset(testAssetMainSubgraph);
                var mainAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(testAssetMainSubgraph);
                var mainGraph = mainAsset.GetOrCreateResource().GetOrCreateGraph();

                var subGraph = mainGraph.children.OfType<VFXSubgraphOperator>().FirstOrDefault();
                Assert.IsNotNull(subGraph);
                Assert.AreEqual(2, subGraph.outputSlots.Count);
                Assert.IsFalse(subGraph.outputSlots.Any(o => o == null));
            }
            yield return null;

            //Finally, open the source asset it will crash if the vfxasset is wrongly formed
            resource = VisualEffectResource.GetResourceAtPath(testAssetMainSubgraph);
            window = VFXViewWindow.GetWindow<VFXViewWindow>();
            window.LoadResource(resource, null);

            for (int i = 0; i < 16; ++i)
                yield return null;
        }

        [UnityTest][Description("(Non regression test for Jira case UUM-2272")]
        public IEnumerator ConvertToSubgraphOperator_AfterCopyPaste()
        {
            testAssetRandomFileName = $"Assets/TmpTests/random_{Guid.NewGuid()}.vfx";
            // Create default VFX Graph
            var templateString = File.ReadAllText(VFXTestCommon.simpleParticleSystemPath);
            File.WriteAllText(testAssetRandomFileName, templateString);
            AssetDatabase.ImportAsset(testAssetRandomFileName);

            // Open this vfx the same way it would be done by a user
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(testAssetRandomFileName);
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetInstanceID(), 0));

            var window = VFXViewWindow.GetWindow(asset);
            window.LoadAsset(asset, null); //Should not be needed, without, viewController is null on Yamato. See UUM-11596.
            var viewController = window.graphView.controller;
            Assert.IsNotNull(viewController);
            viewController.ApplyChanges();
            yield return null;
            window.graphView.ExecuteCommand(ExecuteCommandEvent.GetPooled("SelectAll"));
            window.graphView.CopySelectionCallback();
            window.graphView.PasteCallback();
            yield return null;

            var addOperator = ScriptableObject.CreateInstance<Operator.Add>();
            viewController.graph.AddChild(addOperator);
            viewController.ApplyChanges();
            yield return null;

            VFXConvertSubgraph.ConvertToSubgraphOperator(window.graphView, window.graphView.Query<VFXOperatorUI>().ToList().Select(t => t.controller), Rect.zero, testSubgraphSubOperatorAssetName);
            yield return null;

            window.graphView.controller = null;
        }

        [UnityTest]
        public IEnumerator ConvertToSubGraphBlock_Nested()
        {
            string vfxPath;
            {
                var vfxGraph = VFXTestCommon.CreateGraph_And_System();
                vfxPath = AssetDatabase.GetAssetPath(vfxGraph);
                var update = vfxGraph.children.OfType<VFXBasicUpdate>().Single();
                var gravityDesc = VFXLibrary.GetBlocks().First(o => o.modelType == typeof(Gravity));
                var gravity = gravityDesc.CreateInstance();
                update.AddChild(gravity);
                AssetDatabase.ImportAsset(vfxPath);
            }

            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetInstanceID(), 0));

            var window = VFXViewWindow.GetWindow(asset);
            window.LoadAsset(asset, null);
            var viewController = window.graphView.controller;
            Assert.IsNotNull(viewController);

            var firstSubgraphBlockPath = vfxPath + "block";
            var secondSubgraphBlockPath = firstSubgraphBlockPath.Replace(".vfxblock", "_bis.vfxblock");

            {
                var update = viewController.graph.children.OfType<VFXBasicUpdate>().Single();
                var gravityBlock = update.children.OfType<Gravity>().First();
                var controller = viewController.GetNodeController(gravityBlock, 0);
                VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, new[] { controller }, Rect.zero, firstSubgraphBlockPath);
                viewController.ApplyChanges();
            }

            yield return null;

            {
                var update = viewController.graph.children.OfType<VFXBasicUpdate>().Single();
                var subgraphBlock = update.children.OfType<VFXSubgraphBlock>().First();
                var controller = viewController.GetNodeController(subgraphBlock, 0);
                VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, new[] { controller }, Rect.zero, secondSubgraphBlockPath);
                viewController.ApplyChanges();
            }

            yield return null;

            //Basic check on expected shader generation output
            AssetDatabase.ImportAsset(vfxPath);
            var graph = asset.GetOrCreateResource();
            bool foundGravityInSource = false;
            for (int shaderIndex = 0; shaderIndex < graph.GetShaderSourceCount(); ++shaderIndex)
            {
                if (!graph.GetShaderSourceName(shaderIndex).Contains("Update"))
                    continue;

                var source = graph.GetShaderSource(shaderIndex);
                if (source.Contains("Gravity"))
                {
                    foundGravityInSource = true;
                    break;
                }
            }
            Assert.IsTrue(foundGravityInSource);
        }

        [UnityTest][Description("(Non regression test for FB case #1419176")]
        public IEnumerator Rename_Asset_Dont_Lose_Subgraph()
        {
            testAssetRandomFileName = $"Assets/TmpTests/random_{Guid.NewGuid()}.vfx";
            // Create default VFX Graph
            var templateString = File.ReadAllText(VFXTestCommon.simpleParticleSystemPath);
            File.WriteAllText(testAssetRandomFileName, templateString);
            AssetDatabase.ImportAsset(testAssetRandomFileName);

            // Open this vfx the same way it would be done by a user
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(testAssetRandomFileName);
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetInstanceID(), 0));

            var window = VFXViewWindow.GetWindow(asset);
            window.LoadAsset(asset, null); //Should not be needed, without, viewController is null on Yamato. See UUM-11596.
            var viewController = window.graphView.controller;
            Assert.IsNotNull(viewController);

            // Convert the first set attribute block into a subgraph block
            var initializeContext = viewController.graph.children.OfType<VFXBasicInitialize>().Single();
            var setAttributeBlock = initializeContext.children.OfType<SetAttribute>().First();
            var controller = viewController.GetNodeController(setAttributeBlock, 0);
            VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, new [] { controller }, Rect.zero, testSubgraphBlockAssetName);
            viewController.ApplyChanges();

            var subGraphBlock = window.graphView.controller.AllSlotContainerControllers
                .Select(x => x.model)
                .OfType<VFXSubgraphBlock>()
                .Single();
            Assert.IsNotNull(subGraphBlock.subgraph);

            // Rename the asset
            var newFileName = $"zz-random_{Guid.NewGuid()}.vfx";
            var result = AssetDatabase.RenameAsset(testAssetRandomFileName, newFileName);
            Assert.IsEmpty(result);
            testAssetRandomFileName = $"Assets/TmpTests/{newFileName}";

            yield return null;
            yield return null;

            // Check the subgraph is still properly referenced
            subGraphBlock = window.graphView.controller.AllSlotContainerControllers
                .Select(x => x.model)
                .OfType<VFXSubgraphBlock>()
                .Single();
            Assert.IsNotNull(subGraphBlock.subgraph);

            window.Close();

            yield return null;
        }

        [UnityTest, Description("Repro from UUM-39696")]
        public IEnumerator Convert_To_Subgraph_Block_With_Different_Slot_Type()
        {
            var kSourceAsset = "Assets/AllTests/Editor/Tests/VFXSubGraph_Repro_39696.vfx_";
            var graph = VFXTestCommon.CopyTemporaryGraph(kSourceAsset);
            Assert.IsNotNull(graph);
            yield return null;

            var assetPath = AssetDatabase.GetAssetPath(graph);
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetInstanceID(), 0));

            var window = VFXViewWindow.GetWindow(asset);
            window.LoadAsset(asset, null);
            var viewController = window.graphView.controller;
            Assert.IsNotNull(viewController);
            yield return null;

            var initializeContext = viewController.graph.children.OfType<VFXBasicInitialize>().Single();
            var setVelocityBlock = initializeContext.children.OfType<VelocityDirection>().First();

            var controller = viewController.GetNodeController(setVelocityBlock, 0);
            var subgraphPath = $"Assets/TmpTests/subgraph_39696_{Guid.NewGuid()}.vfxblock";

            VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, new[] { controller }, Rect.zero, subgraphPath);
            viewController.ApplyChanges();
            yield return null;

            //Main Graph Content
            initializeContext = viewController.graph.children.OfType<VFXBasicInitialize>().Single();
            setVelocityBlock = initializeContext.children.OfType<VelocityDirection>().FirstOrDefault();
            Assert.IsNull(setVelocityBlock);

            var subgraphBlock = initializeContext.children.OfType<VFXSubgraphBlock>().Single();
            Assert.AreEqual(2u, subgraphBlock.inputSlots.Count);

            Assert.AreEqual(subgraphBlock.inputSlots[0].valueType, VFXValueType.Boolean);
            Assert.AreEqual(subgraphBlock.inputSlots[1].valueType, VFXValueType.Float);

            Assert.IsTrue(subgraphBlock.inputSlots[0].HasLink());
            Assert.IsTrue(subgraphBlock.inputSlots[1].HasLink());

            //Subgraph Content
            var subgraphContent = AssetDatabase.LoadAssetAtPath<VisualEffectSubgraph>(subgraphPath);
            var subgraph = (VFXGraph)subgraphContent.GetOrCreateResource().graph;
            var blockSubgraphContext = subgraph.children.OfType<VFXBlockSubgraphContext>().Single();
            Assert.AreEqual(1u, blockSubgraphContext.children.Count());

            var innerSetVelocityBlock = blockSubgraphContext.children.OfType<VelocityDirection>().First();
            Assert.IsTrue(innerSetVelocityBlock.activationSlot.HasLink());

            foreach (var slot in innerSetVelocityBlock.inputSlots)
            {
                if (slot.name == "MinSpeed")
                    Assert.IsTrue(slot.HasLink());
                else
                    Assert.IsFalse(slot.HasLink(true));
            }

            var parameters = subgraph.children.OfType<VFXParameter>().ToList();
            Assert.AreEqual(2, parameters.Count);

            var enabled = parameters.First(o => o.exposedName == "enabled"); //There is an automatic dodge of reserved name, it shouldn't be _vfx_enabled here.
            var minSpeed = parameters.First(o => o.exposedName == "MinSpeed");

            Assert.IsTrue(enabled.exposed);
            Assert.IsTrue(minSpeed.exposed);

            Assert.AreEqual(typeof(bool), enabled.type);
            Assert.AreEqual(typeof(float), minSpeed.type);

            window.Close();
            yield return null;
        }

        [UnityTest, Description("Repro from UUM-84060")]
        public IEnumerator CustomHLSL_Usage_In_Sample_Water_Unexpected_Dirty()
        {
            string vfxPath = null;

            //Prepare Asset
            {
                var vfxGraph = VFXTestCommon.CreateGraph_And_System();
                vfxPath = AssetDatabase.GetAssetPath(vfxGraph);

                var sampleWaterDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Sample Water Surface");
                Assert.IsNotNull(sampleWaterDesc);
                var sampleWater = sampleWaterDesc.CreateInstance();
                vfxGraph.AddChild(sampleWater);

                var output = vfxGraph.children.OfType<VFXAbstractRenderedOutput>().SingleOrDefault();
                Assert.IsNotNull(output);

                var setPosition = ScriptableObject.CreateInstance<SetAttribute>();
                setPosition.SetSettingValue("attribute", "position");
                output.AddChild(setPosition);
                Assert.IsTrue(sampleWater.outputSlots[0].Link(setPosition.inputSlots[0]));
                AssetDatabase.ImportAsset(vfxPath);
                yield return null;
            }

            //Prepare Controller
            VFXViewWindow window = null;
            {
                var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
                Assert.IsNotNull(asset);
                Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetInstanceID(), 0));

                window = VFXViewWindow.GetWindow(asset);
                window.LoadAsset(asset, null);
                var viewController = window.graphView.controller;
                Assert.IsNotNull(viewController);
                yield return null;
            }

            for (int i = 0; i < 4; i++)
                yield return null;

            //A failure would log "Expression graph was marked as dirty after compiling context for UI. Discard to avoid infinite compilation loop." is logged
            window.Close();
            yield return null;
        }

        [UnityTest, Description("Repro from UUM-113869")]
        public IEnumerator Group_Selection_No_Delete_Empty_Groups()
        {
            //Prepare Asset
            var vfxGraph = VFXTestCommon.CreateGraph_And_System();
            var vfxPath = AssetDatabase.GetAssetPath(vfxGraph);

            AssetDatabase.ImportAsset(vfxPath);
            yield return null;

            //Prepare Controller
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            Assert.IsNotNull(asset);
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetInstanceID(), 0));

            var window = VFXViewWindow.GetWindow(asset);
            window.LoadAsset(asset, null);
            var controller = window.graphView.controller;

            controller.AddStickyNote(Vector2.one * 400, null);
            controller.AddGroupNode(500 * Vector2.right);

            for (int i = 0; i < 4; i++)
                yield return null;

            var stickyNoteController = controller.stickyNotes.Single(); // This will confirm there's only one sticky note
            controller.GroupNodes(Array.Empty<VFXNodeController>(), new[] { stickyNoteController });
            yield return null;

            Assert.AreEqual(2, controller.groupNodes.Count);
        }
    }
}
#endif
