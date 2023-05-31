using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[ExecuteAlways]
public class EnableGlobalCustomPassInScene : MonoBehaviour
{
    // Start is called before the first frame update
    void OnEnable()
    {
        foreach (var c in CustomPassVolume.GetGlobalCustomPasses(CustomPassInjectionPoint.BeforePostProcess))
            c.instance.enabled = true;
        foreach (var c in CustomPassVolume.GetGlobalCustomPasses(CustomPassInjectionPoint.AfterPostProcess))
            c.instance.enabled = true;
    }

    // Update is called once per frame
    void OnDisable()
    {
        foreach (var c in CustomPassVolume.GetGlobalCustomPasses(CustomPassInjectionPoint.BeforePostProcess))
            c.instance.enabled = false;
        foreach (var c in CustomPassVolume.GetGlobalCustomPasses(CustomPassInjectionPoint.AfterPostProcess))
            c.instance.enabled = false;
    }
}
