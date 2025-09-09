using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tools
{
    [TestFixture]
    [Category("Graphics Tools")]
    class ReadonlyMaterialConverterTests_MaterialReferenceBuilder
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
                Assert.Ignore("Current pipeline is not URP. Skipping tests");
        }

        public class TestClass_PublicPropertySharedProperty
        {
            public Material Single { get; set; }
            public Material[] Array { get; set; }
            public Material SharedSingle { get; set; }
            public Material[] SharedArray { get; set; }
        }

        [Test]
        public void TypeWithSharedAndNonSharedProperties_OnlySharedOnesAreReturnedOnMaterialAccessor()
        {
            var obj = Activator.CreateInstance(typeof(TestClass_PublicPropertySharedProperty));
            Assert.IsTrue(MaterialReferenceBuilder.TryGetReferenceInfoFromType(obj.GetType(), out var entry));

            Assert.AreEqual(2, entry.materialAccessors.Count);
            Assert.AreEqual("SharedSingle", entry.materialAccessors[0].member.Name);
            Assert.AreEqual("SharedArray", entry.materialAccessors[1].member.Name);
        }
    }

}
