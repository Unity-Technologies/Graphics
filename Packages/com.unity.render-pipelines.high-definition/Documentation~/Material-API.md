# Modifying HDRP Materials in C#

When you change a [Material’s properties](https://docs.unity3d.com/Manual/SL-Properties.html) in the Inspector, HDRP automatically validates a material when you change its properties in the inspector. This means that HDRP automatically sets up material properties, keywords, and passes.

However, if you use a script to change the properties of a material made from an HDRP Shader, or a ShaderGraph that has an HDRP target, HDRP doesn’t validate the material automatically.

When a material in your scene isn't valid, it might make a property change have no effect or cause the material to fail to render.

To do this, when you modify an HDRP material in C#:

- [Use the HDMaterial API](#hdmaterial) to change a property and validate it if it includes a method for the property the shader uses.
- [Use ValidateMaterial](#validatematerial) after you change a property manually to validate the material. Use this method when `HDMaterial` doesn't include a method for the property the shader uses.

You can find more information about Material properties in Unity in the [Material section of the Unity Manual](https://docs.unity3d.com/Manual/MaterialsAccessingViaScript.html). For more information about shader variants, shader keywords, and access in standalone builds, see [Branching, variants, and keywords](https://docs.unity3d.com/Manual/shader-variants-and-keywords.html).

<a name="hdmaterial"></a>

## Modify a Material with the HDMaterial API

To modify a Material property, use the methods in the `HDMaterial` class. When you use a method in `HDMaterial` to change a property, it automatically validates the Material.

The `HDMaterial` class contains methods that correspond to certain material properties. For a full list of `HDMaterial` methods, see the [HDMaterial API documentation](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/api/UnityEngine.Rendering.HighDefinition.HDMaterial.html).

The following example script: 

- Creates a Material with the [HDRP/Lit](Lit-Shader.md) shader. 
- Uses `HDmaterial.SetAlphaClipping` to enable alpha clipping.
- Uses `HDmaterial.SetAlphaCutoff` to set the cutoff value to 0.2. 
- Automatically sets keywords appropriately because it uses the `HDmaterial` API.

```c#
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

<a name="validatematerial"></a>

## Modify and validate a Material with ValidateMaterial

To validate a Material property which isn’t included in `HDMaterial`, use the `ValidateMaterial` method to force HDRP to validate the material.

The following example script: 

- Creates a Material with the [HDRP/Lit](Lit-Shader.md) shader.
- Uses `_AlphaCutoffEnable` to enable alpha clipping.
- Uses  "_AlphaCutoff" to set the cutoff value to 0.2.
- Uses the `ValidateMaterial` function to validate this Material.

```C#
using UnityEngine.Rendering.HighDefinition;

public class CreateCutoutMaterial : MonoBehaviour

{

   void Start()

  {

       var material = new Material(Shader.Find("HDRP/Lit"));

       material.SetFloat("_AlphaCutoffEnable", 1.0f);

       material.SetFloat("_AlphaCutoff", 0.2f);

      HDMaterial.ValidateMaterial(material);

  }

}
```