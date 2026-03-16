using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

#pragma warning disable 618

namespace UnityEditor.Rendering.HighDefinition.Tests
{
    class CustomPostProcessComponentListTest
    {
        [TearDown]
        public void TearDown()
        {
        }

        [Obsolete("Obsolete to strip from builds")]
        class TestComponent : CustomPostProcessVolumeComponent
        {
            public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
            {
                throw new System.NotImplementedException();
            }
        }

        [Obsolete("Obsolete to strip from builds")]
        class TestComponent2 : CustomPostProcessVolumeComponent
        {
            public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
            {
                throw new System.NotImplementedException();
            }
        }

        class TestNoInheritComponent
        {

        }

        [Test]
        public void AddRemoveFunctionalityIsCorrect()
        {
            CustomPostProcessVolumeComponentList list = new(CustomPostProcessInjectionPoint.BeforePostProcess);
            Assert.IsTrue(list.Add<TestComponent>());
            Assert.IsTrue(list.Contains<TestComponent>());
            Assert.IsFalse(list.Add(typeof(TestComponent).AssemblyQualifiedName));
            Assert.IsTrue(list.Remove<TestComponent>());
            Assert.IsFalse(list.Remove(typeof(TestComponent).AssemblyQualifiedName));
        }

        [Test]
        public void AddRemoveFunctionalityIsCorrectWhenAddingNotAValidType()
        {
            CustomPostProcessVolumeComponentList list = new(CustomPostProcessInjectionPoint.BeforePostProcess);
            Assert.IsFalse(list.Add(typeof(TestNoInheritComponent).AssemblyQualifiedName));
        }

        static IEnumerable TestCaseSources()
        {
            yield return new TestCaseData(new List<string>() { typeof(TestComponent).AssemblyQualifiedName, typeof(TestComponent2).AssemblyQualifiedName })
                .SetName("All elements are correct");

            yield return new TestCaseData(new List<string>() { typeof(TestComponent).AssemblyQualifiedName, typeof(TestComponent2).AssemblyQualifiedName, typeof(TestNoInheritComponent).AssemblyQualifiedName })
                .SetName("There is an invalid type");
        }

        [Test]
        [TestCaseSource(nameof(TestCaseSources))]
        public void AddRangeWorksCorrect(List<string> types)
        {
            CustomPostProcessVolumeComponentList list = new(CustomPostProcessInjectionPoint.BeforePostProcess);
            Assert.IsTrue(list.AddRange(types));

            List<Type> actualTypes = new();
            for (int i = 0; i < list.Count; ++i)
            {
                actualTypes.Add(list[i]);
            }

            var expectedTypes = new List<Type>()
                { typeof(TestComponent), typeof(TestComponent2) };

            Assert.AreEqual(expectedTypes, actualTypes);
        }
    }
}
