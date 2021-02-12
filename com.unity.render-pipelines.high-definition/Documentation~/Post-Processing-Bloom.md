# Bloom

The Bloom effect creates fringes of light extending from the borders of bright areas in an image. This creates the illusion of an extremely bright light overwhelming the Camera.

Bloom in the High Definition Render Pipeline (HDRP) is energy-conserving. This means that you must use correct physical values for lighting and Materials for it to work correctly. For information on the light units that HDRP uses, see the [Physical Light Units documentation](Physical-Light-Units.md).

The Bloom effect also has a **Lens Dirt** feature, which you can use to apply a full-screen layer of smudges or dust to diffract the Bloom effect.

## Using Bloom

**Bloom** uses the [Volume](Volumes.md) framework, so to enable and modify **Bloom** properties, you must add a **Bloom** override to a [Volume](Volumes.md) in your Scene. To add **Bloom** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Post-processing** and click on **Bloom**. HDRP now applies **Bloom** to any Camera this Volume affects.

Bloom includes [additional properties](More-Options.md) that you must manually expose.

[!include[](snippets/volume-override-api.md)]

## Properties

![](Images/Post-processingBloom1.png)

### Bloom

| **Property**  | **Description**                                              |
| ------------- | ------------------------------------------------------------ |
| **Threshold** | Use the slider to set the level of brightness to filter out pixels under this level. This value is expressed in gamma-space. A value higher than 0 will break the energy conservation rule. |
| **Intensity** | Use the slider to set the strength of the Bloom filter.      |
| **Scatter**   | Use the slider to change the extent of the veiling effect.   |
| **Tint**      | Use the color picker to select a color for the Bloom effect to tint to. |

### Lens Dirt

| **Property**  | **Description**                                              |
| ------------- | ------------------------------------------------------------ |
| **Texture**   | Assign a Texture to apply dirtiness (for example, smudges or dust) to the lens. |
| **Intensity** | Set the strength of the Lens Dirt effect.                    |

### Advanced Tweaks

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Resolution**             | Use the drop-down to set the resolution at which HDRP processes the Bloom effect. If you target consoles that use a very high resolution (for example, 4k), select **Quarter,** because it is less resource-intensive.<br />&#8226; **Quarter**: Uses quarter the screen resolution.<br />&#8226; **Half**: Uses half the screen resolution.<br/>This property only appears when you enable [additional properties](More-Options.md). |
| **High Quality Prefiltering** | Enable the checkbox to make HDRP use 13 samples instead of 4 during the prefiltering pass. This increases the resource intensity of the Bloom effect, but results in less flickering by small and bright objects like the sun.<br />This property only appears when you enable [additional properties](More-Options.md). |
| **High Quality Filtering** | Enable the checkbox to make HDRP use bicubic filtering instead of bilinear filtering. This increases the resource intensity of the Bloom effect, but results in smoother visuals.<br />This property only appears when you enable [additional properties](More-Options.md). |
| **Anamorphic**             | Enable the checkbox to make the bloom effect take the **Anamorphism** property of the Camera into account. This stretches the bloom horizontally or vertically like it would on anamorphic sensors.<br />This property only appears when you enable [additional properties](More-Options.md). |

## Details

From 2019.3, HDRP provides lookup Textures that you can use to customize this effect. These lookup Textures are for the **Texture** property in the **Lens Dirt** section. To add these Textures to your Unity Project, you must use the Package Manager:

1. In the top menu, go to **Window > Package Manager**.
2. In the **Packages** window, select **High Definition RP**.
3. In the **High Definition RP** section, navigate to **Additional Post-processing Data** and click **Import into Project** next to it.
4. The Textures that are relevant to Bloom are in the **Lens Dirt** folder, so if you only want the lookup Textures for Bloom, only import the contents of the **Lens Dirt** folder.
