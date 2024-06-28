using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
public class FoamBuffer : MonoBehaviour
{
    public WaterSurface waterSurface;

    // Update is called once per frame
    void Update()
    {
        Vector2 foamArea;
        this.GetComponent<DecalProjector>().material.SetTexture("_Base_Color", waterSurface.GetFoamBuffer(out foamArea));
    }
}
