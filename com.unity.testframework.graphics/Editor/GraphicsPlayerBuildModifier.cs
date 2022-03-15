using UnityEditor;
using UnityEditor.TestTools;
using UnityEngine.TestTools.Graphics;

[assembly: TestPlayerBuildModifier(typeof(GraphicsPlayerBuildModifier))]
public class GraphicsPlayerBuildModifier : ITestPlayerBuildModifier
{
    public BuildPlayerOptions ModifyOptions(BuildPlayerOptions playerOptions)
    {
        // Add an extra define to the player so that XR test code can be enabled while still using the regular (non-VR) reference images
        if (RuntimeSettings.reuseTestsForXR)
            AddExtraScriptingDefine(ref playerOptions, "XR_REUSE_TESTS_STANDALONE");

        return playerOptions;
    }

    private void AddExtraScriptingDefine(ref BuildPlayerOptions playerOptions, string extraScriptingDefine)
    {
#if UNITY_2020_1_OR_NEWER
        if (playerOptions.extraScriptingDefines != null)
        {
            string[] extraScriptingDefines = new string[1 + playerOptions.extraScriptingDefines.Length];
            playerOptions.extraScriptingDefines.CopyTo(extraScriptingDefines, 0);
            extraScriptingDefines[playerOptions.extraScriptingDefines.Length] = extraScriptingDefine;

            playerOptions.extraScriptingDefines = extraScriptingDefines;
        }
        else
        {
            playerOptions.extraScriptingDefines = new string[] { extraScriptingDefine };
        }
#endif
    }
}
