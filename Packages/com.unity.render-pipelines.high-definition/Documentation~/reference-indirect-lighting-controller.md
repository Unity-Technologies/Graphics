# Indirect lighting controller reference

[!include[](snippets/Volume-Override-Enable-Properties.md)]

| Property                                         | Description                                                  |
| ------------------------------------------------ | ------------------------------------------------------------ |
| **Indirect Diffuse Lighting Multiplier**         | A multiplier for lightmaps, Light Probes, Light Probe Volumes, Screen-Space Global Illumination, and [Ray-Traced Global Illumination](Ray-Traced-Global-Illumination.md). HDRP multiplies the light data from these by this value. |
| **Indirect Diffuse Rendering Layer Mask**        | Specifies the [Rendering Layer Mask](Rendering-Layers.md) for indirect diffuse lighting multiplier. If you enable Light Layers, you can use them to decouple Meshes in your Scene from the above multiplier. |
| **Reflection Lighting Multiplier**               | A multiplier for baked, realtime, custom [Reflection Probes](Reflection-Probe.md) and [Planar Probes](Planar-Reflection-Probe.md), [Screen-Space Reflection](Override-Screen-Space-Reflection.md), [Ray-Traced Reflection](Ray-Traced-Reflections.md), and Sky Reflection. HDRP multiplies the light data from these by this value. |
| **Reflection Rendering Layer Mask**              | Specifies the [Rendering Layer Mask](Rendering-Layers.md) for reflection lighting. If you enable Light Layers, you can use them to decouple Meshes in your Scene from the above multiplier. |
| **Reflection/Planar Probe Intensity Multiplier** | A multiplier for baked, realtime, and custom [Reflection Probes](Reflection-Probe.md) and [Planar Probes](Planar-Reflection-Probe.md). HDRP multiplies the Reflection Probe data by this value. |