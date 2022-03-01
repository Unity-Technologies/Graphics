using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class LostEdgesTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [UnityTest]
        public IEnumerator LostEdgesAreDrawn()
        {
            var operatorModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-100, -100));
            IConstantNodeModel intModel = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "int", new Vector2(-150, -100));
            var edge = (EdgeModel)GraphModel.CreateEdge(operatorModel.Input0, intModel.OutputPort);

            // simulate a renamed port by changing the edge's port id

            var field = typeof(EdgeModel).GetField("m_ToPortReference", BindingFlags.Instance | BindingFlags.NonPublic);
            var inputPortReference = (PortReference)field.GetValue(edge);
            inputPortReference.UniqueId = "asd";
            field.SetValue(edge, inputPortReference);

            edge.ResetPortCache(); // get rid of cached port models

            MarkGraphViewStateDirty();
            yield return null;

            var lostPortsAdded = GraphView.Query(className: "ge-port--data-type-missing-port").Build().ToList().Count;
            Assert.AreEqual(1, lostPortsAdded);
        }
    }
}
