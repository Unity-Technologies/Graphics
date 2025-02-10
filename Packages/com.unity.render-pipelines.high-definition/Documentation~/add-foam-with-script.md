# Add foam with a script

To add caustics or foam, or get information about water surface displacement due to waves and ripples, get buffers from the [GetFoamBuffer](../api/UnityEngine.Rendering.HighDefinition.WaterSurface.html#UnityEngine_Rendering_HighDefinition_WaterSurface_GetFoamBuffer_UnityEngine_Vector2__) API.

## Example: Add foam

```
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class WaterFoamExample : MonoBehaviour
{
    // Reference to the water surface component
    public WaterSurface waterSurface;

    // The area of the water surface where the foam buffer should be queried
    public Vector2 foamArea;

    // Material to apply the foam effect
    public Material waterMaterial;

    // Shader property name for foam texture in the water material
    private readonly string _foamTextureProperty = "_FoamTex";

    void Start()
    {
        // Get the foam buffer for the specified 2D area on the water surface
        Texture foamBuffer = waterSurface.GetFoamBuffer(out foamArea);

        if (foamBuffer != null)
        {
            // Apply the foam buffer as a texture to the water material
            waterMaterial.SetTexture(_foamTextureProperty, foamBuffer);
            Debug.Log("Foam buffer applied successfully.");
        }
        else
        {
            Debug.LogWarning("Foam buffer could not be retrieved.");
        }
    }
}
```
