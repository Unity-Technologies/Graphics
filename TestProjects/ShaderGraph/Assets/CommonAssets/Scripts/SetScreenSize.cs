using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetScreenSize : MonoBehaviour
{
    void Update()
    {
        var renderer = GetComponent<Renderer>();
        var camera = Camera.main;
        var settings = camera.GetComponent<ShaderGraphGraphicsTestSettings>();
        if(settings != null)
        {
            renderer.material.SetFloat("_ScreenHeight", settings.TargetHeight);
            renderer.material.SetFloat("_ScreenWidth", settings.TargetWidth);
        }
    }
}
