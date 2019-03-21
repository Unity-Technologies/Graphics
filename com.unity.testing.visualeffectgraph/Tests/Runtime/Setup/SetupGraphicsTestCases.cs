using UnityEngine.TestTools;

// Work around case #1033694, unable to use PrebuildSetup types directly from assemblies that don't have special names.
// Once that's fixed, this class can be deleted and the SetupGraphicsTestCases class in Unity.TestFramework.Graphics.Editor
// can be used directly instead.
public class SetupGraphicsTestCases : IPrebuildSetup
{
    public void Setup()
    {
        new UnityEditor.TestTools.Graphics.SetupGraphicsTestCases().Setup();
    }
}
