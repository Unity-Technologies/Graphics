# Screen Space Reflection

The **Screen Space Reflection** (SSR) override is a High Definition Render Pipeline (HDRP) feature that uses the depth and color buffer of the screen to calculate reflections. For information about how SSR works in HDRP, see the [reflection documentation](Reflection-in-HDRP.md#ScreenSpaceReflection).

## Enabling Screen Space Reflection

[!include[](snippets/Volume-Override-Enable.md)]

For this feature:
The property to enable in your HDRP Asset is: **Lighting > Reflections > Screen Space Reflection**.
The property to enable in your Frame Settings is: **Lighting > Screen Space Reflection**.

## Using Screen Space Reflection

HDRP uses the [Volume](Volumes.md) framework to calculate SSR, so to enable and modify SSR properties, you must add a **Screen Space Reflection** override to a [Volume](Volumes.md) in your Scene. To add **Screen Space Reflection** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Lighting** and click **Screen Space Reflection**. 
   HDRP now calculates SSR for any Camera this Volume affects.

## Properties

![](Images/Override-ScreenSpaceReflection1.png)

[!include[](snippets/Volume-Override-Enable-Properties.md)]

| **Property**                  | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **Enable**                    | Indicates whether HDRP processes SSR for Cameras in the influence of this effect's Volume . |
| **Algorithm**                 | Unity provide two algorithm 'Approximation' and 'PBR Accumulation'. 'Approximation' provide faster and less precise result in particular for rough surfaces, 'PBR Accumulation' provide a more accurate result by accumulating through multiple frames (controlable with 'Accumulation Factor') but produce more ghosting and is a bit more resources intensive. 'PBR Accumulation' don't apply to transparent Material which will always used 'Approximation'mode. |
| **Minimum Smoothness**        | Use the slider to set the minimum amount of surface smoothness at which HDRP performs SSR tracing. Lower values result in HDRP performing SSR tracing for less smooth GameObjects. |
| **Smoothness Fade Start**     | Use the slider to set the smoothness value at which SSR reflections begin to fade out. Lower values result in HDRP fading out SSR reflections for less smooth GameObjects |
| **Reflect Sky**               | Indicates whether HDRP should use SSR to handle sky reflection. If you disable this property, pixels that reflect the sky use the next level of the [reflection hierarchy](Reflection-in-HDRP.md#ReflectionHierarchy).<br />**Note**: SSR uses the depth buffer to calculate reflection and HDRP does not add transparent GameObjects to the depth buffer. If you enable this property, transparent GameObject that appear over the sky in the color buffer can cause visual artifacts and incorrect looking reflection. This is a common limitation for SSR techniques. |
| **Screen Edge Fade Distance** | Use the slider to control the distance at which HDRP fades out screen space reflections when the destination of the ray is near the boundaries of the screen. Increase this value to increase the distance from the screen edge at which HDRP fades out screen space reflections for a ray destination. |
| **Object Thickness**          | Use the slider to control the thickness of the GameObjects on screen. Because the SSR algorithm can not distinguish thin GameObjects from thick ones, this property helps trace rays behind GameObjects. The algorithm applies this property to every GameObject uniformly. |
| **Quality**                   | Specifies the quality level to use for this effect. Each quality level applies different preset values. Unity also stops you from editing the properties that the preset overrides. If you want to set your own values for every property, select **Custom**. |
| **Max Ray Steps**             | Sets the maximum number of iterations that the algorithm can execute before it stops trying to find an intersection with a Mesh. For example, if you set the number of iterations to 1000 and the algorithm only needs 10 to find an intersection, the algorithm terminates after 10 iterations. If you set this value too low, the algorithm may terminate too early and abruptly stop reflections. |
| **Accumulation Factor**       | Use the slider to control the speed of converge. 0 means no accumulation. 1 means accumulation is very slow which is useful for fixed image. Use carefuly to find a balance between convergence and ghosting. The more rough the reflection surfaces is, the more accumulation it will need to have a converged image without noise. |

## Limitations

To calculate SSR, HDRP reads a color buffer with a blurred mipmap generated during the previous frame. The color buffer only includes transparent GameObjects that use the **BeforeRefraction** [Rendering Pass](Surface-Type.md). 

When the 'Receive SSR transparent' surface option on Material is enabled, the transparent SSR will always used the 'Approximation' mode even if 'PBR Accumulation' is selected.
