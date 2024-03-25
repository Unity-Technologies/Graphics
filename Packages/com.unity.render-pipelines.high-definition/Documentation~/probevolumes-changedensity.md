# Configure the size and density of Adaptive Probe Volumes

Refer to [Understanding Adaptive Probe Volumes](probevolumes-concept.md) for more information about how Adaptive Probe Volumes work.

## Change the size

To ensure HDRP considers static geometry from all loaded scenes when it places Light Probes, set **Mode** to **Global** in the Adaptive Probe Volume Inspector window so the Adaptive Probe Volume covers the entire scene.

You can also do one of the following in the Inspector of an Adaptive Probe Volume, to set the size of an Adaptive Probe Volume:

- Set **Mode** to **Local** and set the size manually.
- Set **Mode** to **Local** and select **Fit to all Scenes**, **Fit to Scene**, or **Fit to Selection**. Refer to [Adaptive Probe Volume Inspector reference](probevolumes-inspector-reference.md) for more information.
- To exclude certain GameObjects when HDRP calculates Light Probe positions, enable **Override Renderer Filters**. For more information about Layers, refer to [Layers and Layer Masks](https://docs.unity3d.com/Manual/layers-and-layermasks.html).

You can use multiple Adaptive Probe Volumes in a single scene, and they can overlap. However in a Baking Set, HDRP creates only a single Light Probe structure. 

## Adjust Light Probe density

You might need to do the following in your project:

- Increase Light Probe density in highly detailed scenes or areas such as interiors, to get a good lighting result.
- Decrease Light Probe density in empty areas, to avoid those areas using disk space and increasing bake time unnecessarily.

In the [Inspector for an Adaptive Probe Volume](probevolumes-inspector-reference.md), enable and adjust **Override Probe Spacing** to set a minimum and maximum density for the Light Probes in the Adaptive Probe Volume.

The values can't exceed the **Min Probe Spacing** or **Max Probe Spacing** values in the **Probe Placement** section of the [Adaptive Probe Volumes panel](probevolumes-lighting-panel-reference.md), so you might need to adjust these values first.

You can also add local Adaptive Probe Volumes in different areas with different **Override Probe Spacing** values, to control Light Probe density more granularly. For example, in empty areas, add a local Adaptive Probe Volume with a higher **Override Probe Spacing** minimum value, to make sure Light Probes have a lower density in those areas.

If you increase Light Probe density, you might increase bake time and how much disk space your Adaptive Probe Volume uses.

### Decrease Light Probe density for terrain

Because terrain is detailed but less important than your main scenery or characters, you can do the following:

1. Put terrain on its own [Layer](https://docs.unity3d.com/Manual/layers-and-layermasks.html).
2. Surround the terrain with an Adaptive Probe Volume.
3. In the Inspector for the Adaptive Probe Volume, enable **Override Renderer Filters**, then in **Layer Mask** select only your terrain Layer.
4. To adjust Light Probe density to capture more or less lighting detail, enable **Override Probe Spacing** and adjust the values.
