# Assign an ambient occlusion texture

To assign an [ambient occlusion texture](ambient-occlusion-introduction.md) to a GameObject, follow these steps:

1. Use an external software package to create a single-channel ambient occlusion texture that maps the corners and crevices where light is occluded. Use values closer to `0` to indicate more occlusion, and values closer to `1` to indicate less occlusion.

1. Create a [mask map](Mask-Map-and-Detail-Map.md#MaskMap) texture, and use the ambient occlusion texture as the green channel.

1. Import the mask map texture into Unity.

1. Select a material in the **Project** window, then drag the mask map texture into the **Occlusion** (⊙) property of the **Inspector** window.

HDRP also uses the ambient occlusion texture to calculate specular occlusion, by reducing the intensity of reflections in corners.

**Note**: Ambient occlusion in a Lit Shader using [deferred rendering](Forward-And-Deferred-Rendering.md) affects emission due to a technical constraint. Lit Shaders that use [forward rendering](Forward-And-Deferred-Rendering.md) don't have this constraint and don't affect emission.

For more information about ambient occlusion texture properties in an HDRP material, refer to the material in [Materials and surfaces](materials-and-surfaces.md).

## Additional resources

- [Screen space ambient occlusion (SSAO)](Override-Ambient-Occlusion.md)
- [Ray-traced ambient occlusion (RTAO)](Ray-Traced-Ambient-Occlusion.md)
- [Mask and detail maps](Mask-Map-and-Detail-Map.md#MaskMap)
- [Textures](https://docs.unity3d.com/Manual/Textures-landing.html)
