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
            VFXGraphAsset asset = new VFXGraphAsset();

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

            Assert.NotZero(blockPresenter.allChildren.Where(t => t is VFXDataInputAnchorPresenter && (t as VFXDataInputAnchorPresenter).name == "aVector3").Count());

            blockPresenter.ExpandPath("aVector3");

            Assert.NotZero(blockPresenter.allChildren.Where(t => t is VFXDataInputAnchorPresenter && (t as VFXDataInputAnchorPresenter).name == "aVector3.x").Count());
            Assert.NotZero(blockPresenter.allChildren.Where(t => t is VFXDataInputAnchorPresenter && (t as VFXDataInputAnchorPresenter).name == "aVector3.y").Count());
            Assert.NotZero(blockPresenter.allChildren.Where(t => t is VFXDataInputAnchorPresenter && (t as VFXDataInputAnchorPresenter).name == "aVector3.z").Count());


            blockPresenter.RetractPath("aVector3");

            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXDataInputAnchorPresenter && (t as VFXDataInputAnchorPresenter).name == "aVector3.x").Count(), 0);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXDataInputAnchorPresenter && (t as VFXDataInputAnchorPresenter).name == "aVector3.y").Count(), 0);
            Assert.AreEqual(blockPresenter.allChildren.Where(t => t is VFXDataInputAnchorPresenter && (t as VFXDataInputAnchorPresenter).name == "aVector3.z").Count(), 0);

            var aVector3Presenter = blockPresenter.allChildren.Where(t => t is VFXDataInputAnchorPresenter && (t as VFXDataInputAnchorPresenter).name == "aVector3").First() as VFXDataInputAnchorPresenter;

            var propertyInfo = aVector3Presenter.propertyInfo;

            propertyInfo.value = new Vector3(1.2f, 3.4f, 5.6f);

            blockPresenter.PropertyValueChanged(ref propertyInfo);


            var properties = newBlock.GetProperties();
            var values = newBlock.GetCurrentPropertiesValues();

            int vector3Index = System.Array.FindIndex(properties, t => t.name == "aVector3");

            Assert.AreNotEqual(vector3Index, -1);
            Assert.AreEqual(values[vector3Index], new Vector3(1.2f, 3.4f, 5.6f));

            blockPresenter.ExpandPath("aVector3");

            var vector3yPresenter = blockPresenter.allChildren.Where(t => t is VFXDataInputAnchorPresenter && (t as VFXDataInputAnchorPresenter).name == "aVector3.y").First() as VFXDataInputAnchorPresenter;

            propertyInfo = vector3yPresenter.propertyInfo;

            propertyInfo.value = 7.8f;

            blockPresenter.PropertyValueChanged(ref propertyInfo);

            Assert.AreEqual(values[vector3Index], new Vector3(1.2f, 7.8f, 5.6f));

            DestroyTestAsset();

        }
    }
}