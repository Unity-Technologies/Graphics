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
            VFXGraphAsset asset = VFXGraphAsset.CreateInstance<VFXGraphAsset>();

            var directoryPath = Path.GetDirectoryName(testAssetName);
            if ( ! Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            AssetDatabase.CreateAsset(asset, testAssetName);

            m_ViewPresenter = ScriptableObject.CreateInstance<VFXViewPresenter>();
            m_ViewPresenter.SetGraphAsset(asset,false);
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

                Assert.AreEqual(contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).Model == newBlock).Count(), 1);

                var blockPresenter = contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).Model == newBlock).First() as VFXBlockPresenter;

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

                Assert.AreEqual(contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).Model == newBlock).Count(), 1);

                var blockPresenter = contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).Model == newBlock).First() as VFXBlockPresenter;

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

                Assert.AreEqual(contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).Model == newBlock).Count(), 1);

                var blockPresenter = contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).Model == newBlock).First() as VFXBlockPresenter;

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

            Assert.AreEqual(contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).Model == newBlock).Count(), 1);

            var blockPresenter = contextPresenter.allChildren.Where(t => t is VFXBlockPresenter && (t as VFXBlockPresenter).Model == newBlock).First() as VFXBlockPresenter;

            Assert.NotNull(blockPresenter);

            Assert.NotZero(blockPresenter.allChildren.Where(t => t is VFXBlockDataInputAnchorPresenter && (t as VFXBlockDataInputAnchorPresenter).name == "aVector3").Count());

            VFXSlot slot = blockPresenter.Model.inputSlots.First(t => t.name == "aVector3");


            var aVector3Presenter = blockPresenter.allChildren.Where(t => t is VFXBlockDataInputAnchorPresenter && (t as VFXBlockDataInputAnchorPresenter).name == "aVector3").First() as VFXBlockDataInputAnchorPresenter;

            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXBlockDataInputAnchorPresenter && (t as VFXBlockDataInputAnchorPresenter).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXBlockDataInputAnchorPresenter && (t as VFXBlockDataInputAnchorPresenter).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXBlockDataInputAnchorPresenter && (t as VFXBlockDataInputAnchorPresenter).path == "aVector3.z").Count(), 1);

            aVector3Presenter.ExpandPath();

            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXBlockDataInputAnchorPresenter && (t as VFXBlockDataInputAnchorPresenter).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXBlockDataInputAnchorPresenter && (t as VFXBlockDataInputAnchorPresenter).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXBlockDataInputAnchorPresenter && (t as VFXBlockDataInputAnchorPresenter).path == "aVector3.z").Count(), 1);


            aVector3Presenter.RetractPath();

            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXBlockDataInputAnchorPresenter && (t as VFXBlockDataInputAnchorPresenter).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXBlockDataInputAnchorPresenter && (t as VFXBlockDataInputAnchorPresenter).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXBlockDataInputAnchorPresenter && (t as VFXBlockDataInputAnchorPresenter).path == "aVector3.z").Count(), 1);


            aVector3Presenter.SetPropertyValue(new Vector3(1.2f, 3.4f, 5.6f));

            Assert.AreEqual(slot.value, new Vector3(1.2f, 3.4f, 5.6f));

            aVector3Presenter.ExpandPath();

            var vector3yPresenter = blockPresenter.allChildren.Where(t => t is VFXBlockDataInputAnchorPresenter && (t as VFXBlockDataInputAnchorPresenter).path == "aVector3.y").First() as VFXBlockDataInputAnchorPresenter;

            vector3yPresenter.SetPropertyValue(7.8f);

            Assert.AreEqual(slot.value, new Vector3(1.2f, 7.8f, 5.6f));

            DestroyTestAsset();

        }

        [Test]
        public void CascadedOperatorAdd()
        {
            CreateTestAsset();

            Func<IVFXSlotContainer, VFXNodePresenter> fnFindPresenter = delegate (IVFXSlotContainer slotContainer)
            {
                var allPresenter = m_ViewPresenter.allChildren.OfType<VFXNodePresenter>().Cast<VFXNodePresenter>();
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
            for (int i=0; i<4; ++i)
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
        public void UndoRedoOperatorSettings()
        {
            CreateTestAsset();

            Func<IVFXSlotContainer, VFXNodePresenter> fnFindPresenter = delegate (IVFXSlotContainer slotContainer)
            {
                var allPresenter = m_ViewPresenter.allChildren.OfType<VFXNodePresenter>().Cast<VFXNodePresenter>();
                return allPresenter.FirstOrDefault(o => o.slotContainer == slotContainer);
            };

            var componentMaskDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "ComponentMask");
            var componentMask = m_ViewPresenter.AddVFXOperator(new Vector2(0, 0), componentMaskDesc);
            var componentMaskPresenter = fnFindPresenter(componentMask) as VFXOperatorPresenter;

            var maskList = new string[] { "xy", "yww", "xw", "z" };
            for (int i=0; i<maskList.Length; ++i)
            {
                Undo.IncrementCurrentGroup();
                componentMaskPresenter.settings = new VFXOperatorComponentMask.Settings() { mask = maskList[i] };
                Assert.AreEqual(maskList[i], (componentMaskPresenter.settings as VFXOperatorComponentMask.Settings).mask);
            }

            for (int i=maskList.Length-1; i > 0; --i)
            {
                Undo.PerformUndo();
                Assert.AreEqual(maskList[i-1], (componentMaskPresenter.settings as VFXOperatorComponentMask.Settings).mask);
            }

            var final = "xyzw";
            //Can cause infinite loop if value is wrongly tested
            componentMaskPresenter.settings = new VFXOperatorComponentMask.Settings() { mask = final };
            Assert.AreEqual(final, (componentMaskPresenter.settings as VFXOperatorComponentMask.Settings).mask);

            DestroyTestAsset();
        }

    }
}