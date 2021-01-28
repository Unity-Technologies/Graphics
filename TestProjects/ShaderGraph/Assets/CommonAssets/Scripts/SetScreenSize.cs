using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetScreenSize : MonoBehaviour
{
    void Start()
    {
        var renderer = GetComponent<Renderer>();
        renderer.material.SetFloat("_ScreenHeight", Screen.height);
        renderer.material.SetFloat("_ScreenWidth", Screen.width);
    }
}
