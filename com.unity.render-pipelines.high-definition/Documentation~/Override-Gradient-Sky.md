# Gradient Sky

The Gradient Sky Volume component override controls settings relevant to rendering a simple representation of the sky. It allows you to define three colors:

* **Top**
* **Middle**
* **Bottom**

HDRP interpolates between these colors to create a gradient sky. You can alter these values at runtime.

This component also exposes options that you can use to define how HDRP updates the indirect lighting the sky generates in the Scene.

##  Using Gradient Sky

**Gradient Sky** uses the [Volume](Volumes.md) framework, so to enable and modify **Gradient Sky** properties, you must add a **Gradient Sky** override to a [Volume](Volumes.md) in your Scene. To add **Gradient  Sky** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override** > **Sky** and select on **Gradient Sky**.

After you add a **Gradient Sky** override, you must set the Volume to use **Gradient Sky**. The [Visual Environment](Override-Visual-Environment.md) override controls which type of sky the Volume uses. To set the Volume to use **Gradient Sky**:

1. In the **Visual Environment** override, go to **Sky** > **Sky Type**
2. Set **Sky Type** to **Gradient Sky**.

HDRP now renders a **Gradient Sky** for any Camera this Volume affects.

[!include[](snippets/volume-override-api.md)]

## Properties

![](Images/Override-GradientSky1.png)

[!include[](snippets/Volume-Override-Enable-Properties.md)]

<table>
<thead>
  <tr>
    <th><strong>Property</strong></th>
    <th></th>
    <th><strong>Description</strong></th>
  </tr>
</thead>
<tbody>
  <tr>
    <td><strong>Top</strong></td>
    <td></td>
    <td>Use the color picker to select the color of the upper hemisphere of the sky.</td>
  </tr>
  <tr>
    <td><strong>Middle</strong></td>
    <td></td>
    <td>Use the color picker to select the color of the horizon.</td>
  </tr>
  <tr>
    <td><strong>Bottom</strong></td>
    <td></td>
    <td>Use the color picker to select the color of the lower hemisphere of the sky. This is below the horizon.</td>
  </tr>
  <tr>
    <td><strong>Gradient Diffusion</strong></td>
    <td></td>
    <td>Set the size of the <strong>Middle</strong> property in the Skybox. Higher values make the gradient thinner, shrinking the size of the <strong>Middle</strong> section. Low values make the gradient thicker, increasing the size of the <strong>Middle</strong> section.</td>
  </tr>
  <tr>
    <td><strong>Intensity Mode</strong></td>
    <td></td>
    <td>Use the drop-down to select the method that HDRP uses to calculate the sky intensity.<br/><br/>&#8226; <strong>Exposure</strong>: HDRP calculates intensity from an exposure value in EV100.<br/>&#8226; <strong>Multiplier</strong>: HDRP calculates intensity from a flat multiplier.</td>
  </tr>
  <tr></strong>
    <td></td>
    <td><strong>Exposure</strong></td>
    <td>Set the amount of light per unit area that HDRP applies to the HDRI Sky cubemap.<br/>This property only appears when you select <strong>Exposure</strong> from the <strong>Intensity Mode</strong> drop-down.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Multiplier</strong></td>
    <td>Set the multiplier for HDRP to apply to the Scene as environmental light. HDRP multiplies the environment light in your Scene by this value.<br/>This property only appears when you select <strong>Multiplier</strong> from the <strong>Intensity Mode</strong> drop-down.</td>
  </tr>
  <tr>
    <td><strong>Update Mode</strong></td>
    <td></td>
    <td>Use the drop-down to set the rate at which HDRP updates the sky environment (using Ambient and Reflection Probes).<br/><br/>&#8226; <strong>On Changed</strong>: HDRP updates the sky environment when one of the sky properties changes.<br/>&#8226; <strong>On Demand</strong>: HDRP waits until you manually call for a sky environment update from a script.<br/>&#8226; <strong>Realtime</strong>: HDRP updates the sky environment at regular intervals defined by the <strong>Update Period</strong>.</td>
  </tr>
</tbody>
</table>
