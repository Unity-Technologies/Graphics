# Lit Shader

The Lit Shader lets you easily create realistic materials with minimal configuration. It includes options for effects including subsurface scattering, iridescence, vertex or pixel displacement, and decal compatibility. For more information about Materials, Shaders and Textures, see the [Unity User Manual](https://docs.unity3d.com/Manual/Shaders.html).

When you create new Materials in HDRP, they use the Lit Shader by default.

## Creating a Lit Material

To create a new Lit Shader Material, navigate to your Project's Asset window, right-click the Asset Window and select __Create > Material__. This adds a new Material to your Unity Project’s Asset folder.

## Lit Shader Parameters

### Surface options

Surface options control the overall look of your Material's surface and how Unity renders the Material on screen. 

| Property| Description |
|:---|:---|
| **Surface type** | Controls whether your Shader supports transparency or not. HDRP exposes more properties depending on the Surface Type you select. See the [Surface Type](Surface-Type.html) documentation for more information. |
| **Alpha Cutoff Enable** | Controls whether your Material acts like a Cutout Shader or not. Enabling this feature exposes more properties. See the [Alpha Clipping](Alpha-Clipping.html) documentation for more information. |
| **Double Sided** | Controls whether HDRP renders both faces of the polygons in your geometry, or just the side defined by the normal. See the [Double Sided](Double-Sided.html) documentation for more information. |
| **Material Type** | Allows you to give your Material a type, which allows you to customize it with different settings depending on the Material Type you select. See the [Material Type](Material-Type.html) documentation for more information. |
| **Enable Decal** | Enable to allow this Material to receive decals that the Decal Project Component casts into the Scene. |
| **Enable geometric specular AA** | Tick this checkbox to tell HDRP to perform geometric anti-aliasing on this Material. This removes specular artifacts that occur on high density Meshes with a high smoothness. |
| **- Offset multiplier** | Modules the geometric specular anti-aliasing effect between 0 and 1. |
| **- Offset threshold** | Clamps the maximum of the offset effect. |
| **Receives SSR** | Enable to allow this Material to receive screen space reflection. |
| **Displacement Mode** | Controls the method HDRP uses to displace the Material. |



### Vertex Animation

| Property| Description |
|:---|:---|
| **Enable Motion Vector For Vertex Animation** | Removes ghosting coming from vertex animation.  |



### Inputs

| Property| Description |
|:---|:---|
| **Base Color +** | Controls both the color and opacity of your Material. To assign a texture to this field, click the radio button and select your texture in the Select Texture window. To change the color of your Material, click the box to the right of the Base Color + option and use the color picker to select the color you want. You can also enter RGB values, or use the color picker tool to select a color from anywhere on your screen. The alpha value of the Base color controls the transparency level for the Material. This only has an effect if the Surface Type for the Material is set to Transparent, and not Opaque. |
| **Metallic** | Use this slider to adjust how "metal-like" the surface of your Material is (between 0 and 1). When a surface is more metallic, it reflects the environment more and its albedo color becomes less visible. At full metallic level, the surface color is entirely driven by reflections from the environment. When a surface is less metallic, its albedo color is clearer and any surface reflections are visible on top of the surface color, rather than obscuring it. This property is only available if you set your Material Type to Standard.  |
| **Smoothness** | Use this slider to adjust the smoothness of your Material. All light rays hitting a smooth surface bounce off at predictable and consistent angles. A perfectly smooth surface (smoothness value 1) reflects light like a mirror. Less smooth surfaces reflect light over a wider range of angles (as the light hits the bumps in the microsurface), and therefore the reflections have less detail and are spread across the surface in a more diffused pattern. |
| **Mask map** | Defines a map that packs different Material maps into each of its RGBA channels. <br/>Red channel: Metallic mask. 0 = not metallic, 1 = metallic. <br/>Green channel: Ambient occlusion. <br/>Blue channel: Detail map mask. <br/>Alpha channel: Smoothness. |
| **Normal Map space** | Sets the type of Normal Map space, which can be either. **TangentSpace** normal maps, which must be a normal map texture type (BC7/BC5/DXT5nm), or **ObjectSpace** normal maps, which must be a texture default of default type (RGB).  |
| **Normal Map/Normal Map OS** | Assigns the normal map for this Material. If Normal Map space is set to TangentSpace, use the handle to modulate the normal intensity between 0 and 8. |
| **Bent normal map/Bent normal map OS** | Assigns the bent normal map for this Material. HDRP uses bent normal maps to simulate more accurate ambient occlusion. Note: This only works with diffuse lighting. |
| **Coat Mask** | Assigns the coat mask for this Material. HDRP uses this mask to simulate a clear coat effect on the Material. The Coat Mask value is 0 by default, but you can use the handle to modulate the clear Coat Mask effect using a value between 0 and 1. Use the Coat Mask to mimic Materials like car paint or plastics.  |
| **Base UV mapping** | Sets the type of base UV mapping, which can be UV0, UV1 (used by the lightmap), UV2, UV3, planar or triplanar. Planar and triplanar use a world scale. This ratio depends on the size of the textures and the texel ratio wanted. By default it is 1, which means the Material is applied on 1 meter. A value of 0.5 applies the material on 2 meters. |
| **Tiling** | Sets the X/Y values to tile the Material. |
| **Offset** | Sets on X/Y offset for the UV. |



### __Detail Inputs__

| Property| Description |
|:---|:---|
| **Detail Map** | Select the type of composited map that HDRP uses to add micro details into the Material. The blue channel of the Mask Map manages the visibility of the Detail Map. <br/>The Detail Map uses the following channel settings: <br/>**A(R)**:  Red channel stores the grey scale as albedo. <br/>**Ny(G)**: Green channel stores the detail normal map.<br/>**S(B)**: Blue channel stores the detail smoothness.<br/>**Nx(A)**: Alpha channel stores the red channel of the detail normal map.<br/><br/>HDRP organises channels like this because each channel uses a different compression qualities. |
| **Detail UV mapping** | Select the type of UV map to use, which can be UV0, UV1, UV2 or UV3. If the Material’s UV Set property is set to planar or triplanar, the Detail UV Mapping is also set to planar or triplanar. By default, a detail texture is linked to the Material to be able to add a micro detail to the Material. To remove this link, uncheck the Lock to base Tiling/Offset checkbox. |
| **Tiling** | Set the tiling of the detail texture inside a tile of the Material. For example, if the Material is tiled by 2 on a plane and the detail is tiled by 2 on the Material, then the detail texture is tiled by 4 on the plane. In this case, you can change the tiling of the Material without having to set the detail UV. |
| **Offset** | Set an X and Y offset for the detail UV. |
| **Detail AlbedoScale** | Use this slider to modulate the detail albedo (red channel) between 0 and 2, like an overlay effect. The default value is 1 and has no scale. |
| **Detail NormalScale** | Use this slider to modulate the intensity of the detail normal map, between 0 and 2. The default value is 1 and has no scale. |
| **Detail SmoothnessScale** | Use this slider modulate the detail smoothness (blue channel) between 0 and 2, like an overlay effect. The default value is 1 and has no scale. |

### Transparency Inputs

Set the __Surface Type__ to __Transparent__ to expose the __Transparency Inputs__ section in the Material Inspector.

| Property| Description |
|:---|:---|
| **Distortion** | Check this box to distort the light passing through this transparent Material. Checking this box exposes the following properties. |
| **- Distortion Blend Mode** | Set the mode HDRP uses to blend overlayed distortion surfaces. |
| **- Distortion Only** | Check this box to only show the distortion effect and set all inputs to have no effect. |
| **- Distortion Depth Test** | Check this box to have closer GameObjects hide the distortion effect, otherwise you can always see the effect. If you do no enable this feature then the distortion appears on top of the rendering. |
| **- Distortion Vector Map** | HDRP uses the red and green channels of this texture to calculate distortion. It also uses the blue channel to manage the blur intensity between 0 and 1. By default, a texture has values between 0 and 1. To be able to produce distortion in either direction, you must remap the distortion texture between -1 and 1. HDRP provides two values you can use to remap this distortion texture. It takes the original value from the map and multiplies it by the value on the left then adds the value on the right. For example, to remap the original values, from 0 to 1, to -1 to 1, enter 2 for the first value and -1 for the second value. |
| **- Distortion Scale** | A multiplier for the distortion effect. Set this to a value higher than 1 to amplify the effect. |
| **- Distortion Blur Scale** | A multiplier for the distortion blur. Set this to a value higher than 1 to amplify the blur. |
| **- Distortion Blur Remapping** | This handle clamps the values of the blue channel of the Distortion Vector Map. Use this to refine the blur setting this handle clamps the values of the blue channel of the distortion vector map. |



### Emissive inputs

| Property| Description |
|:---|:---|
| **Emissive Color** | The emission texture and HDR color this Material uses for emission. If you set an emission texture in this field then HDRP multiplies the emission texture by the HDR color. If you do not set an emission texture then HDRP only uses the HDR color to calculate the final emissive color of the Material. You can set the intensity of the HDR color within the HDR color picker. |
| **- Tiling** | HDRP uses the X and Y values of this property to tile the emissive texture from the Emissive Color property on the object space x-axis and y-axis respectively. |
| **- Offset** | HDRP uses the X and Y values of this property to offset the emissive texture from the Emissive Color property on the object space x-axis and y-axis respectively. |
| **Albedo Affect Emissive** | Allows the albedo to produce color for the emissive texture. To produce the final emissive color, HDRP multiplies the albedo by emissive color and color picker. By default, this setting is enabled. As an example, you can use the emissive color map as an emissive mask, the albedo to do the color and the color picker to modulate mask. |



### Advanced options

| Property| Description |
|:---|:---|
| **Enable GPU instancing** | Tick this checkbox to tell HDRP to render meshes with the same geometry and Material/Shader in one batch when possible. This makes rendering faster. HDRP can not render Meshes in one batch if they have different Materials, or if the hardware does not support GPU instancing. For example, GameObjects with an animation base on the object pivot can’t be static batched (unique pivot for all) but they can be instanced by GPU. |
| **Enable Specular Occlusion from Bent normal** | This option uses the Bent Normal Map to do specular occlusion for the reflection probe. |



