using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    class ConstantNodeSpawningTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [Test]
        public void TestCanCreateConstantNodeForBasicTypes()
        {
            Assert.That(GraphModel.NodeModels.Count, NUnit.Framework.Is.Zero);
            var expectedTypes = new[] { typeof(string), typeof(Boolean), typeof(Int32), typeof(Double), typeof(Single), typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Quaternion), typeof(Color) };
            for (var i = 0; i < expectedTypes.Length; i++)
            {
                var type = expectedTypes[i];
                var typeHandle = type.GenerateTypeHandle();
                Type constantNodeType = GraphModel.Stencil.GetConstantNodeValueType(typeHandle);
                var constantNode = GraphModel.CreateConstantNode(type.GenerateTypeHandle(), constantNodeType.Name, 100f * i * Vector2.right);
                Assert.IsEmpty((constantNode.OutputPort as IHasTitle)?.Title);
                Assert.IsNotEmpty(constantNode.OutputPort.UniqueName);
            }
            Assert.That(GraphModel.NodeModels.OfType<ConstantNodeModel>().Count(), NUnit.Framework.Is.EqualTo(expectedTypes.Length));
        }
    }
}
