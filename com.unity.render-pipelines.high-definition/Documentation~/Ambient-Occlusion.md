# Ambient occlusion

The High Definition Render Pipeline (HDRP) uses ambient occlusion to approximate ambient light on a GameObject’s surface that has been cast by details present in the Material but not the surface geometry. Since these details do not exist on the model, you must provide an ambient occlusion Texture for HDRP to occlude indirect lighting (lighting from Lightmaps, [Light Probes](https://docs.unity3d.com/Manual/LightProbes.html) or Ambient Light Probes). HDRP also uses the ambient occlusion Texture to calculate specular occlusion. It calculates specular occlusion from the Camera's view vector and the ambient occlusion Texture in order to dim reflections in cavities.

To generate an ambient occlusion Texture, you can use external software like:

* xNormal
* Substance Designer or Painter
* Knald

When authoring ambient occlusion Textures, be aware that a value of 0 specifies an area that is fully occluded and a value of 1 specifies an area that is fully visible.

When you create the Texture, you must apply it to a Material. To do this, you must use the green channel of a [mask map](Mask-Map-and-Detail-Map.html#MaskMap).

Note: Ambient occlusion in a Lit Shader using [deferred rendering](Forward-And-Deferred-Rendering.html) affects emission due to a technical constraint. Lit Shaders that use [forward rendering](Forward-And-Deferred-Rendering.html) do not have this constraint and do not affect emission.

## Properties

The ambient occlusion properties are in the **Surface Inputs** drop-down of your Shader.

| Property                        | Description                                                  |
| ------------------------------- | ------------------------------------------------------------ |
| **Mask Map - Green channel**   | Assign the ambient occlusion map in the green channel of the **Mask Map** Texture. HDRP uses the green channel of this map to calculate ambient occlusion. |
| **Ambient Occlusion Remapping** | Remaps the ambient occlusion map in the green channel of the **Mask Map** between the minimum and maximum values you define on the slider. These values are between 0 and 1.<br/>&#8226; Drag the left handle to the right to make the ambient occlusion more subtle.<br/>&#8226; Drag the right handle to the left to apply ambient occlusion to the whole Material. This is useful when the GameObject this Material is on is occluded by a dynamic GameObject.<br/>This property only appears when you assign a Texture to the **Mask Map**. |
