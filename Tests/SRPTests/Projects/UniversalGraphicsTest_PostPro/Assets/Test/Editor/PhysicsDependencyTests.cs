using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools;
using Unity.Graphics.Tests;

namespace UnityEngine.Rendering.Tests
{
    // In order to test whether URP correctly handles the absence of the Physics module, we need to remove the
    // XR testing package that depends on it.
    //
    // We are testing this with two fixtures:
    // * PhysicsDependencyTests_WithXRTestingPackage
    //   - This is the default state
    //   - Tests verify that physics module is installed and local volumes work normally.
    // * PhysicsDependencyTests_WithoutXRTestingPackage
    //   - Fixture will remove the XR Testing Package before running the tests, and add it back after the tests are done.
    //   - Fixture is disabled when we are running XR tests.
    //   - Tests verify that physics module gets automatically disabled, and local volumes are disabled.

    // BUG: There is a bug where domain reload doesn't not work correctly with UnityOneTimeSetup/UnityOneTimeTearDown. Therefore
    // we cannot use them and need to use UnitySetup/UnityTearDown instead. As a consequence, instead of running tests separately,
    // we run all test code inside a single test body to avoid doing the costly recompilation operation multiple times.
    // https://jira.unity3d.com/browse/UUM-114078

#if !(ENABLE_VR && ENABLE_XR_MODULE) // When testing XR, we cannot disable Physics as it's required by the XR testing package.
    [TestFixture]
    public class PhysicsDependencyTests_WithoutPhysics : PhysicsDependencyTests
    {
        const string k_XrTestingPackageName = "com.unity.testing.xr";

        public PhysicsDependencyTests_WithoutPhysics() : base(physicsModuleInstalled: false) { }

        // BUG: This should be UnityOneTimeSetup
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            // In order to test without Physics module installed, we need to remove any packages that depend on it.
            yield return PackageDependencyTestHelper.RemovePackages(k_XrTestingPackageName);
            yield return new WaitForDomainReload();
        }


        // BUG: This should be UnityOneTimeTearDown
        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            // Restore the removed packages afterwards. Note: use relative path
            yield return PackageDependencyTestHelper.AddPackagesRelativePath(k_XrTestingPackageName);
            yield return new WaitForDomainReload();
        }
    }
#endif

    [TestFixture]
    public class PhysicsDependencyTests_WithPhysics : PhysicsDependencyTests
    {
        public PhysicsDependencyTests_WithPhysics() : base(physicsModuleInstalled: true) { }
    }

    public abstract class PhysicsDependencyTests
    {
        public const string k_PhysicsModuleName = "com.unity.modules.physics";

        readonly bool m_PhysicsModuleInstalled;

        public PhysicsDependencyTests(bool physicsModuleInstalled)
        {
            m_PhysicsModuleInstalled = physicsModuleInstalled;
        }

        // BUG: As a workaround for UnityOneTimeSetup/UnityOneTimeTearDown bug, we run all tests in a single test body.
        // If the issue gets fixed, remove this and make the individual tests run separately again.
        // NOTE: Waiting to enable this test once https://jira.unity3d.com/browse/PHYS-443 has landed.
        //[UnityTest]
        public IEnumerator AllTestsCombined()
        {
            yield return PhysicsModulePresence();
            yield return VolumeManager_LocalVolumesBehaveAsExpected();
        }

        // Disabled - see AllTestsCombined()
        //[UnityTest]
        public IEnumerator PhysicsModulePresence()
        {
            bool isPhysicsInstalled = false;
            yield return PackageDependencyTestHelper.IsPackageInstalled(k_PhysicsModuleName, result => isPhysicsInstalled = result);

            if (m_PhysicsModuleInstalled)
                Assert.True(isPhysicsInstalled, "Test is expecting Physics module to be installed.");
            else
                // If this assert starts to fail, it means that something else has introduced a dependency to the Physics module,
                // and it should be investigated if that is expected. If this test project now requires the Physics module, we need to either
                // move this test to a different project, or remove more packages to allow this test to run without Physics module present.
                Assert.False(isPhysicsInstalled, "Physics module should not be installed. Did you introduce a new dependency to Physics module somewhere?");
        }

        // Disabled - see AllTestsCombined()
        //[UnityTest]
        public IEnumerator VolumeManager_LocalVolumesBehaveAsExpected()
        {
            EditorSceneManager.OpenScene("Assets/CommonAssets/Scenes/LocalVolumeTestScene.unity");
            yield return null;

            // Ensure VolumeManager is updated with the main camera position (might not be true depending on editor layout as we are not in Play Mode)
            Assume.That(Camera.main != null);
            VolumeManager.instance.Update(Camera.main.transform, (LayerMask)1);

            var volumeComponent = VolumeManager.instance.stack.GetComponent<TestVolumeComponent>();

            if (m_PhysicsModuleInstalled)
            {
                Assert.True(volumeComponent.parameter.overrideState, "Expected local volumes to be enabled when Physics module is installed.");
                Assert.AreEqual(TestVolumeComponent.k_OverrideValue, volumeComponent.parameter.value);
            }
            else
            {
                Assert.False(volumeComponent.parameter.overrideState, "Expected local volumes to be disabled when Physics module is not installed.");
                Assert.AreEqual(TestVolumeComponent.k_DefaultValue, volumeComponent.parameter.value);
            }
        }
    }


}
