using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    class ConstantNodeTests : BaseFixture<NoUIGraphViewTestGraphTool>
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
                Type constantType = GraphModel.Stencil.GetConstantType(typeHandle);
                var constantNode = GraphModel.CreateConstantNode(typeHandle, constantType.Name, 100f * i * Vector2.right);
                Assert.IsEmpty((constantNode.OutputPort as IHasTitle)?.Title);
                Assert.IsNotEmpty(constantNode.OutputPort.UniqueName);
            }
            Assert.That(GraphModel.NodeModels.OfType<ConstantNodeModel>().Count(), NUnit.Framework.Is.EqualTo(expectedTypes.Length));
        }

        [Test]
        public void CloneConstantWorks()
        {
            var stencil = GraphModel.Stencil;
            var c1 = stencil.CreateConstantValue(TypeHandle.Int);
            Assert.IsInstanceOf<IntConstant>(c1);

            var c2 = c1.Clone();
            Assert.IsInstanceOf<IntConstant>(c2);

            var c1Cast = (IntConstant)c1;
            var c2Cast = (IntConstant)c2;
            Assert.AreEqual(c1Cast.Type, c2Cast.Type);
            Assert.AreEqual(c1Cast.Value, c2Cast.Value);
            Assert.AreEqual(c1Cast.ObjectValue, c2Cast.ObjectValue);
            Assert.AreEqual(c1Cast.DefaultValue, c2Cast.DefaultValue);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        enum TestEnum
        {
            A,
            B
        }

        [Test]
        public void CloneEnumConstantWorks()
        {
            var stencil = GraphModel.Stencil;
            var typeHandle = TypeHandleHelpers.GenerateTypeHandle(typeof(TestEnum));
            var c1 = stencil.CreateConstantValue(typeHandle);
            Assert.IsInstanceOf<EnumConstant>(c1);

            var c2 = c1.Clone();
            Assert.IsInstanceOf<EnumConstant>(c2);

            var c1Cast = (EnumConstant)c1;
            var c2Cast = (EnumConstant)c2;
            Assert.AreEqual(c1Cast.Type, c2Cast.Type);
            Assert.AreEqual(c1Cast.Value, c2Cast.Value);
            Assert.AreEqual(c1Cast.ObjectValue, c2Cast.ObjectValue);
            Assert.AreEqual(c1Cast.DefaultValue, c2Cast.DefaultValue);
        }
    }
}
