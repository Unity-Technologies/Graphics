using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Commands
{
    [Category("Command")]
    class NodeCommandGuidTests : BaseFixture<NoUIGraphViewTestGraphTool>
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        class NodeDesc
        {
            public Type Type;
            public SerializableGUID Guid;

            public string Name => Type.ToString();
        }

        [Test]
        public void Test_DeleteNodeWithGuid([Values] TestingMode mode)
        {
            var nodes = new[]
            {
                new NodeDesc { Type = typeof(bool) },
                new NodeDesc { Type = typeof(float) },
                new NodeDesc { Type = typeof(Quaternion) },
            };

            foreach (var n in nodes)
            {
                var node = GraphModel.CreateConstantNode(n.Type.GenerateTypeHandle(), n.Name, Vector2.zero);
                n.Guid = node.Guid;

                TestPrereqCommandPostreq(mode,
                    () =>
                    {
                        Assert.IsTrue(GraphModel.TryGetModelFromGuid(n.Guid, out var model));
                        return new DeleteElementsCommand(model);
                    },
                    () =>
                    {
                        Assert.IsFalse(GraphModel.TryGetModelFromGuid(n.Guid, out _));
                    });
            }
        }
    }
}
