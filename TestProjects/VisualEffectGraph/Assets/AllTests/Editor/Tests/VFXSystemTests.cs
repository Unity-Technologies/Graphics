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
        private VFXAsset CreateAndInitContainer()
        {
            var graph = new VFXAsset();

            var system0 = ScriptableObject.CreateInstance<VFXSystem>();
            system0.AddChild(ScriptableObject.CreateInstance<VFXBasicInitialize>());
            system0.AddChild(ScriptableObject.CreateInstance<VFXBasicUpdate>());
            system0.AddChild(ScriptableObject.CreateInstance<VFXBasicOutput>());
            graph.GetOrCreateGraph().AddChild(system0);

            var system1 = ScriptableObject.CreateInstance<VFXSystem>();
            system1.AddChild(ScriptableObject.CreateInstance<VFXBasicInitialize>());
            system1.AddChild(ScriptableObject.CreateInstance<VFXBasicUpdate>());
            system1.AddChild(ScriptableObject.CreateInstance<VFXBasicOutput>());
            graph.GetOrCreateGraph().AddChild(system1);

            return graph;
        }

        [Test]
        public void ConnectContext()
        {
            var asset = CreateAndInitContainer();
            var graph = asset.GetOrCreateGraph();

            VFXSystem.ConnectContexts(((VFXSystem)graph[0]).GetChild(0), ((VFXSystem)graph[1]).GetChild(1), graph);


            Assert.AreEqual(3, graph.GetNbChildren());
            Assert.AreEqual(3, graph[0].GetNbChildren());
            Assert.AreEqual(1, graph[1].GetNbChildren());
            Assert.AreEqual(2, graph[2].GetNbChildren());

            Object.DestroyImmediate(asset);
        }

        [Test]
        public void DisconnectContext()
        {
            var asset = CreateAndInitContainer();
            var graph = asset.GetOrCreateGraph();

            VFXSystem.DisconnectContext(((VFXSystem)graph[0]).GetChild(1), graph);
            VFXSystem.DisconnectContext(((VFXSystem)graph[1]).GetChild(2), graph);

            Assert.AreEqual(4, graph.GetNbChildren());
            Assert.AreEqual(1, graph[0].GetNbChildren());
            Assert.AreEqual(2, graph[1].GetNbChildren());
            Assert.AreEqual(2, graph[2].GetNbChildren());
            Assert.AreEqual(1, graph[3].GetNbChildren());

            Object.DestroyImmediate(graph);
        }
    }
}
