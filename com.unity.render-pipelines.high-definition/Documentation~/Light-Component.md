# Lights in the High Definition Render Pipeline

Use the Light component to create light sources in your [Scene](https://docs.unity3d.com/Manual/CreatingScenes.html). The Light component controls the shape, color, and intensity of the light. It also controls whether or not the Light casts shadows in your Scene, as well as more advanced settings.

![](Images/HDRPFeatures-LightTypes.png)

## Creating Lights

There are two ways to add Lights to your Scene. You can create a new Light [GameObject](https://docs.unity3d.com/Manual/class-GameObject.html), or you can add a Light component to an existing GameObject.

To create a new Light Gameobject:

1. In the Unity Editor, go to **GameObject > Light**
2. Select the Light type you want to add.

Unity creates a new GameObject and attaches a Light component, as well as two other High Definition Render Pipeline (HDRP) components: **HD Additional Light Data** and **HD Additional Shadow Data**. Unity places the new Light GameObject into your Scene, centered on your current view in the Scene window.

To add a Light component to an existing GameObject:

1. Select the GameObject you want to add a Light.
2.  In the Inspector, click **Add Component**.
3. Go to the **Rendering** section, and click **Light**. This creates a new Light source and attaches it to the currently selected GameObject. It also adds the **HD Additional Light Data** and **HD Additional Shadow Data** HDRP components.

Alternatively, you can search for "Light" in the **Add Component** window, then click **Light** to add the Light component.

## Configuring Lights

To configure the properties of a Light, select a GameObject in your Scene with a Light component attached. See the Light’s properties in the Inspector window.

HDRP includes multiple types of Light. Although HDRP Lights share many properties, each one has its own unique behavior and set of properties.

For more detailed information on how to configure realistic light fixtures, see the [Create High-Quality Light Fixtures in Unity](https://docs.unity3d.com/uploads/ExpertGuides/Create_High-Quality_Light_Fixtures_in_Unity.pdf) expert guide.

### Properties

The properties available for Lights are in separate drop-down sections. Each drop-down section contains some properties that all Lights share, and also properties that customize the behavior of the specific type of Light. These sections also contain [advanced properties](Advanced-Properties.html) that you can expose if you want to fine-tune your light's behavior. The drop-down sections are:

- [General](#GeneralProperties)
- [Shape](#ShapeProperties)
- [Emission](#EmissionProperties)
- [Volumetrics](#VolumetricProperties)
- [Shadows](#ShadowProperties)

<a name="GeneralProperties"></a>

### General

**General** properties control the type of Light, how HDRP processes this Light, and whether this Light affects everything in the Scene or just GameObjects on a specific Light Layer.

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Type**        | Defines the Light’s type. Lights of different Types behave differently, so when you change the **Type**, the properties change in the Inspector. |
| **Mode**        | Specify the [Light Mode](https://docs.unity3d.com/Manual/LightModes.html) that HDRP uses to determine how to bake a Light, if at all. Possible modes are:<br />&#8226; [Realtime](https://docs.unity3d.com/Manual/LightMode-Realtime.html) <br />&#8226; [Mixed](https://docs.unity3d.com/Manual/LightMode-Mixed.html) <br />&#8226; [Baked](https://docs.unity3d.com/Manual/LightMode-Baked.html) |
| **Light Layer** | A  mask that allows you to choose which Light Layers this Light affects. The affected Light only lights up Mesh Renderers with a matching **Rendering Layer Mask**.<br />This property only appears when you enable the [advanced properties](Advanced-Properties.html) for this section. |

#### Light Types guide

| **Type**        | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Spot**        | Emits light from a specified location and range over which the light diminishes. A Spot Light constrains the light it emits to an angle, which results in a cone-shaped region of illumination. The center of the cone points in the forward direction (z-axis) of the Light GameObject. Light also diminishes at the edges of the Spot Light’s cone. Increase the **Spot Angle** to increase the width of the cone. |
| **Directional** | Creates effects that are similar to sunlight in your Scene. Like sunlight, Directional Lights are distant light sources that HDRP treats as though they are infinitely far away. A Directional Light does not have any identifiable source position, and you can place the Light GameObject anywhere in the Scene. A **Directional Light** illuminates all GameObjects in the Scene as if the Light rays are parallel and always from the same direction. The Light disregards the distance between the Light itself and the target GameObject, so the Light does not diminish with distance. |
| **Point**       | Projects light out equally in all directions from a point in space. The direction of light hitting a surface is the line from the point of contact back to the center of the Light GameObject. The light intensity diminishes with increased distance from the Light, and it reaches zero at the distance specified in the **Range** field. Light intensity is inversely proportional to the square of the distance from the source. This is known as the [Inverse-square law](https://en.wikipedia.org/wiki/Inverse-square_law), and is similar to how light behaves in the real world. |
| **Rectangle**   | Projects light from a rectangle. Light shines in all directions uniformly from the surface of the rectangle. |
| **Tube**        | Emits light in all directions equally along a line in space. |

<a name="ShapeProperties"></a>

### **Shape**

These settings define the area this Light affects. Each Light **Type** has its own unique **Shape** properties.

#### **Spot Light**

| **Property**        | **Description**                                              |
| ------------------- | ------------------------------------------------------------ |
| **Shape**           | HDRP Spot Lights can use three shapes.<br />&#8226; **Cone** : Projects light from a single point at the GameObject’s position, out to a circular base, like a cone. Alter the radius of the circular base  by changing the **Spot Angle** and the **Range**.<br />&#8226; **Pyramid** : Projects light from a single point at the GameObject’s position onto a base that is a square with its side length equal to the diameter of the **Cone**.<br />&#8226; **Box** : Projects light evenly across a rectangular area defined by a horizontal and vertical size. |
| **Outer Angle**     | The angle in degrees at the base of a Spot Light’s cone. This property is only for Lights with a **Cone Shape**. |
| **Inner Angle (%)** | Determines where the attenuation between the inner cone and the outer cone starts. Higher values cause the light at the edges of the Spot Light to fade out. Lower values stop the light from fading at the edges. This property is only for Lights with a **Cone Shape**. |
| **Spot Angle**      | The angle in degrees at the base of a Spot Light’s cone.     |
| **Aspect Ratio**    | Adjusts the shape of a Pyramid Spot Light to create rectangular Spot Lights. Set this to 1 for a square projection. Values lower than 1 make the Light wider, from the point of origin. Values higher than 1 make the Light longer. This property is only for Lights with a **Pyramid Shape**. |
| **Emission Radius** | The radius of the light source.                              |
| **Max Smoothness**  | For **Cone** and **Pyramid**. Changes the specular highlight in order to mimic a spherical Light. This allows you to avoid very sharp specular highlights that do not match the shape of the source Light. |
| **Size X**          | For **Box**. Adjusts the horizontal size of the Box Light. No light shines outside of the dimensions you set. |
| **Size Y**          | For **Box**. Adjusts the vertical size of the Box Light. No light shines outside of the dimensions you set. |

#### Directional Light

| **Property**                | **Description**                                              |
| --------------------------- | ------------------------------------------------------------ |
| **Max Smoothness**          | Allows you to alter the specular highlight. This allows you to avoid very sharp specular highlights that do not match the shape of the Light source. |

#### Point Light

| **Property**        | **Description**                                              |
| ------------------- | ------------------------------------------------------------ |
| **Emission Radius** | Defines the radius of the light source.                      |
| **Max Smoothness**  | Allows you to alter the specular highlight. This allows you to avoid very sharp specular highlights that do not match the shape of the Light source. Acts as a less resource-intensive method for faking spherical lighting. |

#### Rectangle Light

| **Property** | **Description**                                     |
| ------------ | --------------------------------------------------- |
| **Size X**   | Defines the horizontal size of the Rectangle Light. |
| **Size Y**   | Defines the vertical size of the Rectangle Light.   |

#### Tube Light

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Length**   | Defines the length of the Tube Light. The center of the Light is the Transform Position and the Light itself extends out from the center symmetrically. The **Length** is the distance from one end of the tube to the other. |

<a name="EmissionProperties"></a>

### **Emission**

These settings define the emissive behavior of your Light. You can set the Light’s color, strength, and maximum range. If you do not see these properties in the Light Inspector, make sure you expose the [advanced properties](#AdvancedProperties). Most Lights share **Emission** properties. Below are the list of properties that more than one Light **Type** share, followed by unique properties only available for a single Light **Type**.

#### Shared Properties

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Color Temperature**     | Enable the checkbox to set the color temperature mode for this Light. Color Temperature mode adjusts the color of your Light based on a red-to-blue kelvin temperature scale. When enabled, this hides the **Color** property and exposes **Filter** and **Temperature**. Disable this checkbox to only display the **Color** field in the Inspector and use that as the Light color, without the temperature. |
| **- Filter**              | Allows you to select the color of the Light’s filter using the colour picker. HDRP uses this and the **Temperature** property to calculate the final color of the Light. |
| **- Temperature**         | Allows you to select a temperature that HDRP uses to calculate a color on a red-to-blue kelvin temperature scale. You can move the slider along the scale itself, or specify an exact temperature value in the field to the right of the slider scale. |
| **Color**                 | Allows you to select the color of the Light using the colour picker. |
| **Intensity**             | The strength of the Light. Intensity is expressed in the following units: <br />&#8226; A Spot Light can use [Lumen](Physical-Light-Units.html#Lumen), [Candela](Physical-Light-Units.html#Candela), [Lux](Physical-Light-Units.html#Lux), and [EV<sub>100</sub>](Physical-Light-Units.html#EV).<br />&#8226; A Directional Light can only use **Lumen**, **Candela**, **Lux**, and **EV<sub>100</sub>**.<br />&#8226; A Point Light can use **Lumen** and **Candela**.<br />&#8226; A Rectangle Light can use **Lumen**, [Luminance](Physical-Light-Units.html#Luminance), and **EV<sub>100</sub>**.<br />&#8226; A Tube Light can use **Lumen**, **Luminance**, and **EV<sub>100</sub>**.<br /><br />Generally, the further the light travels from its source, the weaker it gets. The only exception to this is the **Directional Light** which has the same intensity regardless of distance. For the rest of the Light types, lower values cause light to diminish closer to the source. Higher values cause light to diminish further away from the source. |
| **Range**                 | The range of influence for this Light. Defines how far the emitted light reaches. This property is available for all **Light Types** except **Directional**. |
| **Indirect Multiplier**   | The intensity of [indirect](https://docs.unity3d.com/Manual/LightModes-TechnicalInformation.html) light in your Scene. A value of 1 mimics realistic light behavior. A value of 0 disables indirect lighting for this Light. If both **Realtime** and **Baked** Global Illumination are disabled in Lighting Settings (menu: **Window > Rendering > Lighting Settings**), the Indirect Multiplier has no effect. |
| **Cookie**                | An RGB Texture that the Light projects. For example, to create silhouettes or patterned illumination for the Light. Texture shapes should be 2D for Spot and Directional Lights and Cube for Point Lights. Always import **Cookie** textures as the default texture type. This property is available for **Spot**, **Directional**, and **Point** Lights. |
| **Affect Diffuse**        | Enable the checkbox to apply [diffuse](<https://docs.unity3d.com/Manual/shader-NormalDiffuse.html>) lighting to this Light.<br />This property only appears when you enable the [advanced properties](Advanced-Properties.html) for this section. |
| **Affect Specular**       | Enable the checkbox to apply [specular](https://docs.unity3d.com/Manual/shader-NormalSpecular.html) lighting to this Light.<br />This property only appears when you enable the [advanced properties](Advanced-Properties.html) for this section. |
| **Range Attenuation**     | Enable the checkbox to make this Light shine uniformly across its range. This stops light from fading around the edges. This setting is useful when the range limit is not visible on screen, and you do not want the edges of your light to fade out. This property is available for all **Light Types** except **Directional**.<br />This property only appears when you enable the [advanced properties](Advanced-Properties.html) for this section. |
| **Fade Distance**         | The distance between the Light source and the Camera at which the Light begins to fade out. Measured in meters. This property is available for all **Light Types** except **Directional**.<br />This property only appears when you enable the [advanced properties](Advanced-Properties.html) for this section. |
| **Dimmer**                | Dims the Light. Does not affect the intensity of the light. You can also modify this property via [Timeline](https://docs.unity3d.com/Manual/TimelineSection.html), Scripting or [animation](https://docs.unity3d.com/Manual/animeditor-AnimatingAGameObject.html). The parameter lets you fade the Light in and out without having to store its original intensity.<br />This property only appears when you enable the [advanced properties](Advanced-Properties.html) for this section. |
| **Display Emissive Mesh** | Enable the checkbox to make Unity automatically generate a Mesh with an emissive Material using the size, colour, and intensity of this Light. Unity automatically adds the Mesh and Material to the GameObject the Light component is attached to. This property is available for **Rectangle** and **Tube** Lights.<br />This property only appears when you enable the [advanced properties](Advanced-Properties.html) for this section. |

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

These settings define the volumetric behavior of this Light. Alter these settings to change how this Light behaves with [Atmospheric Scattering](Atmospheric-Scattering.html). All Light **Types** share the same **Volumetric** properties.

#### Shared Properties

| **Property**      | **Description**                                              |
| ----------------- | ------------------------------------------------------------ |
| **Enable**        | Enable the checkbox to simulate light scattering through volumetric fog. Enabling this property allows you to edit the **Dimmer** and **Shadow Dimmer** properties. |
| **Dimmer**        | Dims the volumetric lighting effect of this Light.           |
| **Shadow Dimmer** | Dims the volumetric fog effect of this Light. Set this property to 0 to make the volumetric scattering compute faster. |

<a name="ShadowProperties"></a>

### **Shadows**

Use the Shadows section to adjust the Shadows cast by this Light. HDRP currently does not support shadowing **Tube** Lights. Because of this, Unity does not expose the **Shadows** drop-down section in the Inspector when you select either of this **Type**. The Light **Types** that HDRP does support shadowing for (**Spot**, **Directional**, and **Point**) share almost all of their properties.

Unity exposes extra properties in the **Shadows** section depending on the **Mode** you set in the [General](#GeneralProperties) section. It also exposes extra properties depending on the **Filtering Quality** set in your Unity Project’s [HDRP Asset](HDRP-Asset.html). To change the **Filtering Quality** property, navigate to your Project’s **HDRP Asset > Shadows** and use the **Filtering Quality** drop-down  to select the shadow filtering mode. Setting **Filtering Quality** to **High** or **Very High** exposes extra properties in the Light Inspector’s **Shadow** drop-down section.

&#8226; For more information on shadow filtering in HDRP, see the documentation on [Shadow Filtering](Shadows-in-HDRP.html#ShadowFiltering).

&#8226; For a list of the the available filter quality presets in HDRP, see the [Filtering Qualities table](HDRP-Asset.html#FilteringQualities).

#### Properties

##### Shadow Map

| **Property**                | **Description**                                              |
| --------------------------- | ------------------------------------------------------------ |
| **Enable**                  | Enable the checkbox to add shadows to this Light.            |
| **Update Mode**             | Use the drop-down to select the mode that HDRP uses to determine when to update a shadow map.<br />For information on the modes available, see the [Shadows in HDRP documentation](Shadows-in-HDRP.html#ShadowUpdateMode). |
| **Resolution**              | The resolution of this Light’s shadow maps. Measured in pixels. A higher resolution increases the fidelity of shadows at the cost of GPU performance and memory usage, so if you experience any performance issues, try using a lower value. |
| **Near Plane**              | The distance, in meters, from the Light that GameObjects begin to cast shadows. |
| **Shadowmask Mode**         | Defines how the shadowmask behaves for this Light. For detailed information on each **Shadowmask Mode**, see the documentation on [Shadowmasks](Shadows-in-HDRP.html#ShadowmaskModes). This property is only visible if you tet the **Mode**, under [General](#GeneralProperties), to **Mixed**. |
| **View Bias Scale**         | Defines how much the [View Bias](https://docs.unity3d.com/Manual/ShadowOverview.html#LightBias) scales with distance for this Light. Surfaces directly illuminated by a Light can sometimes appear to be partly in shadow and parts of the surface might be incorrectly illuminated due to low-resolution shadow maps or shadow filtering. If the shadows that this Light casts appear incorrectly, use the slider to adjust this value until they are correct. |
| **View Bias**               | The minimum [View Bias](https://docs.unity3d.com/Manual/ShadowOverview.html#LightBias) for this Light. For more information about View Bias in HDRP, see documentation on [Shadows](https://github.com/Unity-Technologies/ScriptableRenderPipeline/wiki/Shadows).<br />This property only appears when you enable the [advanced properties](Advanced-Properties.html) for this section. |
| **Normal Bias**             | Controls the amount of normal [bias](https://docs.unity3d.com/Manual/ShadowOverview.html#LightBias) this Light applies along the [normal](https://docs.unity3d.com/Manual/AnatomyofaMesh.html) of the illuminated surface.<br />This property only appears when you enable the [advanced properties](Advanced-Properties.html) for this section. |
| **Edge Leak Fixup**         | Enable the checkbox to prevent light leaking at the edge of shadows this Light casts.<br />This property only appears when you enable the [advanced properties](Advanced-Properties.html) for this section. |
| **- Edge Tolerance Normal** | Enable the checkbox to use the edge leak fix in normal mode. Uncheck this box to use the edge leak fix in view mode.<br />This property only appears when you enable the [advanced properties](Advanced-Properties.html) for this section. |
| **- Edge Tolerance**        | The threshold, between 0 and 1, which determines whether to apply the edge leak fix.<br />This property only appears when you enable the [advanced properties](Advanced-Properties.html) for this section. |
| **Dimmer**                  | Dims the shadows this Light casts so they become more faded and transparent.<br />This property only appears when you enable the [advanced properties](Advanced-Properties.html) for this section. |
| **Tint**                    | Tint the shadows this Light casts so they become colored and transparent.<br />This property only appears when you enable the [advanced properties](Advanced-Properties.html) for this section. |
| **Fade Distance**           | The distance, in meters, between the Camera and the Light at which shadows fade out. This property is available for **Spot** and **Point** Lights.<br />This property only appears when you enable the [advanced properties](Advanced-Properties.html) for this section. |

##### High Filtering Quality properties

In your [HDRP Asset](HDRP-Asset.html), select **High** from the **Filtering Quality** drop-down to expose the following properties.

| **Property**                   | **Description**                                              |
| ------------------------------ | ------------------------------------------------------------ |
| **Shadow Softness**            | Defines the behavior of area light shadows. Higher softness values mimic a larger emission radius while lower values [punctual light](Glossary.html#PunctualLights) shadows. High values increase shadow blur depending on the distance between the pixel receiving the shadow and the shadow caster. |
| **Blocker Sample Count**       | The number of samples HDRP uses to evaluate the distance between the pixel receiving the shadow and the shadow caster. Higher values give better accuracy. |
| **Filter Sample Count**        | The number of samples HDRP uses to blur shadows. Higher values give smoother results. |
| **Minimal size of the filter** | The minimum size of the whole shadow’s blur effect, no matter the distance between the pixel and the shadow caster. Higher values give blurrier results. |

##### Very High Filtering Quality properties

In your [HDRP Asset](HDRP-Asset.html), select **Very High** from the **Filtering Quality** drop-down to expose the following properties. These properties only apply to Directional Lights. Spot and Point Lights use **High** when you select the **Very High** option from the **Filtering Quality** drop-down. 

| **Property**       | **Description**                                              |
| ------------------ | ------------------------------------------------------------ |
| **Kernel size**    | The size of the kernel that HDRP uses to process filtering. Larger values make shadows appear softer. |
| **Light Angle**    | Represents the radius of the sun in the sky. It controls the acceleration of the shadow softness. |
| **Max Depth Bias** | The depth bias value for the maximum  region size that the kernel can use. |

##### Contact Shadows

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Enable**   | Enable the checkbox to add [Contact Shadows](Override-Contact-Shadows.html) to this Light. |

##### Baked Shadows

Set the **Mode**, under [General,](#GeneralProperties) to **Mixed** or **Baked** to expose the following properties.

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Radius**   | The emission radius of the light for calculating baked shadows (shadowmask or fully baked light). Higher values simulate a bigger light source and result in softer shadows. Small values simulate a small light source and result in sharper shadows. |
