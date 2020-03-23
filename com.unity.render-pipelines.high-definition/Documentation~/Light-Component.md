# Lights in the High Definition Render Pipeline

Use the Light component to create light sources in your [Scene](https://docs.unity3d.com/Manual/CreatingScenes.html). The Light component controls the shape, color, and intensity of the light. It also controls whether or not the Light casts shadows in your Scene, as well as more advanced settings.

![](Images/HDRPFeatures-LightTypes.png)

## Creating Lights

There are two ways to add Lights to your Scene. You can create a new Light [GameObject](https://docs.unity3d.com/Manual/class-GameObject.html), or you can add a Light component to an existing GameObject.

To create a new Light Gameobject:

1. In the Unity Editor, go to **GameObject > Light**
2. Select the Light type you want to add.

Unity creates a new GameObject and attaches a Light component, as well as another High Definition Render Pipeline (HDRP) component: **HD Additional Light Data**. Unity places the new Light GameObject into your Scene, centered on your current view in the Scene window.

To add a Light component to an existing GameObject:

1. Select the GameObject you want to add a Light.
2.  In the Inspector, click **Add Component**.
3. Go to the **Rendering** section, and click **Light**. This creates a new Light source and attaches it to the currently selected GameObject. It also adds the **HD Additional Light Data** HDRP component.

Alternatively, you can search for "Light" in the **Add Component** window, then click **Light** to add the Light component.

## Configuring Lights

To configure the properties of a Light, select a GameObject in your Scene with a Light component attached. See the Light’s properties in the Inspector window.

HDRP includes multiple types of Light. Although HDRP Lights share many properties, each one has its own unique behavior and set of properties.

For more detailed information on how to configure realistic light fixtures, see the [Create High-Quality Light Fixtures in Unity](https://docs.unity3d.com/uploads/ExpertGuides/Create_High-Quality_Light_Fixtures_in_Unity.pdf) expert guide.

### Properties

The properties available for Lights are in separate sections. Each section contains some properties that all Lights share, and also properties that customize the behavior of the specific type of Light. These sections also contain [more options](More-Options.html) that you can expose if you want to fine-tune your light's behavior. The sections are:

- [General](#GeneralProperties)
- [Shape](#ShapeProperties)
- [Celestial Body](#CelestialBodyProperties)
- [Emission](#EmissionProperties)
- [Volumetrics](#VolumetricProperties)
- [Shadows](#ShadowProperties)

### Animation

To make the Light work with the **Animation window**, when you click on the **Add Property** button, you need to use the properties inside the **HD Additional Light Data** component and not inside the Light component itself. If you do edit the properties inside the Light component, this modifies the built-in light values, which HDRP does not support. Alternatively, you can use the record button and modify the values directly inside the Inspector.

<a name="GeneralProperties"></a>

### General

**General** properties control the type of Light, how HDRP processes this Light, and whether this Light affects everything in the Scene or just GameObjects on a specific Light Layer.

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Type**        | Defines the Light’s type. Lights of different Types behave differently, so when you change the **Type**, the properties change in the Inspector. Possible types are:<br />&#8226; Directional<br />&#8226; Point<br />&#8226; Spot<br />&#8226; Area |
| **Mode**        | Specify the [Light Mode](https://docs.unity3d.com/Manual/LightModes.html) that HDRP uses to determine how to bake a Light, if at all. Possible modes are:<br />&#8226; [Realtime](https://docs.unity3d.com/Manual/LightMode-Realtime.html) <br />&#8226; [Mixed](https://docs.unity3d.com/Manual/LightMode-Mixed.html) <br />&#8226; [Baked](https://docs.unity3d.com/Manual/LightMode-Baked.html) |
| **Light Layer** | A  mask that allows you to choose which Light Layers this Light affects. The affected Light only lights up Mesh Renderers with a matching **Rendering Layer Mask**.<br />This property only appears when you enable [more options](More-Options.html) for this section. |

#### Light Types guide

| **Type**        | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Spot**        | Emits light from a specified location and range over which the light diminishes. A Spot Light constrains the light it emits to an angle, which results in a cone-shaped region of illumination. The center of the cone points in the forward direction (z-axis) of the Light GameObject. Light also diminishes at the edges of the Spot Light’s cone. Increase the **Spot Angle** to increase the width of the cone. |
| **Directional** | Creates effects that are similar to sunlight in your Scene. Like sunlight, Directional Lights are distant light sources that HDRP treats as though they are infinitely far away. A Directional Light does not have any identifiable source position, and you can place the Light GameObject anywhere in the Scene. A **Directional Light** illuminates all GameObjects in the Scene as if the Light rays are parallel and always from the same direction. The Light disregards the distance between the Light itself and the target GameObject, so the Light does not diminish with distance. |
| **Point**       | Projects light out equally in all directions from a point in space. The direction of light hitting a surface is the line from the point of contact back to the center of the Light GameObject. The light intensity diminishes with increased distance from the Light, and it reaches zero at the distance specified in the **Range** field. Light intensity is inversely proportional to the square of the distance from the source. This is known as the [Inverse-square law](https://en.wikipedia.org/wiki/Inverse-square_law), and is similar to how light behaves in the real world. |
| **Area**        | Projects light from a surface. Light shines in all directions uniformly from the surface of the rectangle. |

<a name="ShapeProperties"></a>

### **Shape**

These settings define the area this Light affects. Each Light **Type** has its own unique **Shape** properties.

#### **Spot Light**

| **Property**        | **Description**                                              |
| ------------------- | ------------------------------------------------------------ |
| **Shape**           | HDRP Spot Lights can use three shapes.<br />&#8226; **Cone** : Projects light from a single point at the GameObject’s position, out to a circular base, like a cone. Alter the radius of the circular base  by changing the **Outer Angle** and the **Range**.<br />&#8226; **Pyramid** : Projects light from a single point at the GameObject’s position onto a base that is a square with its side length equal to the diameter of the **Cone**.<br />&#8226; **Box** : Projects light evenly across a rectangular area defined by a horizontal and vertical size. |
| **Outer Angle**     | The angle in degrees at the base of a Spot Light’s cone. This property is only for Lights with a **Cone Shape**. |
| **Inner Angle (%)** | Determines where the attenuation between the inner cone and the outer cone starts. Higher values cause the light at the edges of the Spot Light to fade out. Lower values stop the light from fading at the edges. This property is only for Lights with a **Cone Shape**. |
| **Spot Angle**      | The angle in degrees used to determine the size of a Spot Light using a **Pyramid** shape. |
| **Aspect Ratio**    | Adjusts the shape of a Pyramid Spot Light to create rectangular Spot Lights. Set this to 1 for a square projection. Values lower than 1 make the Light wider, from the point of origin. Values higher than 1 make the Light longer. This property is only for Lights with a **Pyramid Shape**. |
| **Radius**          | The radius of the light source. This has an impact on the size of specular highlights, diffuse lighting falloff, and the softness of baked, ray-traced, and PCSS shadows. |
| **Size X**          | For **Box**. Adjusts the horizontal size of the Box Light. No light shines outside of the dimensions you set. |
| **Size Y**          | For **Box**. Adjusts the vertical size of the Box Light. No light shines outside of the dimensions you set. |

<a name="DirectionalLight"></a>

#### Directional Light

| **Property**         | **Description**                                              |
| -------------------- | ------------------------------------------------------------ |
| **Angular Diameter** | Allows you to set the area of a distant light source through an angle in degrees. This has an impact on the size of specular highlights, and the softness of baked, ray-traced, and PCSS shadows.|

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

<a name="CelestialBodyProperties"></a>

### **Celestial Body (Directional only)**

These settings define the behavior of the light when you use it as a celestial body with the [Physically Based Sky](Override-Physically-Based-Sky.html).

| **Property**         | **Description**                                              |
| -------------------- | ------------------------------------------------------------ |
| **Affect Physically Based Sky** | When using a **Physically Based Sky**, this displays a sun disc in the sky in this Light's direction. The diameter, color, and intensity of the sun disc match the properties of this Directional Light.<br />This property only appears when you enable [more options](More-Options.html) for this section. |
| **- Flare Size** | Controls the size of the flare around the celestial body (in degrees).. |
| **- Flare Falloff** | Controls the falloff rate of flare intensity as the angle from the light increases. |
| **- Flare Tint** | Controls the tint of the flare of the celestial body. |
| **- Surface Texture** | Sets a 2D (disk) Texture for the surface of the celestial body. This acts like a multiplier. |
| **- Surface Tint** | Tints the surface of the celestial body. |
| **- Distance** | Controls the distance of the sun disc. This is useful if you have multiple sun discs in the sky and want to change their sort order. HDRP draws sun discs with smaller **Distance** values on top of those with larger **Distance** values.<br />This property only appears when you enable [more options](More-Options.html) for this section. |

<a name="EmissionProperties"></a>

### **Emission**

These settings define the emissive behavior of your Light. You can set the Light’s color, strength, and maximum range. If you do not see these properties in the Light Inspector, make sure you expose [more options](More-Options.html). Most Lights share **Emission** properties. Below are the list of properties that more than one Light **Type** share, followed by unique properties only available for a single Light **Type**.

#### Shared Properties

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Color Temperature**     | Enable the checkbox to set the color temperature mode for this Light. Color Temperature mode adjusts the color of your Light based on a red-to-blue kelvin temperature scale. When enabled, this hides the **Color** property and exposes **Filter** and **Temperature**. Disable this checkbox to only display the **Color** field in the Inspector and use that as the Light color, without the temperature. |
| **- Filter**              | Allows you to select the color of the Light’s filter using the colour picker. HDRP uses this and the **Temperature** property to calculate the final color of the Light. |
| **- Temperature**         | Allows you to select a temperature that HDRP uses to calculate a color on a red-to-blue kelvin temperature scale. You can move the slider along the scale itself, or specify an exact temperature value in the field to the right of the slider scale. |
| **Color**                 | Allows you to select the color of the Light using the colour picker. |
| **Intensity**             | The strength of the Light. Intensity is expressed in the following units: <br />&#8226; A Spot Light can use [Lumen](Physical-Light-Units.html#Lumen), [Candela](Physical-Light-Units.html#Candela), [Lux](Physical-Light-Units.html#Lux), and [EV<sub>100</sub>](Physical-Light-Units.html#EV).<br />&#8226; A Directional Light can only use **Lux**.<br />&#8226; A Point Light can use **Lumen**, **Candela**, **Lux**, and **EV<sub>100</sub>**.<br />&#8226; A Area Light can use **Lumen**, [Nits](Physical-Light-Units.html#Nits), and **EV<sub>100</sub>**.<br /><br />Generally, the further the light travels from its source, the weaker it gets. The only exception to this is the **Directional Light** which has the same intensity regardless of distance. For the rest of the Light types, lower values cause light to diminish closer to the source. Higher values cause light to diminish further away from the source. |
| **Range**                 | The range of influence for this Light. Defines how far the emitted light reaches. This property is available for all **Light Types** except **Directional**. |
| **Indirect Multiplier**   | The intensity of [indirect](https://docs.unity3d.com/Manual/LightModes-TechnicalInformation.html) light in your Scene. A value of 1 mimics realistic light behavior. A value of 0 disables indirect lighting for this Light. If both **Realtime** and **Baked** Global Illumination are disabled in Lighting Settings (menu: **Window > Rendering > Lighting Settings**), the Indirect Multiplier has no effect. |
| **Cookie**                | An RGB Texture that the Light projects. For example, to create silhouettes or patterned illumination for the Light. Texture shapes should be 2D for Spot and Directional Lights and Cube for Point Lights. Always import **Cookie** textures as the default texture type. This property is available for **Spot**, **Directional**, and **Point** Lights. |
| **Affect Diffuse**        | Enable the checkbox to apply [diffuse](<https://docs.unity3d.com/Manual/shader-NormalDiffuse.html>) lighting to this Light.<br />This property only appears when you enable [more options](More-Options.html) for this section. It is only available in Realtime or Mixed light **Mode**. |
| **Affect Specular**       | Enable the checkbox to apply [specular](https://docs.unity3d.com/Manual/shader-NormalSpecular.html) lighting to this Light.<br />This property only appears when you enable [more options](More-Options.html) for this section. It is only available in Realtime or Mixed light **Mode**. |
| **Range Attenuation**     | Enable the checkbox to make this Light shine uniformly across its range. This stops light from fading around the edges. This setting is useful when the range limit is not visible on screen, and you do not want the edges of your light to fade out. This property is available for all **Light Types** except **Directional**.<br />This property only appears when you enable [more options](More-Options.html) for this section. It is only available in Realtime or Mixed light **Mode** for **Type** Area. |
| **Fade Distance**         | The distance between the Light source and the Camera at which the Light begins to fade out. Measured in meters. This property is available for all **Light Types** except **Directional**.<br />This property only appears when you enable [more options](More-Options.html) for this section. It is only available in Realtime or Mixed light **Mode**. |
| **Intensity Multiplier**                | A multiplier that gets applied to the intensity of the Light. Does not affect the intensity value, but only gets applied during the evaluation of the lighting. You can also modify this property via [Timeline](https://docs.unity3d.com/Manual/TimelineSection.html), Scripting or [animation](https://docs.unity3d.com/Manual/animeditor-AnimatingAGameObject.html). The parameter lets you fade the Light in and out without having to store its original intensity.<br />This property only appears when you enable [more options](More-Options.html) for this section. It is only available in Realtime or Mixed light **Mode**. |
| **Display Emissive Mesh** | Enable the checkbox to make Unity automatically generate a Mesh with an emissive Material using the size, colour, and intensity of this Light. Unity automatically adds the Mesh and Material to the GameObject the Light component is attached to. This property is available for **Rectangle** and **Tube** Lights.<br />This property only appears when you enable [more options](More-Options.html) for this section. |

#### Spot Light

| **Property**  | **Description**                                              |
| ------------- | ------------------------------------------------------------ |
| **Reflector** | Enable the checkbox to simulate a reflective surface behind the Spot Light. Spot Lights are Point Lights that are partly occluded at the back by a reflective surface. Simulating this reflective surface increases the intensity of the Spot Light because the reflective surface reflects light originally directed backwards to focus the intensity in the Spot Light’s direction. |

#### Directional Light

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Size X**   | The horizontal size of the projected cookie texture in pixels.<br />This property only appears when you set a **Cookie** in the Light Inspector. |
| **Size Y**   | The vertical size of the projected cookie texture in pixels.<br />This property only appears when you set a **Cookie** in the Light Inspector. |

<a name="VolumetricProperties"></a>

### **Volumetrics**

These settings define the volumetric behavior of this Light. Alter these settings to change how this Light behaves with [Atmospheric Scattering](Atmospheric-Scattering.html). All Light **Types** share the same **Volumetric** properties, except **Area** Light. It is only available in Realtime or Mixed light **Mode**.

#### Shared Properties

| **Property**      | **Description**                                              |
| ----------------- | ------------------------------------------------------------ |
| **Enable**        | Enable the checkbox to simulate light scattering through volumetric fog. Enabling this property allows you to edit the **Dimmer** and **Shadow Dimmer** properties. |
| **Dimmer**        | Dims the volumetric lighting effect of this Light.           |
| **Shadow Dimmer** | Dims the volumetric fog effect of this Light. Set this property to 0 to make the volumetric scattering compute faster. |

<a name="ShadowProperties"></a>

### **Shadows**

Use the Shadows section to adjust the Shadows cast by this Light. HDRP currently does not support shadowing **Tube** Lights. Because of this, Unity does not expose the **Shadows** drop-down section in the Inspector when you select this **Type**. The Light **Types** that HDRP does support shadowing for (**Spot**, **Directional**, and **Point**) share almost all of their properties.

Unity exposes extra properties in the **Shadows** section depending on the **Mode** you set in the [General](#GeneralProperties) section. It also exposes extra properties depending on the **Filtering Quality** set in your Unity Project’s [HDRP Asset](HDRP-Asset.html). To change the **Filtering Quality** property, navigate to your Project’s **HDRP Asset > Shadows** and use the **Filtering Quality** drop-down  to select the shadow filtering mode. Setting **Filtering Quality** to **High** exposes extra properties in the Light Inspector’s **Shadow** drop-down section.

&#8226; For more information on shadow filtering in HDRP, see the documentation on [Shadow Filtering](Shadows-in-HDRP.html#ShadowFiltering).

&#8226; For a list of the the available filter quality presets in HDRP, see the [Filtering Qualities table](HDRP-Asset.html#FilteringQualities).

#### Properties

##### Shadow Map

This section is only available in Realtime or Mixed light **Mode**.

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Enable**                 | Enable the checkbox to let this Light cast shadows.          |
| **Update Mode**            | Use the drop-down to select the mode that HDRP uses to determine when to update a shadow map.<br />For information on the modes available, see the [Shadows in HDRP documentation](Shadows-in-HDRP.html#ShadowUpdateMode). |
| **Resolution**             | Set the resolution of this Light’s shadow maps. Use the drop-down to set the modeIf you enable , use the drop-down to select which quality mode to derive the resolution from. If you do not enable **Use Quality Settings**, set the resolution, measured in pixels, in the input field.A higher resolution increases the fidelity of shadows at the cost of GPU performance and memory usage, so if you experience any performance issues, try using a lower value. |
| **Near Plane**             | The distance, in meters, from the Light that GameObjects begin to cast shadows. |
| **Shadowmask Mode**        | Defines how the shadowmask behaves for this Light. For detailed information on each **Shadowmask Mode**, see the documentation on [Shadowmasks](Shadows-in-HDRP.html#ShadowmaskModes). This property is only visible if you tet the **Mode**, under [General](#GeneralProperties), to **Mixed**. |
| **Slope-Scale Depth Bias** | Use the slider to set the bias that HDRP adds to the distance in this Light's shadow map to avoid self intersection. This bias is proportional to the slope of the polygons represented in the shadow map.<br /> This property only appears when you enable [more options](More-Options.html) for this section. |
| **Normal Bias**            | Controls the amount of normal [bias](https://docs.unity3d.com/Manual/ShadowOverview.html#LightBias) this Light applies along the [normal](https://docs.unity3d.com/Manual/AnatomyofaMesh.html) of the illuminated surface.<br />This property only appears when you enable [more options](More-Options.html) for this section. |
| **Custom Spot Angle**      | Enable the checkbox to use a custom angle to render shadow maps with.<br /> This property only appears if you select **Spot** from the **Type** drop-down and enable [more options](More-Options.html) for this section. |
| **Shadow Angle**           | Use the slider to set a custom angle to use for shadow map rendering.<br /> This property only appears if you enable **Custom Spot Angle** and enable [more options](More-Options.html) for this section. |
| **Shadow Cone**            | Use the slider to set the aperture of the shadow cone this area Light uses for shadowing. This property only appears if you select **Rectangle** from the **Type** drop-down. |
| **EVSM Exponent**          | Use the slider to set the exponent this area Light uses for depth warping. [EVSM](Glossary.html#ExponentialVarianceShadowMap) modifies its shadow distribution representation by this exponent. Increase this value to reduce light leaking and change the appearance of the shadow. This property only appears if you select **Rectangle** from the **Type** drop-down and enable [more options](More-Options.html) for this section. |
| **Light Leak Bias**        | Use this slider to set the bias that HDRP uses to prevent light leaking through Scene geometry. Increasing this value prevents light leaks, but removes some of the shadow softness. This property only appears if you select **Rectangle** from the **Type** drop-down and enable [more options](More-Options.html) for this section. |
| **Variance Bias**          | Use the slider to fix numerical accuracy issues in the [EVSM](Glossary.html#ExponentialVarianceShadowMap).  This property only appears if you select **Rectangle** from the **Type** drop-down and enable [more options](More-Options.html) for this section. |
| **Blur Passes**            | Use the slider to set the number of blur passes HDRP performs on this shadow map. Increasing this value softens shadows, but impacts performance. This property only appears if you select **Rectangle** from the **Type** drop-down and enable [more options](More-Options.html) for this section. |
| **Dimmer**                 | Dims the shadows this Light casts so they become more faded and transparent.<br />This property only appears when you enable [more options](More-Options.html) for this section. |
| **Tint**                   | Specifies whether HDRP should tint the shadows this Light casts. This option affects dynamic shadows, [Contact Shadows](Override-Contact-Shadows.md), and [ShadowMask](Lighting-Mode-Shadowmask.md). It does not affect baked shadows. You can use this behavior to change the color and transparency of shadows.<br />This property only appears when you enable the [more options](More-Options.html) for this section. |
| **Penumbra Tint**          | Specifies whether the tint should only affect the shadow's penumbra.<br />This property only appears when you enable the [more options](More-Options.htmlMore-Options.html) for this section. |
| **Fade Distance**          | The distance, in meters, between the Camera and the Light at which shadows fade out. This property is available for **Spot** and **Point** Lights.<br />This property only appears when you enable [more options](More-Options.html) for this section. |
| **Link Light Layer**       | Enable the checkbox to use the same [Light Layer](Light-Layers.html) for shadows and lighting. If you enable this feature, then HDRP uses the Light Layer from the **Light Layer** drop-down in the **General** section for shadowing. If you disable this feature, then HDRP uses the **Light Layer** drop-down in this section for shadowing.<br /> This property only appears if you enable [more options](More-Options.html) for this section.To access this property, enable **Light Layers** in your [HDRP Asset](HDRP-Asset.html). |
| **Light Layer**            | Use the drop-down to set the Light Layer HDRP uses for shadowing. This Light therefore only casts shadows for GameObjects that use a matching Light Layer. For more information about using Light Layers for shadowing, see [Shadow Light Layers](Light-Layers.html#ShadowLightLayers).<br /> This property only appears if you enable [more options](More-Options.html) for this section.To access this property, disable the **Link Light Layer** checkbox. |

##### Contact Shadows

This section is only available in Realtime or Mixed light **Mode**.

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Enable**   | Add [Contact Shadows](Override-Contact-Shadows.html) to this Light. Use the drop-down to select a quality mode for the Contact Shadows. Select **Custom** to expose a checkbox that allows you to enable or disable Contact Shadows at will. |

##### Baked Shadows

This section is only available in Baked light **Mode**.

| **Property**   | **Description**                                                                 |
| -------------- | ------------------------------------------------------------------------------- |
| **Enable**     | Enable the checkbox to let this Light cast shadows.                             |
| **Near Plane** | The distance, in meters, from the Light that GameObjects begin to cast shadows. |

##### High Filtering Quality properties

In your [HDRP Asset](HDRP-Asset.html), select **High** from the **Filtering Quality** drop-down to expose the following properties.

| **Property**                   | **Description**                                              |
| ------------------------------ | ------------------------------------------------------------ |
| **Shadow Softness**            | Defines the behavior of area light shadows. Higher softness values mimic a larger emission radius while lower values mimic a [punctual light](Glossary.html#PunctualLights). High values increase shadow blur depending on the distance between the pixel receiving the shadow and the shadow caster. |
| **Blocker Sample Count**       | The number of samples HDRP uses to evaluate the distance between the pixel receiving the shadow and the shadow caster. Higher values give better accuracy. |
| **Filter Sample Count**        | The number of samples HDRP uses to blur shadows. Higher values give smoother results. |
| **Minimum Size of the Filter** | The minimum size of the whole shadow’s blur effect, no matter the distance between the pixel and the shadow caster. Higher values give blurrier results. |
