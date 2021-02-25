#if UITK_ENABLE_GFX_TESTS
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;

// There's an issue with the test framework where it won't call  the static method
// defined by [PrebuildSetup("SetupGraphicsTestCases")]. This seems related to case 1033694.
// We provide our own prebuild setup to work around this issue.
public class SetupGraphicsTestCases : IPrebuildSetup
{
    public void Setup()
    {
        UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.Setup(Tests.UIElementsGraphicsTests.referenceImagesPath);
    }
}
#endif
