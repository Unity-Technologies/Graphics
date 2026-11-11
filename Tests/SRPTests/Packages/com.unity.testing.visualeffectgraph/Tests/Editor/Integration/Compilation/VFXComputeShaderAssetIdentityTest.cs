using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.VFX.Test.Integration.Compilation
{
    [TestFixture]
    public class VFXComputeShaderAssetIdentityTest
    {
        [OneTimeSetUp]
        public void Init()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
        }

        [TearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        static string CreateTwoSystemGraph()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            VFXTestCommon.CreateSystem(graph);
            VFXTestCommon.CreateSystem(graph);
            VFXTestCommon.ReimportVFXGraph(graph);
            return AssetDatabase.GetAssetPath(graph);
        }

        static ComputeShader[] LoadComputeShaders(string vfxPath)
        {
            ComputeShader[] computeShaders = AssetDatabase.LoadAllAssetsAtPath(vfxPath).OfType<ComputeShader>().ToArray();
            Assert.GreaterOrEqual(computeShaders.Length, 4);
            return computeShaders;
        }

        static long GetLocalFileId(ComputeShader computeShader, out string guid)
        {
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(computeShader, out guid, out long localId));
            return localId;
        }

        static SortedSet<long> CollectLocalFileIds(string vfxPath)
        {
            var ids = new SortedSet<long>();
            foreach (ComputeShader computeShader in LoadComputeShaders(vfxPath))
                Assert.IsTrue(ids.Add(GetLocalFileId(computeShader, out _)), "Duplicate local file ID");
            return ids;
        }

        static void AssertSerializedAssetGuid(SerializedObject serializedObject, string assetGuid)
        {
            SerializedProperty guidProperty = serializedObject.FindProperty("m_AssetGUID");
            Assert.IsNotNull(guidProperty);

            SerializedProperty end = guidProperty.GetEndProperty();
            SerializedProperty word = guidProperty.Copy();
            bool enterChildren = true;
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(word.Next(enterChildren) && !SerializedProperty.EqualContents(word, end));
                enterChildren = false;

                uint expected = 0;
                for (int j = 0; j < 8; j++)
                    expected |= System.Convert.ToUInt32(assetGuid[i * 8 + j].ToString(), 16) << (j * 4);
                Assert.AreEqual(expected, unchecked((uint)word.longValue), "m_AssetGUID word " + i);
            }
        }

        static void AssertStampedIdentity(string vfxPath)
        {
            string assetGuid = AssetDatabase.AssetPathToGUID(vfxPath);
            foreach (ComputeShader computeShader in LoadComputeShaders(vfxPath))
            {
                long localId = GetLocalFileId(computeShader, out string guid);
                Assert.AreEqual(assetGuid, guid);
                Assert.AreNotEqual(0L, localId);

                var serializedShader = new SerializedObject(computeShader);
                AssertSerializedAssetGuid(serializedShader, assetGuid);

                SerializedProperty stamped = serializedShader.FindProperty("m_AssetLocalIdentifierInFile");
                Assert.IsNotNull(stamped);
                Assert.AreEqual(localId, stamped.longValue);
            }
        }

        [Test]
        public void VFXImport_StampsAssetGuidAndDistinctLocalFileIds()
        {
            string vfxPath = CreateTwoSystemGraph();

            CollectLocalFileIds(vfxPath);
            AssertStampedIdentity(vfxPath);
        }

        [Test]
        public void VFXRename_KeepsComputeLocalFileIds()
        {
            string vfxPath = CreateTwoSystemGraph();
            SortedSet<long> before = CollectLocalFileIds(vfxPath);

            Assert.AreEqual("", AssetDatabase.RenameAsset(vfxPath, "VFXComputeIdentityRenamed"));
            string renamedPath = VFXTestCommon.tempBasePath + "VFXComputeIdentityRenamed.vfx";
            AssetDatabase.ImportAsset(renamedPath, ImportAssetOptions.ForceUpdate);

            CollectionAssert.AreEqual(before, CollectLocalFileIds(renamedPath));
            AssertStampedIdentity(renamedPath);
        }

        [Test]
        public void VFXMove_KeepsComputeLocalFileIds()
        {
            string vfxPath = CreateTwoSystemGraph();
            SortedSet<long> before = CollectLocalFileIds(vfxPath);

            AssetDatabase.CreateFolder(VFXTestCommon.tempBasePath.TrimEnd('/'), "Moved");
            Assert.IsTrue(AssetDatabase.IsValidFolder(VFXTestCommon.tempBasePath + "Moved"));
            string movedPath = VFXTestCommon.tempBasePath + "Moved/" + Path.GetFileName(vfxPath);
            Assert.AreEqual("", AssetDatabase.MoveAsset(vfxPath, movedPath));
            AssetDatabase.ImportAsset(movedPath, ImportAssetOptions.ForceUpdate);

            CollectionAssert.AreEqual(before, CollectLocalFileIds(movedPath));
            AssertStampedIdentity(movedPath);
        }
    }
}
