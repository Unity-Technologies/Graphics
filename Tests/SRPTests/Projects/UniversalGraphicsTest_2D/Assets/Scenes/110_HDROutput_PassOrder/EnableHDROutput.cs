using TestRuntime.FakingHDR;
using UnityEngine;

public class EnableHDROutput : MonoBehaviour
{
    public void OnEnable()
    {
        HDREmulation.SetFakeHDROutputEnabled(true);
    }

    public void OnDisable()
    {
        HDREmulation.SetFakeHDROutputEnabled(false);
    }
}
