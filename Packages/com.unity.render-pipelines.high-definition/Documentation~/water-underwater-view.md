# Underwater view

The underwater view is rendered as a full-screen post-processing effect. The view is rendered in one of the following ways:

* Using a simple analytic formula to estimate light absorption by the water volume.

* If volumetric fog is enabled, the underwater view is included in the volumetric buffer and rendered using volumetric lighting, supporting light shafts and light shafts from shadows.

To view non-infinite water surfaces from underwater, you have to specify a [collider](https://docs.unity3d.com/Manual/Glossary.html#Collider). You can either use the box collider HDRP automatically provides or select a box collider in the scene to use for this purpose.

To view infinite water surfaces from underwater, you have to specify a **Volume Depth**. The **Volume Depth** property is only available in Ocean water body types, so this feature is limited to underwater views of infinite Ocean surfaces.

# Water line

When the camera is at the limit of the water's surface, the underwater view adds a boundary when transitioning from below to above the water's surface. 

![](Images/water-waterline-raw.png)

To customize the water line even more, you can sample the generated underwater buffer in a [Custom Pass](Custom-Pass.md) by using the [HD Sample Buffer](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/HD-Sample-Buffer-Node.html) node from the Shader Graph using the **IsUnderwater** option from the Source Buffer dropdown.

Refer to the Waterline scene in the [HDRP Water samples](HDRP-Sample-Content.md#water-samples) for more details.

![](Images/water-sample-buffer-underwater.png)

## Limitations

* When using a custom mesh, underwater doesn't behave as expected if the mesh's Y position isn't at 0, or if the mesh isn't flat.
* The **Receive Fog** option on transparent materials also disables underwater. This can be useful to disable absorption on objects when using excluder underwater (like a porthole in the hold of a boat), or as an optimization when you know that fog doesn't affect the object's color.

# Additional resources
* [Settings and properties related to the water system](settings-and-properties-related-to-the-water-system.md)
