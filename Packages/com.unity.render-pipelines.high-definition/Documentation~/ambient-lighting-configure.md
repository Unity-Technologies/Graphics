# Configure environment lighting

Control how your scene receives light from the environment.

## Make scene elements use the ambient light probe

If you have lightmap textures or Light Probes in your scene, HDRP doesn't use the ambient light probe by default.

To set objects and fog to use the ambient light probe, follow these steps:

1. Select a GameObject, then in the **Mesh Renderer** component disable the GameObject from receiving light from global illumination.
1. In the **Fog** volume override, in the **Volumetric Fog** section,  set **GI Dimmer** to 0.

<a name="DecoupleVisualEnvironment"></a>

## Decouple lighting from the sky

To decouple lighting from the sky, use a lighting override mask. For example, you can do the following:

- Render a dark sky, but calculate brighter lighting on GameObjects so they display clearly. 
- Use a directional light for a moving sun, but a sky background that excludes the sun to avoid double lighting. 

First, create a volume with the sky you want to use for lighting:

1. Create a new sky and fog global volume. From the main menu, select **GameObject** > **Volume** > **Sky and Fog Global Volume**.
1. Select the volume, then use the **Visual Environment** volume override to set the type of sky you want HDRP to use for lighting.
1. At the top of the **Inspector** window, open the **Layers** dropdown and set the volume to a different layer. 

You can now set HDRP to use the layer for lighting, without affecting the sky background:

1. From the main menu, select **Edit** > **Project Settings**.
1. Go to **Quality** > **HDRP**.
1. In the **Lighting** > **Sky** section, set **Lighting Override Mask** to the layer. 

## Additional resources

- [Environment lighting](Environment-lighting.md)
- [Ambient light](https://docs.unity3d.com/Manual/lighting-ambient-light.html)
