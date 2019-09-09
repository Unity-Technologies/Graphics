# HDRI Sky

The **HDRI Sky Volume** component override controls settings you can use to set up an HDRI Sky. An HDRI Sky is a simple sky representation that uses a cubemap texture. This component also enables you to define how HDRP updates the indirect lighting the sky generates in the Scene.

Tip: [Unity HDRI Pack](https://assetstore.unity.com/packages/essentials/beta-projects/unity-hdri-pack-72511) is available on the Unity Asset Store and provides 7 pre-converted HDR Cubemaps ready for use within your Project.

## Using HDRI Sky

**HDRI Sky** uses the [Volume](Volumes.html) framework, so to enable and modify **HDRI Sky** properties, you must add an **HDRI Sky** override to a [Volume](Volumes.html) in your Scene. To add **HDRI Sky** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Sky** and click on **HDRI Sky**.

After you add an **HDRI Sky** override, you must set the Volume to use **HDRI Sky**. The [Visual Environment](Override-Visual-Environment.html) override controls which type of sky the Volume uses. In the **Visual Environment** override, navigate to the **Sky** section and set the **Type** to **HDRI Sky**. HDRP now renders an **HDRI Sky** for any Camera this Volume affects.

## Properties

![](Images/Override-HDRISky1.png)

| Property                         | Description                                                  |
| -------------------------------- | ------------------------------------------------------------ |
| **HDRI Sky**                     | Assign a HDRI Texture that HDRP uses to render the sky.      |
| **Sky Intensity Mode**           | Use the drop-down to select the mode that HDRP uses to calculate the intensity of the environment light it applies to the Scene.<br />&#8226; **Exposure**: Defines the sky intensity using the **Exposure** and **Multiplier** properties.<br />&#8226; **Lux**: Define the sky intensity in terms of Lux values. |
| - **Exposure**                   | Set the amount of light per unit area that HDRP applies to the HDRI Sky cubemap.<br />This property only appears when you select **Exposure** from the **Sky Intensity Mode** drop-down. |
| - **Multiplier**                 | Set the multiplier for HDRP to apply to the Scene as environmental light. HDRP multiplies the environment light in your Scene by this value.<br />This property only appears when you select **Exposure** from the **Sky Intensity Mode** drop-down. |
| - **Desired Lux Value**          | Set an absolute intensity for the HDR Texture you set in **HDRI Sky**, in Lux, for lighting received in a direction perpendicular to the ground. This is similar to the Lux unit you use to represent the Sun and thus is complimentary.<br />This property only appears when you select **Lux** from the **Sky Intensity Mode** drop-down. |
| - **Upper Hemisphere Lux Value** | Displays the relative intensity, in Lux, for the current HDR texture set in **HDRI Sky**. The final multiplier HDRP applies for intensity is **Desired Lux Value** / **Upper Hemisphere Lux Value**. This field is an informative helper.<br />This property only appears when you select **Lux** from the **Sky Intensity Mode** drop-down. |
| **Rotation**                     | Use the slider to set the angle to rotate the cubemap, in degrees. |
| **Update Mode**                  | Use the drop-down to set the rate at which HDRP updates the sky environment (using Ambient and Reflection Probes).<br />&#8226; **On Changed**: HDRP updates the sky environment when one of the sky properties changes.<br />&#8226; **On Demand**: HDRP waits until you manually call for a sky environment update from a script.<br />&#8226; **Realtime**: HDRP updates the sky environment at regular intervals defined by the **Update Period**. |
| - **Update Period**              | Set the period (in seconds) for HDRP to update the sky environment. Set the value to 0 if you want HDRP to update the sky environment every frame. This property only appears when you set the **Update Mode** to **Realtime**. |