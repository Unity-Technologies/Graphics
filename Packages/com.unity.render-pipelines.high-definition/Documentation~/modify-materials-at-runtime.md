# Modify materials at runtime

Most HDRP shaders allow you to enable and disable functionalities through the use of Shader keywords.

For example, on the HDRP Lit Shader, normal mapping code is stripped from Materials that don't use a normal map. These Materials don’t include the code for normal mapping, so they're faster to run and compile. The keyword for normal mapping is activated automatically when you use the Material Inspector, but you need to do it explicitly when you use a script.

You can find more information about Material parameters in Unity in the [Material section](https://docs.unity3d.com/Manual/MaterialsAccessingViaScript.html) of the Unity Manual. Information about shader variants, shader keywords and access in standalone builds can be found [here](https://docs.unity3d.com/Manual/shader-variants-and-keywords.html).

## Modifying HDRP Materials in scripts

When you change a Material’s properties in the Inspector, HDRP sets up properties, keywords, and passes on the Material to make sure HDRP can render it correctly. This is called a validation step.
When you use a script to change a Material’s properties, HDRP doesn't perform this step automatically. This means you must validate that Material manually.

### Validating a Material in HDRP

To validate a Material In HDRP, use the function `ValidateMaterial` to  force HDRP to perform a validation step on any Material made from an HDRP Shader or a ShaderGraph that has a HDRP Target.

The following example script:

 * Creates a Material with the [HDRP/Lit](lit-material.md) shader,
 * Enables Alpha Clipping and sets its cutoff value to `0.2`,
 * Uses the `ValidateMaterial` function to enable the Alpha Clipping keywords on the Material.

```C#
using UnityEngine.Rendering.HighDefinition;

public class CreateCutoutMaterial : MonoBehaviour
{
    void Start()
    {
        var material = new Material(Shader.Find("HDRP/Lit"));
        material.SetFloat("_AlphaCutoffEnable", 1.0f);
        material.SetFloat("_AlphaCutoff", 0.2f); // Settings this property is for HDRP
        material.SetFloat("_Cutoff", 0.2f); // Setting this property is for the GI baking system
        HDMaterial.ValidateMaterial(material);
    }
}
```

### HDRP Material API

Some properties of HDRP shaders aren't independent, and they require changes to other properties or keywords to have any effect.
To help modify these properties, HDRP provides a set of functions that will take care of setting all the required states.
You can find a list of available methods in the [Scripting API](xref:UnityEngine.Rendering.HighDefinition.HDMaterial).
Please refer to the documentation to know with which shaders the methods are compatible.

The example script below:

 * Creates a Material with the HDRP/Lit shader,
 * Enables Alpha Clipping and sets its cutoff value to `0.2`. Keywords are automatically set appropriately.

To do this, it uses the following helper functions:

 * material.SetAlphaClipping
 * material.SetAlphaCutoff

```C#
using UnityEngine.Rendering.HighDefinition;

public class CreateCutoutMaterial : MonoBehaviour
{
    void Start()
    {
        var material = new Material(Shader.Find("HDRP/Lit"));
        HDMaterial.SetAlphaClipping(material, true);
        HDMaterial.SetAlphaCutoff(material, 0.2f);
    }
}
```

## Making shader variants available at runtime

Unity uses the set of keywords enabled on a Material to determine which Shader Variant to use. When you build a project, Unity only includes the Shader Variants in the build that the Materials of the project are currently using.
Because Unity has to compile Shader Variants at build time, changing keywords on a Material at runtime can require using a Variant that Unity hasn't built.

To make all Shader Variants you need available at runtime, you need to ensure Unity knows that you need them. There are several ways to do that:

1. You can record the shader variants used during a play session and store them in a **Shader Variant Collection** asset. To do that, navigate to the Project Settings window, open the Graphics tab and select **Save to asset…** This will build a collection containing all Shader Variants currently in use and save them out as an asset. You must then add this asset to the list of Preloaded Shaders for the variants to be included in a build.

2. You can include at least one Material using each variant in your Assets folder. You must use this Material in a scene or place it in your Resources Folder, otherwise Unity ignores this Material when it builds the project.
