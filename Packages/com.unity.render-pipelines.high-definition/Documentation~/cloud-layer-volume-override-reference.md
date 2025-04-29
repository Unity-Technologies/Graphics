# Cloud Layer Volume Override reference

The Cloud Layer Volume Override lets you configure a simple representation of clouds.

## Properties

[!include[](snippets/Volume-Override-Enable-Properties.md)]

| Property                  | Description                                                  |
| ------------------------- | ------------------------------------------------------------ |
| **Opacity**               | The global opacity of the cloud layer. A value of 0 makes clouds completely transparent. |
| **Upper Hemisphere Only** | Indicates whether the Cloud Layer exclusively renders above the horizon or not. When enabled, HDRP still uses the entire **Cloud Map** texture, but the clouds will be renderer above the horizon. |
| **Layers**                | The number of cloud layers to render. Each layer has its own set of properties. The options are:<br/>&#8226; **1**: Renders a single cloud layer.<br/>&#8226; **2**: Renders two cloud layers. |
| **Resolution**            | The resolution of the texture HDRP uses to bake the clouds.  |

### Per-layer

The Inspector shows the following properties for each cloud layer. The **Layers** property determines the number of cloud layers to render.

| Property               | Description                                                  |
| ---------------------- | ------------------------------------------------------------ |
| **Cloud Map**          | Assign a Texture that HDRP uses to render the cloud layer. For more information about the cloud map, see [about the cloud map](create-simple-clouds-cloud-layer#about-the-cloud-map). |
| - **Opacity R**        | The opacity multiplier to apply to the **Cloud Map**'s red channel. |
| - **Opacity G**        | The opacity multiplier to apply to the **Cloud Map**'s green channel. |
| - **Opacity B**        | The opacity multiplier to apply to the **Cloud Map**'s blue channel. |
| - **Opacity A**        | The opacity multiplier to apply to the **Cloud Map**'s alpha channel. |
| **Altitude**           | The altitude of the Cloud Layer in meters, used to calculate the sun light attenuation. |
| **Rotation**           | The angle to rotate the **Cloud Layer** texture by, in degrees. |
| **Tint**               | The color HDRP uses to tint the clouds.                      |
| **Exposure Compensation** | The exposure compensation of the clouds in EV relative to the main directional [Light](Light-Component.md). |
| **Wind**               | Specifies the method HDRP uses to simulate wind.<br />&#8226; **None**: No wind.<br />&#8226; **Horizontal**: HDRP distorts the clouds using a uniform horizontal wind direction.<br />&#8226; **Flowmap**: HDRP distorts the clouds using the **Flowmap** texture. |
| - **Orientation**      | The orientation of the wind relative to the X world vector (in degrees).<br />This value can be relative to the **Global Wind Orientation** defined in the **Visual Environment**. |
| - **Speed**            | The wind speed in kilometers per hour.<br />This value can be relative to the **Global Wind Speed** defined in the **Visual Environment**. |
| - **Flowmap**          | The flowmap HDRP uses to distort UVs when rendering the clouds. For more information about the flowmap, see [controlling cloud movement](create-simple-clouds-cloud-layer#controlling-cloud-movement).<br />This property only appears when you select **Flowmap** from the **Distortion** drop-down. |
| **Raymarching**        | Indicates whether HDRP calculates lighting for the clouds using the main directional light. When enabled, HDRP uses 2D raymarching on the Cloud Map to approximate self-shadowing from the sun light.<br /> The lighting computations are baked inside a texture and only recomputed when any of the relevant parameter changes. |
| - **Steps**            | The number of raymarching steps HDRP uses to calculate lighting for the clouds. The higher the value, the greater the travelled distance is. |
| - **Density**          | The density of the clouds. The larger the value, the darker the clouds will appear. |
| - **Ambient Probe Dimmer** | Controls the influence of the ambient probe on the cloud layer volume. A lower value will suppress the ambient light and produce darker clouds overall. |
| **Cast Shadows**       | Indicates whether clouds cast shadows for the main directional light.<br/>This calculates the shadow texture and sets it as the light cookie for the main direction Light. |

| Shadows Property      | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| **Shadow Multiplier** | The opacity of the cloud shadows. The higher the value, the darker the shadows. |
| **Shadow Tint**       | The tint HDRP applies to the cloud shadows.                  |
| **Shadow Resolution** | The resolution of the cloud shadows texture.                 |
| **Shadow Size**       | The size of the projected cloud shadows.                     |
