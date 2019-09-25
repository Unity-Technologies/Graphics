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
| **Mode**     | Use the drop-down to select the method that HDRP uses to process exposure: <br/>&#8226;  [**Fixed**](#FixedProperties): Allows you to manually sets the Scene exposure.<br/>&#8226;  [**Automatic**](#AutomaticProperties): Automatically sets the exposure depending on what is on screen.<br/>&#8226;  [**Curve Mapping**](#CurveMappingProperties): Maps the current Scene exposure to a custom curve.<br/>&#8226;  [**Use Physical Camera**](#UsePhysicalCameraProperties): Uses the current physical Camera settings to set the Scene exposure. |

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

<a name="UsingAutomatic"></a>

#### Using Automatic

To configure **Automatic Mode**, select the **Metering Mode**. This tells the Camera how to measure the current Scene exposure. You can set the **Metering Mode** to:

- **Average**: The Camera uses the entire luminance buffer to measure exposure.
- **Spot**: The Camera only uses the center of the buffer to measure exposure. This is useful if you want to only expose light against what is in the center of your screen.

![](Images/Override-Exposure2.png)

- **Center Weighted**: The Camera applies a weight to every pixel in the buffer and then uses them to measure the exposure. Pixels in the center have the maximum weight, pixels at the screen borders have the minimum weight, and pixels in between have a progressively lower weight the closer they are to the screen borders.

![](Images/Override-Exposure3.png)

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