# Chromatic Aberration

Chromatic Aberration mimics the effect that a real-world camera produces when its lens fails to join all colors to the same point.

For more information on the Chromatic Aberration effect, see the [Chromatic Aberration](https://docs.unity3d.com/Manual/PostProcessing-ChromaticAberration.html) documentation in the Unity Manual.

## Comparison images

## Using Chromatic Aberration

**Chromatic Aberration** uses the [Volume](Volumes.html) framework, so to enable and modify **Chromatic Aberration** properties, you must add a **Chromatic Aberration** override to a [Volume](Volumes.html) in your Scene. To add **Chromatic Aberration** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Post-processing** and click on **Chromatic Aberration**. HDRP now applies **Chromatic Aberration** to any Camera this Volume affects.

## Properties

![](Images/Post-processingChromaticAberration1.png)

| **Property**     | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Spectral Lut** | Assign a Texture to use for a custom fringing color. Leave this field empty to use the default Texture. |
| **Intensity**    | Use the slider to set the strength of the Chromatic Aberration effect. |
| **Max Samples**  | Use the slider to set the maximum number of samples that HDRP uses to render the Chromatic Aberration effect. |

## Details

From 2019.3, HDRP provides lookup Textures that you can use to customize this effect. These lookup Textures are for the **Spectral Lut** property. To add these Textures to your Unity Project, you must use the Package Manager:

1. Select **Window > Package Manager**.
2. In the **Packages** window, select **High Definition RP**.
3. In the **High Definition RP** section, navigate to **Additional Post-processing Data** and click **Import into Project** next to it.
4. The Textures that are relevant to Chromatic Aberration are in the **Spectral LUTs** folder,  so if you only want the lookup Textures for Chromatic Aberration, only import the contents of the **Spectral LUTs** folder.