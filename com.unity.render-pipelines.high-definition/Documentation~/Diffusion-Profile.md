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
| **Texturing Mode** | Use the drop-down to select when HDRP applies the albedo of the Material.<br />&#8226; **Post-Scatter**: HDRP applies the albedo to the Material after the subsurface scattering pass pass. This means that the contents of the albedo texture are not blurred. Use this mode for scanned data and photographs that already contain some blur due to subsurface scattering. <br />&#8226; **Pre- and Post-Scatter**: Albedo is partially applied twice, before and after the subsurface scattering pass pass. Effectively, this blurs the albedo, resulting in a softer, more natural look. |



## Transmission only

| Property| Description |
|:---|:---|
| **Transmission Mode** | Use the drop-down to select a method for calculating light transmission. <br />&#8226; **Thick Object**: is for geometrically thick objects. Note that since this mode makes use of shadow maps, directional lights automatically fall back to the thin object mode that relies solely on thickness maps (since shadow maps of directional lights do not offer sufficient precision for thickness estimation). <br />&#8226; **Thin Object**: is for thin, double-sided, geometry. |
| **Transmission Tint** | Specifies the tint of the translucent lighting (that is transmitted through objects). |
| **Min-Max Thickness (mm)** | Sets the range of thickness values (in millimeters) corresponding to the [0, 1] range of texel values stored in the Thickness Map. This range corresponds to the minimum and maximum values of the Thickness Remap (mm) slider below. |
| **Thickness Remap (mm)** | Sets the range of thickness values (in millimeters) corresponding to the [0, 1] range of texel values stored in the Thickness Map. This range is displayed by the Min-Max Thickness (mm) fields above. |



## Profile Previews

| Property| Description |
|:---|:---|
| **Profile Preview** | Displays the fraction of lights scattered from the source located in the center. The distance to the boundary of the image corresponds to the Max Radius. |
| **Transmission Preview** | Displays the fraction of light passing through the GameObject depending on the values from the Thickness Remap (mm).  |



## Working with different Transmission Modes

The main difference between the two __Transmission Modes__ is how they use shadows.
If you disable shadows on your Light, both __Transmission Modes__ give the same results, and derive their appearance from the __Thickness Map__ and the __Diffusion Profile__.
The results change if you enable shadows. The __Thin Object__ mode (that only evaluates shadowing once, at the front face) is likely to cause self-shadowing issues (for thick objects) that can cause the object to appear completely black. The __Thick Object__ mode derives the thickness from the shadow map, taking the largest value between the baked thickness and the shadow thickness, and uses this to evaluate the light transmittance.

Because you cannot control the distances HDRP derives from the shadow map, the best way to approach __Thick Object__ is to enable shadows, then adjust the __Scattering Distance__ until the overall transmission intensity is in the desired range, and then use the __Thickness Map__ to mask any shadow mapping artifacts.
