using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    [TestFixture]
    class ProbeVolumeBakingSetTests
    {
        ProbeVolumeBakingSet m_BakingSet;
        string m_SceneGUID;

        [SetUp]
        public void SetUp()
        {
            m_BakingSet = ScriptableObject.CreateInstance<ProbeVolumeBakingSet>();
            m_BakingSet.SetDefaults();
            m_SceneGUID = GUID.Generate().ToString();
        }

        [TearDown]
        public void TearDown()
        {
            Undo.ClearUndo(m_BakingSet);
            ProbeVolumeBakingSet.SceneToBakingSet.Instance.Remove(m_SceneGUID);
            Object.DestroyImmediate(m_BakingSet);
        }

        [Test]
        public void SceneCanBeReAddedAfterUndoOfAdd()
        {
            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(m_BakingSet, "Added scene in baking set");
            m_BakingSet.AddScene(m_SceneGUID);
            Assert.IsTrue(m_BakingSet.sceneGUIDs.Contains(m_SceneGUID));
            Assert.AreEqual(m_BakingSet, ProbeVolumeBakingSet.GetBakingSetForScene(m_SceneGUID));

            Undo.IncrementCurrentGroup();
            Undo.PerformUndo();
            Assert.IsFalse(m_BakingSet.sceneGUIDs.Contains(m_SceneGUID));
            Assert.IsNull(ProbeVolumeBakingSet.GetBakingSetForScene(m_SceneGUID));

            Assert.IsTrue(m_BakingSet.TryAddScene(m_SceneGUID));
            Assert.IsTrue(m_BakingSet.sceneGUIDs.Contains(m_SceneGUID));
            Assert.AreEqual(m_BakingSet, ProbeVolumeBakingSet.GetBakingSetForScene(m_SceneGUID));
        }

        [Test]
        public void RedoOfAddRestoresSceneAndMapping()
        {
            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(m_BakingSet, "Added scene in baking set");
            m_BakingSet.AddScene(m_SceneGUID);

            Undo.IncrementCurrentGroup();
            Undo.PerformUndo();
            Assert.IsFalse(m_BakingSet.sceneGUIDs.Contains(m_SceneGUID));

            Undo.PerformRedo();
            Assert.IsTrue(m_BakingSet.sceneGUIDs.Contains(m_SceneGUID));
            Assert.AreEqual(m_BakingSet, ProbeVolumeBakingSet.GetBakingSetForScene(m_SceneGUID));
        }

        [Test]
        public void UndoOfRemoveRestoresSceneAndMapping()
        {
            m_BakingSet.AddScene(m_SceneGUID);

            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(m_BakingSet, "Deleted scene in baking set");
            m_BakingSet.RemoveScene(m_SceneGUID);
            Assert.IsFalse(m_BakingSet.sceneGUIDs.Contains(m_SceneGUID));
            Assert.IsNull(ProbeVolumeBakingSet.GetBakingSetForScene(m_SceneGUID));

            Undo.IncrementCurrentGroup();
            Undo.PerformUndo();
            Assert.IsTrue(m_BakingSet.sceneGUIDs.Contains(m_SceneGUID));
            Assert.AreEqual(m_BakingSet, ProbeVolumeBakingSet.GetBakingSetForScene(m_SceneGUID));
        }

        [Test]
        public void DuplicateSetDoesNotStealSceneMapping()
        {
            m_BakingSet.AddScene(m_SceneGUID);

            var duplicate = Object.Instantiate(m_BakingSet);
            try
            {
                ProbeVolumeBakingSet.SceneToBakingSet.Resync(duplicate);
                Assert.AreEqual(m_BakingSet, ProbeVolumeBakingSet.GetBakingSetForScene(m_SceneGUID));

                duplicate.RemoveScene(m_SceneGUID);
                Assert.IsFalse(duplicate.sceneGUIDs.Contains(m_SceneGUID));
                Assert.AreEqual(m_BakingSet, ProbeVolumeBakingSet.GetBakingSetForScene(m_SceneGUID));
            }
            finally
            {
                Object.DestroyImmediate(duplicate);
            }
        }

        [Test]
        public void SetSceneOnDuplicateDoesNotEraseOwnersMapping()
        {
            m_BakingSet.AddScene(m_SceneGUID);

            var duplicate = Object.Instantiate(m_BakingSet);
            var replacementGUID = GUID.Generate().ToString();
            try
            {
                duplicate.SetScene(replacementGUID, 0);
                Assert.AreEqual(m_BakingSet, ProbeVolumeBakingSet.GetBakingSetForScene(m_SceneGUID));
                Assert.AreEqual(duplicate, ProbeVolumeBakingSet.GetBakingSetForScene(replacementGUID));
            }
            finally
            {
                ProbeVolumeBakingSet.SceneToBakingSet.Instance.Remove(replacementGUID);
                Object.DestroyImmediate(duplicate);
            }
        }

        [Test]
        public void UndoOfMoveBetweenSetsRestoresOwnership()
        {
            m_BakingSet.singleSceneMode = false;
            m_BakingSet.AddScene(m_SceneGUID);

            var otherSet = ScriptableObject.CreateInstance<ProbeVolumeBakingSet>();
            otherSet.SetDefaults();
            otherSet.singleSceneMode = false;
            try
            {
                Undo.IncrementCurrentGroup();
                Undo.RegisterCompleteObjectUndo(new Object[] { otherSet, m_BakingSet }, "Moved scene to baking set");
                otherSet.MoveSceneToBakingSet(m_SceneGUID, -1);
                Assert.IsFalse(m_BakingSet.sceneGUIDs.Contains(m_SceneGUID));
                Assert.IsTrue(otherSet.sceneGUIDs.Contains(m_SceneGUID));
                Assert.AreEqual(otherSet, ProbeVolumeBakingSet.GetBakingSetForScene(m_SceneGUID));

                Undo.IncrementCurrentGroup();
                Undo.PerformUndo();
                Assert.IsTrue(m_BakingSet.sceneGUIDs.Contains(m_SceneGUID));
                Assert.IsFalse(otherSet.sceneGUIDs.Contains(m_SceneGUID));
                Assert.AreEqual(m_BakingSet, ProbeVolumeBakingSet.GetBakingSetForScene(m_SceneGUID));
            }
            finally
            {
                Undo.ClearUndo(otherSet);
                Object.DestroyImmediate(otherSet);
            }
        }
    }
}
