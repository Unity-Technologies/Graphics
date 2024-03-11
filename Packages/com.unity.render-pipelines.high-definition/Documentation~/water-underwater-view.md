# Underwater view

Underwater is rendered as a fullscreen postprocess effect. It uses either a simple analytic formula to estimate light absorption by the water volume, or if volumetric fog is enabled, it will be included in the vbuffer and rendered using volumetric lighting, supporting god rays and light shafts from shadows.

To view non-infinite water surfaces from underwater, you have to specify a [collider](https://docs.unity3d.com/Manual/Glossary.html#Collider). You can either use the box collider HDRP automatically provides or select a box collider in the scene to use for this purpose.

To view infinite water surfaces from underwater, you have to specify a **Volume Depth**.

# Water line

When the camera is at the limit of the water surface, the underwater view adds a boundary when transitioning from below to above the water's surface. 

![](Images/water-waterline-raw.png)

Additionaly, to customize even more the water line, you can sample the generated underwater buffer in a [Custom Pass](Custom-Pass.md) by using the [HD Sample Buffer](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/HD-Sample-Buffer-Node.html) node from the Shader Graph using the "IsUnderwater" option from the Source Buffer dropdown.

See the Waterline scene in the [HDRP Water samples](HDRP-Sample-Content.md#water-samples) for more details.

![](Images/water-sample-buffer-underwater.png)

## Limitations

* When using a custom mesh, underwater will not behave as expected if mesh is not at 0, or if the mesh isn't flat.
* The **Receive Fog** option on transparent materials will also disable underwater. This can be useful to disable absorption on objects when using excluder underwater (like a porthole in the hold of a boat), or simply as an optimization when it is known that fog will not affect the object color.

# Additional resources
* [Settings and properties related to the Water System](settings-and-properties-related-to-the-water-system.md)
