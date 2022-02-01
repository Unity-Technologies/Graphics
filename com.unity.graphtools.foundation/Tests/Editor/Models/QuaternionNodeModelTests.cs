using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    public class QuaternionNodeModelTests
    {
        [Test]
        public void TestQuaternionConstantDefaultValue()
        {
            var node = new QuaternionConstant();
            Assert.AreEqual(Quaternion.identity, node.DefaultValue);
        }
    }
}
