using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Tests;

namespace UnityEditor.Rendering.Tests
{
    [TestFixture]
    public class VolumeProfileUtilsTests
    {
        #region CopyValuesToComponent Tests

        [Test]
        public void CopyValuesToComponent_CopiesAllParameters_WhenCopyOnlyOverriddenParamsIsFalse()
        {
            using var componentScope1 = new VolumeComponentScope<CopyPasteTestComponent1>(out var source);
            source.p1.value = 10f;
            source.p1.overrideState = true;
            source.p2.value = 20;
            source.p2.overrideState = false;

            using var componentScope2 = new VolumeComponentScope<CopyPasteTestComponent1>(out var target);
            target.p1.value = 0f;
            target.p2.value = 0;

            VolumeProfileUtils.CopyValuesToComponent(source, target, false);

            Assert.AreEqual(10f, target.p1.value);
            Assert.AreEqual(20, target.p2.value);
        }

        [Test]
        public void CopyValuesToComponent_CopiesOnlyOverriddenParameters_WhenCopyOnlyOverriddenParamsIsTrue()
        {
            using var componentScope1 = new VolumeComponentScope<CopyPasteTestComponent1>(out var source);
            source.p1.value = 10f;
            source.p1.overrideState = true;
            source.p2.value = 20;
            source.p2.overrideState = false;

            using var componentScope2 = new VolumeComponentScope<CopyPasteTestComponent1>(out var target);
            target.p1.value = 0f;
            target.p2.value = 0;

            VolumeProfileUtils.CopyValuesToComponent(source, target, true);

            Assert.AreEqual(10f, target.p1.value);
            Assert.AreEqual(0, target.p2.value);
        }

        [Test]
        public void CopyValuesToComponent_HandlesNullTargetComponent_Gracefully()
        {
            var source = ScriptableObject.CreateInstance<CopyPasteTestComponent1>();
            source.p1.value = 10f;

            Assert.DoesNotThrow(() =>
                VolumeProfileUtils.CopyValuesToComponent(source, null, false));

            ScriptableObject.DestroyImmediate(source);
        }

        #endregion

        #region CreateNewComponent Tests

        [Test]
        public void CreateNewComponent_CreatesComponentWithCorrectType()
        {
            var component = VolumeProfileUtils.CreateNewComponent(typeof(CopyPasteTestComponent1));

            Assert.IsNotNull(component);
            Assert.AreEqual(typeof(CopyPasteTestComponent1), component.GetType());

            ScriptableObject.DestroyImmediate(component);
        }

        [Test]
        public void CreateNewComponent_SetsCorrectHideFlags()
        {
            var component = VolumeProfileUtils.CreateNewComponent(typeof(CopyPasteTestComponent1));

            Assert.IsTrue((component.hideFlags & HideFlags.HideInInspector) != 0);
            Assert.IsTrue((component.hideFlags & HideFlags.HideInHierarchy) != 0);

            ScriptableObject.DestroyImmediate(component);
        }

        [Test]
        public void CreateNewComponent_SetsCorrectName()
        {
            var component = VolumeProfileUtils.CreateNewComponent(typeof(CopyPasteTestComponent1));

            Assert.AreEqual("CopyPasteTestComponent1", component.name);

            ScriptableObject.DestroyImmediate(component);
        }

        #endregion

        #region EnsureAllOverridesForDefaultProfile Tests

        [Test]
        public void EnsureAllOverridesForDefaultProfile_EnablesAllOverrideStates()
        {
            using var volumeProfileScope = new VolumeProfileScope(out var profile);

            var component = profile.Add<CopyPasteTestComponent1>();
            component.p1.overrideState = false;
            component.p2.overrideState = false;

            VolumeProfileUtils.EnsureAllOverridesForDefaultProfile(profile);

            Assert.IsTrue(component.p1.overrideState);
            Assert.IsTrue(component.p2.overrideState);
        }

        [Test]
        public void EnsureAllOverridesForDefaultProfile_ActivatesAllComponents()
        {
            using var volumeProfileScope = new VolumeProfileScope(out var profile);

            var component = profile.Add<CopyPasteTestComponent1>();
            component.active = false;

            VolumeProfileUtils.EnsureAllOverridesForDefaultProfile(profile);

            Assert.IsTrue(component.active);
        }

        [Test]
        public void EnsureAllOverridesForDefaultProfile_UsesDefaultValueSourceWhenProvided()
        {
            using var volumeProfileScope = new VolumeProfileScope(out var profile);
            using var volumeProfileScope2 = new VolumeProfileScope(out var defaultValueSource, "DefaultTestProfile");

            profile.Add<CopyPasteTestComponent1>();

            var defaultComponent = defaultValueSource.Add<CopyPasteTestComponent1>();
            defaultComponent.p1.value = 999f;
            defaultComponent.p1.overrideState = true;

            VolumeProfileUtils.EnsureAllOverridesForDefaultProfile(profile, defaultValueSource);

            Assert.IsTrue(profile.TryGet<CopyPasteTestComponent1>(out var resultComponent));
            Assert.AreEqual(999f, resultComponent.p1.value);
        }

        [Test]
        public void TryEnsureAllOverridesForDefaultProfile_ReturnsTrue_WhenChangesAreMade()
        {
            using var volumeProfileScope = new VolumeProfileScope(out var profile);

            var component = profile.Add<CopyPasteTestComponent1>();
            component.p1.overrideState = false;

            bool result = VolumeProfileUtils.TryEnsureAllOverridesForDefaultProfile(profile);

            Assert.IsTrue(result);
        }

