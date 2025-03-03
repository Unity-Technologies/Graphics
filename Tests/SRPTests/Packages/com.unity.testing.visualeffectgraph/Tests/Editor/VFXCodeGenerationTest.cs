using System;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXCodeGenerationTest
    {
        [Test]
        public void ArtifactHashUnchanged_OnReimporting_ShaderGraph_With_VfxTarget()
        {
            var path = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/SG_Lit_VfxTarget.shadergraph";

            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();

            var initialHash = AssetDatabase.GetAssetDependencyHash(path);

            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();

            var finalHash = AssetDatabase.GetAssetDependencyHash(path);

            Assert.AreEqual(initialHash, finalHash);
        }
    }
}
