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

            var system0 = new VFXSystem();
            system0.AddChild(new VFXContext(VFXContextType.kInit));
            system0.AddChild(new VFXContext(VFXContextType.kUpdate));
            system0.AddChild(new VFXContext(VFXContextType.kOutput));
            graph.root.AddChild(system0);

            var system1 = new VFXSystem();
            system1.AddChild(new VFXContext(VFXContextType.kInit));
            system1.AddChild(new VFXContext(VFXContextType.kUpdate));
            system1.AddChild(new VFXContext(VFXContextType.kOutput));
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
