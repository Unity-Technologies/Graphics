## Light component reference

The properties available for Lights are in separate sections. Each section contains some properties that all Lights share, and also properties that customize the behavior of the specific type of Light. These sections also contain [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) that you can expose if you want to fine-tune your light's behavior. The sections are:

- [General](#General)
- [Shape](#Shape)
- [Celestial Body](#CelestialBody)
- [Emission](#Emission)
- [Volumetrics](#Volumetric)
- [Shadows](#Shadow)

### Animation

To make the Light work with the **Animation window**, when you click on the **Add Property** button, you need to use the properties inside the **HD Additional Light Data** component and not inside the Light component itself. If you do edit the properties inside the Light component, this modifies the built-in light values, which HDRP doesn't support. Alternatively, you can use the record button and modify the values directly inside the Inspector.

<a name="General"></a>

### General

**General** properties control the type of Light, how HDRP processes this Light, and whether this Light affects everything in the Scene or just GameObjects on a specific Rendering Layer.

| **Property**             | **Description**                                              |
| ------------------------ | ------------------------------------------------------------ |
| **Type**                 | Defines the Light’s type. Lights of different Types behave differently, so when you change the **Type**, the properties change in the Inspector. Possible types are:<br />&#8226; Directional<br />&#8226; Point<br />&#8226; Spot<br />&#8226; Area |
| **Mode**                 | Specify the [Light Mode](https://docs.unity3d.com/Manual/LightModes.html) that HDRP uses to determine how to bake a Light, if at all. Possible modes are:<br />&#8226; [Realtime](https://docs.unity3d.com/Manual/LightMode-Realtime.html): Unity performs the lighting calculations for Realtime Lights at runtime, once per frame. <br />&#8226; [Mixed](https://docs.unity3d.com/Manual/LightMode-Mixed.html): Mixed Lights combine elements of both realtime and baked lighting. <br />&#8226; [Baked](https://docs.unity3d.com/Manual/LightMode-Baked.html): Unity performs lighting calculations for Baked Lights in the Unity Editor, and saves the results to disk as lighting data. Note that soft falloff/range attenuation isn't supported for Baked Area Lights. |
| **Rendering Layer Mask** | Defines which Rendering Layers this Light affects. The affected Light only lights up Mesh Renderers or Terrain with a matching **Rendering Layer Mask**. To use this property:<br/>&#8226; Set up [light layers](Rendering-Layers.md) in your project.<br/>&#8226; Enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. |

#### Light Types guide

| **Type**        | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Spot**        | Emits light from a specified location and range over which the light diminishes. A Spot Light constrains the light it emits to an angle, which results in a cone-shaped region of illumination. The center of the cone points in the forward direction (z-axis) of the Light GameObject. Light also diminishes at the edges of the Spot Light’s cone. Increase the **Spot Angle** to increase the width of the cone. |
| **Directional** | Creates effects that are similar to sunlight in your Scene. Like sunlight, Directional Lights are distant light sources that HDRP treats as though they are infinitely far away. A Directional Light doesn't have any identifiable source position, and you can place the Light GameObject anywhere in the Scene. <br/>A **Directional Light** illuminates all GameObjects in the Scene as if the Light rays are parallel and always from the same direction. The Light disregards the distance between the Light itself and the target GameObject, so the Light doesn't diminish with distance |
| **Point**       | Projects light out equally in all directions from a point in space. The direction of light hitting a surface is the line from the point of contact back to the center of the Light GameObject. The light intensity diminishes with increased distance from the Light, and it reaches zero at the distance specified in the **Range** field. <br/>Light intensity is inversely proportional to the square of the distance from the source. This is known as the [Inverse-square law](https://en.wikipedia.org/wiki/Inverse-square_law), and is similar to how light behaves in the real world. |
| **Area**        | Projects light from a surface. Light shines in all directions uniformly from the surface of the rectangle. |

<a name="Shape"></a>

### Shape

These settings define the area this Light affects. Each Light **Type** has its own unique **Shape** properties.

#### Spot Light

| **Property**                 | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Shape**                    | HDRP Spot Lights can use three shapes.<br />&#8226; **Cone** : Projects light from a single point at the GameObject’s position, out to a circular base, like a cone. Alter the radius of the circular base  by changing the **Outer Angle** and the **Range**.<br />&#8226; **Pyramid** : Projects light from a single point at the GameObject’s position onto a base that's a square with its side length equal to the diameter of the **Cone**.<br />&#8226; **Box** : Projects light evenly across a rectangular area defined by a horizontal and vertical size. This light has no attenuation unless **Range Attenuation** is checked. |
| **Inner / Outer Spot Angle** | Determines both the outer angle in degrees at the base of a Spot Light’s cone and where the attenuation between the inner cone and the outer cone starts. Lower inner angle values cause the light at the edges of the Spot Light to fade out. Higher values stop the light from fading at the edges. This property is only for Lights with a **Cone Shape**. |
| **Spot Angle**               | The angle in degrees used to determine the size of a Spot Light using a **Pyramid** shape. |
| **Aspect Ratio**             | Adjusts the shape of a Pyramid Spot Light to create rectangular Spot Lights. Set this to 1 for a square projection. Values lower than 1 make the Light wider, from the point of origin. Values higher than 1 make the Light longer. This property is only for Lights with a **Pyramid Shape**. |
| **Radius**                   | The radius of the light source. This has an impact on the size of specular highlights, diffuse lighting falloff, and the softness of baked, ray-traced, and PCSS shadows. This will not have an impact on the angle attenuation of the cone. |
| **Size X**                   | For **Box**. Adjusts the horizontal size of the Box Light. No light shines outside of the dimensions you set. |
| **Size Y**                   | For **Box**. Adjusts the vertical size of the Box Light. No light shines outside of the dimensions you set. |

<a name="DirectionalLight"></a>

#### Directional Light

| **Property**         | **Description**                                              |
| -------------------- | ------------------------------------------------------------ |
| **Angular Diameter** | Allows you to set the area of a distant light source through an angle in degrees. This has an impact on the size of specular highlights, and the softness of baked, ray-traced, and PCSS shadows. |

<a name="PointLight"></a>

#### Point Light

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Radius**   | Defines the radius of the light source. This has an impact on the size of specular highlights, diffuse lighting falloff and the smoothness of baked shadows and ray-traced shadows. |

#### Area Light

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Shape**    | HDRP Area Lights can use three shapes. <br />• **Rectangle** : Projects light from a rectangle shape at the GameObject’s position and orientation, in perpendicular direction, out to a certain **Range**. <br />• **Tube** : Projects light from a single line at the GameObject’s position in every direction, out to a certain **Range**.  This shape is only for **Realtime Mode** at the moment.<br />• **Disc** : Projects light from a disc shape at the GameObject’s position and orientation, in perpendicular direction, out to a certain **Range**.  This shape is only for **Baked Mode** at the moment. |
| **Size X**   | For **Rectangle**. Defines the horizontal size of the Rectangle Light. |
| **Size Y**   | For **Rectangle**. Defines the vertical size of the Rectangle Light. |
| **Length**   | For **Tube**. Defines the length of the Tube Light. The center of the Light is the Transform Position and the Light itself extends out from the center symmetrically. The **Length** is the distance from one end of the tube to the other. |
| **Radius**   | For **Disc**. Define the radius of the Disc Light.           |

<a name="CelestialBody"></a>

### Celestial Body (Directional only)

These settings define the behavior of the light when you use it as a celestial body with the [Physically Based Sky](create-a-physically-based-sky.md).

| **Property**         | **Description**                                              |
| -------------------- | ------------------------------------------------------------ |
| **Affect Physically Based Sky** | When using a **Physically Based Sky**, this displays a sun disc in the sky in this Light's direction. The diameter, color, and intensity of the sun disc match the properties of this Directional Light.<br />This property only appears when you enable [additional properties](More-Options.md) for this section. |
| **- Diameter Multiplier**       | Controls the size of the sun disk by multiplying or overriding the value of the angular diameter. This allows artificially increasing the size of the celestial body on screen without impacting the specular highlights or softness of shadows. Additionally, if the sun is only a few pixels large and very bright, you might experience flickering when using bloom. Using an angular diameter multiplier for rendering the disk will solve this. |
| **- Distance**                  | Controls the distance of the sun disc. This is useful if you have multiple sun discs in the sky and want to change their sort order. HDRP draws sun discs with smaller **Distance** values on top of those with larger **Distance** values. |
| **- Surface Color**             | Sets a 2D (disk) Texture and color multiplier for the surface of the celestial body. Rotate the light component on the Z axis to rotate this texture. |
| **- Shading**                   | Specify the light source used for shading of the Celestial Body.<br />&#8226; **Emission** : Used to simulate a Sun. The celestial body will emit light based on the intensity parameter set in the Emission section.<br />&#8226; **Reflect Sun Light** : Used to simulate moons or planets. The celestial body will be illuminated by a directionaly light.<br />&#8226; **Manual** : Used to simulate moons or planets with complete control over the phase angle and rotation, as well as the reflected light intensity. |
| **-- Sun Light Override**       | Specifiy the Directional Light that should illuminate this Celestial Body. If not specified, HDRP will use the directional light in the scene with the highest intensity. |
| **-- Earthshine**               | Controls the intensity of the light reflected from the planet onto the Celestial Body. |
| **-- Sun Color**                | Color of the artificial light source in **Manual** mode. |
| **-- Sun Intensity**            | Intensity of the artificial light source in **Manual** mode. |
| **-- Phase**                    | Controls the area of the surface illuminated by the Sun in **Manual** mode. A phase value of 0.5 means the surface is fully illuminated. |
| **-- Phase Rotation**           | Rotates the Light Source relatively to the Celestial Body in **Manual** mode. |
| **- Flare Size**                | Controls the size of the flare around the celestial body (in degrees). This is not a physically realist behavior but can be used to simulate sun flare when not using bloom or aerosol anisotropy of the PBR Sky.|
| **- Flare Falloff**             | Controls the falloff rate of flare intensity as the angle from the light increases. |
| **- Flare Tint**                | Controls the tint of the flare of the celestial body. |
| **- Flare Multiplier**          | Multiplier applied on the flare intensity. |

<a name="Emission"></a>

### Emission

These settings define the emissive behavior of your Light. You can set the Light’s color, strength, and maximum range. If you don't see these properties in the Light Inspector, make sure you enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html). Most Lights share **Emission** properties. Below are the list of properties that more than one Light **Type** share, followed by unique properties only available for a single Light **Type**.

#### Shared Properties

| **Property**                | **Description**                                              |
| --------------------------- | ------------------------------------------------------------ |
| **Color Temperature**       | Enable the checkbox to set the color temperature mode for this Light. Color Temperature mode adjusts the color of your Light based on a red-to-blue kelvin temperature scale. When enabled, this hides the **Color** property and exposes **Filter** and **Temperature**. Disable this checkbox to only display the **Color** field in the Inspector and use that as the Light color, without the temperature. |
| **- Filter**                | Allows you to select the color of the Light’s filter using the color picker. HDRP uses this and the **Temperature** property to calculate the final color of the Light. |
| **- Temperature**           | Allows you to select a temperature that HDRP uses to calculate a color on a red-to-blue kelvin temperature scale. You can move the slider along the scale itself, or specify an exact temperature value in the field below the slider scale.<br/>This property includes an icon to the right of the slider which represents the light source that best matches the current value set. The icon is also a button which you can click to access a list of preset values that match real world light sources. |
| **Color**                   | Allows you to select the color of the Light using the color picker. |
| **Intensity**               | The strength of the Light. Intensity is expressed in the following units: <br />&#8226; A Spot Light can use [Lumen](Physical-Light-Units.md#Lumen), [Candela](Physical-Light-Units.md#Candela), [Lux](Physical-Light-Units.md#Lux), and [EV<sub>100</sub>](Physical-Light-Units.md#EV).<br />&#8226; A Directional Light can only use **Lux**.<br />&#8226; A Point Light can use **Lumen**, **Candela**, **Lux**, and **EV<sub>100</sub>**.<br />&#8226; A Area Light can use **Lumen**, [Nits](Physical-Light-Units.md#Nits), and **EV<sub>100</sub>**.<br /><br />Generally, the further the light travels from its source, the weaker it gets. The only exception to this is the **Directional Light** which has the same intensity regardless of distance. For the rest of the Light types, lower values cause light to diminish closer to the source. Higher values cause light to diminish further away from the source.<br><br/>This property includes an icon to the right of the slider which represents the light source that best matches the current value set. The icon is also a button which you can click to access a list of preset values that match real world light sources. |
| **Range**                   | The range of influence for this Light. Defines how far the emitted light reaches. This property is available for all **Light Types** except **Directional**. |
| **Indirect Multiplier**     | The intensity of [indirect](https://docs.unity3d.com/Manual/LightModes-TechnicalInformation.html) light in your Scene. A value of 1 mimics realistic light behavior. A value of 0 disables indirect lighting for this Light. If both **Realtime** and **Baked** Global Illumination are disabled in Lighting Settings (menu: **Window > Rendering > Lighting Settings**), the Indirect Multiplier has no effect. |
| **Cookie**                  | An RGB Texture that the Light projects. For example, to create silhouettes or patterned illumination for the Light. Texture shapes should be 2D for Spot and Directional Lights and Cube for Point Lights. Always import **Cookie** textures as the default texture type. This property is available for **Spot**, **Area** (Rectangular only), **Directional**, and **Point** Lights.<br />Pyramid and Box lights will use an implicit 4x4 white cookie if none is specified. |
| **IES Profile**             | An IES File that describes the light profile. HDRP uses a linear average of a cookie and an IES profile in your scene. If you use an IES profile and a cookie at the same time during light baking, the Light in your scene only uses the cookie. You can't assign an IES file with code. Instead, use the **Cookie** property with the Textures that IES generates. |
| **IES cutoff angle (%)**    | Cut off of the IES Profile, as a percentage of the Outer angle. During a baking of a lightmap this parameter isn't used. |
| **Affect Diffuse**          | Enable the checkbox to apply [diffuse](<https://docs.unity3d.com/Manual/shader-NormalDiffuse.html>) lighting to this Light.<br />This property only appears when you enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. It's only available in Realtime or Mixed light **Mode**. |
| **Affect Specular**         | Enable the checkbox to apply [specular](https://docs.unity3d.com/Manual/shader-NormalSpecular.html) lighting to this Light.<br />This property only appears when you enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html)for this section. It's only available in Realtime or Mixed light **Mode**. |
| **Range Attenuation**       | Enable the checkbox to make this Light shine uniformly across its range. This stops light from fading around the edges. This setting is useful when the range limit isn't visible on screen, and you don't want the edges of your light to fade out. This property is available for all **Light Types** except **Directional**.<br />This property only appears when you enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. It's only available in Realtime or Mixed light **Mode** for **Type** Area. |
| **Fade Distance**           | The distance between the Light source and the Camera at which the Light begins to fade out. Measured in meters. This property is available for all **Light Types** except **Directional**.<br />This property only appears when you enable [additional properties](expose-all-additional-properties.md) for this section. It's only available in Realtime or Mixed light **Mode**. |
| **Intensity Multiplier**    | A multiplier that gets applied to the intensity of the Light. Doesn't affect the intensity value, but only gets applied during the evaluation of the lighting. You can also modify this property via [Timeline](https://docs.unity3d.com/Manual/TimelineSection.html), Scripting or [animation](https://docs.unity3d.com/Manual/animeditor-AnimatingAGameObject.html). The parameter lets you fade the Light in and out without having to store its original intensity.<br />This property does not affect the [Physically Based Sky](physically-based-sky-volume-override-reference.html) rendering for the main directionnal light.<br />This property only appears when you enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. It's only available in Realtime or Mixed light **Mode**. |
| **Display Emissive Mesh**   | Enable the checkbox to make Unity automatically generate a Mesh with an emissive Material using the size, color, and intensity of this Light. Unity automatically adds the Mesh and Material to the GameObject the Light component is attached to. This property is available for **Rectangle** and **Tube** Lights.<br />This property only appears when you enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. (In case of an IES profile and a cookie used at the same time, only the cookie will be displayed). |
| **Include For Ray Tracing** | Enable the checkbox to make this Light active when you enable the **Ray Tracing** [Frame Setting](Frame-Settings.md) on the Camera. This applies to rasterization and [ray tracing](Ray-Tracing-Getting-Started.md) passes.<br />This property only appears when you enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. It's only available in Realtime or Mixed light **Mode**. |
| **Include For Path Tracing** | Enable the checkbox to make this Light active when [Path Tracing](Ray-Tracing-Path-Tracing.md) is enabled. |

#### Spot Light

| **Property**  | **Description**                                              |
| ------------- | ------------------------------------------------------------ |
| **Reflector** | Enable the checkbox to simulate a reflective surface behind the Spot Light. Spot Lights are Point Lights that are partly occluded at the back by a reflective surface. Simulating this reflective surface increases the intensity of the Spot Light because the reflective surface reflects light originally directed backwards to focus the intensity in the Spot Light’s direction. |

#### Directional Light

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Size X**   | The horizontal size of the projected cookie texture in pixels.<br />This property only appears when you set a **Cookie** in the Light Inspector. |
| **Size Y**   | The vertical size of the projected cookie texture in pixels.<br />This property only appears when you set a **Cookie** in the Light Inspector. |

<a name="Volumetric"></a>

### Volumetrics

These settings define the volumetric behavior of this Light. Alter these settings to change how this Light behaves with [Atmospheric Scattering](Atmospheric-Scattering.md). All Light **Types** share the same **Volumetric** properties, except **Area** Light. It's only available in Realtime or Mixed light **Mode**.

#### Shared Properties

| **Property**      | **Description**                                              |
| ----------------- | ------------------------------------------------------------ |
| **Enable**        | Enable the checkbox to simulate light scattering through volumetric fog. Enabling this property allows you to edit the **Dimmer** and **Shadow Dimmer** properties. |
| **Dimmer**        | Dims the volumetric lighting effect of this Light.           |
| **Shadow Dimmer** | Dims the volumetric fog effect of this Light. Set this property to 0 to make the volumetric scattering compute faster. |

<a name="Shadow"></a>

### **Shadows**

Use this section to adjust the Shadows cast by this Light.

Unity exposes extra properties in this section depending on the **Mode** you set in the [General](#general) section. Unity also exposes extra properties depending on the **Filtering Quality** set in your Unity Project’s [HDRP Asset](HDRP-Asset.md).

&#8226; For more information on shadow filtering in HDRP, see [Shadow Filtering](Shadows-in-HDRP.md##ShadowFiltering).

&#8226; For a list of the available filter quality presets in HDRP, see the [Filtering Qualities table](HDRP-Asset.md#filtering-quality).

#### Properties

##### Shadow Map

This section is only available in Realtime or Mixed light **Mode**.

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Enable**                 | Enable the checkbox to let this Light cast shadows.          |
| **Update Mode**            | Use the drop-down to select the mode that HDRP uses to determine when to update a shadow map.<br />For information on the modes available, see the [Shadows in HDRP documentation](shadow-update-mode.md). |
| **Resolution**             | Set the resolution of this Light’s shadow maps. Use the drop-down to select which quality mode to derive the resolution from. If you don't enable **Use Quality Settings**, or you select **Custom**, set the resolution, measured in pixels, in the input field.<br/>A higher resolution increases the fidelity of shadows at the cost of GPU performance and memory usage, so if you experience any performance issues, try using a lower value. Shadows can be turned off by setting the resolution to 0. |
| **Near Plane**             | The distance, in meters, from the Light that GameObjects begin to cast shadows. |
| **Shadowmask Mode**        | Defines how the shadowmask behaves for this Light. For detailed information on each **Shadowmask Mode**, see the documentation on [Shadowmasks](Lighting-Mode-Shadowmask.md). This property is only visible if you tet the **Mode**, under [General](#general), to **Mixed**. |
| **Slope-Scale Depth Bias** | Use the slider to set the bias that HDRP adds to the distance in this Light's shadow map to avoid self intersection. This bias is proportional to the slope of the polygons represented in the shadow map.<br /> This property only appears when you enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. |
| **Normal Bias**            | Controls the amount of normal [bias](https://docs.unity3d.com/Manual/ShadowOverview.html#LightBias) this Light applies along the [normal](https://docs.unity3d.com/Manual/AnatomyofaMesh.html) of the illuminated surface.<br />This property only appears when you enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. |
| **Custom Spot Angle**      | Enable the checkbox to use a custom angle to render shadow maps with.<br /> This property only appears if you select **Spot** from the **Type** drop-down and enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. |
| **Shadow Angle**           | Use the slider to set a custom angle to use for shadow map rendering.<br /> This property only appears if you enable **Custom Spot Angle** and enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. |
| **Shadow Cone**            | Use the slider to set the aperture of the shadow cone this area Light uses for shadowing. This property only appears if you select **Rectangle** from the **Type** drop-down. |
| **EVSM Exponent**          | Use the slider to set the exponent this area Light uses for depth warping. [EVSM](Glossary.md#ExponentialVarianceShadowMap) modifies its shadow distribution representation by this exponent. Increase this value to reduce light leaking and change the appearance of the shadow. This property only appears if you select **Rectangle** from the **Type** drop-down and enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. |
| **Light Leak Bias**        | Use this slider to set the bias that HDRP uses to prevent light leaking through Scene geometry. Increasing this value prevents light leaks, but removes some of the shadow softness. This property only appears if you select **Rectangle** from the **Type** drop-down and enable [additional properties](expose-all-additional-properties.md) for this section. |
| **Variance Bias**          | Use the slider to fix numerical accuracy issues in the [EVSM](Glossary.md#ExponentialVarianceShadowMap).  This property only appears if you select **Rectangle** from the **Type** drop-down and enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. |
| **Blur Passes**            | Use the slider to set the number of blur passes HDRP performs on this shadow map. Increasing this value softens shadows, but impacts performance. This property only appears if you select **Rectangle** from the **Type** drop-down and enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. |
| **Dimmer**                 | Dims the shadows this Light casts so they become more faded and transparent.<br />This property only appears when you enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. |
| **Tint**                   | Specifies whether HDRP should tint the shadows this Light casts. This option affects dynamic shadows, [Contact Shadows](Override-Contact-Shadows.md), and [ShadowMask](Lighting-Mode-Shadowmask.md). It doesn't affect baked shadows. You can use this behavior to change the color and transparency of shadows.<br />This property only appears when you enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. |
| **Penumbra Tint**          | Specifies whether the tint should only affect the shadow's penumbra. If you enable this property, HDRP only applies the color tint to the shadow's penumbra. If you disable this property, HDRP applies the color tint to the entire shadow including the penumbra. To change the color HDRP tints the shadow to, see the above **Tint** property.<br />This property only appears when you enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html)for this section. |
| **Fade Distance**          | The distance, in meters, between the Camera and the Light at which shadows fade out. This property is available for **Spot** and **Point** Lights.<br />This property only appears when you enable [additional properties](expose-all-additional-properties.md) for this section. |
| **Custom Shadow Layers**   | Enable the checkbox to use a different [Rendering Layer Mask](Rendering-Layers.md) for shadows than the one used for lighting. If you enable this feature, then HDRP uses the **Shadow Layers** drop-down in this section for shadowing. If you disable it, then HDRP uses the **Rendering Layer Mask** drop-down in the **General** section for shadowing. <br />This property only appears when you enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. To access this property, enable **Light Layers** in your [HDRP Asset](HDRP-Asset.md). |
| **Shadow Layers**          | Use the drop-down to set the [Rendering Layer Mask](Rendering-Layers.md) HDRP uses for shadowing. This Light therefore only casts shadows for GameObjects that use a matching Rendering Layer. For more information about using Rendering Layers for shadowing, see [Shadow Light Layers](Rendering-Layers.md#ShadowLightLayers).<br /> This property only appears when you enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for this section. To access this property, enable the **Custom Shadow Layers** checkbox. |

##### Contact Shadows

This section is only available in Realtime or Mixed light **Mode**.

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Enable**   | Add [Contact Shadows](Override-Contact-Shadows.md) to this Light. Use the drop-down to select a quality mode for the Contact Shadows. Select **Custom** to expose a checkbox that allows you to enable or disable Contact Shadows at will. |

##### Baked Shadows

This section is only available in Baked light **Mode**.

| **Property**   | **Description**                                              |
| -------------- | ------------------------------------------------------------ |
| **Enable**     | Enable the checkbox to let this Light cast shadows.          |
| **Near Plane** | The distance, in meters, from the Light that GameObjects begin to cast shadows. |

##### High Filtering Quality properties

In your [HDRP Asset](HDRP-Asset.md), select **High** from the **Filtering Quality** drop-down to expose the following properties.

| **Property**                   | **Description**                                              |
| ------------------------------ | ------------------------------------------------------------ |
| **Shadow Softness**            | Defines the behavior of area light shadows. Higher softness values mimic a larger emission radius while lower values mimic a [punctual light](Glossary.md#PunctualLight). High values increase shadow blur depending on the distance between the pixel receiving the shadow and the shadow caster. |
| **Blocker Sample Count**       | The number of samples HDRP uses to evaluate the distance between the pixel receiving the shadow and the shadow caster. Higher values give better accuracy. |
| **Filter Sample Count**        | The number of samples HDRP uses to blur shadows. Higher values give smoother results. |
| **Minimum Size of the Filter** | The minimum size of the whole shadow’s blur effect, no matter the distance between the pixel and the shadow caster. Higher values give blurrier results. |
