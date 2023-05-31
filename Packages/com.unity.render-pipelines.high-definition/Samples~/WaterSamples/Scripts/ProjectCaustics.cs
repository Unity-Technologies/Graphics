using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
public class ProjectCaustics : MonoBehaviour
{
    public Material decal;
    public WaterSurface waterSurface;
    public float regionSize = 20f;

    void Update()
    {
        if(waterSurface.GetCausticsBuffer(out regionSize) != null && decal.GetTexture("_Texture2D") == null)
        {
            decal.SetTexture("_Texture2D", waterSurface.GetCausticsBuffer(out regionSize));
        }
    }
}