        [Test]
        public void TryEnsureAllOverridesForDefaultProfile_ReturnsFalse_WhenNoChangesNeeded()
        {
            using var volumeProfileScope = new VolumeProfileScope(out var profile);

            VolumeProfileUtils.TryEnsureAllOverridesForDefaultProfile(profile);

            bool result = VolumeProfileUtils.TryEnsureAllOverridesForDefaultProfile(profile);

            Assert.IsFalse(result);
        }

        #endregion

        #region SetComponentEditorsExpanded Tests

        [Test]
        public void SetComponentEditorsExpanded_SetsAllEditorsToExpanded()
        {
            using var volumeProfileScope = new VolumeProfileScope(out var profile);
            profile.Add<CopyPasteTestComponent1>();
            profile.Add<CopyPasteTestComponent2>();

            var editors = profile.components
                .Select(c => (VolumeComponentEditor)Editor.CreateEditor(c, typeof(VolumeComponentEditor)))
                .ToList();

            foreach (var editor in editors)
                editor.expanded = false;

            VolumeProfileUtils.SetComponentEditorsExpanded(editors, true);

            foreach (var editor in editors)
                Assert.IsTrue(editor.expanded);

            foreach (var editor in editors)
                UnityEngine.Object.DestroyImmediate(editor);
        }

        [Test]
        public void SetComponentEditorsExpanded_SetsAllEditorsToCollapsed()
        {
            using var volumeProfileScope = new VolumeProfileScope(out var profile);
            profile.Add<CopyPasteTestComponent1>();
            profile.Add<CopyPasteTestComponent2>();

            var editors = profile.components
                .Select(c => (VolumeComponentEditor)Editor.CreateEditor(c, typeof(VolumeComponentEditor)))
                .ToList();

            foreach (var editor in editors)
                editor.expanded = true;

            VolumeProfileUtils.SetComponentEditorsExpanded(editors, false);

            foreach (var editor in editors)
                Assert.IsFalse(editor.expanded);

            foreach (var editor in editors)
                UnityEngine.Object.DestroyImmediate(editor);
        }

        #endregion

        #region VolumeProfile Hashcode Tests

        [Test]
        public void VolumeProfile_GetComponentListHashCode_IsStable()
        {
            using var volumeProfileScope = new VolumeProfileScope(out var profile);

            profile.Add<CopyPasteTestComponent1>();
            profile.Add<CopyPasteTestComponent2>();

            // Get hash multiple times - should be consistent
            int hash1 = profile.GetComponentListHashCode();
            int hash2 = profile.GetComponentListHashCode();
            int hash3 = profile.GetComponentListHashCode();

            Assert.AreEqual(hash1, hash2, "Hash changed between calls");
            Assert.AreEqual(hash2, hash3, "Hash changed between calls");
        }

        [Test]
        public void VolumeComponent_HashCode_DoesNotChangeWithParameterValues()
        {
            // This test ensures that VolumeComponent.GetHashCode() doesn't change
            // when only parameter values change (not the structure).
            using var componentScope1 = new VolumeComponentScope<CopyPasteTestComponent1>(out var component1);
            using var componentScope2 = new VolumeComponentScope<CopyPasteTestComponent1>(out var component2);

            component1.p1.value = 100f;
            component1.p2.value = 200;

            component2.p1.value = 999f;
            component2.p2.value = 888;

            int initialHash1 = component1.GetHashCode();
            int initialHash2 = component2.GetHashCode();

            component1.p1.value = 555f;
            component2.p2.value = 333;

            int afterChangeHash1 = component1.GetHashCode();
            int afterChangeHash2 = component2.GetHashCode();

            Assert.AreEqual(initialHash1, afterChangeHash1,
                "Component hash should not change when only parameter values change");
            Assert.AreEqual(initialHash2, afterChangeHash2,
                "Component hash should not change when only parameter values change");
        }

        [Test]
        public void VolumeProfile_HashCode_DoesNotChangeWithParameterValues()
        {
            using var volumeProfileScope = new VolumeProfileScope(out var profile);

            var comp1 = profile.Add<CopyPasteTestComponent1>();
            comp1.p1.value = 100f;

            int initialHash = profile.GetHashCode();
            comp1.p1.value = 999f;

            int afterChangeHash = profile.GetHashCode();

            Assert.AreEqual(initialHash, afterChangeHash,
                "Profile hash should not change when only component parameter values change");
        }

        #endregion

        readonly struct VolumeComponentScope<T> : IDisposable
            where T : VolumeComponent
        {
            readonly T m_Component;

            public VolumeComponentScope(out T component)
            {
                component = ScriptableObject.CreateInstance<T>();
                m_Component = component;
            }

            public void Dispose()
            {
                ScriptableObject.DestroyImmediate(m_Component);
            }
        }

        readonly struct VolumeProfileScope : IDisposable
        {
            public readonly VolumeProfile profile;
            readonly string m_AssetPath;

            public VolumeProfileScope(out VolumeProfile outProfile, string assetName = "TestProfile")
            {
                m_AssetPath = $"Assets/{assetName}.asset";
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, m_AssetPath);
                outProfile = profile;
            }

            public void Dispose()
            {
                AssetDatabase.DeleteAsset(m_AssetPath);
            }
        }
    }
}
