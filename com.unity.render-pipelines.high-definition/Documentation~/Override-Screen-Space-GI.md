# Screen Space Global Illumination (Preview)

The **Screen Space Illumination** (SSGI) override is a High Definition Render Pipeline (HDRP) feature that uses the depth and color buffer of the screen to calculate diffuse light bounces.

## Enabling Contact Shadows
[!include[](Snippets/Volume-Override-Enable.md)]

For this feature:
The property to enable in your HDRP Asset is: **Lighting > Screen Space Global Illumination**.
The property to enable in your Frame Settings is: **Lighting > Screen Space Global Illumination**.

## Using Screen Space Global Illumination

To use SSGI in your Scene, you must enable it in your [HDRP Asset](HDRP-Asset.md), go to  **Lighting section** and enable the **Screen Space Global Illumination** checkbox.
Your Cameras must also have the settings enabled. Go to **Default Frame Settings > Lighting** section and enable the **Screen Space Global Illumination** checkbox.

HDRP uses the [Volume](Volumes.md) framework to calculate SSGI, so to enable and modify SSGI properties, you must add a **Screen Space Reflection** override to a [Volume](Volumes.md) in your Scene. To add **Screen Space Reflection** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Lighting** and click **Screen Space Global Illumination**. 
   HDRP now calculates SSGI for any Camera this Volume affects.

## Properties

[!include[](Snippets/Volume-Override-Enable-Properties.md)]

| **Property**                  | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **Quality**                   | Determines the overall quality of the effect. |
| **Full Resolution**           | Toggles whether HDRP calculates SSGI at full resolution. |
| **Ray Steps**                 | The number of ray steps to use to calculate SSGI. If you set this to a higher value, the quality of the effect improves, however it is more resource intensive to process.  |
| **Maximal Radius**            | TODO |
| **Clamp Value**               | TODO |
| **Filter Radius**             | TODO |
| **Depth Buffer Thickness**    | TODO |
