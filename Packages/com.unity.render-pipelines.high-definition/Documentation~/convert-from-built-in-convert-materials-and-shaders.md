# Convert materials and shaders

To upgrade the Materials in your Scene to HDRP-compatible Materials:

1. Go to **Edit** > **Rendering** > **Materials**
2. Choose one of the following options:

    * **Convert All Built-in Materials to HDRP**: Converts every compatible Material in your Project to an HDRP Material.
    * **Convert Selected Built-in Materials to HDRP**: Converts every compatible Material currently selected in the Project window to an HDRP Material.
    * **Convert Scene Terrains to HDRP Terrains**: Replaces the built-in default standard terrain Material in every [Terrain](https://docs.unity3d.com/Manual/script-Terrain.html) in the scene with HDRP default Terrain Material.

## Limitations

The automatic upgrade options described above can't upgrade all Materials to HDRP correctly:

* You can't automatically upgrade custom Materials or Shaders to HDRP. You must [convert custom Materials and Shaders manually](#ManualConversion).
* HDRP can only convert materials from the **Assets** folder of your project. HDRP uses the [error shader](xref:shader-error) for GameObjects that use the default read-only material from the Built-In Render Pipeline, for example [primitives](xref:um-primitive-objects).  
* Height mapped Materials might look incorrect. This is because HDRP supports more height map displacement techniques and decompression options than the Built-in Render Pipeline. To upgrade a Material that uses a heightmap, modify the Material's **Amplitude** and **Base** properties until the result more closely matches the Built-in Render Pipeline version.
* You can't upgrade particle shaders. HDRP doesn't support particle shaders, but it does provide Shader Graphs that are compatible with the [Built-in Particle System](https://docs.unity3d.com/Manual/Built-inParticleSystem.html). These Shader Graphs work in a similar way to the built-in particle shaders. To use these Shader Graphs, import the **Particle System Shader Samples** sample:

    1. Open the Package Manager window (menu: **Window** > **Package Manager**).
    2. Find and click the **High Definition RP** entry.
    3. In the package information for **High Definition RP**, go to the **Samples** section and click the **Import into Project** button next to **Particle System Shader Samples**.

<a name="ManualConversion"></a>

## Converting Materials manually

HDRP uses multiple processes to automatically convert Built-in Standard and Unlit Materials to HDRP Lit and Unlit Materials respectively. These processes use an overlay function to blend the color channels together, similar to the process you would use in image editing software like Adobe Photoshop.

To help you convert custom Materials manually, this section describes the maps that the converter creates from the Built-in Materials.

### Mask maps

The Built-in Shader to HDRP Shader conversion process combines the different Material maps of the Built-in Standard Shader into the separate RGBA channels of the mask map in the HDRP [Lit Material](lit-material.md). For information on which color channel each map goes in, see [mask map](Mask-Map-and-Detail-Map.md#MaskMap).

### Detail maps

The Built-in Shader to HDRP Shader conversion process combines the different detail maps of the Built-in Standard Shader into the separate RGBA channels of the detail map in the HDRP [Lit Material](lit-material.md). It also adds a smoothness detail too. For information on which color channel each map goes in, see [detail map](Mask-Map-and-Detail-Map.md#DetailMap).
