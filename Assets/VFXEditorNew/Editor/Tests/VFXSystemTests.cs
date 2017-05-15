using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSystemTests
    {
        private VFXGraphAsset CreateAndInitContainer()
        {
            var graph = ScriptableObject.CreateInstance<VFXGraphAsset>();

            var system0 = ScriptableObject.CreateInstance<VFXSystem>();
            system0.AddChild(ScriptableObject.CreateInstance<VFXBasicInitialize>());
            system0.AddChild(ScriptableObject.CreateInstance<VFXBasicUpdate>());
            system0.AddChild(ScriptableObject.CreateInstance<VFXBasicOutput>());
            graph.root.AddChild(system0);

            var system1 = ScriptableObject.CreateInstance<VFXSystem>();
            system1.AddChild(ScriptableObject.CreateInstance<VFXBasicInitialize>());
            system1.AddChild(ScriptableObject.CreateInstance<VFXBasicUpdate>());
            system1.AddChild(ScriptableObject.CreateInstance<VFXBasicOutput>());
            graph.root.AddChild(system1);

            return graph;
        }

        [Test]
        public void ConnectContext()
        {
            var graph = CreateAndInitContainer();

            VFXSystem.ConnectContexts(((VFXSystem)graph.root[0]).GetChild(0), ((VFXSystem)graph.root[1]).GetChild(1), graph.root);

            Assert.AreEqual(3, graph.root.GetNbChildren());
            Assert.AreEqual(3, graph.root[0].GetNbChildren());
            Assert.AreEqual(1, graph.root[1].GetNbChildren());
            Assert.AreEqual(2, graph.root[2].GetNbChildren());

            Object.DestroyImmediate(graph);
        }

        [Test]
        public void DisconnectContext()
        {
            var graph = CreateAndInitContainer();

            VFXSystem.DisconnectContext(((VFXSystem)graph.root[0]).GetChild(1), graph.root);
            VFXSystem.DisconnectContext(((VFXSystem)graph.root[1]).GetChild(2), graph.root);

            Assert.AreEqual(4, graph.root.GetNbChildren());
            Assert.AreEqual(1, graph.root[0].GetNbChildren());
            Assert.AreEqual(2, graph.root[1].GetNbChildren());
            Assert.AreEqual(2, graph.root[2].GetNbChildren());
            Assert.AreEqual(1, graph.root[3].GetNbChildren());

            Object.DestroyImmediate(graph);
        }
    }
}
