#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.UI;
using System.IO;
using UnityEditor.VFX.Block.Test;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXControllersTests
    {
        VFXViewController m_ViewController;

        const string testAssetName = "Assets/TmpTests/VFXGraph1.vfx";
        const string testSubgraphAssetName = "Assets/TmpTests/VFXGraphSub.vfx";
        const string testSubgraphSubAssetName = "Assets/TmpTests/VFXGraphSub_Subgraph.vfx";

        private int m_StartUndoGroupId;

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

        [TearDown]
        public void DestroyTestAsset()
        {
            m_ViewController.useCount--;
            Undo.RevertAllDownToGroup(m_StartUndoGroupId);
            AssetDatabase.DeleteAsset(testAssetName);
            AssetDatabase.DeleteAsset(testAssetName);
        }

        #pragma warning disable 0414
        static private bool[] usePosition = { true, false };

        #pragma warning restore 0414
        [Test]
        public void LinkPositionOrVectorAndDirection([ValueSource("usePosition")] bool usePosition)
        {
            var crossDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name.Contains("Cross"));
            var positionDesc = VFXLibrary.GetParameters().FirstOrDefault(o => o.name.Contains("Position"));
            var vectorDesc = VFXLibrary.GetParameters().FirstOrDefault(o => o.name == "Vector");
            var directionDesc = VFXLibrary.GetParameters().FirstOrDefault(o => o.name.Contains("Direction"));

            var cross = m_ViewController.AddVFXOperator(new Vector2(1, 1), crossDesc);
            var position = m_ViewController.AddVFXParameter(new Vector2(2, 2), positionDesc);
            var vector = m_ViewController.AddVFXParameter(new Vector2(3, 3), vectorDesc);
            var direction = m_ViewController.AddVFXParameter(new Vector2(4, 4), directionDesc);
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
            var directionDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.model is VFXInlineOperator && (o.model as VFXInlineOperator).type == typeof(DirectionType));
            var vector3Desc = VFXLibrary.GetOperators().FirstOrDefault(o => o.model is VFXInlineOperator && (o.model as VFXInlineOperator).type == typeof(Vector3));

            var direction = m_ViewController.AddVFXOperator(new Vector2(1, 1), directionDesc);
            var vector3 = m_ViewController.AddVFXOperator(new Vector2(2, 2), vector3Desc);
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
            var crossDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name.Contains("Cross"));
            var cross = m_ViewController.AddVFXOperator(new Vector2(0, 0), crossDesc);

            foreach (var slot in cross.inputSlots.Concat(cross.outputSlots))
            {
                Undo.IncrementCurrentGroup();
                Assert.IsTrue(slot.collapsed);
                slot.collapsed = false;
            }

            m_ViewController.ApplyChanges();

            var totalSlotCount = cross.inputSlots.Concat(cross.outputSlots).Count();
            for (int step = 1; step < totalSlotCount; step++)
            {
                Undo.PerformUndo();
                var vfxOperatorController = m_ViewController.allChildren.OfType<VFXOperatorController>().FirstOrDefault();
                Assert.IsNotNull(vfxOperatorController);

                var slots = vfxOperatorController.model.inputSlots.Concat(vfxOperatorController.model.outputSlots).Reverse();
                for (int i = 0; i < totalSlotCount; ++i)
                {
                    var slot = slots.ElementAt(i);
                    Assert.AreEqual(i < step, slot.collapsed);
                }
            }

            for (int step = 1; step < totalSlotCount; step++)
            {
                Undo.PerformRedo();
                var vfxOperatorController = m_ViewController.allChildren.OfType<VFXOperatorController>().FirstOrDefault();
                Assert.IsNotNull(vfxOperatorController);

                var slots = vfxOperatorController.model.inputSlots.Concat(vfxOperatorController.model.outputSlots);
                for (int i = 0; i < totalSlotCount; ++i)
                {
                    var slot = slots.ElementAt(i);
                    Assert.AreEqual(i > step, slot.collapsed);
                }
            }
        }

        [Test]
        public void UndoRedoMoveOperator()
        {
            Undo.IncrementCurrentGroup();
            var absDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Absolute");
            var abs = m_ViewController.AddVFXOperator(new Vector2(0, 0), absDesc);

            var positions = new[] { new Vector2(1, 1), new Vector2(2, 2), new Vector2(3, 3), new Vector2(4, 4) };
            foreach (var position in positions)
            {
                Undo.IncrementCurrentGroup();
                abs.position = position;
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
            m_ViewController.AddVFXOperator(new Vector2(0, 0), absDesc);

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
            m_ViewController.AddVFXOperator(new Vector2(0, 0), absDesc);

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
            var inlineOperatorDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.modelType == typeof(VFXInlineOperator));
            var inlineOperator = m_ViewController.AddVFXOperator(new Vector2(0, 0), inlineOperatorDesc);

            m_ViewController.ApplyChanges();
            var allController = m_ViewController.allChildren.OfType<VFXNodeController>().ToArray();
            var inlineOperatorController = allController.OfType<VFXOperatorController>().FirstOrDefault();
            inlineOperator.SetSettingValue("m_Type", (SerializableType)typeof(Position));

            Assert.AreEqual(inlineOperator.inputSlots[0].space, VFXCoordinateSpace.Local);
            Assert.AreEqual((inlineOperatorController.model as VFXInlineOperator).inputSlots[0].space, VFXCoordinateSpace.Local);
            Assert.AreEqual((inlineOperatorController.model as VFXInlineOperator).inputSlots[0].GetSpaceTransformationType(), SpaceableType.Position);

            Undo.IncrementCurrentGroup();
            inlineOperator.inputSlots[0].space = VFXCoordinateSpace.World;
            Assert.AreEqual((inlineOperatorController.model as VFXInlineOperator).inputSlots[0].space, VFXCoordinateSpace.World);
            Assert.AreEqual((inlineOperatorController.model as VFXInlineOperator).inputSlots[0].GetSpaceTransformationType(), SpaceableType.Position);

            Undo.PerformUndo(); //Should go back to local
            Assert.AreEqual((inlineOperatorController.model as VFXInlineOperator).inputSlots[0].space, VFXCoordinateSpace.Local);
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
            m_ViewController.AddVFXOperator(new Vector2(0, 0), absDesc);

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
            m_ViewController.AddVFXOperator(new Vector2(0, 0), absDesc);

            var absOperator = fnAllOperatorController()[0];

            Undo.IncrementCurrentGroup();
            absOperator.inputPorts[0].value = 0;

            absOperator.position = new Vector2(1, 2);


            Undo.IncrementCurrentGroup();

            absOperator.inputPorts[0].value = 123;
            absOperator.position = new Vector2(123, 456);

            Undo.PerformUndo();

            Assert.AreEqual(123, absOperator.inputPorts[0].value);
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
            m_ViewController.AddVFXOperator(new Vector2(0, 0), cosDesc);
            Undo.IncrementCurrentGroup();
            m_ViewController.AddVFXOperator(new Vector2(1, 1), sinDesc);
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
            var contextUpdateDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Update"));
            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.modelType == typeof(Block.SetAttribute));

            m_ViewController.AddVFXOperator(new Vector2(0, 0), cosDesc);
            m_ViewController.AddVFXContext(new Vector2(2, 2), contextUpdateDesc);
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
            m_ViewController.AddVFXOperator(new Vector2(0, 0), swizzleDesc);

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
            var contextUpdateDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Update"));
            var blockDesc = new VFXModelDescriptor<VFXBlock>(ScriptableObject.CreateInstance<AllType>());

            m_ViewController.AddVFXContext(Vector2.one, contextUpdateDesc);
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
            m_ViewController.AddVFXContext(Vector2.zero, contextDesc);

            Assert.NotNull(fnFirstContextController());
            Undo.PerformUndo();
            Assert.Null(fnFirstContextController(), "Fail Undo Create");

            Undo.IncrementCurrentGroup();
            m_ViewController.AddVFXContext(Vector2.zero, contextDesc);
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

            var contextSpawner = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Spawn"));
            var contextEvent = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Event"));

            m_ViewController.AddVFXContext(new Vector2(1, 1), contextSpawner);
            var eventStartController = m_ViewController.AddVFXContext(new Vector2(2, 2), contextEvent) as VFXBasicEvent;
            var eventStopController = m_ViewController.AddVFXContext(new Vector2(3, 3), contextEvent) as VFXBasicEvent;
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

            var contextInitializeDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Init"));
            var contextUpdateDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Update"));

            m_ViewController.AddVFXContext(new Vector2(1, 1), contextInitializeDesc);
            m_ViewController.AddVFXContext(new Vector2(2, 2), contextUpdateDesc);

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
        public void DeleteSubSlotWithLink()
        {
            var crossProductDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name.Contains("Cross"));
            var sinDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name.Contains("Sin"));
            var cosDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name.Contains("Cos"));

            var crossProduct = m_ViewController.AddVFXOperator(new Vector2(0, 0), crossProductDesc);
            var sin = m_ViewController.AddVFXOperator(new Vector2(8, 8), sinDesc);
            var cos = m_ViewController.AddVFXOperator(new Vector2(-8, 8), cosDesc);

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
            VFXParameter newParameter = m_ViewController.AddVFXParameter(Vector2.zero, VFXLibrary.GetParameters().First(t => t.model.type == typeof(AABox)));

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
            foreach (var system in spawners)
                names.Add(systemNames.GetUniqueSystemName(system));

            Assert.IsTrue(names.Where(name => !string.IsNullOrEmpty(name)).Distinct().Count() == count, "Some spawners have the same name or are null or empty.");

            var GPUSystems = VFXTestCommon.GetFieldValue<VFXView, List<VFXSystemBorder>>(view, "m_Systems");
            VFXTestCommon.CreateSystems(view, m_ViewController, count, count);
            var uniqueSystemNames = GPUSystems.Select(system => system.controller.title).Distinct();

            Assert.IsTrue(uniqueSystemNames.Count() == count, "Some GPU systems have the same name or are null or empty.");
        }

        [Test]
        public void ConvertToSubgraph()
        {
            //Create a new vfx based on the usual template
            var templateString = System.IO.File.ReadAllText(VisualEffectGraphPackageInfo.assetPackagePath + "/Editor/Templates/Simple Particle System.vfx");
            System.IO.File.WriteAllText(testSubgraphAssetName, templateString);

            VFXViewWindow window = VFXViewWindow.GetWindow<VFXViewWindow>();
            window.LoadAsset(AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(testAssetName), null);

            VFXConvertSubgraph.ConvertToSubgraphContext(window.graphView, window.graphView.Query<VFXContextUI>().ToList().Where(t => !(t.controller.model is VFXBasicSpawner)).Select(t => t.controller).Cast<Controller>(), Rect.zero, testSubgraphSubAssetName);

            window.graphView.controller = null;
        }
    }
}
#endif
