using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using UnityEngine.Rendering.HighDefinition;


public class WaterSurfaceBinder : VFXBinderBase
{
    public WaterSurface waterSurface;

    public override bool IsValid(VisualEffect component)
    {
        return waterSurface != null;
    }

    public override void UpdateBinding(VisualEffect component)
    {
        waterSurface.SetGlobalTextures();
    }

    public override string ToString()
    {
        return string.Format($"Water Surface : '{(waterSurface == null ? "null" : waterSurface.gameObject.name)}'");
    }
}
