
# Capabilities of the water system
The water system simulates the behavior of water surfaces. It is adjustable and customizable.
A scene can support multiple water surfaces simultaneously.

## Water body types
HDRP provides for three water surface types: **Pool**, **River**, and **Ocean, Sea, or Lake**. The last two of these include multi-band simulation properties to provide larger and more complex wave patterns.

## Underwater rendering
It is possible to view water surfaces from below if you enable the **Underwater** option.

## Wind and current
Wind and swell or ripple properties combine with **Current** values to determine the motion of the [water simulation](water-water-system-simulation.md).
For more information about customizing current, see [Create a current in the water system](water-create-a-current-in-the-water-system) section.

## Simulation foam
**River** and **Ocean, Sea, or Lake** surfaces support surface simulation foam. You can adjust the amount of foam, its smoothness, mask it and dim it depending on the **Distant Wind Speed** using the **Wind Speed Dimmer**.

##  Caustics
You can adjust caustic resolution for all surface types, and specify which **Simulation Band** Unity should use to generate the caustic texture for  **River** and **Ocean, Sea, or Lake** surface types.

## Material properties
It is possible to adjust the **Smoothness**, **Refraction**, and light **Scattering** qualities of a water surface.

## ShaderGraph
For more in-depth water Material customizations, you can use [the water ShaderGraph](master-stack-water.md).

## Masking
You can assign custom masks to attenuate or supress ripples, swells, and foam on specific portions of a water surface. You can also use them to adjust decal and light layers.
## Scripting
It is possible to create [scripts that interact with the water system](water-scripting-in-the-water-system.md), to imitate buoyancy, for example.

## Limitations and caveats
The water system does not currently support:
* Breaking waves on shorelines
* Views from the side (as in an aquarium)
* Spherical or vertical body of water (like planets or water flowing in a ring planet)
* Selection on the scene view (selection of the water surface must be done through the hierarchy tab) 

Foam and caustics are monochrome.
### Feature compatibility
The water system is compatible with most HDRP and Unity features, with some specific exceptions.
#### Lighting
You cannot bake lighting for a water surface.
#### Dynamic resolution and antialiasing
HDRP's water implementation does not use motion vectors. This means that techniques like [Deep learning super sampling](deep-learning-super-sampling-in-hdrp.md), and [temporal antialiasing](Anti-Aliasing.md) produce blurry results with a lot of ghosting if you use them with water. [Multisample anti-aliasing](Anti-Aliasing.md#MSAA) is entirely incompatible with water, however.


#### Raytracing caveats
Although water surfaces can receive [Ray-Traced Reflections](Ray-Traced-Reflections.md), they cannot contribute to them. This means if you hold up a mirror to a water surface, the water does not reflect in the mirror, for example.

#### Reflection and refraction
HDRP treats Water surfaces as refractive transparent objects. This means water has the same behavior and limitations as refractive transparent GameObjects in the Default render queue. This has the following results:
- Water surfaces don't appear correctly behind another refractive transparent object.
- If you enable **Transparent Depth Prepass** on a transparent GameObject, then any water surface behind the transparent GameObject disappears.
- [Screen Space Reflection](Override-Screen-Space-Reflection.md) does not fall back to [Reflection Probes](Reflection-in-HDRP.md) underwater.

#### Decals
HDRP provides limited decal support for water surfaces. Global opacity controls the strength of the decal influence. Also, certain [Decal Shader](decal-material-inspector-reference.md) Surface Options do not work with water surfaces:
* **Affect Metal**
* **Affect Ambient Occlusion**
* **Affect Emission**

Also, **Affect Base Color** only produces monochromatic output.


# Additional resources
* [Settings and properties related to the water system](settings-and-properties-related-to-the-water-system.md)
* [Scripting in the water system](water-scripting-in-the-water-system.md)
