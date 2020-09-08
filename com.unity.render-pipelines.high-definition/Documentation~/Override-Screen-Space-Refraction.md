# Screen Space Refraction

The **Screen Space Refraction** (SSR) override is a High Definition Render Pipeline (HDRP) feature that uses the depth and color buffer of the screen to calculate  refraction. For information about how screen space refraction works in HDRP, see the [Screen space refraction documentation](Refraction-in-HDRP.md#ScreenSpaceRefraction).

## Using Screen Space Refraction

HDRP uses the [Volume](Volumes.md) framework to calculate SSR, so to enable and modify SSR properties, you must add a **Screen Space Refraction** override to a [Volume](Volumes.md) in your Scene. To add **Screen Space Refraction** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Lighting** and click **Screen Space Refraction**. 
   HDRP now calculates SSR for any Camera this Volume affects.

## Properties

![](Images/Override-ScreenSpaceRefraction1.png)

[!include[](snippets/Volume-Override-Enable-Properties.md)]

| **Property**                  | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **Screen Edge Fade Distance** | Use the slider to control the distance at which HDRP fades out the refraction effect when the destination of the ray is near the boundaries of the screen. Increase this value to increase the distance from the screen edge at which HDRP fades out the refraction effect for a ray destination. |
