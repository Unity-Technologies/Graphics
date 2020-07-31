# Screen Space Global Illumination (Preview)

The **Screen Space Illumination** (SSGI) override is a High Definition Render Pipeline (HDRP) feature that uses the depth and color buffer of the screen to calculate diffuse light bounce.

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
| **Quality**                   | TODO |
| **Full Resolution**           | TODO |
| **Ray Steps**                 | TODO |
| **Maximal Radius**            | TODO |
| **Clamp Value**               | TODO |
| **Filter Radius**             | TODO |
| **Depth Buffer Thickness**    | TODO |
