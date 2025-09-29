using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEditor.Rendering.Universal.MaterialReferenceBuilder;

namespace UnityEditor.Rendering.Universal.Tools
{
    [TestFixture]
    [Category("Graphics Tools")]
    class ReadonlyMaterialConverterTests_MaterialReferenceChanger
    {
        #region Types Definition
        public class TestClass_PublicNonSerializedField
        {
            [NonSerialized] public Material Single;
            [NonSerialized] public Material[] Array;
        }

        public class TestClass_PrivateField
        {
            Material Single;
            Material[] Array;
        }

        public class TestClass_NoGetProperties
        {
            private Material m_Single;
            private Material[] m_Array;

            public Material Single { set => m_Single = value; }
            public Material[] Array { set => m_Array = value; }
        }

        public class TestClass_PublicField
        {
            public Material Single;
            public Material[] Array;
        }

        public class TestClass_PrivateSerializedField
        {
            [SerializeField] Material Single;
            [SerializeField] Material[] Array;
        }

        public class TestClass_PrivateProperties
        {
            Material Single { get; set; }
            Material[] Array { get; set; }
        }

        public class TestClass_PublicProperty
        {
            public Material Single { get; set; }
            public Material[] Array { get; set; }
        }

        public class TestClass_PublicPropertyWithBackingField
        {
            Material m_Single;
            Material[] m_Array;

            public Material Single { get => m_Single; set => m_Single = value; }
            public Material[] Array { get => m_Array; set => m_Array = value; }
        }
        #endregion

        Material[] m_Materials;
        Material[] m_ExpectedMaterials;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
                Assert.Ignore("Current pipeline is not URP. Skipping tests");

            m_Materials = ReadonlyMaterialMap.GetBuiltInMaterials();
            Assume.That(m_Materials.Length > 0, "There are no mapping materials");

            m_ExpectedMaterials = new Material[m_Materials.Length];
            for(int i = 0; i < m_Materials.Length; ++i)
            {
                Assert.IsTrue(ReadonlyMaterialMap.TryGetMappingMaterial(m_Materials[i], out var expectedMaterial));
                m_ExpectedMaterials[i] = expectedMaterial;
            }
        }

        public static IEnumerable<TestCaseData> Success_TestCases()
        {
            yield return new TestCaseData(typeof(TestClass_PublicField)).SetName("Public Fields are migrated");
            yield return new TestCaseData(typeof(TestClass_PrivateSerializedField)).SetName("Private Serialized Fields are migrated");
            yield return new TestCaseData(typeof(TestClass_PublicProperty)).SetName("Public Properties are migrated");
            yield return new TestCaseData(typeof(TestClass_PrivateProperties)).SetName("Private Properties are migrated");
            yield return new TestCaseData(typeof(TestClass_PublicPropertyWithBackingField)).SetName("Public Property With Serialized Field are migrated");
        }

        public static IEnumerable<TestCaseData> Failing_TestCases()
        {
            yield return new TestCaseData(typeof(TestClass_PublicNonSerializedField)).SetName("Public Non Fields");
            yield return new TestCaseData(typeof(TestClass_PrivateField)).SetName("Private Fields");
            yield return new TestCaseData(typeof(TestClass_NoGetProperties)).SetName("Setter only Properties ");
        }

        [TestCaseSource(nameof(Failing_TestCases))]
        public void ReassignMaterial_Failing_Tests(Type classType)
        {
            var obj = Activator.CreateInstance(classType);
            Assert.IsFalse(MaterialReferenceBuilder.TryGetReferenceInfoFromType(obj.GetType(), out var entry));
        }

        private bool TryGetMembers(object obj, MaterialReferenceInfo entry, out Func<object> getterArray, out Action<object> setterArray, out Func<object> getterSingle, out Action<object> setterSingle)
        {
            getterArray = null;
            setterArray = null;
            getterSingle = null;
            setterSingle = null;

            bool ok = true;
            foreach (var materialAccessor in entry.materialAccessors)
            {
                ok &= materialAccessor.isArray ?
                    MaterialReferenceBuilder.TryGetFromMemberInfoAccessors(obj, materialAccessor.member, out getterArray, out setterArray) :
                    MaterialReferenceBuilder.TryGetFromMemberInfoAccessors(obj, materialAccessor.member, out getterSingle, out setterSingle);
            }

            return getterArray != null && setterArray != null && getterSingle != null && setterSingle != null;
        }

        private object CreateInstanceWithAllMaterials(Type classType, MaterialReferenceInfo entry, out Func<object> getterArray, out Action<object> setterArray, out Func<object> getterSingle, out Action<object> setterSingle)
        {
            var obj = Activator.CreateInstance(classType);
            Assert.IsTrue(TryGetMembers(obj, entry, out getterArray, out setterArray, out getterSingle, out setterSingle), "Failed to get material accessors");

            var copy = (Material[])m_Materials.Clone();
            // Set initial materials
            setterSingle(copy[0]);
            setterArray(copy);

            return obj;
        }

