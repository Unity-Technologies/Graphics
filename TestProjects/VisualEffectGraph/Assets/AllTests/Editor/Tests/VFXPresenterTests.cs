using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.UI;
using System.IO;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXPresentersTests
    {
        VFXViewPresenter m_ViewPresenter;

        const string testAssetName = "Assets/TmpTests/VFXGraph1.asset";

        void CreateTestAsset()
        {
            VFXAsset asset = new VFXAsset();

            var directoryPath = Path.GetDirectoryName(testAssetName);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            AssetDatabase.CreateAsset(asset, testAssetName);

            m_ViewPresenter = ScriptableObject.CreateInstance<VFXViewPresenter>();
            m_ViewPresenter.SetVFXAsset(asset, false);
        }

        void DestroyTestAsset()
        {
            AssetDatabase.DeleteAsset(testAssetName);
        }

        [Test]
        public void CreateAllInitializeBlocks()
        {
            CreateTestAsset();


            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Initialize").First();

            var newContext = m_ViewPresenter.AddVFXContext(new Vector2(100, 100), initContextDesc);

            Assert.AreEqual(m_ViewPresenter.allChildren.Where(t => t is VFXContextPresenter).Count(), 1);

            var contextPresenter = m_ViewPresenter.allChildren.Where(t => t is VFXContextPresenter).First() as VFXContextPresenter;

            Assert.AreEqual(contextPresenter.model, newContext);

            // Adding every block compatible with an init context
            foreach (var block in VFXLibrary.GetBlocks().Where(t => t.AcceptParent(newContext)))
            {
                var newBlock = block.CreateInstance();
                contextPresenter.AddBlock(0, newBlock);

                Assert.AreEqual(contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).block == newBlock).Count(), 1);

                var blockPresenter = contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).block == newBlock).First() as VFXBlockPresenter;

                Assert.NotNull(blockPresenter);
            }

            DestroyTestAsset();
        }

        [Test]
        public void CreateAllUpdateBlocks()
        {
            CreateTestAsset();

            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Update").First();

            var newContext = m_ViewPresenter.AddVFXContext(new Vector2(100, 100), initContextDesc);

            Assert.AreEqual(m_ViewPresenter.allChildren.Where(t => t is VFXContextPresenter).Count(), 1);

            var contextPresenter = m_ViewPresenter.allChildren.Where(t => t is VFXContextPresenter).First() as VFXContextPresenter;

            Assert.AreEqual(contextPresenter.model, newContext);

            // Adding every block compatible with an init context
            foreach (var block in VFXLibrary.GetBlocks().Where(t => t.AcceptParent(newContext)))
            {
                var newBlock = block.CreateInstance();
                contextPresenter.AddBlock(0, newBlock);

                Assert.AreEqual(contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).block == newBlock).Count(), 1);

                var blockPresenter = contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).block == newBlock).First() as VFXBlockPresenter;

                Assert.NotNull(blockPresenter);
            }

            DestroyTestAsset();
        }

        [Test]
        public void CreateAllOutputBlocks()
        {
            CreateTestAsset();

            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Output").First();

            var newContext = m_ViewPresenter.AddVFXContext(new Vector2(100, 100), initContextDesc);

            Assert.AreEqual(m_ViewPresenter.allChildren.Where(t => t is VFXContextPresenter).Count(), 1);

            var contextPresenter = m_ViewPresenter.allChildren.Where(t => t is VFXContextPresenter).First() as VFXContextPresenter;

            Assert.AreEqual(contextPresenter.model, newContext);

            // Adding every block compatible with an init context
            foreach (var block in VFXLibrary.GetBlocks().Where(t => t.AcceptParent(newContext)))
            {
                var newBlock = block.CreateInstance();
                contextPresenter.AddBlock(0, newBlock);

                Assert.AreEqual(contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).block == newBlock).Count(), 1);

                var blockPresenter = contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).block == newBlock).First() as VFXBlockPresenter;

                Assert.NotNull(blockPresenter);
            }

            DestroyTestAsset();
        }

        [Test]
        public void ExpandRetractAndSetPropertyValue()
        {
            CreateTestAsset();

            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Initialize").First();

            var newContext = m_ViewPresenter.AddVFXContext(new Vector2(100, 100), initContextDesc);

            Assert.AreEqual(m_ViewPresenter.allChildren.Where(t => t is VFXContextPresenter).Count(), 1);

            var contextPresenter = m_ViewPresenter.allChildren.Where(t => t is VFXContextPresenter).First() as VFXContextPresenter;

            Assert.AreEqual(contextPresenter.model, newContext);

            // Adding every block compatible with an init context

            var block = VFXLibrary.GetBlocks().Where(t => t.name == "Test").First();

            var newBlock = block.CreateInstance();
            contextPresenter.AddBlock(0, newBlock);

            Assert.IsTrue(newBlock is VFXAllType);

            Assert.AreEqual(contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).block == newBlock).Count(), 1);

            var blockPresenter = contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).block == newBlock).First() as VFXBlockPresenter;

            Assert.NotNull(blockPresenter);

            Assert.NotZero(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).name == "aVector3").Count());

            VFXSlot slot = blockPresenter.block.inputSlots.First(t => t.name == "aVector3");


            var aVector3Presenter = blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).name == "aVector3").First() as VFXContextDataInputAnchorPresenter;

            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.z").Count(), 1);

            aVector3Presenter.ExpandPath();

            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.z").Count(), 1);


            aVector3Presenter.RetractPath();

            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.z").Count(), 1);


            aVector3Presenter.SetPropertyValue(new Vector3(1.2f, 3.4f, 5.6f));

            Assert.AreEqual(slot.value, new Vector3(1.2f, 3.4f, 5.6f));

            aVector3Presenter.ExpandPath();

            var vector3yPresenter = blockPresenter.allChildren.Where(t => t is VFXContextDataInputAnchorPresenter && (t as VFXContextDataInputAnchorPresenter).path == "aVector3.y").First() as VFXContextDataInputAnchorPresenter;

            vector3yPresenter.SetPropertyValue(7.8f);

            Assert.AreEqual(slot.value, new Vector3(1.2f, 7.8f, 5.6f));

            DestroyTestAsset();
        }

        [Test]
        public void CascadedOperatorAdd()
        {
            CreateTestAsset();

            Func<IVFXSlotContainer, VFXSlotContainerPresenter> fnFindPresenter = delegate(IVFXSlotContainer slotContainer)
                {
                    var allPresenter = m_ViewPresenter.allChildren.OfType<VFXSlotContainerPresenter>();
                    return allPresenter.FirstOrDefault(o => o.slotContainer == slotContainer);
                };

            var vector2Desc = VFXLibrary.GetParameters().FirstOrDefault(o => o.name == "Vector2");
            var vector2 = m_ViewPresenter.AddVFXParameter(new Vector2(-100, -100), vector2Desc);

            var addDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Add");
            var add = m_ViewPresenter.AddVFXOperator(new Vector2(100, 100), addDesc);

            var absDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Abs");
            var abs = m_ViewPresenter.AddVFXOperator(new Vector2(100, 100), absDesc);

            var absPresenter = fnFindPresenter(abs);
            var addPresenter = fnFindPresenter(add);
            var edgePresenter = ScriptableObject.CreateInstance<VFXDataEdgePresenter>();

            edgePresenter.input = addPresenter.outputAnchors[0];
            edgePresenter.output = absPresenter.inputAnchors[0];
            m_ViewPresenter.AddElement(edgePresenter);
            Assert.AreEqual(VFXValueType.kFloat, abs.outputSlots[0].GetExpression().ValueType);

            var vector2Presenter = fnFindPresenter(vector2);
            for (int i = 0; i < 4; ++i)
            {
                edgePresenter = ScriptableObject.CreateInstance<VFXDataEdgePresenter>();
                edgePresenter.input = vector2Presenter.outputAnchors[0];
                edgePresenter.output = addPresenter.inputAnchors[i];
                m_ViewPresenter.AddElement(edgePresenter);
            }

            Assert.AreEqual(VFXValueType.kFloat2, add.outputSlots[0].GetExpression().ValueType);
            Assert.AreEqual(VFXValueType.kFloat2, abs.outputSlots[0].GetExpression().ValueType);

            m_ViewPresenter.RemoveElement(addPresenter);
            Assert.AreEqual(VFXValueType.kFloat, abs.outputSlots[0].GetExpression().ValueType);

            DestroyTestAsset();
        }

        [Test]
        public void UndoRedoAddOperator()
        {
            CreateTestAsset();
            Func<VFXSlotContainerPresenter[]> fnAllOperatorPresenter = delegate()
                {
                    var allPresenter = m_ViewPresenter.allChildren.OfType<VFXSlotContainerPresenter>();
                    return allPresenter.OfType<VFXOperatorPresenter>().ToArray();
                };

            Action fnResync = delegate()
                {
                    //Force Resync (in test suite, Undo.undoRedoPerformed isn't triggered)
                    m_ViewPresenter.SetVFXAsset(m_ViewPresenter.GetVFXAsset(), true);
                };

            Action fnTestShouldExist = delegate()
                {
                    var allOperatorPresenter = fnAllOperatorPresenter();
                    Assert.AreEqual(1, allOperatorPresenter.Length);
                    Assert.IsInstanceOf(typeof(VFXOperatorAbs), allOperatorPresenter[0].model);
                };

            Action fnTestShouldNotExist = delegate()
                {
                    var allOperatorPresenter = fnAllOperatorPresenter();
                    Assert.AreEqual(0, allOperatorPresenter.Length);
                };

            Undo.IncrementCurrentGroup();
            var absDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Abs");
            var abs = m_ViewPresenter.AddVFXOperator(new Vector2(0, 0), absDesc);

            fnTestShouldExist();
            Undo.PerformUndo(); fnResync();
            fnTestShouldNotExist();
            Undo.PerformRedo(); fnResync();
            fnTestShouldExist();

            Undo.IncrementCurrentGroup();
            m_ViewPresenter.RemoveElement(fnAllOperatorPresenter()[0]);
            fnTestShouldNotExist();
            Undo.PerformUndo(); fnResync();
            fnTestShouldExist();
            Undo.PerformRedo(); fnResync();
            fnTestShouldNotExist();

            DestroyTestAsset();
        }

        [Test]
        public void UndoRedoOperatorLinkSimple()
        {
            Action fnResync = delegate()
                {
                    //Force Resync (in test suite, Undo.undoRedoPerformed isn't triggered)
                    m_ViewPresenter.SetVFXAsset(m_ViewPresenter.GetVFXAsset(), true);
                };

            CreateTestAsset();

            Func<Type, VFXSlotContainerPresenter> fnFindPresenter = delegate(Type type)
                {
                    var allPresenter = m_ViewPresenter.allChildren.OfType<VFXSlotContainerPresenter>();
                    return allPresenter.FirstOrDefault(o => type.IsInstanceOfType(o.slotContainer));
                };

            var cosDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Cos");
            var sinDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Sin");
            Undo.IncrementCurrentGroup();
            var cos = m_ViewPresenter.AddVFXOperator(new Vector2(0, 0), cosDesc);
            Undo.IncrementCurrentGroup();
            var sin = m_ViewPresenter.AddVFXOperator(new Vector2(1, 1), sinDesc);
            var cosPresenter = fnFindPresenter(typeof(VFXOperatorCos));
            var sinPresenter = fnFindPresenter(typeof(VFXOperatorSin));

            Func<int> fnCountEdge = delegate()
                {
                    return m_ViewPresenter.allChildren.OfType<VFXDataEdgePresenter>().Count();
                };

            Undo.IncrementCurrentGroup();
            Assert.AreEqual(0, fnCountEdge());

            var edgePresenterSin = ScriptableObject.CreateInstance<VFXDataEdgePresenter>();
            edgePresenterSin.input = cosPresenter.outputAnchors[0];
            edgePresenterSin.output = sinPresenter.inputAnchors[0];
            m_ViewPresenter.AddElement(edgePresenterSin);
            Assert.AreEqual(1, fnCountEdge());

            Undo.PerformUndo(); fnResync();
            Assert.AreEqual(0, fnCountEdge());
            Assert.NotNull(fnFindPresenter(typeof(VFXOperatorCos)));
            Assert.NotNull(fnFindPresenter(typeof(VFXOperatorSin)));

            DestroyTestAsset();
        }

        [Test]
        public void UndoRedoOperatorLinkAdvanced()
        {
            Action fnResync = delegate()
                {
                    //Force Resync (in test suite, Undo.undoRedoPerformed isn't triggered)
                    m_ViewPresenter.SetVFXAsset(m_ViewPresenter.GetVFXAsset(), true);
                };

            Func<Type, VFXSlotContainerPresenter> fnFindPresenter = delegate(Type type)
                {
                    var allPresenter = m_ViewPresenter.allChildren.OfType<VFXSlotContainerPresenter>();
                    return allPresenter.FirstOrDefault(o => type.IsInstanceOfType(o.slotContainer));
                };

            Func<int> fnCountEdge = delegate()
                {
                    return m_ViewPresenter.allChildren.OfType<VFXDataEdgePresenter>().Count();
                };

            CreateTestAsset();

            var absDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Abs");
            var appendDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "AppendVector");
            var crossDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Cross");
            var cosDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Cos");
            var sinDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Sin");

            var abs = m_ViewPresenter.AddVFXOperator(new Vector2(0, 0), absDesc);
            var append = m_ViewPresenter.AddVFXOperator(new Vector2(1, 1), appendDesc);
            var cross = m_ViewPresenter.AddVFXOperator(new Vector2(2, 2), crossDesc);
            var cos = m_ViewPresenter.AddVFXOperator(new Vector2(3, 3), cosDesc);
            var sin = m_ViewPresenter.AddVFXOperator(new Vector2(4, 4), sinDesc);

            var absPresenter = fnFindPresenter(typeof(VFXOperatorAbs));
            var appendPresenter = fnFindPresenter(typeof(VFXOperatorAppendVector));
            var crossPresenter = fnFindPresenter(typeof(VFXOperatorCross));

            for (int i = 0; i < 3; ++i)
            {
                var edgePresenter = ScriptableObject.CreateInstance<VFXDataEdgePresenter>();
                edgePresenter.input = absPresenter.outputAnchors[0];
                edgePresenter.output = appendPresenter.inputAnchors[i];
                m_ViewPresenter.AddElement(edgePresenter);
            }

            var edgePresenterCross = ScriptableObject.CreateInstance<VFXDataEdgePresenter>();
            edgePresenterCross.input = appendPresenter.outputAnchors[0];
            edgePresenterCross.output = crossPresenter.inputAnchors[0];
            m_ViewPresenter.AddElement(edgePresenterCross);

            Undo.IncrementCurrentGroup();
            Assert.AreEqual(4, fnCountEdge());
            Assert.IsInstanceOf(typeof(VFXSlotFloat3), (appendPresenter.outputAnchors[0] as VFXDataAnchorPresenter).model);

            //Find last edge in append node
            var referenceAnchor = appendPresenter.inputAnchors[2];
            var edgeToDelete = m_ViewPresenter.allChildren
                .OfType<VFXDataEdgePresenter>()
                .Cast<VFXDataEdgePresenter>()
                .FirstOrDefault(e =>
                {
                    return e.input == referenceAnchor;
                });
            Assert.NotNull(edgeToDelete);

            m_ViewPresenter.RemoveElement(edgeToDelete);
            Assert.AreEqual(2, fnCountEdge()); //cross should be implicitly disconnected ...
            Assert.IsInstanceOf(typeof(VFXSlotFloat2), (appendPresenter.outputAnchors[0] as VFXDataAnchorPresenter).model);

            Undo.PerformUndo(); fnResync();
            Assert.AreEqual(4, fnCountEdge()); //... and restored !
            Assert.IsInstanceOf(typeof(VFXSlotFloat3), (fnFindPresenter(typeof(VFXOperatorAppendVector)).outputAnchors[0] as VFXDataAnchorPresenter).model);
            Undo.PerformRedo(); fnResync();
            Assert.AreEqual(2, fnCountEdge());
            Assert.IsInstanceOf(typeof(VFXSlotFloat2), (fnFindPresenter(typeof(VFXOperatorAppendVector)).outputAnchors[0] as VFXDataAnchorPresenter).model);

            //Improve test connecting cos & sin => then try delete append
            Undo.PerformUndo(); fnResync();
            Undo.IncrementCurrentGroup();
            Assert.AreEqual(4, fnCountEdge());
            Assert.IsInstanceOf(typeof(VFXSlotFloat3), (fnFindPresenter(typeof(VFXOperatorAppendVector)).outputAnchors[0] as VFXDataAnchorPresenter).model);

            var edgePresenterCos = ScriptableObject.CreateInstance<VFXDataEdgePresenter>();
            edgePresenterCos.input = fnFindPresenter(typeof(VFXOperatorAppendVector)).outputAnchors[0];
            edgePresenterCos.output = fnFindPresenter(typeof(VFXOperatorCos)).inputAnchors[0];
            m_ViewPresenter.AddElement(edgePresenterCos);
            Assert.AreEqual(5, fnCountEdge());

            var edgePresenterSin = ScriptableObject.CreateInstance<VFXDataEdgePresenter>();
            edgePresenterSin.input = fnFindPresenter(typeof(VFXOperatorAppendVector)).outputAnchors[0];
            edgePresenterSin.output = fnFindPresenter(typeof(VFXOperatorSin)).inputAnchors[0];
            m_ViewPresenter.AddElement(edgePresenterSin);
            Assert.AreEqual(6, fnCountEdge());

            m_ViewPresenter.RemoveElement(fnFindPresenter(typeof(VFXOperatorAppendVector)));
            Assert.AreEqual(0, fnCountEdge());

            DestroyTestAsset();
        }

        [Test]
        public void UndoRedoOperatorSettings()
        {
            CreateTestAsset();

            Func<IVFXSlotContainer, VFXSlotContainerPresenter> fnFindPresenter = delegate(IVFXSlotContainer slotContainer)
                {
                    var allPresenter = m_ViewPresenter.allChildren.OfType<VFXSlotContainerPresenter>();
                    return allPresenter.FirstOrDefault(o => o.slotContainer == slotContainer);
                };

            var componentMaskDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "ComponentMask");
            var componentMask = m_ViewPresenter.AddVFXOperator(new Vector2(0, 0), componentMaskDesc);
            var componentMaskPresenter = fnFindPresenter(componentMask) as VFXOperatorPresenter;

            var maskList = new string[] { "xy", "yww", "xw", "z" };
            for (int i = 0; i < maskList.Length; ++i)
            {
                Undo.IncrementCurrentGroup();
                componentMaskPresenter.settings = new VFXOperatorComponentMask.Settings() { mask = maskList[i] };
                Assert.AreEqual(maskList[i], (componentMaskPresenter.settings as VFXOperatorComponentMask.Settings).mask);
            }

            for (int i = maskList.Length - 1; i > 0; --i)
            {
                Undo.PerformUndo();
                Assert.AreEqual(maskList[i - 1], (componentMaskPresenter.settings as VFXOperatorComponentMask.Settings).mask);
            }

            var final = "xyzw";
            //Can cause infinite loop if value is wrongly tested
            componentMaskPresenter.settings = new VFXOperatorComponentMask.Settings() { mask = final };
            Assert.AreEqual(final, (componentMaskPresenter.settings as VFXOperatorComponentMask.Settings).mask);

            DestroyTestAsset();
        }

        [Test]
        public void UndoRedoAddBlockContext()
        {
            CreateTestAsset();

            var contextUpdateDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Update"));
            var blockDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.modelType == typeof(VFXAllType));

            var contextUpdate = m_ViewPresenter.AddVFXContext(Vector2.one, contextUpdateDesc);
            var contextPresenter  = m_ViewPresenter.allChildren.OfType<VFXContextPresenter>().FirstOrDefault() as VFXContextPresenter;

            //Creation
            Undo.IncrementCurrentGroup();
            contextPresenter.AddBlock(0, blockDesc.CreateInstance());
            Assert.AreEqual(1, contextPresenter.context.children.Count());
            Undo.PerformUndo();
            Assert.AreEqual(0, contextPresenter.context.children.Count());

            //Deletion
            var block = blockDesc.CreateInstance();
            contextPresenter.AddBlock(0, block);
            Assert.AreEqual(1, contextPresenter.context.children.Count());
            Undo.IncrementCurrentGroup();
            contextPresenter.RemoveBlock(block);
            Assert.AreEqual(0, contextPresenter.context.children.Count());

            Undo.PerformUndo();
            Assert.AreEqual(1, contextPresenter.context.children.Count());
            Assert.IsInstanceOf(typeof(VFXAllType), contextPresenter.context.children.First());

            DestroyTestAsset();
        }

