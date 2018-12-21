# HDRP Light component

Use the Light component to create light sources in your [Scene](https://docs.unity3d.com/Manual/CreatingScenes.html). The Light component controls the shape, color, and intensity of the light. It also controls whether or not the Light casts shadows in your Scene, as well as more advanced settings. 

## Creating Lights

There are two ways to add Lights to your Scene. You can create a new Light [GameObject](https://docs.unity3d.com/Manual/class-GameObject.html), or you can add a Light component to an existing GameObject. 

To create a new Light Gameobject: 

1. Navigate to __GameObject &gt; Light__, and then select the Light type you want to add. 

Unity creates a new GameObject and attaches a Light component, as well as two other HDRP specific components, __HD Additional Light Data__ and __Additional Shadow Data__. Unity places the new Light GameObject into your Scene, centered on your current view in the Scene window. 

To add a Light component to an existing GameObject:

1. Select the GameObject you want to add a Light to, then, in the Inspector, click __Add Component__.
2. Navigate to __Rendering__, and click __Light__. This creates a new Light source and attaches it to the currently selected GameObject. It also adds the __HD Additional Light Data__ and __Additional Shadow Data__ HDRP specific components.

Alternatively, you can:

1. Search for “Light” in the __Add Component__ window, then click __Light__ to add the Light component. 

## Configuring Lights

To configure the properties of a Light, select a GameObject in your Scene with a Light component attached. In the Inspector window, you will see the Light’s options.

Different types of Lights have different options for you to alter. More options are available if you enable the __Enable Shadows__ or the __Show Additional Settings__ checkboxes.

![](Images/LightComponent1.png)

The Light Inspector window for a Directional Light with __Enable Shadows__ and __Show Additional Settings__ selected. 

<a name=”Features”></a>

### Features :

Use the __Features__ section to enable or disable shadows, or access additional settings for this Light. 

- __Shadows__ : Check this box to make this Light cast Shadows in your Scene. When you check this box, the Shadows section appears in the Light component Inspector window. There, you can configure the Shadows cast by this Light.
- __Show Additional Settings__ : Check this box to display additional settings in the Light component Inspector window. The default settings are suitable for most use-cases, but you can use the additional settings to fine-tune your Light component for a specific look or Light behavior.

### Shape : 

Use the Shape section to change the shape of your Light. 

__Type__: Use this dropdown to select the type of Light.

* __Spot__ : A Spot Light has a specified location and range over which the light diminishes. The light it emits is constrained to an angle, which results in a cone-shaped region of illumination. The center of the cone points in the forward (z-axis) direction of the Light GameObject. Light also diminishes at the edges of the Spot Light’s cone. Increase the __Spot Angle__ to increase the width of the cone. A Spot Light has the following extra Shape properties:

* __Range__ : Controls the maximum distance the light can reach from its origin. 

* __Shape__ :

  * __Cone__ : Projects light from a single point at the GameObject’s position, out to a circular base, like a cone. Alter the radius of the circle by changing the __Spot Angle__ and the __Range__ . A Spot Light using a Cone shape has the following Shape properties:

  	* __Spot Angle__ : The angle in degrees at the base of a Spot Light’s cone.
  	* __Inner Percent__ : Determines where the attenuation between the inner cone and the outer cone starts. Higher values cause the light at the edges of the Spot to fade out. Lower values stop the light from fading at the edges. 
  	* __Max smoothness__ : Change the specular highlight in order to mimic a spherical Light. This allows you to avoid very sharp specular highlights that do not match the shape of the source Light.

  ![](Images/LightComponent2.png)

  * __Pyramid__ : Projects light from the a single point at the GameObject’s position onto a base that is a square with its side length equal to the diameter of the __Cone__. 

    ![](Images/LightComponent3.png) 
    
    A Spot Light using a __Pyramid__ shape has the following Shape properties:
    * __Spot Angle__ : The angle in degrees at the base of a Spot Light’s cone.
    * __Aspect ratio__ : Adjusts the shape of a Pyramid Spot Light to create rectangular Spot Lights. The Light will be square if Aspect ratio is set to 1. Values lower than 1 make the Light wider, from the point of origin. Values higher than 1 make the Light longer.

    * __Max smoothness__ : Change the specular highlight in order to mimic a rectangle Light. This allows you to avoid very sharp specular highlights that do not match the shape of the Light source.

  * __Box__ : Projects light evenly across a rectangular area defined by a horizontal and vertical size. A Spot Light using a __Box__ shape has the following Shape properties:

  	* __Size X__ : Adjust the horizontal size of the Box Light. No light shines outside of the dimensions you set.

  	* __Size Y__ : Adjust the vertical size of the Box Light. No light shines outside of the dimensions you set.

  	* __Max smoothness__ : Change the specular highlight in order to mimic a rectangle Light. This allows you to avoid very sharp specular highlights that do not match the shape of the Light source.

* __Directional Light__ : Create effects that are similar to sunlight in your Scene. Like sunlight, Directional Lights are distant light sources that light models treat as though they are infinitely far away. A Directional Light does not have any identifiable source position, and you can place the Light GameObject anywhere in the Scene. A __Directional Light__ illuminates all GameObjects in the Scene as if the Light rays are parallel and always from the same direction. The Light disregards the distance between the Light itself and the target GameObject, so the Light does not diminish with distance.

* __Point Light__ : Projects light out equally in all directions from at a point in space. The direction of light hitting a surface is the line from the point of contact back to the center of the Light GameObject. The light intensity diminishes with increased distance from the Light, and it reaches zero at the range specified in the __Range__ field. Light intensity is inversely proportional to the square of the distance from the source. This is known as the [Inverse-square law](https://en.wikipedia.org/wiki/Inverse-square_law), and is similar to how light behaves in the real world. A Point Light has the following Shape properties:

	* __Emission Radius__ : Controls the maximum distance the light can reach from its origin. 
	* __Max Smoothness__ : Change the specular highlight in order to mimic a Rectangle Light. This allows you to avoid very sharp specular highlights that do not match the shape of the Light source.

* __Rectangle__ : Projects light defined by a rectangle in space. From the surface of the rectangle, Light shines in all directions uniformly. A Rectangle Light has the following Shape properties:

	* __Range__ : Control the maximum distance the light can reach from its origin. 
	* __Size X__ : Adjust the horizontal size of the Rectangle Light.
	* __Size Y__ : Adjust the vertical size of the Rectangle Light.
	* __Max smoothness__ : Change the specular highlight in order to mimic a rectangle light. This allows you to avoid very sharp specular highlights that do not match the shape of the Light source.
  - 

* __Line__ : A Line Light is defined by a line in space. Light shines in all directions equally along the line. A Line Light has the following Shape properties:

	* __Range__ : Control the maximum distance the light can reach from its origin. 
	* __Length__ : Adjust the length of the Line Light. The Line Light emits light from its full length.

### Light: 

Use the __Light__ section to adjust the way your Light component shines light into your Scene.

* __Use color temperature mode__ : Check this box to enable color temperature mode for this Light. Color temperature mode adjusts the color of your Light based on a red-to-blue kelvin temperature scale. This color is then multiplied by the color specified in the __Color__ field. Uncheck this box to only display the __Color__ field in the Inspector and use it for the Light color, without the temperature.

* __Color__ : Select the color of the Light using the colour picker, or entering RGB or HSV values.

* __Intensity__ : Change the intensity of the Light. Intensity is expressed in the units specified below. The further the light travels from its source, the weaker it gets. Lower values cause light to diminish closer to the source. Higher values cause light to diminish further away from the source. 

	* A Spot Light can use [Lumen](Glossary.html#Lumen) and [Candela](Glossary.html#Candela)
	* A Directional Light can only use [Lux](Glossary.html#Lux)
	* A Point Light can use __Lumen__ and __Candela__
	* A Rectangle Light can use __Lumen__, [Luminance](Glossary.html#Luminance), and [Ev 100](Glossary.html#EV)
	* A Line Light can use __Lumen__, __Luminance__, and __Ev 100__

* __Indirect Multiplier__ : Change the intensity of [indirect](https://docs.unity3d.com/Manual/LightModes-TechnicalInformation.html) light in your Scene. A value of 1 mimics realistic light behavior. A value of 0 disables indirect lighting for this Light. If both __Realtime__ and __Baked__ Global Illumination are disabled in Lighting Settings (menu: __Window &gt; Rendering &gt; Lighting Settings__), the Indirect Multiplier has no effect. 

* __Mode__ : Specify the [Light Mode](https://docs.unity3d.com/Manual/LightModes.html) that is used to determine how a Light is baked, if at all. Possible modes are Realtime, Mixed and Baked. For more detailed information, see documentation on [Realtime Lighting](https://docs.unity3d.com/Manual/LightMode-Realtime.html), [Mixed Lighting](https://docs.unity3d.com/Manual/LightMode-Mixed.html), and [Baked Lighting](https://docs.unity3d.com/Manual/LightMode-Baked.html). .

* __Cookie__ : Specify a RGB Texture that the Light projects. For example, to create silhouettes or patterned illumination for the Light. Texture shapes should be 2D for Spot and Directional Lights and Cube for Point Lights. Always import __Cookie__ textures as the default texture type. 

	* __Size X__ (Directional Light only) :  Adjust the horizontal size of the projected cookie texture in pixels.
	* __Size Y__ (Directional Light only) : Define the vertical size of the projected cookie texture in pixels.

* __Additional settings__ :

	* __Light Layer__ : Choose which rendering Layer this Light will affect. This Light will only light up Mesh Renderers with a matching rendering Layer.
	* __Affect Diffuse__ : Check this box to enable [diffuse](https://docs.unity3d.com/Manual/shader-NormalDiffuse.html) lighting for this Light. 
	* __Affect Specular__ : Check this box to enable [specular](https://docs.unity3d.com/Manual/shader-NormalSpecular.html) lighting for this Light. 
	* __Fade Distance__ : Set the distance between the Light source and the camera at which the Light begins to fade out. Measured in Unity units.
	* __Dimmer__ : Dim the Light. Does not affect the intensity of the light. You can also modify the dimming parameter via Timeline, scripting or [animation](https://docs.unity3d.com/Manual/animeditor-AnimatingAGameObject.html). The parameter lets you fade the light in and out without having to store its original intensity.
	* __Volumetric Dimmer__ : Multiplies the contribution of this Light to volumetric lighting.
	* __Apply range attenuation__ (not available on Directional Lights) : Uncheck this box to make this Light shine uniformly across its range. This stops light from fading around the edges. This setting is useful when the range limit is not visible on screen, and you do not want the edges of your light to fade out. 
	* __Display Emissive Mesh__ : Check this box to make Unity automatically generate an emissive mesh using the size, colour, and intensity of this Light. The mesh is automatically added to the GameObject the Light component is attached to. 

### Shadows :

Use the Shadows section to adjust the Shadows cast by this Light. This section only shows when __Enable Shadows__ is checked in the [Features](#Features) section.

* __Resolution__ : Set the resolution of the shadow maps used by this Light. Measured in pixels. A higher resolution increases the fidelity of shadows at the cost of GPU performance and memory usage, so if you experience any performance issues, try using a lower value.
* __View Bias Scale__ : Adjust how much the [View Bias](https://docs.unity3d.com/Manual/ShadowOverview.html#LightBias) scales with distance for this Light. Surfaces directly illuminated by a Light can sometimes appear to be partly in shadow. Or parts of the surface can be incorrectly illuminated due to low-resolution Shadow maps or Shadow Filtering. If the Shadows cast by this Light appear incorrectly, use the slider to adjust this value until they are correct.
* __Near Plane__ : Set the distance (in Unity units) from the Light where objects start casting shadows.
* __Baked Shadow Radius__ (Mixed and Baked Lights only) : The Lightmapper considers this Light to be a sphere when you use values higher than 0. The Lightmapper uses this value as the radius of the sphere. Higher values result in shadows that soften with distance from the shadow caster.
* __Non Lightmapped Only__ (Mixed Lights only) : Check this box to force this Light to use the baked shadow mask for static GameObjects, and only render dynamic shadows for non-static GameObjects. 

* __Additional Settings__ : 

	* __Fade Distance__ : Set the distance, in Unity units, between the camera and the Light where shadows fade out.

	* __Dimmer__ : Dim the shadows cast by this Light so they become more faded. You can also modify this parameter through Timeline, scripting or animation. 

	* __View Bias__ : Set the minimum [View Bias](https://docs.unity3d.com/Manual/ShadowOverview.html#LightBias) for this Light. For more information about View Bias in HDRP, see documentation on [Shadows](https://github.com/Unity-Technologies/ScriptableRenderPipeline/wiki/Shadows). 

	* __Normal Bias__ : Control the amount of normal [bias](https://docs.unity3d.com/Manual/ShadowOverview.html#LightBias) applied along the [normal](https://docs.unity3d.com/Manual/AnatomyofaMesh.html) of the illuminated surface. 

	* __Edge leak fixup__ : Check this box to prevent light leaking at the edge of shadows cast by this Light.

		* __Edge Tolerance normal__ : Check this box to use the edge leak fix in normal mode. Uncheck this box to use the edge leak fix in view mode.
		* __Edge Tolerance__ : Set the threshold between 0 and 1 to determine whether to apply the edge leak fixup.