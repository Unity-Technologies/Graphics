# Diffusion Profile

The High Definition Render Pipeline (HDRP) stores most [subsurface scattering](Subsurface-Scattering.html) settings in a __Diffusion Profile__ Asset. You can assign a __Diffusion Profile__ Asset directly to Materials that use Subsurface Scattering.

To create a Diffusion Profile, navigate to __Assets > Create > Rendering > Diffusion Profile__. To use it, open your HDRP Asset and add it to the __Diffusion Profile List__ property.

| Property| Description |
|:---|:---|
| **Name** | The name of the Diffusion Profile. |
| **Scattering Distance** | Use the color picker to select the shape and blur radius of the Diffusion Profile. Defines how far light travels below the surface. This affects the color bleeding and blurring behavior of Subsurface Scattering, as well as the color tint of Transmission. |
| **Max Radius** | An informative helper value that displays the effective maximum radius (in millimeters) of the effect you define in Scattering Distance. You can not change this value directly. |
| **Index of Refraction** | Use the slider to set the refractive behavior of the Material. Larger values increase the intensity of specular reflection. For example, the index of refraction of skin is about 1.4. For more example values for the index of refraction of different materials, see Pixel and Poly’s [list of indexes of refraction values](https://pixelandpoly.com/ior.html). |
| **World Scale** | Controls the scale of Unity’s world units for this Diffusion Profile. By default, HDRP assumes that 1 Unity unit is 1 meter. This property only affects the subsurface scattering pass. |



## Subsurface Scattering only

| Property| Description |
|:---|:---|
| **Texturing Mode** | Use the drop-down to select when HDRP applies the albedo of the Material.<br />&#8226; **Post-Scatter**: HDRP applies the albedo to the Material after the subsurface scattering pass pass. This means that the contents of the texture are not blurred. Use this mode for scanned data and photographs that already contain some blur due to subsurface scattering. <br />&#8226; **Pre- and Post-Scatter**: HDRP effectively blurs the albedo. This results in a softer, more natural look. |



## Transmission only

| Property| Description |
|:---|:---|
| **Transmission Mode** | Use the drop-down to select a method for calculating light transmission. <br />&#8226; **Thick Object**: is for geometrically thick meshes.<br />&#8226; **Thin Object**: is for thin, double-sided, geometry. |
| **Transmission Tint** | Specifies a color to tint the translucent lighting. Unlike the Scattering Distance, its effect does not change depending on the distance below the surface. |
| **Min-Max Thickness (mm)** | Sets the range of the thickness of the Mesh. Displays the minimum and maximum values of the Thickness Remap (mm) slider property below. |
| **Thickness Remap (mm)** | Sets the range of the thickness. The Material’s Thickness Map modulates this value. |



## Profile Previews

| Property| Description |
|:---|:---|
| **Profile Preview** | Displays the fraction of lights scattered from the source located in the center. The distance to the boundary of the image corresponds to the Max Radius. |
| **Transmission Preview** | Displays the fraction of light passing through the GameObject depending on the values from the Thickness Remap (mm).  |



## Working with different Transmission Modes

The main difference between the two __Transmission Modes__ is how they use shadows.
If you disable shadows on your Light, both __Transmission Modes__ give the same results, and derive their appearance from the __Thickness Map__ and the __Diffusion Profile__.
The results change if you enable shadows. The __Thin Object__ mode is likely to cause self-shadowing, which can cause the object to appear completely black. The __Thick Object__ mode derives the thickness from the shadow map, taking the largest value between the baked thickness and the shadow thickness, and uses this to evaluate the light transmittance.

Because you cannot control the distances HDRP derives from the shadow map, the best way to approach __Thick Object__ is to enable shadows, then adjust the __Scattering Distance__ until the overall transmission intensity is in the desired range, and then use the __Thickness Map__ to mask any shadow mapping artifacts.



## Upgrading to the new diffusion profile system

For HDRP 5.5.0-preview and 6.3.0-preview or newer. 
Materials should smoothly upgrade themselves to reference the __Diffusion Profile__ Asset instead of the old index in the Diffusion Profile List. There are some exceptions:

- ShaderGraphs produce an error message saying that HDRP can not upgrade the __Diffusion Profile__. You must set the __Diffusion Profile__ slot / node value manually.
- Visual Effect Graphs also produce an error and you must set the __Diffusion Profile__ reference manually.
- You must update Materials serialized inside the Scene (not existing as an Asset) manually. Navigate to __Edit > Render Pipeline > Upgrade all Materials to newer version__. Note that you must load the Materials in the Scene to upgrade them.