#if WIP_ENABLE_UNDO_REDO_CONTEXT //see RecordFlowEdgePresenter, refactor in progress
        [Test]
        public void UndoRedoContext()
        {
            CreateTestAsset();

            Func<VFXContextPresenter> fnFirstContextPresenter = delegate()
                {
                    return m_ViewPresenter.allChildren.OfType<VFXContextPresenter>().FirstOrDefault() as VFXContextPresenter;
                };

            Action fnResync = delegate()
                {
                    //Force Resync (in test suite, Undo.undoRedoPerformed isn't triggered)
                    m_ViewPresenter.SetVFXAsset(m_ViewPresenter.GetVFXAsset(), true);
                };

            var contextDesc = VFXLibrary.GetContexts().FirstOrDefault();
            Undo.IncrementCurrentGroup();
            m_ViewPresenter.AddVFXContext(Vector2.zero, contextDesc);

            Assert.NotNull(fnFirstContextPresenter());
            Undo.PerformUndo(); fnResync();
            Assert.Null(fnFirstContextPresenter(), "Fail Undo Create");

            Undo.IncrementCurrentGroup();
            m_ViewPresenter.AddVFXContext(Vector2.zero, contextDesc);
            Assert.NotNull(fnFirstContextPresenter());
            m_ViewPresenter.RemoveElement(fnFirstContextPresenter());
            Assert.Null(fnFirstContextPresenter());

            Undo.PerformUndo(); fnResync();
            Assert.NotNull(fnFirstContextPresenter(), "Fail Undo Delete");

            DestroyTestAsset();
        }

        [Test]
        public void UndoRedoContextLink()
        {
            CreateTestAsset();

            Func<VFXContextPresenter[]> fnContextPresenter = delegate()
                {
                    return m_ViewPresenter.allChildren.OfType<VFXContextPresenter>().Cast<VFXContextPresenter>().ToArray();
                };

            Func<VFXContextPresenter> fnInitializePresenter = delegate()
                {
                    var presenter = fnContextPresenter();
                    return presenter.FirstOrDefault(o => o.model.name.Contains("Init"));
                };

            Func<VFXContextPresenter> fnUpdatePresenter = delegate()
                {
                    var presenter = fnContextPresenter();
                    return presenter.FirstOrDefault(o => o.model.name.Contains("Update"));
                };

            Func<int> fnFlowEdgeCount = delegate()
                {
                    return m_ViewPresenter.allChildren.OfType<VFXFlowEdgePresenter>().Count();
                };

            var contextInitializeDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Init"));
            var contextUpdateDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Update"));

            var contextInitialize = m_ViewPresenter.AddVFXContext(new Vector2(1, 1), contextInitializeDesc);
            var contextUpdate = m_ViewPresenter.AddVFXContext(new Vector2(2, 2), contextUpdateDesc);

            //Creation
            var flowEdge = ScriptableObject.CreateInstance<VFXFlowEdgePresenter>();
            flowEdge.input = fnUpdatePresenter().inputAnchors.FirstOrDefault();
            flowEdge.output = fnInitializePresenter().outputAnchors.FirstOrDefault();

            Undo.IncrementCurrentGroup();
            m_ViewPresenter.AddElement(flowEdge);
            Assert.AreEqual(1, fnFlowEdgeCount());

            Undo.PerformUndo();
            Assert.AreEqual(0, fnFlowEdgeCount(), "Fail undo Create");

            //Deletion
            flowEdge = ScriptableObject.CreateInstance<VFXFlowEdgePresenter>();
            flowEdge.input = fnUpdatePresenter().outputAnchors.FirstOrDefault();
            flowEdge.output = fnInitializePresenter().inputAnchors.FirstOrDefault();
            m_ViewPresenter.AddElement(flowEdge);
            Assert.AreEqual(1, fnFlowEdgeCount());

            Undo.IncrementCurrentGroup();
            m_ViewPresenter.RemoveElement(m_ViewPresenter.allChildren.OfType<VFXFlowEdgePresenter>().FirstOrDefault());
            Assert.AreEqual(0, fnFlowEdgeCount());

            Undo.PerformUndo();
            Assert.AreEqual(1, fnFlowEdgeCount(), "Fail undo Delete");

            DestroyTestAsset();
        }

#endif
    }
}
