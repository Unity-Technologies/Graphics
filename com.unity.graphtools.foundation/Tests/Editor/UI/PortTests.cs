using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    public class PortTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [UnityTest]
        public IEnumerator CanAcceptDropWorks()
        {
            var node = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.one * 200);

            var compatibleConstant = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "compatibleConstant", Vector2.zero);
            var incompatibleConstant = GraphModel.CreateConstantNode(typeof(bool).GenerateTypeHandle(), "incompatibleConstant", Vector2.zero);

            var compatibleVdm = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "compatibleVdm", ModifierFlags.None, false);
            var incompatibleVdm = GraphModel.CreateGraphVariableDeclaration(typeof(bool).GenerateTypeHandle(), "incompatibleVdm", ModifierFlags.None, false);

            var outputTriggerVdm = GraphModel.CreateGraphVariableDeclaration(TypeHandle.ExecutionFlow, "triggerVdm", ModifierFlags.Write, false);

            var compatibleVariableNode = GraphModel.CreateVariableNode(compatibleVdm, Vector2.zero);
            var incompatibleVariableNode = GraphModel.CreateVariableNode(incompatibleVdm, Vector2.zero);

            var otherNode = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.one * 200);

            MarkGraphModelStateDirty();
            yield return null;

            var dataInputPort = node.Input0.GetView<Port>(GraphView);
            Assert.NotNull(dataInputPort);
            Assert.True(dataInputPort.PortModel.PortType == PortType.Data);
            Assert.True(dataInputPort.PortModel.DataTypeHandle == TypeHandle.Int);

            // Constants
            Assert.True(dataInputPort.CanAcceptDrop(new List<IGraphElementModel> { compatibleConstant }));
            Assert.False(dataInputPort.CanAcceptDrop(new List<IGraphElementModel> { incompatibleConstant }));

            // Variable declarations
            Assert.True(dataInputPort.CanAcceptDrop(new List<IGraphElementModel> { compatibleVdm }));
            Assert.False(dataInputPort.CanAcceptDrop(new List<IGraphElementModel> { incompatibleVdm }));

            // Variable nodes
            Assert.True(dataInputPort.CanAcceptDrop(new List<IGraphElementModel> { compatibleVariableNode }));
            Assert.False(dataInputPort.CanAcceptDrop(new List<IGraphElementModel> { incompatibleVariableNode }));

            // Other nodes
            Assert.False(dataInputPort.CanAcceptDrop(new List<IGraphElementModel> { otherNode }));

            // More than one element
            Assert.False(dataInputPort.CanAcceptDrop(new List<IGraphElementModel> { compatibleConstant, compatibleVariableNode }));

            // Triggers
            var exeOutputPort = node.ExeOutput0.GetView<Port>(GraphView);
            Assert.NotNull(exeOutputPort);
            Assert.True(exeOutputPort.PortModel.PortType == PortType.Execution);
            Assert.True(exeOutputPort.CanAcceptDrop(new List<IGraphElementModel> { outputTriggerVdm }));
            Assert.False(dataInputPort.CanAcceptDrop(new List<IGraphElementModel> { outputTriggerVdm }));
        }
    }
}
