# HDRI Sky Volume Override reference

The HDRI Sky Volume Override component provides options to define how the High Definition Render Pipeline (HDRP) renders an HDRI sky.

Refer to [Create an HDRI sky](create-an-HDRI-sky.md) for more information.

## Properties

[!include[](snippets/Volume-Override-Enable-Properties.md)]

<table>
<thead>
  <tr>
    <th><strong>Property</strong></th>
    <th><strong>Sub-property</strong></th>
    <th><strong>Description</strong></th>
  </tr>
</thead>
<tbody>
  <tr>
    <td><strong>HDRI Sky</strong></td>
    <td>N/A</td>
    <td>Assign an HDRI texture to render the sky in HDRP.</td>
  </tr>
  <tr>
    <td><strong>Distortion Mode</strong></td>
    <td>N/A</td>
    <td>Select how HDRP calculates sky distortion:<br>&#8226; <strong>None</strong>: No distortion.<br>&#8226; <strong>Procedural</strong>: Distorts the sky using uniform wind direction.<br>&#8226; <strong>Flowmap</strong>: Uses a user-provided flowmap for distortion.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Orientation</strong></td>
    <td>Set the distortion orientation relative to the X-world vector (degrees).<br/>This can be relative to the <strong>Global Wind Orientation</strong> in the <strong>Visual Environment</strong>.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Speed</strong></td>
    <td>Define how fast HDRP scrolls the distortion texture. <br>This value can be relative to the <strong>Global Wind Speed</strong> defined in the <strong>Visual Environment</strong>.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Flowmap</strong></td>
    <td>Assign a LatLong flowmap for sky UV distortion.<br/>Visible only when you select <strong>Flowmap</strong> from the <strong>Distortion Mode</strong> drop-down.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Upper Hemisphere Only</strong></td>
    <td>Enable if the flowmap distorts only the sky above the horizon.<br/>Visible only when you select <strong>Flowmap</strong> from the <strong>Distortion Mode</strong> drop-down.</td>
  </tr>
  <tr>
    <td><strong>Intensity Mode</strong></td>
    <td>N/A</td>
    <td>Choose how HDRP calculates sky intensity:<br>&#8226; <strong>Exposure</strong>: Based on EV100 exposure.<br>&#8226; <strong>Multiplier</strong>: Applies a flat multiplier.<br>&#8226; <strong>Lux</strong>: Targets a specific Lux value.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Exposure</strong></td>
    <td>Set the light per unit area applied to the HDRI Sky cubemap.<br/>Visible only when you set <strong>Exposure</strong> in <strong>Intensity Mode</strong> from the <strong>Intensity Mode</strong> drop-down.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Multiplier</strong></td>
    <td>Set a multiplier for environment light in the scene.<br/>Visible only when you select <strong>Multiplier</strong> from the <strong>Intensity Mode</strong> drop-down.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Desired Lux Value</strong></td>
    <td>Set an absolute intensity for the HDR Texture you set in <strong>HDRI Sky</strong>, in <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.1/manual/Physical-Light-Units.html#Lux">Lux</a>. This value represents the light received in a direction perpendicular to the ground. This is similar to the Lux unit you use to represent the Sun, so it's complimentary.<br>Visible only when you select <strong>Lux</strong> from the <strong>Intensity Mode</strong> drop-down.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Upper Hemisphere Lux Value</strong></td>
    <td>Displays the relative intensity, in Lux, for the current HDR texture set in <strong>HDRI Sky</strong>. The final multiplier HDRP applies for intensity is <strong>Desired Lux Value</strong> / <strong>Upper Hemisphere Lux Value</strong>. This field is an informative helper.<br>This property only appears when you select <strong>Lux</strong> from the <strong>Intensity Mode</strong> drop-down.</td>
  </tr>
  <tr>
    <td><strong>Rotation</strong></td>
    <td>N/A</td>
    <td>Use the slider to set the angle to rotate the cubemap, in degrees.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Lock Sun</strong></td>
    <td>Make the Sun rotate automatically when you move the HDRI Sky, and the HDRI Sky rotate automatically when you rotate the sun.</td>
  </tr>
  <tr>
    <td><strong>Update Mode</strong></td>
    <td>N/A</td>
    <td>Use the drop-down to set the rate at which HDRP updates the sky environment (using Ambient and Reflection Probes).<br>&#8226; <strong>On Changed</strong>: HDRP updates the sky environment when one of the sky properties changes.<br>&#8226; <strong>On Demand</strong>: HDRP waits until you manually call for a sky environment update from a script.<br>&#8226; <strong>Realtime</strong>: HDRP updates the sky environment at regular intervals defined by the Update Period.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Update Period</strong></td>
    <td>Set the update interval in seconds. Use <strong>0</strong> for per-frame updates. Visible only when you set the <strong>Update Mode</strong> to <strong>Realtime</strong>.</td>
  </tr>
