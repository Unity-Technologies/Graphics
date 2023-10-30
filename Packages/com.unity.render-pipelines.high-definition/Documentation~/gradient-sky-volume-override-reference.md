# Gradient Sky Volume Override reference

![](Images/Override-GradientSky1.png)

The Gradient Sky Volume Override component exposes options that you can use to define how the High Definition Render Pipeline (HDRP) updates the indirect lighting the sky generates in the Scene.

[!include[](snippets/Volume-Override-Enable-Properties.md)]

Refer to [Create a gradient sky](create-a-gradient-sky.md) for more information.

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
