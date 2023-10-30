# Chromatic Aberration

Chromatic Aberration mimics the effect that a real-world camera produces when its lens fails to join all colors to the same point.

For more information on the Chromatic Aberration effect, see the [Chromatic Aberration](https://docs.unity3d.com/Manual/PostProcessing-ChromaticAberration.html) documentation in the Unity Manual.

## Using Chromatic Aberration

**Chromatic Aberration** uses the [Volume](understand-volumes.md) framework, so to enable and modify **Chromatic Aberration** properties, you must add a **Chromatic Aberration** override to a [Volume](understand-volumes.md) in your Scene. To add **Chromatic Aberration** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override** > **Post-processing** and select **Chromatic Aberration**. HDRP now applies **Chromatic Aberration** to any Camera this Volume affects.

[!include[](snippets/volume-override-api.md)]

## Properties

| **Property**     | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Spectral Lut** | Assign a Texture to use for a custom fringing color. Leave this field empty to use the default Texture. |
| **Intensity**    | Use the slider to set the strength of the Chromatic Aberration effect. |
| **Quality**    | Specify the quality level HDRP uses for performance relevant parameters: <br/><br/>&#8226; **Custom**: Set your own **Max Samples** value, using the slider below.<br/><br/>&#8226; **Low**: Use the low **Max Samples** value, predefined in your HDRP Asset.<br/><br/>&#8226; **Medium**: Use the medium **Max Samples** value, predefined in your HDRP Asset.<br/><br/>&#8226; **High**: Use the high **Max Samples** value, predefined in your HDRP Asset.|
| **Max Samples**  | Use the slider to set the maximum number of samples that HDRP uses to render the Chromatic Aberration effect. |

## Details

From 2019.3, HDRP provides lookup Textures that you can use to customize this effect. These lookup Textures are for the **Spectral Lut** property. To add these Textures to your Unity Project, you must use the Package Manager:

1. Select **Window** > **Package Manager**.
2. In the **Packages** window, select **High Definition RP**.
3. In the **High Definition RP** section, go to **Additional Post-processing Data** and select **Import into Project**.
4. The Textures that are relevant to Chromatic Aberration are in the **Spectral LUTs** folder,  so if you only want the lookup Textures for Chromatic Aberration, only import the contents of the **Spectral LUTs** folder.

Care is needed when using the [Bloom](Post-Processing-Bloom.md) effect with Chromatic Abberation. For performance reasons, Chromatic Aberation is computed after the Bloom computation. This results in Bloom overpowering the Chromatic Aberration effect when the Bloom Intensity is set to a very high value. However, in a typical Bloom configuration, the Intensity should never need to be set high enough for this to be an issue.