        [TestCaseSource(nameof(Success_TestCases))]
        public void ReassignMaterial_Success_Tests(Type classType)
        {
            Assert.IsTrue(MaterialReferenceBuilder.TryGetReferenceInfoFromType(classType, out var entry));
            var obj = CreateInstanceWithAllMaterials(classType, entry, out var getterArray, out var setterArray, out var getterSingle, out var setterSingle);
           
            // Perform the reassignment
            using (var materialReferenceChanger = new MaterialReferenceChanger())
            {
                var sb = new StringBuilder();
                bool ok = materialReferenceChanger.ReassignMaterials(obj, null, entry, sb);
                Assert.IsTrue(ok, "Reassing Materials Failed");
            }

            Assert.IsTrue(MaterialReferenceChanger.AreMaterialsEqual(m_ExpectedMaterials, getterArray() as Material[]), "Materials Arrays NOT changed");
            Assert.IsTrue(MaterialReferenceChanger.AreMaterialsEqual(m_ExpectedMaterials[0], getterSingle() as Material), "Material Single NOT changed");
        }

        [TestCaseSource(nameof(Success_TestCases))]
        public void ReassignMaterial_OnPrefabWhenNoOverride(Type classType)
        {
            Assert.IsTrue(MaterialReferenceBuilder.TryGetReferenceInfoFromType(classType, out var entry));
            var obj = CreateInstanceWithAllMaterials(classType, entry, out var getterArray, out var setterArray, out var getterSingle, out var setterSingle);
            var objPrefab = CreateInstanceWithAllMaterials(classType, entry, out var getterPrefabArray, out var setterPrefabArray, out var getterPrefabSingle, out var setterPrefabSingle);

            // Perform the reassignment
            using (var materialReferenceChanger = new MaterialReferenceChanger())
            {
                var sb = new StringBuilder();
                bool ok = materialReferenceChanger.ReassignMaterials(obj, objPrefab, entry, sb);
                Assert.IsTrue(ok, "Reassing Materials Failed");
            }

            // Nothing is changed, as the instance should not change prefab materials.
            var expectedBuiltIn = (Material[])m_Materials.Clone();
            Assert.IsTrue(MaterialReferenceChanger.AreMaterialsEqual(expectedBuiltIn, getterArray() as Material[]), "Materials from the instance have been modified");
            Assert.IsTrue(MaterialReferenceChanger.AreMaterialsEqual(expectedBuiltIn, getterPrefabArray() as Material[]), "Materials from the prefab have NOT been modified");

            Assert.IsTrue(MaterialReferenceChanger.AreMaterialsEqual(expectedBuiltIn[0], getterSingle() as Material), "Materials from the instance have been modified");
            Assert.IsTrue(MaterialReferenceChanger.AreMaterialsEqual(expectedBuiltIn[0], getterPrefabSingle() as Material), "Materials from the prefab have NOT been modified");
        }

        [TestCaseSource(nameof(Success_TestCases))]
        public void ReassignMaterial_OnInstanceWhenOverridingPrefab(Type classType)
        {
            Assert.IsTrue(MaterialReferenceBuilder.TryGetReferenceInfoFromType(classType, out var entry));
            var obj = CreateInstanceWithAllMaterials(classType, entry, out var getterArray, out var setterArray, out var getterSingle, out var setterSingle);
            var objPrefab = CreateInstanceWithAllMaterials(classType, entry, out var getterPrefabArray, out var setterPrefabArray, out var getterPrefabSingle, out var setterPrefabSingle);

            // Prefab will point to 0, and instance to last, changing instance to fake an override from the prefab
            setterSingle(m_Materials[m_Materials.Length - 1]);

            // Change the instance materials to reverse order
            var builtInReversed = (Material[])m_Materials.Clone();
            Array.Reverse(builtInReversed);
            setterArray(builtInReversed);

            // Perform the reassignment
            using (var materialReferenceChanger = new MaterialReferenceChanger())
            {
                var sb = new StringBuilder();
                bool ok = materialReferenceChanger.ReassignMaterials(obj, objPrefab, entry, sb);
                Assert.IsTrue(ok, "Reassing Materials Failed");
            }

            var expectedBuiltIn = (Material[])m_Materials.Clone();

            var reversedExpected = (Material[])m_ExpectedMaterials.Clone();
            Array.Reverse(reversedExpected);

            // As the instance is overriding materials, those must me changed, but not the prefab
            Assert.IsTrue(MaterialReferenceChanger.AreMaterialsEqual(reversedExpected, getterArray() as Material[]), "Materials from the instance have NOT been modified");
            Assert.IsTrue(MaterialReferenceChanger.AreMaterialsEqual(expectedBuiltIn, getterPrefabArray() as Material[]), "Materials from the prefab have been modified");

            Assert.IsTrue(MaterialReferenceChanger.AreMaterialsEqual(reversedExpected[0], getterSingle() as Material), "Materials from the instance have NOT been modified");
            Assert.IsTrue(MaterialReferenceChanger.AreMaterialsEqual(expectedBuiltIn[0], getterPrefabSingle() as Material), "Materials from the prefab have been modified");
        }
    }

}