</tbody>
</table>

## Advanced Properties

These properties only appear if you enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) and then enable **Backplate**.

<table>
<thead>
  <tr>
    <th><strong>Property</strong></th>
    <th><strong>Option</strong></th>
    <th><strong>Sub-option</strong></th>
    <th><strong>Description</strong></th>
  </tr>
</thead>
<tbody>
  <tr>
    <td><strong>Backplate</strong></td>
    <td>N/A</td>
    <td>N/A</td>
    <td>Projects the lower hemisphere of the HDRI onto a selected shape (Rectangle, Circle, Ellipse, or Infinite plane).</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Type</strong></td>
    <td>N/A</td>
    <td>Specifies the shape of the backplate.<br/><br/>&#8226; <strong>Disc</strong>: Projects the bottom of the HDRI texture onto a disc.<br/><br/>&#8226; <strong>Rectangle</strong>: Projects the bottom of the HDRI texture onto a rectangle.<br/><br/>&#8226; <strong>Ellipse</strong>: Projects the bottom of the HDRI texture onto an ellipse.<br/><br/>&#8226; <strong>Infinite</strong>: Projects the bottom of the HDRI texture onto an infinite plane.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Ground Level</strong></td>
    <td>N/A</td>
    <td>Specifies the height of the ground in the scene.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Scale</strong></td>
    <td>N/A</td>
    <td>The scale of the backplate. HDRP uses the X and Y values of this property to scale the backplate (for Ellipse <strong>X</strong> and <strong>Y</strong> must be different).</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Projection</strong></td>
    <td>N/A</td>
    <td>HDRP uses this number to control the projection of the bottom hemisphere of the HDRI on the backplate. Small projection distance implies higher pixel density with more distortion, large projection distance implies less pixel density with less distortion.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Rotation</strong></td>
    <td>N/A</td>
    <td>Rotates the physical backplate.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Texture Rotation</strong></td>
    <td>N/A</td>
    <td>Rotates the HDRI texture projected onto the backplate.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Texture Offset</strong></td>
    <td>N/A</td>
    <td>Offsets the texture projected onto the backplate.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Blend Amount</strong></td>
    <td>N/A</td>
    <td>The percentage of the transition between the backplate and the background HDRI. <strong>0</strong> means no blending, <strong>25</strong> means the blending starts at the end of the boundary of the backplate, <strong>50</strong> means the blending starts from the middle of the backplate and <strong>100</strong> means the transition starts from the center of the backplate with a smoothstep.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Point/Spot Shadow</strong></td>
    <td>N/A</td>
    <td>Indicates whether the backplate receives shadows from point/spot <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.1/manual/Light-Component.html">Lights</a>.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Directional Shadow</strong></td>
    <td>N/A</td>
    <td>Enables shadows from the main directional light on the backplate.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Area Shadow</strong></td>
    <td>N/A</td>
    <td>Indicates whether the backplate receives shadows from area Lights.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Shadow Tint</strong></td>
    <td>N/A</td>
    <td>Specifies the color to tint shadows cast onto the backplate.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Reset Color</strong></td>
    <td>N/A</td>
    <td>Resets the saved Shadow Tint for the shadow. HDRP calculates a new default shadow tint when the HDRI changes.</td>
  </tr>
</tbody>
</table>

**Note**: To use ambient occlusion in the backplate, increase the value of the **Direct Lighting Strength** property on the [Ambient Occlusion](Override-Ambient-Occlusion.md) component override. As the backplate doesn't have global illumination, it can only get ambient occlusion from direct lighting.

**Limitation**: The backplate only appears in local reflection probes and doesn't appear in the default sky reflection. This is because the default sky reflection is a cubemap projected at infinity which is incompatible with how Unity renders the backplate.
