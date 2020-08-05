# Cloud Layer

The **Cloud Layer** Volume component override controls an optional cloud layer you can add on top of any Sky. The cloud layer is a 2D texture blended with the sky that can be animated using a flowmap.

## Using Cloud Layer

The **Cloud Layer** uses the [Volume](Volumes.md) framework, so to enable and modify **Cloud Layer** properties, you must add a **Cloud Layer** override to a [Volume](Volumes.md) in your Scene. To add **Cloud Layer** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Sky** and click on **Cloud Layer**.

After you add a **Cloud Layer** override, you must enable it in the override itself. In the override, Check the **Enable** property. HDRP now renders **Cloud Layer** for any Camera this Volume affects.

<a name="DefaultCloudMap"></a>

## Using the default Cloud Map

HDRP includes an example Cloud Map texture named `DefaultCloudLayer`.
This example Cloud Map is a read-only **CustomRenderTexture**. This means that, if you want to modify any of its properties, you need to create a duplicate of it and modify that instead. To duplicate the example Cloud Map:

1. Duplicate the `DefaultCloudLayer` asset.
2. Duplicate the Material referenced by the DefaultCloudLayer asset. This Material is called `DefaultCloudLayerMaterial`.
3. In the Inspector for the new `DefaultCloudLayer`, assign the new `DefaultCloudLayerMaterial` to the **Material** property.

<a name="CustomizingCloudMap"></a>

## Customizing the Cloud Map

The Cloud Map is a 2D texture in LatLong layout (sometimes called Cylindrical or Equirectangular) that contains cloud opacity in the red channel.
If **Upper Hemisphere Only** is checked, the map is interpreted as being the upper half of a LatLong texture. It means that it will only conver the sky above the horizon.

<a name="CustomizingFlowmap"></a>

## Customizing the Flowmap

The Flowmap must have the same layout as the cloud map, and is also subject to the **Upper Hemisphere Only** property.
Only the red and green channel are used and they represent respectively horizontal and vertical displacement. For each of these channels, a value of `0.5` means no displacement, a value of `0` means a negative displacement and a value of `1` means a positive displacement.

## Properties

![](Images/Override-CloudLayer.png)

| Property                      | Description                                                  |
| ----------------------------- | ------------------------------------------------------------ |
| **Enable**                    | Enables the cloud layer. |
| **Cloud Map*                  | Assign a Texture that HDRP uses to render the cloud layer. Refer to the section [Using the default Cloud Map](#DefaultCloudMap) or [Customizing the Cloud Map](#CustomizingCloudMap) for more details. |
| **Upper Hemisphere Only**     | Check the box to display the cloud layer above the horizon only. |
| **Tint**                      | Specifies a color that HDRP uses to tint the Cloud Layer. |
| **Intensity Multiplier**      | Set the multiplier by which HDRP multiplies the Cloud Layer color. |
| **Rotation**                  | Use the slider to set the angle to rotate the Cloud Layer, in degrees. |
| **Enable Distortion**         | Enable or disable cloud motion using UV distortion. |
| - **Distortion Mode**         | Use the drop-down to select the method that HDRP uses to calculate the cloud distortion.<br />&#8226; **Procedural**: HDRP distorts the clouds using a uniform wind direction.<br />&#8226; **Flowmap**: HDRP distorts the clouds with a user provided flowmap. |
| -- **Flowmap**                | Assign a flowmap, in LatLong layout, that HDRP uses to distort UVs when rendering the clouds. Refer to the section [Customizing the Flowmap](#CustomizingFlowmap) for more details.<br />This property only appears when you select **Flowmap** from the **Distortion Mode** drop-down. |
| - **Scroll direction**        | Use the slider to set the scrolling direction for the distortion. |
| - **Scroll speed**            | Modify the speed at which HDRP scrolls the distortion texture. |
