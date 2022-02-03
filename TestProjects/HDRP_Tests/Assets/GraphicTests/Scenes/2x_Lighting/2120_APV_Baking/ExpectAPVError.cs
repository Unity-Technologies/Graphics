using UnityEngine;
using UnityEngine.TestTools;

public class ExpectAPVError : MonoBehaviour
{
    // TODO: When APV is enabled, the lightmapper will always throw an error on project open
    // Once it's fixed, this component + gameobjects that use it + asmdef should be deleted
    void Awake()
    {
        try
        {
            LogAssert.Expect(LogType.Error, "Additional bake inputs will not be processed. Please make sure to enable Baked Global Illumination and select Progressive as the Lightmapper before generating lighting.");
            LogAssert.Expect(LogType.Error, "AdditionalBakedProbes ID 912345678 does not exist/has no data.");
        }
        catch (System.InvalidOperationException) // thrown if there is no logscope
        { }
    }
}
