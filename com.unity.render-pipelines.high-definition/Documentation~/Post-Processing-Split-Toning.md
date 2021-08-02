# Split Toning

This effect tints different areas of the image based on luminance values, to help you achieve a more distinctive look. You can use this to add different color tones to the shadows and highlights in your Scene. 

## Using Split Toning

**Split Toning** uses the [Volume](Volumes.md) framework, so to enable and modify **Split Toning** properties, you must add a **Split Toning** override to a [Volume](Volumes.md) in your Scene. To add **Split Toning** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Post-processing** and click on **Split Toning**. HDRP now applies **Split Toning** to any Camera this Volume affects.

[!include[](snippets/volume-override-api.md)]

## Properties

When you adjust the color in the color picker for each property, you should only adjust the **Hue** and **Saturation**. **Value** also changes the overall image brightness.

![](Images/Post-processingSplitToning1.png)

| **Property**   | **Description**                                              |
| -------------- | ------------------------------------------------------------ |
| **Shadows**    | Use the color picker to select the color that HDRP uses for tinting shadows. |
| **Highlights** | Use the color picker to select the color that HDRP uses for tinting highlights. |
| **Balance**    | Use the slider to set the balance between **Shadows** and **Highlights**. Lower values result in more pronounced shadow toning is compared to highlight toning. Higher values result in the opposite effect, with more pronounced highlight toning compared to shadow toning. |
