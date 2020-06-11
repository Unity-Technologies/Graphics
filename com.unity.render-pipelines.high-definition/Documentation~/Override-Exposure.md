# Exposure

To work with physically-based lighting and Materials, you need to set up the Scene exposure correctly. The High Definition Render Pipeline (HDRP) includes several methods for calculating exposure to suit most use cases. HDRP expresses all exposure values that it uses in [EV<sub>100</sub>](Physical-Light-Units.html#EV).

## Using Exposure

**Exposure** uses the [Volume](Volumes.html) framework, so to enable and modify **Exposure** properties, you must add an **Exposure** override to a [Volume](Volumes.html) in your Scene. To add **Exposure** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override** and click on **Exposure**. HDRP now applies **Exposure** correction to any Camera this Volume affects.

## Properties

![](Images/Override-Exposure1.png)

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Mode**     | Use the drop-down to select the method that HDRP uses to process exposure: <br/>&#8226;  [**Fixed**](#FixedProperties): Allows you to manually sets the Scene exposure.<br/>&#8226;  [**Automatic**](#AutomaticProperties): Automatically sets the exposure depending on what is on screen.<br/>&#8226;  [**Automatic Histogram**](#AutomaticHistogram): Extends Automatic exposure with histogram control.<br/>&#8226;  [**Curve Mapping**](#CurveMappingProperties): Maps the current Scene exposure to a custom curve.<br/>&#8226;  [**Use Physical Camera**](#UsePhysicalCameraProperties): Uses the current physical Camera settings to set the Scene exposure. |

<a name="FixedProperties"></a>

### Fixed

This is the simplest, and least flexible, method for calculating exposure but it is very useful when you have a Scene with a relatively uniform exposure or when you want to take images of static areas. You can also use local [Volumes](Volumes.html) to blend between various fixed exposure values in your Scenes.

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

The automatic histogram is an extension of the Automatic mode. In particular, in order to achieve a more stable exposure result, an histogram of the image is computed and it is possible to exclude parts of it in order to discard very bright or very dark areas of the screen. 

In order to control this process, in addition to the parameter already mentioned for Automatic mode, the following are added: 

#### Properties

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Histogram Percentages** | Use this field to select the range of the histogram that is considered for auto exposure computations. The values for this field are percentiles, meaning that for example if low percentile set is X, if a pixel has lower intensity than (100-X)% of all the pixels on screen it is discarded for the sake of auto-exposure. Similarly, if the higher percentile is set to Y, then it means that a pixel is discarded if it is brighter than Y% of all other pixels on screen. <br />This effectively allows to discard unwanted outliers in the shadows and highlight regions. |
| **Use curve remapping**   | Enabling this field enables all options that are used for the Curve Mapping mode on top of the histogram mode. See [Curve Mapping section](#Curve Mapping). |

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
  | **Center Around Exposure target** | Whether the procedural mask will be centered around the GameObject set as Exposure Target in the [Camera](HDRP-Camera.html). |
  | **Center**                        | Sets the center of the procedural metering mask ([0,0] being bottom left of the screen and [1,1] top right of the screen). Available only when **Center Around Exposure target**  is enabled. |
  | **Offset**                        | Sets an offset to where mask is centered . Available only when **Center Around Exposure target**  is enabled. |
  | **Radii**                         | Sets the radii (horizontal and vertical) of the procedural mask, in terms of fraction of the screen (i.e. 0.5 means a radius that stretch half of the screen). |
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

HDRP offers several debug modes to help with setting the proper exposure for your scene. These can be activated in the [Debug Window](Render-Pipeline-Debug-Window.html). 

#### Scene EV100 Values 

 This debug mode shows a heat map of the scene luminance converted in EV100 units across the screen which is useful when determining the distribution of intensity across the screen and can help identify whether the right limits have been set and it is an informative view on how the brightness is distributed in your scene. 

The debug view also shows the numerical value of the pixel at the center of the screen and that value is also indicated as a bar in the heatmap indicator to show where it is relative to the full range.


<img src="Images/Override-Exposure4.png" style="zoom: 67%;" />



#### Histogram View

Setting the percentiles for the histogram mode might not be the most immediate thing. To help the process and to provide an overview of what the scene brightness distribution looks like HDRP provides an histogram debug view. 

<img src="Images/Override-Exposure5.png" style="zoom: 67%;" />

At the bottom of the screen, the histogram is displayed, the bars are colored in blue if they are being excluded as below the lower percentile, similarly they are red if excluded because above the higher percentile set. At the very bottom a yellow arrow points to the target exposure, while a grey arrow points at the current exposure.  
On the screen, pixels that are excluded from auto-exposure computation because falling below the lower percentile are overlaid with a blue pattern, while pixels above the higher percentile are overlaid with a red pattern. 

When the **Show Tonemap Curve** option is enabled, the curve used for tonemapping is overlaid to the histogram view. 

By default the X axis with the EV units is fixed, however the histogram in the debug view can be fixed around the current exposure by enabling the **Center Around Exposure** option. This can be useful to fix the tonemap curve overlay and have a clearer view on how the scene distributes under that curve. 

#### Metering Weighted

This mode can be used to show a picture in picture of the scene alongside a view of what the scene looks like after being weighted by the metering mask. This is particularly useful for setting up procedural metering masks or determining the right texture mask. 

<img src="Images/Override-Exposure6.png" style="zoom: 67%;" />