# Material Scripting API

All the parameters of a material asset that you see in the Inspector window are accessible via script, giving you the power to change or animate how a material works at runtime.

You can find more information in the [Material section of the Unity Manual](https://docs.unity3d.com/Manual/MaterialsAccessingViaScript.html).

## Modifying HDRP materials in scripts

When modifying a material via the Inspector, HDRP runs a validation step to setup properties, keywords and passes on the material to ensure it is in a valid state for rendering.
When modifying a material via scripts, this validation is not done automatically, so it must be performed manually.

HDRP provides a function [ValidateMaterial](../api/UnityEngine.Rendering.HighDefinition.HDMaterial.html#UnityEngine_Rendering_HighDefinition_HDMaterial_ValidateMaterial) that will setup any material made from an HDRP Shader or a ShaderGraph with an HDRP Target.

This examples creates a material with the HDRP/Lit shader and enables Alpha Clipping with a cutoff value of `0.2`:

```csharp
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

## HDRP Material API

However, some properties of HDRP shaders are not independent, and they require changes to other properties in order to have any effect.
To help modifying these properties, HDRP provides a set of functions that will take care of setting all the required states.

This list of available methods is in the [Scripting API](../api/UnityEngine.Rendering.HighDefinition.HDMaterial.html).
Refer to the documentation to know with which shaders the function is compatible.

This is the same example as above but using the helper functions:

```csharp
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

## Changing keyword state at runtime

To enable and disable some features, HDRP Shaders make use of [Shader Variants](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html).

For example, if you choose to assign a Normal Map to your material, you need to activate the variant of the shader which supports Normal Mapping.
This enables more efficient shaders as the code for normal mapping is not run if it's not used by a material. Additionally, when building a project, Unity will not include any variant that is not in use, thus reducing build time. As a result, these variants cannot be activated at runtime.

To ensure all variants you need will be available at runtime, you need to make sure Unity knows you need them by including at least one Material using each variant in your Assets. The material must be used in a scene or alternatively be placed in your [Resources Folder](https://docs.unity3d.com/Manual/LoadingResourcesatRuntime.html), otherwise Unity will still omit it from your build, because it appeared unused.

Another option is to use [Shader Variant Collections](https://docs.unity3d.com/ScriptReference/ShaderVariantCollection.html).
You can either manually build the collection using the inspector, or record the shader variants used during a play session. To do that, click on the button "Save to asset..." in the **Graphics** tab of the **Project Settings** window. This will build a collection containing all Shader Variants currently in use and save them out as an asset. You must then add the asset to the list of Preloaded Shaders in the **Graphics** settings for the variants to be included in a build.
