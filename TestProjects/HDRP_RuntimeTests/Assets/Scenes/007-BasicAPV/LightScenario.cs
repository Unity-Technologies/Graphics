using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class LightScenario : MonoBehaviour
{
    const string scenario1 = "Scenario 1";
    const string scenario2 = "Scenario 2";

    public Color scenario1Color = Color.red;
    public Color scenario2Color = Color.green;

    void OnEnable()
    {
#if UNITY_EDITOR
        Lightmapping.bakeStarted += SetupLight;
#else
        // Ensure Light is baked and not coming from realtime
        if (Application.isPlaying)
            GetComponent<Light>().enabled = false;
#endif
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            ProbeReferenceVolume.instance.SetNumberOfCellsLoadedPerFrame(100);
            ProbeReferenceVolume.instance.lightingScenario = scenario1;
            ProbeReferenceVolume.instance.BlendLightingScenario(scenario2, 0.5f);
        }
    }

#if UNITY_EDITOR
    void OnDisable()
    {
        Lightmapping.bakeStarted -= SetupLight;
    }
#endif

    void SetupLight()
    {
        var color = ProbeReferenceVolume.instance.lightingScenario == scenario1 ? scenario1Color : scenario2Color;
        GetComponent<Light>().color = color;
    }
}
