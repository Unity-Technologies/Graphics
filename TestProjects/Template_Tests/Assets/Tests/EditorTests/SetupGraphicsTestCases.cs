using UnityEngine.TestTools;

// Work around case #1033694, unable to use PrebuildSetup types directly from assemblies that don't have special names.
// Once that's fixed, this class can be deleted and the SetupGraphicsTestCases class in Unity.TestFramework.Graphics.Editor
// can be used directly instead.
public class SetupGraphicsTestCases : IPrebuildSetup
{
#if UNITY_2020_3
    private const string UniversalPackagePath = "Assets/ReferenceImages/2020_3";
#elif UNITY_2021_3
    private const string UniversalPackagePath = "Assets/ReferenceImages/2021_3";
#elif UNITY_2022_1
    private const string UniversalPackagePath = "Assets/ReferenceImages/2022_1";
#elif UNITY_2022_2
    private const string UniversalPackagePath = "Assets/ReferenceImages/2022_2";
#else
    private const string UniversalPackagePath = "Assets/ReferenceImages/trunk";
#endif

    public void Setup()
    {
        UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.Setup(UniversalPackagePath);
    }
}
