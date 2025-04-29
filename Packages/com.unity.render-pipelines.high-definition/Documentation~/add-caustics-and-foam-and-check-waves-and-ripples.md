# Check waves and ripples

To get information about water surface displacement due to waves and ripples, use the [GetDeformationBuffer](../api/UnityEngine.Rendering.HighDefinition.WaterSurface.html#UnityEngine_Rendering_HighDefinition_WaterSurface_GetDeformationBuffer) API.

## Example

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
