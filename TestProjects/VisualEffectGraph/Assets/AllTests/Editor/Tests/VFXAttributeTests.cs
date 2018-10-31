#if !UNITY_EDITOR_OSX
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXAttributeTests
    {
        private abstract class ContextTest : VFXContext
        {
            protected ContextTest(VFXContextType type) : base(type, VFXDataType.kParticle, VFXDataType.kParticle)
            {}

            public override IEnumerable<VFXAttributeInfo> attributes
            {
                get { return attributeInfos; }
            }

            public List<VFXAttributeInfo> attributeInfos = new List<VFXAttributeInfo>();
        }

        private class ContextTestInit : ContextTest
        {
            public ContextTestInit() : base(VFXContextType.kInit) {}
        }
        private class ContextTestOutput : ContextTest
        {
            public ContextTestOutput() : base(VFXContextType.kOutput) {}
        }

        private VFXAttribute Attrib1 = new VFXAttribute("attrib1", VFXValueType.Float);
        private VFXAttribute Attrib2 = new VFXAttribute("attrib2", VFXValueType.Float2);
        private VFXAttribute Attrib3 = new VFXAttribute("attrib3", VFXValueType.Float3);

        private void TestAttributes(Action<VFXGraph> init, Action<VFXData> test)
        {
            var graph = ScriptableObject.CreateInstance<VFXGraph>();
            var initCtx = ScriptableObject.CreateInstance<ContextTestInit>();
            var outputCtx = ScriptableObject.CreateInstance<ContextTestOutput>();

            graph.AddChild(initCtx);
            graph.AddChild(outputCtx);
            initCtx.LinkTo(outputCtx);

            if (init != null)
                init(graph);

            var models = new HashSet<ScriptableObject>();
            graph.CollectDependencies(models);

            var data = models.OfType<VFXData>().First();
            data.CollectAttributes();

            test(data);
        }

        [Test]
        public void TestNoAttributes()
        {
            TestAttributes(
                null,
                (data) => Assert.AreEqual(0, data.GetNbAttributes()));
        }

        [Test]
        public void TestSimpleAttributes()
        {
            TestAttributes(
                (graph) =>
                {
                    ((ContextTest)graph[0]).attributeInfos.Add(new VFXAttributeInfo(Attrib1, VFXAttributeMode.Write));
                    ((ContextTest)graph[0]).attributeInfos.Add(new VFXAttributeInfo(Attrib2, VFXAttributeMode.ReadWrite));
                    ((ContextTest)graph[1]).attributeInfos.Add(new VFXAttributeInfo(Attrib1, VFXAttributeMode.Read));
                    ((ContextTest)graph[1]).attributeInfos.Add(new VFXAttributeInfo(Attrib3, VFXAttributeMode.Read));
                },
                (data) =>
                {
                    Assert.AreEqual(3, data.GetNbAttributes());
                    Assert.AreEqual(VFXAttributeMode.ReadWrite, data.GetAttributeMode(Attrib1));
                    Assert.AreEqual(VFXAttributeMode.ReadWrite, data.GetAttributeMode(Attrib2));
                    Assert.AreEqual(VFXAttributeMode.Read,      data.GetAttributeMode(Attrib3));
                });
        }
    }
}
#endif
