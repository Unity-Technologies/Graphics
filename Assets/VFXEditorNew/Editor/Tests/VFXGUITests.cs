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
    public class VFXGUITests
    {

        [MenuItem("VFX Editor/Run GUI Tests")]
        static void RunGUITests()
        {
            VFXGUITests tests = new VFXGUITests();

            tests.CreateTestAsset("GUITest");
            tests.CreateAllInitializeBlocks();
            tests.CreateAllUpdateBlocks();
            tests.CreateAllOutputBlocks();
        }
        VFXViewPresenter m_ViewPresenter;
        VFXViewWindow m_Window;
        VFXView m_View;

        const string testAssetName = "Assets/TmpTests/{0}.asset";

        public void CreateTestAsset(string name)
        {
            VFXGraphAsset asset = VFXGraphAsset.CreateInstance<VFXGraphAsset>();

            var filePath = string.Format(testAssetName, name);
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            AssetDatabase.CreateAsset(asset, filePath);

           Selection.activeObject = AssetDatabase.LoadAssetAtPath<VFXGraphAsset>(filePath);

            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            window.Close();
            window = EditorWindow.GetWindow<VFXViewWindow>();
            m_ViewPresenter = window.GetPresenter<VFXViewPresenter>();
            m_View = m_ViewPresenter.View;
        }

        void DestroyTestAsset(string name)
        {
            var filePath = string.Format(testAssetName, name);
            AssetDatabase.DeleteAsset(filePath);
            UnityEngine.Object.DestroyImmediate(Selection.activeObject);

            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            window.Close();
        }

        [Test]
        public void CreateAllInitializeBlocksTest()
        {
            CreateTestAsset("TestGUI1");
            CreateAllInitializeBlocks();
            DestroyTestAsset("TestGUI1");
        }
            public void CreateAllInitializeBlocks()
        {


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
        }


        [Test]
        public void CreateAllUpdateBlocksTest()
        {
            CreateTestAsset("TestGUI2");
            CreateAllUpdateBlocks();
            DestroyTestAsset("TestGUI2");
        }

        public void CreateAllUpdateBlocks()
        {

            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Update").First();

            var newContext = m_ViewPresenter.AddVFXContext(new Vector2(100, 1000), initContextDesc);

            var contextPresenter = m_ViewPresenter.allChildren.Where(t => t is VFXContextPresenter && (t as VFXContextPresenter).model == newContext).First() as VFXContextPresenter;

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
        }

        [Test]
        public void CreateAllOutputBlocksTest()
        {
            CreateTestAsset("TestGUI3");
            CreateAllOutputBlocks();
            DestroyTestAsset("TestGUI3");
        }

        public void CreateAllOutputBlocks()
        {

            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.name == "Output").First();

            var newContext = m_ViewPresenter.AddVFXContext(new Vector2(100, 2000), initContextDesc);

            var contextPresenter = m_ViewPresenter.allChildren.Where(t => t is VFXContextPresenter && (t as VFXContextPresenter).model == newContext).First() as VFXContextPresenter;

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
        }

        [Test]
        public void ExpandRetractAndSetPropertyValue()
        {
            CreateTestAsset("TestGUI4");

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

            DestroyTestAsset("TestGUI4");
        }
    }
}
