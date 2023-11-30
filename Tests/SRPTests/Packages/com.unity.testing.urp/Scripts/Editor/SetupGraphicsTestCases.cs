using UnityEngine.TestTools;

public class SetupGraphicsTestCases : IPrebuildSetup
{
    public void Setup()
    {
#if !OCULUS_SDK && USE_XR_MOCK_HMD
        // First, configure project for XR tests by adding the MockHMD plugin if required.
        Unity.Testing.XR.Editor.SetupMockHMD.SetupLoader();
#endif

        // Work around case #1033694, unable to use PrebuildSetup types directly from assemblies that don't have special names.
        // Once that's fixed, this class can be deleted and the SetupGraphicsTestCases class in Unity.TestFramework.Graphics.Editor
        // can be used directly instead.
        UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.Setup(UniversalGraphicsTests.universalPackagePath);
    }
}
