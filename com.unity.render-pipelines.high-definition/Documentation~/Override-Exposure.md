# Exposure

To work with physically-based lighting and Materials, you need to set up the Scene exposure correctly. The High Definition Render Pipeline (HDRP) includes several methods for calculating exposure to suit most use cases. HDRP expresses all exposure values that it uses in [EV<sub>100</sub>](Physical-Light-Units.md#EV).

## Using Exposure

**Exposure** uses the [Volume](Volumes.md) framework, so to enable and modify **Exposure** properties, you must add an **Exposure** override to a [Volume](Volumes.md) in your Scene. To add **Exposure** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override** and click on **Exposure**. HDRP now applies **Exposure** correction to any Camera this Volume affects.

## Properties

![](Images/Override-Exposure1.png)

[!include[](snippets/Volume-Override-Enable-Properties.md)]

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Mode**     | Use the drop-down to select the method that HDRP uses to process exposure: <br/>&#8226;  [**Fixed**](#FixedProperties): Allows you to manually sets the Scene exposure.<br/>&#8226;  [**Automatic**](#AutomaticProperties): Automatically sets the exposure depending on what is on screen.<br/>&#8226;  [**Automatic Histogram**](#AutomaticHistogram): Extends Automatic exposure with histogram control.<br/>&#8226;  [**Curve Mapping**](#CurveMappingProperties): Maps the current Scene exposure to a custom curve.<br/>&#8226;  [**Use Physical Camera**](#UsePhysicalCameraProperties): Uses the current physical Camera settings to set the Scene exposure. |

<a name="FixedProperties"></a>

### Fixed

This is the simplest, and least flexible, method for calculating exposure but it is very useful when you have a Scene with a relatively uniform exposure or when you want to take images of static areas. You can also use local [Volumes](Volumes.md) to blend between various fixed exposure values in your Scenes.

#### Properties

| **Property**       | **Description**                                         |
| ------------------ | ------------------------------------------------------- |
| **Fixed Exposure** | Set the exposure value for Cameras this Volume affects. |

<a name="AutomaticProperties"></a>

### Automatic

The human eye can function in both very dark and very bright areas. However, at any single moment, the eye can only sense a contrast ratio of roughly one millionth of the total range. The eye functions well in multiple light levels by adapting and redefining what is black.

**Automatic Mode** dynamically adjusts the exposure according to the range of brightness levels on the screen. The adjustment takes place gradually, which means that the user can be briefly dazzled by bright outdoor light when they emerge from a dark area. Equally, when moving from a bright area to a dark one, the Camera takes a moment to adjust.

#### Properties

| **Property**            | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Metering Mode**       | Use the drop-down to select the metering method that HDRP uses to filter the luminance source. For information on the **Metering Mode**s available, see the [Using Automatic section](#UsingAutomatic). |
| **Luminance Source**    | Use the drop-down to set the luminance source that HDRP uses to calculate the current Scene exposure. HDRP does not currently support the **Lighting Buffer** option. |
| **Compensation**        | Set the value that the Camera uses to compensate the automatically calculated exposure value. This is useful if you want to over or under expose the Scene. |
| **Limit Min**           | Set the minimum value that the Scene exposure can be set to. |
| **Limit Max**           | Set the maximum value that the Scene exposure can be set to. |
| **Mode**                | Use the drop-down to select the method that HDRP uses to change the exposure when the Camera moves from dark to light and vice versa:<br />&#8226; **Progressive**: The exposure changes over the period of time defined by the **Speed Dark to Light** and **Speed Light to Dark** property fields.<br />&#8226; **Fixed**: The exposure changes instantly. Note: The Scene view uses **Fixed**. |
| **Speed Dark to Light** | Set the speed at which the exposure changes when the Camera moves from a dark area to a bright area.<br />This property only appears when you set the **Mode** to **Progressive**. |
| **Speed Light to Dark** | Set the speed at which the exposure changes when the Camera moves from a bright area to a dark area.<br />This property only appears when you set the **Mode** to **Progressive**. |
| **Target Mid Gray**     | Sets the desired Mid gray level used by the auto exposure (i.e. to what grey value the auto exposure system maps the average scene luminance).<br/>Note that the lens model used in HDRP is not of a perfect lens, hence it will not map precisely to the selected value. |

<a name="AutomaticHistogram"></a>

### Automatic Histogram

The automatic histogram is an extension of the [**Automatic**](#AutomaticProperties) mode. In order to achieve a more stable exposure result, this mode calculates a histogram of the image which makes it possible exclude parts of the image from the exposure calculation. This is useful to discard very bright or very dark areas of the screen. 

To control this process, in addition to the properties for **Automatic** mode, this mode includes the following properties:

#### Properties

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Histogram Percentages** | Use this field to select the range of the histogram to consider for auto exposure calculations. The values for this field are percentiles. This means that, for example, if you set the low percentile to *X*, if a pixel has a lower intensity than (100-*X*)% of all the pixels on screen, HDRP discards it from the exposure calculation. Similarly, if you set the higher percentile to *Y*, it means that if a pixel has a higher intensity than *Y*%, HDRP discards it from the exposure calculation.<br />This allows the exposure calculation to discard unwanted outlying values in the shadows and highlight regions. |
| **Use Curve Remapping**   | Specifies whether to apply curve mapping on top of this exposure mode or not. For information on curve mapping properties, see the [Curve Mapping section](#Curve Mapping). |

<a name="UsingAutomatic"></a>

#### Using Automatic

To configure **Automatic Mode**, select the **Metering Mode**. This tells the Camera how to measure the current Scene exposure. You can set the **Metering Mode** to:

- **Average**: The Camera uses the entire luminance buffer to measure exposure.
- **Spot**: The Camera only uses the center of the buffer to measure exposure. This is useful if you want to only expose light against what is in the center of your screen.

![](Images/Override-Exposure2.png)

- **Center Weighted**: The Camera applies a weight to every pixel in the buffer and then uses them to measure the exposure. Pixels in the center have the maximum weight, pixels at the screen borders have the minimum weight, and pixels in between have a progressively lower weight the closer they are to the screen borders.

![](Images/Override-Exposure3.png)

- **Mask Weighted**: The Camera applies a weight to every pixel in the buffer then uses the weights to measure the exposure. To specify the weighting, this technique uses the Texture set in the **Weight Texture Mask** field. Note that, if you do not provide a Texture, this metering mode is equivalent to **Average**.
  
- **Procedural Mask**: The Camera applies applies a weight to every pixel in the buffer then uses the weights to measure the exposure. The weights are generated using a mask that is procedurally generated with the following parameters:
  
  | **Property**                      | **Description**                                              |
  | --------------------------------- | ------------------------------------------------------------ |
  | **Center Around Exposure target** | Whether the procedural mask will be centered around the GameObject set as Exposure Target in the [Camera](HDRP-Camera.md). |
  | **Center**                        | Sets the center of the procedural metering mask ([0,0] being bottom left of the screen and [1,1] top right of the screen). Available only when **Center Around Exposure target**  is disabled. |
  | **Offset**                        | Sets an offset to where mask is centered . Available only when **Center Around Exposure target**  is enabled. |
  | **Radii**                         | Sets the radii (horizontal and vertical) of the procedural mask, in terms of fraction of half the screen (i.e. 0.5 means a mask that stretch half of the screen in both directions). |
  | **Softness**                      | Sets the softness of the mask, the higher the value the less influence is given to pixels at the edge of the mask. |
  | **Mask Min Intensity**            | All pixels below this threshold (in EV100 units) will be assigned a weight of 0 in the metering mask. |
  | **Mask Max Intensity**            | All pixels above this threshold (in EV100 units) will be assigned a weight of 0 in the metering mask. |
  


Next, set the **Limit Min** and **Limit Max** to define the minimum and maximum exposure values respectively. Move between light and dark areas of your Scene and alter each property until you find the perfect values for your Scene.

 

Now use the **Compensation** property to over or under-expose the Scene. This works in a similar way to how exposure compensation works on most cameras.

Finally, you can tweak the adaptation speed. This controls how fast the exposure adapts to exposure changes. The human eye adapts slower to darkness than to lightness, so use a lower value for **Speed Light to Dark** than for **Speed Dark to Light**.

<a name="CurveMappingProperties"></a>

### Curve Mapping

The **Curve Mapping Mode** is a variant of [**Automatic**](#AutomaticProperties) **Mode**. Instead of setting limits, you manipulate a curve, where the x-axis represents the current Scene exposure and the y-axis represents the exposure you want. This lets you set the exposure in a more precise and controlled way for all lighting conditions at once.

#### Properties

| **Property**  | **Description**                                              |
| ------------- | ------------------------------------------------------------ |
| **Curve Map** | Use the curve to remap the Scene exposure (x-axis) to the exposure you want (y-axis). |

<a name="UsePhysicalCameraProperties"></a>

### Use Physical Camera

This mode mainly relies on the [Camera’s](https://docs.unity3d.com/Manual/class-Camera.html) **Physical Settings**. The only property this **Mode** exposes allows you to over or under expose the Scene.

#### Properties

| **Property**     | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Compensation** | Set the value that the Camera uses to compensate the automatically computed exposure value. This is useful if you want to over or under expose the Scene. This works similarly to how exposure compensation works on most cameras. |

<a name="DebugModes"></a>

### Exposure Debug Modes

HDRP offers several debug modes to help you to set the correct exposure for your scene. You can activate these in the [Debug window](Render-Pipeline-Debug-Window.md). 

#### Scene EV<sup>100</sup> Values 

This debug mode shows a heat map of the scene luminance converted to [EV<sup>100</sup>](Physical-Light-Units.md#EV) units across the screen. This is useful to determine the distribution of intensity across the screen which can help you to identify whether you have set the right exposure limits. It is also an informative view on how the brightness is distributed in your scene.

Furthermore, this debug view shows the numerical value of the pixel at the center of the screen. It also displays this value in the heatmap indicator at the bottom of the screen to show where it is relative to the full range.


![](Images/Override-Exposure4.png)



#### Histogram View

In **Automatic Histogram** mode, if may be difficult to set the upper and lower brightness percentages without a references. To help with this, HDRP includes the Histogram debug view which shows an overview of what the scene brightness distribution looks like.

![](Images/Override-Exposure5.png)

There are two places this debug mode displays information. On the screen and on a histogram at the bottom of the screen. Both of these methods show whether the exposure algorithm accepts or excludes a particular brightness value. To do this on the screen, the debug mode overlays excluded pixels with a particular color. The histogram draws bars that use the same colors to show the range of brightness values and their validity. The colors correspond to:

* **Blue**: The brightness value is below the lower percentile and is excluded.
* **Red**: The brightness value is above the higher percentile and is excluded.
* **White**: The brightness value is between the upper and lower percentiles and is accepted.

At the bottom of the histogram, a yellow arrow points to the target exposure, while a grey arrow points at the current exposure.  

If you enable the **Show Tonemap Curve** option, the debug view overlays the curve used to tonemap to the histogram view.

By default, the values on the x-axis are fixed, however, you can also make the histogram fix around the current exposure. To do this, enable the **Center Around Exposure** option. This can be useful to fix the tonemap curve overlay and have a clearer view on how the scene distributes under that curve.

#### Metering Weighted

The Metering Weighted debug view displays the scene alongside a picture of what the scene looks like after HDRP weights it with the metering mask. This is particularly useful to set up the procedural metering masks or determine the right texture mask.

![](Images/Override-Exposure6.png)

#### Final Image Histogram

The final image histogram debug view displays the scene alongside an overlay representing the histogram of the image after all post-processing (tonemapping and gamma correction included) is applied. This histogram has 256 bins to map to 8-bit image values. 
This view can display both luminance histogram or RGB channels represented separately. 

![](Images/Override-Exposure7.png)
