# Add caustics and check waves and ripples

To add caustics or get information about water surface displacement due to waves and ripples, get buffers from the [`WaterSurface`](xref:UnityEngine.Rendering.HighDefinition.WaterSurface) class:

| Action                      | API                                                               |
|-----------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Add caustics            | [GetCausticsBuffer](../api/UnityEngine.Rendering.HighDefinition.WaterSurface.html#UnityEngine_Rendering_HighDefinition_WaterSurface_GetCausticsBuffer_System_Single__) |
| Check waves and ripples | [GetDeformationBuffer](../api/UnityEngine.Rendering.HighDefinition.WaterSurface.html#UnityEngine_Rendering_HighDefinition_WaterSurface_GetDeformationBuffer)           |

## Example: Add caustics

```
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class WaterCausticsExample : MonoBehaviour
{
    // Reference to the water surface component
    public WaterSurface waterSurface;

    // Size of the region on the water surface from which the caustics effect is calculated
    public float regionSize = 1.0f;

    // Material to apply the caustics effect
    public Material waterMaterial;

    void Start()
    {
        // Get the caustics buffer for the specified region size on the water surface
        Texture causticsBuffer = waterSurface.GetCausticsBuffer(out regionSize);

        if (causticsBuffer != null)
        {
            // Apply the caustics buffer as a texture to the water material
            waterMaterial.SetTexture("_CausticsTex", causticsBuffer);
            Debug.Log("Caustics buffer applied successfully.");
        }
        else
        {
            Debug.LogWarning("Caustics buffer could not be retrieved.");
        }
    }
}
```

## Example: Check waves and ripples

```
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class WaterDeformationExample : MonoBehaviour
{
    // Reference to the water surface component
    public WaterSurface waterSurface;

    // Material to apply the deformation (waves and ripples) effect
    public Material waterMaterial;

    // Shader property name for deformation texture in the water material
    private readonly string _deformationTextureProperty = "_DeformationTex";

    void Start()
    {
        // Get the deformation buffer for the entire water surface
        Texture deformationBuffer = waterSurface.GetDeformationBuffer();

        if (deformationBuffer != null)
        {
            // Apply the deformation buffer as a texture to the water material
            waterMaterial.SetTexture(_deformationTextureProperty, deformationBuffer);
            Debug.Log("Deformation buffer applied successfully.");
        }
        else
        {
            Debug.LogWarning("Deformation buffer could not be retrieved.");
        }
    }

}
```
