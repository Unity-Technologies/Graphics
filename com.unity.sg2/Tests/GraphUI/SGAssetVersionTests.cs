using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    public class SGAssetVersionTest
    {
        [Test]
        public void TestCompareTo()
        {
            SGAssetVersion v0 = new SGAssetVersion(0, 0, 0);
            SGAssetVersion v1 = new SGAssetVersion(0, 0, 0);
            Assert.Zero(v0.CompareTo(v1), $"{v0} should equal {v1}");
            v1 = new SGAssetVersion(1, 0, 0);
            Assert.Greater(v1.CompareTo(v0), 0, $"{v1} should be greater than {v0}");
            Assert.Less(v0.CompareTo(v1), 0, $"{v0} should be less than {v1}");
            v1 = new SGAssetVersion(0, 1, 0);
            Assert.Greater(v1.CompareTo(v0), 0, $"{v1} should be greater than {v0}");
            Assert.Less(v0.CompareTo(v1), 0, $"{v0} should be less than {v1}");
            v1 = new SGAssetVersion(0, 0, 1);
            Assert.Greater(v1.CompareTo(v0), 0, $"{v1} should be greater than {v0}");
            Assert.Less(v0.CompareTo(v1), 0, $"{v0} should be less than {v1}");
            v1 = new SGAssetVersion(0, 0, 0, "b");
            Assert.Greater(v1.CompareTo(v0), 0, $"{v0} should be greater than {v1}");
            Assert.Less(v0.CompareTo(v1), 0, $"{v1} should be less than {v0}");
            v0 = new SGAssetVersion(0, 0, 0, "alpha");
            Assert.Greater(v1.CompareTo(v0), 0, $"{v1} should be greater than {v0}");
            Assert.Less(v0.CompareTo(v1), 0, $"{v0} should be less than {v1}");
        }

        [Test]
        public void TestToString()
        {
            SGAssetVersion v = new SGAssetVersion(0, 0, 0);
            Assert.AreEqual("0.0.0", v.ToString());
            v = new SGAssetVersion(0, 0, 0, "a");
            Assert.AreEqual("0.0.0a", v.ToString());
            v = new SGAssetVersion(0, 0, 0, "beta");
            Assert.AreEqual("0.0.0beta", v.ToString());
        }
    }
}
