using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Tests;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.Tests
{
    [TestFixture]
    public class VolumeProfileGlobalDefaultTests
    {
        VolumeManager m_VolumeManager;
        List<string> m_AssetsToDelete;

        readonly string m_AssetPath = $"Assets/{nameof(TestRenderPipelineAssetForVolume)}.asset";
        RenderPipelineAsset m_RenderPipelineAsset;

        [UnityOneTimeSetUp]
        public IEnumerator OneTimeSetup()
        {
            m_RenderPipelineAsset = ScriptableObject.CreateInstance<TestRenderPipelineAssetForVolume>();
            AssetDatabase.CreateAsset(m_RenderPipelineAsset, m_AssetPath);
            GraphicsSettings.defaultRenderPipeline = m_RenderPipelineAsset;
            Assume.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<TestRenderPipelineAssetForVolume>());

            var camera = new GameObject("TestCamera").AddComponent<Camera>();
            camera.Render();
            yield return null;
            Object.DestroyImmediate(camera.gameObject);
            Assume.That(RenderPipelineManager.currentPipeline, Is.InstanceOf<TestRenderPipeline>());

            m_VolumeManager = VolumeManager.instance;
            Assume.That(m_VolumeManager, Is.Not.Null);
        }

        [UnityOneTimeTearDown]
        public IEnumerator OneTimeTearDown()
        {
            GraphicsSettings.defaultRenderPipeline = null;
            AssetDatabase.DeleteAsset(m_AssetPath);

            Assume.That(m_VolumeManager, Is.Not.Null);
            if (m_VolumeManager.isInitialized)
                m_VolumeManager.Deinitialize();

            yield return null;
        }

        [SetUp]
        public void SetUp()
        {
            if (m_VolumeManager.isInitialized)
                m_VolumeManager.Deinitialize();

            m_AssetsToDelete = new List<string>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var assetPath in m_AssetsToDelete)
            {
                if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(assetPath) != null)
                    AssetDatabase.DeleteAsset(assetPath);
            }

            m_AssetsToDelete.Clear();
        }

        VolumeProfile CreateProfileAsset(string name, params Type[] componentTypes)
        {
            string assetPath = $"Assets/{name}.asset";
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, assetPath);

            foreach (var type in componentTypes)
            {
                profile.Add(type);
            }

            AssetDatabase.SaveAssets();
            m_AssetsToDelete.Add(assetPath);
            return profile;
        }

        [Test]
        public void UpdateGlobalDefaultVolumeProfile_HandlesProfileSwitch()
        {
            // Create first profile with components 1 and 2
            var profile1 = CreateProfileAsset(
                "DefaultProfile_Switch1",
                typeof(CopyPasteTestComponent1),
                typeof(CopyPasteTestComponent2)
            );

            // Initialize with first profile
            m_VolumeManager.Initialize(profile1);
            var initialTypes = m_VolumeManager.baseComponentTypeArray;

            // Create second profile with components 2 and 3 (different set)
            var profile2 = CreateProfileAsset(
                "DefaultProfile_Switch2",
                typeof(CopyPasteTestComponent2),
                typeof(CopyPasteTestComponent3)
            );

            // Switch to second profile
            VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<TestRenderPipeline>(profile2);

            // Verify VolumeManager updated correctly
            Assert.IsTrue(m_VolumeManager.isInitialized);
            var newTypes = m_VolumeManager.baseComponentTypeArray;
            Assert.IsNotNull(newTypes);

            Assert.That(newTypes.Length, Is.EqualTo(initialTypes.Length));
            // Verify components are still properly sorted after switch
            for (int i = 1; i < newTypes.Length; i++)
            {
                Assert.That(newTypes[i], Is.EqualTo(initialTypes[i]),
                    $"Types not sorted the same way after profile switch. This could break existing VolumeStack.");
            }
        }

        [Test]
        public void UpdateGlobalDefaultVolumeProfile_VolumeManagerUpdateDoesNotThrow()
        {
            // Create and set default profile
            var profile = CreateProfileAsset(
                "DefaultProfile_NoThrow",
                typeof(CopyPasteTestComponent1),
                typeof(CopyPasteTestComponent2)
            );

            m_VolumeManager.Initialize(profile);

            // Create a camera for volume update
            var cameraGO = new GameObject("TestCamera");
            var camera = cameraGO.AddComponent<Camera>();
            LayerMask layerMask = 1;

            try
            {
                // This should not throw with the bug fixes
                Assert.DoesNotThrow(() =>
                {
                    m_VolumeManager.Update(camera.transform, layerMask);
                }, "VolumeManager.Update should not throw after setting global default profile");

                // Verify stack is valid
                Assert.IsNotNull(m_VolumeManager.stack);
            }
            finally
            {
                Object.DestroyImmediate(cameraGO);
            }
        }

        [Test]
        public void UpdateGlobalDefaultVolumeProfile_MultipleUpdatesSameProfile()
        {
            // Create profile
            var profile = CreateProfileAsset(
                "DefaultProfile_MultiUpdate",
                typeof(CopyPasteTestComponent1)
            );

            m_VolumeManager.Initialize(profile);

            // Update multiple times - should not cause issues
            Assert.DoesNotThrow(() =>
            {
                VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<TestRenderPipeline>(profile);
                VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<TestRenderPipeline>(profile);
                VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<TestRenderPipeline>(profile);
            }, "Multiple updates with same profile should not throw");

            Assert.IsTrue(m_VolumeManager.isInitialized);
        }

        [Test]
        public void SwitchingProfiles_WithDifferentComponents_MaintainsStability()
        {
            var cameraGO = new GameObject("TestCamera");
            var camera = cameraGO.AddComponent<Camera>();
            LayerMask layerMask = 1;

            try
            {
                // Profile A: Components 1, 2
                var profileA = CreateProfileAsset(
                    "ProfileA",
                    typeof(CopyPasteTestComponent1),
                    typeof(CopyPasteTestComponent2)
                );

                m_VolumeManager.Initialize(profileA);
                m_VolumeManager.Update(camera.transform, layerMask);

                var typesA = m_VolumeManager.baseComponentTypeArray.ToList();
                Assume.That(typesA.Count, Is.GreaterThan(0));

                // Profile B: Components 2, 3 (partial overlap)
                var profileB = CreateProfileAsset(
                    "ProfileB",
                    typeof(CopyPasteTestComponent2),
                    typeof(CopyPasteTestComponent3)
                );

                VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<TestRenderPipeline>(profileB);

                // Update should still work without errors
                Assert.DoesNotThrow(() =>
                {
                    m_VolumeManager.Update(camera.transform, layerMask);
                }, "Update after profile switch should not throw");

                var typesB = m_VolumeManager.baseComponentTypeArray.ToList();
                Assume.That(typesB.Count, Is.GreaterThan(0));

                // Profile C: Components 1, 3 (no overlap with B except potentially base types)
                var profileC = CreateProfileAsset(
                    "ProfileC",
                    typeof(CopyPasteTestComponent1),
                    typeof(CopyPasteTestComponent3)
                );

                VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<TestRenderPipeline>(profileC);

                // Update should still work
                Assert.DoesNotThrow(() =>
                {
                    m_VolumeManager.Update(camera.transform, layerMask);
                }, "Update after second profile switch should not throw");

                // Back to Profile A
                VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<TestRenderPipeline>(profileA);

                Assert.DoesNotThrow(() =>
                {
                    m_VolumeManager.Update(camera.transform, layerMask);
                }, "Update after switching back to original profile should not throw");

                var typesAAfter = m_VolumeManager.baseComponentTypeArray;
                Assert.IsNotNull(typesAAfter);
            }
            finally
            {
                Object.DestroyImmediate(cameraGO);
            }
        }

        [Test]
        public void EnsureAllOverridesForDefaultProfile_HandlesComponentAddition()
        {
            // Create profile with one component
            var profile = CreateProfileAsset(
                "ProfileForEnsure",
                typeof(CopyPasteTestComponent1)
            );

            int initialComponentCount = profile.components.Count;

            // Create a default value source with additional components
            var defaultSource = CreateProfileAsset(
                "DefaultSource",
                typeof(CopyPasteTestComponent1),
                typeof(CopyPasteTestComponent2),
                typeof(CopyPasteTestComponent3)
            );

            // This should add missing components to the profile
            VolumeProfileUtils.EnsureAllOverridesForDefaultProfile(profile, defaultSource);

            Assert.IsNotNull(profile.components);
            Assert.Greater(profile.components.Count, initialComponentCount);
        }

        [Test]
        public void VolumeStack_RemainsValid_AfterProfileSwitch()
        {
            var cameraGO = new GameObject("TestCamera");
            var camera = cameraGO.AddComponent<Camera>();
            LayerMask layerMask = 1;

            try
            {
                // Initial profile
                var profile1 = CreateProfileAsset(
                    "StackValidProfile1",
                    typeof(CopyPasteTestComponent1)
                );

                m_VolumeManager.Initialize(profile1);
                m_VolumeManager.Update(camera.transform, layerMask);

                var stack = m_VolumeManager.stack;
                Assert.IsNotNull(stack);

                // Switch profile
                var profile2 = CreateProfileAsset(
                    "StackValidProfile2",
                    typeof(CopyPasteTestComponent2)
                );

                VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<TestRenderPipeline>(profile2);
                m_VolumeManager.Update(camera.transform, layerMask);

                // Stack should still be valid
                Assert.IsNotNull(m_VolumeManager.stack);

                // Should be able to update again without issues
                Assert.DoesNotThrow(() =>
                {
                    m_VolumeManager.Update(camera.transform, layerMask);
                    m_VolumeManager.Update(camera.transform, layerMask);
                });
            }
            finally
            {
                Object.DestroyImmediate(cameraGO);
            }
        }
    }
}
