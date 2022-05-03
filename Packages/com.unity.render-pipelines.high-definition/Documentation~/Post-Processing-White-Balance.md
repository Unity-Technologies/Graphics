# White Balance

The White Balance component applies a white balance effect that removes unrealistic color casts, so that items that would appear white in real life render as white in your final image. You can also use white balance to create an overall colder or warmer feel in the final render.

## Using White Balance

**White Balance** uses the [Volume](Volumes.md) framework, so to enable and modify **White Balance** properties, you must add a **White Balance** override to a [Volume](Volumes.md) in your Scene. To add **White Balance** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override** > **Post-processing** and select **White Balance**. HDRP now applies **White Balance** to any Camera this Volume affects.

[!include[](snippets/volume-override-api.md)]

## Properties

![](Images/Post-processingWhiteBalance1.png)

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Temperature** | Use the slider to set the white balance to a custom color temperature. Higher values result in a warmer color temperature and lower values result in a colder color temperature. See [Wikipedia: Color balance](https://en.wikipedia.org/wiki/Color_balance) for more information about color temperature. |
| **Tint**        | Use the slider to compensate for a green or magenta tint.    |
