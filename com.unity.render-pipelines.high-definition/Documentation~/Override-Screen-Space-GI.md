# Screen Space Global Illumination (Preview)

The **Screen Space Illumination** (SSGI) override is a High Definition Render Pipeline (HDRP) feature that uses the depth and color buffer of the screen to calculate diffuse light bounces.

![](Images/HDRPFeatures-SSGI.png)

## Enabling Screen Space Global Illumination
[!include[](Snippets/Volume-Override-Enable.md)]

For this feature:
The property to enable in your HDRP Asset is: **Lighting > Screen Space Global Illumination**.
The property to enable in your Frame Settings is: **Lighting > Screen Space Global Illumination**.

## Using Screen Space Global Illumination

HDRP uses the [Volume](Volumes.md) framework to calculate SSGI, so to enable and modify SSGI properties, you must add a **Screen Space Global Illumination** override to a [Volume](Volumes.md) in your Scene. To add **Screen Space Global Illumination** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Lighting** and click **Screen Space Global Illumination**. 
   HDRP now calculates SSGI for any Camera this Volume affects.

## Properties

[!include[](Snippets/Volume-Override-Enable-Properties.md)]

| **Property**                  | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **Quality**                   | Specifies the overall quality of the effect. The higher the quality, the more resource-intensive the effect is to process.|
| **Full Resolution**           | Toggles whether HDRP calculates SSGI at full resolution. |
| **Ray Steps**                 | The number of ray steps to use to calculate SSGI. If you set this to a higher value, the quality of the effect improves, however it is more resource intensive to process.  |
| **Filter Radius**             | The size of the filter use to smooth the effect after raymarching. Higher value mean blurrier result and is more resource intensive. |
| **Object Thickness**          | Use the slider to control the thickness of the GameObjects on screen. Because the SSR algorithm can not distinguish thin GameObjects from thick ones, this property helps trace rays behind GameObjects. The algorithm applies this property to every GameObject uniformly. |
