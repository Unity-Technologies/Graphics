using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.UI;
using System.IO;
using UnityEditor.VFX.Block.Test;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXControlllersTests
    {
        VFXViewController m_ViewController;

        const string testAssetName = "Assets/TmpTests/VFXGraph1.asset";

        private int m_StartUndoGroupId;

        [SetUp]
        public void CreateTestAsset()
        {
            VFXAsset asset = new VFXAsset();

            var directoryPath = Path.GetDirectoryName(testAssetName);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            AssetDatabase.CreateAsset(asset, testAssetName);

            m_ViewController = VFXViewController.GetController(asset);
            m_ViewController.useCount++;

            m_StartUndoGroupId = Undo.GetCurrentGroup();
        }

        [TearDown]
        public void DestroyTestAsset()
        {
            m_ViewController.useCount--;
            Undo.RevertAllDownToGroup(m_StartUndoGroupId);
            AssetDatabase.DeleteAsset(testAssetName);
        }

        [Test]
        public void CascadedOperatorAdd()
        {
            Func<IVFXSlotContainer, VFXSlotContainerController> fnFindControlller = delegate(IVFXSlotContainer slotContainer)
                {
                    var allControlller = m_ViewController.allChildren.OfType<VFXSlotContainerController>();
                    return allControlller.FirstOrDefault(o => o.slotContainer == slotContainer);
                };

            var vector2Desc = VFXLibrary.GetParameters().FirstOrDefault(o => o.name == "Vector2");
            var vector2 = m_ViewController.AddVFXParameter(new Vector2(-100, -100), vector2Desc);

            var addDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Add");
            var add = m_ViewController.AddVFXOperator(new Vector2(100, 100), addDesc);

            var absDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Absolute");
            var abs = m_ViewController.AddVFXOperator(new Vector2(100, 100), absDesc);

            m_ViewController.ApplyChanges();

            var absControlller = fnFindControlller(abs);
            var addControlller = fnFindControlller(add);
            var edgeControlller = new VFXDataEdgeController(absControlller.inputPorts.First(), addControlller.outputPorts.First());
            m_ViewController.AddElement(edgeControlller);
            Assert.AreEqual(VFXValueType.kFloat, abs.outputSlots[0].GetExpression().valueType);

            var vector2Controlller = fnFindControlller(vector2);
            for (int i = 0; i < 4; ++i)
            {
                edgeControlller = new VFXDataEdgeController(addControlller.inputPorts.First(), vector2Controlller.outputPorts.First());
                m_ViewController.AddElement(edgeControlller);
            }

            Assert.AreEqual(VFXValueType.kFloat2, add.outputSlots[0].GetExpression().valueType);
            Assert.AreEqual(VFXValueType.kFloat2, abs.outputSlots[0].GetExpression().valueType);

            m_ViewController.RemoveElement(addControlller);
            Assert.AreEqual(VFXValueType.kFloat, abs.outputSlots[0].GetExpression().valueType);
        }

        [Test]
        public void AppendOperator()
        {
            Action fnResync = delegate()
                {
                    m_ViewController.ForceReload();
                };

            Func<IVFXSlotContainer, VFXSlotContainerController> fnFindControlller = delegate(IVFXSlotContainer slotContainer)
                {
                    var allControlller = m_ViewController.allChildren.OfType<VFXSlotContainerController>();
                    return allControlller.FirstOrDefault(o => o.slotContainer == slotContainer);
                };

            var absDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Absolute");
            var abs = m_ViewController.AddVFXOperator(new Vector2(100, 100), absDesc); fnResync();

            var cosDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Cosine");
            var cos = m_ViewController.AddVFXOperator(new Vector2(200, 100), cosDesc); fnResync();

            var appendDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "AppendVector");
            var append = m_ViewController.AddVFXOperator(new Vector2(300, 100), appendDesc); fnResync();

            var edgeControlllerCos = new VFXDataEdgeController(fnFindControlller(cos).inputPorts.First(), fnFindControlller(append).outputPorts.First());
            m_ViewController.AddElement(edgeControlllerCos); fnResync();

            var edgeControlllerAppend_A = new VFXDataEdgeController(fnFindControlller(append).inputPorts.First(), fnFindControlller(abs).outputPorts.First());
            m_ViewController.AddElement(edgeControlllerAppend_A); fnResync();

            var edgeControlllerAppend_B = new VFXDataEdgeController(fnFindControlller(append).inputPorts[1], fnFindControlller(abs).outputPorts.First());
            m_ViewController.AddElement(edgeControlllerAppend_B); fnResync();

            var edgeControlllerAppend_C = new VFXDataEdgeController(fnFindControlller(append).inputPorts[2], fnFindControlller(abs).outputPorts.First());
            m_ViewController.AddElement(edgeControlllerAppend_C); fnResync();

            var edgeControlllerAppend_D = new VFXDataEdgeController(fnFindControlller(append).inputPorts[3], fnFindControlller(abs).outputPorts.First());
            m_ViewController.AddElement(edgeControlllerAppend_D); fnResync();
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

            var totalSlotCount = cross.inputSlots.Concat(cross.outputSlots).Count();
            for (int step = 1; step < totalSlotCount; step++)
            {
                Undo.PerformUndo();
                var vfxOperatorControlller = m_ViewController.allChildren.OfType<VFXOperatorController>().FirstOrDefault();
                Assert.IsNotNull(vfxOperatorControlller);

                var slots = vfxOperatorControlller.Operator.inputSlots.Concat(vfxOperatorControlller.Operator.outputSlots).Reverse();
                for (int i = 0; i < totalSlotCount; ++i)
                {
                    var slot = slots.ElementAt(i);
                    Assert.AreEqual(i < step, slot.collapsed);
                }
            }

            for (int step = 1; step < totalSlotCount; step++)
            {
                Undo.PerformRedo();
                var vfxOperatorControlller = m_ViewController.allChildren.OfType<VFXOperatorController>().FirstOrDefault();
                Assert.IsNotNull(vfxOperatorControlller);

                var slots = vfxOperatorControlller.Operator.inputSlots.Concat(vfxOperatorControlller.Operator.outputSlots);
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

            Func<Type, VFXSlotContainerController> fnFindControlller = delegate(Type type)
                {
                    m_ViewController.ApplyChanges();
                    var allControlller = m_ViewController.allChildren.OfType<VFXSlotContainerController>();
                    return allControlller.FirstOrDefault(o => type.IsInstanceOfType(o.slotContainer));
                };

            for (int i = 0; i < positions.Length; ++i)
            {
                var currentAbs = fnFindControlller(typeof(VFXOperatorAbsolute));
                Assert.IsNotNull(currentAbs);
                Assert.AreEqual(positions[positions.Length - i - 1].x, currentAbs.model.position.x);
                Assert.AreEqual(positions[positions.Length - i - 1].y, currentAbs.model.position.y);
                Undo.PerformUndo();
            }

            for (int i = 0; i < positions.Length; ++i)
            {
                Undo.PerformRedo();
                var currentAbs = fnFindControlller(typeof(VFXOperatorAbsolute));
                Assert.IsNotNull(currentAbs);
                Assert.AreEqual(positions[i].x, currentAbs.model.position.x);
                Assert.AreEqual(positions[i].y, currentAbs.model.position.y);
            }
        }

        [Test]
        public void UndoRedoAddOperator()
        {
            Func<VFXSlotContainerController[]> fnAllOperatorControlller = delegate()
                {
                    m_ViewController.ApplyChanges();
                    var allControlller = m_ViewController.allChildren.OfType<VFXSlotContainerController>();
                    return allControlller.OfType<VFXOperatorController>().ToArray();
                };


            Action fnTestShouldExist = delegate()
                {
                    var allOperatorControlller = fnAllOperatorControlller();
                    Assert.AreEqual(1, allOperatorControlller.Length);
                    Assert.IsInstanceOf(typeof(VFXOperatorAbsolute), allOperatorControlller[0].model);
                };

            Action fnTestShouldNotExist = delegate()
                {
                    var allOperatorControlller = fnAllOperatorControlller();
                    Assert.AreEqual(0, allOperatorControlller.Length);
                };

            Undo.IncrementCurrentGroup();
            var absDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Absolute");
            var abs = m_ViewController.AddVFXOperator(new Vector2(0, 0), absDesc);

            fnTestShouldExist();
            Undo.PerformUndo();
            fnTestShouldNotExist();
            Undo.PerformRedo();
            fnTestShouldExist();

            Undo.IncrementCurrentGroup();
            m_ViewController.RemoveElement(fnAllOperatorControlller()[0]);
            fnTestShouldNotExist();
            Undo.PerformUndo();
            fnTestShouldExist();
            Undo.PerformRedo();
            fnTestShouldNotExist();
        }

        [Test]
        public void UndoRedoOperatorLinkSimple()
        {
            Func<Type, VFXSlotContainerController> fnFindControlller = delegate(Type type)
                {
                    m_ViewController.ApplyChanges();
                    var allControlller = m_ViewController.allChildren.OfType<VFXSlotContainerController>();
                    return allControlller.FirstOrDefault(o => type.IsInstanceOfType(o.slotContainer));
                };

            var cosDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Cosine");
            var sinDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Sine");
            Undo.IncrementCurrentGroup();
            var cos = m_ViewController.AddVFXOperator(new Vector2(0, 0), cosDesc);
            Undo.IncrementCurrentGroup();
            var sin = m_ViewController.AddVFXOperator(new Vector2(1, 1), sinDesc);
            var cosControlller = fnFindControlller(typeof(VFXOperatorCosine));
            var sinControlller = fnFindControlller(typeof(VFXOperatorSine));

            Func<int> fnCountEdge = delegate()
                {
                    return m_ViewController.allChildren.OfType<VFXDataEdgeController>().Count();
                };

            Undo.IncrementCurrentGroup();
            Assert.AreEqual(0, fnCountEdge());

            var edgeControlllerSin = new VFXDataEdgeController(sinControlller.inputPorts[0], cosControlller.outputPorts[0]);
            m_ViewController.AddElement(edgeControlllerSin);
            Assert.AreEqual(1, fnCountEdge());

            Undo.PerformUndo();
            Assert.AreEqual(0, fnCountEdge());
            Assert.NotNull(fnFindControlller(typeof(VFXOperatorCosine)));
            Assert.NotNull(fnFindControlller(typeof(VFXOperatorSine)));
        }

        [Test]
        public void UndoRedoOperatorLinkToBlock()
        {
            Func<VFXContextController> fnFirstContextControlller = delegate()
                {
                    m_ViewController.ApplyChanges();
                    return m_ViewController.allChildren.OfType<VFXContextController>().FirstOrDefault();
                };

            Func<Type, VFXSlotContainerController> fnFindControlller = delegate(Type type)
                {
                    m_ViewController.ApplyChanges();
                    var allControlller = m_ViewController.allChildren.OfType<VFXSlotContainerController>();
                    return allControlller.FirstOrDefault(o => type.IsInstanceOfType(o.slotContainer));
                };

            Func<VFXBlockController> fnFirstBlockControlller = delegate()
                {
                    m_ViewController.ApplyChanges();
                    return m_ViewController.allChildren.OfType<VFXContextController>().SelectMany(t => t.blockControllers).FirstOrDefault();
                };

            Func<VFXDataEdgeController> fnFirstEdgeControlller = delegate()
                {
                    m_ViewController.ApplyChanges();
                    return m_ViewController.allChildren.OfType<VFXDataEdgeController>().FirstOrDefault();
                };

            Undo.IncrementCurrentGroup();
            var cosDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Cosine");
            var contextUpdateDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Update"));
            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.modelType == typeof(Block.SetAttribute));

            var cos = m_ViewController.AddVFXOperator(new Vector2(0, 0), cosDesc);
            var update = m_ViewController.AddVFXContext(new Vector2(2, 2), contextUpdateDesc);
            var blockAttribute = blockAttributeDesc.CreateInstance();
            blockAttribute.SetSettingValue("attribute", "color");
            fnFirstContextControlller().AddBlock(0, blockAttribute);

            var edgeControlller = new VFXDataEdgeController(fnFirstBlockControlller().inputPorts[0], fnFindControlller(typeof(VFXOperatorCosine)).outputPorts[0]);
            m_ViewController.AddElement(edgeControlller);
            Undo.IncrementCurrentGroup();

            m_ViewController.RemoveElement(fnFirstEdgeControlller());
            Assert.IsNull(fnFirstEdgeControlller());
            Undo.IncrementCurrentGroup();

            Undo.PerformUndo();
            Assert.IsNotNull(fnFirstEdgeControlller());

            Undo.PerformRedo();
            Assert.IsNull(fnFirstEdgeControlller());
        }

        [Test]
        public void UndoRedoOperatorLinkAdvanced()
        {
            Func<Type, VFXSlotContainerController> fnFindControlller = delegate(Type type)
                {
                    m_ViewController.ApplyChanges();
                    var allControlller = m_ViewController.allChildren.OfType<VFXSlotContainerController>();
                    return allControlller.FirstOrDefault(o => type.IsInstanceOfType(o.slotContainer));
                };

            Func<int> fnCountEdge = delegate()
                {
                    m_ViewController.ApplyChanges();
                    return m_ViewController.allChildren.OfType<VFXDataEdgeController>().Count();
                };


            var absDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Absolute");
            var appendDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "AppendVector");
            var crossDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Cross Product");
            var cosDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Cosine");
            var sinDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Sine");

            var abs = m_ViewController.AddVFXOperator(new Vector2(0, 0), absDesc);
            var append = m_ViewController.AddVFXOperator(new Vector2(1, 1), appendDesc);
            var cross = m_ViewController.AddVFXOperator(new Vector2(2, 2), crossDesc);
            var cos = m_ViewController.AddVFXOperator(new Vector2(3, 3), cosDesc);
            var sin = m_ViewController.AddVFXOperator(new Vector2(4, 4), sinDesc);

            var absControlller = fnFindControlller(typeof(VFXOperatorAbsolute));
            var appendControlller = fnFindControlller(typeof(VFXOperatorAppendVector));
            var crossControlller = fnFindControlller(typeof(VFXOperatorCrossProduct));

            for (int i = 0; i < 3; ++i)
            {
                var edgeControlller = new VFXDataEdgeController(appendControlller.inputPorts[i], absControlller.outputPorts[0]);
                m_ViewController.AddElement(edgeControlller);
                m_ViewController.ApplyChanges();
            }

            var edgeControlllerCross = new VFXDataEdgeController(crossControlller.inputPorts[0], appendControlller.outputPorts[0]);
            m_ViewController.AddElement(edgeControlllerCross);

            Undo.IncrementCurrentGroup();
            Assert.AreEqual(4, fnCountEdge());
            Assert.IsInstanceOf(typeof(VFXSlotFloat3), (appendControlller.outputPorts[0] as VFXDataAnchorController).model);

            //Find last edge in append node
            var referenceAnchor = appendControlller.inputPorts[2];
            m_ViewController.ApplyChanges();
            var edgeToDelete = m_ViewController.allChildren
                .OfType<VFXDataEdgeController>()
                .Cast<VFXDataEdgeController>()
                .FirstOrDefault(e =>
                {
                    return e.input == referenceAnchor;
                });
            Assert.NotNull(edgeToDelete);

            m_ViewController.RemoveElement(edgeToDelete);
            Assert.AreEqual(2, fnCountEdge()); //cross should be implicitly disconnected ...
            Assert.IsInstanceOf(typeof(VFXSlotFloat2), (appendControlller.outputPorts[0] as VFXDataAnchorController).model);

            Undo.PerformUndo();
            Assert.AreEqual(4, fnCountEdge()); //... and restored !
            Assert.IsInstanceOf(typeof(VFXSlotFloat3), (fnFindControlller(typeof(VFXOperatorAppendVector)).outputPorts[0] as VFXDataAnchorController).model);
            Undo.PerformRedo();
            Assert.AreEqual(2, fnCountEdge());
            Assert.IsInstanceOf(typeof(VFXSlotFloat2), (fnFindControlller(typeof(VFXOperatorAppendVector)).outputPorts[0] as VFXDataAnchorController).model);

            //Improve test connecting cos & sin => then try delete append
            Undo.PerformUndo();
            Undo.IncrementCurrentGroup();
            Assert.AreEqual(4, fnCountEdge());
            Assert.IsInstanceOf(typeof(VFXSlotFloat3), (fnFindControlller(typeof(VFXOperatorAppendVector)).outputPorts[0] as VFXDataAnchorController).model);

            var edgeControlllerCos = new VFXDataEdgeController(fnFindControlller(typeof(VFXOperatorCosine)).inputPorts[0], fnFindControlller(typeof(VFXOperatorAppendVector)).outputPorts[0]);
            m_ViewController.AddElement(edgeControlllerCos);
            Assert.AreEqual(5, fnCountEdge());

            var edgeControlllerSin = new VFXDataEdgeController(fnFindControlller(typeof(VFXOperatorSine)).inputPorts[0], fnFindControlller(typeof(VFXOperatorAppendVector)).outputPorts[0]);
            m_ViewController.AddElement(edgeControlllerSin);
            Assert.AreEqual(6, fnCountEdge());

            m_ViewController.RemoveElement(fnFindControlller(typeof(VFXOperatorAppendVector)));
            Assert.AreEqual(0, fnCountEdge());
        }

        [Test]
        public void UndoRedoOperatorSettings()
        {
            Func<VFXOperatorController> fnFirstOperatorControlller = delegate()
                {
                    m_ViewController.ApplyChanges();
                    return m_ViewController.allChildren.OfType<VFXOperatorController>().FirstOrDefault();
                };

            Action<VFXOperatorComponentMask, string> fnSetSetting = delegate(VFXOperatorComponentMask target, string mask)
                {
                    target.x = target.y = target.z = target.w = VFXOperatorComponentMask.Component.None;
                    for (int i = 0; i < mask.Length; ++i)
                    {
                        var current = (VFXOperatorComponentMask.Component)Enum.Parse(typeof(VFXOperatorComponentMask.Component),  mask[i].ToString().ToUpper());
                        if (i == 0)
                        {
                            target.x = current;
                        }
                        else if (i == 1)
                        {
                            target.y = current;
                        }
                        else if (i == 2)
                        {
                            target.z = current;
                        }
                        else if (i == 3)
                        {
                            target.w = current;
                        }
                    }
                    target.Invalidate(VFXModel.InvalidationCause.kSettingChanged);
                };

            Func<VFXOperatorComponentMask, string> fnGetSetting = delegate(VFXOperatorComponentMask target)
                {
                    var value = "";
                    if (target.x != VFXOperatorComponentMask.Component.None)
                        value += target.x.ToString().ToLower();
                    if (target.y != VFXOperatorComponentMask.Component.None)
                        value += target.y.ToString().ToLower();
                    if (target.z != VFXOperatorComponentMask.Component.None)
                        value += target.z.ToString().ToLower();
                    if (target.w != VFXOperatorComponentMask.Component.None)
                        value += target.w.ToString().ToLower();
                    return value;
                };

            var componentMaskDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "ComponentMask");
            var componentMask = m_ViewController.AddVFXOperator(new Vector2(0, 0), componentMaskDesc);

            var maskList = new string[] { "xy", "yww", "xw", "z" };
            for (int i = 0; i < maskList.Length; ++i)
            {
                var componentMaskControlller = fnFirstOperatorControlller();
                Undo.IncrementCurrentGroup();
                fnSetSetting(componentMaskControlller.model as VFXOperatorComponentMask, maskList[i]);
                Assert.AreEqual(maskList[i], fnGetSetting(componentMaskControlller.model as VFXOperatorComponentMask));
            }

            for (int i = maskList.Length - 1; i > 0; --i)
            {
                Undo.PerformUndo();
                var componentMaskControlller = fnFirstOperatorControlller();
                Assert.AreEqual(maskList[i - 1], fnGetSetting(componentMaskControlller.model as VFXOperatorComponentMask));
            }

            for (int i = 0; i < maskList.Length - 1; ++i)
            {
                Undo.PerformRedo();
                var componentMaskControlller = fnFirstOperatorControlller();
                Assert.AreEqual(maskList[i + 1], fnGetSetting(componentMaskControlller.model as VFXOperatorComponentMask));
            }
        }

        [Test]
        public void UndoRedoAddBlockContext()
        {
            var contextUpdateDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Update"));
            var blockDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.modelType == typeof(AllType));

            var contextUpdate = m_ViewController.AddVFXContext(Vector2.one, contextUpdateDesc);
            Func<VFXContextController> fnContextControlller = delegate()
                {
                    m_ViewController.ApplyChanges();
                    var allContextControlller = m_ViewController.allChildren.OfType<VFXContextController>().ToArray();
                    return allContextControlller.FirstOrDefault() as VFXContextController;
                };
            Assert.IsNotNull(fnContextControlller());
            //Creation
            Undo.IncrementCurrentGroup();
            fnContextControlller().AddBlock(0, blockDesc.CreateInstance());
            Assert.AreEqual(1, fnContextControlller().context.children.Count());
            Undo.PerformUndo();
            Assert.AreEqual(0, fnContextControlller().context.children.Count());

            //Deletion
            var block = blockDesc.CreateInstance();
            fnContextControlller().AddBlock(0, block);
            Assert.AreEqual(1, fnContextControlller().context.children.Count());
            Undo.IncrementCurrentGroup();
            fnContextControlller().RemoveBlock(block);
            Assert.AreEqual(0, fnContextControlller().context.children.Count());

            Undo.PerformUndo();
            Assert.IsNotNull(fnContextControlller());
            Assert.AreEqual(1, fnContextControlller().context.children.Count());
            Assert.IsInstanceOf(typeof(AllType), fnContextControlller().context.children.First());
        }

        [Test]
        public void UndoRedoContext()
        {
            Func<VFXContextController> fnFirstContextControlller = delegate()
                {
                    m_ViewController.ApplyChanges();
                    return m_ViewController.allChildren.OfType<VFXContextController>().FirstOrDefault() as VFXContextController;
                };

            var contextDesc = VFXLibrary.GetContexts().FirstOrDefault();
            Undo.IncrementCurrentGroup();
            m_ViewController.AddVFXContext(Vector2.zero, contextDesc);

            Assert.NotNull(fnFirstContextControlller());
            Undo.PerformUndo();
            Assert.Null(fnFirstContextControlller(), "Fail Undo Create");

            Undo.IncrementCurrentGroup();
            m_ViewController.AddVFXContext(Vector2.zero, contextDesc);
            Assert.NotNull(fnFirstContextControlller());

            Undo.IncrementCurrentGroup();
            m_ViewController.RemoveElement(fnFirstContextControlller());
            Assert.Null(fnFirstContextControlller());

            Undo.PerformUndo();
            Assert.NotNull(fnFirstContextControlller(), "Fail Undo Delete");
        }

        [Test]
        public void UndoRedoContextLinkMultiSlot()
        {
            Func<VFXContextController[]> fnContextControlller = delegate()
                {
                    m_ViewController.ApplyChanges();
                    return m_ViewController.allChildren.OfType<VFXContextController>().Cast<VFXContextController>().ToArray();
                };

            Func<VFXContextController> fnSpawner = delegate()
                {
                    var controller = fnContextControlller();
                    return controller.FirstOrDefault(o => o.model.name.Contains("Spawner"));
                };

            Func<string, VFXContextController> fnEvent = delegate(string name)
                {
                    var controller = fnContextControlller();
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

            var contextSpawner = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Spawner"));
            var contextEvent = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Event"));

            m_ViewController.AddVFXContext(new Vector2(1, 1), contextSpawner);
            var eventStartControlller = m_ViewController.AddVFXContext(new Vector2(2, 2), contextEvent) as VFXBasicEvent;
            var eventStopControlller = m_ViewController.AddVFXContext(new Vector2(3, 3), contextEvent) as VFXBasicEvent;
            eventStartControlller.SetSettingValue("eventName", "Start");
            eventStopControlller.SetSettingValue("eventName", "Stop");

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
            Func<VFXContextController[]> fnContextControlller = delegate()
                {
                    m_ViewController.ApplyChanges();
                    return m_ViewController.allChildren.OfType<VFXContextController>().Cast<VFXContextController>().ToArray();
                };

            Func<VFXContextController> fnInitializeControlller = delegate()
                {
                    var controller = fnContextControlller();
                    return controller.FirstOrDefault(o => o.model.name.Contains("Init"));
                };

            Func<VFXContextController> fnUpdateControlller = delegate()
                {
                    var controller = fnContextControlller();
                    return controller.FirstOrDefault(o => o.model.name.Contains("Update"));
                };

            Func<int> fnFlowEdgeCount = delegate()
                {
                    m_ViewController.ApplyChanges();
                    return m_ViewController.allChildren.OfType<VFXFlowEdgeController>().Count();
                };

            var contextInitializeDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Init"));
            var contextUpdateDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Update"));

            var contextInitialize = m_ViewController.AddVFXContext(new Vector2(1, 1), contextInitializeDesc);
            var contextUpdate = m_ViewController.AddVFXContext(new Vector2(2, 2), contextUpdateDesc);

            //Creation
            var flowEdge = new VFXFlowEdgeController(fnUpdateControlller().flowInputAnchors.FirstOrDefault(), fnInitializeControlller().flowOutputAnchors.FirstOrDefault());

            Undo.IncrementCurrentGroup();
            m_ViewController.AddElement(flowEdge);
            Assert.AreEqual(1, fnFlowEdgeCount());

            Undo.PerformUndo();
            Assert.AreEqual(0, fnFlowEdgeCount(), "Fail undo Create");

            //Deletion
            flowEdge = new VFXFlowEdgeController(fnUpdateControlller().flowInputAnchors.FirstOrDefault(), fnInitializeControlller().flowOutputAnchors.FirstOrDefault());
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

            var crossControlller = m_ViewController.allChildren.OfType<VFXOperatorController>().First(o => o.model.name.Contains("Cross"));
            m_ViewController.RemoveElement(crossControlller);

            Assert.IsFalse(cos.inputSlots[0].HasLink(true));
            Assert.IsFalse(sin.inputSlots[0].HasLink(true));
        }
    }
}
