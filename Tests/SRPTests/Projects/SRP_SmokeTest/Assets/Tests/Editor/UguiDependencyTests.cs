using System.Collections;
using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Unity.Graphics.Tests;

namespace UnityEditor.Rendering.Tests
{
    // Test that URP compiles when uGUI is installed/not installed, and DebugManager throws an exception when trying to open runtime Rendering Debugger.

    // BUG: There is a bug where domain reload doesn't not work correctly with UnityOneTimeSetup/UnityOneTimeTearDown. Therefore
    // we cannot use them and need to use UnitySetup/UnityTearDown instead. As a consequence, instead of running tests separately,
    // we run all test code inside a single test body to avoid doing the costly recompilation operation multiple times.
    // https://jira.unity3d.com/browse/UUM-114078

    [TestFixture]
    public class UguiDependencyTests_WithUgui : UguiDependencyTests
    {
        public UguiDependencyTests_WithUgui() : base(uguiInstalled: true) { }

        // BUG: This should be UnityOneTimeSetup
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            yield return PackageDependencyTestHelper.AddPackages(k_UguiPackageName);
            yield return new WaitForDomainReload();
        }

        // BUG: This should be UnityOneTimeTearDown
        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            yield return PackageDependencyTestHelper.RemovePackages(k_UguiPackageName);
            yield return new WaitForDomainReload();
        }
    }

    [TestFixture]
    public class UguiDependencyTests_WithoutUgui : UguiDependencyTests
    {
        public UguiDependencyTests_WithoutUgui() : base(uguiInstalled: false) { }
    }

    public abstract class UguiDependencyTests
    {
        public const string k_UguiPackageName = "com.unity.ugui";

        readonly bool m_UguiInstalled;

        public UguiDependencyTests(bool uguiInstalled)
        {
            m_UguiInstalled = uguiInstalled;
        }

        // BUG: As a workaround for UnityOneTimeSetup/UnityOneTimeTearDown bug, we run all tests in a single test body.
        // If the issue gets fixed, remove this and make the individual tests run separately again.
        [UnityTest]
        public IEnumerator AllTestsCombined()
        {
            yield return UguiPackagePresence();
            yield return DebugManager_BehavesAsExpected();
        }

        // Disabled - see AllTestsCombined()
        //[UnityTest]
        public IEnumerator UguiPackagePresence()
        {
            bool isUguiInstalled = false;
            yield return PackageDependencyTestHelper.IsPackageInstalled(k_UguiPackageName, result => isUguiInstalled = result);

            if (m_UguiInstalled)
                Assert.True(isUguiInstalled, "Test is expecting uGUI Package to be installed.");
            else
                // If this assert starts to fail, it means that something has introduced a dependency to the uGUI Package,
                // and it should be investigated if that is expected.
                Assert.False(isUguiInstalled, "uGUI Package should not be installed. Did you introduce a new dependency to uGUI somewhere?");
        }

        // Disabled - see AllTestsCombined()
        //[UnityTest]
        public IEnumerator DebugManager_BehavesAsExpected()
        {
            if (m_UguiInstalled)
            {
                bool runtimeUIOpened = false;
                DebugManager.instance.onDisplayRuntimeUIChanged += b => runtimeUIOpened = b;
                DebugManager.instance.displayRuntimeUI = true;
                Assert.True(runtimeUIOpened);
                DebugManager.instance.displayRuntimeUI = false;
            }
            else
            {
                Assert.Throws<System.NotSupportedException>(() =>
                {
                    DebugManager.instance.displayRuntimeUI = true;
                });
            }
            yield return null;
        }
    }
}
