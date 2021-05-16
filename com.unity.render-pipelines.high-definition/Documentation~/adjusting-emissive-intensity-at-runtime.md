# Adjusting emissive intensity at runtime

For High Definition Render Pipeline (HDRP) non-Shader Graph shaders, such as the [Lit](Lit-Shader.md), [Unlit](Unlit-Shader.md), and [Decal](Decal-Shader.md) shaders, changing the `_EmissiveIntensity` property does not have any effect at runtime. This is because `_EmissiveIntensity` is not an independent property. The shader only uses `_EmissiveIntensity` to serialize the property in the UI and stores the final result in the `_EmissiveColor` property. To edit the intensity of emissive materials, set the `_EmissiveColor` and multiply it by the intensity you want.

```
using UnityEngine;

public class EditEmissiveIntensityExample : MonoBehaviour
{
    public GameObject m_EmissiveObject;

    void Start()
    {
        float emissiveIntensity = 10;
        Color emissiveColor = Color.green;
        m_EmissiveObject.GetComponent<Renderer>().material.SetColor("_EmissiveColor", emissiveColor * emissiveIntensity);
    }
}
```
