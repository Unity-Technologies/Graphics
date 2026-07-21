using System.Collections;
using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Unity.Graphics.Tests;

namespace UnityEditor.Rendering.Tests
{
    // Test that URP compiles when UIElementsModule is installed/not installed, and DebugManager throws an exception when trying to open runtime Rendering Debugger.

    // BUG: There is a bug where domain reload doesn't not work correctly with UnityOneTimeSetup/UnityOneTimeTearDown. Therefore
    // we cannot use them and need to use UnitySetup/UnityTearDown instead. As a consequence, instead of running tests separately,
    // we run all test code inside a single test body to avoid doing the costly recompilation operation multiple times.
    // https://jira.unity3d.com/browse/UUM-114078

    [TestFixture]
    public class UIElementsDependencyTests_WithUIElements : UIElementsDependencyTests
    {
        public UIElementsDependencyTests_WithUIElements() : base(uiElementsInstalled: true) { }
    }

    // SRP_SmokeTest depends on com.unity.ugui (needed by the sample-import tests for TextMeshPro),
    // which has a hard dependency on com.unity.modules.uielements. The module therefore cannot be
    // removed at runtime, so the "without UIElements" domain-reload scenario cannot be exercised in
    // this project (UnitySetUp's WaitForDomainReload never fires). The UITK-dependency guard is
    // still covered by the WithUIElements fixture and by the equivalent test in projects that do
    // not pull in uGUI.
    [Ignore("com.unity.ugui pulls in com.unity.modules.uielements as a hard dependency in SRP_SmokeTest, so the module cannot be uninstalled to exercise the without-UIElements path.")]
    [TestFixture]
    public class UIElementsDependencyTests_WithoutUIElements : UIElementsDependencyTests
    {
        public UIElementsDependencyTests_WithoutUIElements() : base(uiElementsInstalled: false) { }

        // BUG: This should be UnityOneTimeSetup
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            yield return PackageDependencyTestHelper.RemovePackages(k_UIElementsModuleName);
            yield return new WaitForDomainReload();
        }

        // BUG: This should be UnityOneTimeTearDown
        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            yield return PackageDependencyTestHelper.AddPackages(k_UIElementsModuleName);
            yield return new WaitForDomainReload();
        }
    }

    public abstract class UIElementsDependencyTests
    {
        public const string k_UIElementsModuleName = "com.unity.modules.uielements";

        readonly bool m_UIElementsInstalled;

        public UIElementsDependencyTests(bool uiElementsInstalled)
        {
            m_UIElementsInstalled = uiElementsInstalled;
        }

        // BUG: As a workaround for UnityOneTimeSetup/UnityOneTimeTearDown bug, we run all tests in a single test body.
        // If the issue gets fixed, remove this and make the individual tests run separately again.
        [UnityTest]
        public IEnumerator AllTestsCombined()
        {
            yield return UIElementsModulePresence();
            yield return DebugManager_BehavesAsExpected();
        }

        // Disabled - see AllTestsCombined()
        //[UnityTest]
        public IEnumerator UIElementsModulePresence()
        {
            bool uiElementsInstalled = false;
            yield return PackageDependencyTestHelper.IsPackageInstalled(k_UIElementsModuleName, result => uiElementsInstalled = result);

            if (m_UIElementsInstalled)
                Assert.True(uiElementsInstalled, "Test is expecting UIElementsModule to be installed.");
            else
                // If this assert starts to fail, it means that something has introduced a dependency to the UIElementsModule,
                // and it should be investigated if that is expected.
                Assert.False(uiElementsInstalled, "UIElementsModule should not be installed. Did you introduce a new dependency to UITK somewhere?");
        }

        // Disabled - see AllTestsCombined()
        //[UnityTest]
        public IEnumerator DebugManager_BehavesAsExpected()
        {
            if (m_UIElementsInstalled)
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
