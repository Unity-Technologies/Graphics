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
        private VFXModelContainer CreateAndInitContainer()
        {
            var modelContainer = ScriptableObject.CreateInstance<VFXModelContainer>();

            var system0 = new VFXSystem();
            system0.AddChild(new VFXContext(VFXContextType.kInit));
            system0.AddChild(new VFXContext(VFXContextType.kUpdate));
            system0.AddChild(new VFXContext(VFXContextType.kOutput));
            modelContainer.m_Roots.Add(system0);

            var system1 = new VFXSystem();
            system1.AddChild(new VFXContext(VFXContextType.kInit));
            system1.AddChild(new VFXContext(VFXContextType.kUpdate));
            system1.AddChild(new VFXContext(VFXContextType.kOutput));
            modelContainer.m_Roots.Add(system1);

            return modelContainer;
        }

        [Test]
        public void ConnectContext()
        {
            var modelContainer = CreateAndInitContainer();

            VFXSystem.ConnectContexts(((VFXSystem)modelContainer.m_Roots[0]).GetChild(0), ((VFXSystem)modelContainer.m_Roots[1]).GetChild(1), modelContainer);

            Assert.AreEqual(3, modelContainer.m_Roots.Count);
            Assert.AreEqual(3, modelContainer.m_Roots[0].GetNbChildren());
            Assert.AreEqual(1, modelContainer.m_Roots[1].GetNbChildren());
            Assert.AreEqual(2, modelContainer.m_Roots[2].GetNbChildren());

            Object.DestroyImmediate(modelContainer);
        }

        [Test]
        public void DisconnectContext()
        {
            var modelContainer = CreateAndInitContainer();

            VFXSystem.DisconnectContext(((VFXSystem)modelContainer.m_Roots[0]).GetChild(1), modelContainer);
            VFXSystem.DisconnectContext(((VFXSystem)modelContainer.m_Roots[1]).GetChild(2), modelContainer);

            Assert.AreEqual(4, modelContainer.m_Roots.Count);
            Assert.AreEqual(1, modelContainer.m_Roots[0].GetNbChildren());
            Assert.AreEqual(2, modelContainer.m_Roots[1].GetNbChildren());
            Assert.AreEqual(2, modelContainer.m_Roots[2].GetNbChildren());
            Assert.AreEqual(1, modelContainer.m_Roots[3].GetNbChildren());

            Object.DestroyImmediate(modelContainer);
        }
    }
}
