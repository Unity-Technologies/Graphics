# HDRI Sky

The **HDRI Sky Volume** component override controls settings you can use to set up an HDRI Sky. An HDRI Sky is a simple sky representation that uses a cubemap texture. This component also enables you to define how HDRP updates the indirect lighting the sky generates in the Scene.

Tip: [Unity HDRI Pack](https://assetstore.unity.com/packages/essentials/beta-projects/unity-hdri-pack-72511) is available on the Unity Asset Store and provides 7 pre-converted HDR Cubemaps ready for use within your Project.

![](Images/HDRPFeatures-HDRISky.png)

## Using HDRI Sky

**HDRI Sky** uses the [Volume](Volumes.md) framework, so to enable and modify **HDRI Sky** properties, you must add an **HDRI Sky** override to a [Volume](Volumes.md) in your Scene. To add **HDRI Sky** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Sky** and click on **HDRI Sky**.

After you add an **HDRI Sky** override, you must set the Volume to use **HDRI Sky**. The [Visual Environment](Override-Visual-Environment.md) override controls which type of sky the Volume uses. In the **Visual Environment** override, navigate to the **Sky** section and set the **Type** to **HDRI Sky**. HDRP now renders an **HDRI Sky** for any Camera this Volume affects.

[!include[](snippets/volume-override-api.md)]

## Properties

![](Images/Override-HDRISky1.png)

[!include[](snippets/Volume-Override-Enable-Properties.md)]

| Property                         | Description                                                  |
| -------------------------------- | ------------------------------------------------------------ |
| **HDRI Sky**                     | Assign a HDRI Texture that HDRP uses to render the sky.      |
| **Enable Distortion**            | Enable or disable UV distortion. |
| - **Distortion Mode**            | Use the drop-down to select the method that HDRP uses to calculate the sky distortion.<br />&#8226; **Procedural**: HDRP distorts the sky using a uniform wind direction.<br />&#8226; **Flowmap**: HDRP distorts the sky with a user provided flowmap. |
| -- **Flowmap**                   | Assign a flowmap, in LatLong layout, that HDRP uses to distort UVs when rendering the sky.<br />This property only appears when you select **Flowmap** from the **Distortion Mode** drop-down. |
| -- **Upper Hemisphere Only**     | Check the box if the flowmap contains distortion for the sky above the horizon only.<br />This property only appears when you select **Flowmap** from the **Distortion Mode** drop-down. |
| - **Scroll direction**           | Use the slider to set the scrolling direction for the distortion. |
| - **Scroll speed**               | Modify the speed at which HDRP scrolls the distortion texture. |
| **Intensity Mode**        | Use the drop-down to select the method that HDRP uses to calculate the sky intensity.<br />&#8226; **Exposure**: HDRP calculates intensity from an exposure value in EV100.<br />&#8226; **Multiplier**: HDRP calculates intensity from a flat multiplier. <br />&#8226; **Lux**: HDRP calculates intensity in terms of a target Lux value. |
| - **Exposure**                   | Set the amount of light per unit area that HDRP applies to the HDRI Sky cubemap.<br />This property only appears when you select **Exposure** from the **Intensity Mode** drop-down. |
| - **Multiplier**                 | Set the multiplier for HDRP to apply to the Scene as environmental light. HDRP multiplies the environment light in your Scene by this value.<br />This property only appears when you select **Multiplier** from the **Intensity Mode** drop-down. |
| - **Desired Lux Value**          | Set an absolute intensity for the HDR Texture you set in **HDRI Sky**, in [Lux](Physical-Light-Units.md#Lux). This value represents the light received in a direction perpendicular to the ground. This is similar to the Lux unit you use to represent the Sun and thus is complimentary.<br />This property only appears when you select **Lux** from the **Sky Intensity Mode** drop-down. |
| - **Upper Hemisphere Lux Value** | Displays the relative intensity, in Lux, for the current HDR texture set in **HDRI Sky**. The final multiplier HDRP applies for intensity is **Desired Lux Value** / **Upper Hemisphere Lux Value**. This field is an informative helper.<br />This property only appears when you select **Lux** from the **Sky Intensity Mode** drop-down. |
| **Rotation**                     | Use the slider to set the angle to rotate the cubemap, in degrees. |
| **Update Mode**                  | Use the drop-down to set the rate at which HDRP updates the sky environment (using Ambient and Reflection Probes).<br />&#8226; **On Changed**: HDRP updates the sky environment when one of the sky properties changes.<br />&#8226; **On Demand**: HDRP waits until you manually call for a sky environment update from a script.<br />&#8226; **Realtime**: HDRP updates the sky environment at regular intervals defined by the **Update Period**. |
| - **Update Period**              | Set the period (in seconds) for HDRP to update the sky environment. Set the value to 0 if you want HDRP to update the sky environment every frame. This property only appears when you set the **Update Mode** to **Realtime**. |

## Advanced Properties

![](Images/Override-HDRISky2.png)

These properties only appear if you enable [more options](More-Options.md).


| Property                         | Description                                                  |
| -------------------------------- | ------------------------------------------------------------ |
| **Backplate** | Indicates whether to project the bottom part of the HDRI onto a plane with various shapes such as a Rectangle, Circle, Ellipse, or Infinite plane. |
| - **Type** | Specifies the shape of the backplate. The options are:<br/>&#8226; **Disc**: Projects the bottom of the HDRI texture onto a disc.<br/>&#8226; **Rectangle**: Projects the bottom of the HDRI texture onto a rectangle.<br/>&#8226; **Ellipse**: Projects the bottom of the HDRI texture onto an ellipse.<br/>&#8226; **Infinite**: Projects the bottom of the HDRI texture onto an infinite plane. |
| - **Ground Level** | The height of the ground level in the scene. |
| - **Scale** | The scale of the backplate. HDRP uses the **X** and **Y** values of this property to scale the backplate (for Ellipse **X** and **Y** must be different). |
| - **Projection Distance** | HDRP uses this number to control the projection of the bottom hemisphere of the HDRI on the backplate. Small projection distance implies higher pixels density with more distortion, large projection distance implies less pixels density with less distortion. |
| - **Backplate Rotation** | The rotation of the physical backplate. |
| - **Texture Rotation** | The rotation of the HDRI texture HDRP projects onto the backplate. |
| - **Texture Offset** | The offset value to apply to the texture HDRP projects onto the backplate. |
| - **Blend Amount** | The percentage of the transition between the backplate and the background HDRI. **0** means no blending, **25** means the blending starts at the end of the boundary of the backplate, **50** means the blending starts from the middle of the backplate and **100** means the transition starts from the center of the backplate with a smoothstep. |
| - **Point Light Shadow** | Indicates whether the backplate receives shadows from point/spot [Lights](Light-Component.md). |
| - **Directional Light Shadow** | Indicates whether the backplate receives shadows from the main directional Light. |
| - **Area Light Shadow** | Indicates whether the backplate receives shadows from area Lights. |
| -**Shadow Tint** | Specifies the color to tint shadows cast onto the backplate. |
| - **Reset Color** | Resets the saved **Shadow Tint** for the shadow. HDRP calculates a new default shadow tint when the HDRI changes. |

**Note**: To use ambient occlusion in the backplate, increase the value of the **Direct Lighting Strength** property on the [Ambient Occlusion](Override-Ambient-Occlusion.md) component override. As the backplate does not have global illumination, it can only get ambient occlusion from direct lighting.

**Limitation**: The backplate only appears in local reflection probes and it does not appear in the default sky reflection. This is because the default sky reflection is a cubemap projected at infinity which is incompatible with how Unity renders the backplate.
